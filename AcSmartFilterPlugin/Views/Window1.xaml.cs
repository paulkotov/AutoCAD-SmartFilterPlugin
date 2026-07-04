using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Autodesk.AutoCAD.DatabaseServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AcSmartFilterPlugin.Views
{
    /// <summary>
    /// Элемент списка слоёв с состоянием выбора (для привязки к чекбоксу).
    /// </summary>
    public class LayerItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public LayerItem(string name, ObjectId id)
        {
            Name = name;
            Id = id;
        }

        public string Name { get; }

        public ObjectId Id { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    /// <summary>
    /// Окно фильтрации объектов чертежа по слоям.
    /// Пользователь отмечает один или несколько слоёв — на экране остаются
    /// только объекты отмеченных слоёв, остальные слои выключаются.
    /// </summary>
    public partial class FilterView : Window
    {
        private readonly ObservableCollection<LayerItem> _layers = new ObservableCollection<LayerItem>();

        // Исходное состояние видимости слоёв на момент открытия окна,
        // чтобы корректно восстановить его при сбросе фильтра.
        private readonly Dictionary<ObjectId, LayerVisibility> _originalState =
            new Dictionary<ObjectId, LayerVisibility>();

        // Текущий слой на момент открытия окна — восстанавливаем его при сбросе.
        private ObjectId _originalClayer;

        // Блокирует реакцию на программное изменение чекбоксов (например, при сбросе).
        private bool _suppressFilter;

        public FilterView()
        {
            InitializeComponent();
            LayerListBox.ItemsSource = _layers;
            Loaded += FilterView_Loaded;
            Closed += FilterView_Closed;
        }

        private void FilterView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadLayers();
        }

        private void FilterView_Closed(object sender, EventArgs e)
        {
            // Возвращаем чертёж в исходное состояние при закрытии окна.
            RestoreOriginalState();
        }

        private void LoadLayers()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return;
            }

            var items = new List<LayerItem>();

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                _originalClayer = doc.Database.Clayer;

                var layerTable = (LayerTable)tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId layerId in layerTable)
                {
                    var layer = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead);
                    items.Add(new LayerItem(layer.Name, layerId));
                    _originalState[layerId] = new LayerVisibility(layer.IsOff, layer.IsFrozen);
                }

                tr.Commit();
            }

            foreach (var item in items.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
            {
                item.PropertyChanged += LayerItem_PropertyChanged;
                _layers.Add(item);
            }
        }

        private void LayerItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_suppressFilter || e.PropertyName != nameof(LayerItem.IsSelected))
            {
                return;
            }

            ApplyFilter();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _suppressFilter = true;
            foreach (var item in _layers)
            {
                item.IsSelected = false;
            }
            _suppressFilter = false;

            RestoreOriginalState();
        }

        /// <summary>
        /// Применяет фильтр: показывает только отмеченные слои, остальные выключает.
        /// Если ничего не отмечено — восстанавливает исходное состояние.
        /// </summary>
        private void ApplyFilter()
        {
            var selectedIds = new HashSet<ObjectId>(
                _layers.Where(l => l.IsSelected).Select(l => l.Id));

            if (selectedIds.Count == 0)
            {
                RestoreOriginalState();
                return;
            }

            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return;
            }

            var db = doc.Database;

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                // Если текущий слой будет выключен, AutoCAD выдаёт предупреждение.
                // Переназначаем текущий слой на один из отмеченных, чтобы этого избежать.
                if (!selectedIds.Contains(db.Clayer))
                {
                    db.Clayer = selectedIds.First();
                }

                var layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId layerId in layerTable)
                {
                    var layer = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForWrite);

                    if (selectedIds.Contains(layerId))
                    {
                        if (layer.IsFrozen)
                        {
                            layer.IsFrozen = false;
                        }

                        layer.IsOff = false;
                    }
                    else
                    {
                        layer.IsOff = true;
                    }
                }

                tr.Commit();
            }

            doc.Editor.Regen();
        }

        /// <summary>
        /// Восстанавливает состояние видимости слоёв, зафиксированное при открытии окна.
        /// </summary>
        private void RestoreOriginalState()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null || _originalState.Count == 0)
            {
                return;
            }

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                // Сначала возвращаем исходный текущий слой (он не был заморожен),
                // чтобы можно было корректно восстановить заморозку прочих слоёв.
                if (!_originalClayer.IsNull && _originalClayer.IsValid)
                {
                    doc.Database.Clayer = _originalClayer;
                }

                var layerTable = (LayerTable)tr.GetObject(doc.Database.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId layerId in layerTable)
                {
                    if (!_originalState.TryGetValue(layerId, out var state))
                    {
                        continue;
                    }

                    var layer = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForWrite);

                    // Замораживать текущий слой нельзя — пропускаем такой случай.
                    if (layer.IsFrozen != state.IsFrozen && !(state.IsFrozen && layerId == doc.Database.Clayer))
                    {
                        layer.IsFrozen = state.IsFrozen;
                    }

                    if (layer.IsOff != state.IsOff)
                    {
                        layer.IsOff = state.IsOff;
                    }
                }

                tr.Commit();
            }

            doc.Editor.Regen();
        }

        /// <summary>
        /// Снимок видимости слоя.
        /// </summary>
        private readonly struct LayerVisibility
        {
            public LayerVisibility(bool isOff, bool isFrozen)
            {
                IsOff = isOff;
                IsFrozen = isFrozen;
            }

            public bool IsOff { get; }

            public bool IsFrozen { get; }
        }
    }
}

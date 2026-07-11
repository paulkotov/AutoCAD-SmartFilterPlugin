using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Autodesk.AutoCAD.DatabaseServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AcSmartFilterPlugin.Views
{
    /// <summary>
    /// Элемент списка слоёв (для отображения в ListBox).
    /// </summary>
    public class LayerItem
    {
        public LayerItem(string name, ObjectId id)
        {
            Name = name;
            Id = id;
        }

        public string Name { get; }

        public ObjectId Id { get; }
    }

    /// <summary>
    /// Окно фильтрации объектов чертежа по слоям.
    /// Пользователь выбирает слои в списке (или включает режим «текущий слой»)
    /// и нажимает «Применить» — на экране остаются только объекты выбранных
    /// слоёв, остальные слои выключаются.
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
                _layers.Add(item);
            }
        }

        private void CurrentLayerCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // В режиме «текущий слой» ручной выбор в списке не используется.
            bool useCurrentLayer = CurrentLayerCheckBox.IsChecked == true;
            LayerListBox.IsEnabled = !useCurrentLayer;

            if (useCurrentLayer)
            {
                LayerListBox.UnselectAll();
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedIds = GetSelectedLayerIds();

            if (selectedIds.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "Выберите хотя бы один слой или включите режим «текущий слой».",
                    "Фильтр по слоям",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            ApplyFilter(selectedIds);
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            LayerListBox.UnselectAll();
            CurrentLayerCheckBox.IsChecked = false;
            RestoreOriginalState();
        }

        /// <summary>
        /// Собирает идентификаторы слоёв, по которым нужно фильтровать:
        /// либо текущий слой чертежа, либо выбранные в списке.
        /// </summary>
        private HashSet<ObjectId> GetSelectedLayerIds()
        {
            if (CurrentLayerCheckBox.IsChecked == true)
            {
                var doc = AcApp.DocumentManager.MdiActiveDocument;
                if (doc == null)
                {
                    return new HashSet<ObjectId>();
                }

                return new HashSet<ObjectId> { doc.Database.Clayer };
            }

            return new HashSet<ObjectId>(
                LayerListBox.SelectedItems.Cast<LayerItem>().Select(l => l.Id));
        }

        /// <summary>
        /// Применяет фильтр: показывает только выбранные слои, остальные выключает.
        /// </summary>
        private void ApplyFilter(HashSet<ObjectId> selectedIds)
        {
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
                // Переназначаем текущий слой на один из выбранных, чтобы этого избежать.
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

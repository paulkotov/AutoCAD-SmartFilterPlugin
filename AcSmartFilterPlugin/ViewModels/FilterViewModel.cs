using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Autodesk.AutoCAD.DatabaseServices;
using AcSmartFilterPlugin.Models;
using AcSmartFilterPlugin.Mvvm;
using AcSmartFilterPlugin.Services;

namespace AcSmartFilterPlugin.ViewModels
{
    /// <summary>
    /// ViewModel окна фильтрации по слоям. Содержит состояние выбора и команды,
    /// а всю работу с чертежом делегирует <see cref="ILayerFilterService"/>.
    /// </summary>
    public sealed class FilterViewModel : INotifyPropertyChanged
    {
        private readonly ILayerFilterService _layerService;
        private readonly IFilterConfigStore _configStore;
        private LayerSnapshot _originalSnapshot;
        private bool _useCurrentLayer;

        public FilterViewModel(ILayerFilterService layerService, IFilterConfigStore configStore)
        {
            _layerService = layerService ?? throw new ArgumentNullException(nameof(layerService));
            _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));

            Layers = new ObservableCollection<LayerListItem>();
            ApplyCommand = new RelayCommand(ApplyFilter, CanApplyFilter);
            ResetCommand = new RelayCommand(Reset);
            CancelCommand = new RelayCommand(Cancel);
        }

        /// <summary>Запрос на закрытие окна. Обрабатывается представлением.</summary>
        public event EventHandler CloseRequested;

        public ObservableCollection<LayerListItem> Layers { get; }

        public ICommand ApplyCommand { get; }

        public ICommand ResetCommand { get; }

        public ICommand CancelCommand { get; }

        /// <summary>Режим «фильтровать по текущему слою».</summary>
        public bool UseCurrentLayer
        {
            get => _useCurrentLayer;
            set
            {
                if (_useCurrentLayer == value)
                {
                    return;
                }

                _useCurrentLayer = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLayerListEnabled));

                // В режиме «текущий слой» ручной выбор в списке не используется.
                if (value)
                {
                    ClearSelection();
                }

                CommandManager.InvalidateRequerySuggested();
            }
        }

        /// <summary>Список слоёв доступен, только если не включён режим «текущий слой».</summary>
        public bool IsLayerListEnabled => !_useCurrentLayer;

        /// <summary>Загружает слои активного чертежа. Вызывается при открытии окна.</summary>
        public void Initialize()
        {
            if (!_layerService.HasActiveDocument)
            {
                return;
            }

            _originalSnapshot = _layerService.LoadLayers();
            if (_originalSnapshot == null)
            {
                return;
            }

            ClearLayers();

            foreach (var info in _originalSnapshot.Layers.OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase))
            {
                var item = new LayerListItem(info.Id, info.Name);
                item.PropertyChanged += OnLayerItemChanged;
                Layers.Add(item);
            }

            RestoreSavedConfig();
        }

        /// <summary>
        /// Читает сохранённую в чертеже конфигурацию фильтра и восстанавливает
        /// выбор слоёв (или режим «текущий слой») при открытии окна.
        /// </summary>
        private void RestoreSavedConfig()
        {
            var config = _configStore.Load();
            if (config == null)
            {
                return;
            }

            if (config.UseCurrentLayer)
            {
                UseCurrentLayer = true;
                return;
            }

            var savedNames = new HashSet<string>(config.LayerNames, StringComparer.OrdinalIgnoreCase);
            foreach (var item in Layers)
            {
                item.IsSelected = savedNames.Contains(item.Name);
            }
        }

        /// <summary>Восстанавливает исходное состояние слоёв. Вызывается при закрытии окна.</summary>
        public void RestoreOriginalState()
        {
            if (_originalSnapshot != null)
            {
                _layerService.RestoreState(_originalSnapshot);
            }
        }

        private bool CanApplyFilter()
            => _useCurrentLayer || Layers.Any(l => l.IsSelected);

        private void ApplyFilter()
        {
            var selectedIds = GetSelectedLayerIds();
            if (selectedIds.Count == 0)
            {
                return;
            }

            _layerService.ApplyFilter(selectedIds);
            SaveCurrentConfig();
        }

        /// <summary>Сохраняет текущую конфигурацию фильтра внутрь чертежа.</summary>
        private void SaveCurrentConfig()
        {
            var selectedNames = _useCurrentLayer
                ? Array.Empty<string>()
                : Layers.Where(l => l.IsSelected).Select(l => l.Name).ToArray();

            _configStore.Save(new FilterConfig(selectedNames, _useCurrentLayer));
        }

        /// <summary>
        /// Сбрасывает весь выбор: снимает выделение слоёв, выключает режим
        /// «текущий слой» и возвращает видимость слоёв к состоянию на момент открытия окна.
        /// </summary>
        private void Reset()
        {
            UseCurrentLayer = false;
            ClearSelection();
            RestoreOriginalState();
        }

        /// <summary>
        /// Закрывает окно без применения фильтра. Откат видимости слоёв выполняет
        /// представление при закрытии через <see cref="RestoreOriginalState"/>.
        /// </summary>
        private void Cancel()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Идентификаторы слоёв для фильтрации: либо текущий слой чертежа,
        /// либо выбранные в списке (в порядке отображения — по имени).
        /// </summary>
        private IReadOnlyCollection<ObjectId> GetSelectedLayerIds()
        {
            if (_useCurrentLayer)
            {
                var currentLayerId = _layerService.GetCurrentLayerId();
                return currentLayerId.IsNull
                    ? Array.Empty<ObjectId>()
                    : new[] { currentLayerId };
            }

            return Layers.Where(l => l.IsSelected).Select(l => l.Id).ToList();
        }

        private void ClearSelection()
        {
            foreach (var item in Layers)
            {
                item.IsSelected = false;
            }
        }

        private void ClearLayers()
        {
            foreach (var item in Layers)
            {
                item.PropertyChanged -= OnLayerItemChanged;
            }

            Layers.Clear();
        }

        private void OnLayerItemChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LayerListItem.IsSelected))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

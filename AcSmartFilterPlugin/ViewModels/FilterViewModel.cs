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
    /// ViewModel окна фильтрации. Содержит состояние выбора и команды,
    /// а работу с чертежом делегирует сервисам слоёв и видимости объектов.
    /// </summary>
    public sealed class FilterViewModel : INotifyPropertyChanged
    {
        private readonly ILayerFilterService _layerService;
        private readonly IPickObjectService _pickService;
        private readonly IEntityVisibilityFilter _entityFilter;
        private readonly IFilterConfigStore _configStore;
        private LayerSnapshot _originalSnapshot;
        private EntityVisibilitySnapshot _hiddenEntities;
        private PickedObjectInfo _pickedObject;
        private bool _useCurrentLayer;
        private bool _filterByObjectType;
        private bool _filterByObjectColor;
        private bool _isPicking;

        public FilterViewModel(
            ILayerFilterService layerService,
            IPickObjectService pickService,
            IEntityVisibilityFilter entityFilter,
            IFilterConfigStore configStore)
        {
            _layerService = layerService ?? throw new ArgumentNullException(nameof(layerService));
            _pickService = pickService ?? throw new ArgumentNullException(nameof(pickService));
            _entityFilter = entityFilter ?? throw new ArgumentNullException(nameof(entityFilter));
            _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));

            Layers = new ObservableCollection<LayerListItem>();
            ApplyCommand = new RelayCommand(ApplyFilter, CanApplyFilter);
            ResetCommand = new RelayCommand(Reset);
            CancelCommand = new RelayCommand(Cancel);
            PickObjectCommand = new RelayCommand(PickObject, CanPickObject);
        }

        /// <summary>Запрос на закрытие окна. Обрабатывается представлением.</summary>
        public event EventHandler CloseRequested;

        /// <summary>Запрос вернуть фокус окну после работы в чертеже.</summary>
        public event EventHandler ActivationRequested;

        public ObservableCollection<LayerListItem> Layers { get; }

        public ICommand ApplyCommand { get; }

        public ICommand ResetCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand PickObjectCommand { get; }

        /// <summary>Последний указанный в чертеже объект (или <c>null</c>).</summary>
        public PickedObjectInfo PickedObject
        {
            get => _pickedObject;
            private set
            {
                _pickedObject = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PickedObjectDescription));
                OnPropertyChanged(nameof(HasPickedObject));
                OnPropertyChanged(nameof(FilterByTypeCaption));
                OnPropertyChanged(nameof(FilterByColorCaption));

                // Новый образец включает оба критерия; сброс объекта их выключает.
                FilterByObjectType = value != null;
                FilterByObjectColor = value != null;
            }
        }

        /// <summary>В чертеже указан объект, с которого можно взять тип и цвет.</summary>
        public bool HasPickedObject => _pickedObject != null;

        /// <summary>Сведения об указанном объекте для отображения в окне.</summary>
        public string PickedObjectDescription => _pickedObject == null
            ? "Объект не указан"
            : $"Тип: {_pickedObject.ObjectType}\nЦвет: {_pickedObject.Color.DisplayName}\nСлой: {_pickedObject.LayerName}\nХэндл: {_pickedObject.Handle}";

        /// <summary>Подпись критерия типа с DXF-именем указанного объекта.</summary>
        public string FilterByTypeCaption => _pickedObject == null
            ? "Фильтровать по типу"
            : $"Фильтровать по типу ({_pickedObject.ObjectType})";

        /// <summary>Подпись критерия цвета с назначением цвета указанного объекта.</summary>
        public string FilterByColorCaption => _pickedObject == null
            ? "Фильтровать по цвету"
            : $"Фильтровать по цвету ({_pickedObject.Color.DisplayName})";

        /// <summary>Скрывать объекты другого типа, чем у указанного.</summary>
        public bool FilterByObjectType
        {
            get => _filterByObjectType;
            set
            {
                if (value && _pickedObject == null)
                {
                    return;
                }

                if (_filterByObjectType == value)
                {
                    return;
                }

                _filterByObjectType = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Скрывать объекты с другим назначением цвета, чем у указанного.</summary>
        public bool FilterByObjectColor
        {
            get => _filterByObjectColor;
            set
            {
                if (value && _pickedObject == null)
                {
                    return;
                }

                if (_filterByObjectColor == value)
                {
                    return;
                }

                _filterByObjectColor = value;
                OnPropertyChanged();
            }
        }

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

        /// <summary>Идёт выбор объектов в чертеже — повторный запрос недопустим.</summary>
        public bool IsPicking
        {
            get => _isPicking;
            private set
            {
                if (_isPicking == value)
                {
                    return;
                }

                _isPicking = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

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

            // Список слоёв заполнен — команды, зависящие от него, могли стать доступны.
            CommandManager.InvalidateRequerySuggested();
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

        /// <summary>
        /// Восстанавливает видимость объектов и исходное состояние слоёв.
        /// Вызывается при сбросе и при закрытии окна.
        /// </summary>
        public void RestoreOriginalState()
        {
            RestoreEntityFilter();

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

            // Предыдущее скрытие снимаем до нового прохода, иначе совпавшие
            // с новыми критериями объекты так и останутся невидимыми.
            RestoreEntityFilter();
            _layerService.ApplyFilter(selectedIds);
            ApplyEntityFilter();
            SaveCurrentConfig();
        }

        /// <summary>
        /// Скрывает объекты, которые не совпали с включёнными критериями
        /// типа и цвета указанного объекта.
        /// </summary>
        private void ApplyEntityFilter()
        {
            if (_pickedObject == null || (!_filterByObjectType && !_filterByObjectColor))
            {
                return;
            }

            var objectType = _filterByObjectType ? _pickedObject.ObjectType : null;
            var color = _filterByObjectColor ? _pickedObject.Color : null;
            var hidden = _entityFilter.HideNonMatching(objectType, color);

            // Объекты, которые не удалось показать на прошлом шаге, остаются в снимке:
            // новый проход их пропускает, потому что они уже невидимы.
            _hiddenEntities = MergeSnapshots(_hiddenEntities, hidden);
        }

        private static EntityVisibilitySnapshot MergeSnapshots(
            EntityVisibilitySnapshot existing,
            EntityVisibilitySnapshot added)
        {
            var hasExisting = existing != null && existing.HiddenEntityIds.Count > 0;
            var hasAdded = added != null && added.HiddenEntityIds.Count > 0;
            if (!hasExisting)
            {
                return added;
            }

            if (!hasAdded)
            {
                return existing;
            }

            return new EntityVisibilitySnapshot(existing.HiddenEntityIds.Concat(added.HiddenEntityIds).Distinct());
        }

        private void RestoreEntityFilter()
        {
            if (_hiddenEntities == null || _hiddenEntities.HiddenEntityIds.Count == 0)
            {
                _hiddenEntities = null;
                return;
            }

            _hiddenEntities = _entityFilter.Restore(_hiddenEntities);
            if (_hiddenEntities.HiddenEntityIds.Count == 0)
            {
                _hiddenEntities = null;
            }
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
        /// Сбрасывает весь выбор: снимает выделение слоёв, выключает режим «текущий слой»,
        /// забывает указанный объект и возвращает видимость слоёв и объектов к состоянию
        /// на момент открытия окна.
        /// </summary>
        private void Reset()
        {
            UseCurrentLayer = false;
            ClearSelection();
            PickedObject = null;
            RestoreOriginalState();
        }

        /// <summary>
        /// Закрывает окно без сохранения фильтра. Откат видимости выполняет
        /// представление при закрытии через <see cref="RestoreOriginalState"/>.
        /// </summary>
        private void Cancel()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool CanPickObject()
            => !_useCurrentLayer && !_isPicking && Layers.Count > 0;

        /// <summary>
        /// Просит указать объект в чертеже, показывает его тип, цвет и слой
        /// и отмечает в списке его слой вместо текущего выбора.
        /// Тип и цвет становятся критериями фильтра до нажатия «Применить».
        /// </summary>
        private async void PickObject()
        {
            if (_isPicking)
            {
                return;
            }

            IsPicking = true;
            try
            {
                var pickedObject = await _pickService.PickObjectAsync();
                if (pickedObject != null)
                {
                    PickedObject = pickedObject;
                    SelectLayer(pickedObject.LayerId);
                }
            }
            catch (Exception)
            {
                // async void: непойманное исключение завершило бы процесс AutoCAD.
            }
            finally
            {
                IsPicking = false;
                ActivationRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Отмечает указанный слой и снимает выбор с остальных. Если слоя нет в списке
        /// (например, он создан после открытия окна), текущий выбор не меняется.
        /// </summary>
        private void SelectLayer(ObjectId layerId)
        {
            if (!Layers.Any(l => l.Id == layerId))
            {
                return;
            }

            foreach (var item in Layers)
            {
                item.IsSelected = item.Id == layerId;
            }
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

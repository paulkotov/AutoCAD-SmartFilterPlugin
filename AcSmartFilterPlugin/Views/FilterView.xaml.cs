using System.Windows;
using AcSmartFilterPlugin.Services;
using AcSmartFilterPlugin.ViewModels;

namespace AcSmartFilterPlugin.Views
{
    /// <summary>
    /// Окно фильтрации объектов чертежа по слоям. Логики в code-behind нет —
    /// всё состояние и поведение вынесены в <see cref="FilterViewModel"/>.
    /// </summary>
    public partial class FilterView : Window
    {
        private readonly FilterViewModel _viewModel;

        public FilterView()
        {
            InitializeComponent();

            _viewModel = new FilterViewModel(new LayerFilterService());
            DataContext = _viewModel;

            Loaded += (s, e) => _viewModel.Initialize();
            Closed += (s, e) => _viewModel.RestoreOriginalState();
        }
    }
}

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace AcSmartFilterPlugin.ViewModels
{
    /// <summary>
    /// Элемент списка слоёв. Хранит признак выбора (<see cref="IsSelected"/>),
    /// который двусторонне связан с состоянием выделения в ListBox.
    /// </summary>
    public sealed class LayerListItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public LayerListItem(ObjectId id, string name)
        {
            Id = id;
            Name = name;
        }

        public ObjectId Id { get; }

        public string Name { get; }

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
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

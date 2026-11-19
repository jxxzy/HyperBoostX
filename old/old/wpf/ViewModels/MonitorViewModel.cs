using System.ComponentModel;

namespace HyperBoostX.ViewModels
{
    public class MonitorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _memoryUsage = "0%";

        public string MemoryUsage
        {
            get => _memoryUsage;
            set
            {
                if (_memoryUsage != value)
                {
                    _memoryUsage = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MemoryUsage)));
                }
            }
        }
    }
}

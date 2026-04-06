using System.ComponentModel;

namespace HyperBoostX.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _cpuUsage = "0%";

        public string CpuUsage
        {
            get => _cpuUsage;
            set
            {
                if (_cpuUsage != value)
                {
                    _cpuUsage = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CpuUsage)));
                }
            }
        }
    }
}

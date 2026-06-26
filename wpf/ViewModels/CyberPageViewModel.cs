using System.Collections.ObjectModel;

namespace HyperBoostX.ViewModels
{
    public abstract class CyberPageViewModel : BaseViewModel
    {
        private string _status = "Ready";

        protected CyberPageViewModel(string title, string subtitle)
        {
            Title = title;
            Subtitle = subtitle;
        }

        public string Title { get; }
        public string Subtitle { get; }
        public ObservableCollection<CyberMetricViewModel> Metrics { get; } = new();
        public ObservableCollection<string> Recommendations { get; } = new();
        public string PrimaryAction { get; protected set; } = "Preview safe plan";
        public string Status { get => _status; set => SetProperty(ref _status, value); }
    }
}

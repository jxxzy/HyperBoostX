using System.Collections.ObjectModel;

namespace HyperBoostX.ViewModels
{
    public abstract class CyberPageViewModel : BaseViewModel
    {
        private string _status = "Ready";
        private string _liveResultTitle = "Live Result";
        private string _liveResult = "Run a feature action to load live backend data.";
        private string _lastUpdated = "Not run yet";
        private bool _isBusy;

        protected CyberPageViewModel(string title, string subtitle, string featureKey = null)
        {
            Title = title;
            Subtitle = subtitle;
            FeatureKey = featureKey;
            LegacyFeatureCatalog.Apply(this);
            var actionKey = string.IsNullOrWhiteSpace(FeatureKey)
                ? GetType().Name.Replace("ViewModel", string.Empty)
                : FeatureKey;
            foreach (var action in FeatureActionCatalog.LoadFor(actionKey))
                FeatureActions.Add(action);
        }

        public string Title { get; }
        public string Subtitle { get; }
        public string FeatureKey { get; }
        public ObservableCollection<CyberMetricViewModel> Metrics { get; } = new();
        public ObservableCollection<string> Recommendations { get; } = new();
        public ObservableCollection<LegacyToolViewModel> LegacyTools { get; } = new();
        public ObservableCollection<FeatureActionViewModel> FeatureActions { get; } = new();
        public bool HasLegacyTools => LegacyTools.Count > 0;
        public bool HasFeatureActions => FeatureActions.Count > 0;
        public string PrimaryAction { get; protected set; } = "Preview safe plan";
        public string Status { get => _status; set => SetProperty(ref _status, value); }
        public string LiveResultTitle { get => _liveResultTitle; set => SetProperty(ref _liveResultTitle, value); }
        public string LiveResult { get => _liveResult; set => SetProperty(ref _liveResult, value); }
        public string LastUpdated { get => _lastUpdated; set => SetProperty(ref _lastUpdated, value); }
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
    }
}

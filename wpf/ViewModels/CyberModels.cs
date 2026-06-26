namespace HyperBoostX.ViewModels
{
    public sealed class CyberMetricViewModel : BaseViewModel
    {
        private string _title = "Metric";
        private string _value = "--";
        private string _detail = "Waiting for scan";
        private int _score;
        private string _glyph = "*";

        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string Value { get => _value; set => SetProperty(ref _value, value); }
        public string Detail { get => _detail; set => SetProperty(ref _detail, value); }
        public int Score { get => _score; set => SetProperty(ref _score, value); }
        public string Glyph { get => _glyph; set => SetProperty(ref _glyph, value); }
    }

    public sealed class NavigationItemViewModel : BaseViewModel
    {
        private bool _isActive;

        public string Key { get; set; } = "Dashboard";
        public string Label { get; set; } = "Dashboard";
        public string Glyph { get; set; } = ">";
        public string Group { get; set; } = "Overview";
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (SetProperty(ref _isActive, value))
                    OnPropertyChanged(nameof(ActiveTag));
            }
        }
        public string ActiveTag => IsActive ? "Active" : "";
    }
}

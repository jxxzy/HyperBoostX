namespace HyperBoostX.ViewModels
{
    public sealed class SettingsViewModel : PlacementPageViewModel
    {
        private bool _enableAnimations = true;
        private bool _reduceMotion;
        private string _accentColor = "Cyan";
        private string _mode = "Beginner";

        public SettingsViewModel() : base("Settings", "Theme, motion, safety mode, and privacy-first preferences.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Telemetry", Value = "OFF", Detail = "Opt-in only", Score = 100, Glyph = "PR" });
            Metrics.Add(new CyberMetricViewModel { Title = "Mode", Value = "BEGINNER", Detail = "Safe defaults", Score = 94, Glyph = "MD" });
            Recommendations.Add("Reduce Motion disables scanner and pulse effects.");
            Recommendations.Add("Expert Preview remains off by default.");
            PrimaryAction = "Save Settings";
        }

        public bool EnableAnimations { get => _enableAnimations; set => SetProperty(ref _enableAnimations, value); }
        public bool ReduceMotion { get => _reduceMotion; set => SetProperty(ref _reduceMotion, value); }
        public string AccentColor { get => _accentColor; set => SetProperty(ref _accentColor, value); }
        public string Mode { get => _mode; set => SetProperty(ref _mode, value); }
    }
}

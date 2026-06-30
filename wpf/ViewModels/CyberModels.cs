using Newtonsoft.Json.Linq;

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

    public sealed class LegacyToolViewModel : BaseViewModel
    {
        private string _category = "Legacy tool";
        private string _title = "Tool";
        private string _flow = "Preview-first";
        private string _safety = "Safety Guard";
        private string _route = "Open page action";

        public string Category { get => _category; set => SetProperty(ref _category, value); }
        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string Flow { get => _flow; set => SetProperty(ref _flow, value); }
        public string Safety { get => _safety; set => SetProperty(ref _safety, value); }
        public string Route { get => _route; set => SetProperty(ref _route, value); }
    }

    public sealed class FeatureActionViewModel : BaseViewModel
    {
        private string _status = "Real";
        private bool _isEnabled = true;

        public string Id { get; set; } = "feature.action";
        public string MenuKey { get; set; } = "Dashboard";
        public string Label { get; set; } = "Run action";
        public string Command { get; set; } = "RunActionCommand";
        public string Method { get; set; } = "GET";
        public string Path { get; set; } = "/api/health";
        public JObject Payload { get; set; }
        public bool RequiresAdmin { get; set; }
        public bool PreviewRequired { get; set; } = true;
        public bool ConfirmationRequired { get; set; }
        public bool SafetyGuard { get; set; } = true;
        public bool Restore { get; set; } = true;
        public bool IsDestructive { get; set; }
        public bool Partial { get; set; }
        public string TestCoverage { get; set; } = "tests/test_ui_action_map_v210.py";
        public string Tooltip { get; set; } = "Preview-first local backend action";
        public string SuccessState { get; set; } = "Success state updates Live Result";
        public string ErrorState { get; set; } = "Failure is rendered as a safe human-friendly message";
        public string LoadingState { get; set; } = "Buttons disabled while backend call is running";
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }
    }

    public sealed class NavigationItemViewModel : BaseViewModel
    {
        private bool _isActive;
        private string _status = "Real";

        public string Key { get; set; } = "Dashboard";
        public string Label { get; set; } = "Dashboard";
        public string Glyph { get; set; } = ">";
        public string Group { get; set; } = "Overview";
        public string Status { get => _status; set => SetProperty(ref _status, value); }
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

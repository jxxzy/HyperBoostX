using System.Collections.ObjectModel;

namespace HyperBoostX.ViewModels
{
    public sealed class DashboardViewModel : BaseViewModel
    {
        private string _backendStatus = "Connecting";
        private string _activeGpu = "Run Smart Scan first";
        private string _aiRecommendation = "Run Smart Scan to diagnose bottlenecks, overlays, startup pressure, and restore readiness.";
        private bool _isBusy;

        public ObservableCollection<CyberMetricViewModel> Scores { get; } = new()
        {
            new CyberMetricViewModel { Title = "PC Health", Value = "Scan", Detail = "Run Smart Scan first", Score = 0, Glyph = "HLT" },
            new CyberMetricViewModel { Title = "Gaming Readiness", Value = "Scan", Detail = "Run Smart Scan first", Score = 0, Glyph = "GM" },
            new CyberMetricViewModel { Title = "Streaming Readiness", Value = "Scan", Detail = "Run Smart Scan first", Score = 0, Glyph = "STR" },
            new CyberMetricViewModel { Title = "Storage Score", Value = "Scan", Detail = "Run Smart Scan first", Score = 0, Glyph = "SSD" },
            new CyberMetricViewModel { Title = "Network Score", Value = "Scan", Detail = "Run Smart Scan first", Score = 0, Glyph = "NET" },
            new CyberMetricViewModel { Title = "Safety Score", Value = "Guard", Detail = "Dangerous tweaks blocked", Score = 100, Glyph = "SAFE" }
        };

        public ObservableCollection<CyberMetricViewModel> SystemMetrics { get; } = new()
        {
            new CyberMetricViewModel { Title = "CPU", Value = "Run scan", Detail = "Run Smart Scan first", Score = 0, Glyph = "CPU" },
            new CyberMetricViewModel { Title = "RAM", Value = "Run scan", Detail = "Run Smart Scan first", Score = 0, Glyph = "RAM" },
            new CyberMetricViewModel { Title = "GPU", Value = "Run scan", Detail = "Sensor unavailable until backend scan", Score = 0, Glyph = "GPU" },
            new CyberMetricViewModel { Title = "VRAM", Value = "Run scan", Detail = "Sensor unavailable until backend scan", Score = 0, Glyph = "VR" },
            new CyberMetricViewModel { Title = "Storage", Value = "Run scan", Detail = "Safe cleanup allowlist", Score = 0, Glyph = "SSD" },
            new CyberMetricViewModel { Title = "Network", Value = "Run scan", Detail = "Use Network Tools for diagnostics", Score = 0, Glyph = "NET" },
            new CyberMetricViewModel { Title = "Power Plan", Value = "Unknown", Detail = "Permission may be required to read/change", Score = 50, Glyph = "PWR" },
            new CyberMetricViewModel { Title = "Active Game", Value = "Detect", Detail = "Auto gaming waits for local process detection", Score = 0, Glyph = "GAME" },
            new CyberMetricViewModel { Title = "Overlays", Value = "Review", Detail = "Run Smart Scan for detected overlays", Score = 0, Glyph = "OVR" },
            new CyberMetricViewModel { Title = "Restore", Value = "No changes", Detail = "Restore sessions appear after approved actions", Score = 80, Glyph = "RST" },
            new CyberMetricViewModel { Title = "Backend", Value = "Checking", Detail = "127.0.0.1 local API", Score = 50, Glyph = "API" },
            new CyberMetricViewModel { Title = "Safety Guard", Value = "Active", Detail = "Blocks unsafe actions", Score = 100, Glyph = "SAFE" },
            new CyberMetricViewModel { Title = "Release", Value = "2.10.0", Detail = "Stable source and local gates; signing remains documented separately", Score = 90, Glyph = "REL" }
        };

        public string BackendStatus
        {
            get => _backendStatus;
            set => SetProperty(ref _backendStatus, value);
        }

        public string ActiveGpu
        {
            get => _activeGpu;
            set => SetProperty(ref _activeGpu, value);
        }

        public string AiRecommendation
        {
            get => _aiRecommendation;
            set => SetProperty(ref _aiRecommendation, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }
    }
}

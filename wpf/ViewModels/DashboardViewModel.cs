using System.Collections.ObjectModel;

namespace HyperBoostX.ViewModels
{
    public sealed class DashboardViewModel : BaseViewModel
    {
        private string _backendStatus = "Connecting";
        private string _activeGpu = "Detecting GPU";
        private string _aiRecommendation = "Run Smart Scan to diagnose bottlenecks, overlays, startup pressure, and restore readiness.";

        public ObservableCollection<CyberMetricViewModel> Scores { get; } = new()
        {
            new CyberMetricViewModel { Title = "PC Health", Value = "92", Detail = "Safe local profile", Score = 92, Glyph = "H" },
            new CyberMetricViewModel { Title = "Gaming Readiness", Value = "88", Detail = "Overlays need review", Score = 88, Glyph = "G" },
            new CyberMetricViewModel { Title = "Streaming Readiness", Value = "84", Detail = "Encoder-ready", Score = 84, Glyph = "S" },
            new CyberMetricViewModel { Title = "Startup Cleanliness", Value = "79", Detail = "Review startup apps", Score = 79, Glyph = "B" },
            new CyberMetricViewModel { Title = "Network Score", Value = "90", Detail = "DNS route healthy", Score = 90, Glyph = "N" },
            new CyberMetricViewModel { Title = "Safety Score", Value = "100", Detail = "Dangerous tweaks blocked", Score = 100, Glyph = "SG" }
        };

        public ObservableCollection<CyberMetricViewModel> SystemMetrics { get; } = new()
        {
            new CyberMetricViewModel { Title = "CPU", Value = "--%", Detail = "Live backend metric", Score = 38, Glyph = "CPU" },
            new CyberMetricViewModel { Title = "RAM", Value = "--%", Detail = "Memory pressure", Score = 50, Glyph = "RAM" },
            new CyberMetricViewModel { Title = "GPU", Value = "--%", Detail = "Vendor-aware mode", Score = 11, Glyph = "GPU" },
            new CyberMetricViewModel { Title = "VRAM", Value = "Guide", Detail = "Shown when telemetry is available", Score = 76, Glyph = "VR" },
            new CyberMetricViewModel { Title = "Storage", Value = "Scan", Detail = "Safe cleanup allowlist", Score = 84, Glyph = "SSD" },
            new CyberMetricViewModel { Title = "Network", Value = "17 ms", Detail = "Local DNS route", Score = 94, Glyph = "NET" },
            new CyberMetricViewModel { Title = "Power Plan", Value = "Balanced", Detail = "No forced unsafe tweak", Score = 88, Glyph = "PWR" },
            new CyberMetricViewModel { Title = "Active Game", Value = "None", Detail = "Auto gaming waits for detection", Score = 70, Glyph = "GAME" },
            new CyberMetricViewModel { Title = "Overlays", Value = "Review", Detail = "Discord, Steam, capture tools", Score = 78, Glyph = "OVR" },
            new CyberMetricViewModel { Title = "Restore", Value = "Available", Detail = "Undo remains visible", Score = 100, Glyph = "RST" },
            new CyberMetricViewModel { Title = "Backend", Value = "Checking", Detail = "127.0.0.1 local API", Score = 50, Glyph = "API" },
            new CyberMetricViewModel { Title = "Safety Guard", Value = "Active", Detail = "Blocks unsafe actions", Score = 100, Glyph = "SAFE" }
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
    }
}

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
            new CyberMetricViewModel { Title = "PC Health", Value = "Pending", Detail = "Smart Scan has not run yet", Score = 0, Glyph = "HLT" },
            new CyberMetricViewModel { Title = "Gaming Readiness", Value = "Pending", Detail = "Smart Scan has not run yet", Score = 0, Glyph = "GM" },
            new CyberMetricViewModel { Title = "Streaming Readiness", Value = "Pending", Detail = "Smart Scan has not run yet", Score = 0, Glyph = "STR" },
            new CyberMetricViewModel { Title = "Storage Score", Value = "Pending", Detail = "Smart Scan has not run yet", Score = 0, Glyph = "SSD" },
            new CyberMetricViewModel { Title = "Network Score", Value = "Pending", Detail = "Smart Scan has not run yet", Score = 0, Glyph = "NET" },
            new CyberMetricViewModel { Title = "Safety Score", Value = "Pending", Detail = "Smart Scan has not run yet", Score = 0, Glyph = "SAFE" }
        };

        public ObservableCollection<CyberMetricViewModel> SystemMetrics { get; } = new()
        {
            new CyberMetricViewModel { Title = "CPU", Value = "Checking", Detail = "Live backend metric when available", Score = 0, Glyph = "CPU" },
            new CyberMetricViewModel { Title = "RAM", Value = "Checking", Detail = "Live backend metric when available", Score = 0, Glyph = "RAM" },
            new CyberMetricViewModel { Title = "GPU", Value = "Checking", Detail = "Hardware API evidence when available", Score = 0, Glyph = "GPU" },
            new CyberMetricViewModel { Title = "VRAM", Value = "Checking", Detail = "VRAM sensor evidence when available", Score = 0, Glyph = "VR" },
            new CyberMetricViewModel { Title = "Storage", Value = "Checking", Detail = "System drive usage from backend", Score = 0, Glyph = "SSD" },
            new CyberMetricViewModel { Title = "Network", Value = "Checking", Detail = "Throughput snapshot; DNS tools handle latency", Score = 0, Glyph = "NET" },
            new CyberMetricViewModel { Title = "Backend", Value = "Checking", Detail = "127.0.0.1 local API", Score = 0, Glyph = "API" },
            new CyberMetricViewModel { Title = "Safety Guard", Value = "Active", Detail = "Blocks unsafe actions", Score = 100, Glyph = "SAFE" },
            new CyberMetricViewModel { Title = "Restore", Value = "No changes", Detail = "Restore sessions appear after approved actions", Score = 0, Glyph = "RST" },
            new CyberMetricViewModel { Title = "Active Game", Value = "Detect", Detail = "Auto gaming waits for local process detection", Score = 0, Glyph = "GAME" },
            new CyberMetricViewModel { Title = "Overlays", Value = "Review", Detail = "Run Smart Scan for detected overlays", Score = 0, Glyph = "OVR" }
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

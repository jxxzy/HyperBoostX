using System.Collections.ObjectModel;

namespace HyperBoostX.ViewModels
{
    public sealed class MainWindowViewModel : BaseViewModel
    {
        private string _pageTitle = "Dashboard";
        private string _pageSubtitle = "Safe AI Windows Gaming Optimizer";
        private string _backendStatus = "Connecting";
        private string _backendBadge = "CONNECTING";
        private string _activeGpu = "GPU scanning";
        private string _currentMode = "Beginner";
        private bool _restoreAvailable = true;
        private bool _animationsEnabled = true;
        private bool _reduceMotion;
        private string _accentColor = "Cyan";
        private string _toastMessage = "Cyber UI loaded";

        public ObservableCollection<NavigationItemViewModel> NavigationItems { get; } = new()
        {
            new NavigationItemViewModel { Key = "Dashboard", Label = "Dashboard", Glyph = "01", Group = "Overview", IsActive = true },
            new NavigationItemViewModel { Key = "AIPerformanceAdvisor", Label = "AI Advisor", Glyph = "02", Group = "Overview" },
            new NavigationItemViewModel { Key = "AutoGamingMode", Label = "Auto Gaming", Glyph = "03", Group = "Gaming" },
            new NavigationItemViewModel { Key = "GameLibrary", Label = "Game Library", Glyph = "04", Group = "Gaming" },
            new NavigationItemViewModel { Key = "GameProfiles", Label = "Game Profiles", Glyph = "05", Group = "Gaming" },
            new NavigationItemViewModel { Key = "GpuCenter", Label = "GPU Center", Glyph = "06", Group = "Gaming" },
            new NavigationItemViewModel { Key = "HyperBalance", Label = "HyperBalance", Glyph = "07", Group = "Optimization" },
            new NavigationItemViewModel { Key = "OneClickBoost", Label = "One Click Boost", Glyph = "08", Group = "Optimization" },
            new NavigationItemViewModel { Key = "ProcessAnalyzer", Label = "Process Analyzer", Glyph = "09", Group = "Optimization" },
            new NavigationItemViewModel { Key = "StartupManager", Label = "Startup Manager", Glyph = "10", Group = "Optimization" },
            new NavigationItemViewModel { Key = "Cleanup", Label = "Cleanup", Glyph = "11", Group = "Optimization" },
            new NavigationItemViewModel { Key = "NetworkTools", Label = "Network Tools", Glyph = "12", Group = "Optimization" },
            new NavigationItemViewModel { Key = "BenchmarkLab", Label = "Benchmark Lab", Glyph = "13", Group = "Reports" },
            new NavigationItemViewModel { Key = "PerformanceHistory", Label = "Performance History", Glyph = "14", Group = "Reports" },
            new NavigationItemViewModel { Key = "PerformanceReport", Label = "Performance Report", Glyph = "15", Group = "Reports" },
            new NavigationItemViewModel { Key = "StreamingCenter", Label = "Streaming Center", Glyph = "16", Group = "Modes" },
            new NavigationItemViewModel { Key = "CreatorMode", Label = "Creator Mode", Glyph = "17", Group = "Modes" },
            new NavigationItemViewModel { Key = "GamingEssentials", Label = "Gaming Essentials", Glyph = "18", Group = "Modes" },
            new NavigationItemViewModel { Key = "RestoreBackup", Label = "Restore & Backup", Glyph = "19", Group = "Safety" },
            new NavigationItemViewModel { Key = "ProtectedApps", Label = "Protected Apps", Glyph = "20", Group = "Safety" },
            new NavigationItemViewModel { Key = "KnowledgeBase", Label = "Knowledge Base", Glyph = "21", Group = "System" },
            new NavigationItemViewModel { Key = "Settings", Label = "Settings", Glyph = "22", Group = "System" },
            new NavigationItemViewModel { Key = "FeatureAudit", Label = "Feature Audit", Glyph = "23", Group = "System" },
            new NavigationItemViewModel { Key = "About", Label = "About", Glyph = "24", Group = "System" }
        };

        public string PageTitle { get => _pageTitle; set => SetProperty(ref _pageTitle, value); }
        public string PageSubtitle { get => _pageSubtitle; set => SetProperty(ref _pageSubtitle, value); }
        public string BackendStatus { get => _backendStatus; set => SetProperty(ref _backendStatus, value); }
        public string BackendBadge { get => _backendBadge; set => SetProperty(ref _backendBadge, value); }
        public string ActiveGpu { get => _activeGpu; set => SetProperty(ref _activeGpu, value); }
        public string CurrentMode { get => _currentMode; set => SetProperty(ref _currentMode, value); }
        public bool RestoreAvailable { get => _restoreAvailable; set => SetProperty(ref _restoreAvailable, value); }
        public bool AnimationsEnabled { get => _animationsEnabled; set => SetProperty(ref _animationsEnabled, value); }
        public bool ReduceMotion { get => _reduceMotion; set => SetProperty(ref _reduceMotion, value); }
        public string AccentColor { get => _accentColor; set => SetProperty(ref _accentColor, value); }
        public string ToastMessage { get => _toastMessage; set => SetProperty(ref _toastMessage, value); }
    }
}

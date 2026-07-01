using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

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
        private string _runtimeMode = "Stable";
        private bool _restoreAvailable = true;
        private bool _animationsEnabled = true;
        private bool _reduceMotion;
        private string _accentColor = "Cyan";
        private string _toastMessage = "Cyber UI loaded";
        private string _searchText = "";
        private readonly List<NavigationItemViewModel> _allNavigationItems;
        private List<NavigationItemViewModel> _runtimeNavigationItems = new();

        private static readonly IReadOnlyList<string> BeginnerNavigationKeys = new[]
        {
            "Dashboard",
            "OneClickBoost",
            "AutoGamingMode",
            "AIPerformanceAdvisor",
            "PerformanceBoost",
            "StartupManager",
            "BackgroundApps",
            "Cleanup",
            "Storage",
            "GpuCenter",
            "GamingBooster",
            "StreamingCenter",
            "CreatorMode",
            "NetworkBooster",
            "DnsLatencyTools",
            "PrivacyCenter",
            "SecurityHealth",
            "AppsManager",
            "TweaksCenter",
            "WindowsFeatures",
            "UpdateControl",
            "RepairTools",
            "DriverUpdateCenter",
            "AppUninstaller",
            "RestoreBackup",
            "Settings",
            "About"
        };

        private static readonly IReadOnlyList<string> AdvancedNavigationKeys = BeginnerNavigationKeys
            .Concat(new[]
            {
                "SmartScan",
                "HyperBoostScore",
                "CpuTurboDiagnostic",
                "CpuRamOptimizer",
                "HyperBalance",
                "ProcessAnalyzer",
                "DriverRecommendation",
                "OverlayConflictDetector",
                "GameLibrary",
                "GameProfiles",
                "AdvancedMicMixer",
                "WebcamStudio",
                "CameraTracking",
                "NetworkOptimization",
                "NetworkTools",
                "SystemRealityGuard",
                "LcdPerformanceGuard",
                "DefenderScanGuard",
                "SecurityRealityAudit",
                "ProtectedApps",
                "AdvancedTweaks",
                "WindowsServices",
                "PowerOptimization",
                "VisualEffects",
                "MsiSafeOptimizer",
                "RestorePointManager",
                "Reports",
                "PerformanceHistory",
                "PerformanceReport",
                "ScheduledAutomation",
                "TaskRuleSystem",
                "UtilitiesTools",
                "BenchmarkLab",
                "ReleaseReadiness",
                "FeatureAudit",
                "MasterTestEngine",
                "FeatureAuditMatrix",
                "KnowledgeBase"
            })
            .Distinct()
            .ToList();

        public ObservableCollection<NavigationItemViewModel> NavigationItems { get; } = new()
        {
            new NavigationItemViewModel { Key = "OneClickBoost", Label = "One Click Boost", Glyph = "OB", Group = "Quick Access" },
            new NavigationItemViewModel { Key = "AutoGamingMode", Label = "Gaming Mode", Glyph = "GM", Group = "Quick Access" },
            new NavigationItemViewModel { Key = "SmartScan", Label = "Smart Scan", Glyph = "SS", Group = "Quick Access" },
            new NavigationItemViewModel { Key = "AIPerformanceAdvisor", Label = "Smart Recommendation", Glyph = "SR", Group = "Quick Access" },
            new NavigationItemViewModel { Key = "AICenter", Label = "AI Center", Glyph = "AI", Group = "Quick Access" },
            new NavigationItemViewModel { Key = "NvidiaCopilot", Label = "NVIDIA Copilot", Glyph = "NV", Group = "Quick Access" },

            new NavigationItemViewModel { Key = "Dashboard", Label = "Dashboard", Glyph = "DB", Group = "Performance", IsActive = true },
            new NavigationItemViewModel { Key = "HyperBoostScore", Label = "HyperBoost Score", Glyph = "HS", Group = "Performance" },
            new NavigationItemViewModel { Key = "PerformanceBoost", Label = "Performance", Glyph = "PF", Group = "Performance" },
            new NavigationItemViewModel { Key = "CpuTurboDiagnostic", Label = "CPU Turbo Diagnostic", Glyph = "CPU", Group = "Performance" },
            new NavigationItemViewModel { Key = "CpuRamOptimizer", Label = "CPU/RAM Optimizer", Glyph = "CR", Group = "Performance" },
            new NavigationItemViewModel { Key = "StartupManager", Label = "Startup", Glyph = "ST", Group = "Performance" },
            new NavigationItemViewModel { Key = "BackgroundApps", Label = "Background Apps", Glyph = "BG", Group = "Performance" },
            new NavigationItemViewModel { Key = "HyperBalance", Label = "HyperBalance", Glyph = "HB", Group = "Performance" },
            new NavigationItemViewModel { Key = "ProcessAnalyzer", Label = "Process Analyzer", Glyph = "PROC", Group = "Performance" },
            new NavigationItemViewModel { Key = "Cleanup", Label = "Cleanup", Glyph = "CL", Group = "Performance" },
            new NavigationItemViewModel { Key = "Storage", Label = "Storage", Glyph = "DISK", Group = "Performance" },

            new NavigationItemViewModel { Key = "GpuCenter", Label = "GPU Center", Glyph = "GPU", Group = "Gaming & Creator" },
            new NavigationItemViewModel { Key = "DriverRecommendation", Label = "Driver Recommendation", Glyph = "DR", Group = "Gaming & Creator" },
            new NavigationItemViewModel { Key = "OverlayConflictDetector", Label = "Overlay Conflict Detector", Glyph = "OC", Group = "Gaming & Creator" },
            new NavigationItemViewModel { Key = "RgbSoftwareDetector", Label = "RGB Software Detector", Glyph = "RGB", Group = "Gaming & Creator" },
            new NavigationItemViewModel { Key = "GamingBooster", Label = "Gaming Booster", Glyph = "GB", Group = "Gaming & Creator" },
            new NavigationItemViewModel { Key = "GameLibrary", Label = "Game Library", Glyph = "GL", Group = "Gaming & Creator" },
            new NavigationItemViewModel { Key = "GameProfiles", Label = "Game Profiles", Glyph = "GP", Group = "Gaming & Creator" },
            new NavigationItemViewModel { Key = "StreamingCenter", Label = "Streaming Mode", Glyph = "SC", Group = "Gaming & Creator" },
            new NavigationItemViewModel { Key = "CreatorMode", Label = "Creator Mode", Glyph = "CM", Group = "Gaming & Creator" },
            new NavigationItemViewModel { Key = "AdvancedMicMixer", Label = "Voice Meter / Mic Mixer", Glyph = "MIC", Group = "Gaming & Creator" },
            new NavigationItemViewModel { Key = "WebcamStudio", Label = "Webcam Diagnostics", Glyph = "CAM", Group = "Gaming & Creator" },
            new NavigationItemViewModel { Key = "CameraTracking", Label = "Camera Tracking", Glyph = "TRK", Group = "Gaming & Creator" },

            new NavigationItemViewModel { Key = "NetworkBooster", Label = "Network Booster", Glyph = "NB", Group = "Network" },
            new NavigationItemViewModel { Key = "DnsLatencyTools", Label = "DNS & Latency Tools", Glyph = "DNS", Group = "Network" },
            new NavigationItemViewModel { Key = "NetworkOptimization", Label = "Network Optimization", Glyph = "NO", Group = "Network" },
            new NavigationItemViewModel { Key = "NetworkTools", Label = "Network Tools", Glyph = "NW", Group = "Network" },

            new NavigationItemViewModel { Key = "PrivacyCenter", Label = "Privacy Center", Glyph = "PV", Group = "Privacy & Security" },
            new NavigationItemViewModel { Key = "SecurityHealth", Label = "Security & Health", Glyph = "SH", Group = "Privacy & Security" },
            new NavigationItemViewModel { Key = "SystemRealityGuard", Label = "System Reality Guard", Glyph = "SRG", Group = "Privacy & Security" },
            new NavigationItemViewModel { Key = "LcdPerformanceGuard", Label = "LCD Performance Guard", Glyph = "LCD", Group = "Privacy & Security" },
            new NavigationItemViewModel { Key = "DefenderScanGuard", Label = "Defender Scan Guard", Glyph = "DEF", Group = "Privacy & Security" },
            new NavigationItemViewModel { Key = "SecurityRealityAudit", Label = "Security Reality Audit", Glyph = "SRA", Group = "Privacy & Security" },
            new NavigationItemViewModel { Key = "ProtectedApps", Label = "Protected Apps", Glyph = "PA", Group = "Privacy & Security" },

            new NavigationItemViewModel { Key = "AppsManager", Label = "Apps Manager", Glyph = "AM", Group = "App Management" },

            new NavigationItemViewModel { Key = "TweaksCenter", Label = "Tweaks Center", Glyph = "TW", Group = "System Config" },
            new NavigationItemViewModel { Key = "WindowsFeatures", Label = "Windows Features", Glyph = "WF", Group = "System Config" },
            new NavigationItemViewModel { Key = "UpdateControl", Label = "Update Control", Glyph = "UP", Group = "System Config" },

            new NavigationItemViewModel { Key = "RepairTools", Label = "Repair Tools", Glyph = "RT", Group = "System Tools" },
            new NavigationItemViewModel { Key = "DriverUpdateCenter", Label = "Driver & Update Center", Glyph = "DU", Group = "System Tools" },
            new NavigationItemViewModel { Key = "AppUninstaller", Label = "App Uninstaller", Glyph = "AU", Group = "System Tools" },
            new NavigationItemViewModel { Key = "GamingEssentials", Label = "Gaming Essentials", Glyph = "GE", Group = "System Tools" },

            new NavigationItemViewModel { Key = "AdvancedTweaks", Label = "Advanced Tweaks", Glyph = "AT", Group = "Advanced System" },
            new NavigationItemViewModel { Key = "WindowsServices", Label = "Windows Services", Glyph = "WS", Group = "Advanced System" },
            new NavigationItemViewModel { Key = "PowerOptimization", Label = "Power Optimization", Glyph = "PW", Group = "Advanced System" },
            new NavigationItemViewModel { Key = "VisualEffects", Label = "Visual Effects", Glyph = "FX", Group = "Advanced System" },
            new NavigationItemViewModel { Key = "MsiSafeOptimizer", Label = "MSI Safe Optimizer", Glyph = "MSI", Group = "Advanced System" },

            new NavigationItemViewModel { Key = "RestoreBackup", Label = "Restore & Backup", Glyph = "RS", Group = "Backup & Restore" },
            new NavigationItemViewModel { Key = "RestorePointManager", Label = "Restore Point Manager", Glyph = "RP", Group = "Backup & Restore" },
            new NavigationItemViewModel { Key = "Reports", Label = "Reports", Glyph = "REP", Group = "Backup & Restore" },
            new NavigationItemViewModel { Key = "PerformanceHistory", Label = "Performance History", Glyph = "PH", Group = "Backup & Restore" },
            new NavigationItemViewModel { Key = "PerformanceReport", Label = "Performance Report", Glyph = "PR", Group = "Backup & Restore" },

            new NavigationItemViewModel { Key = "ScheduledAutomation", Label = "Scheduled Automation", Glyph = "SA", Group = "Automation" },
            new NavigationItemViewModel { Key = "TaskRuleSystem", Label = "Task & Rule System", Glyph = "TR", Group = "Automation" },

            new NavigationItemViewModel { Key = "UtilitiesTools", Label = "Utilities Tools", Glyph = "UT", Group = "Extra Tools" },
            new NavigationItemViewModel { Key = "BenchmarkLab", Label = "Benchmark Lab", Glyph = "BM", Group = "Extra Tools" },
            new NavigationItemViewModel { Key = "PluginMarketplace", Label = "Plugin Marketplace", Glyph = "PLG", Group = "Extra Tools" },
            new NavigationItemViewModel { Key = "CloudSyncLicense", Label = "Cloud & License Boundary", Glyph = "CLD", Group = "Extra Tools" },
            new NavigationItemViewModel { Key = "ReleaseReadiness", Label = "Release Readiness", Glyph = "RR", Group = "Extra Tools" },
            new NavigationItemViewModel { Key = "FeatureAudit", Label = "Feature Audit", Glyph = "FA", Group = "Extra Tools" },
            new NavigationItemViewModel { Key = "MasterTestEngine", Label = "Master Test Engine", Glyph = "MT", Group = "Extra Tools" },
            new NavigationItemViewModel { Key = "FeatureAuditMatrix", Label = "Feature Audit Matrix", Glyph = "FM", Group = "Extra Tools" },

            new NavigationItemViewModel { Key = "KnowledgeBase", Label = "Knowledge Base", Glyph = "KB", Group = "Settings" },
            new NavigationItemViewModel { Key = "Settings", Label = "App Settings", Glyph = "SET", Group = "Settings" },
            new NavigationItemViewModel { Key = "About", Label = "About App", Glyph = "AB", Group = "About" }
        };

        public MainWindowViewModel()
        {
            _allNavigationItems = NavigationItems.ToList();
            _runtimeNavigationItems = _allNavigationItems;
        }

        public string PageTitle { get => _pageTitle; set => SetProperty(ref _pageTitle, value); }
        public string PageSubtitle { get => _pageSubtitle; set => SetProperty(ref _pageSubtitle, value); }
        public string BackendStatus { get => _backendStatus; set => SetProperty(ref _backendStatus, value); }
        public string BackendBadge { get => _backendBadge; set => SetProperty(ref _backendBadge, value); }
        public string ActiveGpu { get => _activeGpu; set => SetProperty(ref _activeGpu, value); }
        public string CurrentMode { get => _currentMode; set => SetProperty(ref _currentMode, value); }
        public string RuntimeMode { get => _runtimeMode; set => SetProperty(ref _runtimeMode, value); }
        public bool RestoreAvailable { get => _restoreAvailable; set => SetProperty(ref _restoreAvailable, value); }
        public bool AnimationsEnabled { get => _animationsEnabled; set => SetProperty(ref _animationsEnabled, value); }
        public bool ReduceMotion { get => _reduceMotion; set => SetProperty(ref _reduceMotion, value); }
        public string AccentColor { get => _accentColor; set => SetProperty(ref _accentColor, value); }
        public string ToastMessage { get => _toastMessage; set => SetProperty(ref _toastMessage, value); }
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    ApplySearchFilter();
            }
        }

        public void ApplyFeatureVisibility()
        {
            var snapshot = FeatureVisibilityService.Current;
            var source = _allNavigationItems.Count > 0 ? _allNavigationItems : NavigationItems.ToList();
            var modeLabel = string.IsNullOrWhiteSpace(CurrentMode) ? "Beginner" : CurrentMode.Trim();

            foreach (var item in source)
                item.Status = FeatureVisibilityService.GetStatus(item.Key);

            var visible = source
                .Where(item => FeatureVisibilityService.IsVisible(item.Key))
                .ToList();

            if (snapshot.Mode != HyperBoostAppMode.Stable || !snapshot.BlockNonRealStableUi)
                visible = source.ToList();

            visible = ApplyExperienceModeFilter(visible, modeLabel);

            RuntimeMode = snapshot.Mode == HyperBoostAppMode.Stable
                ? $"Stable / {modeLabel} ({visible.Count} visible)"
                : $"Dev / {modeLabel} ({visible.Count} visible)";

            _runtimeNavigationItems = visible;
            ApplySearchFilter();
            ToastMessage = snapshot.Mode == HyperBoostAppMode.Stable
                ? $"{modeLabel} sidebar ready: {visible.Count} stable-real page(s), {snapshot.HiddenFromStable} non-real beta/dev feature(s) hidden"
                : "DEV_MODE shows experimental features for internal audit";
        }

        private void ApplySearchFilter()
        {
            var query = SearchText?.Trim();
            var filtered = string.IsNullOrWhiteSpace(query)
                ? _runtimeNavigationItems
                : _runtimeNavigationItems.Where(item =>
                    Contains(item.Key, query) ||
                    Contains(item.Label, query) ||
                    Contains(item.Group, query) ||
                    Contains(item.Status, query)).ToList();

            NavigationItems.Clear();
            foreach (var item in filtered)
                NavigationItems.Add(item);

            if (NavigationItems.All(item => !item.IsActive) && NavigationItems.Count > 0)
                NavigationItems[0].IsActive = true;
        }

        private static List<NavigationItemViewModel> ApplyExperienceModeFilter(
            IReadOnlyList<NavigationItemViewModel> visible,
            string modeLabel)
        {
            if (modeLabel.Contains("Expert", System.StringComparison.OrdinalIgnoreCase))
                return visible.ToList();

            var allowedKeys = modeLabel.Contains("Advanced", System.StringComparison.OrdinalIgnoreCase)
                ? AdvancedNavigationKeys
                : BeginnerNavigationKeys;

            var byKey = visible.ToDictionary(item => item.Key, System.StringComparer.OrdinalIgnoreCase);
            var ordered = new List<NavigationItemViewModel>();
            foreach (var key in allowedKeys)
            {
                if (byKey.TryGetValue(key, out var item))
                    ordered.Add(item);
            }

            return ordered;
        }

        private static bool Contains(string value, string query)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

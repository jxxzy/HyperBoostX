using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using HyperBoostX.Services;
using HyperBoostX.ViewModels;
using HyperBoostX.Views;

namespace HyperBoostX
{
    public partial class MainWindow : Window
    {
        private sealed class FeatureAuditResult
        {
            public string Name { get; set; } = "";
            public bool Success { get; set; }
            public long DurationMs { get; set; }
            public string Details { get; set; } = "";
        }

        private readonly IHyperBoostBackendClient _backendClient;
        private readonly MainWindowViewModel _viewModel = new();
        private readonly NavigationService _navigationService = new();
        private readonly BackendStatusService _backendStatusService;
        private readonly LocalConfigService _localConfigService = new();
        private readonly DispatcherTimer _backendTimer = new();
        private readonly List<FeatureAuditResult> _lastFeatureAuditResults = new();
        private static readonly IReadOnlyDictionary<string, string> LegacyNavigationAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SmartRecommendation"] = "AIPerformanceAdvisor",
            ["Gaming"] = "AutoGamingMode",
            ["Performance"] = "PerformanceBoost",
            ["Startup"] = "StartupManager",
            ["Booster"] = "GamingBooster",
            ["Streaming"] = "StreamingCenter",
            ["Creator"] = "CreatorMode",
            ["Network"] = "NetworkBooster",
            ["DnsLatency"] = "DnsLatencyTools",
            ["Privacy"] = "PrivacyCenter",
            ["Tweaks"] = "TweaksCenter",
            ["Repair"] = "RepairTools",
            ["Drivers"] = "DriverUpdateCenter",
            ["Advanced"] = "AdvancedTweaks",
            ["Services"] = "WindowsServices",
            ["Power"] = "PowerOptimization",
            ["Visual"] = "VisualEffects",
            ["Restore"] = "RestoreBackup",
            ["RestorePoint"] = "RestorePointManager",
            ["Automation"] = "ScheduledAutomation",
            ["Utilities"] = "UtilitiesTools",
            ["Testing"] = "MasterTestEngine"
        };
        private static bool _cyberResourcesEnsured;
        private bool _backendCheckInProgress;
        private bool _isClosing;

        public MainWindow()
            : this(new HyperBoostBackendClient())
        {
        }

        public MainWindow(IHyperBoostBackendClient backendClient)
        {
            _backendClient = backendClient ?? throw new ArgumentNullException(nameof(backendClient));
            _backendStatusService = new BackendStatusService(_backendClient);

            EnsureCyberResources();
            InitializeComponent();
            DataContext = _viewModel;

            RegisterRoutes();
            ApplySavedUiSettings();
            _viewModel.ApplyFeatureVisibility();
            NavigateToPage(ResolveStartupPageKey());

            _backendTimer.Interval = TimeSpan.FromSeconds(10);
            _backendTimer.Tick += async (_, _) => await UpdateBackendStatusAsync();
        }

        private static void EnsureCyberResources()
        {
            if (_cyberResourcesEnsured || Application.Current == null)
                return;

            var resourcePaths = new[]
            {
                "Themes/CyberTheme.xaml",
                "Themes/AccentColors.xaml",
                "Themes/Animations.xaml",
                "Styles/Buttons.xaml",
                "Styles/Controls.xaml",
                "Styles/Cards.xaml",
                "Styles/Sidebar.xaml",
                "Styles/Badges.xaml",
                "Styles/ProgressRings.xaml",
                "Styles/Toasts.xaml",
                "Styles/Modals.xaml"
            };

            foreach (var path in resourcePaths)
            {
                var source = new Uri($"pack://application:,,,/HyperBoostX;component/{path}", UriKind.Absolute);
                var alreadyMerged = Application.Current.Resources.MergedDictionaries.Any(dictionary =>
                    dictionary.Source != null &&
                    string.Equals(dictionary.Source.ToString(), source.ToString(), StringComparison.OrdinalIgnoreCase));

                if (!alreadyMerged)
                    Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = source });
            }

            _cyberResourcesEnsured = true;
        }

        public void NavigateToPage(string key)
        {
            var normalizedKey = NormalizeNavigationKey(key);
            if (!FeatureVisibilityService.IsVisible(normalizedKey))
            {
                _viewModel.ToastMessage = $"{normalizedKey} is hidden in Stable mode because it is not fully real yet.";
                normalizedKey = "Dashboard";
            }

            var view = _navigationService.Navigate(normalizedKey);
            PageHost.Content = view;

            foreach (var item in _viewModel.NavigationItems)
                item.IsActive = string.Equals(item.Key, normalizedKey, StringComparison.OrdinalIgnoreCase);

            if (view.DataContext is CyberPageViewModel page)
            {
                _viewModel.PageTitle = page.Title;
                _viewModel.PageSubtitle = page.Subtitle;
            }
            else if (view is DashboardView)
            {
                _viewModel.PageTitle = "Dashboard";
                _viewModel.PageSubtitle = "Safe AI Windows Gaming Optimizer";
            }

            _viewModel.ToastMessage = $"Loaded {_viewModel.PageTitle}";
            AnimatePageTransition();
        }

        private void RegisterRoutes()
        {
            _navigationService.Register("Dashboard", () => new DashboardView());
            _navigationService.Register("AIPerformanceAdvisor", () => new AIPerformanceAdvisorView());
            _navigationService.Register("AutoGamingMode", () => new AutoGamingModeView());
            _navigationService.Register("GameLibrary", () => new GameLibraryView());
            _navigationService.Register("GameProfiles", () => new GameProfilesView());
            _navigationService.Register("GpuCenter", () => new GpuCenterView());
            _navigationService.Register("HyperBalance", () => new HyperBalanceView());
            _navigationService.Register("OneClickBoost", () => new OneClickBoostView());
            _navigationService.Register("ProcessAnalyzer", () => new ProcessAnalyzerView());
            _navigationService.Register("StartupManager", () => new StartupManagerView());
            _navigationService.Register("Cleanup", () => new CleanupView());
            _navigationService.Register("NetworkTools", () => new NetworkToolsView());
            _navigationService.Register("BenchmarkLab", () => new BenchmarkLabView());
            _navigationService.Register("PerformanceHistory", () => new PerformanceHistoryView());
            _navigationService.Register("PerformanceReport", () => new PerformanceReportView());
            _navigationService.Register("StreamingCenter", () => new StreamingCenterView());
            _navigationService.Register("CreatorMode", () => new CreatorModeView());
            _navigationService.Register("GamingEssentials", () => new GamingEssentialsView());
            _navigationService.Register("RestoreBackup", () => new RestoreBackupView());
            _navigationService.Register("ProtectedApps", () => new ProtectedAppsView());
            _navigationService.Register("KnowledgeBase", () => new KnowledgeBaseView());
            _navigationService.Register("Settings", () => new SettingsView());
            _navigationService.Register("FeatureAudit", () => new FeatureAuditView());
            _navigationService.Register("About", () => new AboutView());

            RegisterLegacyRoute("SmartScan", "Smart Scan", "Unified safe scan flow for system pressure, GPU guidance, overlays, startup, cleanup, report, and restore readiness.", "Run Smart Scan", "Scan", "Unified", "Uses local backend routes only", "Flow", "Preview", "Scan output feeds safe plan and reports", "Run scan first, then review recommendations before applying anything.", "Smart Scan may record local history, but it does not force system tweaks.");
            RegisterLegacyRoute("HyperBoostScore", "HyperBoost Score", "Score engine view for health, gaming readiness, network, storage, and safety signals.", "Calculate Score", "Score", "Local", "No guaranteed FPS claim", "Report", "Available", "Scores are explainable heuristics", "Use score as guidance, not a promise of performance gain.", "Before/after report is the source for actual local comparison.");
            RegisterLegacyRoute("CpuRamOptimizer", "CPU/RAM Optimizer", "CPU and memory pressure analysis with protected-process guardrails and review-only apply path.", "Analyze CPU/RAM", "Mode", "Guarded", "No force-kill by default", "Safety", "Active", "System/security/driver processes are protected", "Beginner mode shows safe guidance only.", "Expert mode still cannot bypass Safety Guard.");
            RegisterLegacyRoute("DriverRecommendation", "Driver Recommendation", "Vendor-aware driver guidance using official-link routing without automatic download or install.", "Review Drivers", "Driver", "Guidance", "No fabricated latest-version claim", "Install", "Manual", "Driver installs stay outside HyperBoostX automation", "Use vendor/OEM sources for downloads.", "HyperBoostX never disables driver services.");
            RegisterLegacyRoute("OverlayConflictDetector", "Overlay Conflict Detector", "Overlay conflict detection and guidance for launchers, capture tools, vendor overlays, and game bars.", "Scan Overlays", "Overlay", "Live", "Detection and guidance only", "Action", "Manual", "Overlay shutdown is not forced", "Review overlay recommendations before a gaming session.", "Protected game and anti-cheat processes are never modified.");
            RegisterLegacyRoute("RgbSoftwareDetector", "RGB Software Detector", "RGB software detection kept as partial guidance; no global RGB control is claimed in v2.10.0 Stable Unsigned.", "Detect RGB Tools", "RGB", "Partial", "Detects known vendor software where available", "Control", "Roadmap", "No fan, driver, or lighting service control", "Use vendor RGB apps for actual lighting changes.", "HyperBoostX only reports conflicts and safety notes in this build.");
            RegisterLegacyRoute("Reports", "Reports", "Before/after reports, export, action log, and restore-session visibility for local-first QA.", "Open Reports", "Reports", "Local", "Stored under HyperBoost X data root", "Restore", "Visible", "Reports are redacted before export", "Export reports after scan/apply/restore flows.", "No token, API key, or sensitive username should appear in reports.");
            RegisterLegacyRoute("ReleaseReadiness", "Release Readiness", "Stable gate status for version sync, installed runtime, admin rollback, hardware lab, code signing, and package evidence.", "Check Readiness", "Channel", "Stable unsigned", "Stable is claimed only after gates pass", "Gate", "Manual", "Installed runtime and hardware lab evidence must remain attached", "Use this page before tagging a stable release.", "v2.10.0 requires installed runtime gate evidence before release.");
            RegisterLegacyRoute("PluginMarketplace", "Plugin Marketplace", "Roadmap-only plugin marketplace boundary with signed-package and local trust requirements.", "Open Registry", "Status", "Roadmap", "Registry/status only", "Install", "Blocked", "Unsigned plugin install is guarded", "Treat plugin marketplace as a future feature.", "No marketplace claim should be made for this stable unsigned release.");
            RegisterLegacyRoute("CloudSyncLicense", "Cloud Sync & License Boundary", "Roadmap boundary for cloud sync and license flows; v2.10.0 Stable Unsigned remains local-first.", "Review Boundary", "Cloud", "Roadmap", "No account or sync backend is active", "License", "Boundary", "License checks cannot block local safety features", "Keep privacy docs aligned with local-first behavior.", "Do not claim cloud sync or license enforcement as complete.");

            RegisterLegacyRoute("AICenter", "AI Center", "Local advisor, NVIDIA Copilot status, approval flow, safety rules, and action history in one stable diagnostics hub.", "Open AI Status", "AI", "Guarded", "Plans only, no arbitrary shell", "Approval", "Required", "AI actions stay preview/approval gated", "Use Smart Recommendation for local diagnosis first.", "NVIDIA Copilot requires owner credentials before live provider calls.");
            RegisterLegacyRoute("NvidiaCopilot", "NVIDIA Copilot", "NVIDIA Copilot surface with secure credential status, model registry, test connection, fallback, and Safety Guard.", "Test NVIDIA Status", "Provider", "NVIDIA", "API key is stored only through secure settings", "Models", "10", "Model registry and fallback remain visible", "No API key is shown after save.", "AI output cannot execute shell or bypass Safety Guard.");
            RegisterLegacyRoute("PerformanceBoost", "Performance Boost", "Performance boost surface with scan, preview, approval, restore, report, and undo gates.", "Scan Performance", "Safety Level", "Preview", "No direct risky changes", "Restore", "Required", "Mutating actions require restore metadata", "Use the preview flow before applying anything.", "Extreme actions remain Expert-only and blocked by Safety Guard.");
            RegisterLegacyRoute("BackgroundApps", "Background Apps", "Read-only process pressure view with protected-process guidance.", "Analyze Background Apps", "Mode", "Read-only", "No force-kill from beginner mode", "Protection", "Active", "System, security, anti-cheat, driver, and vendor utilities are protected", "Review high-memory apps manually before gaming.", "Browsers are treated as browser/work apps, not games.");
            RegisterLegacyRoute("Storage", "Storage", "System drive and cleanup preview surface without destructive delete defaults.", "Refresh Storage", "Storage", "Live", "Reads backend system drive data", "Cleanup", "Preview", "Documents, Downloads, Desktop, media, game saves, and project folders stay excluded", "Run cleanup preview before deleting anything.", "Duplicate-file cleanup remains review-only.");
            RegisterLegacyRoute("GamingBooster", "Gaming Booster", "Instant gaming boost flow routed to safe boost plan endpoints.", "Create Gaming Plan", "Plan", "Safe", "Scan and preview first", "Undo", "Available", "Boost undo route remains visible", "Do not apply gaming boost unless a real game is selected/detected.", "Chrome and browsers are not treated as games by default.");
            RegisterLegacyRoute("AdvancedMicMixer", "Advanced Mic Mixer", "Streaming mic guidance with Windows sound settings handoff and no invasive audio driver edits.", "Check Mic Tools", "Audio", "Guide", "Uses safe streaming diagnostics", "Driver", "Protected", "Audio driver services are never disabled", "Use Windows Sound Settings for device changes.", "OBS/Discord/TikTok Live guidance stays in Streaming Center.");
            RegisterLegacyRoute("WebcamStudio", "Webcam Studio", "Webcam guidance surface with privacy-aware camera diagnostics.", "Check Webcam", "Camera", "Guide", "No background recording or hidden capture", "Privacy", "Visible", "Windows camera/privacy settings stay user-controlled", "Open camera settings from Streaming Center when needed.", "Hardware access can be blocked by OS privacy settings.");
            RegisterLegacyRoute("CameraTracking", "Camera Tracking", "Real-time camera tracking entry as opt-in local camera tool.", "Open Tracking Status", "Tracking", "Opt-in", "No silent camera activation", "Privacy", "Guarded", "Camera access requires explicit user action", "Camera Tracking remains disabled until user opens the dedicated camera window.", "If camera is blocked, HyperBoostX shows a privacy guidance state.");
            RegisterLegacyRoute("NetworkBooster", "Network Booster", "Network optimization as diagnostics, DNS checks, and approval-gated cache actions.", "Run Network Diagnostics", "Network", "Diagnostics", "No fake ping improvement claim", "Risk", "Preview", "Network reset/flush actions require confirmation", "Use DNS/latency test before changing anything.", "Destructive network reset is never automatic.");
            RegisterLegacyRoute("DnsLatencyTools", "DNS & Latency Tools", "DNS test and latency diagnostics with report export.", "Test DNS", "DNS", "Live", "Uses backend diagnostic route", "Latency", "Measured", "No hardcoded ping value", "Run diagnostics to get real local results.", "DNS changes require approval.");
            RegisterLegacyRoute("NetworkOptimization", "Network Optimization", "Safe network profile surface without dangerous registry tweaks.", "Preview Network Plan", "Profile", "Safe", "Diagnostics first", "Approval", "Required", "TCP/network actions require explicit approval", "Flush DNS is separate from destructive network reset.", "No ping guarantee is made.");
            RegisterLegacyRoute("PrivacyCenter", "Privacy Center", "Read-only privacy surface with clear warnings for cookies, sessions, and personal folders.", "Review Privacy", "Privacy", "Read-only", "No personal data cleanup by default", "Sessions", "Protected", "Cookies and saved sessions require explicit warning", "Privacy cleanup is not treated as a default performance action.", "Personal folders are excluded from cleanup flows.");
            RegisterLegacyRoute("SecurityHealth", "Security & Health", "Security status surface as read-only guidance.", "Review Security", "Security", "Read-only", "Defender, firewall, anti-cheat, and update services are protected", "Safety", "Blocked", "Dangerous security tweaks are blocked", "HyperBoostX will not disable Defender, Firewall, or anti-cheat.", "Admin-required security status may be unavailable without elevation.");
            RegisterLegacyRoute("SystemRealityGuard", "System Reality Guard", "Evidence-based diagnostics for LCD vendor apps, Defender scans, CPU turbo, MSI Center, and security reality without panic labels.", "Run Reality Scan", "Mode", "Diagnostic", "Bridge Mode detects/monitors only", "Hybrid", "Preview", "Unsafe vendor/system changes are blocked", "Use this page to explain real background CPU causes before applying anything.", "HyperBoostX does not patch vendor binaries or disable Defender.");
            RegisterLegacyRoute("LcdPerformanceGuard", "LCD Performance Guard", "KANALI, TRCC, HiMOS, helper process, wallpaper weight, hybrid preview, and native compatibility diagnostics.", "Analyze LCD Apps", "Bridge", "Detect-only", "Hybrid reduces duplicate work only where safe", "Native", "Gated", "Required LCD apps are protected", "Review TRCC helpers such as ffmpeg, HWiNFO, USB_LCD, and USBLCDNEW.", "Bridge Mode does not guarantee CPU reduction.");
            RegisterLegacyRoute("DefenderScanGuard", "Defender Scan Guard", "Defender scan status and CPU impact advisor with hard blocks for kill MsMpEng, permanent disable, and broad exclusions.", "Check Defender Scan", "Defender", "Protected", "No permanent disable", "Exclusions", "Specific only", "Full-drive and broad user-folder exclusions are blocked", "High CPU during full scan can be normal and temporary.", "Do not disable Defender permanently.");
            RegisterLegacyRoute("SecurityRealityAudit", "Security Reality Audit", "WSL, remote access, startup, scheduled task, PowerShell, and vendor service classifier with evidence-based verdicts.", "Run Security Audit", "Verdict", "Evidence", "No panic labels for Microsoft/Intel/vendor signed components", "Review", "Manual", "Suspicious AppData/Temp scripts are flagged for review", "Use evidence before calling something a threat.", "HyperBoostX does not auto-delete files from this audit.");
            RegisterLegacyRoute("AppsManager", "Apps Manager", "Installed/running app overview with impact guidance.", "List Apps", "Apps", "Read-only", "Inventory and impact guidance only", "Uninstall", "Confirm", "Uninstall preview requires user confirmation", "No app is removed silently.", "Impact guidance avoids protected system apps.");
            RegisterLegacyRoute("AppUninstaller", "App Uninstaller", "App uninstall entry as confirmation-first workflow.", "Preview Uninstall", "Mode", "Preview", "No silent uninstall", "Safety", "Confirm", "Owner must confirm uninstall outside hidden automation", "Use Windows Apps Settings for final uninstall when needed.", "System apps are protected.");
            RegisterLegacyRoute("TweaksCenter", "Tweaks Center", "Tweaks as allowlisted preview-only surface.", "Load Tweaks", "Tweaks", "Allowlist", "Unsafe tweaks blocked", "Apply", "Approval", "Apply requires preview, approval, and restore", "No arbitrary shell command is exposed.", "Expert tweaks stay disabled by default.");
            RegisterLegacyRoute("AdvancedTweaks", "Advanced Tweaks", "Advanced tweak surface with Expert-only warnings.", "Preview Advanced Tweaks", "Mode", "Expert", "No default risky action", "Guard", "Active", "Safety Guard blocks destructive changes", "CPU affinity, services, and update-control actions require explicit review.", "Beginner mode keeps advanced actions informational.");
            RegisterLegacyRoute("WindowsFeatures", "Windows Features", "Windows optional features surface as read-only/preview guidance.", "Review Features", "Features", "Read-only", "No silent enable/disable", "Admin", "Needed", "Changes require Windows UI/admin path", "Feature changes can require restart.", "HyperBoostX does not force component changes silently.");
            RegisterLegacyRoute("WindowsServices", "Windows Services", "Windows services surface with protected service list.", "Review Services", "Services", "Protected", "Driver/security/anti-cheat services protected", "Apply", "Blocked", "Service stop/disable is not automatic", "Use preview before any service action.", "Driver, audio, network, anti-cheat, Defender, and fan/RGB services are protected.");
            RegisterLegacyRoute("UpdateControl", "Update Control", "Update control as status/temporary guidance, never permanent disable.", "Review Update Status", "Updates", "Read-only", "Permanent disable blocked", "Pause", "Temporary", "Only temporary, reversible guidance is allowed", "Do not permanently disable Windows Update.", "Update service hacks are blocked.");
            RegisterLegacyRoute("RepairTools", "Repair Tools", "Repair tools with admin/time warnings and no silent command execution.", "Review Repair Tools", "Repair", "Preview", "SFC/DISM require admin and time", "Report", "Available", "Repair results are reportable", "Run repair actions only after reading impact.", "No arbitrary repair command is generated by AI.");
            RegisterLegacyRoute("DriverUpdateCenter", "Driver & Update Center", "Driver status without fabricated latest version or silent installs.", "Review Drivers", "Driver", "Manual", "Official vendor/OEM check only", "Install", "Manual", "No auto-download or auto-install", "Use vendor/OEM source for driver downloads.", "HyperBoostX never disables driver services.");
            RegisterLegacyRoute("PowerOptimization", "Power Optimization", "Power plan surface as preview/approval workflow.", "Preview Power Plan", "Power", "Review", "No forced power-plan switch", "Restore", "Required", "Power changes must be reversible", "Ultimate Performance is Expert-only and not automatic.", "Laptop battery impact must be reviewed.");
            RegisterLegacyRoute("VisualEffects", "Visual Effects", "Visual effects optimization as reversible preview guidance.", "Preview Visual Effects", "Effects", "Preview", "No silent theme/system visual changes", "Restore", "Required", "Visual changes must be reversible", "Beginner mode shows simple recommendations.", "Advanced details remain opt-in.");
            RegisterLegacyRoute("CpuTurboDiagnostic", "CPU Turbo Diagnostic", "CPU base/current clock, stress sample, Windows power plan, MSI mode hint, and BIOS checklist diagnostics without BIOS or voltage automation.", "Check Turbo", "CPU", "Diagnostic", "Low-load tests are invalid", "BIOS", "Manual", "No BIOS/voltage/overclock changes are automated", "Use a real stress sample before judging turbo behavior.", "If CPU is stuck near base clock, review power policy, MSI mode, thermals, and limits.");
            RegisterLegacyRoute("MsiSafeOptimizer", "MSI Safe Optimizer", "MSI Center detection and recommendations that avoid breaking fan, RGB, hardware profile, audio, or network services.", "Review MSI Center", "MSI", "Safe", "No service disable by default", "Mode", "Manual", "MSI Center may be required for fan/RGB/hardware control", "Avoid Silent/Eco while testing CPU turbo.", "HyperBoostX never blindly disables MSI or driver services.");
            RegisterLegacyRoute("RestorePointManager", "Restore Point Manager", "Restore point entry with session metadata and admin-aware status.", "Review Restore Points", "Restore", "Metadata", "Lists HyperBoostX restore sessions", "Point", "Admin", "Windows restore point creation may require elevation", "Restore metadata is required before mutating actions.", "If restore point creation fails, risky actions stay blocked.");
            RegisterLegacyRoute("ScheduledAutomation", "Scheduled Automation", "Scheduled automation as scan/report-only by default.", "Review Automation", "Automation", "Safe-only", "No unattended risky action", "Rules", "Dry-run", "Rules must pass Safety Guard", "Default automation runs scans/reports only.", "Mutating automation requires explicit owner setup.");
            RegisterLegacyRoute("TaskRuleSystem", "Task & Rule System", "Task/rule system with dry-run and Safety Guard gating.", "Review Rules", "Rules", "Dry-run", "No dangerous unattended action", "AI", "Guarded", "AI-generated rules cannot bypass allowlist", "Rules should be exported and reviewed before enabling.", "Expert-only rule details remain visible in result output.");
            RegisterLegacyRoute("UtilitiesTools", "Utilities Tools", "Utility toolbox as links/status/reporting without destructive defaults.", "Open Utilities", "Tools", "Safe", "Read-only and preview utilities first", "Risk", "Labeled", "Risky utilities are blocked or approval-gated", "Use Feature Audit and reports for diagnostics.", "No WinUtil-style raw script execution is exposed.");
            RegisterLegacyRoute("MasterTestEngine", "Master Test Engine", "Master test engine as QA/status launcher for backend, routes, UI, safety, and version checks.", "Run Test Status", "QA", "Smoke", "Reports current verification status", "Release", "Blocked", "Release remains blocked until installed validation passes", "Run automated tests before packaging.", "Installed smoke still requires installer flow evidence.");
            RegisterLegacyRoute("FeatureAuditMatrix", "Feature Audit Matrix", "Feature audit matrix with release-gate status.", "Open Audit Matrix", "Matrix", "Live", "Feature audit status from backend", "Parity", "Tracked", "Missing/partial/roadmap states stay visible", "Do not mark Complete without evidence.", "Public release remains blocked until all gates pass.");

            RegisterLegacyAliases();
        }

        private void RegisterLegacyAliases()
        {
            foreach (var alias in LegacyNavigationAliases)
            {
                var targetKey = alias.Value;
                _navigationService.Register(alias.Key, () => _navigationService.Navigate(targetKey));
            }
        }

        private static string NormalizeNavigationKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return "Dashboard";

            return LegacyNavigationAliases.TryGetValue(key.Trim(), out var targetKey)
                ? targetKey
                : key.Trim();
        }

        private void RegisterLegacyRoute(
            string key,
            string title,
            string subtitle,
            string primaryAction,
            string firstMetricTitle,
            string firstMetricValue,
            string firstMetricDetail,
            string secondMetricTitle,
            string secondMetricValue,
            string secondMetricDetail,
            params string[] recommendations)
        {
            _navigationService.Register(key, () => new LegacyFeatureView(new LegacyFeaturePageViewModel(
                key,
                title,
                subtitle,
                primaryAction,
                firstMetricTitle,
                firstMetricValue,
                firstMetricDetail,
                secondMetricTitle,
                secondMetricValue,
                secondMetricDetail,
                recommendations)));
        }

        private string ResolveStartupPageKey()
        {
            var requestedPage = Environment.GetEnvironmentVariable("HYPERBOOSTX_START_PAGE");
            if (string.IsNullOrWhiteSpace(requestedPage))
                return "Dashboard";

            var normalizedPage = NormalizeNavigationKey(requestedPage);
            return _viewModel.NavigationItems.Any(item => string.Equals(item.Key, normalizedPage, StringComparison.OrdinalIgnoreCase))
                ? normalizedPage
                : "Dashboard";
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _backendTimer.Start();
            await UpdateBackendStatusAsync();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _isClosing = true;
            _backendTimer.Stop();
            if (_backendClient is IDisposable disposable)
                disposable.Dispose();
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { CommandParameter: string key })
                NavigateToPage(key);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void QuickSmartScan_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage("Dashboard");
            if (PageHost.Content is DashboardView dashboard)
                await dashboard.RunSmartScanAsync();
        }

        private void QuickSafeBoost_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage("OneClickBoost");
        }

        private void QuickRestore_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage("RestoreBackup");
        }

        private async Task UpdateBackendStatusAsync()
        {
            if (_backendCheckInProgress || _isClosing)
                return;

            _backendCheckInProgress = true;
            try
            {
                var online = await _backendStatusService.IsOnlineAsync();
                _viewModel.BackendStatus = online ? "127.0.0.1 backend online" : "Backend offline - launcher may not be running";
                _viewModel.BackendBadge = online ? "ONLINE" : "OFFLINE";
                BackendPulseDot.Fill = online
                    ? (Brush)FindResource("Brush.Status.Success")
                    : (Brush)FindResource("Brush.Status.Warning");

                if (PageHost.Content is DashboardView dashboard && dashboard.DataContext is DashboardViewModel dashboardVm)
                    dashboardVm.BackendStatus = online ? "Online" : "Offline";
            }
            catch
            {
                _viewModel.BackendStatus = "Backend check failed safely";
                _viewModel.BackendBadge = "OFFLINE";
                BackendPulseDot.Fill = (Brush)FindResource("Brush.Status.Danger");
            }
            finally
            {
                _backendCheckInProgress = false;
            }
        }

        private void ApplySavedUiSettings()
        {
            try
            {
                var settings = _localConfigService.LoadUiSettings();
                _viewModel.AnimationsEnabled = settings.EnableAnimations;
                _viewModel.ReduceMotion = settings.ReduceMotion;
                _viewModel.AccentColor = settings.AccentColor;
                _viewModel.CurrentMode = settings.Mode;
                _viewModel.RuntimeMode = $"{FeatureVisibilityService.ModeLabel} / {settings.Mode}";
            }
            catch
            {
                _viewModel.ToastMessage = "Using default cyber UI settings";
            }
        }

        public void ReloadUiSettingsFromSettingsPage()
        {
            ApplySavedUiSettings();
            _viewModel.ApplyFeatureVisibility();
            _viewModel.ToastMessage = "Settings applied to the current shell.";
        }

        private void AnimatePageTransition()
        {
            if (!_viewModel.AnimationsEnabled)
                return;

            var translate = PageHost.RenderTransform as TranslateTransform;
            if (translate == null)
            {
                translate = new TranslateTransform();
                PageHost.RenderTransform = translate;
            }

            PageHost.Opacity = 0;
            translate.X = _viewModel.ReduceMotion ? 0 : 18;

            PageHost.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(_viewModel.ReduceMotion ? 120 : 260))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

            if (!_viewModel.ReduceMotion)
            {
                translate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(260))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });
            }
        }

        private async Task RunTestingSuiteAsync(string suiteName)
        {
            _lastFeatureAuditResults.Clear();

            foreach (var check in BuildFeatureAuditChecks(suiteName))
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    await check.ExecuteAsync();
                    sw.Stop();
                    _lastFeatureAuditResults.Add(new FeatureAuditResult
                    {
                        Name = check.Name,
                        Success = true,
                        DurationMs = sw.ElapsedMilliseconds,
                        Details = "Passed"
                    });
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    _lastFeatureAuditResults.Add(new FeatureAuditResult
                    {
                        Name = check.Name,
                        Success = false,
                        DurationMs = sw.ElapsedMilliseconds,
                        Details = ex.Message
                    });
                }
            }
        }

        private IReadOnlyList<(string Name, Func<Task> ExecuteAsync)> BuildFeatureAuditChecks(string suiteName)
        {
            return new List<(string Name, Func<Task> ExecuteAsync)>
            {
                ($"{suiteName} - cyber resources loaded", () =>
                {
                    _ = FindResource("CyberButtonStyle");
                    _ = FindResource("CyberCardStyle");
                    _ = FindResource("CyberSidebarButtonStyle");
                    return Task.CompletedTask;
                }),
                ($"{suiteName} - shell navigation routes", () =>
                {
                    var dashboard = _navigationService.Navigate("Dashboard");
                    var audit = _navigationService.Navigate("FeatureAudit");
                    if (dashboard == null || audit == null)
                        throw new InvalidOperationException("Required routes are not registered.");
                    return Task.CompletedTask;
                }),
                ($"{suiteName} - sidebar page coverage", () =>
                {
                    var snapshot = FeatureVisibilityService.Current;
                    var minimumCount = snapshot.Mode == HyperBoostAppMode.Stable ? 24 : 50;
                    if (_viewModel.NavigationItems.Count < minimumCount)
                        throw new InvalidOperationException("Cyber sidebar is missing required runtime-visible pages.");
                    return Task.CompletedTask;
                }),
                ($"{suiteName} - backend offline tolerant", async () =>
                {
                    await _backendClient.HealthCheckAsync();
                }),
                ($"{suiteName} - settings persistence readable", () =>
                {
                    _ = _localConfigService.LoadUiSettings();
                    return Task.CompletedTask;
                })
            };
        }
    }
}

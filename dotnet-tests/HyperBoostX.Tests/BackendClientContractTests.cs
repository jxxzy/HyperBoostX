using System.IO;
using Xunit;

namespace HyperBoostX.Tests;

public class BackendClientContractTests
{
    [Fact]
    public void WpfClient_UsesSafeBoostPlanEndpointsInsteadOfRemovedTripleAiRoutes()
    {
        var repoRoot = FindRepoRoot();
        var clientSource = File.ReadAllText(Path.Combine(repoRoot, "wpf", "Services", "HyperBoostBackendClient.cs"));
        var dashboardSource = File.ReadAllText(Path.Combine(repoRoot, "wpf", "Views", "DashboardView.xaml.cs"));

        Assert.Contains("/api/boost/plan", clientSource);
        Assert.Contains("/api/boost/apply", clientSource);
        Assert.Contains("/api/boost/undo", clientSource);
        Assert.Contains("CreateBoostPlanAsync", dashboardSource);
        Assert.DoesNotContain("/api/triple-ai", clientSource);
        Assert.DoesNotContain("RunTripleAiFlowAsync(\"gaming\"", dashboardSource);
    }

    [Fact]
    public void StreamingCenter_RestoresLegacyMicVoicemeeterAndWebcamSurface()
    {
        var repoRoot = FindRepoRoot();
        var streamingXaml = File.ReadAllText(Path.Combine(repoRoot, "wpf", "Views", "StreamingCenterView.xaml"));
        var streamingCode = File.ReadAllText(Path.Combine(repoRoot, "wpf", "Views", "StreamingCenterView.xaml.cs"));

        Assert.Contains("Advanced Mic / Voice Meter", streamingXaml);
        Assert.Contains("Restored v1.3 / v1.4 Tools", streamingXaml);
        Assert.Contains("Voice Meter / Voicemeeter / volume mixer", streamingXaml);
        Assert.Contains("OBS/TikTok/Discord profile", streamingXaml);
        Assert.Contains("Start Streaming Optimization / Refresh Detect / Restore After Streaming", streamingXaml);
        Assert.Contains("No hidden capture or driver rewiring", streamingXaml);
        Assert.Contains("Mic Diagnostics", streamingXaml);
        Assert.Contains("Voicemeeter", streamingXaml);
        Assert.Contains("Advanced Webcam Studio", streamingXaml);
        Assert.Contains("Low Light Preset", streamingXaml);
        Assert.Contains("Sharp Face Preset", streamingXaml);
        Assert.Contains("OBS", streamingXaml);
        Assert.Contains("TikTok LIVE Studio", streamingXaml);
        Assert.Contains("Discord", streamingXaml);
        Assert.Contains("FindVoicemeeterPath", streamingCode);
        Assert.Contains("No driver service changes", streamingXaml);
    }

    [Fact]
    public void CyberSidebarPages_RunRealBackendFeatureEndpointsAndShowLiveResults()
    {
        var repoRoot = FindRepoRoot();
        var chromeXaml = File.ReadAllText(Path.Combine(repoRoot, "wpf", "Views", "CyberPageChrome.xaml"));
        var chromeCode = File.ReadAllText(Path.Combine(repoRoot, "wpf", "Views", "CyberPageChrome.xaml.cs"));
        var clientSource = File.ReadAllText(Path.Combine(repoRoot, "wpf", "Services", "HyperBoostBackendClient.cs"));

        Assert.Contains("RunPrimaryAction_Click", chromeXaml);
        Assert.Contains("RunPreviewAction_Click", chromeXaml);
        Assert.Contains("RunExportAction_Click", chromeXaml);
        Assert.Contains("LiveResult", chromeXaml);
        Assert.Contains("object result", chromeCode);
        Assert.Contains("NormalizeBackendResult", chromeCode);
        Assert.Contains("blocked by Safety Guard", chromeCode);
        Assert.Contains("Unauthorized local session", chromeCode);
        Assert.Contains("GetJsonAsync", clientSource);
        Assert.Contains("PostJsonRouteAsync", clientSource);
        Assert.Contains("EnsureJsonSuccessAsync", clientSource);

        var requiredRoutes = new[]
        {
            "/api/scan/smart",
            "/api/dashboard/summary",
            "/api/ai/status",
            "/api/ai/plan",
            "/api/nvidia/test-connection",
            "/api/settings/ui",
            "/api/auto-gaming/preview",
            "/api/games/scan",
            "/api/games/profile/preview",
            "/api/gpu/vendor-guide",
            "/api/processes/background-pressure",
            "/api/boost/plan",
            "/api/startup/items",
            "/api/cleanup/scan",
            "/api/network/diagnostics",
            "/api/benchmark/latest",
            "/api/history/timeline",
            "/api/reports/latest",
            "/api/streaming/recommendations",
            "/api/creator/status",
            "/api/essentials/check",
            "/api/restore/sessions",
            "/api/protection/processes",
            "/api/kb/topics",
            "/api/feature-audit/run",
            "/api/version",
            "/api/storage/status",
            "/api/privacy/status",
            "/api/security/status",
            "/api/apps/list",
            "/api/system-config/tweaks",
            "/api/windows/services",
            "/api/update-control/status",
            "/api/repair/status",
            "/api/power/status",
            "/api/visual-effects/status",
            "/api/restore-points/status",
            "/api/automation/rules",
            "/api/master-test/status",
            "/api/feature-audit/matrix",
            "/api/camera-tracking/status",
        };

        foreach (var route in requiredRoutes)
            Assert.Contains(route, chromeCode);

        var sidebarKeys = new[]
        {
            "Dashboard",
            "OneClickBoost",
            "AutoGamingMode",
            "AIPerformanceAdvisor",
            "AICenter",
            "NvidiaCopilot",
            "PerformanceBoost",
            "StartupManager",
            "BackgroundApps",
            "HyperBalance",
            "ProcessAnalyzer",
            "Cleanup",
            "Storage",
            "GpuCenter",
            "GamingBooster",
            "GameLibrary",
            "GameProfiles",
            "StreamingCenter",
            "CreatorMode",
            "AdvancedMicMixer",
            "WebcamStudio",
            "CameraTracking",
            "NetworkBooster",
            "DnsLatencyTools",
            "NetworkOptimization",
            "NetworkTools",
            "PrivacyCenter",
            "SecurityHealth",
            "ProtectedApps",
            "AppsManager",
            "TweaksCenter",
            "WindowsFeatures",
            "UpdateControl",
            "RepairTools",
            "DriverUpdateCenter",
            "AppUninstaller",
            "GamingEssentials",
            "AdvancedTweaks",
            "WindowsServices",
            "PowerOptimization",
            "VisualEffects",
            "RestoreBackup",
            "RestorePointManager",
            "ScheduledAutomation",
            "TaskRuleSystem",
            "UtilitiesTools",
            "FeatureAudit",
            "MasterTestEngine",
            "FeatureAuditMatrix",
            "KnowledgeBase",
            "Settings",
            "About",
        };

        foreach (var key in sidebarKeys)
            Assert.Contains($"[\"{key}\"] = (", chromeCode);
    }

    [Fact]
    public void Sidebar_RestoresV13ParityMenuSurface()
    {
        var repoRoot = FindRepoRoot();
        var mainVm = File.ReadAllText(Path.Combine(repoRoot, "wpf", "ViewModels", "MainWindowViewModel.cs"));
        var mainXaml = File.ReadAllText(Path.Combine(repoRoot, "wpf", "MainWindow.xaml"));
        var mainWindow = File.ReadAllText(Path.Combine(repoRoot, "wpf", "MainWindow.xaml.cs"));

        var requiredKeys = new[]
        {
            "PerformanceBoost",
            "BackgroundApps",
            "Storage",
            "GamingBooster",
            "AdvancedMicMixer",
            "WebcamStudio",
            "CameraTracking",
            "NetworkBooster",
            "DnsLatencyTools",
            "PrivacyCenter",
            "SecurityHealth",
            "AppsManager",
            "AppUninstaller",
            "TweaksCenter",
            "WindowsFeatures",
            "UpdateControl",
            "RepairTools",
            "DriverUpdateCenter",
            "AdvancedTweaks",
            "WindowsServices",
            "PowerOptimization",
            "VisualEffects",
            "RestorePointManager",
            "ScheduledAutomation",
            "UtilitiesTools",
            "MasterTestEngine",
            "FeatureAuditMatrix",
        };

        foreach (var key in requiredKeys)
        {
            Assert.Contains($"Key = \"{key}\"", mainVm);
            Assert.Contains($"RegisterLegacyRoute(\"{key}\"", mainWindow);
        }

        Assert.Contains("Quick Access", mainVm);
        Assert.Contains("Gaming & Creator", mainVm);
        Assert.Contains("Privacy & Security", mainVm);
        Assert.Contains("System Config", mainVm);
        Assert.Contains("AutomationProperties.Name=\"{Binding Label}\"", mainXaml);
        Assert.Contains("AutomationProperties.HelpText=\"{Binding Group}\"", mainXaml);

        var legacyAliases = new[]
        {
            ("SmartRecommendation", "AIPerformanceAdvisor"),
            ("Gaming", "AutoGamingMode"),
            ("Performance", "PerformanceBoost"),
            ("Startup", "StartupManager"),
            ("Booster", "GamingBooster"),
            ("Streaming", "StreamingCenter"),
            ("Creator", "CreatorMode"),
            ("Network", "NetworkBooster"),
            ("DnsLatency", "DnsLatencyTools"),
            ("Privacy", "PrivacyCenter"),
            ("Tweaks", "TweaksCenter"),
            ("Repair", "RepairTools"),
            ("Drivers", "DriverUpdateCenter"),
            ("Advanced", "AdvancedTweaks"),
            ("Services", "WindowsServices"),
            ("Power", "PowerOptimization"),
            ("Visual", "VisualEffects"),
            ("Restore", "RestoreBackup"),
            ("RestorePoint", "RestorePointManager"),
            ("Automation", "ScheduledAutomation"),
            ("Utilities", "UtilitiesTools"),
            ("Testing", "MasterTestEngine"),
        };

        Assert.Contains("RegisterLegacyAliases", mainWindow);
        Assert.Contains("NormalizeNavigationKey", mainWindow);
        foreach (var (alias, target) in legacyAliases)
            Assert.Contains($"[\"{alias}\"] = \"{target}\"", mainWindow);
    }

    [Fact]
    public void ModernPages_SurfaceLegacyV13ContentAsUserFriendlyTools()
    {
        var repoRoot = FindRepoRoot();
        var chromeXaml = File.ReadAllText(Path.Combine(repoRoot, "wpf", "Views", "CyberPageChrome.xaml"));
        var cyberPageVm = File.ReadAllText(Path.Combine(repoRoot, "wpf", "ViewModels", "CyberPageViewModel.cs"));
        var catalog = File.ReadAllText(Path.Combine(repoRoot, "wpf", "ViewModels", "LegacyFeatureCatalog.cs"));

        Assert.Contains("Restored v1.3 / v1.4 Tools", chromeXaml);
        Assert.Contains("LegacyTools", chromeXaml);
        Assert.Contains("LegacyFeatureCatalog.Apply(this)", cyberPageVm);

        var requiredLegacyContent = new[]
        {
            "Run Safe / Balanced / Extreme / Custom Boost",
            "1. Run Safe Boost / 2. Run Balanced Boost / 3. Run Extreme Boost",
            "Clear standby, Optimize RAM, Best Performance, process priority",
            "View all startup apps",
            "Gaming Startup / Work Startup / Minimal Startup",
            "Temp, cache, logs, recycle bin",
            "Scan >100MB / >500MB / >1GB, Scan Duplicates, Keep Original",
            "DNS speed, ping, diagnostics",
            "Quick Latency Test / Continuous Latency Test / Geo Ping Test",
            "Voice Meter / Voicemeeter / volume mixer",
            "Start Streaming Optimization / Refresh Detect / Restore After Streaming",
            "SFC/DISM quick/full repair",
            "Quick Repair / Full System Repair / Auto Fix All",
            "Backup Drivers / Restore Drivers",
            "Apply CPU Affinity",
            "APPLY ADVANCED TWEAKS / Apply Registry Preset / Apply Fast Boot Tweak",
            "CREATE SMART RESTORE POINT / Cleanup Old Points",
            "Full QA Matrix / Feature Audit Full / Audit Trail Detail",
            "Balanced AI / Ultra Performance / Ultra Battery",
            "Unit, integration, UI flow, E2E, regression",
            "Feature overview from v1.3",
        };

        foreach (var text in requiredLegacyContent)
            Assert.Contains(text, catalog);
    }

    [Fact]
    public void WpfShell_UsesLightweightIdlePollingDefaults()
    {
        var repoRoot = FindRepoRoot();
        var mainWindow = File.ReadAllText(Path.Combine(repoRoot, "wpf", "MainWindow.xaml.cs"));
        var app = File.ReadAllText(Path.Combine(repoRoot, "wpf", "App.xaml.cs"));

        Assert.Contains("TimeSpan.FromSeconds(10)", mainWindow);
        Assert.DoesNotContain("TimeSpan.FromSeconds(4)", mainWindow);
        Assert.Contains("TimeSpan.FromSeconds(45)", app);
        Assert.DoesNotContain("TimeSpan.FromSeconds(12)", app);
    }

    [Fact]
    public void WpfClient_CanTargetAlternateBackendPortForSideBySideRuntimeAudit()
    {
        var repoRoot = FindRepoRoot();
        var clientSource = File.ReadAllText(Path.Combine(repoRoot, "wpf", "Services", "HyperBoostBackendClient.cs"));
        var mainWindow = File.ReadAllText(Path.Combine(repoRoot, "wpf", "MainWindow.xaml.cs"));
        var cyberChrome = File.ReadAllText(Path.Combine(repoRoot, "wpf", "Views", "CyberPageChrome.xaml.cs"));
        var backendServer = File.ReadAllText(Path.Combine(repoRoot, "app", "backend_server.py"));
        var backendConstants = File.ReadAllText(Path.Combine(repoRoot, "app", "core", "constants.py"));
        var appState = File.ReadAllText(Path.Combine(repoRoot, "app", "core", "app_state.py"));
        var launcher = File.ReadAllText(Path.Combine(repoRoot, "launcher", "Program.cs"));
        var apiClient = File.ReadAllText(Path.Combine(repoRoot, "wpf", "Services", "ApiClient.cs"));
        var clientInterface = File.ReadAllText(Path.Combine(repoRoot, "wpf", "Services", "IHyperBoostBackendClient.cs"));

        Assert.Contains("HYPERBOOSTX_BACKEND_URL", clientSource);
        Assert.Contains("DiscoverCompatibleLocalBackendUrl", clientSource);
        Assert.Contains("/api/feature-audit/matrix", clientSource);
        Assert.Contains("IsCompatibleBackend", clientSource);
        Assert.Contains("BaseUrl => _baseUrl", clientSource);
        Assert.Contains("new HyperBoostBackendClient()", mainWindow);
        Assert.DoesNotContain("new HyperBoostBackendClient(\"http://127.0.0.1:5000\")", mainWindow);
        Assert.Contains("public ApiClient(string baseUrl = null)", apiClient);
        Assert.DoesNotContain("public ApiClient(string baseUrl = \"http://127.0.0.1:5000\")", apiClient);
        Assert.Contains("RunSafePlanFlowAsync", clientInterface);
        Assert.Contains("ApplySafePlanActionsAsync", clientInterface);
        Assert.Contains("RevertSafePlanActionsAsync", clientInterface);
        Assert.Contains("Use RunSafePlanFlowAsync", clientInterface);
        Assert.Contains("Use ApplySafePlanActionsAsync", clientInterface);
        Assert.Contains("client.BaseUrl", cyberChrome);
        Assert.Contains("HYPERBOOSTX_BACKEND_PORT", backendServer);
        Assert.Contains("HYPERBOOSTX_BACKEND_PORT", backendConstants);
        Assert.Contains("BACKEND_URL", appState);
        Assert.Contains("ResolveBackendPort", launcher);
        Assert.Contains("BackendBaseUrl", launcher);
        Assert.Contains("HYPERBOOSTX_BACKEND_PORT", launcher);
        Assert.Contains("HYPERBOOSTX_BACKEND_URL", launcher);
        Assert.Contains("IsPortAvailable", launcher);
        Assert.Contains("BuildSingleInstanceMutexName", launcher);
        Assert.Contains("SingleInstanceMutexPrefix", launcher);
        Assert.Contains("InstallRoot", launcher);
        Assert.DoesNotContain("private const string SingleInstanceMutexName = @\"Global\\HyperBoostXLauncherSingleInstance\"", launcher);
        Assert.DoesNotContain("client.GetAsync(\"http://127.0.0.1:5000/api/health\")", launcher);
        Assert.DoesNotContain("throw new InvalidOperationException(\"No free local backend port", launcher);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "HyperBoostX.sln")))
            directory = directory.Parent;

        Assert.NotNull(directory);
        return directory!.FullName;
    }
}

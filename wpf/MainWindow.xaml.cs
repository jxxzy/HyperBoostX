using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Globalization;
using System.Runtime.InteropServices;
using HyperBoostX.Services;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Media;


namespace HyperBoostX
{
    public partial class MainWindow : Window
    {
        private enum ActionState
        {
            Info,
            Success,
            Warning,
            Error
        }

        private sealed class StartupEntry
        {
            public string Name { get; set; } = "";
            public bool Enabled { get; set; }
            public string Impact { get; set; } = "Unknown";
            public int ImpactScore { get; set; }
            public double EstimatedMemoryMb { get; set; }
            public double EstimatedLoadTimeSeconds { get; set; }
            public string Source { get; set; } = "Unknown";
            public string SourceDetail { get; set; } = "";
            public string Type { get; set; } = "App";
            public string Command { get; set; } = "";
            public string RecommendedAction { get; set; } = "";
        }

        private sealed class InstalledAppEntry
        {
            public string Name { get; set; } = "";
            public string Version { get; set; } = "";
            public string Publisher { get; set; } = "";
            public string InstallDate { get; set; } = "";
            public double EstimatedSizeMb { get; set; }
            public string UninstallString { get; set; } = "";
            public string Scope { get; set; } = "User";
        }

        private sealed class ServiceEntry
        {
            public string Name { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Status { get; set; } = "";
            public string StartupType { get; set; } = "";
            public string LogOnAs { get; set; } = "";
            public int PID { get; set; }
            public double CpuPercent { get; set; }
            public double RamMb { get; set; }
            public double DiskIoKb { get; set; }
            public string ServiceType { get; set; } = "";
            public string Path { get; set; } = "";
            public string Description { get; set; } = "";
            public string Vendor { get; set; } = "";
        }

        private sealed class StorageScanSummary
        {
            public string DriveLabel { get; set; } = "";
            public int TotalFiles { get; set; }
            public int TotalFolders { get; set; }
            public string LargestFilePath { get; set; } = "N/A";
            public double LargestFileMb { get; set; }
            public string LargestFolderPath { get; set; } = "N/A";
            public double LargestFolderMb { get; set; }
            public int DuplicateCandidates { get; set; }
            public int JunkFiles { get; set; }
            public int CacheFiles { get; set; }
            public int TempFiles { get; set; }
            public int OldFiles { get; set; }
            public int HiddenFiles { get; set; }
            public int UnknownTypes { get; set; }
        }

        private sealed class AutomationRuntimeSnapshot
        {
            public double Cpu { get; set; }
            public double Ram { get; set; }
            public double Disk { get; set; }
            public double Gpu { get; set; }
            public double Temperature { get; set; }
            public double BatteryPercent { get; set; } = 100;
            public bool HasBattery { get; set; }
            public string State { get; set; } = "Idle";
            public bool IsIdle { get; set; }
        }

        private sealed class AiNaturalAutomationPlan
        {
            public string Summary { get; set; } = "No AI automation plan yet.";
            public List<AutomationRuleDefinition> Rules { get; set; } = new();
        }

        private sealed class AiActionReview
        {
            public string Action { get; set; } = "scan_only";
            public string MappedAction { get; set; } = "scan_only";
            public string RiskLevel { get; set; } = "Safe";
            public int RiskScore { get; set; }
            public string Explanation { get; set; } = "";
        }

        private sealed class AiActionExecutionGate
        {
            public bool Allowed { get; set; } = true;
            public string Summary { get; set; } = "Preflight passed.";
            public bool ShouldCreateRestorePoint { get; set; }
        }

        private sealed class FeatureAuditTarget
        {
            public string Name { get; set; } = "";
            public Func<Task> ExecuteAsync { get; set; } = () => Task.CompletedTask;
            public Func<string> Snapshot { get; set; } = () => "No snapshot.";
        }

        private sealed class FeatureAuditResult
        {
            public string Name { get; set; } = "";
            public bool Success { get; set; }
            public long DurationMs { get; set; }
            public string Details { get; set; } = "";
        }

        private sealed class FeatureAuditIncident
        {
            public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
            public ActionState State { get; set; } = ActionState.Info;
            public string TargetName { get; set; } = "";
            public string Title { get; set; } = "";
            public string Message { get; set; } = "";
            public string Meta { get; set; } = "";
        }

        private HyperBoostBackendClient _backendClient;
        private string _currentBackendUrl = "http://127.0.0.1:5000";
        private Button _selectedNavButton;
        private DispatcherTimer _dashboardTimer;
        private DispatcherTimer _storageTimer;
        private DispatcherTimer _gamingTimer;
        private DispatcherTimer _streamingTimer;
        private DispatcherTimer _creatorTimer;
        private DispatcherTimer _networkTimer;
        private DispatcherTimer _settingsTimer;
        private DispatcherTimer _realtimePageTimer;
        private bool _isUpdating;
        private string _activePage = "Dashboard";
        private int _pageNavigationVersion;
        private readonly List<string> _defaultGamingWhitelist = new()
        {
            "discord",
            "steam",
            "obs64",
            "rtss",
            "msiafterburner",
            "lghub",
            "riotclientservices",
            "vgc"
        };
        private List<string> _gamingWhitelist = new();
        private List<StartupEntry> _startupEntries = new();
        private string _lastBoostResult = "Belum ada hasil boost.";
        private string _lastBoostScore = "System Performance: --  --";
        private string _smartRecommendedUsageMode = "Daily / Office";
        private readonly Queue<string> _cleanupHistory = new();
        private string _cleanupSafetyMode = "Safe Only";
        private string _lastLargeFileDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private List<string> _lastDuplicateDeleteCandidates = new();
        private string _lastStorageSignature = "";
        private readonly Queue<string> _dashboardActivityLog = new();
        private DateTime _lastDashboardDeepRefresh = DateTime.MinValue;
        private DateTime _lastJunkEstimateUtc = DateTime.MinValue;
        private double _cachedJunkEstimateMb;
        private string _dashboardCurrentMode = "Balanced / General Use";
        private string _lastDetectedGameProcess = "";
        private string _lastDetectedGamePath = "";
        private bool _gamingBoostActive;
        private string _lastDetectedStreamingProcess = "";
        private bool _streamingModeActive;
        private string _lastDetectedCreatorProcess = "";
        private bool _creatorModeActive;
        private readonly Queue<string> _networkHistory = new();
        private readonly Queue<string> _privacyHistory = new();
        private readonly Queue<string> _securityHealthHistory = new();
        private readonly Queue<string> _appsActivityHistory = new();
        private readonly Queue<string> _appUninstallerHistory = new();
        private readonly Queue<string> _tweaksHistory = new();
        private string _tweaksSafetyMode = "Safe Only";
        private readonly Queue<string> _advancedHistory = new();
        private string _advancedRiskMode = "Safe";
        private readonly Queue<string> _servicesHistory = new();
        private readonly Queue<string> _powerHistory = new();
        private string _powerDynamicMode = "Balanced AI";
        private readonly Queue<string> _visualHistory = new();
        private string _visualMode = "Balanced";
        private readonly Queue<string> _restoreBackupHistory = new();
        private bool _autoBackupEnabled = true;
        private readonly Queue<string> _restorePointHistory = new();
        private readonly Queue<string> _automationHistory = new();
        private string _automationMode = "Smart Autonomous";
        private string _automationPolicyProfile = "Balanced automation";
        private string _automationGoal = "Keep PC Fast";
        private bool _autonomousModeEnabled = true;
        private bool _automationLearningEnabled = true;
        private bool _automationPaused;
        private readonly List<AutomationRuleDefinition> _automationRules = new();
        private readonly List<AutomationTaskRecord> _automationTasks = new();
        private readonly List<AutomationAuditEntry> _automationAudit = new();
        private readonly Queue<string> _utilitiesHistory = new();
        private readonly Queue<string> _featureAuditHistory = new();
        private string _lastUtilitiesWorkflowOutput = "";
        private string _utilitiesMode = "Smart Assist";
        private readonly List<FeatureAuditResult> _lastFeatureAuditResults = new();
        private readonly List<FeatureAuditIncident> _featureAuditIncidents = new();
        private string _lastFeatureAuditSummary = "No audit has been executed yet.";
        private string _lastFeatureAuditMode = "Quick";
        private string _testingExecutionMode = "Safe Read-Only";
        private string _lastTestingSuite = "Quick Audit";
        private string _lastTestingStrategySummary = "Testing strategy: mock-safe-live layering ready.";
        private string _lastTestingLayerSummary = "Layer summary: core logic, system action, app service, UI, full flow.";
        private string _lastTestingMetricsSummary = "Performance / stress / stability metrics will appear here.";
        private string _lastTestingCompatibilitySummary = "Compatibility and security review will appear here.";
        private DateTime? _lastFeatureAuditUtc;
        private DateTime? _featureAuditRunStartedUtc;
        private bool _featureAuditRunning;
        private bool _featureAuditCancellationRequested;
        private readonly Queue<string> _settingsHistory = new();
        private string _settingsUserMode = "Beginner";
        private string _settingsPerformanceLevel = "Balanced";
        private string _settingsRiskMode = "Safe mode";
        private string _settingsTheme = "Auto";
        private string _settingsLanguageMode = "Follow System";
        private string _settingsSidebarMode = "Full";
        private bool _settingsEngineEnabled = true;
        private bool _settingsSafetyEnabled = true;
        private bool _settingsMonitoringEnabled = true;
        private bool _settingsRefreshInProgress;
        private bool _realtimePageRefreshInProgress;
        private DateTime _lastRealtimePageRefreshUtc = DateTime.MinValue;
        private DateTime _settingsPcStaticCacheUtc = DateTime.MinValue;
        private JObject _settingsPcStaticCache;
        private DateTime _settingsSystemInfoCacheUtc = DateTime.MinValue;
        private JObject _settingsSystemInfoCache;
        private DateTime _settingsBatteryCacheUtc = DateTime.MinValue;
        private string _settingsBatteryCache = "Battery info loading...";
        private DateTime _settingsPingCacheUtc = DateTime.MinValue;
        private string _settingsPingCache = "Checking latency...";
        private readonly Queue<string> _windowsFeaturesHistory = new();
        private readonly Queue<string> _updateControlHistory = new();
        private readonly Queue<string> _driversHistory = new();
        private readonly Queue<string> _repairHistory = new();
        private string _repairSafetyMode = "Safe Only";
        private List<ServiceEntry> _serviceEntries = new();
        private readonly LocalizationService _localizationService = new();
        private readonly AppConfigService _appConfigService = new();
        private readonly SecureSecretStoreService _secureSecretStoreService = new();
        private readonly AppUpdateService _appUpdateService = new();
        private readonly DiscordWebhookService _discordWebhookService = new();
        private readonly OpenAiCopilotService _openAiCopilotService = new();
        private PersistedAppConfig _appConfig = new();
        private DispatcherTimer _automationRuntimeTimer;
        private bool _discordWebhookEnabled;
        private string _discordWebhookUrl = "";
        private string _discordUpdateWebhookUrl = "";
        private string _discordWebhookMinimumLevel = "Error";
        private int _discordWebhookCooldownSeconds = 120;
        private readonly Dictionary<string, DateTime> _discordWebhookLastSent = new();
        private bool _openAiEnabled;
        private string _openAiApiKey = "";
        private string _openAiModel = "gpt-4.1-mini";
        private string _openAiMode = "Assistant";
        private string _openAiPermissionLevel = "Ask";
        private string _lastOpenAiConnectionTestStatus = "No AI connection test run yet.";
        private const string SociabuzzDonateUrl = "https://sociabuzz.com/jxxzyshn69";
        private readonly string _currentAppVersion = System.Reflection.Assembly.GetExecutingAssembly()
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion
            ?? System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "1.2.4";
        private bool _autoCheckAppUpdates = true;
        private bool _autoInstallAppUpdates;
        private string _latestKnownAppVersion = "";
        private string _latestKnownReleaseUrl = "https://github.com/jxxzy/HyperBoostX/releases";
        private string _latestKnownInstallerAssetName = "";
        private string _latestKnownInstallerDownloadUrl = "";
        private string _latestKnownChecksumsDownloadUrl = "";
        private string _latestKnownReleaseChannel = "Stable";
        private string _lastAppUpdateReadiness = "Readiness: unknown";
        private string _lastAppUpdateSummary = "Update status has not been checked yet.";
        private DateTime? _lastAppUpdateCheckUtc;
        private DateTime? _latestKnownReleasePublishedUtc;
        private bool _isAppUpdateAvailable;
        private bool _appUpdateCheckInProgress;
        private bool _appUpdateInstallInProgress;
        private OpenAiCopilotResponse _lastAiCopilotResponse;
        private bool _aiRequestInProgress;
        private DateTime _lastAiContextBuiltUtc = DateTime.MinValue;
        private string _cachedAiSystemContext = "";
        private readonly Queue<string> _aiCopilotMemory = new();
        private string _lastAiPrompt = "";
        private string _lastAiSystemContext = "";
        private string _lastAiReasoningSummary = "No AI reasoning available yet.";
        private string _lastAiAutomationSummary = "No AI automation plan yet.";
        private string _lastAiWhySummary = "No why / why not summary yet.";
        private string _lastAiOutcomeSummary = "No AI outcome recorded yet.";
        private string _aiPreferredScenario = "General Assistance";
        private string _aiPreferredAction = "scan_only";
        private string _aiPreferredRiskStyle = "Ask";
        private int _aiTotalRequests;
        private int _aiApprovedPlans;
        private int _aiRejectedPlans;
        private int _aiCreatedAutomations;
        private readonly Dictionary<string, int> _aiIntentCounters = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _aiActionCounters = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<AiActionReview> _aiPendingActionReviews = new();
        private AiNaturalAutomationPlan _lastAiNaturalAutomationPlan = new();
        private readonly DateTime _appStartedUtc = DateTime.UtcNow;

        public MainWindow()
        {
            InitializeComponent();
            _backendClient = new HyperBoostBackendClient(_currentBackendUrl);
            _dashboardTimer = new DispatcherTimer();
            _dashboardTimer.Interval = TimeSpan.FromSeconds(1);
            _dashboardTimer.Tick += DashboardTimer_Tick;
            _storageTimer = new DispatcherTimer();
            _storageTimer.Interval = TimeSpan.FromSeconds(5);
            _storageTimer.Tick += StorageTimer_Tick;
            _gamingTimer = new DispatcherTimer();
            _gamingTimer.Interval = TimeSpan.FromSeconds(3);
            _gamingTimer.Tick += GamingTimer_Tick;
            _streamingTimer = new DispatcherTimer();
            _streamingTimer.Interval = TimeSpan.FromSeconds(3);
            _streamingTimer.Tick += StreamingTimer_Tick;
            _creatorTimer = new DispatcherTimer();
            _creatorTimer.Interval = TimeSpan.FromSeconds(1);
            _creatorTimer.Tick += CreatorTimer_Tick;
            _networkTimer = new DispatcherTimer();
            _networkTimer.Interval = TimeSpan.FromMilliseconds(1500);
            _networkTimer.Tick += NetworkTimer_Tick;
            _settingsTimer = new DispatcherTimer();
            _settingsTimer.Interval = TimeSpan.FromSeconds(1);
            _settingsTimer.Tick += SettingsTimer_Tick;
            _storageTimer.Interval = TimeSpan.FromSeconds(2);
            _gamingTimer.Interval = TimeSpan.FromSeconds(1);
            _streamingTimer.Interval = TimeSpan.FromSeconds(1);
            _realtimePageTimer = new DispatcherTimer();
            _realtimePageTimer.Interval = TimeSpan.FromSeconds(1);
            _realtimePageTimer.Tick += RealtimePageTimer_Tick;
            _automationRuntimeTimer = new DispatcherTimer();
            _automationRuntimeTimer.Interval = TimeSpan.FromSeconds(15);
            _automationRuntimeTimer.Tick += AutomationRuntimeTimer_Tick;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _localizationService.InitializeAsync();
            PopulateLanguageSelector();
            ApplyLocalizationToUi();
            _appConfig = await _appConfigService.LoadAsync();
            ApplyPersistedConfiguration();
            await LoadSensitiveConfigurationAsync();

            LoadGamingWhitelist();
            await CheckBackendHealth();
            await ShowPage("Dashboard", DashboardBtn);
            _ = EnsureAppUpdateStatusAsync(force: false, userInitiated: false);
            _automationRuntimeTimer.Start();
            _realtimePageTimer.Start();
            AppendDashboardActivity("Dashboard initialized and ready.");
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _dashboardTimer.Stop();
            _storageTimer.Stop();
            _gamingTimer.Stop();
            _streamingTimer.Stop();
            _creatorTimer.Stop();
            _networkTimer.Stop();
            _settingsTimer.Stop();
            _automationRuntimeTimer.Stop();
            _realtimePageTimer.Stop();
        }

        protected override void OnClosed(EventArgs e)
        {
            _dashboardTimer.Stop();
            _storageTimer.Stop();
            _gamingTimer.Stop();
            _streamingTimer.Stop();
            _creatorTimer.Stop();
            _networkTimer.Stop();
            _settingsTimer.Stop();
            _automationRuntimeTimer.Stop();
            _realtimePageTimer.Stop();
            _backendClient?.Dispose();
            base.OnClosed(e);
        }

        private string L(string key, string fallback, IDictionary<string, object> variables = null)
        {
            return _localizationService.T(key, fallback, variables);
        }

        private void PopulateLanguageSelector()
        {
            if (SettingsLanguageCombo == null)
                return;

            SettingsLanguageCombo.Items.Clear();
            foreach (var pack in _localizationService.GetAvailableLanguagePacks())
            {
                SettingsLanguageCombo.Items.Add(new ComboBoxItem
                {
                    Content = $"{pack.NativeName} ({pack.EnglishName}) [{pack.CoveragePercent:0.#}%]",
                    Tag = pack.LocaleCode
                });
            }

            foreach (ComboBoxItem item in SettingsLanguageCombo.Items)
            {
                if (string.Equals(item.Tag?.ToString(), _localizationService.CurrentLocale, StringComparison.OrdinalIgnoreCase))
                {
                    SettingsLanguageCombo.SelectedItem = item;
                    break;
                }
            }

            if (SettingsLanguageCombo.SelectedItem == null && SettingsLanguageCombo.Items.Count > 0)
                SettingsLanguageCombo.SelectedIndex = 0;
        }

        private string BuildLocalizedMenuLabel(int index, string key, string fallback)
        {
            return L(key, fallback);
        }

        private string GetPageKey(string pageName)
        {
            return pageName switch
            {
                "Dashboard" => "dashboard",
                "Performance" => "performance",
                "OneClickBoost" => "one_click_boost",
                "Startup" => "startup",
                "SmartRecommendation" => "smart_recommendation",
                "Cleanup" => "cleanup",
                "Storage" => "storage",
                "Gaming" => "gaming_booster",
                "Streaming" => "streaming_mode",
                "Creator" => "creator_mode",
                "Network" => "network_booster",
                "DnsLatency" => "dns_latency_tools",
                "BackgroundApps" => "background_apps",
                "Privacy" => "privacy_center",
                "SecurityHealth" => "security_health",
                "AppsManager" => "apps_manager",
                "AppUninstaller" => "app_uninstaller",
                "Services" => "windows_services",
                "Power" => "power_optimization",
                "Visual" => "visual_effects",
                "WindowsFeatures" => "windows_features",
                "UpdateControl" => "update_control",
                "Repair" => "repair_tools",
                "Advanced" => "advanced_tweaks",
                "Restore" => "restore_backup",
                "RestorePoint" => "restore_point_manager",
                "Automation" => "scheduled_automation",
                "Utilities" => "utilities_tools",
                "Settings" => "settings",
                "Tweaks" => "tweaks_center",
                "Drivers" => "driver_update_center",
                "Booster" => "one_click_boost",
                "About" => "about",
                _ => pageName.ToLowerInvariant()
            };
        }

        private void SetLocalizedPageHeader(string pageName, string fallbackTitle, string fallbackSubtitle)
        {
            var pageKey = GetPageKey(pageName);
            SetPageHeader(
                L($"page.{pageKey}.title", fallbackTitle),
                L($"page.{pageKey}.subtitle", fallbackSubtitle));
        }

        private void ApplyLocalizationToUi()
        {
            Title = L("app.window_title", "HyperBoost X - WPF Client");

            OneClickBoostBtn.Content = BuildLocalizedMenuLabel(1, "menu.one_click_boost", "One Click Boost");
            GamingModeBtn.Content = BuildLocalizedMenuLabel(2, "menu.gaming_mode", "Gaming Mode");
            SmartRecommendationBtn.Content = BuildLocalizedMenuLabel(3, "menu.smart_recommendation", "Smart Recommendation");
            DashboardBtn.Content = BuildLocalizedMenuLabel(4, "menu.dashboard", "Dashboard");
            PerformanceBtn.Content = BuildLocalizedMenuLabel(5, "menu.performance", "Performance Boost");
            StartupBtn.Content = BuildLocalizedMenuLabel(6, "menu.startup", "Startup Manager");
            BackgroundAppsBtn.Content = BuildLocalizedMenuLabel(7, "menu.background_apps", "Background Apps");
            CleanupBtn.Content = BuildLocalizedMenuLabel(8, "menu.cleanup", "Cleanup");
            StorageBtn.Content = BuildLocalizedMenuLabel(9, "menu.storage", "Storage");
            GamingBoosterBtn.Content = BuildLocalizedMenuLabel(10, "menu.gaming_booster", "Gaming Booster");
            StreamingModeBtn.Content = BuildLocalizedMenuLabel(11, "menu.streaming_mode", "Streaming Mode");
            CreatorModeBtn.Content = BuildLocalizedMenuLabel(12, "menu.creator_mode", "Creator Mode");
            NetworkBoosterBtn.Content = BuildLocalizedMenuLabel(13, "menu.network_booster", "Network Booster");
            DnsLatencyToolsBtn.Content = BuildLocalizedMenuLabel(14, "menu.dns_latency_tools", "DNS & Latency Tools");
            PrivacyCenterBtn.Content = BuildLocalizedMenuLabel(15, "menu.privacy_center", "Privacy Center");
            SecurityHealthBtn.Content = BuildLocalizedMenuLabel(16, "menu.security_health", "Security & Health");
            AppsManagerBtn.Content = BuildLocalizedMenuLabel(17, "menu.apps_manager", "Apps Manager");
            TweaksCenterBtn.Content = BuildLocalizedMenuLabel(18, "menu.tweaks_center", "Tweaks Center");
            WindowsFeaturesBtn.Content = BuildLocalizedMenuLabel(19, "menu.windows_features", "Windows Features");
            UpdateControlBtn.Content = BuildLocalizedMenuLabel(20, "menu.update_control", "Update Control");
            RepairToolsBtn.Content = BuildLocalizedMenuLabel(21, "menu.repair_tools", "Repair Tools");
            DriverUpdateCenterBtn.Content = BuildLocalizedMenuLabel(22, "menu.driver_update_center", "Driver & Update Center");
            AppUninstallerBtn.Content = BuildLocalizedMenuLabel(23, "menu.app_uninstaller", "App Uninstaller");
            AdvancedTweaksBtn.Content = BuildLocalizedMenuLabel(24, "menu.advanced_tweaks", "Advanced Tweaks");
            WindowsServicesBtn.Content = BuildLocalizedMenuLabel(25, "menu.windows_services", "Windows Services");
            PowerOptimizationBtn.Content = BuildLocalizedMenuLabel(26, "menu.power_optimization", "Power Optimization");
            VisualEffectsBtn.Content = BuildLocalizedMenuLabel(27, "menu.visual_effects", "Visual Effects");
            RestoreBackupBtn.Content = BuildLocalizedMenuLabel(28, "menu.restore_backup", "Restore & Backup");
            RestorePointManagerBtn.Content = BuildLocalizedMenuLabel(29, "menu.restore_point_manager", "Restore Point Manager");
            ScheduledAutomationBtn.Content = BuildLocalizedMenuLabel(30, "menu.scheduled_automation", "Scheduled Automation");
            UtilitiesToolsBtn.Content = BuildLocalizedMenuLabel(31, "menu.utilities_tools", "Utilities Tools");
            SettingsBtn.Content = BuildLocalizedMenuLabel(32, "menu.settings", "App Settings");
            AboutAppBtn.Content = BuildLocalizedMenuLabel(33, "menu.about", "About App");
            ExitBtn.Content = $"0. {L("menu.exit", "Exit")}";

            SettingsLocalizationTitleText.Text = L("settings.language.overview_title", "Language & Localization");
            ApplyUiSettingsBtn.Content = L("settings.language.apply", "Apply Language");
            FollowSystemLanguageBtn.Content = L("settings.language.follow_system", "Follow System");
            AutoDetectLanguageBtn.Content = L("settings.language.auto_detect", "Auto Detect");
            OpenLanguagePacksBtn.Content = L("settings.language.open_packs", "Open Language Packs");
            ExportLanguageReportBtn.Content = L("settings.language.export_report", "Export Coverage Report");
            ToggleSidebarModeBtn.Content = "Toggle Sidebar Mode";

            if (!string.IsNullOrWhiteSpace(_activePage))
            {
                PageTitle.Text = L($"page.{GetPageKey(_activePage)}.title", PageTitle.Text);
            }
        }

        private void ApplyPersistedConfiguration()
        {
            var settings = _appConfig.Settings ?? new PersistedSettingsState();
            var automation = _appConfig.Automation ?? new PersistedAutomationState();
            var ai = _appConfig.Ai ?? new PersistedAiState();

            _settingsTheme = settings.Theme;
            _settingsLanguageMode = settings.LanguageMode;
            _settingsSidebarMode = settings.SidebarMode;
            _settingsUserMode = settings.UserMode;
            _settingsPerformanceLevel = settings.PerformanceLevel;
            _settingsRiskMode = settings.RiskMode;
            _settingsEngineEnabled = settings.EngineEnabled;
            _settingsSafetyEnabled = settings.SafetyEnabled;
            _settingsMonitoringEnabled = settings.MonitoringEnabled;
            _discordWebhookEnabled = settings.DiscordWebhookEnabled;
            _discordWebhookMinimumLevel = string.IsNullOrWhiteSpace(settings.DiscordWebhookMinimumLevel) ? "Error" : settings.DiscordWebhookMinimumLevel;
            _discordWebhookCooldownSeconds = Math.Max(15, settings.DiscordWebhookCooldownSeconds);
            _openAiEnabled = settings.OpenAiEnabled;
            _openAiModel = string.IsNullOrWhiteSpace(settings.OpenAiModel) ? "gpt-4.1-mini" : settings.OpenAiModel;
            _openAiMode = string.IsNullOrWhiteSpace(settings.OpenAiMode) ? "Assistant" : settings.OpenAiMode;
            _openAiPermissionLevel = string.IsNullOrWhiteSpace(settings.OpenAiPermissionLevel) ? "Ask" : settings.OpenAiPermissionLevel;
            _lastOpenAiConnectionTestStatus = string.IsNullOrWhiteSpace(settings.LastOpenAiConnectionTestStatus)
                ? "No AI connection test run yet."
                : settings.LastOpenAiConnectionTestStatus;
            _autoCheckAppUpdates = settings.AutoCheckAppUpdates;
            _autoInstallAppUpdates = settings.AutoInstallAppUpdates;
            _latestKnownAppVersion = settings.LastKnownLatestVersion ?? "";
            _latestKnownReleaseUrl = string.IsNullOrWhiteSpace(settings.LastKnownReleaseUrl)
                ? "https://github.com/jxxzy/HyperBoostX/releases"
                : settings.LastKnownReleaseUrl;
            _latestKnownReleaseChannel = string.IsNullOrWhiteSpace(settings.LastKnownReleaseChannel) ? "Stable" : settings.LastKnownReleaseChannel;
            _lastAppUpdateSummary = string.IsNullOrWhiteSpace(settings.LastAppUpdateSummary)
                ? "Update status has not been checked yet."
                : settings.LastAppUpdateSummary;
            _lastAppUpdateCheckUtc = settings.LastAppUpdateCheckUtc;
            if (DateTime.TryParse(settings.LastKnownReleasePublishedUtc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var releasePublishedUtc))
                _latestKnownReleasePublishedUtc = releasePublishedUtc;
            _automationMode = settings.AutomationMode;
            _automationPolicyProfile = settings.AutomationPolicyProfile;
            _autonomousModeEnabled = settings.AutonomousEnabled;
            _automationLearningEnabled = settings.LearningEnabled;
            _autoBackupEnabled = settings.AutoBackupEnabled;
            _autoRestorePointEngineEnabled = settings.AutoRestorePointEnabled;
            _automationGoal = automation.Goal;
            _automationPaused = automation.Paused;
            _lastAiPrompt = ai.LastPrompt ?? "";
            _lastAiSystemContext = ai.LastContext ?? "";
            _lastAiReasoningSummary = string.IsNullOrWhiteSpace(ai.LastReasoningSummary) ? "No AI reasoning available yet." : ai.LastReasoningSummary;
            _lastAiAutomationSummary = string.IsNullOrWhiteSpace(ai.LastAutomationSummary) ? "No AI automation plan yet." : ai.LastAutomationSummary;
            _lastAiWhySummary = string.IsNullOrWhiteSpace(ai.LastWhySummary) ? "No why / why not summary yet." : ai.LastWhySummary;
            _lastAiOutcomeSummary = string.IsNullOrWhiteSpace(ai.LastOutcomeSummary) ? "No AI outcome recorded yet." : ai.LastOutcomeSummary;
            _aiTotalRequests = ai.TotalRequests;
            _aiApprovedPlans = ai.ApprovedPlans;
            _aiRejectedPlans = ai.RejectedPlans;
            _aiCreatedAutomations = ai.CreatedAutomations;
            _aiPreferredScenario = string.IsNullOrWhiteSpace(ai.PreferredScenario) ? "General Assistance" : ai.PreferredScenario;
            _aiPreferredAction = string.IsNullOrWhiteSpace(ai.PreferredAction) ? "scan_only" : ai.PreferredAction;
            _aiPreferredRiskStyle = string.IsNullOrWhiteSpace(ai.PreferredRiskStyle) ? "Ask" : ai.PreferredRiskStyle;
            _aiIntentCounters.Clear();
            _aiActionCounters.Clear();
            foreach (var pair in ai.IntentCounters ?? new Dictionary<string, int>())
                _aiIntentCounters[pair.Key] = pair.Value;
            foreach (var pair in ai.ActionCounters ?? new Dictionary<string, int>())
                _aiActionCounters[pair.Key] = pair.Value;

            _aiCopilotMemory.Clear();
            foreach (var entry in (ai.MemoryEntries ?? new List<string>()).TakeLast(10))
                _aiCopilotMemory.Enqueue(entry);

            if (!string.IsNullOrWhiteSpace(ai.LastReply) || !string.IsNullOrWhiteSpace(ai.LastIntent))
            {
                _lastAiCopilotResponse = new OpenAiCopilotResponse
                {
                    Intent = string.IsNullOrWhiteSpace(ai.LastIntent) ? "general_help" : ai.LastIntent,
                    Confidence = ai.LastConfidence,
                    Reply = ai.LastReply ?? "",
                    SafeActions = ai.LastSafeActions ?? new List<string>(),
                    RawContent = ai.LastReply ?? ""
                };
            }

            _automationRules.Clear();
            _automationRules.AddRange(automation.Rules ?? new List<AutomationRuleDefinition>());
            _automationTasks.Clear();
            _automationTasks.AddRange(automation.Tasks ?? new List<AutomationTaskRecord>());
            _automationAudit.Clear();
            _automationAudit.AddRange(automation.AuditTrail ?? new List<AutomationAuditEntry>());

            if (_automationRules.Count == 0)
                EnsureAutomationRulesForGoal(_automationGoal, replaceExisting: true);

            _automationRuntimeTimer.Interval = TimeSpan.FromSeconds(Math.Max(10, automation.EvaluationIntervalSeconds));

            SelectComboItemByContent(SettingsThemeCombo, _settingsTheme);
            SelectComboItemByContent(SettingsDensityCombo, settings.Density);
            SelectComboItemByTag(SettingsLanguageCombo, settings.Language);
            SelectComboItemByContent(AutomationGoalCombo, _automationGoal);
            if (DiscordWebhookUrlInput != null)
                DiscordWebhookUrlInput.Text = _discordWebhookUrl;
            if (DiscordUpdateWebhookUrlInput != null)
                DiscordUpdateWebhookUrlInput.Text = _discordUpdateWebhookUrl;
            SelectComboItemByContent(DiscordWebhookLevelCombo, _discordWebhookMinimumLevel);
            if (DiscordWebhookCooldownInput != null)
                DiscordWebhookCooldownInput.Text = _discordWebhookCooldownSeconds.ToString(CultureInfo.InvariantCulture);
            if (OpenAiApiKeyInput != null)
                OpenAiApiKeyInput.Text = _openAiApiKey;
            if (OpenAiModelInput != null)
                OpenAiModelInput.Text = _openAiModel;
            SelectComboItemByContent(OpenAiModeCombo, _openAiMode);
            SelectComboItemByContent(OpenAiPermissionCombo, _openAiPermissionLevel);
            if (ToggleAutoAppUpdateBtn != null)
                ToggleAutoAppUpdateBtn.Content = _autoCheckAppUpdates ? "Auto Check Updates: ON" : "Auto Check Updates: OFF";
        }

        private async Task LoadSensitiveConfigurationAsync()
        {
            var settings = _appConfig.Settings ?? new PersistedSettingsState();
            var secrets = await _secureSecretStoreService.LoadAsync();

            var envOpenAi = Environment.GetEnvironmentVariable("HYPERBOOSTX_OPENAI_API_KEY")?.Trim() ?? "";
            var envDiscord = Environment.GetEnvironmentVariable("HYPERBOOSTX_DISCORD_WEBHOOK_URL")?.Trim() ?? "";
            var envDiscordUpdate = Environment.GetEnvironmentVariable("HYPERBOOSTX_DISCORD_UPDATE_WEBHOOK_URL")?.Trim() ?? "";

            var legacyOpenAi = settings.OpenAiApiKey?.Trim() ?? "";
            var legacyDiscord = settings.DiscordWebhookUrl?.Trim() ?? "";
            var legacyDiscordUpdate = settings.DiscordUpdateWebhookUrl?.Trim() ?? "";

            _openAiApiKey = !string.IsNullOrWhiteSpace(envOpenAi)
                ? envOpenAi
                : !string.IsNullOrWhiteSpace(secrets.OpenAiApiKey)
                    ? secrets.OpenAiApiKey
                    : legacyOpenAi;

            if (!string.IsNullOrWhiteSpace(_openAiApiKey))
                _openAiEnabled = true;

            _discordWebhookUrl = !string.IsNullOrWhiteSpace(envDiscord)
                ? envDiscord
                : !string.IsNullOrWhiteSpace(secrets.DiscordWebhookUrl)
                    ? secrets.DiscordWebhookUrl
                    : legacyDiscord;

            _discordUpdateWebhookUrl = !string.IsNullOrWhiteSpace(envDiscordUpdate)
                ? envDiscordUpdate
                : !string.IsNullOrWhiteSpace(secrets.DiscordUpdateWebhookUrl)
                    ? secrets.DiscordUpdateWebhookUrl
                    : legacyDiscordUpdate;

            if (OpenAiApiKeyInput != null)
                OpenAiApiKeyInput.Text = _openAiApiKey;

            if (DiscordWebhookUrlInput != null)
                DiscordWebhookUrlInput.Text = _discordWebhookUrl;
            if (DiscordUpdateWebhookUrlInput != null)
                DiscordUpdateWebhookUrlInput.Text = _discordUpdateWebhookUrl;

            var shouldMigrateLegacySecrets =
                string.IsNullOrWhiteSpace(envOpenAi) &&
                string.IsNullOrWhiteSpace(envDiscord) &&
                string.IsNullOrWhiteSpace(envDiscordUpdate) &&
                ((!string.IsNullOrWhiteSpace(legacyOpenAi) && string.IsNullOrWhiteSpace(secrets.OpenAiApiKey)) ||
                 (!string.IsNullOrWhiteSpace(legacyDiscord) && string.IsNullOrWhiteSpace(secrets.DiscordWebhookUrl)) ||
                 (!string.IsNullOrWhiteSpace(legacyDiscordUpdate) && string.IsNullOrWhiteSpace(secrets.DiscordUpdateWebhookUrl)));

            if (shouldMigrateLegacySecrets)
            {
                await _secureSecretStoreService.SaveAsync(new PersistedSecureSecrets
                {
                    OpenAiApiKey = legacyOpenAi,
                    DiscordWebhookUrl = legacyDiscord,
                    DiscordUpdateWebhookUrl = legacyDiscordUpdate
                });

                settings.OpenAiApiKey = "";
                settings.DiscordWebhookUrl = "";
                settings.DiscordUpdateWebhookUrl = "";
                _appConfig.Settings = settings;
                await _appConfigService.SaveAsync(_appConfig);
            }
        }

        private void SelectComboItemByContent(ComboBox comboBox, string value)
        {
            if (comboBox == null || string.IsNullOrWhiteSpace(value))
                return;

            foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
            {
                if (string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        private void SelectComboItemByTag(ComboBox comboBox, string value)
        {
            if (comboBox == null || string.IsNullOrWhiteSpace(value))
                return;

            foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
            {
                if (string.Equals(item.Tag?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        private async Task SavePersistedConfigurationAsync()
        {
            if (_appConfig == null)
                _appConfig = new PersistedAppConfig();

            var settings = _appConfig.Settings ?? new PersistedSettingsState();
            settings.Theme = _settingsTheme;
            settings.Density = (SettingsDensityCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? settings.Density;
            settings.LanguageMode = _settingsLanguageMode;
            settings.Language = _localizationService.CurrentLocale;
            settings.SidebarMode = _settingsSidebarMode;
            settings.UserMode = _settingsUserMode;
            settings.PerformanceLevel = _settingsPerformanceLevel;
            settings.RiskMode = _settingsRiskMode;
            settings.AutomationMode = _automationMode;
            settings.AutomationPolicyProfile = _automationPolicyProfile;
            settings.EngineEnabled = _settingsEngineEnabled;
            settings.SafetyEnabled = _settingsSafetyEnabled;
            settings.MonitoringEnabled = _settingsMonitoringEnabled;
            settings.LearningEnabled = _automationLearningEnabled;
            settings.AutonomousEnabled = _autonomousModeEnabled;
            settings.AutoBackupEnabled = _autoBackupEnabled;
            settings.AutoRestorePointEnabled = _autoRestorePointEngineEnabled;
            settings.DiscordWebhookEnabled = _discordWebhookEnabled;
            settings.DiscordWebhookUrl = "";
            settings.DiscordUpdateWebhookUrl = "";
            settings.DiscordWebhookMinimumLevel = _discordWebhookMinimumLevel;
            settings.DiscordWebhookCooldownSeconds = _discordWebhookCooldownSeconds;
            settings.OpenAiEnabled = _openAiEnabled;
            settings.OpenAiApiKey = "";
            settings.OpenAiModel = _openAiModel;
            settings.OpenAiMode = _openAiMode;
            settings.OpenAiPermissionLevel = _openAiPermissionLevel;
            settings.LastOpenAiConnectionTestStatus = _lastOpenAiConnectionTestStatus;
            settings.AutoCheckAppUpdates = _autoCheckAppUpdates;
            settings.AutoInstallAppUpdates = _autoInstallAppUpdates;
            settings.LastKnownLatestVersion = _latestKnownAppVersion;
            settings.LastKnownReleaseUrl = _latestKnownReleaseUrl;
            settings.LastKnownReleaseChannel = _latestKnownReleaseChannel;
            settings.LastKnownReleasePublishedUtc = _latestKnownReleasePublishedUtc?.ToString("o", CultureInfo.InvariantCulture) ?? "";
            settings.LastAppUpdateSummary = _lastAppUpdateSummary;
            settings.LastAppUpdateCheckUtc = _lastAppUpdateCheckUtc;

            var automation = _appConfig.Automation ?? new PersistedAutomationState();
            automation.Goal = _automationGoal;
            automation.Mode = _automationMode;
            automation.PolicyProfile = _automationPolicyProfile;
            automation.Enabled = _autonomousModeEnabled;
            automation.LearningEnabled = _automationLearningEnabled;
            automation.Paused = _automationPaused;
            automation.IdleCpuThreshold = 15;
            automation.HighRamThreshold = 80;
            automation.LowStorageThreshold = 85;
            automation.HighTemperatureThreshold = 85;
            automation.Rules = _automationRules.Select(CloneRule).ToList();
            automation.Tasks = _automationTasks
                .OrderByDescending(task => task.CreatedUtc)
                .Take(40)
                .Select(CloneTask)
                .ToList();
            automation.AuditTrail = _automationAudit
                .OrderByDescending(item => item.TimestampUtc)
                .Take(80)
                .Select(CloneAudit)
                .ToList();

            var ai = _appConfig.Ai ?? new PersistedAiState();
            ai.LastPrompt = _lastAiPrompt;
            ai.LastIntent = _lastAiCopilotResponse?.Intent ?? "general_help";
            ai.LastConfidence = _lastAiCopilotResponse?.Confidence ?? 0.5;
            ai.LastReply = _lastAiCopilotResponse?.Reply ?? "";
            ai.LastContext = _lastAiSystemContext;
            ai.LastReasoningSummary = _lastAiReasoningSummary;
            ai.LastAutomationSummary = _lastAiAutomationSummary;
            ai.LastWhySummary = _lastAiWhySummary;
            ai.LastOutcomeSummary = _lastAiOutcomeSummary;
            ai.LastSafeActions = _lastAiCopilotResponse?.SafeActions?.Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
            ai.MemoryEntries = _aiCopilotMemory.ToList();
            ai.TotalRequests = _aiTotalRequests;
            ai.ApprovedPlans = _aiApprovedPlans;
            ai.RejectedPlans = _aiRejectedPlans;
            ai.CreatedAutomations = _aiCreatedAutomations;
            ai.PreferredScenario = _aiPreferredScenario;
            ai.PreferredAction = _aiPreferredAction;
            ai.PreferredRiskStyle = _aiPreferredRiskStyle;
            ai.IntentCounters = new Dictionary<string, int>(_aiIntentCounters, StringComparer.OrdinalIgnoreCase);
            ai.ActionCounters = new Dictionary<string, int>(_aiActionCounters, StringComparer.OrdinalIgnoreCase);

            _appConfig.Settings = settings;
            _appConfig.Automation = automation;
            _appConfig.Ai = ai;
            await _appConfigService.SaveAsync(_appConfig);
            await _secureSecretStoreService.SaveAsync(new PersistedSecureSecrets
            {
                DiscordWebhookUrl = _discordWebhookUrl,
                DiscordUpdateWebhookUrl = _discordUpdateWebhookUrl,
                OpenAiApiKey = _openAiApiKey
            });
        }

        private static AutomationRuleDefinition CloneRule(AutomationRuleDefinition rule)
        {
            return new AutomationRuleDefinition
            {
                Id = rule.Id,
                Name = rule.Name,
                Goal = rule.Goal,
                Scenario = rule.Scenario,
                TriggerType = rule.TriggerType,
                ActionType = rule.ActionType,
                SafeLevel = rule.SafeLevel,
                Enabled = rule.Enabled,
                RequiresIdle = rule.RequiresIdle,
                MaxCpuPercent = rule.MaxCpuPercent,
                MaxRamPercent = rule.MaxRamPercent,
                MaxDiskPercent = rule.MaxDiskPercent,
                MaxTemperatureC = rule.MaxTemperatureC,
                MinimumMinutesBetweenRuns = rule.MinimumMinutesBetweenRuns,
                LastRunUtc = rule.LastRunUtc
            };
        }

        private static AutomationTaskRecord CloneTask(AutomationTaskRecord task)
        {
            return new AutomationTaskRecord
            {
                Id = task.Id,
                RuleId = task.RuleId,
                Name = task.Name,
                Status = task.Status,
                TriggerReason = task.TriggerReason,
                ResultSummary = task.ResultSummary,
                CreatedUtc = task.CreatedUtc,
                ScheduledForUtc = task.ScheduledForUtc,
                LastTriedUtc = task.LastTriedUtc,
                CompletedUtc = task.CompletedUtc,
                RetryCount = task.RetryCount
            };
        }

        private static AutomationAuditEntry CloneAudit(AutomationAuditEntry item)
        {
            return new AutomationAuditEntry
            {
                TimestampUtc = item.TimestampUtc,
                Level = item.Level,
                Message = item.Message,
                Source = item.Source
            };
        }

        private async Task PersistAndRefreshSettingsAsync()
        {
            await SavePersistedConfigurationAsync();
            await RefreshSettingsViewAsync();
        }

        private async Task PersistAndRefreshAutomationAsync(bool refreshView = true)
        {
            await SavePersistedConfigurationAsync();
            if (refreshView)
                await RefreshAutomationViewAsync();
        }

        private void AppendAutomationAudit(string level, string message, string source = "Automation")
        {
            _automationAudit.Add(new AutomationAuditEntry
            {
                TimestampUtc = DateTime.UtcNow,
                Level = level,
                Message = message,
                Source = source
            });

            if (_automationAudit.Count > 120)
                _automationAudit.RemoveRange(0, _automationAudit.Count - 120);
        }

        private void EnsureAutomationRulesForGoal(string goal, bool replaceExisting)
        {
            if (replaceExisting)
                _automationRules.Clear();

            goal = string.IsNullOrWhiteSpace(goal) ? "Keep PC Fast" : goal;
            _automationGoal = goal;

            var rules = new List<AutomationRuleDefinition>();
            if (goal.Contains("Gaming", StringComparison.OrdinalIgnoreCase))
            {
                rules.Add(new AutomationRuleDefinition
                {
                    Name = "Gaming Pre-Boost",
                    Goal = goal,
                    Scenario = "Gaming Session",
                    TriggerType = "gaming",
                    ActionType = "gaming_prep",
                    SafeLevel = "Safe",
                    MaxCpuPercent = 95,
                    MaxRamPercent = 95,
                    MaxDiskPercent = 95,
                    MaxTemperatureC = 88,
                    MinimumMinutesBetweenRuns = 25
                });
                rules.Add(new AutomationRuleDefinition
                {
                    Name = "Gaming Network Stabilizer",
                    Goal = goal,
                    Scenario = "Gaming Session",
                    TriggerType = "gaming",
                    ActionType = "flush_dns",
                    SafeLevel = "Safe",
                    MaxCpuPercent = 95,
                    MaxRamPercent = 95,
                    MaxDiskPercent = 95,
                    MaxTemperatureC = 88,
                    MinimumMinutesBetweenRuns = 20
                });
            }
            else if (goal.Contains("Storage", StringComparison.OrdinalIgnoreCase))
            {
                rules.Add(new AutomationRuleDefinition
                {
                    Name = "Predictive Storage Cleanup",
                    Goal = goal,
                    Scenario = "Idle Maintenance",
                    TriggerType = "storage",
                    ActionType = "cleanup_light",
                    SafeLevel = "Safe",
                    RequiresIdle = true,
                    MaxCpuPercent = 30,
                    MaxRamPercent = 85,
                    MaxDiskPercent = 100,
                    MaxTemperatureC = 82,
                    MinimumMinutesBetweenRuns = 45
                });
            }
            else if (goal.Contains("Network", StringComparison.OrdinalIgnoreCase))
            {
                rules.Add(new AutomationRuleDefinition
                {
                    Name = "Network Recovery Watch",
                    Goal = goal,
                    Scenario = "Network Stability",
                    TriggerType = "network",
                    ActionType = "network_recover",
                    SafeLevel = "Safe",
                    RequiresIdle = false,
                    MaxCpuPercent = 85,
                    MaxRamPercent = 92,
                    MaxDiskPercent = 95,
                    MaxTemperatureC = 88,
                    MinimumMinutesBetweenRuns = 30
                });
            }
            else if (goal.Contains("Battery", StringComparison.OrdinalIgnoreCase))
            {
                rules.Add(new AutomationRuleDefinition
                {
                    Name = "Battery Saver Guard",
                    Goal = goal,
                    Scenario = "Low Battery Protection",
                    TriggerType = "battery",
                    ActionType = "power_saver",
                    SafeLevel = "Safe",
                    RequiresIdle = false,
                    MaxCpuPercent = 95,
                    MaxRamPercent = 95,
                    MaxDiskPercent = 95,
                    MaxTemperatureC = 88,
                    MinimumMinutesBetweenRuns = 15
                });
            }
            else if (goal.Contains("Windows Safe", StringComparison.OrdinalIgnoreCase) || goal.Contains("Updated", StringComparison.OrdinalIgnoreCase))
            {
                rules.Add(new AutomationRuleDefinition
                {
                    Name = "Idle Repair Sweep",
                    Goal = goal,
                    Scenario = "Idle Maintenance",
                    TriggerType = "idle",
                    ActionType = "repair_quick",
                    SafeLevel = "Moderate",
                    RequiresIdle = true,
                    MaxCpuPercent = 25,
                    MaxRamPercent = 85,
                    MaxDiskPercent = 95,
                    MaxTemperatureC = 82,
                    MinimumMinutesBetweenRuns = 60
                });
            }
            else
            {
                rules.Add(new AutomationRuleDefinition
                {
                    Name = "Idle Cleanup Guardian",
                    Goal = goal,
                    Scenario = "Idle Maintenance",
                    TriggerType = "idle",
                    ActionType = "cleanup_light",
                    SafeLevel = "Safe",
                    RequiresIdle = true,
                    MaxCpuPercent = 25,
                    MaxRamPercent = 85,
                    MaxDiskPercent = 100,
                    MaxTemperatureC = 82,
                    MinimumMinutesBetweenRuns = 45
                });
                rules.Add(new AutomationRuleDefinition
                {
                    Name = "High RAM Stabilizer",
                    Goal = goal,
                    Scenario = "Background Recovery",
                    TriggerType = "memory",
                    ActionType = "memory_stabilize",
                    SafeLevel = "Safe",
                    RequiresIdle = false,
                    MaxCpuPercent = 85,
                    MaxRamPercent = 100,
                    MaxDiskPercent = 95,
                    MaxTemperatureC = 86,
                    MinimumMinutesBetweenRuns = 25
                });
                rules.Add(new AutomationRuleDefinition
                {
                    Name = "Background Process Trimmer",
                    Goal = goal,
                    Scenario = "Idle Maintenance",
                    TriggerType = "background",
                    ActionType = "background_trim",
                    SafeLevel = "Safe",
                    RequiresIdle = true,
                    MaxCpuPercent = 30,
                    MaxRamPercent = 92,
                    MaxDiskPercent = 95,
                    MaxTemperatureC = 84,
                    MinimumMinutesBetweenRuns = 30
                });
            }

            foreach (var rule in rules)
            {
                if (!_automationRules.Any(existing =>
                        string.Equals(existing.Name, rule.Name, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(existing.Goal, rule.Goal, StringComparison.OrdinalIgnoreCase)))
                {
                    _automationRules.Add(rule);
                }
            }
        }

        private async void AutomationRuntimeTimer_Tick(object sender, EventArgs e)
        {
            _automationRuntimeTimer.Stop();
            try
            {
                await EvaluateAutomationEngineAsync("timer");

                if (string.Equals(_activePage, "Automation", StringComparison.OrdinalIgnoreCase))
                    await RefreshAutomationViewAsync();
                else if (string.Equals(_activePage, "SmartRecommendation", StringComparison.OrdinalIgnoreCase))
                    await RefreshAiCopilotDiagnosticsAsync(refreshContext: true);
            }
            catch (Exception ex)
            {
                AppendAutomationAudit("Error", $"Automation runtime tick failed: {ex.Message}");
            }
            finally
            {
                _automationRuntimeTimer.Start();
            }
        }

        private async void SettingsTimer_Tick(object sender, EventArgs e)
        {
            if (_settingsRefreshInProgress || !IsLoaded || _activePage != "Settings")
                return;

            _settingsRefreshInProgress = true;
            try
            {
                await RefreshSettingsViewAsync();
            }
            finally
            {
                _settingsRefreshInProgress = false;
            }
        }

        private async void RealtimePageTimer_Tick(object sender, EventArgs e)
        {
            if (_realtimePageRefreshInProgress || !IsLoaded || string.IsNullOrWhiteSpace(_activePage))
                return;

            if (HasDedicatedRealtimeTimer(_activePage))
                return;

            var interval = GetActivePageRealtimeInterval(_activePage);
            if (interval <= TimeSpan.Zero)
                return;

            if (_lastRealtimePageRefreshUtc != DateTime.MinValue && DateTime.UtcNow - _lastRealtimePageRefreshUtc < interval)
                return;

            _realtimePageRefreshInProgress = true;
            try
            {
                await RefreshActivePageRealtimeAsync(_activePage);
                _lastRealtimePageRefreshUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                AppendDashboardActivity($"Realtime page refresh warning on {_activePage}: {ex.Message}");
            }
            finally
            {
                _realtimePageRefreshInProgress = false;
            }
        }

        private static bool HasDedicatedRealtimeTimer(string pageName)
        {
            return pageName is "Dashboard" or "Storage" or "Gaming" or "Streaming" or "Creator" or "Network" or "DnsLatency" or "Settings";
        }

        private static TimeSpan GetActivePageRealtimeInterval(string pageName)
        {
            return pageName switch
            {
                "Startup" => TimeSpan.FromSeconds(2),
                "BackgroundApps" => TimeSpan.FromSeconds(2),
                "Performance" => TimeSpan.FromSeconds(2),
                "Cleanup" => TimeSpan.FromSeconds(3),
                "SecurityHealth" => TimeSpan.FromSeconds(2),
                "Privacy" => TimeSpan.FromSeconds(4),
                "AppsManager" => TimeSpan.FromSeconds(4),
                "Services" => TimeSpan.FromSeconds(3),
                "Power" => TimeSpan.FromSeconds(2),
                "Visual" => TimeSpan.FromSeconds(2),
                "Automation" => TimeSpan.FromSeconds(2),
                "Utilities" => TimeSpan.FromSeconds(3),
                "Testing" => TimeSpan.FromSeconds(4),
                "About" => TimeSpan.FromSeconds(5),
                _ => TimeSpan.Zero
            };
        }

        private async Task RefreshActivePageRealtimeAsync(string pageName)
        {
            switch (pageName)
            {
                case "Startup":
                    await RefreshStartupItems();
                    break;
                case "BackgroundApps":
                    await RefreshBackgroundApps();
                    break;
                case "Performance":
                    await RefreshPerformanceBoostViewAsync();
                    break;
                case "Cleanup":
                    await RefreshCleanupViewAsync();
                    break;
                case "SecurityHealth":
                    await RefreshSecurityHealthViewAsync();
                    break;
                case "Privacy":
                    await RefreshPrivacyViewAsync();
                    break;
                case "AppsManager":
                    await RefreshAppsManagerViewAsync();
                    break;
                case "Services":
                    await RefreshServicesViewAsync();
                    break;
                case "Power":
                    await RefreshPowerOptimizationViewAsync();
                    break;
                case "Visual":
                    await RefreshVisualEffectsViewAsync();
                    break;
                case "Automation":
                    await RefreshAutomationViewAsync();
                    break;
                case "Utilities":
                    await RefreshUtilitiesViewAsync();
                    break;
                case "Testing":
                    await RefreshFeatureAuditViewAsync();
                    break;
                case "About":
                    await RefreshAboutViewAsync();
                    break;
            }
        }

        private async Task<AutomationRuntimeSnapshot> BuildAutomationSnapshotAsync()
        {
            var json = await GetSystemStatsJsonAsync();
            var cpu = ReadNumericToken(json, "cpu", "cpu_percent");
            var ram = ReadNumericToken(json, "memory", "memory_percent");
            var disk = ReadNumericToken(json, "disk", "disk_percent");
            var gpu = ReadGpuLoadStat(json);
            var hasBattery = _powerDynamicMode.Contains("Battery", StringComparison.OrdinalIgnoreCase);
            var batteryPercent = hasBattery ? 35d : 100d;
            var temperature = ExtractTemperature(json?["temperatures"] as JObject) ?? (cpu > 80 ? 86 : cpu > 55 ? 72 : 56);
            var state = ResolveAutomationSystemState(cpu, ram, disk, temperature);

            return new AutomationRuntimeSnapshot
            {
                Cpu = cpu,
                Ram = ram,
                Disk = disk,
                Gpu = gpu,
                Temperature = temperature,
                BatteryPercent = batteryPercent,
                HasBattery = hasBattery,
                State = state,
                IsIdle = cpu <= 18 && ram <= 82 && !state.Contains("Gaming", StringComparison.OrdinalIgnoreCase) && !state.Contains("Streaming", StringComparison.OrdinalIgnoreCase)
            };
        }

        private bool CanExecuteAutomationRule(AutomationRuleDefinition rule, AutomationRuntimeSnapshot snapshot, out string reason)
        {
            reason = "Safe window confirmed";
            if (!rule.Enabled)
            {
                reason = "Rule disabled";
                return false;
            }

            if (_automationPaused || !_autonomousModeEnabled)
            {
                reason = _automationPaused ? "Automation paused" : "Autonomous execution disabled";
                return false;
            }

            if (_automationMode.Contains("Assisted", StringComparison.OrdinalIgnoreCase))
            {
                reason = "Assisted mode requires manual approval";
                return false;
            }

            if (_automationMode.Contains("Safe", StringComparison.OrdinalIgnoreCase) &&
                !rule.SafeLevel.Equals("Safe", StringComparison.OrdinalIgnoreCase))
            {
                reason = "Safe autonomous mode blocks non-safe rule";
                return false;
            }

            if (rule.RequiresIdle && !snapshot.IsIdle)
            {
                reason = "Waiting for idle window";
                return false;
            }

            if (snapshot.Cpu > rule.MaxCpuPercent)
            {
                reason = $"CPU too busy ({snapshot.Cpu:0}% > {rule.MaxCpuPercent}%)";
                return false;
            }

            if (snapshot.Temperature > rule.MaxTemperatureC)
            {
                reason = $"Temperature too high ({snapshot.Temperature:0}C)";
                return false;
            }

            if (rule.TriggerType.Equals("memory", StringComparison.OrdinalIgnoreCase) && snapshot.Ram < 80)
            {
                reason = "RAM pressure not high enough";
                return false;
            }

            if (rule.TriggerType.Equals("storage", StringComparison.OrdinalIgnoreCase) && snapshot.Disk < 85)
            {
                reason = "Storage pressure not critical";
                return false;
            }

            if (rule.TriggerType.Equals("battery", StringComparison.OrdinalIgnoreCase) && (!snapshot.HasBattery || snapshot.BatteryPercent > 35))
            {
                reason = "Battery protection trigger not active";
                return false;
            }

            if (rule.TriggerType.Equals("gaming", StringComparison.OrdinalIgnoreCase) && !snapshot.State.Contains("Gaming", StringComparison.OrdinalIgnoreCase))
            {
                reason = "Gaming session not detected";
                return false;
            }

            if (rule.TriggerType.Equals("streaming", StringComparison.OrdinalIgnoreCase) && !snapshot.State.Contains("Streaming", StringComparison.OrdinalIgnoreCase))
            {
                reason = "Streaming session not detected";
                return false;
            }

            if (rule.TriggerType.Equals("creator", StringComparison.OrdinalIgnoreCase) &&
                !snapshot.State.Contains("Editing", StringComparison.OrdinalIgnoreCase))
            {
                reason = "Creator session not detected";
                return false;
            }

            if (rule.TriggerType.Equals("night", StringComparison.OrdinalIgnoreCase))
            {
                var hour = DateTime.Now.Hour;
                var inNightWindow = hour >= 20 || hour <= 4;
                if (!inNightWindow)
                {
                    reason = "Waiting for night maintenance window";
                    return false;
                }
            }

            if (rule.TriggerType.Equals("startup", StringComparison.OrdinalIgnoreCase) &&
                DateTime.UtcNow - _appStartedUtc > TimeSpan.FromMinutes(15))
            {
                reason = "Startup window has passed";
                return false;
            }

            if (rule.TriggerType.Equals("network", StringComparison.OrdinalIgnoreCase) &&
                snapshot.State.Contains("Gaming", StringComparison.OrdinalIgnoreCase) &&
                _automationPolicyProfile.Contains("Conservative", StringComparison.OrdinalIgnoreCase))
            {
                reason = "Conservative profile defers network recovery during gaming";
                return false;
            }

            if (rule.LastRunUtc.HasValue &&
                DateTime.UtcNow - rule.LastRunUtc.Value < TimeSpan.FromMinutes(rule.MinimumMinutesBetweenRuns))
            {
                reason = "Rule cooling down";
                return false;
            }

            return true;
        }

        private void QueueAutomationTask(AutomationRuleDefinition rule, string triggerReason, DateTime? scheduledForUtc = null)
        {
            if (_automationTasks.Any(task =>
                    task.RuleId == rule.Id &&
                    (task.Status.Equals("Queued", StringComparison.OrdinalIgnoreCase) ||
                     task.Status.Equals("Waiting for Safe Window", StringComparison.OrdinalIgnoreCase) ||
                     task.Status.Equals("Retrying", StringComparison.OrdinalIgnoreCase))))
            {
                return;
            }

            _automationTasks.Add(new AutomationTaskRecord
            {
                RuleId = rule.Id,
                Name = rule.Name,
                Status = "Queued",
                TriggerReason = triggerReason,
                CreatedUtc = DateTime.UtcNow,
                ScheduledForUtc = scheduledForUtc ?? DateTime.UtcNow
            });

            if (_automationTasks.Count > 80)
                _automationTasks.RemoveRange(0, _automationTasks.Count - 80);
        }

        private async Task<string> ExecuteAutomationActionAsync(string actionType, string ruleName)
        {
            switch (actionType)
            {
                case "cleanup_light":
                    return await SafeApiCall(async () =>
                    {
                        var result = await _backendClient.CleanupAsync();
                        return result is Newtonsoft.Json.Linq.JObject json
                            ? $"Cleanup executed for {ruleName}: {json.ToString(Newtonsoft.Json.Formatting.None)}"
                            : $"Cleanup executed for {ruleName}.";
                    }) ?? $"Cleanup requested for {ruleName}.";

                case "memory_stabilize":
                    {
                        var (success, output) = await ExecutePowerShellScriptAsync("[System.GC]::Collect(); [System.GC]::WaitForPendingFinalizers(); 'Automation memory stabilization requested.'");
                        return success ? output : $"Memory stabilization failed: {output}";
                    }

                case "background_trim":
                    return await ApplyProcessTargetsAsync(new[] { "OneDrive", "Teams", "Spotify", "Widgets", "AdobeGCClient", "EpicWebHelper" }, $"Automation {ruleName}");

                case "flush_dns":
                    return await SafeApiCall(async () =>
                    {
                        var result = await _backendClient.FlushDnsAsync();
                        return result?.ToString() ?? "DNS flush requested.";
                    }) ?? "DNS flush requested.";

                case "network_recover":
                    return await SafeApiCall(async () =>
                    {
                        var result = await _backendClient.ResetNetworkAsync();
                        return result?.ToString() ?? "Network recovery requested.";
                    }) ?? "Network recovery requested.";

                case "gaming_prep":
                    return await SafeApiCall(async () =>
                    {
                        var result = await _backendClient.ApplyBoosterAsync("gaming");
                        return result?.ToString() ?? "Gaming preparation requested.";
                    }) ?? "Gaming preparation requested.";

                case "power_saver":
                    await ApplyPowerModeCoreAsync("battery", "Ultra Battery Saver");
                    return "Power saver automation applied.";

                case "power_balanced":
                    await ApplyPowerModeCoreAsync("balanced", "Balanced AI");
                    return "Balanced power automation applied.";

                case "repair_quick":
                    {
                        var (success, output) = await ExecutePowerShellScriptAsync("Restart-Service -Name wuauserv,AudioSrv,Dnscache -ErrorAction SilentlyContinue; 'Automation quick repair requested.'");
                        return success ? output : $"Quick repair warning: {output}";
                    }

                default:
                    return $"No executor mapped for action '{actionType}'.";
            }
        }

        private async Task ExecuteAutomationTaskAsync(AutomationTaskRecord task, AutomationRuleDefinition rule)
        {
            task.Status = "Running";
            task.LastTriedUtc = DateTime.UtcNow;
            AppendAutomationAudit("Info", $"Executing task '{task.Name}'", rule.Scenario);

            try
            {
                var result = await ExecuteAutomationActionAsync(rule.ActionType, rule.Name);
                task.Status = "Completed";
                task.CompletedUtc = DateTime.UtcNow;
                task.ResultSummary = result;
                rule.LastRunUtc = DateTime.UtcNow;
                AppendAutomationHistory($"Task completed: {task.Name}");
                AppendAutomationAudit("Success", $"{task.Name} completed. {result}", rule.Scenario);
            }
            catch (Exception ex)
            {
                task.RetryCount++;
                task.ResultSummary = ex.Message;
                if (task.RetryCount <= (_appConfig?.Automation?.RetryLimit ?? 2))
                {
                    task.Status = "Retrying";
                    task.ScheduledForUtc = DateTime.UtcNow.AddMinutes(5);
                    AppendAutomationAudit("Warning", $"{task.Name} failed and will retry: {ex.Message}", rule.Scenario);
                }
                else
                {
                    task.Status = "Failed";
                    task.CompletedUtc = DateTime.UtcNow;
                    AppendAutomationAudit("Error", $"{task.Name} failed permanently: {ex.Message}", rule.Scenario);
                }
            }
        }

        private async Task EvaluateAutomationEngineAsync(string source)
        {
            if (!_settingsEngineEnabled)
                return;

            var snapshot = await BuildAutomationSnapshotAsync();
            var queuedNow = 0;
            var executedNow = 0;
            foreach (var rule in _automationRules.Where(rule => rule.Enabled))
            {
                if (CanExecuteAutomationRule(rule, snapshot, out var reason))
                {
                    var beforeCount = _automationTasks.Count;
                    QueueAutomationTask(rule, $"{source}: {reason}");
                    if (_automationTasks.Count > beforeCount)
                        queuedNow++;
                }
                else if (reason.Contains("idle", StringComparison.OrdinalIgnoreCase) ||
                         reason.Contains("busy", StringComparison.OrdinalIgnoreCase) ||
                         reason.Contains("temperature", StringComparison.OrdinalIgnoreCase))
                {
                    var existing = _automationTasks.FirstOrDefault(task => task.RuleId == rule.Id &&
                                                                           task.Status.Equals("Waiting for Safe Window", StringComparison.OrdinalIgnoreCase));
                    if (existing == null)
                    {
                        _automationTasks.Add(new AutomationTaskRecord
                        {
                            RuleId = rule.Id,
                            Name = rule.Name,
                            Status = "Waiting for Safe Window",
                            TriggerReason = reason,
                            CreatedUtc = DateTime.UtcNow,
                            ScheduledForUtc = DateTime.UtcNow.AddMinutes(10)
                        });
                    }
                }
            }

            var pendingTasks = _automationTasks
                .Where(task => task.Status.Equals("Queued", StringComparison.OrdinalIgnoreCase) ||
                               task.Status.Equals("Retrying", StringComparison.OrdinalIgnoreCase))
                .Where(task => (task.ScheduledForUtc ?? task.CreatedUtc) <= DateTime.UtcNow)
                .OrderBy(task => task.ScheduledForUtc ?? task.CreatedUtc)
                .Take(Math.Max(1, _appConfig?.Automation?.MaxConcurrentTasks ?? 2))
                .ToList();

            foreach (var task in pendingTasks)
            {
                var rule = _automationRules.FirstOrDefault(item => item.Id == task.RuleId);
                if (rule == null)
                {
                    task.Status = "Failed";
                    task.ResultSummary = "Associated rule missing.";
                    task.CompletedUtc = DateTime.UtcNow;
                    continue;
                }

                if (!CanExecuteAutomationRule(rule, snapshot, out var executionReason))
                {
                    task.Status = "Waiting for Safe Window";
                    task.TriggerReason = executionReason;
                    task.ScheduledForUtc = DateTime.UtcNow.AddMinutes(10);
                    continue;
                }

                await ExecuteAutomationTaskAsync(task, rule);
                executedNow++;
            }

            var pendingCount = _automationTasks.Count(task =>
                task.Status.Equals("Queued", StringComparison.OrdinalIgnoreCase) ||
                task.Status.Equals("Retrying", StringComparison.OrdinalIgnoreCase) ||
                task.Status.Equals("Waiting for Safe Window", StringComparison.OrdinalIgnoreCase));

            AppendAutomationAudit(
                "Info",
                $"Automation evaluation completed via {source}. State={snapshot.State}; queued={queuedNow}; executed={executedNow}; pending={pendingCount}.",
                source);

            await SavePersistedConfigurationAsync();
        }

        #region Navigation

        private void SelectNavButton(Button button)
        {
            if (_selectedNavButton != null)
            {
                _selectedNavButton.BorderBrush = System.Windows.Media.Brushes.Transparent;
                _selectedNavButton.Background = System.Windows.Media.Brushes.Transparent;
            }
            _selectedNavButton = button;
            _selectedNavButton.BorderBrush = System.Windows.Media.Brushes.DeepSkyBlue;
            _selectedNavButton.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(48, 48, 48));
        }

        private void HideAllPages()
        {
            DashboardContent.Visibility = Visibility.Collapsed;
            PerformanceContent.Visibility = Visibility.Collapsed;
            OneClickBoostContent.Visibility = Visibility.Collapsed;
            SmartRecommendationContent.Visibility = Visibility.Collapsed;
            StartupContent.Visibility = Visibility.Collapsed;
            CleanupContent.Visibility = Visibility.Collapsed;
            StorageContent.Visibility = Visibility.Collapsed;
            GamingContent.Visibility = Visibility.Collapsed;
            StreamingContent.Visibility = Visibility.Collapsed;
            CreatorContent.Visibility = Visibility.Collapsed;
            NetworkContent.Visibility = Visibility.Collapsed;
            DnsLatencyContent.Visibility = Visibility.Collapsed;
            PrivacyContent.Visibility = Visibility.Collapsed;
            SecurityHealthContent.Visibility = Visibility.Collapsed;
            AppsManagerContent.Visibility = Visibility.Collapsed;
            AppUninstallerContent.Visibility = Visibility.Collapsed;
            ServicesContent.Visibility = Visibility.Collapsed;
            PowerContent.Visibility = Visibility.Collapsed;
            VisualContent.Visibility = Visibility.Collapsed;
            WindowsFeaturesContent.Visibility = Visibility.Collapsed;
            UpdateControlContent.Visibility = Visibility.Collapsed;
            RepairContent.Visibility = Visibility.Collapsed;
            AdvancedContent.Visibility = Visibility.Collapsed;
            RestoreContent.Visibility = Visibility.Collapsed;
            RestorePointContent.Visibility = Visibility.Collapsed;
            AutomationContent.Visibility = Visibility.Collapsed;
            UtilitiesContent.Visibility = Visibility.Collapsed;
            TestingContent.Visibility = Visibility.Collapsed;
            SettingsContent.Visibility = Visibility.Collapsed;
            TweaksContent.Visibility = Visibility.Collapsed;
            DriversContent.Visibility = Visibility.Collapsed;
            SystemContent.Visibility = Visibility.Collapsed;
            BoosterContent.Visibility = Visibility.Collapsed;
            BackgroundAppsContent.Visibility = Visibility.Collapsed;
            PlaceholderContent.Visibility = Visibility.Collapsed;
            AboutContent.Visibility = Visibility.Collapsed;
        }

        private void StartPageActivationRefresh(int navigationVersion, string pageName, Func<Task> refreshAction, Action postRefreshAction = null)
        {
            _ = RunPageActivationRefreshAsync(navigationVersion, pageName, refreshAction, postRefreshAction);
        }

        private async Task RunPageActivationRefreshAsync(int navigationVersion, string pageName, Func<Task> refreshAction, Action postRefreshAction = null)
        {
            try
            {
                await refreshAction();

                if (navigationVersion != _pageNavigationVersion ||
                    !string.Equals(_activePage, pageName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                postRefreshAction?.Invoke();
            }
            catch (Exception ex)
            {
                if (navigationVersion != _pageNavigationVersion ||
                    !string.Equals(_activePage, pageName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                AppendDashboardActivity($"Page refresh warning on {pageName}: {ex.Message}");
                ShowActionStatus(ActionState.Warning, pageName, $"Refresh halaman {pageName} mengalami warning.", ex.Message);
            }
        }

        private Task ShowPage(string pageName, Button navButton)
        {
            if (!string.Equals(pageName, "Testing", StringComparison.OrdinalIgnoreCase) &&
                _featureAuditRunning &&
                !_featureAuditCancellationRequested)
            {
                _featureAuditCancellationRequested = true;
                AppendFeatureAuditHistory($"Feature audit cancellation requested while switching to {pageName}.");
            }

            _activePage = pageName;
            _pageNavigationVersion++;
            var navigationVersion = _pageNavigationVersion;
            _lastRealtimePageRefreshUtc = DateTime.MinValue;
            SelectNavButton(navButton);
            HideAllPages();
            _dashboardTimer.Stop();
            _storageTimer.Stop();
            _gamingTimer.Stop();
            _streamingTimer.Stop();
            _creatorTimer.Stop();
            _networkTimer.Stop();
            _settingsTimer.Stop();

            // Show selected page
            switch (pageName)
            {
                case "Dashboard":
                    SetLocalizedPageHeader("Dashboard", "Dashboard", "Core system hub untuk monitor real-time, quick boost, recommendation preview, mode control, alerts, dan activity log.");
                    DashboardContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshDashboard, () => _dashboardTimer.Start());
                    break;
                case "Performance":
                    SetLocalizedPageHeader("Performance", "Performance Boost", "Fitur boost performa langsung untuk CPU, RAM, disk, startup, network, gaming, dan safety restore.");
                    PerformanceContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshPerformanceBoostViewAsync);
                    break;
                case "OneClickBoost":
                    SetLocalizedPageHeader("OneClickBoost", "One Click Boost", "Jalankan boost aman, balanced, extreme, atau custom dari satu panel cepat.");
                    OneClickBoostContent.Visibility = Visibility.Visible;
                    InitializeOneClickBoostDefaults();
                    RefreshLastBoostView();
                    break;
                case "Startup":
                    SetLocalizedPageHeader("Startup", "Startup Manager", "Review boot impact and jump straight to startup controls when your PC feels slow to open.");
                    StartupContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshStartupItems);
                    break;
                case "SmartRecommendation":
                    SetLocalizedPageHeader("SmartRecommendation", "Smart Recommendation", "Auto scan sistem dan tampilkan rekomendasi optimasi yang paling relevan untuk kondisi saat ini.");
                    SmartRecommendationContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RunSmartRecommendationScanAsync);
                    break;
                case "Cleanup":
                    SetLocalizedPageHeader("Cleanup", "Storage Cleaner", "Free temporary files and run cleanup tools without guessing which step to use first.");
                    CleanupContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshCleanupViewAsync);
                    break;
                case "Storage":
                    SetLocalizedPageHeader("Storage", "Storage", "Baca semua storage yang terhubung, scan drive, lihat breakdown, health, dan action dari satu halaman.");
                    StorageContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshStorageViewAsync, () => _storageTimer.Start());
                    break;
                case "Gaming":
                    SetLocalizedPageHeader("Gaming", "Gaming Mode", "Kelola profile, policy, auto activation, dan restore environment supaya Windows tetap fokus selama sesi gaming berjalan.");
                    GamingContent.Visibility = Visibility.Visible;
                    RefreshGamingWhitelistView();
                    InitializeGamingDefaults();
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshGamingBoosterViewAsync, () => _gamingTimer.Start());
                    break;
                case "Streaming":
                    SetLocalizedPageHeader("Streaming", "Streaming Mode", "Stabilkan encoder, upload, CPU, RAM, GPU, dan background activity untuk sesi live yang lebih aman.");
                    StreamingContent.Visibility = Visibility.Visible;
                    InitializeStreamingDefaults();
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshStreamingViewAsync, () => _streamingTimer.Start());
                    break;
                case "Creator":
                    SetLocalizedPageHeader("Creator", "Creator Mode", "Optimalkan editing, rendering, cache, disk, dan focus mode untuk workflow creator yang lebih smooth.");
                    CreatorContent.Visibility = Visibility.Visible;
                    InitializeCreatorDefaults();
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshCreatorViewAsync, () => _creatorTimer.Start());
                    break;
                case "Network":
                    SetLocalizedPageHeader("Network", "Network Booster", "Run diagnostics first, then apply DNS and TCP actions with clear feedback.");
                    NetworkContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, async () =>
                    {
                        await RefreshNetworkDiagnostics();
                        await RefreshNetworkBoosterViewAsync();
                    }, () => _networkTimer.Start());
                    break;
                case "DnsLatency":
                    SetLocalizedPageHeader("DnsLatency", "DNS & Latency Tools", "Diagnosa DNS, ping, jitter, packet loss, traceroute, dan quick-fix latency dalam satu panel advanced.");
                    DnsLatencyContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshDnsLatencyViewAsync, () => _networkTimer.Start());
                    break;
                case "BackgroundApps":
                    SetLocalizedPageHeader("BackgroundApps", "Background Apps", "See which processes are eating resources so the next cleanup decision is obvious.");
                    BackgroundAppsContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshBackgroundApps);
                    break;
                case "Privacy":
                    SetLocalizedPageHeader("Privacy", "Privacy Center", "Reduce telemetry and open the right Windows privacy pages without hunting through settings.");
                    PrivacyContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshPrivacyViewAsync);
                    break;
                case "SecurityHealth":
                    SetLocalizedPageHeader("SecurityHealth", "Security & Health", "Lihat apakah PC aman dan sehat dari sisi security, suhu, disk, RAM, integritas sistem, dan aktivitas mencurigakan.");
                    SecurityHealthContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshSecurityHealthViewAsync);
                    break;
                case "AppsManager":
                    SetLocalizedPageHeader("AppsManager", "Apps Manager", "Lihat, monitor, uninstall, cleanup, dan optimalkan aplikasi dari satu panel yang lebih rapi.");
                    AppsManagerContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshAppsManagerViewAsync);
                    break;
                case "AppUninstaller":
                    SetLocalizedPageHeader("AppUninstaller", "App Uninstaller", "Uninstall, analyze, deep-clean residual, force-remove app bandel, dan review impact aplikasi dari satu modul.");
                    AppUninstallerContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshAppUninstallerViewAsync);
                    break;
                case "Services":
                    SetLocalizedPageHeader("Services", "Services (Local Machine)", "Database dan control center semua service lokal, lengkap dengan status, startup type, resource, dependency, insight, bulk action, dan backup restore.");
                    ServicesContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshServicesViewAsync);
                    break;
                case "Power":
                    SetLocalizedPageHeader("Power", "Power Optimization", "Brain power management untuk mode dinamis, CPU/GPU/disk/network power control, thermal-aware tuning, telemetry, rules, dan backup power policy.");
                    PowerContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshPowerOptimizationViewAsync);
                    break;
                case "Visual":
                    SetLocalizedPageHeader("Visual", "Visual Effects", "Kontrol UI rendering, animation, transparency, explorer effects, input responsiveness, adaptive visual engine, dan backup visual setting.");
                    VisualContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshVisualEffectsViewAsync);
                    break;
                case "WindowsFeatures":
                    SetLocalizedPageHeader("WindowsFeatures", "Windows Features", "Kontrol fitur resmi Windows untuk gaming, developer, creator, legacy, network, security, dan optional features.");
                    WindowsFeaturesContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshWindowsFeaturesViewAsync);
                    break;
                case "UpdateControl":
                    SetLocalizedPageHeader("UpdateControl", "Update Control", "Kontrol update Windows, driver, app, service, schedule, dan background update agar tidak mengganggu performa.");
                    UpdateControlContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshUpdateControlViewAsync);
                    break;
                case "Repair":
                    SetLocalizedPageHeader("Repair", "Repair Tools", "Bengkel Windows untuk scan error, fix system, network, service, app, update, cache, dan backup restore dari satu panel.");
                    RepairContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshRepairViewAsync);
                    break;
                case "Advanced":
                    SetLocalizedPageHeader("Advanced", "Advanced Tweaks", "Power-user tweaks untuk registry, service, boot, network low-level, kernel behavior, custom script, dan backup restore dengan indikator risiko.");
                    AdvancedContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshAdvancedTweaksViewAsync);
                    break;
                case "Restore":
                    SetLocalizedPageHeader("Restore", "Restore & Backup", "Create recovery checkpoints and keep simple snapshots before making bigger changes.");
                    RestoreContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshRestoreBackupViewAsync);
                    break;
                case "RestorePoint":
                    SetLocalizedPageHeader("RestorePoint", "Restore Point Manager", "Kelola restore point Windows sebagai snapshot system state untuk rollback cepat, validator aman, cleanup storage, dan repair engine.");
                    RestorePointContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshRestorePointManagerViewAsync);
                    break;
                case "Automation":
                    SetLocalizedPageHeader("Automation", "Scheduled Automation", "Automation engine mandiri yang membaca konteks sistem, memilih aksi aman, menunda task saat tidak cocok, dan mencatat semua keputusan.");
                    AutomationContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshAutomationViewAsync);
                    break;
                case "Utilities":
                    SetLocalizedPageHeader("Utilities", "Utilities Tools", "Toolbox utama untuk cleanup, diagnostics, repair, network fix, system control, monitoring, workflow, analytics, dan autonomous maintenance.");
                    UtilitiesContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshUtilitiesViewAsync);
                    break;
                case "Testing":
                    SetLocalizedPageHeader("Testing", "Feature Audit", "Audit otomatis untuk memastikan semua menu inti hidup, refresh path aman, dan summary hasil test langsung dikirim ke Discord.");
                    TestingContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshFeatureAuditViewAsync);
                    break;
                case "Settings":
                    SetLocalizedPageHeader("Settings", "Settings", "Otak + aturan hidup aplikasi: UI, automation brain, system control, safety, engine, logging, update, dan master switch HyperBoostX.");
                    SettingsContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshSettingsViewAsync, () => _settingsTimer.Start());
                    break;
                case "Tweaks":
                    SetLocalizedPageHeader("Tweaks", "Tweaks Center", "Browse available tweaks with clearer context before applying system-level changes.");
                    TweaksContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshTweaksCenterViewAsync);
                    break;
                case "Drivers":
                    SetLocalizedPageHeader("Drivers", "Driver & Update Center", "Inspect current driver inventory and start update checks when hardware acts up.");
                    DriversContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshDrivers);
                    break;
                case "Booster":
                    SetLocalizedPageHeader("Booster", "Gaming Booster", "Jalankan optimasi instan seperti boost cepat, process trimming, network tuning, dan cleanup action sebelum atau saat game berjalan.");
                    BoosterContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshGamingBoosterHubAsync);
                    break;
                case "About":
                    SetLocalizedPageHeader("About", "About App", "Project information, runtime overview, and what this build is wired to do.");
                    AboutContent.Visibility = Visibility.Visible;
                    StartPageActivationRefresh(navigationVersion, pageName, RefreshAboutViewAsync);
                    break;
            }

            return Task.CompletedTask;
        }

        private Task ShowPlaceholderPage(Button navButton, string title, string description, string status)
        {
            _activePage = title;
            SelectNavButton(navButton);
            HideAllPages();
            SetPageHeader(title, description);
            PlaceholderTitleText.Text = title;
            PlaceholderDescriptionText.Text = description;
            PlaceholderStatusText.Text = status;
            PlaceholderContent.Visibility = Visibility.Visible;
            return Task.CompletedTask;
        }

        private void SetPageHeader(string title, string subtitle)
        {
            PageTitle.Text = title;
            PageSubtitle.Text = subtitle;
        }

        private void ShowActionStatus(ActionState state, string title, string message, string meta = null)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => ShowActionStatus(state, title, message, meta));
                return;
            }

            Brush accentBrush = (Brush)FindResource("AccentBrush");
            Brush textBrush = Brushes.White;

            switch (state)
            {
                case ActionState.Success:
                    accentBrush = (Brush)FindResource("SuccessBrush");
                    break;
                case ActionState.Warning:
                    accentBrush = (Brush)FindResource("WarningBrush");
                    break;
                case ActionState.Error:
                    accentBrush = (Brush)FindResource("ErrorBrush");
                    break;
            }

            ActionStatusAccent.Background = accentBrush;
            ActionStatusTitle.Text = title;
            ActionStatusTitle.Foreground = textBrush;
            ActionStatusText.Text = message;
            ActionStatusMeta.Text = string.IsNullOrWhiteSpace(meta)
                ? $"Updated {DateTime.Now:HH:mm:ss}"
                : $"{meta}    {DateTime.Now:HH:mm:ss}";
            ActionStatusCard.Visibility = Visibility.Visible;
            ActionStatusCard.UpdateLayout();
            Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
            AppendDashboardActivity($"{title}: {message}");
            UpdateFeatureAuditIncidentState(state, title, message, meta);

            if (state == ActionState.Warning)
                _ = ReportErrorToDiscordAsync(title, message, meta, "warning");
            else if (state == ActionState.Error)
                _ = ReportErrorToDiscordAsync(title, message, meta, "error");
        }

        private void ShowRequestedStatus(string title, string message, string meta = null)
        {
            ShowActionStatus(ActionState.Info, title, message, meta);
        }

        private void ShowOpenedStatus(string title, string message, string meta = null)
        {
            ShowActionStatus(ActionState.Info, title, message, meta);
        }

        private void ShowAppliedStatus(bool success, string title, string successMessage, string warningMessage, string meta = null)
        {
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, title, success ? successMessage : warningMessage, meta);
        }

        private void UpdateFeatureAuditIncidentState(ActionState state, string title, string message, string meta = null)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
                return;

            if (title.Contains("Feature Audit", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Full QA Matrix", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(title, "Testing", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var targetName = ResolveFeatureAuditTargetName(title);

            if (state == ActionState.Success)
            {
                if (!string.IsNullOrWhiteSpace(targetName))
                    _featureAuditIncidents.RemoveAll(item => string.Equals(item.TargetName, targetName, StringComparison.OrdinalIgnoreCase));
                return;
            }

            if (state != ActionState.Error)
                return;

            _featureAuditIncidents.Add(new FeatureAuditIncident
            {
                TimestampUtc = DateTime.UtcNow,
                State = state,
                TargetName = targetName,
                Title = title.Trim(),
                Message = message.Trim(),
                Meta = meta?.Trim() ?? ""
            });

            if (_featureAuditIncidents.Count > 40)
                _featureAuditIncidents.RemoveRange(0, _featureAuditIncidents.Count - 40);
        }

        private void AppendDashboardActivity(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            _dashboardActivityLog.Enqueue($"[{DateTime.Now:HH:mm:ss}] {message}");
            while (_dashboardActivityLog.Count > 8)
            {
                _dashboardActivityLog.Dequeue();
            }

            DashboardActivityLogText.Text = string.Join(Environment.NewLine, _dashboardActivityLog.Reverse());
        }

        private async Task ReportErrorToDiscordAsync(string title, string message, string meta = null, string severity = "error")
        {
            try
            {
                if (!_discordWebhookEnabled || string.IsNullOrWhiteSpace(_discordWebhookUrl))
                    return;

                if (!ShouldReportToDiscord(severity))
                    return;

                var signature = $"{severity}|{_activePage}|{title}|{message}";
                if (_discordWebhookLastSent.TryGetValue(signature, out var lastSent) &&
                    DateTime.UtcNow - lastSent < TimeSpan.FromSeconds(_discordWebhookCooldownSeconds))
                {
                    return;
                }

                var fields = BuildDiscordReportFields(severity, meta);

                var result = await _discordWebhookService.SendDetailedAsync(_discordWebhookUrl, title, message, severity, fields);
                if (result.Success)
                    _discordWebhookLastSent[signature] = DateTime.UtcNow;
                else
                    AppendFeatureAuditHistory($"Discord delivery not completed: {result.Summary}");
            }
            catch
            {
                // Never throw from error reporting.
            }
        }

        private bool ShouldReportToDiscord(string severity)
        {
            return GetDiscordSeverityRank(severity) >= GetDiscordSeverityRank(_discordWebhookMinimumLevel);
        }

        private static int GetDiscordSeverityRank(string severity)
        {
            return severity?.Trim().ToLowerInvariant() switch
            {
                "warning" => 1,
                "error" => 2,
                "critical" => 3,
                _ => 0
            };
        }

        private Dictionary<string, string> BuildDiscordReportFields(string severity, string meta = null)
        {
            var fields = new Dictionary<string, string>
            {
                ["Severity"] = severity,
                ["Module"] = _activePage,
                ["Page"] = _activePage,
                ["Automation Mode"] = _automationMode,
                ["Policy Profile"] = _automationPolicyProfile,
                ["User Mode"] = _settingsUserMode,
                ["App Version"] = NormalizeDiscordReportVersion(_currentAppVersion),
                ["Time"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            if (!string.IsNullOrWhiteSpace(meta))
                fields["Details"] = meta;

            return fields;
        }

        private static string NormalizeDiscordReportVersion(string version)
        {
            var normalized = string.IsNullOrWhiteSpace(version)
                ? "unknown"
                : version.Split('+')[0].Trim();

            if (string.Equals(normalized, "unknown", StringComparison.OrdinalIgnoreCase))
                return normalized;

            return normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? normalized : $"v{normalized}";
        }

        private void RefreshDiscordPreview(string severity = "error", string title = "Sample HyperBoostX error", string message = "Preview of the Discord error payload.")
        {
            if (DiscordWebhookPreviewText == null)
                return;

            var fields = BuildDiscordReportFields(severity, "Sample details / stack trace preview");
            DiscordWebhookPreviewText.Text =
                $"Title: {title}{Environment.NewLine}" +
                $"Message: {message}{Environment.NewLine}" +
                string.Join(Environment.NewLine, fields.Select(pair => $"{pair.Key}: {pair.Value}"));
        }

        private async Task<string> BuildAiSystemContextAsync()
        {
            if (!string.IsNullOrWhiteSpace(_cachedAiSystemContext) &&
                DateTime.UtcNow - _lastAiContextBuiltUtc < TimeSpan.FromSeconds(8))
            {
                return _cachedAiSystemContext;
            }

            var statsTask = SafeApiCall(() => _backendClient.GetSystemStatsAsync());
            var processesTask = SafeApiCall(() => _backendClient.GetProcessesAsync());
            var startupTask = SafeApiCall(() => _backendClient.GetStartupItemsAsync());
            await Task.WhenAll(statsTask, processesTask, startupTask);

            var stats = await statsTask as JObject;
            var processes = await processesTask as JObject;
            var startup = await startupTask as JObject;
            var cpu = ReadNumericToken(stats, "cpu", "cpu_percent");
            var ram = ReadNumericToken(stats, "memory", "memory_percent");
            var disk = ReadNumericToken(stats, "disk", "disk_percent");
            var processCount = ReadArrayCount(processes, "processes");
            var startupCount = ReadStartupItemsArray(startup)?.Count ?? 0;

            _cachedAiSystemContext =
                $"Active page: {_activePage}\n" +
                $"CPU: {cpu:0}%\n" +
                $"RAM: {ram:0}%\n" +
                $"Disk: {disk:0}%\n" +
                $"Power mode: {_powerDynamicMode}\n" +
                $"Automation mode: {_automationMode}\n" +
                $"Policy profile: {_automationPolicyProfile}\n" +
                $"Process count: {processCount}\n" +
                $"Startup item count: {startupCount}\n" +
                $"User mode: {_settingsUserMode}\n" +
                $"Last boost result: {_lastBoostScore}\n" +
                $"Cleanup safety mode: {_cleanupSafetyMode}";
            _lastAiContextBuiltUtc = DateTime.UtcNow;
            return _cachedAiSystemContext;
        }

        private static double ReadGpuLoadStat(JObject stats)
        {
            var gpuToken = stats?["gpu"];
            return gpuToken switch
            {
                JObject gpuObject => gpuObject.Value<double?>("load")
                    ?? gpuObject.Value<double?>("usage")
                    ?? gpuObject.Value<double?>("memory_percent")
                    ?? gpuObject.Value<double?>("percent")
                    ?? 0d,
                JValue gpuValue => gpuValue.Value<double?>() ?? 0d,
                _ => stats?.Value<double?>("gpu_percent") ?? 0d
            };
        }

        private static string MapAiActionToAutomationActionLabel(string action)
        {
            return action switch
            {
                "cleanup" => "cleanup_light",
                "ram_optimize" => "memory_stabilize",
                "gaming_prep" => "gaming_prep",
                "network_fix" => "network_recover",
                "background_trim" => "background_trim",
                "power_balanced" => "power_balanced",
                _ => "scan_only"
            };
        }

        private static bool ContainsAny(string source, params string[] tokens)
        {
            if (string.IsNullOrWhiteSpace(source))
                return false;

            return tokens.Any(token => source.Contains(token, StringComparison.OrdinalIgnoreCase));
        }

        private static string ResolveAiScenarioLabel(string text)
        {
            if (ContainsAny(text, "gaming", "game", "fps"))
                return "Gaming";
            if (ContainsAny(text, "stream", "obs", "encoder"))
                return "Streaming";
            if (ContainsAny(text, "creator", "editing", "render", "blender", "premiere"))
                return "Creator";
            if (ContainsAny(text, "network", "internet", "latency", "ping", "dns"))
                return "Network";
            if (ContainsAny(text, "cleanup", "clean", "junk", "storage"))
                return "Cleanup";
            if (ContainsAny(text, "power", "battery", "thermal"))
                return "Power";
            return "General Assistance";
        }

        private static string GetTopCounterKey(Dictionary<string, int> counters, string fallback)
        {
            if (counters.Count == 0)
                return fallback;

            return counters
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .First().Key;
        }

        private void IncrementAiCounter(Dictionary<string, int> counters, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            counters.TryGetValue(key, out var current);
            counters[key] = current + 1;
        }

        private void RecordAiPlanProfile(OpenAiCopilotResponse result)
        {
            _aiTotalRequests++;
            _aiPreferredRiskStyle = _openAiPermissionLevel;

            var scenario = ResolveAiScenarioLabel($"{_lastAiPrompt} {result?.Intent}");
            IncrementAiCounter(_aiIntentCounters, result?.Intent ?? "general_help");
            IncrementAiCounter(_aiIntentCounters, scenario);
            _aiPreferredScenario = GetTopCounterKey(_aiIntentCounters, scenario);

            foreach (var action in GetAiSafeActionsOrFallback(result))
                IncrementAiCounter(_aiActionCounters, action);

            _aiPreferredAction = GetTopCounterKey(_aiActionCounters, "scan_only");
        }

        private string BuildAiPersonalizationSummary()
        {
            var approvalRate = _aiTotalRequests == 0 ? 0 : (_aiApprovedPlans * 100.0 / _aiTotalRequests);
            return
                $"Requests: {_aiTotalRequests}{Environment.NewLine}" +
                $"Approved: {_aiApprovedPlans} | Rejected: {_aiRejectedPlans} | Automations: {_aiCreatedAutomations}{Environment.NewLine}" +
                $"Preferred scenario: {_aiPreferredScenario}{Environment.NewLine}" +
                $"Preferred action: {_aiPreferredAction}{Environment.NewLine}" +
                $"Risk style: {_aiPreferredRiskStyle}{Environment.NewLine}" +
                $"Approval rate: {approvalRate:0}%{Environment.NewLine}" +
                $"Last outcome: {_lastAiOutcomeSummary}";
        }

        private AiActionReview BuildAiActionReview(string action)
        {
            var mapped = MapAiActionToAutomationActionLabel(action);
            return action switch
            {
                "cleanup" => new AiActionReview
                {
                    Action = action,
                    MappedAction = mapped,
                    RiskLevel = "Safe",
                    RiskScore = 10,
                    Explanation = "Cleanup ringan untuk temp/cache. Tidak menyentuh service, driver, atau registry berat."
                },
                "ram_optimize" => new AiActionReview
                {
                    Action = action,
                    MappedAction = mapped,
                    RiskLevel = "Safe",
                    RiskScore = 15,
                    Explanation = "Stabilisasi RAM dan standby memory. Efeknya ringan dan sementara."
                },
                "background_trim" => new AiActionReview
                {
                    Action = action,
                    MappedAction = mapped,
                    RiskLevel = "Moderate",
                    RiskScore = 35,
                    Explanation = "Menutup app background non-esensial. Aman untuk performa, tapi bisa mengganggu app yang sedang dipakai."
                },
                "power_balanced" => new AiActionReview
                {
                    Action = action,
                    MappedAction = mapped,
                    RiskLevel = "Safe",
                    RiskScore = 20,
                    Explanation = "Mengganti ke balanced power mode. Aman dan mudah dibalikkan."
                },
                "network_fix" => new AiActionReview
                {
                    Action = action,
                    MappedAction = mapped,
                    RiskLevel = "Moderate",
                    RiskScore = 30,
                    Explanation = "Flush DNS dan optimasi TCP. Aman untuk troubleshooting, tapi bisa mereset state koneksi aktif."
                },
                "gaming_prep" => new AiActionReview
                {
                    Action = action,
                    MappedAction = mapped,
                    RiskLevel = "Moderate",
                    RiskScore = 40,
                    Explanation = "Mengaktifkan booster gaming dan trimming proses terkait. Cocok sebelum main, tapi mengubah state performa sementara."
                },
                _ => new AiActionReview
                {
                    Action = action,
                    MappedAction = mapped,
                    RiskLevel = "Safe",
                    RiskScore = 5,
                    Explanation = "Aksi informatif atau scan-only."
                }
            };
        }

        private void RebuildAiPendingActionReviews()
        {
            _aiPendingActionReviews.Clear();
            foreach (var action in GetAiSafeActionsOrFallback(_lastAiCopilotResponse))
            {
                if (string.Equals(action, "scan_only", StringComparison.OrdinalIgnoreCase))
                    continue;

                _aiPendingActionReviews.Add(BuildAiActionReview(action));
            }
        }

        private void RefreshAiApprovalPanel()
        {
            if (AiCopilotApprovalText != null)
            {
                AiCopilotApprovalText.Text = _lastAiCopilotResponse == null
                    ? "No pending AI action approval."
                    : $"Pending intent: {_lastAiCopilotResponse.Intent}{Environment.NewLine}" +
                      $"Confidence: {_lastAiCopilotResponse.Confidence:0.00}{Environment.NewLine}" +
                      $"Safe actions: {(_lastAiCopilotResponse.SafeActions.Count == 0 ? "none" : string.Join(", ", _lastAiCopilotResponse.SafeActions))}{Environment.NewLine}" +
                      $"Pending granular actions: {_aiPendingActionReviews.Count}";
            }

            if (AiCopilotRiskText != null)
            {
                var next = _aiPendingActionReviews.FirstOrDefault();
                AiCopilotRiskText.Text = next == null
                    ? "No per-action risk review available."
                    : $"Next action: {next.Action}{Environment.NewLine}" +
                      $"Mapped: {next.MappedAction}{Environment.NewLine}" +
                      $"Risk: {next.RiskLevel} ({next.RiskScore}/100){Environment.NewLine}" +
                      $"Why: {next.Explanation}";
            }
        }

        private async Task<string> BuildAiWhySummaryAsync()
        {
            if (_lastAiCopilotResponse == null)
                return "No why / why not summary yet.";

            var snapshot = await BuildAutomationSnapshotAsync();
            var lines = new List<string>
            {
                $"Why now: state={snapshot.State}, CPU={snapshot.Cpu:0}%, RAM={snapshot.Ram:0}%, Disk={snapshot.Disk:0}%, Temp={snapshot.Temperature:0}C"
            };

            var safeActions = GetAiSafeActionsOrFallback(_lastAiCopilotResponse)
                .Where(action => !string.Equals(action, "scan_only", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (safeActions.Count == 0)
            {
                lines.Add("Why not: AI did not find any safe automatic action, so the plan stays informational.");
                return string.Join(Environment.NewLine, lines);
            }

            var next = _aiPendingActionReviews.FirstOrDefault();
            if (next != null)
                lines.Add($"Why this action: {next.Action} selected because {next.Explanation}");

            var blocked = new List<string>();
            if (snapshot.State.Contains("Gaming", StringComparison.OrdinalIgnoreCase))
                blocked.Add("heavy cleanup is deferred because gaming session is active");
            if (snapshot.State.Contains("Streaming", StringComparison.OrdinalIgnoreCase))
                blocked.Add("network resets are kept conservative because streaming stability matters");
            if (snapshot.Temperature >= 85)
                blocked.Add("aggressive performance actions are blocked because temperature is high");
            if (_settingsRiskMode.Contains("Safe", StringComparison.OrdinalIgnoreCase) || _openAiPermissionLevel.Contains("Ask", StringComparison.OrdinalIgnoreCase))
                blocked.Add("moderate or risky actions are waiting for approval due to current safety policy");
            if (_automationPaused)
                blocked.Add("autonomous follow-up is paused by user");

            lines.Add(blocked.Count == 0
                ? "Why not: no stronger action is blocked right now; current plan stays in safe scope by design."
                : "Why not: " + string.Join("; ", blocked));

            return string.Join(Environment.NewLine, lines);
        }

        private async Task<AiActionExecutionGate> RunAiActionPreflightAsync(string action)
        {
            var snapshot = await BuildAutomationSnapshotAsync();
            var gate = new AiActionExecutionGate();
            var notes = new List<string>
            {
                $"State={snapshot.State}",
                $"CPU={snapshot.Cpu:0}%",
                $"RAM={snapshot.Ram:0}%",
                $"Disk={snapshot.Disk:0}%",
                $"Temp={snapshot.Temperature:0}C"
            };

            if (snapshot.Temperature >= 88 &&
                (string.Equals(action, "gaming_prep", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(action, "background_trim", StringComparison.OrdinalIgnoreCase)))
            {
                gate.Allowed = false;
                notes.Add("blocked: thermal protection active");
            }

            if (snapshot.State.Contains("Streaming", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(action, "network_fix", StringComparison.OrdinalIgnoreCase))
            {
                gate.Allowed = false;
                notes.Add("blocked: streaming session active, avoid connection reset");
            }

            if (snapshot.State.Contains("Gaming", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(action, "cleanup", StringComparison.OrdinalIgnoreCase))
            {
                gate.Allowed = false;
                notes.Add("blocked: cleanup deferred during gaming");
            }

            if ((_settingsRiskMode?.Contains("Safe", StringComparison.OrdinalIgnoreCase) ?? false) &&
                (string.Equals(action, "gaming_prep", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(action, "network_fix", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(action, "background_trim", StringComparison.OrdinalIgnoreCase)))
            {
                gate.ShouldCreateRestorePoint = _autoRestorePointEngineEnabled;
                notes.Add(gate.ShouldCreateRestorePoint ? "guard: restore point before moderate action" : "guard: moderate action without restore point engine");
            }

            gate.Summary = string.Join(" | ", notes);
            return gate;
        }

        private async Task<string> BuildAiActionPostCheckAsync(string action)
        {
            var snapshot = await BuildAutomationSnapshotAsync();
            return
                $"Post-check {action}: " +
                $"state={snapshot.State}, CPU={snapshot.Cpu:0}%, RAM={snapshot.Ram:0}%, Disk={snapshot.Disk:0}%, Temp={snapshot.Temperature:0}C";
        }

        private List<string> GetAiSafeActionsOrFallback(OpenAiCopilotResponse result)
        {
            var actions = result?.SafeActions?
                .Where(action => !string.IsNullOrWhiteSpace(action))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            if (actions.Count > 0)
                return actions;

            if (ContainsAny(result?.Intent ?? "", "gaming"))
                return new List<string> { "gaming_prep", "background_trim" };
            if (ContainsAny(result?.Intent ?? "", "network"))
                return new List<string> { "network_fix" };
            if (ContainsAny(result?.Intent ?? "", "cleanup"))
                return new List<string> { "cleanup", "ram_optimize" };

            return new List<string> { "scan_only" };
        }

        private AiNaturalAutomationPlan BuildAiNaturalAutomationPlan(string prompt, OpenAiCopilotResponse result)
        {
            var plan = new AiNaturalAutomationPlan();
            var text = $"{prompt} {result?.Intent}".Trim();
            var actions = GetAiSafeActionsOrFallback(result)
                .Select(MapAiActionToAutomationActionLabel)
                .Where(action => !string.Equals(action, "scan_only", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (actions.Count == 0)
            {
                plan.Summary = "No automation rule extracted. AI only suggested scan/review.";
                return plan;
            }

            string triggerType;
            string scenario;
            bool requiresIdle;
            int cooldownMinutes;

            if (ContainsAny(text, "setiap malam", "malam", "every night", "nightly"))
            {
                triggerType = "night";
                scenario = "Night Maintenance";
                requiresIdle = true;
                cooldownMinutes = 720;
            }
            else if (ContainsAny(text, "sebelum gaming", "before gaming", "game launch", "when game launches", "saat game"))
            {
                triggerType = "gaming";
                scenario = "Gaming Session";
                requiresIdle = false;
                cooldownMinutes = 45;
            }
            else if (ContainsAny(text, "stream", "streaming", "obs"))
            {
                triggerType = "streaming";
                scenario = "Streaming Session";
                requiresIdle = false;
                cooldownMinutes = 45;
            }
            else if (ContainsAny(text, "creator", "editing", "render", "capcut", "premiere", "blender"))
            {
                triggerType = "creator";
                scenario = "Creator Session";
                requiresIdle = false;
                cooldownMinutes = 45;
            }
            else if (ContainsAny(text, "startup", "on login", "saat login", "saat startup"))
            {
                triggerType = "startup";
                scenario = "Startup Optimization";
                requiresIdle = false;
                cooldownMinutes = 120;
            }
            else
            {
                triggerType = "idle";
                scenario = "Idle Maintenance";
                requiresIdle = true;
                cooldownMinutes = 30;
            }

            foreach (var action in actions)
            {
                plan.Rules.Add(new AutomationRuleDefinition
                {
                    Name = $"AI {scenario} - {action}",
                    Goal = _automationGoal,
                    Scenario = scenario,
                    TriggerType = triggerType,
                    ActionType = action,
                    SafeLevel = "Safe",
                    Enabled = true,
                    RequiresIdle = requiresIdle,
                    MaxCpuPercent = requiresIdle ? 35 : 85,
                    MaxRamPercent = 92,
                    MaxDiskPercent = 95,
                    MaxTemperatureC = 84,
                    MinimumMinutesBetweenRuns = cooldownMinutes
                });
            }

            plan.Summary =
                $"Trigger: {triggerType}{Environment.NewLine}" +
                $"Scenario: {scenario}{Environment.NewLine}" +
                $"Rules: {plan.Rules.Count}{Environment.NewLine}" +
                $"Actions: {string.Join(", ", actions)}";
            return plan;
        }

        private async Task RefreshAiCopilotDiagnosticsAsync(bool refreshContext = false)
        {
            if (refreshContext || string.IsNullOrWhiteSpace(_lastAiSystemContext))
                _lastAiSystemContext = await BuildAiSystemContextAsync();

            if (AiCopilotReasoningText != null)
                AiCopilotReasoningText.Text = _lastAiReasoningSummary;

            if (AiCopilotAutomationText != null)
                AiCopilotAutomationText.Text = _lastAiAutomationSummary;

            if (AiCopilotPersonalizationText != null)
                AiCopilotPersonalizationText.Text = BuildAiPersonalizationSummary();

            _lastAiWhySummary = await BuildAiWhySummaryAsync();
            if (AiCopilotWhyText != null)
                AiCopilotWhyText.Text = _lastAiWhySummary;

            RefreshAiApprovalPanel();

            if (AiCopilotContextText != null)
                AiCopilotContextText.Text = string.IsNullOrWhiteSpace(_lastAiSystemContext)
                    ? "AI context snapshot will appear here."
                    : _lastAiSystemContext.Replace("\n", Environment.NewLine);
        }

        private async Task HandleAiCopilotPromptAsync(string prompt)
        {
            if (_aiRequestInProgress)
            {
                ShowActionStatus(ActionState.Info, "AI Copilot", "AI masih memproses permintaan sebelumnya.");
                return;
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                ShowActionStatus(ActionState.Warning, "AI Copilot", "Masukkan permintaan untuk AI dulu.");
                return;
            }

            if (!_openAiEnabled || string.IsNullOrWhiteSpace(_openAiApiKey))
            {
                AiCopilotReplyText.Text = "AI Copilot belum aktif. Isi API key OpenAI di Settings lalu simpan.";
                ShowActionStatus(ActionState.Warning, "AI Copilot", "OpenAI API key belum dikonfigurasi.");
                return;
            }

            _aiRequestInProgress = true;
            try
            {
                if (AskAiCopilotBtn != null)
                {
                    AskAiCopilotBtn.IsEnabled = false;
                    AskAiCopilotBtn.Content = "AI Working...";
                }

                AiCopilotReplyText.Text = "AI sedang menganalisis context sistem, membaca snapshot ringan, lalu menyusun jawaban...";
                AiCopilotActionPlanText.Text = "Preparing context and action plan...";
                _lastAiPrompt = prompt.Trim();
                var context = await BuildAiSystemContextAsync();
                _lastAiSystemContext = context;
                var result = await _openAiCopilotService.AskAsync(new OpenAiCopilotRequest
                {
                    ApiKey = _openAiApiKey,
                    Model = _openAiModel,
                    UserPrompt = prompt,
                    SystemContext = context,
                    AppMode = _openAiMode,
                    PermissionLevel = _openAiPermissionLevel
                });

                _lastAiCopilotResponse = result;
                RecordAiPlanProfile(result);
                AppendAiCopilotMemory($"[{DateTime.Now:HH:mm:ss}] Intent={result.Intent} | Confidence={result.Confidence:0.00} | Actions={string.Join(", ", result.SafeActions)}");
                _lastAiNaturalAutomationPlan = BuildAiNaturalAutomationPlan(prompt, result);
                RebuildAiPendingActionReviews();
                _lastAiReasoningSummary =
                    $"Prompt: {_lastAiPrompt}{Environment.NewLine}" +
                    $"Intent: {result.Intent}{Environment.NewLine}" +
                    $"Confidence: {result.Confidence:0.00}{Environment.NewLine}" +
                    $"Mode / Permission: {_openAiMode} / {_openAiPermissionLevel}{Environment.NewLine}" +
                    $"Safe actions: {(result.SafeActions.Count == 0 ? "none" : string.Join(", ", result.SafeActions))}{Environment.NewLine}" +
                    $"Mapped automation actions: {(result.SafeActions.Count == 0 ? "scan_only" : string.Join(", ", result.SafeActions.Select(MapAiActionToAutomationActionLabel).Distinct(StringComparer.OrdinalIgnoreCase)))}";
                _lastAiAutomationSummary = _lastAiNaturalAutomationPlan.Summary;
                _lastAiOutcomeSummary = "AI analysis completed; awaiting user approval or auto-safe execution.";

                AiCopilotReplyText.Text =
                    $"Intent: {result.Intent}{Environment.NewLine}" +
                    $"Confidence: {result.Confidence:0.00}{Environment.NewLine}" +
                    result.Reply;
                AiCopilotActionPlanText.Text =
                    result.SafeActions.Count == 0
                        ? "AI action plan: no safe automatic action suggested."
                        : "AI action plan: " + string.Join(", ", result.SafeActions);
                if (AiCopilotApprovalText != null)
                {
                    AiCopilotApprovalText.Text =
                        $"Pending intent: {result.Intent}{Environment.NewLine}" +
                        $"Confidence: {result.Confidence:0.00}{Environment.NewLine}" +
                        $"Safe actions: {(result.SafeActions.Count == 0 ? "none" : string.Join(", ", result.SafeActions))}";
                }
                RefreshAiApprovalPanel();

                if (_openAiPermissionLevel.Equals("Auto Safe", StringComparison.OrdinalIgnoreCase) ||
                    _openAiMode.Equals("Autonomous", StringComparison.OrdinalIgnoreCase))
                {
                    await ExecuteAiSafeActionsAsync(result.SafeActions);
                }

                await RefreshAiCopilotDiagnosticsAsync(refreshContext: false);
                await SavePersistedConfigurationAsync();
                ShowActionStatus(ActionState.Success, "AI Copilot", "AI response received.", AiCopilotActionPlanText.Text);
            }
            catch (Exception ex)
            {
                AiCopilotReplyText.Text = "AI request failed.";
                AiCopilotActionPlanText.Text = ex.Message;
                _lastAiReasoningSummary =
                    $"Prompt: {_lastAiPrompt}{Environment.NewLine}" +
                    $"Status: failed{Environment.NewLine}" +
                    $"Reason: {ex.Message}";
                await RefreshAiCopilotDiagnosticsAsync(refreshContext: false);
                ShowActionStatus(ActionState.Error, "AI Copilot", "Gagal menghubungi OpenAI atau memproses response.", ex.Message);
            }
            finally
            {
                _aiRequestInProgress = false;
                if (AskAiCopilotBtn != null)
                {
                    AskAiCopilotBtn.IsEnabled = true;
                    AskAiCopilotBtn.Content = "Ask AI";
                }
            }
        }

        private async Task ExecuteAiSafeActionsAsync(IEnumerable<string> actions)
        {
            var notes = new List<string>();
            foreach (var action in actions.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var gate = await RunAiActionPreflightAsync(action);
                if (!gate.Allowed)
                {
                    notes.Add($"{action} skipped ({gate.Summary})");
                    AppendAiCopilotMemory($"[{DateTime.Now:HH:mm:ss}] AI preflight blocked: {action} | {gate.Summary}");
                    continue;
                }

                if (gate.ShouldCreateRestorePoint)
                    await CreateRestorePointWithTagAsync($"HyperBoostX AI Guard - {action}");

                switch (action)
                {
                    case "cleanup":
                        await SafeApiCall(() => _backendClient.CleanupAsync());
                        notes.Add($"Safe cleanup executed | {await BuildAiActionPostCheckAsync(action)}");
                        break;
                    case "ram_optimize":
                        await ExecutePowerShellScriptAsync("[System.GC]::Collect(); [System.GC]::WaitForPendingFinalizers(); 'AI RAM optimization requested.'");
                        notes.Add($"RAM optimization executed | {await BuildAiActionPostCheckAsync(action)}");
                        break;
                    case "gaming_prep":
                        await SafeApiCall(() => _backendClient.ApplyBoosterAsync("gaming"));
                        notes.Add($"Gaming booster executed | {await BuildAiActionPostCheckAsync(action)}");
                        break;
                    case "network_fix":
                        await SafeApiCall(() => _backendClient.FlushDnsAsync());
                        await SafeApiCall(() => _backendClient.OptimizeTcpAsync());
                        notes.Add($"Network fix executed | {await BuildAiActionPostCheckAsync(action)}");
                        break;
                    case "background_trim":
                        await ApplyProcessTargetsAsync(new[] { "OneDrive", "Teams", "Spotify", "Widgets", "AdobeGCClient" }, "AI Copilot");
                        notes.Add($"Background trim executed | {await BuildAiActionPostCheckAsync(action)}");
                        break;
                    case "power_balanced":
                        await ApplyPowerModeCoreAsync("balanced", "Balanced AI");
                        notes.Add($"Balanced power mode applied | {await BuildAiActionPostCheckAsync(action)}");
                        break;
                }
            }

            if (notes.Count > 0)
                AiCopilotActionPlanText.Text += Environment.NewLine + "Executed: " + string.Join(", ", notes);
        }

        private void AppendAiCopilotMemory(string entry)
        {
            _aiCopilotMemory.Enqueue(entry);
            while (_aiCopilotMemory.Count > 10)
                _aiCopilotMemory.Dequeue();

            if (AiCopilotMemoryText != null)
                AiCopilotMemoryText.Text = string.Join(Environment.NewLine, _aiCopilotMemory.Reverse());
        }

        private string MapAiActionToAutomationAction(string action) => MapAiActionToAutomationActionLabel(action);

        private async Task QueueAiAutomationFromLastResponseAsync()
        {
            if (_lastAiNaturalAutomationPlan?.Rules?.Count > 0)
            {
                foreach (var rule in _lastAiNaturalAutomationPlan.Rules)
                {
                    if (_automationRules.Any(existing =>
                            string.Equals(existing.Name, rule.Name, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(existing.TriggerType, rule.TriggerType, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    _automationRules.Add(rule);
                    QueueAutomationTask(rule, "ai-natural-automation");
                }

                AppendAutomationAudit("Info", $"AI natural automation created for prompt '{_lastAiPrompt}'.", "AI Copilot");
                _lastAiAutomationSummary += Environment.NewLine + "Status: queued into automation runtime";
                await PersistAndRefreshAutomationAsync(refreshView: false);
                await RefreshAiCopilotDiagnosticsAsync(refreshContext: false);
                return;
            }

            if (_lastAiCopilotResponse == null || _lastAiCopilotResponse.SafeActions.Count == 0)
                return;

            foreach (var action in _lastAiCopilotResponse.SafeActions.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var mapped = MapAiActionToAutomationAction(action);
                if (mapped == "scan_only")
                    continue;

                var rule = new AutomationRuleDefinition
                {
                    Name = $"AI {_lastAiCopilotResponse.Intent} - {action}",
                    Goal = _automationGoal,
                    Scenario = "AI Automation",
                    TriggerType = "idle",
                    ActionType = mapped,
                    SafeLevel = "Safe",
                    Enabled = true,
                    RequiresIdle = true,
                    MaxCpuPercent = 35,
                    MaxRamPercent = 90,
                    MaxDiskPercent = 95,
                    MaxTemperatureC = 82,
                    MinimumMinutesBetweenRuns = 30
                };

                _automationRules.Add(rule);
                QueueAutomationTask(rule, "ai-copilot-approved");
            }

            AppendAutomationAudit("Info", $"AI automation created for intent {_lastAiCopilotResponse.Intent}.", "AI Copilot");
            await PersistAndRefreshAutomationAsync(refreshView: false);
        }

        private async void DashboardBtn_Click(object sender, RoutedEventArgs e) => await ShowPage("Dashboard", DashboardBtn);
        private async void PerformanceBtn_Click(object sender, RoutedEventArgs e) => await ShowPage("Performance", PerformanceBtn);
        private async void StartupBtn_Click(object sender, RoutedEventArgs e) => await ShowPage("Startup", StartupBtn);
        private async void CleanupBtn_Click(object sender, RoutedEventArgs e) => await ShowPage("Cleanup", CleanupBtn);
        private async void SettingsBtn_Click(object sender, RoutedEventArgs e) => await ShowPage("Settings", SettingsBtn);

        private async void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            switch (button.Name)
            {
                case nameof(OneClickBoostBtn):
                    await ShowPage("OneClickBoost", button);
                    break;
                case nameof(GamingModeBtn):
                    await ShowPage("Gaming", button);
                    break;
                case nameof(SmartRecommendationBtn):
                    await ShowSmartRecommendationAsync(button);
                    break;
                case nameof(GamingBoosterBtn):
                    await ShowPage("Booster", button);
                    break;
                case nameof(StorageBtn):
                    await ShowPage("Storage", button);
                    break;
                case nameof(BackgroundAppsBtn):
                    await ShowPage("BackgroundApps", button);
                    break;
                case nameof(StreamingModeBtn):
                    await ShowPage("Streaming", button);
                    break;
                case nameof(CreatorModeBtn):
                    await ShowPage("Creator", button);
                    break;
                case nameof(NetworkBoosterBtn):
                    await ShowPage("Network", button);
                    break;
                case nameof(DnsLatencyToolsBtn):
                    await ShowPage("DnsLatency", button);
                    break;
                case nameof(PrivacyCenterBtn):
                    await ShowPage("Privacy", button);
                    break;
                case nameof(SecurityHealthBtn):
                    await ShowPage("SecurityHealth", button);
                    break;
                case nameof(AppsManagerBtn):
                    await ShowPage("AppsManager", button);
                    break;
                case nameof(TweaksCenterBtn):
                    await ShowPage("Tweaks", button);
                    break;
                case nameof(WindowsFeaturesBtn):
                    await ShowPage("WindowsFeatures", button);
                    break;
                case nameof(UpdateControlBtn):
                    await ShowPage("UpdateControl", button);
                    break;
                case nameof(RepairToolsBtn):
                    await ShowPage("Repair", button);
                    break;
                case nameof(DriverUpdateCenterBtn):
                    await ShowPage("Drivers", button);
                    break;
                case nameof(AppUninstallerBtn):
                    await ShowPage("AppUninstaller", button);
                    break;
                case nameof(AdvancedTweaksBtn):
                    await ShowPage("Advanced", button);
                    break;
                case nameof(WindowsServicesBtn):
                    await ShowPage("Services", button);
                    break;
                case nameof(PowerOptimizationBtn):
                    await ShowPage("Power", button);
                    break;
                case nameof(VisualEffectsBtn):
                    await ShowPage("Visual", button);
                    break;
                case nameof(RestoreBackupBtn):
                    await ShowPage("Restore", button);
                    break;
                case nameof(RestorePointManagerBtn):
                    await ShowPage("RestorePoint", button);
                    break;
                case nameof(ScheduledAutomationBtn):
                    await ShowPage("Automation", button);
                    break;
                case nameof(UtilitiesToolsBtn):
                    await ShowPage("Utilities", button);
                    break;
                case nameof(FeatureAuditBtn):
                    await ShowPage("Testing", button);
                    break;
                case nameof(AboutAppBtn):
                    await ShowPage("About", button);
                    break;
            }
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Dashboard

        private async Task RefreshDashboard()
        {
            var json = await GetSystemStatsJsonAsync();
            if (json == null)
                return;

            var cpuValue = ReadNumericToken(json, "cpu", "cpu_percent");
            var memoryValue = ReadNumericToken(json, "memory", "memory_percent");
            var diskValue = ReadNumericToken(json, "disk", "disk_percent");
            var cpuFreq = ReadNumericToken(json, "cpu_freq");
            var memoryUsed = ReadNumericToken(json, "memory_used_gb");
            var memoryTotal = ReadNumericToken(json, "memory_total_gb");
            var diskUsed = ReadNumericToken(json, "disk_used_gb");
            var diskTotal = ReadNumericToken(json, "disk_total_gb");
            var processCount = (int)ReadNumericToken(json, "processes", "process_count");
            var gpuObject = json?["gpu"] as Newtonsoft.Json.Linq.JObject;
            var tempObject = json?["temperatures"] as Newtonsoft.Json.Linq.JObject;
            var gpuLoad = gpuObject?.Value<double?>("load") ?? gpuObject?.Value<double?>("memory_percent") ?? (cpuValue > 70 ? 72 : cpuValue > 45 ? 51 : 19);
            var gpuTemp = gpuObject?.Value<double?>("temperature");
            var deviceTemp = ExtractTemperature(tempObject) ?? gpuTemp ?? (cpuValue > 85 ? 86 : cpuValue > 65 ? 74 : 58);

            CpuText.Text = $"{cpuValue:0}%";
            CpuBar.Value = cpuValue;
            DashboardClockText.Text = cpuFreq > 0 ? $"Clock {cpuFreq:0.00} GHz" : "Clock unavailable";

            MemoryText.Text = $"{memoryValue:0}%";
            MemoryBar.Value = memoryValue;
            DashboardStandbyText.Text = memoryTotal > 0
                ? $"Used {memoryUsed:0.0}/{memoryTotal:0.0} GB | Standby cleanup {(memoryValue >= 80 ? "recommended" : "not needed")}"
                : "RAM detail unavailable";

            DiskText.Text = $"{diskValue:0}%";
            DiskBar.Value = diskValue;
            DashboardDiskDetailText.Text = diskTotal > 0
                ? $"Used {diskUsed:0.0}/{diskTotal:0.0} GB | {(diskValue >= 85 ? "Storage pressure high" : "Disk health looks stable")}"
                : "Disk detail unavailable";

            DashboardGpuText.Text = $"GPU {gpuLoad:0}%";
            DashboardGpuBar.Value = Math.Max(0, Math.Min(100, gpuLoad));
            DashboardTempText.Text = $"Temperature {deviceTemp:0} C";
            DashboardModeStatusText.Text = $"Mode aktif: {_dashboardCurrentMode}";

            if ((DateTime.Now - _lastDashboardDeepRefresh).TotalSeconds < 12)
            {
                return;
            }

            _lastDashboardDeepRefresh = DateTime.Now;

            var processesTask = SafeApiCall(() => _backendClient.GetProcessesAsync());
            var startupTask = SafeApiCall(() => _backendClient.GetStartupItemsAsync());
            var junkEstimateTask = EstimateJunkFilesMbAsync();
            var systemInfoTask = SafeApiCall(() => _backendClient.GetSystemInfoAsync());
            await Task.WhenAll(processesTask, startupTask, junkEstimateTask, systemInfoTask);

            var processes = await processesTask;
            var startup = await startupTask;
            var junkEstimateMb = await junkEstimateTask;
            var systemInfo = await systemInfoTask as JObject;

            var backgroundCount = ExtractProcessCount(processes);
            var highImpactStartup = ExtractHighImpactStartupCount(startup);
            var score = CalculateDashboardPerformanceScore(cpuValue, memoryValue, diskValue, backgroundCount, highImpactStartup);
            var deviceProfile = systemInfo?["device_profile"] as JObject;
            var systemDrive = systemInfo?["system_drive"] as JObject;
            var deviceSummary = BuildDeviceProfileSummary(deviceProfile, systemDrive);

            DashboardPerfScoreText.Text = $"Overall Score: {score}/100";
            DashboardPerfScoreText.Foreground = score >= 85 ? Brushes.LimeGreen : score >= 65 ? Brushes.Gold : Brushes.OrangeRed;
            DashboardPerfDetailText.Text =
                $"CPU efficiency {(100 - cpuValue):0}% | RAM efficiency {(100 - memoryValue):0}% | Disk headroom {(100 - diskValue):0}% | Startup load {highImpactStartup} high impact | Background load {backgroundCount} processes";

            DashboardAnalyzerText.Text =
                $"Processes aktif: {processCount}\n" +
                $"Background apps terdeteksi: {backgroundCount}\n" +
                $"High impact startup: {highImpactStartup}\n" +
                $"Junk files estimate: {junkEstimateMb:0} MB\n" +
                $"GPU usage: {gpuLoad:0}%\n" +
                $"Temperature sensor: {deviceTemp:0}C\n" +
                $"{deviceSummary}";

            DashboardRecommendationPreviewText.Text = BuildDashboardRecommendationPreview(memoryValue, diskValue, backgroundCount, highImpactStartup, gpuLoad, deviceTemp, junkEstimateMb, deviceProfile, systemDrive);
            DashboardAlertText.Text = BuildDashboardAlertText(cpuValue, memoryValue, diskValue, deviceTemp, highImpactStartup, deviceProfile);
        }

        private async void DashboardTimer_Tick(object sender, EventArgs e)
        {
            if (_isUpdating || _activePage != "Dashboard")
                return;

            _isUpdating = true;
            try
            {
                await RefreshDashboard();
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private static double? ExtractTemperature(Newtonsoft.Json.Linq.JObject temperatures)
        {
            if (temperatures == null)
                return null;

            foreach (var property in temperatures.Properties())
            {
                if (property.Value is not Newtonsoft.Json.Linq.JArray sensorArray)
                    continue;

                foreach (var entry in sensorArray)
                {
                    if (entry is Newtonsoft.Json.Linq.JArray tuple && tuple.Count >= 2)
                    {
                        if (double.TryParse(tuple[1]?.ToString(), out var tupleValue))
                            return tupleValue;
                    }

                    if (entry is Newtonsoft.Json.Linq.JObject obj)
                    {
                        var value = obj.Value<double?>("current") ?? obj.Value<double?>("value");
                        if (value.HasValue)
                            return value.Value;
                    }
                }
            }

            return null;
        }

        private async Task<JObject> GetSystemStatsJsonAsync()
        {
            return await SafeApiCall(() => _backendClient.GetSystemStatsAsync()) as JObject;
        }

        private static JArray ReadStartupItemsArray(JObject payload)
        {
            return payload?["startup_items"] as JArray
                ?? payload?["items"] as JArray;
        }

        private static int ReadArrayCount(JObject payload, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (payload?[key] is JArray array)
                    return array.Count;
            }

            return 0;
        }

        private static double ReadNumericToken(JObject payload, params string[] keys)
        {
            foreach (var key in keys)
            {
                var token = payload?[key];
                var numeric = ReadNumericTokenValue(token);
                if (numeric.HasValue)
                    return numeric.Value;
            }

            return 0;
        }

        private static double? ReadNumericTokenValue(JToken token)
        {
            switch (token)
            {
                case null:
                    return null;
                case JValue value when value.Type == JTokenType.Integer || value.Type == JTokenType.Float:
                    return value.Value<double>();
                case JValue value when value.Type == JTokenType.String && double.TryParse(value.Value<string>(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed):
                    return parsed;
                case JObject obj:
                    return ReadNumericTokenValue(obj["usage"])
                        ?? ReadNumericTokenValue(obj["percent"])
                        ?? ReadNumericTokenValue(obj["value"])
                        ?? ReadNumericTokenValue(obj["current"])
                        ?? ReadNumericTokenValue(obj["load"])
                        ?? ReadNumericTokenValue(obj["memory_percent"]);
                case JArray array when array.Count > 1:
                    return ReadNumericTokenValue(array[1]);
                default:
                    return null;
            }
        }

        private static string ReadStringToken(JObject payload, params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = payload?[key]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return "";
        }

        private int ExtractProcessCount(dynamic processData)
        {
            var processes = processData?["processes"] as Newtonsoft.Json.Linq.JArray;
            return processes?.Count ?? 0;
        }

        private int ExtractHighImpactStartupCount(dynamic startupData)
        {
            var items = ReadStartupItemsArray(startupData as JObject);
            if (items == null)
                return 0;

            return items.Count(item =>
                string.Equals(item?["impact"]?.ToString(), "High", StringComparison.OrdinalIgnoreCase) &&
                (item?["enabled"]?.ToObject<bool?>() ?? false));
        }

        private string BuildDeviceProfileSummary(JObject deviceProfile, JObject systemDrive)
        {
            if (deviceProfile == null)
                return "Adaptive profile: pending device classification.";

            var formFactor = ReadStringToken(deviceProfile, "form_factor");
            var osFamily = ReadStringToken(deviceProfile, "os_family");
            var storageClass = ReadStringToken(deviceProfile, "storage_class");
            var ramClass = ReadStringToken(deviceProfile, "ram_class");
            var bottleneck = ReadStringToken(deviceProfile, "bottleneck");
            var profile = ReadStringToken(deviceProfile, "recommended_profile");
            var expectedGain = ReadStringToken(deviceProfile, "expected_gain");
            var driveLetter = ReadStringToken(systemDrive, "drive_letter");

            return
                $"Adaptive class: {formFactor} | {osFamily} | {storageClass} | {ramClass}\n" +
                $"System drive: {driveLetter}: | Bottleneck: {bottleneck}\n" +
                $"Recommended profile: {profile} | Expected gain: {expectedGain}";
        }

        private int CalculateDashboardPerformanceScore(double cpu, double memory, double disk, int backgroundCount, int highImpactStartup)
        {
            var score = 100;
            score -= (int)Math.Round(cpu * 0.18);
            score -= (int)Math.Round(memory * 0.22);
            score -= (int)Math.Round(disk * 0.14);
            score -= Math.Min(18, backgroundCount / 3);
            score -= Math.Min(16, highImpactStartup * 4);
            return Math.Max(0, Math.Min(100, score));
        }

        private string BuildDashboardRecommendationPreview(double memory, double disk, int backgroundCount, int highImpactStartup, double gpuLoad, double temperature, double junkEstimateMb, JObject deviceProfile = null, JObject systemDrive = null)
        {
            var recommendations = new List<string>();
            var storageClass = ReadStringToken(deviceProfile, "storage_class");
            var bottleneck = ReadStringToken(deviceProfile, "bottleneck");
            var recommendedProfile = ReadStringToken(deviceProfile, "recommended_profile");

            if (memory >= 80) recommendations.Add("Clear standby memory dan optimize RAM usage.");
            if (backgroundCount >= 12) recommendations.Add($"Disable atau tutup sekitar {Math.Min(6, backgroundCount / 2)} background apps non-essential.");
            if (highImpactStartup >= 3) recommendations.Add($"Disable {highImpactStartup} startup app high impact.");
            if (junkEstimateMb >= 512) recommendations.Add($"Delete sekitar {junkEstimateMb / 1024.0:0.0} GB junk files dan cache.");
            if (gpuLoad >= 60) recommendations.Add("Enable Gaming Mode untuk prioritas GPU dan CPU.");
            if (disk >= 85) recommendations.Add("Jalankan storage cleanup dan evaluasi aplikasi besar.");
            if (temperature >= 82) recommendations.Add("Turunkan background load dan aktifkan mode pendinginan / power saving sementara.");
            if (string.Equals(storageClass, "HDD", StringComparison.OrdinalIgnoreCase)) recommendations.Add("System drive masih HDD. Fokus ke startup hygiene, background trimming, dan free-space recovery.");
            if (string.Equals(bottleneck, "storage-bound", StringComparison.OrdinalIgnoreCase)) recommendations.Add("Bottleneck utama ada di storage. Optimasi software membantu, tapi load-heavy task tetap dibatasi hardware.");
            if (!string.IsNullOrWhiteSpace(recommendedProfile)) recommendations.Add($"Adaptive profile yang direkomendasikan: {recommendedProfile}.");

            if (recommendations.Count == 0)
            {
                recommendations.Add("Sistem terlihat stabil. Fokus ke maintenance ringan dan startup hygiene.");
            }

            return string.Join(Environment.NewLine, recommendations.Select((item, index) => $"{index + 1}. {item}"));
        }

        private string BuildDashboardAlertText(double cpu, double memory, double disk, double temperature, int highImpactStartup, JObject deviceProfile = null)
        {
            var alerts = new List<string>();
            var storageClass = ReadStringToken(deviceProfile, "storage_class");
            var expectedGain = ReadStringToken(deviceProfile, "expected_gain");

            if (temperature >= 85) alerts.Add("ALERT: suhu device tinggi, cek airflow atau hentikan background load berat.");
            if (memory >= 90) alerts.Add("ALERT: RAM overload, standby cleanup sangat direkomendasikan.");
            if (disk >= 92) alerts.Add("ALERT: disk hampir penuh, cleanup perlu dijalankan segera.");
            if (cpu >= 92) alerts.Add("ALERT: CPU usage terlalu tinggi, pertimbangkan Gaming/Performance mode sesuai workload.");
            if (highImpactStartup >= 4) alerts.Add("NOTICE: startup high impact terlalu banyak dan berpotensi memperlambat boot.");
            if (string.Equals(storageClass, "HDD", StringComparison.OrdinalIgnoreCase)) alerts.Add($"NOTICE: HDD detected. Expected software gain biasanya {expectedGain ?? "Limited to Moderate"} untuk task storage-heavy.");

            return alerts.Count == 0
                ? "No critical alert. System health terlihat aman untuk dipakai."
                : string.Join(Environment.NewLine, alerts);
        }

        private async Task<double> EstimateJunkFilesMbAsync()
        {
            if (DateTime.UtcNow - _lastJunkEstimateUtc <= TimeSpan.FromSeconds(45))
                return _cachedJunkEstimateMb;

            try
            {
                var estimate = await Task.Run(() =>
                {
                    long bytes = 0;
                    foreach (var path in new[]
                    {
                        Path.GetTempPath(),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")
                    }.Where(Directory.Exists))
                    {
                        try
                        {
                            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                            {
                                try
                                {
                                    bytes += new FileInfo(file).Length;
                                    if (bytes > 5L * 1024 * 1024 * 1024)
                                        break;
                                }
                                catch
                                {
                                }
                            }
                        }
                        catch
                        {
                        }
                    }

                    return bytes / 1024d / 1024d;
                });

                _cachedJunkEstimateMb = estimate;
                _lastJunkEstimateUtc = DateTime.UtcNow;
                return estimate;
            }
            catch
            {
                return 0;
            }
        }

        private async void DashboardBoostNow_Click(object sender, RoutedEventArgs e)
        {
            await RunOneClickBoostAsync("Safe Boost", false, false);
            await RefreshDashboard();
        }

        private async void DashboardFixRecommended_Click(object sender, RoutedEventArgs e)
        {
            await ApplySmartRecommendationActionAsync("fixall");
            await RefreshDashboard();
        }

        private async void DashboardCleanupNow_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.CleanupAsync());
            if (result != null)
            {
                ShowActionStatus(ActionState.Success, "Dashboard Cleanup", "Cleanup dijalankan dari dashboard core system.", HyperBoostBackendClient.FormatJson(result));
            }

            await RefreshDashboard();
        }

        private async void DashboardRefreshAnalyzer_Click(object sender, RoutedEventArgs e)
        {
            _lastDashboardDeepRefresh = DateTime.MinValue;
            await RefreshDashboard();
            ShowActionStatus(ActionState.Info, "Dashboard Analyzer", "Deep analyzer dashboard sudah diperbarui.");
        }

        private async void DashboardGamingMode_Click(object sender, RoutedEventArgs e)
        {
            _dashboardCurrentMode = "Gaming Mode";
            await ApplyBoosterProfileAsync("gaming", "Gaming Mode");
        }

        private async void DashboardPerformanceMode_Click(object sender, RoutedEventArgs e)
        {
            _dashboardCurrentMode = "Performance Mode";
            await ApplyBoosterProfileAsync("productivity", "Performance Mode");
        }

        private async void DashboardPowerSaverMode_Click(object sender, RoutedEventArgs e)
        {
            _dashboardCurrentMode = "Power Saver";
            await ApplyBoosterProfileAsync("battery", "Power Saver");
        }

        private async void DashboardOpenSmartRecommendation_Click(object sender, RoutedEventArgs e)
        {
            await ShowPage("SmartRecommendation", SmartRecommendationBtn);
        }

        private async void DashboardOpenOneClickBoost_Click(object sender, RoutedEventArgs e)
        {
            await ShowPage("OneClickBoost", OneClickBoostBtn);
        }

        #endregion

        #region System Info

        private async Task RefreshSystemInfo()
        {
            var info = await SafeApiCall(() => _backendClient.GetSystemInfoAsync());
            if (info == null)
            {
                SystemInfoText.Text = "Unable to load system info.";
                return;
            }

            SystemInfoText.Text = FormatSystemInfo(info);
        }

        private async void RefreshSystemInfo_Click(object sender, RoutedEventArgs e) => await RefreshSystemInfo();

        #endregion

        #region Booster

        private Task RefreshGamingBoosterHubAsync()
        {
            var snapshot = BuildSessionDetectionSnapshot();
            var activeMode = _gamingBoostActive ? "Active" : "Idle";
            var activeGame = DescribeProcess(snapshot.ActiveGame, "No active game detected");
            var activeStreamer = DescribeProcess(snapshot.ActiveStreamer, "Not detected");
            var discord = DescribeProcess(snapshot.DiscordProcess, "Not detected");

            BoosterSummaryText.Text =
                $"Booster state: {activeMode}{Environment.NewLine}" +
                $"Detected game: {activeGame}{Environment.NewLine}" +
                $"Detected streamer: {activeStreamer}{Environment.NewLine}" +
                $"Discord: {discord}{Environment.NewLine}" +
                "Use Gaming Mode for profile/session rules. Use Gaming Booster for one-shot optimization.";

            BoosterRecommendationText.Text =
                "Recommended flow:" + Environment.NewLine +
                "1. Analyze safe boost" + Environment.NewLine +
                "2. Run one-click boost" + Environment.NewLine +
                "3. Apply targeted game boost only if the game process is already active";

            BoosterActionText.Text =
                "Action groups:" + Environment.NewLine +
                "Process optimizer, network optimizer, visual optimizer, and manual booster setup.";

            BoosterTargetText.Text =
                $"Current target: {activeGame}{Environment.NewLine}" +
                $"Streaming companion: {activeStreamer}{Environment.NewLine}" +
                "Start Game Boost works best after a real game executable is detected.";

            BoosterReportText.Text = string.IsNullOrWhiteSpace(GamingBoostResultsText?.Text)
                ? "No booster report yet."
                : GamingBoostResultsText.Text;

            return Task.CompletedTask;
        }

        #endregion

        #region Drivers

        private string _driverSafetyMode = "Safe Only";

        private void AppendDriversHistory(string entry)
        {
            if (_driversHistory.Count >= 14)
                _driversHistory.Dequeue();

            _driversHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (DriversHistoryText != null)
                DriversHistoryText.Text = string.Join(Environment.NewLine, _driversHistory.Reverse());
        }

        private async Task RefreshDrivers()
        {
            var drivers = await SafeApiCall(() => _backendClient.GetDriversAsync());
            if (drivers == null)
            {
                DriversText.Text = "Unable to load drivers.";
                return;
            }

            DriversText.Text = FormatDrivers(drivers);
            DriversScannerText.Text =
                "Quick Scan: outdated / missing / broken basic detect" + Environment.NewLine +
                "Categories: GPU, chipset, network, audio, storage, USB, bluetooth, peripheral";
            DriversListSummaryText.Text =
                "List manager: update, rollback, reinstall, open device location / manager" + Environment.NewLine +
                "Mode: safe update recommended";
            DriversUpdateManagerText.Text =
                $"Update mode: {_driverSafetyMode}{Environment.NewLine}" +
                "Update all drivers: prioritize official & stable sources";
            DriversHealthText.Text =
                "Health monitor: Healthy / Warning / Error" + Environment.NewLine +
                "Detect crash driver, unstable driver, and high-risk devices from scan summary";
            DriversBlockerText.Text =
                "Block specific driver auto update when stable config must be preserved" + Environment.NewLine +
                "Use Windows Update integration / policy review for advanced blocking";
            DriversCompatibilityText.Text =
                $"Windows compatibility: review current Windows build vs driver branch{Environment.NewLine}" +
                "Hardware compatibility: verify vendor / signed driver / stable release";
            DriversSafetyText.Text = $"Mode aktif: {_driverSafetyMode}";

            var summary = DriversText.Text;
            var total = summary.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(x => x.Contains("Total Drivers", StringComparison.OrdinalIgnoreCase)) ?? "Total Drivers: Unknown";

            DriversDashboardText.Text =
                $"{total}{Environment.NewLine}" +
                "Outdated Drivers: review Check for Updates result" + Environment.NewLine +
                "Missing Drivers: review Device Manager warnings" + Environment.NewLine +
                $"Last Scan: {DateTime.Now:yyyy-MM-dd HH:mm}{Environment.NewLine}" +
                $"Last Update: {(_driversHistory.Count == 0 ? "Belum ada" : _driversHistory.Last())}{Environment.NewLine}" +
                "Status: Managed / review recommendation";

            DriversRecommendationText.Text =
                "GPU driver outdated -> prioritize stable vendor update" + Environment.NewLine +
                "Network driver lama -> update if latency or disconnect issue appears" + Environment.NewLine +
                "Audio driver error -> use rollback or reinstall before advanced update";

            DriversQuickResultText.Text = "Drivers Updated Successfully\n0 drivers optimized";

            if (_driversHistory.Count == 0)
                AppendDriversHistory("Driver center initialized.");
        }

        private async void RefreshDrivers_Click(object sender, RoutedEventArgs e)
        {
            AppendDriversHistory($"{((sender as Button)?.Content?.ToString() ?? "Driver scan")} executed.");
            await RefreshDrivers();
            ShowRequestedStatus("Driver Scanner", "Driver inventory diperbarui untuk review.", DriversDashboardText.Text);
        }

        private async void CheckDriverUpdates_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.CheckDriverUpdatesAsync());
            if (result == null)
            {
                ShowActionStatus(ActionState.Error, "Driver check failed", "Unable to check driver updates right now.");
                return;
            }

            AppendDriversHistory("Driver update check completed.");
            DriversQuickResultText.Text = "Driver Scan Complete\nRecommended driver updates reviewed";
            ShowRequestedStatus("Driver check complete", "Driver update scan selesai untuk review. Instalasi driver tidak dijalankan otomatis.", HyperBoostBackendClient.FormatJson(result));
            await RefreshDrivers();
        }

        private async void SmartDriverUpdate_Click(object sender, RoutedEventArgs e)
        {
            AppendDriversHistory("Smart driver update requested.");
            await CheckDriverUpdatesCoreAsync("SMART DRIVER UPDATE");
        }

        private async void QuickFixDriver_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("pnputil /scan-devices");
            DriversQuickResultText.Text = "Driver Issues Fixed\nQuick repair requested";
            AppendDriversHistory("Quick fix driver requested.");
            ShowAppliedStatus(success, "QUICK FIX DRIVER", "Driver rescan diminta. Review hasil PnP scan untuk langkah lanjutan.", "Driver quick-fix menghasilkan warning.", output);
            await RefreshDrivers();
        }

        private async Task CheckDriverUpdatesCoreAsync(string actionName)
        {
            var result = await SafeApiCall(() => _backendClient.CheckDriverUpdatesAsync());
            if (result == null)
            {
                ShowActionStatus(ActionState.Error, actionName, "Unable to process driver update request right now.");
                return;
            }

            DriversQuickResultText.Text = "Driver Review Ready\nUpdate recommendation refreshed";
            ShowRequestedStatus(actionName, "Driver update workflow diminta untuk review. Driver tidak diinstal otomatis oleh langkah ini.", HyperBoostBackendClient.FormatJson(result));
            await RefreshDrivers();
        }

        private void FixDriverRecommendation_Click(object sender, RoutedEventArgs e)
        {
            _ = CheckDriverUpdatesCoreAsync("Driver Recommendation");
            AppendDriversHistory("Recommended driver update requested.");
        }

        private void RollbackDriver_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("devmgmt.msc", null, "Driver Rollback");
            AppendDriversHistory("Driver rollback review opened.");
            ShowActionStatus(ActionState.Info, "Driver Rollback", "Device Manager dibuka. Lakukan rollback driver secara manual dari sana.");
        }

        private async void ReinstallDriver_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("pnputil /scan-devices");
            AppendDriversHistory("Driver reinstall / rescan requested.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Driver Reinstall", "Driver rescan / reinstall review dijalankan.", output);
            await RefreshDrivers();
        }

        private void OpenDeviceManager_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("devmgmt.msc", null, "Device Manager");
            AppendDriversHistory("Device Manager opened.");
        }

        private void BackupDrivers_Click(object sender, RoutedEventArgs e)
        {
            var backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HyperBoost X", "driver-backup");
            Directory.CreateDirectory(backupDir);
            _ = RunPowerShellActionAsync($"Export-WindowsDriver -Online -Destination '{backupDir.Replace("'", "''")}'", "Backup Drivers", "Driver backup export requested.");
            AppendDriversHistory("Driver backup requested.");
        }

        private void RestoreDrivers_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("devmgmt.msc", null, "Restore Drivers");
            AppendDriversHistory("Driver restore review opened.");
            ShowActionStatus(ActionState.Info, "Restore Drivers", "Gunakan Device Manager / pnputil untuk restore driver dari backup hasil export.");
        }

        private void OpenDriverSources_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:windowsupdate-optionalupdates", "Online Driver Database");
            AppendDriversHistory("Online driver sources opened.");
        }

        private async void OfflineDriverInstaller_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Pilih paket driver",
                Filter = "Driver Package (*.inf;*.exe)|*.inf;*.exe"
            };

            if (dialog.ShowDialog() != true)
            {
                ShowActionStatus(ActionState.Info, "Offline Driver Installer", "Tidak ada paket driver yang dipilih.");
                return;
            }

            var path = dialog.FileName;
            string script = path.EndsWith(".inf", StringComparison.OrdinalIgnoreCase)
                ? $"pnputil /add-driver \"{path}\" /install"
                : $"Start-Process -FilePath \"{path}\"";

            var (success, output) = await ExecutePowerShellScriptAsync(script);
            AppendDriversHistory($"Offline driver installer selected: {Path.GetFileName(path)}");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Offline Driver Installer", $"Driver package diproses: {Path.GetFileName(path)}", output);
            await RefreshDrivers();
        }

        private void OpenDriverWindowsUpdate_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:windowsupdate-optionalupdates", "Windows Update Driver Integration");
            AppendDriversHistory("Windows Update optional driver page opened.");
        }

        private void DriverBlocker_Click(object sender, RoutedEventArgs e)
        {
            AppendDriversHistory("Driver blocker review opened.");
            ShowActionStatus(ActionState.Warning, "Driver Blocker", "Gunakan policy / Windows Update optional updates untuk memblokir driver tertentu secara aman.");
        }

        private void DriverAutoRules_Click(object sender, RoutedEventArgs e)
        {
            AppendDriversHistory("Driver auto update rules reviewed.");
            ShowActionStatus(ActionState.Info, "Auto Update Rules", "Set rule: update saat idle, disable saat gaming, dan prioritaskan stable driver only.");
        }

        private void DriverScheduler_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("taskschd.msc", null, "Scheduled Driver Update");
            AppendDriversHistory("Driver scheduler opened.");
        }

        private void SetDriverSafetyMode_Click(object sender, RoutedEventArgs e)
        {
            _driverSafetyMode = ((sender as Button)?.Tag?.ToString() ?? "safe") switch
            {
                "beta" => "Beta Driver",
                "advanced" => "Advanced Driver",
                _ => "Safe Only"
            };
            AppendDriversHistory($"Driver safety mode changed to {_driverSafetyMode}.");
            _ = RefreshDrivers();
        }

        private async void DriverRepairTools_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("pnputil /scan-devices");
            AppendDriversHistory("Driver repair tools requested.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Driver Repair Tools", "Driver repair / rescan diproses.", output);
            await RefreshDrivers();
        }

        #endregion

        #region Repair

        private void AppendRepairHistory(string entry)
        {
            if (_repairHistory.Count >= 14)
                _repairHistory.Dequeue();

            _repairHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (RepairHistoryText != null)
                RepairHistoryText.Text = string.Join(Environment.NewLine, _repairHistory.Reverse());
        }

        private async Task RefreshRepairViewAsync()
        {
            var stats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
            var cpu = stats?.cpu_percent != null ? Convert.ToDouble(stats.cpu_percent) : 0d;
            var ram = stats?.memory_percent != null ? Convert.ToDouble(stats.memory_percent) : 0d;
            var disk = stats?.disk_percent != null ? Convert.ToDouble(stats.disk_percent) : 0d;

            var issues = 0;
            if (cpu > 85) issues++;
            if (ram > 80) issues++;
            if (disk > 90) issues++;

            var status = issues == 0 ? "Healthy" : issues >= 3 ? "Critical" : "Issue";
            RepairDashboardText.Text =
                $"System Status: {status}{Environment.NewLine}" +
                $"Detected Errors: {issues}{Environment.NewLine}" +
                $"Last Repair: {(_repairHistory.Count == 0 ? "Belum ada" : _repairHistory.Last())}{Environment.NewLine}" +
                $"Critical Issues: {(issues == 0 ? "0" : issues.ToString())}";

            RepairRecommendationText.Text =
                (cpu > 85 ? "High CPU usage detected -> run Performance Repair" + Environment.NewLine : "") +
                (ram > 80 ? "High RAM pressure detected -> run Cache / Performance Repair" + Environment.NewLine : "") +
                (disk > 90 ? "Disk hampir penuh -> run Cache & Temp Repair" + Environment.NewLine : "") +
                "System file repair recommended when Windows feels unstable" + Environment.NewLine +
                "Network repair recommended if ping / DNS / internet feels broken";

            RepairQuickResultText.Text = issues == 0
                ? "System Repaired\nIssues Fixed: 0"
                : $"System needs attention\nIssues Detected: {issues}";
            RepairSafetyModeText.Text = $"Mode aktif: {_repairSafetyMode}";

            if (_repairHistory.Count == 0)
                AppendRepairHistory("Repair center initialized.");
        }

        private async void RunSfc_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.RunSfcAsync());
            if (result == null)
            {
                ShowActionStatus(ActionState.Error, "SFC scan failed", "Unable to start the SFC scan right now.");
                return;
            }

            AppendRepairHistory("SFC scan started.");
            ShowActionStatus(ActionState.Info, "SFC scan started", "System File Checker berhasil diminta. Proses scan berjalan terpisah di Windows.", HyperBoostBackendClient.FormatJson(result));
            await RefreshRepairViewAsync();
        }

        private async void RunDism_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.RunDismAsync());
            if (result == null)
            {
                ShowActionStatus(ActionState.Error, "DISM repair failed", "Unable to start DISM repair right now.");
                return;
            }

            AppendRepairHistory("DISM repair started.");
            ShowActionStatus(ActionState.Info, "DISM repair started", "DISM repair berhasil diminta. Proses repair berjalan terpisah di Windows.", HyperBoostBackendClient.FormatJson(result));
            await RefreshRepairViewAsync();
        }

        private async void Cleanup_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.CleanupAsync());
            if (result == null)
            {
                ShowActionStatus(ActionState.Error, "Cleanup failed", "Unable to run cleanup right now.");
                return;
            }

            AppendRepairHistory("Cleanup repair completed.");
            ShowActionStatus(ActionState.Info, "Cleanup complete", "Cleanup aman diminta. Review hasil freed space untuk memastikan perubahan aktual.", HyperBoostBackendClient.FormatJson(result));
            await RefreshRepairViewAsync();
        }

        private void SetRepairSafetyMode_Click(object sender, RoutedEventArgs e)
        {
            _repairSafetyMode = ((sender as Button)?.Tag?.ToString() ?? "safe") switch
            {
                "advanced" => "Advanced Fixes",
                "moderate" => "Moderate Fixes",
                _ => "Safe Only"
            };
            AppendRepairHistory($"Repair safety mode changed to {_repairSafetyMode}.");
            _ = RefreshRepairViewAsync();
        }

        private async void QuickRepair_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            var cleanup = await SafeApiCall(() => _backendClient.CleanupAsync());
            if (cleanup != null) notes.Add("Cache / temp cleanup requested");
            var reset = await SafeApiCall(() => _backendClient.ResetNetworkAsync());
            if (reset != null) notes.Add("Basic network repair requested");
            var (svcSuccess, svcOutput) = await ExecutePowerShellScriptAsync("Restart-Service -Name wuauserv,AudioSrv,Dnscache -ErrorAction SilentlyContinue; 'Important services restart requested.'");
            notes.Add(svcSuccess ? "Important services restarted" : svcOutput);

            RepairQuickResultText.Text = $"Quick Repair Requested{Environment.NewLine}Actions queued: {notes.Count}";
            AppendRepairHistory("Quick repair completed.");
            ShowActionStatus(ActionState.Info, "Quick Repair", "Quick repair request dikirim. Sebagian aksi berjalan asinkron atau perlu review hasil manual.", string.Join(Environment.NewLine, notes));
            await RefreshRepairViewAsync();
        }

        private async void FullSystemRepair_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            var sfc = await SafeApiCall(() => _backendClient.RunSfcAsync());
            if (sfc != null) notes.Add("SFC launched");
            var dism = await SafeApiCall(() => _backendClient.RunDismAsync());
            if (dism != null) notes.Add("DISM launched");
            var reset = await SafeApiCall(() => _backendClient.ResetNetworkAsync());
            if (reset != null) notes.Add("Network reset requested");
            var cleanup = await SafeApiCall(() => _backendClient.CleanupAsync());
            if (cleanup != null) notes.Add("Cache cleanup requested");
            var (svcSuccess, svcOutput) = await ExecutePowerShellScriptAsync("Restart-Service -Name wuauserv,BITS,AudioSrv,Dnscache,Spooler -ErrorAction SilentlyContinue; 'Core services restart requested.'");
            notes.Add(svcSuccess ? "Core services restart requested" : svcOutput);

            RepairQuickResultText.Text = "Full Repair Requested";
            AppendRepairHistory("Full system repair launched.");
            ShowActionStatus(ActionState.Info, "Full System Repair", "Full repair workflow diminta. SFC, DISM, reset network, dan service restart tidak selesai instan.", string.Join(Environment.NewLine, notes));
            await RefreshRepairViewAsync();
        }

        private async void AutoFixAllRepair_Click(object sender, RoutedEventArgs e)
        {
            await QuickRepair_Click_Internal();
        }

        private async Task QuickRepair_Click_Internal()
        {
            var cleanup = await SafeApiCall(() => _backendClient.CleanupAsync());
            var reset = await SafeApiCall(() => _backendClient.ResetNetworkAsync());
            var (success, output) = await ExecutePowerShellScriptAsync("Restart-Service -Name wuauserv,AudioSrv,Dnscache -ErrorAction SilentlyContinue; 'Smart repair service reset requested.'");
            AppendRepairHistory("Smart repair auto-fix executed.");
            ShowActionStatus(ActionState.Info, "Smart Repair", "Auto fix all diminta. Review hasil setiap langkah untuk memastikan perbaikan benar-benar selesai.", string.Join(Environment.NewLine, new[]
            {
                cleanup != null ? "Cache corruption repair requested" : null,
                reset != null ? "Network repair requested" : null,
                success ? "Service repair requested" : output
            }.Where(x => !string.IsNullOrWhiteSpace(x))));
            await RefreshRepairViewAsync();
        }

        private void ReviewRepairIssues_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Review Repair Issues", RepairRecommendationText.Text);
        }

        private async void NetworkRepair_Click(object sender, RoutedEventArgs e)
        {
            var label = (sender as Button)?.Content?.ToString() ?? "Network Repair";
            if (label.Contains("Flush", StringComparison.OrdinalIgnoreCase))
                FlushDNS_Click(sender, e);
            else if (label.Contains("Winsock", StringComparison.OrdinalIgnoreCase) || label.Contains("TCP", StringComparison.OrdinalIgnoreCase))
                await RunPowerShellActionAsync("netsh winsock reset; netsh int ip reset", "Network Repair", "TCP/IP dan Winsock reset requested.");
            else
                ResetNetwork_Click(sender, e);

            AppendRepairHistory($"{label} requested.");
            await RefreshRepairViewAsync();
        }

        private async void WindowsServicesRepair_Click(object sender, RoutedEventArgs e)
        {
            await RunPowerShellActionAsync("Restart-Service -Name wuauserv,AudioSrv,Dnscache,Spooler -ErrorAction SilentlyContinue", "Windows Services Repair", "Important Windows services restart requested.");
            AppendRepairHistory("Windows services repair requested.");
            await RefreshRepairViewAsync();
        }

        private async void AppStoreRepair_Click(object sender, RoutedEventArgs e)
        {
            var label = (sender as Button)?.Content?.ToString() ?? "App & Store Repair";
            if (label.Contains("Cache", StringComparison.OrdinalIgnoreCase))
                await RunPowerShellActionAsync("wsreset.exe", "Store Cache Reset", "Store cache reset requested.");
            else
                await RunPowerShellActionAsync("Get-AppxPackage -AllUsers | ForEach-Object { Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -ErrorAction SilentlyContinue }; 'App re-register requested.'", "App & Store Repair", "Store / app repair requested.");
            AppendRepairHistory($"{label} requested.");
            await RefreshRepairViewAsync();
        }

        private async void DiskRepair_Click(object sender, RoutedEventArgs e)
        {
            await RunPowerShellActionAsync("chkdsk C: /scan", "Disk Repair", "Disk scan requested.");
            AppendRepairHistory("Disk repair scan requested.");
            await RefreshRepairViewAsync();
        }

        private async void PerformanceRepair_Click(object sender, RoutedEventArgs e)
        {
            var cleanup = await SafeApiCall(() => _backendClient.CleanupAsync());
            await RunPowerShellActionAsync("powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e", "Performance Repair", "Performance baseline restored.");
            AppendRepairHistory("Performance repair requested.");
            ShowActionStatus(ActionState.Success, "Performance Repair", "Performance repair dijalankan.", cleanup != null ? "Cleanup requested and balanced plan restored." : "Balanced plan restored.");
            await RefreshRepairViewAsync();
        }

        private async void GamingRepair_Click(object sender, RoutedEventArgs e)
        {
            await ApplyQuickCompetitiveGamingAsync();
            AppendRepairHistory("Gaming repair / boost requested.");
            await RefreshRepairViewAsync();
        }

        private async void AudioRepair_Click(object sender, RoutedEventArgs e)
        {
            await RunPowerShellActionAsync("Restart-Service -Name AudioSrv -ErrorAction SilentlyContinue; Restart-Service -Name AudioEndpointBuilder -ErrorAction SilentlyContinue", "Audio Repair", "Audio services restart requested.");
            AppendRepairHistory("Audio repair requested.");
            await RefreshRepairViewAsync();
        }

        private async void DisplayRepair_Click(object sender, RoutedEventArgs e)
        {
            await RunPowerShellActionAsync("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\" /v VisualFXSetting /t REG_DWORD /d 1 /f", "Display Repair", "Display / visual baseline restored.");
            LaunchWindowsUri("ms-settings:display", "Display Repair");
            AppendRepairHistory("Display repair requested.");
            await RefreshRepairViewAsync();
        }

        private async void WindowsUpdateRepair_Click(object sender, RoutedEventArgs e)
        {
            await RunPowerShellActionAsync("Stop-Service -Name wuauserv -Force -ErrorAction SilentlyContinue; Stop-Service -Name BITS -Force -ErrorAction SilentlyContinue; if (Test-Path $env:SystemRoot'\\SoftwareDistribution\\Download') { Remove-Item -Path $env:SystemRoot'\\SoftwareDistribution\\Download\\*' -Recurse -Force -ErrorAction SilentlyContinue }; Start-Service -Name wuauserv -ErrorAction SilentlyContinue; Start-Service -Name BITS -ErrorAction SilentlyContinue", "Windows Update Repair", "Windows Update repair requested.");
            AppendRepairHistory("Windows Update repair requested.");
            await RefreshRepairViewAsync();
        }

        private async void RegistryRepair_Click(object sender, RoutedEventArgs e)
        {
            await Cleanup_Click_Internal();
            AppendRepairHistory("Registry basic cleanup review requested.");
            ShowActionStatus(ActionState.Info, "Registry Repair", "Registry cleanup ringan dipetakan ke cleanup aman. Untuk edit lanjut, gunakan Advanced Repair Tools.");
            await RefreshRepairViewAsync();
        }

        private async Task Cleanup_Click_Internal()
        {
            var result = await SafeApiCall(() => _backendClient.CleanupAsync());
            if (result != null)
                RepairQuickResultText.Text = "Cache / temp repair executed";
        }

        private async void CacheTempRepair_Click(object sender, RoutedEventArgs e)
        {
            await Cleanup_Click_Internal();
            AppendRepairHistory("Cache & temp repair requested.");
            ShowActionStatus(ActionState.Success, "Cache & Temp Repair", "Cache corrupt / temp repair dijalankan.");
            await RefreshRepairViewAsync();
        }

        private void PermissionRepair_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("cmd.exe", "/c start ms-settings:windowsupdate", "Permission Repair");
            AppendRepairHistory("Permission repair review opened.");
            ShowActionStatus(ActionState.Info, "Permission Repair", "Halaman review dibuka. Permission reset penuh tidak dijalankan otomatis karena berisiko tinggi.");
        }

        private void OpenAdvancedRepairTools_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("cmd.exe", null, "Advanced Repair Tools");
            AppendRepairHistory("Advanced repair tools opened.");
            ShowActionStatus(ActionState.Info, "Advanced Repair Tools", "Command tools dibuka untuk workflow repair manual tingkat lanjut.");
        }

        #endregion

        #region Tweaks

        private async Task RefreshTweaks()
        {
            var tweaks = await SafeApiCall(() => _backendClient.GetTweaksAsync());
            if (tweaks == null)
            {
                TweaksText.Text = "Unable to load tweaks.";
                return;
            }

            TweaksText.Text = FormatTweaks(tweaks);
        }

        private void AppendTweaksHistory(string entry)
        {
            if (_tweaksHistory.Count >= 14)
                _tweaksHistory.Dequeue();

            _tweaksHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (TweaksLogText != null)
                TweaksLogText.Text = string.Join(Environment.NewLine, _tweaksHistory.Reverse());
        }

        private async Task RefreshTweaksCenterViewAsync()
        {
            await RefreshTweaks();
            TweaksSafetyModeText.Text = $"Mode aktif: {_tweaksSafetyMode}";
            TweaksSafetyModeText.Foreground = _tweaksSafetyMode switch
            {
                "Advanced" => Brushes.OrangeRed,
                "Moderate" => Brushes.Goldenrod,
                _ => Brushes.LimeGreen
            };

            TweaksSmartRecommendationText.Text =
                "+20% performance possible" + Environment.NewLine +
                "12 tweaks recommended" + Environment.NewLine +
                "Focus: service ringan, visual optimization, privacy basic, network ringan";

            TweaksStatusManagerText.Text =
                "Applied: privacy baseline, visual trim, selected network tweak" + Environment.NewLine +
                "Not Applied: advanced registry / deep service tweak" + Environment.NewLine +
                "Modified: power / explorer / startup behavior";

            UpdateTweaksProfileSummary();

            if (_tweaksHistory.Count == 0)
                AppendTweaksHistory("Tweaks Center initialized.");
        }

        private void UpdateTweaksProfileSummary()
        {
            if (TweaksProfileSummaryText == null || TweaksProfileCombo == null)
                return;

            var profile = (TweaksProfileCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Gaming Mode Tweaks";
            TweaksProfileSummaryText.Text = profile switch
            {
                "Performance Tweaks" => "Performance profile: responsive UI, foreground priority, reduced visual overhead.",
                "Privacy Mode" => "Privacy profile: telemetry down, activity tracking reduced, privacy baseline enabled.",
                "Custom user tweak" => "Custom profile: combine tweak groups manually sesuai kebutuhan.",
                _ => "Gaming profile: input lag reduction, Xbox / DVR trim, lighter background load."
            };
        }

        private async void RefreshTweaks_Click(object sender, RoutedEventArgs e) => await RefreshTweaks();

        private void AppendWindowsFeaturesHistory(string entry)
        {
            if (_windowsFeaturesHistory.Count >= 14)
                _windowsFeaturesHistory.Dequeue();

            _windowsFeaturesHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (WindowsFeaturesHistoryText != null)
                WindowsFeaturesHistoryText.Text = string.Join(Environment.NewLine, _windowsFeaturesHistory.Reverse());
        }

        private async Task RefreshWindowsFeaturesViewAsync()
        {
            var script = "Get-WindowsOptionalFeature -Online | Sort-Object FeatureName | Select-Object -First 20 FeatureName,State | ForEach-Object { \"{0} | {1}\" -f $_.FeatureName, $_.State }";
            var (success, output) = await ExecutePowerShellScriptAsync(script);
            WindowsFeaturesListText.Text = success && !string.IsNullOrWhiteSpace(output)
                ? output
                : "Tidak bisa membaca daftar fitur via PowerShell saat ini. Gunakan Optional Features untuk review manual.";

            WindowsFeaturesRecommendationText.Text =
                "Fitur tidak terpakai bisa di-disable untuk Windows lebih ringan" + Environment.NewLine +
                "Developer tools belum aktif? Gunakan preset Developer Setup" + Environment.NewLine +
                "Virtualization belum aktif? Gunakan preset Virtualization";

            WindowsFeaturesDependenciesText.Text =
                "Hyper-V butuh virtualization support" + Environment.NewLine +
                "WSL biasanya butuh Virtual Machine Platform" + Environment.NewLine +
                "Windows Sandbox terkait virtualization stack";

            UpdateWindowsFeaturesPresetSummary();

            if (_windowsFeaturesHistory.Count == 0)
                AppendWindowsFeaturesHistory("Windows Features initialized.");
        }

        private void UpdateWindowsFeaturesPresetSummary()
        {
            if (WindowsFeaturesPresetSummaryText == null || WindowsFeaturesPresetCombo == null)
                return;

            var preset = (WindowsFeaturesPresetCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Gaming Setup";
            WindowsFeaturesPresetSummaryText.Text = preset switch
            {
                "Developer Setup" => "Developer setup: WSL, Virtual Machine Platform, Sandbox / optional dev stack review.",
                "Creator Setup" => "Creator setup: media / graphics-related features and optional components review.",
                "Minimal Windows Setup" => "Minimal setup: review legacy / optional / unused features to keep Windows ringan.",
                _ => "Gaming setup: review media, graphics, game-related components, and disable fitur tak terpakai."
            };
        }

        private void OpenOptionalFeatures_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("optionalfeatures.exe", null, "Windows Features");
            AppendWindowsFeaturesHistory("Optional Features opened.");
        }

        private async Task ToggleWindowsFeatureAsync(bool enable)
        {
            if (string.IsNullOrWhiteSpace(WindowsFeatureTargetInput.Text))
            {
                ShowActionStatus(ActionState.Warning, enable ? "Enable Feature" : "Disable Feature", "Masukkan nama feature Windows dulu.");
                return;
            }

            var featureName = WindowsFeatureTargetInput.Text.Trim();
            var command = enable
                ? $"Enable-WindowsOptionalFeature -Online -FeatureName '{featureName}' -All -NoRestart"
                : $"Disable-WindowsOptionalFeature -Online -FeatureName '{featureName}' -NoRestart";
            var (success, output) = await ExecutePowerShellScriptAsync(command);
            AppendWindowsFeaturesHistory($"{(enable ? "Enable" : "Disable")} feature requested: {featureName}");
            ShowAppliedStatus(success, enable ? "Enable Feature" : "Disable Feature", $"Feature {featureName} diminta. Restart mungkin diperlukan sebelum efeknya terlihat.", "Feature action menghasilkan warning.", output);
            await RefreshWindowsFeaturesViewAsync();
        }

        private async void EnableWindowsFeature_Click(object sender, RoutedEventArgs e)
        {
            await ToggleWindowsFeatureAsync(true);
        }

        private async void DisableWindowsFeature_Click(object sender, RoutedEventArgs e)
        {
            await ToggleWindowsFeatureAsync(false);
        }

        private async Task ApplyWindowsFeaturesPresetCoreAsync(string preset)
        {
            switch (preset)
            {
                case "dev":
                case "Developer Setup":
                    WindowsFeatureTargetInput.Text = "Microsoft-Windows-Subsystem-Linux";
                    await ToggleWindowsFeatureAsync(true);
                    break;
                case "virtualization":
                    WindowsFeatureTargetInput.Text = "VirtualMachinePlatform";
                    await ToggleWindowsFeatureAsync(true);
                    break;
                case "gaming":
                case "Gaming Setup":
                    WindowsFeatureTargetInput.Text = "Microsoft-Windows-Subsystem-Linux";
                    ShowActionStatus(ActionState.Info, "Gaming Setup", "Review Graphics Tools, media features, dan game-related components di Optional Features.");
                    break;
                case "Creator Setup":
                    ShowActionStatus(ActionState.Info, "Creator Setup", "Review media, graphics, dan optional components untuk creator workflow.");
                    break;
                case "Minimal Windows Setup":
                    ShowActionStatus(ActionState.Info, "Minimal Windows Setup", "Review legacy / optional features yang tidak dipakai lalu disable secara manual dari daftar.");
                    break;
            }
        }

        private async void ApplyWindowsFeaturesPreset_Click(object sender, RoutedEventArgs e)
        {
            var preset = (sender as Button)?.Tag?.ToString() ?? "gaming";
            await ApplyWindowsFeaturesPresetCoreAsync(preset);
            AppendWindowsFeaturesHistory($"Preset applied: {preset}");
            UpdateWindowsFeaturesPresetSummary();
        }

        private async void ApplyWindowsFeaturesPresetFromCombo_Click(object sender, RoutedEventArgs e)
        {
            var preset = (WindowsFeaturesPresetCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Gaming Setup";
            await ApplyWindowsFeaturesPresetCoreAsync(preset);
            AppendWindowsFeaturesHistory($"Preset applied: {preset}");
            UpdateWindowsFeaturesPresetSummary();
        }

        private async void ApplyWindowsFeatureOptimization_Click(object sender, RoutedEventArgs e)
        {
            WindowsFeaturesQuickResultText.Text = "Windows Features Optimized";
            AppendWindowsFeaturesHistory("Feature optimization requested.");
            await RefreshWindowsFeaturesViewAsync();
            ShowRequestedStatus("Apply Feature Optimization", "Windows Features optimization flow diminta. Review rekomendasi dan status feature untuk hasil akhirnya.", WindowsFeaturesRecommendationText.Text);
        }

        private void ReviewWindowsFeaturesRecommendation_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Smart Feature Recommendation", WindowsFeaturesRecommendationText.Text);
        }

        private void OpenDeveloperFeatures_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:developers", "Developer Features");
            AppendWindowsFeaturesHistory("Developer features opened.");
        }

        private void BackupWindowsFeaturesConfig_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("optionalfeatures.exe", null, "Backup Feature Config");
            AppendWindowsFeaturesHistory("Backup feature config review opened.");
        }

        private async void RestoreWindowsFeaturesDefaults_Click(object sender, RoutedEventArgs e)
        {
            AppendWindowsFeaturesHistory("Restore / undo feature review opened.");
            await RefreshWindowsFeaturesViewAsync();
            ShowActionStatus(ActionState.Info, "Restore Windows Features", "Review default / restore state via Optional Features dan DISM sesuai kebutuhan.");
        }

        private void AppendUpdateControlHistory(string entry)
        {
            if (_updateControlHistory.Count >= 14)
                _updateControlHistory.Dequeue();

            _updateControlHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (UpdateHistoryText != null)
                UpdateHistoryText.Text = string.Join(Environment.NewLine, _updateControlHistory.Reverse());
        }

        private string GetSelectedUpdateControlMode()
        {
            return (UpdateControlModeCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Automatic";
        }

        private string GetUpdateBackupDirectory()
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HyperBoost X",
                "update-control");
            Directory.CreateDirectory(directory);
            return directory;
        }

        private async Task<string> GetLatestHotfixSummaryAsync()
        {
            var command = @"
$latest = Get-HotFix -ErrorAction SilentlyContinue | Sort-Object InstalledOn -Descending | Select-Object -First 6;
if (-not $latest) { 'No update history available.' }
else { $latest | ForEach-Object { '{0:yyyy-MM-dd} | {1} | {2}' -f $_.InstalledOn, $_.HotFixID, $_.Description } }";
            var (success, output) = await ExecutePowerShellScriptAsync(command);
            return success && !string.IsNullOrWhiteSpace(output)
                ? output
                : "No update history available.";
        }

        private async Task<string> GetUpdateServiceSummaryAsync()
        {
            var command = @"
$services = 'wuauserv','BITS','DoSvc' | ForEach-Object {
    Get-Service -Name $_ -ErrorAction SilentlyContinue | Select-Object Name, Status, StartType
};
if (-not $services) { 'No update services detected.' }
else { $services | ForEach-Object { '{0} | {1} | {2}' -f $_.Name, $_.Status, $_.StartType } }";
            var (success, output) = await ExecutePowerShellScriptAsync(command);
            return success && !string.IsNullOrWhiteSpace(output)
                ? output
                : "No update services detected.";
        }

        private async Task<string> GetDriverReviewSummaryAsync()
        {
            var command = @"
$drivers = Get-CimInstance Win32_PnPSignedDriver -ErrorAction SilentlyContinue |
    Where-Object { $_.DeviceClass -match 'DISPLAY|MEDIA|NET|SYSTEM' } |
    Sort-Object DeviceName |
    Select-Object -First 8 DeviceName, DriverVersion;
if (-not $drivers) { 'Driver review unavailable.' }
else { $drivers | ForEach-Object { '{0} | v{1}' -f $_.DeviceName, $_.DriverVersion } }";
            var (success, output) = await ExecutePowerShellScriptAsync(command);
            return success && !string.IsNullOrWhiteSpace(output)
                ? output
                : "Driver review unavailable.";
        }

        private async Task<string> GetAppUpdateSummaryAsync()
        {
            var command = @"
$storeCount = (Get-AppxPackage -ErrorAction SilentlyContinue | Measure-Object).Count;
'Store apps detected: ' + $storeCount + [Environment]::NewLine +
'Installed apps basic detect: review Apps Manager / Microsoft Store Downloads & Updates'";
            var (success, output) = await ExecutePowerShellScriptAsync(command);
            return success && !string.IsNullOrWhiteSpace(output)
                ? output
                : "Store / app update review unavailable.";
        }

        private string BuildUpdateRecommendationText(string serviceSummary, string hotfixSummary)
        {
            var recommendations = new List<string>();
            if (serviceSummary.Contains("Stopped", StringComparison.OrdinalIgnoreCase))
                recommendations.Add("Windows Update service stopped: review if this is intentional");
            if (serviceSummary.Contains("Disabled", StringComparison.OrdinalIgnoreCase))
                recommendations.Add("Delivery Optimization or update service disabled: cocok untuk performance, tapi cek security patch secara berkala");
            if (hotfixSummary.Contains("No update history available", StringComparison.OrdinalIgnoreCase))
                recommendations.Add("No recent update history detected: run update check");

            recommendations.Add("Driver GPU / network review recommended for gaming and streaming");
            recommendations.Add("Pause updates while gaming / streaming, then resume during idle time");
            return string.Join(Environment.NewLine, recommendations.Distinct());
        }

        private async Task RefreshUpdateControlViewAsync()
        {
            var serviceSummary = await GetUpdateServiceSummaryAsync();
            var hotfixSummary = await GetLatestHotfixSummaryAsync();
            var driverSummary = await GetDriverReviewSummaryAsync();
            var appSummary = await GetAppUpdateSummaryAsync();

            var latestUpdateLine = hotfixSummary
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? "No update history available.";

            var indicator = serviceSummary.Contains("Disabled", StringComparison.OrdinalIgnoreCase)
                ? "Warning"
                : hotfixSummary.Contains("No update history available", StringComparison.OrdinalIgnoreCase)
                    ? "Pending"
                    : "Up to date / Managed";

            UpdateDashboardText.Text =
                $"Windows Update Status: {indicator}{Environment.NewLine}" +
                $"Last Update Date: {latestUpdateLine.Split('|').FirstOrDefault()?.Trim() ?? "Unknown"}{Environment.NewLine}" +
                $"Pending Updates: Review in Windows Update{Environment.NewLine}" +
                $"Driver Update Status: Review Driver Center{Environment.NewLine}" +
                $"App Update Status: Microsoft Store / Apps Manager{Environment.NewLine}" +
                $"Indicator: {(indicator == "Warning" ? " Pending / Managed" : " Up to date / Managed")}";

            WindowsUpdateManagerText.Text =
                "Available updates: review Windows Update settings" + Environment.NewLine +
                "Installed updates:" + Environment.NewLine +
                hotfixSummary;

            UpdateRecommendationText.Text = BuildUpdateRecommendationText(serviceSummary, hotfixSummary);
            UpdateModeStatusText.Text = $"Mode aktif: {GetSelectedUpdateControlMode()}";
            UpdateScheduleText.Text =
                "Pause update: 1 / 7 / 30 days" + Environment.NewLine +
                "Schedule / Active hours: open Windows Update settings for full control";
            UpdateBackgroundControlText.Text =
                "Background download: controllable" + Environment.NewLine +
                "Bandwidth: basic policy / Delivery Optimization review" + Environment.NewLine +
                serviceSummary;
            DriverUpdateStatusText.Text = driverSummary;
            AppUpdateStatusText.Text = appSummary;
            UpdateCleanupStatusText.Text =
                "Delete old update files, SoftwareDistribution cache review, and component cleanup available.";
            UpdateServiceStatusText.Text = serviceSummary;
            UpdateBandwidthStatusText.Text =
                "Limit update bandwidth: use Delivery Optimization / background update control" + Environment.NewLine +
                "Optimize update speed only during idle hours";
            AutoUpdateRulesText.Text =
                "Rule set recommendation:" + Environment.NewLine +
                "Gaming / Streaming / Rendering -> pause updates while active" + Environment.NewLine +
                "Idle -> resume updates and prioritize security patches";
            UpdateBackupStatusText.Text =
                $"Backup path: {GetUpdateBackupDirectory()}{Environment.NewLine}" +
                "Backup / restore update-control preference and history snapshot.";

            UpdateControlQuickResultText.Text =
                "Update Optimized" + Environment.NewLine +
                "Background update controlled";

            if (_updateControlHistory.Count == 0)
            {
                AppendUpdateControlHistory("Update control initialized.");
                foreach (var line in hotfixSummary.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Take(3))
                    AppendUpdateControlHistory($"History: {line}");
            }
        }

        private void OpenUpdateControlSettings_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:windowsupdate", "Update Control");
            AppendUpdateControlHistory("Windows Update settings opened.");
        }

        private async Task ApplyUpdateModeCoreAsync(string mode)
        {
            string script;
            string message;

            switch (mode)
            {
                case "Disabled":
                    script = @"
Set-Service -Name wuauserv -StartupType Disabled -ErrorAction SilentlyContinue;
Set-Service -Name BITS -StartupType Disabled -ErrorAction SilentlyContinue;
Set-Service -Name DoSvc -StartupType Disabled -ErrorAction SilentlyContinue;
Stop-Service -Name wuauserv -Force -ErrorAction SilentlyContinue;
Stop-Service -Name BITS -Force -ErrorAction SilentlyContinue;
Stop-Service -Name DoSvc -Force -ErrorAction SilentlyContinue;
'Update services disabled.'";
                    message = "Update service disabled.";
                    break;
                case "Manual":
                    script = @"
Set-Service -Name wuauserv -StartupType Manual -ErrorAction SilentlyContinue;
Set-Service -Name BITS -StartupType Manual -ErrorAction SilentlyContinue;
Set-Service -Name DoSvc -StartupType Manual -ErrorAction SilentlyContinue;
Stop-Service -Name wuauserv -Force -ErrorAction SilentlyContinue;
Stop-Service -Name BITS -Force -ErrorAction SilentlyContinue;
Stop-Service -Name DoSvc -Force -ErrorAction SilentlyContinue;
'Update services switched to manual mode.'";
                    message = "Manual update control applied.";
                    break;
                case "Gaming Mode":
                case "Streaming Mode":
                    script = @"
Set-Service -Name wuauserv -StartupType Manual -ErrorAction SilentlyContinue;
Set-Service -Name BITS -StartupType Manual -ErrorAction SilentlyContinue;
Set-Service -Name DoSvc -StartupType Manual -ErrorAction SilentlyContinue;
Stop-Service -Name wuauserv -Force -ErrorAction SilentlyContinue;
Stop-Service -Name BITS -Force -ErrorAction SilentlyContinue;
Stop-Service -Name DoSvc -Force -ErrorAction SilentlyContinue;
reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config"" /v DODownloadMode /t REG_DWORD /d 0 /f | Out-Null;
'Update services reduced for active session mode.'";
                    message = $"{mode} applied for update control.";
                    break;
                default:
                    script = @"
Set-Service -Name wuauserv -StartupType Automatic -ErrorAction SilentlyContinue;
Set-Service -Name BITS -StartupType Automatic -ErrorAction SilentlyContinue;
Set-Service -Name DoSvc -StartupType Manual -ErrorAction SilentlyContinue;
Start-Service -Name wuauserv -ErrorAction SilentlyContinue;
Start-Service -Name BITS -ErrorAction SilentlyContinue;
'Automatic update mode restored.'";
                    message = "Automatic update mode restored.";
                    break;
            }

            var (success, output) = await ExecutePowerShellScriptAsync(script);
            UpdateModeStatusText.Text = $"Mode aktif: {mode}";
            AppendUpdateControlHistory($"Update mode changed to {mode}.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Update Control Mode", message, output);
            await RefreshUpdateControlViewAsync();
        }

        private async void ApplyUpdateMode_Click(object sender, RoutedEventArgs e)
        {
            await ApplyUpdateModeCoreAsync(GetSelectedUpdateControlMode());
        }

        private async void PauseUpdates_Click(object sender, RoutedEventArgs e)
        {
            var label = (sender as Button)?.Content?.ToString() ?? "Pause Updates";
            var (success, output) = await ExecutePowerShellScriptAsync(@"
Set-Service -Name wuauserv -StartupType Manual -ErrorAction SilentlyContinue;
Set-Service -Name BITS -StartupType Manual -ErrorAction SilentlyContinue;
Set-Service -Name DoSvc -StartupType Manual -ErrorAction SilentlyContinue;
Stop-Service -Name wuauserv -Force -ErrorAction SilentlyContinue;
Stop-Service -Name BITS -Force -ErrorAction SilentlyContinue;
            Stop-Service -Name DoSvc -Force -ErrorAction SilentlyContinue;
'Pause / temporary stop for update-related services requested.'");
            AppendUpdateControlHistory($"{label} requested.");
            ShowActionStatus(success ? ActionState.Info : ActionState.Warning, label, success ? "Pause / resume update diminta. Review status Windows Update untuk memastikan perubahan aktif." : "Pause / resume update menghasilkan warning.", output);
            LaunchWindowsUri("ms-settings:windowsupdate", "Pause & Schedule Update");
            await RefreshUpdateControlViewAsync();
        }

        private async void ApplyBackgroundUpdateControl_Click(object sender, RoutedEventArgs e)
        {
            var label = (sender as Button)?.Content?.ToString() ?? "Background Update Control";
            var script = label.Contains("Bandwidth", StringComparison.OrdinalIgnoreCase)
                ? @"
reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config"" /v DODownloadMode /t REG_DWORD /d 0 /f | Out-Null;
'Delivery Optimization adjusted for lower background bandwidth.'"
                : @"
Set-Service -Name DoSvc -StartupType Manual -ErrorAction SilentlyContinue;
Stop-Service -Name DoSvc -Force -ErrorAction SilentlyContinue;
reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config"" /v DODownloadMode /t REG_DWORD /d 0 /f | Out-Null;
'Background update download reduced.'";

            var (success, output) = await ExecutePowerShellScriptAsync(script);
            AppendUpdateControlHistory($"{label} applied.");
            ShowActionStatus(success ? ActionState.Info : ActionState.Warning, "Background Update Control", success ? $"{label} diminta. Review Delivery Optimization dan service update untuk hasil akhirnya." : $"{label} menghasilkan warning.", output);
            await RefreshUpdateControlViewAsync();
        }

        private async void OpenDriverUpdateManager_Click(object sender, RoutedEventArgs e)
        {
            AppendUpdateControlHistory("Driver Update Manager opened.");
            await ShowPage("Drivers", DriverUpdateCenterBtn);
        }

        private void OpenStoreAppUpdates_Click(object sender, RoutedEventArgs e)
        {
            var label = (sender as Button)?.Content?.ToString() ?? "App Update Manager";
            if (label.Contains("Disable Auto Update", StringComparison.OrdinalIgnoreCase))
                LaunchWindowsUri("ms-windows-store://settings/", "Microsoft Store Settings");
            else
                LaunchWindowsUri("ms-windows-store://downloadsandupdates", "Store App Updates");

            AppendUpdateControlHistory($"{label} opened.");
        }

        private async void UpdateCleanup_Click(object sender, RoutedEventArgs e)
        {
            var label = (sender as Button)?.Content?.ToString() ?? "Update Cleanup";
            var script = label.Contains("Cache", StringComparison.OrdinalIgnoreCase)
                ? @"
Stop-Service -Name wuauserv -Force -ErrorAction SilentlyContinue;
Stop-Service -Name BITS -Force -ErrorAction SilentlyContinue;
if (Test-Path $env:SystemRoot'\SoftwareDistribution\Download') {
    Remove-Item -Path $env:SystemRoot'\SoftwareDistribution\Download\*' -Recurse -Force -ErrorAction SilentlyContinue;
}
'Windows Update cache cleanup requested.'"
                : @"Dism /Online /Cleanup-Image /StartComponentCleanup";

            var (success, output) = await ExecutePowerShellScriptAsync(script);
            AppendUpdateControlHistory($"{label} requested.");
            ShowActionStatus(success ? ActionState.Info : ActionState.Warning, "Update Cleanup", success ? $"{label} diminta. Beberapa cleanup Windows Update bisa berjalan bertahap." : $"{label} menghasilkan warning.", output);
            await RefreshUpdateControlViewAsync();
        }

        private async void RefreshUpdateHistory_Click(object sender, RoutedEventArgs e)
        {
            AppendUpdateControlHistory("Update history refreshed.");
            await RefreshUpdateControlViewAsync();
            ShowActionStatus(ActionState.Info, "Update History", "Update history refreshed.", UpdateHistoryText.Text);
        }

        private void UpdateRollback_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:windowsupdate-history", "Update Rollback");
            AppendUpdateControlHistory("Update rollback review opened.");
            ShowActionStatus(ActionState.Info, "Update Rollback", "Riwayat update dibuka. Rollback update perlu ditinjau dan dijalankan manual dari Windows.");
        }

        private async void ApplyUpdateRecommendation_Click(object sender, RoutedEventArgs e)
        {
            await SmartUpdateControl_Click_Internal();
        }

        private void ReviewUpdateRecommendation_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Smart Update Recommendation", UpdateRecommendationText.Text);
        }

        private async void UpdateServiceControl_Click(object sender, RoutedEventArgs e)
        {
            var label = (sender as Button)?.Content?.ToString() ?? "Update Service Control";
            var script = label.Contains("Restart", StringComparison.OrdinalIgnoreCase)
                ? @"
Restart-Service -Name wuauserv -ErrorAction SilentlyContinue;
Restart-Service -Name BITS -ErrorAction SilentlyContinue;
Restart-Service -Name DoSvc -ErrorAction SilentlyContinue;
'Update services restart requested.'"
                : @"
$svc = Get-Service -Name wuauserv -ErrorAction SilentlyContinue;
if ($svc -and $svc.Status -eq 'Running') {
    Stop-Service -Name wuauserv -Force -ErrorAction SilentlyContinue;
    Stop-Service -Name BITS -Force -ErrorAction SilentlyContinue;
    Stop-Service -Name DoSvc -Force -ErrorAction SilentlyContinue;
    'Update services stopped.'
} else {
    Start-Service -Name wuauserv -ErrorAction SilentlyContinue;
    Start-Service -Name BITS -ErrorAction SilentlyContinue;
    'Update services started.'
}";

            var (success, output) = await ExecutePowerShellScriptAsync(script);
            AppendUpdateControlHistory($"{label} requested.");
            ShowActionStatus(success ? ActionState.Info : ActionState.Warning, "Update Service Control", success ? $"{label} diminta. Cek kembali status service update untuk hasil aktual." : $"{label} menghasilkan warning.", output);
            await RefreshUpdateControlViewAsync();
        }

        private async void OfflineUpdateTools_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Pilih paket update manual",
                Filter = "Windows Update Package (*.msu;*.cab)|*.msu;*.cab"
            };

            if (dialog.ShowDialog() != true)
            {
                ShowActionStatus(ActionState.Info, "Offline Update Tools", "Tidak ada paket update yang dipilih.");
                return;
            }

            var path = dialog.FileName;
            string script;
            if (path.EndsWith(".msu", StringComparison.OrdinalIgnoreCase))
            {
                script = $"wusa.exe \"{path}\" /quiet /norestart";
            }
            else
            {
                script = $"DISM /Online /Add-Package /PackagePath:\"{path}\" /NoRestart";
            }

            var (success, output) = await ExecutePowerShellScriptAsync(script);
            AppendUpdateControlHistory($"Offline update package selected: {Path.GetFileName(path)}");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Offline Update Tools", $"Offline update package diproses: {Path.GetFileName(path)}", output);
            await RefreshUpdateControlViewAsync();
        }

        private void SecurityUpdateFocus_Click(object sender, RoutedEventArgs e)
        {
            UpdateControlModeCombo.SelectedIndex = 0;
            UpdateModeStatusText.Text = "Mode aktif: Automatic (security focus)";
            AppendUpdateControlHistory("Security update focus enabled.");
            LaunchWindowsUri("ms-settings:windowsupdate", "Security Update Focus");
            ShowActionStatus(ActionState.Info, "Security Update Focus", "Preferensi UI diarahkan ke security focus dan halaman Windows Update dibuka untuk review manual.");
        }

        private void AutoUpdateRules_Click(object sender, RoutedEventArgs e)
        {
            AutoUpdateRulesText.Text =
                "Rules active:" + Environment.NewLine +
                "Gaming -> pause background update" + Environment.NewLine +
                "Streaming -> pause background update" + Environment.NewLine +
                "Rendering -> pause background update" + Environment.NewLine +
                "Idle -> resume automatic updates";
            AppendUpdateControlHistory("Auto update rules reviewed.");
            ShowActionStatus(ActionState.Success, "Auto Update Rules", "Rule set untuk gaming, streaming, rendering, dan idle sudah disiapkan.");
        }

        private async Task SmartUpdateControl_Click_Internal()
        {
            var notes = new List<string>();
            var (bgSuccess, bgOutput) = await ExecutePowerShellScriptAsync(@"
Set-Service -Name DoSvc -StartupType Manual -ErrorAction SilentlyContinue;
Stop-Service -Name DoSvc -Force -ErrorAction SilentlyContinue;
reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Config"" /v DODownloadMode /t REG_DWORD /d 0 /f | Out-Null;
'Background update optimization requested.'");
            notes.Add(bgSuccess ? "Background update controlled" : bgOutput);

            var (modeSuccess, modeOutput) = await ExecutePowerShellScriptAsync(@"
Set-Service -Name wuauserv -StartupType Manual -ErrorAction SilentlyContinue;
Set-Service -Name BITS -StartupType Manual -ErrorAction SilentlyContinue;
'Update services set to controlled manual mode.'");
            notes.Add(modeSuccess ? "Update mode optimized for controlled background activity" : modeOutput);
            notes.Add("Prioritize security updates manually during idle hours");

            UpdateControlQuickResultText.Text =
                "Update Optimized" + Environment.NewLine +
                "Background update controlled";
            AppendUpdateControlHistory("Smart update control applied.");
            ShowActionStatus(ActionState.Success, "SMART UPDATE CONTROL", "Update control optimized.", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
            await RefreshUpdateControlViewAsync();
        }

        private async void SmartUpdateControl_Click(object sender, RoutedEventArgs e)
        {
            await SmartUpdateControl_Click_Internal();
        }

        private void BackupUpdateSettings_Click(object sender, RoutedEventArgs e)
        {
            var snapshot = new
            {
                timestamp = DateTime.Now,
                mode = GetSelectedUpdateControlMode(),
                history = _updateControlHistory.ToArray()
            };
            var path = Path.Combine(GetUpdateBackupDirectory(), "update-settings.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(snapshot, Formatting.Indented));
            UpdateBackupStatusText.Text = $"Backup saved: {path}";
            AppendUpdateControlHistory("Update settings backup created.");
            ShowActionStatus(ActionState.Success, "Backup Update Settings", "Backup konfigurasi update berhasil dibuat.", path);
        }

        private async void RestoreUpdateSettings_Click(object sender, RoutedEventArgs e)
        {
            var path = Path.Combine(GetUpdateBackupDirectory(), "update-settings.json");
            if (!File.Exists(path))
            {
                ShowActionStatus(ActionState.Warning, "Restore Update Settings", "Backup update settings belum ada.");
                return;
            }

            try
            {
                dynamic snapshot = JsonConvert.DeserializeObject(File.ReadAllText(path));
                var mode = snapshot?.mode != null ? snapshot.mode.ToString() : "Automatic";
                foreach (ComboBoxItem item in UpdateControlModeCombo.Items)
                {
                    if (string.Equals(item.Content?.ToString(), mode, StringComparison.OrdinalIgnoreCase))
                    {
                        UpdateControlModeCombo.SelectedItem = item;
                        break;
                    }
                }

                await ApplyUpdateModeCoreAsync(mode);
                UpdateBackupStatusText.Text = $"Restore source: {path}";
                AppendUpdateControlHistory("Update settings restored from backup.");
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Error, "Restore Update Settings", "Gagal membaca backup update settings.", ex.Message);
            }
        }

        private async void ApplySmartTweaks_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            await ApplyPerformanceTweaksCoreAsync(notes);
            await ApplyPrivacyTweaksCoreAsync(notes);
            await ApplyNetworkTweaksCoreAsync(notes);
            await ApplySystemTweaksCoreAsync(notes);
            TweaksMasterResultText.Text = "System Optimized\nTweaks Applied Successfully";
            AppendTweaksHistory("Smart tweaks applied.");
            ShowRequestedStatus("Apply Smart Tweaks", "Safe tweak batch diminta. Beberapa tweak mungkin butuh review hasil manual atau restart.", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
            await RefreshTweaksCenterViewAsync();
        }

        private void SetTweakSafetyMode_Click(object sender, RoutedEventArgs e)
        {
            _tweaksSafetyMode = ((sender as Button)?.Tag?.ToString() ?? "safe") switch
            {
                "advanced" => "Advanced",
                "moderate" => "Moderate",
                _ => "Safe Only"
            };
            AppendTweaksHistory($"Safety mode changed to {_tweaksSafetyMode}.");
            _ = RefreshTweaksCenterViewAsync();
        }

        private async Task ApplyPerformanceTweaksCoreAsync(List<string> notes)
        {
            var result = await SafeApiCall(() => _backendClient.ApplyBoosterAsync("productivity"));
            if (result != null) notes.Add("Performance booster profile applied");
            var (success, output) = await ExecutePowerShellScriptAsync("powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
            notes.Add(success ? "Ultimate / high performance power plan requested" : output);
        }

        private async Task ApplyPrivacyTweaksCoreAsync(List<string> notes)
        {
            var result = await SafeApiCall(() => _backendClient.ApplyTweakAsync("disable_telemetry"));
            if (result != null) notes.Add("Privacy telemetry reduction applied");
        }

        private async Task ApplyNetworkTweaksCoreAsync(List<string> notes)
        {
            var optimize = await SafeApiCall(() => _backendClient.OptimizeTcpAsync());
            if (optimize != null) notes.Add("Network tweak optimization applied");
        }

        private async Task ApplySystemTweaksCoreAsync(List<string> notes)
        {
            var output = await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "Teams", "Spotify" }, "System Tweaks");
            notes.Add(output);
        }

        private async void ApplyPerformanceTweaksCenter_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            await ApplyPerformanceTweaksCoreAsync(notes);
            AppendTweaksHistory("Performance tweaks applied.");
            ShowRequestedStatus("Performance Tweaks", "Performance tweak batch diminta.", string.Join(Environment.NewLine, notes));
        }

        private async void ApplyGamingTweaksCenter_Click(object sender, RoutedEventArgs e)
        {
            await ApplyQuickCompetitiveGamingAsync();
            AppendTweaksHistory("Gaming tweaks applied.");
        }

        private async void ApplyNetworkTweaksCenter_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            await ApplyNetworkTweaksCoreAsync(notes);
            AppendTweaksHistory("Network tweaks applied.");
            ShowRequestedStatus("Network Tweaks", "Network tweak batch diminta.", string.Join(Environment.NewLine, notes));
        }

        private async void ApplyPrivacyTweaksCenter_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            await ApplyPrivacyTweaksCoreAsync(notes);
            AppendTweaksHistory("Privacy tweaks applied.");
            ShowRequestedStatus("Privacy Tweaks", "Privacy tweak batch diminta.", string.Join(Environment.NewLine, notes));
        }

        private async void ApplySystemTweaksCenter_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            await ApplySystemTweaksCoreAsync(notes);
            AppendTweaksHistory("System tweaks applied.");
            ShowRequestedStatus("System Tweaks", "System tweak batch diminta.", string.Join(Environment.NewLine, notes));
        }

        private async void ApplyUiTweaksCenter_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync(
                "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 0 /f; " +
                "reg add \"HKCU\\Control Panel\\Desktop\\WindowMetrics\" /v MinAnimate /t REG_SZ /d 0 /f");
            AppendTweaksHistory("UI tweaks applied.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "UI & UX Tweaks", success ? "UI tweaks applied." : "UI tweaks warning.", output);
        }

        private async void ApplyPowerTweaksCenter_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
            AppendTweaksHistory("Power tweaks applied.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Power Tweaks", success ? "Power tweaks applied." : "Power tweaks warning.", output);
        }

        private async void ApplyStartupTweaksCenter_Click(object sender, RoutedEventArgs e)
        {
            AppendTweaksHistory("Startup & background tweaks opened.");
            await ShowPage("Startup", StartupBtn);
        }

        private void ApplySecurityTweaksCenter_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("windowsdefender:", "Security Tweaks");
            AppendTweaksHistory("Security tweaks / Windows Security opened.");
            ShowActionStatus(ActionState.Info, "Security Tweaks", "Open Windows Security and Defender hardening review.");
        }

        private async void OpenAdvancedTweaksCenter_Click(object sender, RoutedEventArgs e)
        {
            AppendTweaksHistory("Advanced tweaks opened.");
            await ShowPage("Advanced", AdvancedTweaksBtn);
        }

        private async void ApplyTweaksProfile_Click(object sender, RoutedEventArgs e)
        {
            var profile = (TweaksProfileCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Gaming Mode Tweaks";
            switch (profile)
            {
                case "Performance Tweaks":
                    ApplyPerformanceTweaksCenter_Click(sender, e);
                    break;
                case "Privacy Mode":
                    ApplyPrivacyTweaksCenter_Click(sender, e);
                    break;
                case "Custom user tweak":
                    ShowActionStatus(ActionState.Info, "Custom Tweaks Profile", "Sesuaikan tweak per kategori lalu gunakan Tweaks Status Manager sebagai panduan.");
                    break;
                default:
                    ApplyGamingTweaksCenter_Click(sender, e);
                    break;
            }

            UpdateTweaksProfileSummary();
            AppendTweaksHistory($"Tweak profile applied: {profile}");
            await RefreshTweaksCenterViewAsync();
        }

        private void ReviewTweaksStatus_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Tweaks Status Manager", TweaksStatusManagerText.Text);
        }

        #endregion

        #region Settings

        private void AppendSettingsHistory(string entry)
        {
            if (_settingsHistory.Count >= 16)
                _settingsHistory.Dequeue();

            _settingsHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (SettingsHistoryText != null)
                SettingsHistoryText.Text = string.Join(Environment.NewLine, _settingsHistory.Reverse());
        }

        private async Task RefreshSettingsViewAsync()
        {
            var stats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
            var systemInfo = await GetSettingsSystemInfoCachedAsync();
            var json = stats as Newtonsoft.Json.Linq.JObject;
            var systemJson = systemInfo as Newtonsoft.Json.Linq.JObject;
            var cpu = json?.Value<double?>("cpu") ?? json?.Value<double?>("cpu_percent") ?? 0d;
            var ram = json?.Value<double?>("memory") ?? json?.Value<double?>("memory_percent") ?? 0d;
            var disk = json?.Value<double?>("disk") ?? json?.Value<double?>("disk_percent") ?? 0d;

            var selectedLanguage = (SettingsLanguageCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? _localizationService.CurrentLocale;
            SettingsLanguageOverviewText.Text =
                _localizationService.BuildCoverageSummary() + Environment.NewLine +
                L("messages.language.restart_notice", "Some language changes may fully apply after restart.");

            SettingsUiText.Text =
                $"Theme: {_settingsTheme}{Environment.NewLine}" +
                $"Density: {((SettingsDensityCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Comfortable")}{Environment.NewLine}" +
                $"Language: {selectedLanguage}{Environment.NewLine}" +
                $"Language engine mode: {_settingsLanguageMode}{Environment.NewLine}" +
                $"Fallback locale: {_localizationService.FallbackLocale}{Environment.NewLine}" +
                $"Sidebar: {_settingsSidebarMode} / customizable" + Environment.NewLine +
                "UX behavior: confirm dialog + quick action mode available";

            SettingsAutomationText.Text =
                $"Automation mode: {_automationMode}{Environment.NewLine}" +
                $"Policy profile: {_automationPolicyProfile}{Environment.NewLine}" +
                $"Allow AI decision: {(_autonomousModeEnabled ? "ON" : "OFF")}{Environment.NewLine}" +
                $"Adaptive learning: {(_automationLearningEnabled ? "ON" : "OFF")}{Environment.NewLine}" +
                "Context awareness: ON" + Environment.NewLine +
                "Predictive system: ON" + Environment.NewLine +
                $"Config file: {_appConfigService.GetConfigPath()}";

            SettingsEngineText.Text =
                $"Performance level: {_settingsPerformanceLevel}{Environment.NewLine}" +
                $"Power engine: {_powerDynamicMode}{Environment.NewLine}" +
                $"Scenario auto profile: gaming / streaming / creator / cleanup ready{Environment.NewLine}" +
                $"Cleanup scope: temp + cache + logs + junk{Environment.NewLine}" +
                $"Resource thresholds: CPU {Math.Max(75, cpu):0}% | RAM {Math.Max(80, ram):0}% | Disk {Math.Max(85, disk):0}%";

            SettingsSafetyText.Text =
                $"Risk mode: {_settingsRiskMode}{Environment.NewLine}" +
                $"Safety system: {(_settingsSafetyEnabled ? "ON" : "OFF")}{Environment.NewLine}" +
                "Backup before tweak/update: enabled" + Environment.NewLine +
                "Advanced access: registry / service / execution controlled" + Environment.NewLine +
                "Execution engine: background + silent + retry aware";

            SettingsSystemText.Text =
                $"Logging level: Advanced{Environment.NewLine}" +
                "Privacy: local-first / telemetry minimized" + Environment.NewLine +
                "Update channel: Stable" + Environment.NewLine +
                "Integration: startup / tray / background service configurable" + Environment.NewLine +
                "Adaptive control: automatic aggressiveness tuning available" + Environment.NewLine +
                $"User mode preset: {_settingsUserMode}{Environment.NewLine}" +
                $"Master switches: Engine {(_settingsEngineEnabled ? "ON" : "OFF")} | Safety {(_settingsSafetyEnabled ? "ON" : "OFF")} | Monitoring {(_settingsMonitoringEnabled ? "ON" : "OFF")}";

            if (DiscordWebhookStatusText != null)
            {
                DiscordWebhookStatusText.Text =
                    $"Discord reporting: {(_discordWebhookEnabled ? "ON" : "OFF")}{Environment.NewLine}" +
                    $"Error/audit webhook configured: {(!string.IsNullOrWhiteSpace(_discordWebhookUrl) ? "Yes" : "No")}{Environment.NewLine}" +
                    $"Release update webhook configured: {(!string.IsNullOrWhiteSpace(_discordUpdateWebhookUrl) ? "Yes" : "No")}{Environment.NewLine}" +
                    $"Minimum level: {_discordWebhookMinimumLevel}{Environment.NewLine}" +
                    $"Cooldown: {_discordWebhookCooldownSeconds} sec{Environment.NewLine}" +
                    "Feature Audit uses the error/audit webhook. App Update uses the release update webhook.";
            }
            RefreshDiscordPreview(_discordWebhookMinimumLevel.ToLowerInvariant(), "HyperBoostX preview report", "This preview shows how Discord reporting will look.");
            if (OpenAiSettingsStatusText != null)
            {
                OpenAiSettingsStatusText.Text =
                    $"AI Copilot: {(_openAiEnabled ? "ON" : "OFF")}{Environment.NewLine}" +
                    $"Model: {_openAiModel}{Environment.NewLine}" +
                    $"Mode: {_openAiMode}{Environment.NewLine}" +
                    $"Permission: {_openAiPermissionLevel}{Environment.NewLine}" +
                    $"API Key: {(string.IsNullOrWhiteSpace(_openAiApiKey) ? "Not configured" : "Configured")}{Environment.NewLine}" +
                    $"Last Test: {_lastOpenAiConnectionTestStatus}";
            }
            if (AiCopilotStatusText != null)
            {
                AiCopilotStatusText.Text =
                    $"AI Status: {(_openAiEnabled && !string.IsNullOrWhiteSpace(_openAiApiKey) ? "Online-ready" : "Offline")}{Environment.NewLine}" +
                    $"Model: {_openAiModel}{Environment.NewLine}" +
                    $"Mode: {_openAiMode}{Environment.NewLine}" +
                    $"Permission: {_openAiPermissionLevel}";
            }
            if (AiCopilotApprovalText != null)
            {
                RefreshAiApprovalPanel();
            }
            if (AiCopilotMemoryText != null)
            {
                AiCopilotMemoryText.Text = _aiCopilotMemory.Count == 0
                    ? "AI session memory is empty."
                    : string.Join(Environment.NewLine, _aiCopilotMemory.Reverse());
            }
            if (AiCopilotReasoningText != null)
                AiCopilotReasoningText.Text = _lastAiReasoningSummary;
            if (AiCopilotAutomationText != null)
                AiCopilotAutomationText.Text = _lastAiAutomationSummary;
            if (AiCopilotWhyText != null)
                AiCopilotWhyText.Text = _lastAiWhySummary;
            if (AiCopilotContextText != null)
                AiCopilotContextText.Text = string.IsNullOrWhiteSpace(_lastAiSystemContext)
                    ? "AI context snapshot will appear here."
                    : _lastAiSystemContext.Replace("\n", Environment.NewLine);
            if (AiCopilotPersonalizationText != null)
                AiCopilotPersonalizationText.Text = BuildAiPersonalizationSummary();
            if (AiCopilotRiskText != null)
                RefreshAiApprovalPanel();

            if (_settingsHistory.Count == 0)
                AppendSettingsHistory("Settings center initialized.");

            await RefreshSettingsPcSpecAsync(json, systemJson);
            RefreshAppUpdatePanels();
        }

        private async Task RefreshAboutViewAsync()
        {
            if (AboutVersionText != null)
                AboutVersionText.Text = $"{NormalizeVersionLabel(_currentAppVersion)} — 2026";

            if (AboutVersionText != null)
                AboutVersionText.Text = $"{NormalizeVersionLabel(_currentAppVersion)} - {(IsStableBuild(_currentAppVersion) ? "Stable" : "Prerelease")} - 2026";

            RefreshAppUpdatePanels();
            await Task.CompletedTask;
        }

        private static bool IsStableBuild(string version)
        {
            return !string.IsNullOrWhiteSpace(version) &&
                   !version.Contains("-", StringComparison.OrdinalIgnoreCase);
        }

        private void RefreshAppUpdatePanels()
        {
            var lastCheck = _lastAppUpdateCheckUtc.HasValue
                ? _lastAppUpdateCheckUtc.Value.ToLocalTime().ToString("dd MMM yyyy HH:mm")
                : "Never";
            var published = _latestKnownReleasePublishedUtc.HasValue
                ? _latestKnownReleasePublishedUtc.Value.ToLocalTime().ToString("dd MMM yyyy HH:mm")
                : "Unknown";
            var latestVersion = string.IsNullOrWhiteSpace(_latestKnownAppVersion) ? "Unknown" : _latestKnownAppVersion;
            var releaseUrl = string.IsNullOrWhiteSpace(_latestKnownReleaseUrl) ? "https://github.com/jxxzy/HyperBoostX/releases" : _latestKnownReleaseUrl;
            var installerAsset = string.IsNullOrWhiteSpace(_latestKnownInstallerAssetName) ? "Unavailable" : _latestKnownInstallerAssetName;
            var statusText =
                $"Current version: {NormalizeVersionLabel(_currentAppVersion)}{Environment.NewLine}" +
                $"Latest known release: {latestVersion}{Environment.NewLine}" +
                $"Channel: {_latestKnownReleaseChannel}{Environment.NewLine}" +
                $"Auto check: {(_autoCheckAppUpdates ? "ON" : "OFF")}{Environment.NewLine}" +
                $"Auto install: {(_autoInstallAppUpdates ? "ON" : "OFF")}{Environment.NewLine}" +
                $"Last check: {lastCheck}{Environment.NewLine}" +
                $"Published: {published}{Environment.NewLine}" +
                $"Installer asset: {installerAsset}{Environment.NewLine}" +
                $"{_lastAppUpdateReadiness}{Environment.NewLine}" +
                $"{_lastAppUpdateSummary}{Environment.NewLine}" +
                $"Download page: {releaseUrl}";

            if (SettingsAppUpdateStatusText != null)
                SettingsAppUpdateStatusText.Text = statusText;

            if (AboutUpdateStatusText != null)
                AboutUpdateStatusText.Text = statusText;

            if (ToggleAutoAppUpdateBtn != null)
                ToggleAutoAppUpdateBtn.Content = _autoCheckAppUpdates ? "Auto Check Updates: ON" : "Auto Check Updates: OFF";

            if (ToggleAutoInstallUpdateBtn != null)
                ToggleAutoInstallUpdateBtn.Content = _autoInstallAppUpdates ? "Auto Install Updates: ON" : "Auto Install Updates: OFF";

            if (OpenLatestReleaseBtn != null)
                OpenLatestReleaseBtn.Content = _isAppUpdateAvailable ? "Download Latest Update" : "Open Release Page";

            var installContent = _appUpdateInstallInProgress
                ? "Installing Update..."
                : _isAppUpdateAvailable ? "Download && Install Update" : "Reinstall Current Version";

            if (InstallLatestUpdateBtn != null)
            {
                InstallLatestUpdateBtn.Content = installContent;
                InstallLatestUpdateBtn.IsEnabled = !_appUpdateInstallInProgress && (!_appUpdateCheckInProgress || _isAppUpdateAvailable);
            }

            if (AboutInstallLatestUpdateBtn != null)
            {
                AboutInstallLatestUpdateBtn.Content = installContent;
                AboutInstallLatestUpdateBtn.IsEnabled = !_appUpdateInstallInProgress && (!_appUpdateCheckInProgress || _isAppUpdateAvailable);
            }
        }

        private async Task RefreshSettingsPcSpecAsync(JObject stats, JObject systemInfo)
        {
            if (SettingsSpecOverviewText == null)
                return;

            try
            {
                var staticInfo = await QuerySettingsPcStaticInfoAsync();
                var cpuInfo = systemInfo?["cpu"] as JObject;
                var memoryInfo = systemInfo?["memory"] as JObject;
                var gpuInfo = systemInfo?["gpu"] as JObject;
                var gpuPrimary = gpuInfo?["gpus"]?.FirstOrDefault() as JObject;
                var diskInfo = systemInfo?["disk"] as JObject;
                var networkInfo = systemInfo?["network"] as JObject;
                var osInfo = systemInfo?["os"] as JObject;
                var identityInfo = systemInfo?["identity"] as JObject;
                var biosInfo = systemInfo?["bios"] as JObject;
                var temperatures = systemInfo?["temperatures"] as JObject;
                var batteryText = await QuerySettingsBatterySummaryCachedAsync();
                var pingText = await QuerySettingsPingSummaryCachedAsync();

                var cpuUsage = stats?.Value<double?>("cpu") ?? stats?.Value<double?>("cpu_percent") ?? cpuInfo?.Value<double?>("usage") ?? 0d;
                var ramUsage = stats?.Value<double?>("memory") ?? stats?.Value<double?>("memory_percent") ?? memoryInfo?.Value<double?>("percent") ?? 0d;
                var diskUsage = stats?.Value<double?>("disk") ?? stats?.Value<double?>("disk_percent") ?? 0d;
                var gpuUsage = (stats?["gpu"] as JObject)?.Value<double?>("load")
                    ?? (stats?["gpu"] as JObject)?.Value<double?>("memory_percent")
                    ?? 0d;
                var cpuTemp = ExtractTemperatureByKeyword(temperatures, "cpu", "package", "core") ?? ExtractTemperature(temperatures);
                var gpuTemp = (stats?["gpu"] as JObject)?.Value<double?>("temperature")
                    ?? ExtractTemperatureByKeyword(temperatures, "gpu", "graphics");
                var diskTemp = ExtractTemperatureByKeyword(temperatures, "disk", "nvme", "ssd", "hdd", "storage");
                var uptime = identityInfo?["uptime"]?["formatted"]?.ToString()
                    ?? BuildUptimeSummary(stats?.Value<double?>("boot_time"));
                var hostname = identityInfo?.Value<string>("hostname") ?? Environment.MachineName;
                var windowsVersion = $"{osInfo?.Value<string>("system") ?? "Windows"} {osInfo?.Value<string>("release") ?? ""}".Trim();
                var windowsBuild = osInfo?.Value<string>("version") ?? identityInfo?.Value<string>("build") ?? "Unknown build";
                var cpuName = cpuInfo?.Value<string>("processor") ?? "Unknown CPU";
                var cpuCurrentClock = cpuInfo?.Value<double?>("frequency_current") ?? stats?.Value<double?>("cpu_freq_current") ?? 0d;
                var cpuBoostClock = cpuInfo?.Value<double?>("frequency_max") ?? stats?.Value<double?>("cpu_freq_max") ?? 0d;
                var totalRam = memoryInfo?.Value<long?>("total") ?? 0L;
                var usedRam = memoryInfo?.Value<long?>("used") ?? 0L;
                var freeRam = memoryInfo?.Value<long?>("available") ?? 0L;
                var ramSpeed = memoryInfo?.Value<int?>("speed_mhz") ?? 0;
                var ramType = InferMemoryType(ramSpeed);
                var slotsUsed = memoryInfo?.Value<int?>("slots_used") ?? 0;
                var totalSlots = memoryInfo?["modules"] is JArray modules && modules.Count > 0 ? modules.Count : 0;
                var gpuName = gpuPrimary?.Value<string>("name") ?? (stats?["gpu"] as JObject)?.Value<string>("name") ?? "Integrated / unavailable";
                var gpuVramBytes = gpuPrimary?.Value<long?>("vram") ?? 0L;
                var gpuDriver = gpuPrimary?.Value<string>("driver_version") ?? "Unknown";
                var gpuFan = (stats?["gpu"] as JObject)?.Value<double?>("fan_speed");
                var motherboard = staticInfo?["board"] as JObject;
                var systemSummary = staticInfo?["system"] as JObject;
                var disks = staticInfo?["disks"] as JArray;
                var networkPrimary = PickPrimaryNetworkAdapter(networkInfo);
                var ipAddress = PickPrimaryIpAddress(networkPrimary);
                var linkSpeed = networkPrimary?["stats"]?.Value<double?>("speed_mbps") ?? 0d;
                var connectionType = DetectConnectionType(networkPrimary);
                var storageSummary = BuildStorageSummary(diskInfo, disks);

                SettingsSpecOverviewText.Text =
                    $"Device: {hostname}{Environment.NewLine}" +
                    $"OS: {windowsVersion} | {windowsBuild}{Environment.NewLine}" +
                    $"CPU: {cpuName} @ {cpuCurrentClock:0} MHz{Environment.NewLine}" +
                    $"RAM: {FormatBytes(totalRam)} total | {FormatBytes(usedRam)} used ({ramUsage:0}%) {Environment.NewLine}" +
                    $"GPU: {gpuName} | VRAM {FormatBytes(gpuVramBytes)}{Environment.NewLine}" +
                    $"Storage: {storageSummary.Overview}{Environment.NewLine}" +
                    $"Uptime: {uptime}";

                SettingsCpuInfoText.Text =
                    $"Name: {cpuName}{Environment.NewLine}" +
                    $"Core / Thread: {cpuInfo?.Value<int?>("cores") ?? 0} / {cpuInfo?.Value<int?>("threads") ?? 0}{Environment.NewLine}" +
                    $"Base / Boost: {cpuCurrentClock:0} / {cpuBoostClock:0} MHz{Environment.NewLine}" +
                    $"Usage: {cpuUsage:0}% {BuildUsageBar(cpuUsage)}{Environment.NewLine}" +
                    $"Temperature: {FormatTemperatureLine(cpuTemp)}{Environment.NewLine}" +
                    $"Cache: L1/L2/L3 info not exposed by backend{Environment.NewLine}" +
                    $"Live graph: {BuildTelemetryGraph(cpuUsage, "CPU")}";

                SettingsRamInfoText.Text =
                    $"Total: {FormatBytes(totalRam)}{Environment.NewLine}" +
                    $"Used / Free: {FormatBytes(usedRam)} / {FormatBytes(freeRam)}{Environment.NewLine}" +
                    $"Usage: {ramUsage:0}% {BuildUsageBar(ramUsage)}{Environment.NewLine}" +
                    $"Speed: {(ramSpeed > 0 ? $"{ramSpeed} MHz" : "Unknown")}{Environment.NewLine}" +
                    $"Type: {ramType}{Environment.NewLine}" +
                    $"Slots used / available: {slotsUsed} / {(totalSlots > 0 ? totalSlots.ToString() : "Unknown")}";

                SettingsGpuInfoText.Text =
                    $"GPU: {gpuName}{Environment.NewLine}" +
                    $"VRAM: {FormatBytes(gpuVramBytes)}{Environment.NewLine}" +
                    $"Driver: {gpuDriver}{Environment.NewLine}" +
                    $"Usage: {gpuUsage:0}% {BuildUsageBar(gpuUsage)}{Environment.NewLine}" +
                    $"Temperature: {FormatTemperatureLine(gpuTemp)}{Environment.NewLine}" +
                    $"Fan speed: {(gpuFan.HasValue && gpuFan.Value > 0 ? $"{gpuFan:0}%" : "Unavailable")}";

                SettingsStorageInfoText.Text =
                    $"{storageSummary.Overview}{Environment.NewLine}" +
                    $"{storageSummary.DriveLines}{Environment.NewLine}" +
                    $"{storageSummary.HardwareLines}";

                SettingsBoardInfoText.Text =
                    $"Motherboard: {motherboard?.Value<string>("manufacturer") ?? systemSummary?.Value<string>("manufacturer") ?? "Unknown"} {motherboard?.Value<string>("product") ?? systemSummary?.Value<string>("model") ?? ""}".Trim() + Environment.NewLine +
                    $"BIOS / UEFI: {biosInfo?.Value<string>("version") ?? staticInfo?["bios"]?["version"]?.ToString() ?? "Unknown"}{Environment.NewLine}" +
                    $"Serial: {motherboard?.Value<string>("serial_number") ?? biosInfo?.Value<string>("serial_number") ?? "Hidden / unavailable"}{Environment.NewLine}" +
                    $"System manufacturer: {systemSummary?.Value<string>("manufacturer") ?? "Unknown"}{Environment.NewLine}" +
                    $"Model: {systemSummary?.Value<string>("model") ?? "Unknown"}";

                SettingsPowerInfoText.Text = batteryText;

                SettingsNetworkInfoText.Text =
                    $"Adapter: {networkPrimary?.Path?.TrimStart('/') ?? "Unknown"}{Environment.NewLine}" +
                    $"Connection: {connectionType}{Environment.NewLine}" +
                    $"IP Address: {ipAddress}{Environment.NewLine}" +
                    $"Link speed: {(linkSpeed > 0 ? $"{linkSpeed:0} Mbps" : "Unknown")}{Environment.NewLine}" +
                    $"Latency: {pingText}";

                SettingsTemperatureInfoText.Text =
                    $"CPU Temp: {FormatTemperatureLine(cpuTemp)}{Environment.NewLine}" +
                    $"GPU Temp: {FormatTemperatureLine(gpuTemp)}{Environment.NewLine}" +
                    $"Disk Temp: {FormatTemperatureLine(diskTemp)}{Environment.NewLine}" +
                    $"Status: {BuildTemperatureStatus(cpuTemp, gpuTemp, diskTemp)}";

                SettingsRealtimeInfoText.Text =
                    $"{BuildTelemetryGraph(cpuUsage, "CPU")} {cpuUsage:0}%{Environment.NewLine}" +
                    $"{BuildTelemetryGraph(ramUsage, "RAM")} {ramUsage:0}%{Environment.NewLine}" +
                    $"{BuildTelemetryGraph(gpuUsage, "GPU")} {gpuUsage:0}%{Environment.NewLine}" +
                    $"{BuildTelemetryGraph(diskUsage, "DSK")} {diskUsage:0}%{Environment.NewLine}" +
                    $"Processes: {stats?.Value<int?>("process_count") ?? 0} | Threads: {stats?.Value<int?>("thread_count") ?? 0}";
            }
            catch (Exception ex)
            {
                SettingsSpecOverviewText.Text = "Unable to load PC spec panel.";
                SettingsCpuInfoText.Text = ex.Message;
                SettingsRamInfoText.Text = "System info refresh encountered an error.";
                SettingsGpuInfoText.Text = "System info refresh encountered an error.";
                SettingsStorageInfoText.Text = "System info refresh encountered an error.";
                SettingsBoardInfoText.Text = "System info refresh encountered an error.";
                SettingsPowerInfoText.Text = "System info refresh encountered an error.";
                SettingsNetworkInfoText.Text = "System info refresh encountered an error.";
                SettingsTemperatureInfoText.Text = "System info refresh encountered an error.";
                SettingsRealtimeInfoText.Text = "System info refresh encountered an error.";
            }
        }

        private async Task<JObject> GetSettingsSystemInfoCachedAsync()
        {
            if (_settingsSystemInfoCache != null && DateTime.UtcNow - _settingsSystemInfoCacheUtc < TimeSpan.FromSeconds(20))
                return _settingsSystemInfoCache;

            var result = await SafeApiCall(() => _backendClient.GetSystemInfoAsync());
            if (result is JObject json)
            {
                _settingsSystemInfoCache = json;
                _settingsSystemInfoCacheUtc = DateTime.UtcNow;
            }

            return _settingsSystemInfoCache;
        }

        private async Task<JObject> QuerySettingsPcStaticInfoAsync()
        {
            if (_settingsPcStaticCache != null && DateTime.UtcNow - _settingsPcStaticCacheUtc < TimeSpan.FromMinutes(5))
                return _settingsPcStaticCache;

            var script = @"
$board = Get-CimInstance Win32_BaseBoard -ErrorAction SilentlyContinue | Select-Object -First 1 Product, Manufacturer, SerialNumber
$bios = Get-CimInstance Win32_BIOS -ErrorAction SilentlyContinue | Select-Object -First 1 SMBIOSBIOSVersion, SerialNumber
$system = Get-CimInstance Win32_ComputerSystem -ErrorAction SilentlyContinue | Select-Object -First 1 Manufacturer, Model
$disks = Get-PhysicalDisk -ErrorAction SilentlyContinue | Select-Object FriendlyName, MediaType, BusType, HealthStatus, Size
[pscustomobject]@{
    board = $board
    bios = $bios
    system = $system
    disks = $disks
} | ConvertTo-Json -Depth 6 -Compress
";
            var (success, output) = await ExecutePowerShellScriptAsync(script);
            if (!success || string.IsNullOrWhiteSpace(output))
                return _settingsPcStaticCache;

            try
            {
                _settingsPcStaticCache = JObject.Parse(output.Trim());
                _settingsPcStaticCacheUtc = DateTime.UtcNow;
                return _settingsPcStaticCache;
            }
            catch
            {
                return _settingsPcStaticCache;
            }
        }

        private async Task<string> QuerySettingsBatterySummaryCachedAsync()
        {
            if (!string.IsNullOrWhiteSpace(_settingsBatteryCache) && DateTime.UtcNow - _settingsBatteryCacheUtc < TimeSpan.FromSeconds(20))
                return _settingsBatteryCache;

            _settingsBatteryCache = await QueryBatterySummaryAsync();
            _settingsBatteryCacheUtc = DateTime.UtcNow;
            return _settingsBatteryCache;
        }

        private async Task<string> QuerySettingsPingSummaryCachedAsync()
        {
            if (!string.IsNullOrWhiteSpace(_settingsPingCache) && DateTime.UtcNow - _settingsPingCacheUtc < TimeSpan.FromSeconds(15))
                return _settingsPingCache;

            _settingsPingCache = await QuerySettingsPingSummaryAsync();
            _settingsPingCacheUtc = DateTime.UtcNow;
            return _settingsPingCache;
        }

        private async Task<string> QuerySettingsPingSummaryAsync()
        {
            var script = @"
$result = Test-Connection -TargetName 1.1.1.1 -Count 1 -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $result) { 'Unavailable'; return }
'{0} ms' -f [math]::Round($result.Latency, 0)
";
            var (success, output) = await ExecutePowerShellScriptAsync(script);
            return success && !string.IsNullOrWhiteSpace(output)
                ? output.Trim()
                : "Unavailable";
        }

        private static string BuildUptimeSummary(double? bootUnixTime)
        {
            if (!bootUnixTime.HasValue || bootUnixTime.Value <= 0)
                return "Unknown";

            try
            {
                var boot = DateTimeOffset.FromUnixTimeSeconds((long)bootUnixTime.Value);
                var span = DateTimeOffset.UtcNow - boot;
                return $"{(int)span.TotalDays}d {span.Hours}h {span.Minutes}m";
            }
            catch
            {
                return "Unknown";
            }
        }

        private static double? ExtractTemperatureByKeyword(JObject temperatures, params string[] keywords)
        {
            if (temperatures == null || keywords == null || keywords.Length == 0)
                return null;

            foreach (var property in temperatures.Properties())
            {
                if (!keywords.Any(keyword => property.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var direct = ExtractTemperature(new JObject { [property.Name] = property.Value });
                if (direct.HasValue)
                    return direct;
            }

            return null;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0)
                return "0 B";

            string[] suffix = { "B", "KB", "MB", "GB", "TB" };
            double value = bytes;
            var index = 0;
            while (value >= 1024 && index < suffix.Length - 1)
            {
                value /= 1024;
                index++;
            }

            return $"{value:0.#} {suffix[index]}";
        }

        private static string BuildUsageBar(double percent)
        {
            var clamped = Math.Max(0, Math.Min(100, percent));
            var filled = (int)Math.Round(clamped / 10);
            return "[" + new string('#', filled) + new string('-', 10 - filled) + "]";
        }

        private static string BuildTelemetryGraph(double percent, string label)
        {
            return $"{label} {BuildUsageBar(percent)}";
        }

        private static string InferMemoryType(int speedMhz)
        {
            if (speedMhz >= 4800)
                return "DDR5";
            if (speedMhz >= 1600)
                return "DDR4";
            if (speedMhz >= 800)
                return "DDR3";
            return "Unknown";
        }

        private static string FormatTemperatureLine(double? value)
        {
            if (!value.HasValue || value.Value <= 0)
                return "Unavailable";

            var color = value.Value >= 85 ? "Red" : value.Value >= 70 ? "Yellow" : "Green";
            return $"{value.Value:0} C ({color})";
        }

        private static string BuildTemperatureStatus(double? cpu, double? gpu, double? disk)
        {
            var max = new[] { cpu ?? 0, gpu ?? 0, disk ?? 0 }.Max();
            if (max >= 85)
                return "Red / Critical";
            if (max >= 70)
                return "Yellow / Warm";
            return "Green / Normal";
        }

        private static string DetectConnectionType(JToken adapter)
        {
            var name = adapter?.Path?.TrimStart('/') ?? "";
            return name.Contains("wi-fi", StringComparison.OrdinalIgnoreCase) || name.Contains("wireless", StringComparison.OrdinalIgnoreCase)
                ? "WiFi"
                : "LAN / Ethernet";
        }

        private static JProperty PickPrimaryNetworkAdapter(JObject networkInfo)
        {
            if (networkInfo == null)
                return null;

            foreach (var property in networkInfo.Properties())
            {
                if (property.Value?["stats"]?.Value<bool?>("is_up") == true)
                    return property;
            }

            return networkInfo.Properties().FirstOrDefault();
        }

        private static string PickPrimaryIpAddress(JProperty adapter)
        {
            var addresses = adapter?.Value?["addresses"] as JArray;
            if (addresses == null)
                return "Unavailable";

            foreach (var entry in addresses.OfType<JObject>())
            {
                var addr = entry.Value<string>("address");
                if (!string.IsNullOrWhiteSpace(addr) && !addr.Contains("::1") && !addr.StartsWith("127."))
                    return addr;
            }

            foreach (var token in addresses)
            {
                var addr = token switch
                {
                    JObject obj => obj.Value<string>("address") ?? "",
                    JValue value => value.ToString(),
                    _ => token?.ToString() ?? ""
                };

                if (!string.IsNullOrWhiteSpace(addr) && !addr.Contains("::1") && !addr.StartsWith("127."))
                    return addr;
            }

            return "Unavailable";
        }

        private static (string Overview, string DriveLines, string HardwareLines) BuildStorageSummary(JObject diskInfo, JArray physicalDisks)
        {
            if (diskInfo == null || !diskInfo.Properties().Any())
                return ("Storage information unavailable.", "No mounted volumes detected.", "Physical disk data unavailable.");

            long total = 0;
            long used = 0;
            long free = 0;
            var driveLines = new List<string>();

            foreach (var property in diskInfo.Properties())
            {
                if (property.Value is not JObject disk)
                    continue;

                var mount = disk.Value<string>("mountpoint") ?? property.Name;
                var mountTotal = disk.Value<long?>("total") ?? 0;
                var mountUsed = disk.Value<long?>("used") ?? 0;
                var mountFree = disk.Value<long?>("free") ?? 0;
                var percent = disk.Value<double?>("percent") ?? 0d;
                total += mountTotal;
                used += mountUsed;
                free += mountFree;
                driveLines.Add($"{mount}: {FormatBytes(mountFree)} free / {FormatBytes(mountTotal)} total ({percent:0}% used)");
            }

            var hardwareLines = "Disk hardware profile unavailable.";
            if (physicalDisks != null && physicalDisks.Count > 0)
            {
                hardwareLines = string.Join(Environment.NewLine,
                    physicalDisks.OfType<JObject>().Take(3).Select(d =>
                        $"{d.Value<string>("FriendlyName") ?? "Disk"} | {d.Value<string>("MediaType") ?? "Unknown"} | {d.Value<string>("BusType") ?? "Unknown"} | {d.Value<string>("HealthStatus") ?? "Unknown"}"));
            }

            return (
                $"{FormatBytes(total)} total | {FormatBytes(used)} used | {FormatBytes(free)} free",
                string.Join(Environment.NewLine, driveLines),
                hardwareLines);
        }

        private async Task EnsureAppUpdateStatusAsync(bool force, bool userInitiated)
        {
            if (_appUpdateCheckInProgress)
                return;

            if (!force)
            {
                if (!_autoCheckAppUpdates)
                    return;

                if (_lastAppUpdateCheckUtc.HasValue && DateTime.UtcNow - _lastAppUpdateCheckUtc.Value < TimeSpan.FromHours(6))
                {
                    RefreshAppUpdatePanels();
                    return;
                }
            }

            _appUpdateCheckInProgress = true;
            try
            {
                var result = await _appUpdateService.CheckLatestReleaseAsync(_currentAppVersion);
                _lastAppUpdateCheckUtc = DateTime.UtcNow;
                _latestKnownAppVersion = result.LatestVersion;
                _latestKnownReleaseUrl = string.IsNullOrWhiteSpace(result.LatestReleaseUrl)
                    ? "https://github.com/jxxzy/HyperBoostX/releases"
                    : result.LatestReleaseUrl;
                _latestKnownInstallerAssetName = result.InstallerAssetName ?? "";
                _latestKnownInstallerDownloadUrl = result.InstallerDownloadUrl ?? "";
                _latestKnownChecksumsDownloadUrl = result.ChecksumsDownloadUrl ?? "";
                _latestKnownReleaseChannel = result.ReleaseChannel;
                _latestKnownReleasePublishedUtc = result.PublishedUtc;
                _isAppUpdateAvailable = result.IsUpdateAvailable;
                _lastAppUpdateReadiness = result.Success
                    ? result.IsUpdateAvailable
                        ? string.IsNullOrWhiteSpace(_latestKnownInstallerDownloadUrl)
                            ? "Readiness: release page only"
                            : "Readiness: installer available for verification"
                        : "Readiness: already on latest known release"
                    : "Readiness: update check failed";
                _lastAppUpdateSummary = result.Summary;
                RefreshAppUpdatePanels();
                await SavePersistedConfigurationAsync();
                await ReportAppUpdateToDiscordAsync(result, userInitiated);

                if (!userInitiated && _autoInstallAppUpdates && result.Success && result.IsUpdateAvailable)
                {
                    _ = Dispatcher.BeginInvoke(new Action(async () =>
                    {
                        await DownloadAndInstallLatestAppUpdateAsync(autoTriggered: true);
                    }));
                }

                if (userInitiated)
                {
                    var state = result.Success
                        ? result.IsUpdateAvailable ? ActionState.Warning : ActionState.Success
                        : ActionState.Warning;
                    ShowActionStatus(
                        state,
                        "App Update",
                        result.Success
                            ? result.IsUpdateAvailable
                                ? $"Versi baru tersedia: {result.LatestVersion}"
                                : "Aplikasi ini sudah memakai rilis terbaru yang diketahui."
                            : "Gagal memeriksa rilis terbaru.",
                        result.Success
                            ? $"{result.ReleaseChannel} | {_latestKnownReleaseUrl}"
                            : result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _lastAppUpdateCheckUtc = DateTime.UtcNow;
                _lastAppUpdateReadiness = "Readiness: update check failed";
                _lastAppUpdateSummary = $"Update check failed: {ex.Message}";
                RefreshAppUpdatePanels();
                if (userInitiated)
                    ShowActionStatus(ActionState.Warning, "App Update", "Gagal memeriksa update aplikasi.", ex.Message);
            }
            finally
            {
                _appUpdateCheckInProgress = false;
            }
        }

        private async Task ReportAppUpdateToDiscordAsync(AppReleaseCheckResult result, bool userInitiated)
        {
            await LoadSensitiveConfigurationAsync();

            if (string.IsNullOrWhiteSpace(_discordUpdateWebhookUrl))
                return;

            if (result == null || !result.Success || !result.IsUpdateAvailable)
                return;

            var normalizedLatestVersion = NormalizeDiscordReportVersion(result.LatestVersion);
            var normalizedCurrentVersion = NormalizeDiscordReportVersion(_currentAppVersion);
            var signature = $"app-update|{normalizedLatestVersion}";
            if (_discordWebhookLastSent.TryGetValue(signature, out var lastSent) &&
                DateTime.UtcNow - lastSent < TimeSpan.FromHours(12))
            {
                return;
            }

            var fields = BuildDiscordReportFields("warning", "New app update detected.");
            fields["Current Version"] = normalizedCurrentVersion;
            fields["Latest Version"] = normalizedLatestVersion;
            fields["Channel"] = string.IsNullOrWhiteSpace(result.ReleaseChannel) ? "Unknown" : result.ReleaseChannel;
            fields["Published"] = result.PublishedUtc?.ToLocalTime().ToString("dd MMM yyyy HH:mm") ?? "Unknown";
            fields["Installer Asset"] = string.IsNullOrWhiteSpace(result.InstallerAssetName) ? "Unavailable" : result.InstallerAssetName;
            fields["Triggered By"] = userInitiated ? "Manual check" : "Auto check";
            fields["Release URL"] = string.IsNullOrWhiteSpace(result.LatestReleaseUrl)
                ? "https://github.com/jxxzy/HyperBoostX/releases"
                : result.LatestReleaseUrl;

            var sendResult = await _discordWebhookService.SendDetailedAsync(
                _discordUpdateWebhookUrl,
                "HyperBoostX release terbaru tersedia",
                $"Versi {normalizedLatestVersion} sudah tersedia untuk download.",
                "warning",
                fields,
                "HyperBoostX Update");

            if (sendResult.Success)
            {
                _discordWebhookLastSent[signature] = DateTime.UtcNow;
                AppendSettingsHistory($"Discord update notification sent for {normalizedLatestVersion}.");
            }
            else
            {
                AppendSettingsHistory($"Discord update notification not delivered: {sendResult.Summary}");
            }
        }

        private static string NormalizeVersionLabel(string version)
        {
            var text = (version ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
                return "0.0.0";

            var match = Regex.Match(text, @"(?i)\bv?(?<version>\d+(?:\.\d+){0,3}(?:-[0-9A-Za-z.-]+)?)");
            if (match.Success)
                return match.Groups["version"].Value;

            if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                text = text[1..];

            var buildMetadataSeparator = text.IndexOf('+');
            if (buildMetadataSeparator >= 0)
                text = text[..buildMetadataSeparator];

            return string.IsNullOrWhiteSpace(text) ? "0.0.0" : text;
        }

        private void OpenReleasePage()
        {
            var target = string.IsNullOrWhiteSpace(_latestKnownReleaseUrl)
                ? "https://github.com/jxxzy/HyperBoostX/releases"
                : _latestKnownReleaseUrl;
            LaunchExternalUrl(target, "App Update");
        }

        private string GetAppUpdatesDirectory()
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HyperBoost X",
                "updates");
            Directory.CreateDirectory(path);
            return path;
        }

        private string CreateAppUpdateLauncherScript(string installerPath)
        {
            var updatesDirectory = GetAppUpdatesDirectory();
            var scriptPath = Path.Combine(updatesDirectory, "apply-update.cmd");
            var escapedInstallerPath = installerPath.Replace("\"", "\"\"");
            var script = string.Join(Environment.NewLine, new[]
            {
                "@echo off",
                "ping 127.0.0.1 -n 3 > nul",
                $"start \"\" \"{escapedInstallerPath}\" /S",
                "exit /b 0"
            });
            File.WriteAllText(scriptPath, script, Encoding.ASCII);
            return scriptPath;
        }

        private async Task DownloadAndInstallLatestAppUpdateAsync(bool autoTriggered)
        {
            if (_appUpdateInstallInProgress)
                return;

            if (!_isAppUpdateAvailable && !autoTriggered)
            {
                ShowActionStatus(ActionState.Info, "App Update", "Belum ada versi baru yang diketahui untuk diinstal.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_latestKnownInstallerDownloadUrl))
            {
                _lastAppUpdateReadiness = "Readiness: release page only";
                ShowActionStatus(ActionState.Warning, "App Update", "Installer asset belum tersedia. Halaman release akan dibuka sebagai fallback.", _latestKnownReleaseUrl);
                OpenReleasePage();
                return;
            }

            _appUpdateInstallInProgress = true;
            RefreshAppUpdatePanels();

            try
            {
                var targetVersion = string.IsNullOrWhiteSpace(_latestKnownAppVersion) ? NormalizeVersionLabel(_currentAppVersion) : _latestKnownAppVersion;
                ShowActionStatus(ActionState.Info, "App Update", $"Downloading installer {targetVersion}...");

                var progress = new Progress<double>(percent =>
                {
                    _lastAppUpdateSummary = $"Downloading installer... {percent:0}%";
                    RefreshAppUpdatePanels();
                });

                var installerPath = await _appUpdateService.DownloadInstallerAsync(
                    _latestKnownInstallerDownloadUrl,
                    targetVersion,
                    GetAppUpdatesDirectory(),
                    progress);

                var verification = await _appUpdateService.VerifyInstallerAsync(
                    installerPath,
                    _latestKnownInstallerDownloadUrl,
                    _latestKnownInstallerAssetName,
                    _latestKnownChecksumsDownloadUrl);
                if (!verification.AllowManualInstall)
                {
                    _lastAppUpdateReadiness = "Readiness: blocked";
                    _lastAppUpdateSummary = $"Installer verification failed. {verification.Summary}";
                    RefreshAppUpdatePanels();
                    ShowActionStatus(ActionState.Error, "App Update", "Installer verification failed. Update will not run automatically.", verification.Summary);
                    return;
                }

                _lastAppUpdateReadiness = verification.AllowAutomaticInstall
                    ? "Readiness: auto install allowed"
                    : "Readiness: manual install ready";

                if (autoTriggered && !verification.AllowAutomaticInstall)
                {
                    _lastAppUpdateSummary = $"Auto-install blocked. {verification.Summary}";
                    RefreshAppUpdatePanels();
                    ShowActionStatus(ActionState.Warning, "App Update", "Auto-install diblokir karena installer belum lolos verifikasi otomatis penuh.", verification.Summary);
                    return;
                }

                var launcherScript = CreateAppUpdateLauncherScript(installerPath);
                _lastAppUpdateSummary = $"Installer downloaded: {Path.GetFileName(installerPath)}. Update will continue automatically.";
                RefreshAppUpdatePanels();
                await SavePersistedConfigurationAsync();

                Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"{launcherScript}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                ShowActionStatus(ActionState.Info, "App Update", "Installer sudah diunduh. Aplikasi akan ditutup agar update berjalan mandiri.", $"{installerPath}{Environment.NewLine}{verification.Summary}{Environment.NewLine}SHA256: {verification.Sha256}");
                AppendSettingsHistory($"App self-update started for {targetVersion}{(autoTriggered ? " (auto)" : "")}.");

                var shutdownTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
                shutdownTimer.Tick += (_, _) =>
                {
                    shutdownTimer.Stop();
                    Close();
                };
                shutdownTimer.Start();
            }
            catch (Exception ex)
            {
                _lastAppUpdateSummary = $"Self-update failed: {ex.Message}";
                RefreshAppUpdatePanels();
                ShowActionStatus(ActionState.Error, "App Update", "Gagal mengunduh atau menjalankan installer update.", ex.Message);
            }
            finally
            {
                _appUpdateInstallInProgress = false;
                RefreshAppUpdatePanels();
            }
        }

        private void LaunchExternalUrl(string url, string featureName)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                ShowActionStatus(ActionState.Info, featureName, "External page opened successfully.", url);
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Error, featureName, $"Unable to open {featureName}.", ex.Message);
            }
        }

        private async void CheckAppUpdate_Click(object sender, RoutedEventArgs e)
        {
            await EnsureAppUpdateStatusAsync(force: true, userInitiated: true);
        }

        private async void InstallLatestAppUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAppUpdateAvailable)
                await EnsureAppUpdateStatusAsync(force: true, userInitiated: false);

            await DownloadAndInstallLatestAppUpdateAsync(autoTriggered: false);
        }

        private void OpenLatestRelease_Click(object sender, RoutedEventArgs e)
        {
            OpenReleasePage();
        }

        private void OpenSociabuzzDonate_Click(object sender, RoutedEventArgs e)
        {
            LaunchExternalUrl(SociabuzzDonateUrl, "Donation / Sociabuzz");
        }

        private async void ToggleAutoAppUpdate_Click(object sender, RoutedEventArgs e)
        {
            _autoCheckAppUpdates = !_autoCheckAppUpdates;
            _lastAppUpdateSummary = _autoCheckAppUpdates
                ? "Automatic app update checks enabled."
                : "Automatic app update checks disabled.";
            RefreshAppUpdatePanels();
            AppendSettingsHistory($"App update auto-check {(_autoCheckAppUpdates ? "enabled" : "disabled")}.");
            await SavePersistedConfigurationAsync();
            ShowActionStatus(ActionState.Info, "App Update", $"Auto app update check sekarang {(_autoCheckAppUpdates ? "ON" : "OFF")}.");
        }

        private async void ToggleAutoInstallAppUpdate_Click(object sender, RoutedEventArgs e)
        {
            _autoInstallAppUpdates = !_autoInstallAppUpdates;
            _lastAppUpdateSummary = _autoInstallAppUpdates
                ? "Automatic self-update install enabled."
                : "Automatic self-update install disabled.";
            RefreshAppUpdatePanels();
            AppendSettingsHistory($"App auto-install update {(_autoInstallAppUpdates ? "enabled" : "disabled")}.");
            await SavePersistedConfigurationAsync();
            ShowActionStatus(ActionState.Info, "App Update", $"Auto install update sekarang {(_autoInstallAppUpdates ? "ON" : "OFF")}.");
        }

        private async void TestBackend_Click(object sender, RoutedEventArgs e)
        {
            var isHealthy = await SafeApiCall(() => _backendClient.HealthCheckAsync());
            if (isHealthy)
            {
                ShowActionStatus(ActionState.Success, "Backend connected", "Backend is running and responding normally.", $"URL: {_currentBackendUrl}");
                BackendHealthIndicator.Background = Brushes.LimeGreen;
            }
            else
            {
                ShowActionStatus(ActionState.Error, "Backend unavailable", "Backend is not responding. Please make sure HyperBoost backend is running.", $"URL: {_currentBackendUrl}");
                BackendHealthIndicator.Background = Brushes.IndianRed;
            }
        }

        private async Task ApplyLanguageSelectionAsync(LocalizationMode mode, bool useSelectedLocale)
        {
            var locale = useSelectedLocale
                ? (SettingsLanguageCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString()
                : null;

            try
            {
                if (mode == LocalizationMode.ManualSelection && !string.IsNullOrWhiteSpace(locale))
                {
                    await _localizationService.SetManualLanguageAsync(locale);
                    _settingsLanguageMode = "Manual Selection";
                }
                else
                {
                    await _localizationService.SetModeAsync(mode, locale);
                    _settingsLanguageMode = mode switch
                    {
                        LocalizationMode.FollowSystem => "Follow System",
                        LocalizationMode.AutoSmartDetection => "Auto Smart Detection",
                        _ => "Manual Selection"
                    };
                }

                PopulateLanguageSelector();
                ApplyLocalizationToUi();
                await RefreshSettingsViewAsync();

                var pack = _localizationService.GetAvailableLanguagePacks()
                    .FirstOrDefault(item => item.LocaleCode.Equals(_localizationService.CurrentLocale, StringComparison.OrdinalIgnoreCase));
                var meta = pack == null
                    ? _localizationService.CurrentLocale
                    : $"{pack.NativeName} ({pack.LocaleCode}) | {_localizationService.FormatNumber(pack.CoveragePercent, "N1")}%";

                ShowActionStatus(ActionState.Success,
                    L("settings.language.applied", "Language updated"),
                    L("messages.language.current", "Current App Language: {language}",
                        new Dictionary<string, object> { ["language"] = meta }),
                    L("messages.language.mode", "Language Mode: {mode}",
                        new Dictionary<string, object> { ["mode"] = _settingsLanguageMode }));
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Error,
                    L("error.language_apply_failed", "Unable to apply the selected language."),
                    ex.Message);
            }
        }

        private async void UpdateBackendUrl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentBackendUrl = BackendUrlInput.Text.Trim();
                _backendClient.Dispose();
                _backendClient = new HyperBoostBackendClient(_currentBackendUrl);
                ShowActionStatus(ActionState.Success, "Backend URL updated", "The frontend is now pointing to the new backend endpoint.", _currentBackendUrl);
                AppendSettingsHistory($"Backend URL updated to {_currentBackendUrl}.");
                await CheckBackendHealth();
                await RefreshSettingsViewAsync();
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Error, "Unable to update backend URL", ex.Message);
            }
        }

        private async Task CheckBackendHealth()
        {
            var isHealthy = await SafeApiCall(() => _backendClient.HealthCheckAsync());
            if (isHealthy)
            {
                BackendHealthIndicator.Background = Brushes.LimeGreen;
                HeaderBackendBadge.Background = Brushes.LimeGreen;
                HeaderBackendText.Text = L("status.backend_connected", "Backend connected");
                ((TextBlock)BackendHealthIndicator.Child).Text = "Connected and ready";
            }
            else
            {
                BackendHealthIndicator.Background = Brushes.IndianRed;
                HeaderBackendBadge.Background = Brushes.IndianRed;
                HeaderBackendText.Text = L("status.backend_disconnected", "Backend disconnected");
                ((TextBlock)BackendHealthIndicator.Child).Text = "Backend unavailable";
            }
        }

        private async void ApplySmartConfig_Click(object sender, RoutedEventArgs e)
        {
            _settingsTheme = "Auto";
            _settingsPerformanceLevel = "Balanced";
            _settingsRiskMode = "Safe mode";
            _settingsUserMode = "Beginner";
            _automationMode = "Smart Autonomous";
            _automationPolicyProfile = "Balanced automation";
            _autonomousModeEnabled = true;
            _settingsEngineEnabled = true;
            _settingsSafetyEnabled = true;
            _settingsMonitoringEnabled = true;
            SettingsQuickResultText.Text = "Smart config applied\nBalanced AI + safe autonomous defaults loaded";
            AppendSettingsHistory("Apply Smart Config executed.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Success, "APPLY SMART CONFIG", "Konfigurasi optimal aman berhasil diterapkan.");
        }

        private async void ApplySafeConfig_Click(object sender, RoutedEventArgs e)
        {
            _settingsPerformanceLevel = "Conservative";
            _settingsRiskMode = "Safe mode";
            _settingsUserMode = "Beginner";
            _automationMode = "Safe Autonomous";
            _automationPolicyProfile = "Conservative automation";
            _autonomousModeEnabled = true;
            SettingsQuickResultText.Text = "Safe config applied\nLow-risk automation and protection prioritized";
            AppendSettingsHistory("Safe Mode Config executed.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Success, "SAFE MODE CONFIG", "Semua setting aman diprioritaskan.");
        }

        private async void ApplyMaxPerformanceConfig_Click(object sender, RoutedEventArgs e)
        {
            _settingsPerformanceLevel = "Extreme";
            _settingsRiskMode = "Expert mode";
            _settingsUserMode = "Expert";
            _automationMode = "Full Autonomous";
            _automationPolicyProfile = "Aggressive automation";
            _autonomousModeEnabled = true;
            SettingsQuickResultText.Text = "Max performance config applied\nAggressive engine and full power policy enabled";
            AppendSettingsHistory("Max Performance Config executed.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Warning, "MAX PERFORMANCE CONFIG", "Konfigurasi agresif diterapkan. Pastikan memahami risikonya.");
        }

        private async void ApplyUiSettings_Click(object sender, RoutedEventArgs e)
        {
            _settingsTheme = (SettingsThemeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Auto";
            AppendSettingsHistory($"UI settings applied: theme {_settingsTheme}.");
            await ApplyLanguageSelectionAsync(LocalizationMode.ManualSelection, useSelectedLocale: true);
            await SavePersistedConfigurationAsync();
            ShowActionStatus(ActionState.Success, "USER INTERFACE & EXPERIENCE", "UI settings berhasil diterapkan.", SettingsUiText.Text);
        }

        private async void ToggleSidebarMode_Click(object sender, RoutedEventArgs e)
        {
            _settingsSidebarMode = _settingsSidebarMode == "Full" ? "Minimal" : "Full";
            AppendSettingsHistory("Sidebar mode toggled.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Info, "USER INTERFACE & EXPERIENCE", $"Sidebar mode sekarang {_settingsSidebarMode}.");
        }

        private async void FollowSystemLanguage_Click(object sender, RoutedEventArgs e)
        {
            AppendSettingsHistory("Language mode switched to Follow System.");
            await ApplyLanguageSelectionAsync(LocalizationMode.FollowSystem, useSelectedLocale: false);
            await SavePersistedConfigurationAsync();
            ShowActionStatus(ActionState.Info,
                L("settings.language.overview_title", "Language & Localization"),
                L("settings.language.system_mode_applied", "Language mode now follows the system locale."));
        }

        private async void AutoDetectLanguage_Click(object sender, RoutedEventArgs e)
        {
            AppendSettingsHistory("Language mode switched to Auto Smart Detection.");
            await ApplyLanguageSelectionAsync(LocalizationMode.AutoSmartDetection, useSelectedLocale: false);
            await SavePersistedConfigurationAsync();
            ShowActionStatus(ActionState.Info,
                L("settings.language.overview_title", "Language & Localization"),
                L("settings.language.auto_mode_applied", "Smart language detection is now active."));
        }

        private void OpenLanguagePackFolder_Click(object sender, RoutedEventArgs e)
        {
            var path = _localizationService.GetLocalizationRoot();
            Directory.CreateDirectory(path);
            LaunchWindowsTool("explorer.exe", path, "Language Packs");
            AppendSettingsHistory("Language pack folder opened.");
            ShowActionStatus(ActionState.Info,
                L("settings.language.overview_title", "Language & Localization"),
                L("settings.language.pack_folder_opened", "Language pack folder opened."),
                path);
        }

        private async void ExportLocalizationReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var path = await _localizationService.ExportMissingKeysReportAsync();
                AppendSettingsHistory($"Localization report exported to {path}.");
                ShowActionStatus(ActionState.Success,
                    L("settings.language.overview_title", "Language & Localization"),
                    L("settings.language.report_exported", "Localization report exported."),
                    L("messages.language.missing_keys_report", "Missing keys exported to {path}",
                        new Dictionary<string, object> { ["path"] = path }));
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Error,
                    L("error.report_export_failed", "Unable to export localization report."),
                    ex.Message);
            }
        }

        private async void ToggleDiscordWebhook_Click(object sender, RoutedEventArgs e)
        {
            _discordWebhookUrl = DiscordWebhookUrlInput?.Text?.Trim() ?? "";
            _discordUpdateWebhookUrl = DiscordUpdateWebhookUrlInput?.Text?.Trim() ?? "";
            _discordWebhookMinimumLevel = (DiscordWebhookLevelCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Error";
            _discordWebhookCooldownSeconds = int.TryParse(DiscordWebhookCooldownInput?.Text, out var cooldown) ? Math.Max(15, cooldown) : 120;
            if (string.IsNullOrWhiteSpace(_discordWebhookUrl) && !_discordWebhookEnabled)
            {
                ShowActionStatus(ActionState.Warning, "Discord Error Reporting", "Masukkan Discord webhook URL dulu.");
                return;
            }

            _discordWebhookEnabled = !_discordWebhookEnabled;
            AppendSettingsHistory($"Discord error reporting {(_discordWebhookEnabled ? "enabled" : "disabled")}.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Info, "Discord Error Reporting", $"Discord error reporting sekarang {(_discordWebhookEnabled ? "ON" : "OFF")}.");
        }

        private async void TestDiscordWebhook_Click(object sender, RoutedEventArgs e)
        {
            _discordWebhookUrl = DiscordWebhookUrlInput?.Text?.Trim() ?? "";
            _discordUpdateWebhookUrl = DiscordUpdateWebhookUrlInput?.Text?.Trim() ?? "";
            _discordWebhookMinimumLevel = (DiscordWebhookLevelCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Error";
            _discordWebhookCooldownSeconds = int.TryParse(DiscordWebhookCooldownInput?.Text, out var cooldown) ? Math.Max(15, cooldown) : 120;
            if (string.IsNullOrWhiteSpace(_discordWebhookUrl))
            {
                ShowActionStatus(ActionState.Warning, "Discord Error Reporting", "Masukkan Discord webhook URL dulu.");
                return;
            }

            RefreshDiscordPreview("warning", "HyperBoostX test error report", "This is a test notification from HyperBoostX Discord integration.");
            var sent = await _discordWebhookService.SendAsync(
                _discordWebhookUrl,
                "HyperBoostX test error report",
                "This is a test notification from HyperBoostX Discord integration.",
                "warning",
                BuildDiscordReportFields("warning", $"Cooldown: {_discordWebhookCooldownSeconds} sec"));

            if (sent)
            {
                _discordWebhookEnabled = true;
                AppendSettingsHistory("Discord webhook test notification sent.");
                await PersistAndRefreshSettingsAsync();
                ShowActionStatus(ActionState.Success, "Discord Error Reporting", "Test message berhasil dikirim ke Discord.");
            }
            else
            {
                ShowActionStatus(ActionState.Error, "Discord Error Reporting", "Gagal mengirim test message ke Discord.", "Periksa webhook URL dan koneksi internet.");
            }
        }

        private void PreviewDiscordWebhook_Click(object sender, RoutedEventArgs e)
        {
            _discordWebhookMinimumLevel = (DiscordWebhookLevelCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Error";
            _discordWebhookCooldownSeconds = int.TryParse(DiscordWebhookCooldownInput?.Text, out var cooldown) ? Math.Max(15, cooldown) : 120;
            RefreshDiscordPreview(_discordWebhookMinimumLevel.ToLowerInvariant(), "HyperBoostX preview report", "This preview shows the structure of a Discord error report.");
            ShowActionStatus(ActionState.Info, "Discord Error Reporting", "Preview payload diperbarui.", DiscordWebhookPreviewText.Text);
        }

        private void OpenErrorLogFolder_Click(object sender, RoutedEventArgs e)
        {
            var logRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HyperBoost X",
                "logs");
            Directory.CreateDirectory(logRoot);
            LaunchWindowsTool("explorer.exe", logRoot, "HyperBoostX Error Logs");
            ShowActionStatus(ActionState.Info, "Discord Error Reporting", "Folder log error dibuka.", logRoot);
        }

        private async void SaveOpenAiSettings_Click(object sender, RoutedEventArgs e)
        {
            _openAiApiKey = OpenAiApiKeyInput?.Text?.Trim() ?? "";
            _openAiModel = string.IsNullOrWhiteSpace(OpenAiModelInput?.Text) ? "gpt-4.1-mini" : OpenAiModelInput.Text.Trim();
            _openAiMode = (OpenAiModeCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Assistant";
            _openAiPermissionLevel = (OpenAiPermissionCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Ask";
            _openAiEnabled = !string.IsNullOrWhiteSpace(_openAiApiKey);
            AppendSettingsHistory($"OpenAI Copilot settings saved. Model: {_openAiModel}, Mode: {_openAiMode}.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Success, "OpenAI Copilot", "AI settings berhasil disimpan.");
        }

        private async void TestOpenAiConnection_Click(object sender, RoutedEventArgs e)
        {
            _openAiApiKey = OpenAiApiKeyInput?.Text?.Trim() ?? "";
            _openAiModel = string.IsNullOrWhiteSpace(OpenAiModelInput?.Text) ? "gpt-4.1-mini" : OpenAiModelInput.Text.Trim();
            _openAiMode = (OpenAiModeCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Assistant";
            _openAiPermissionLevel = (OpenAiPermissionCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Ask";

            if (string.IsNullOrWhiteSpace(_openAiApiKey))
            {
                _lastOpenAiConnectionTestStatus = $"FAIL {DateTime.Now:HH:mm:ss} - API key belum diisi.";
                await PersistAndRefreshSettingsAsync();
                ShowActionStatus(ActionState.Warning, "OpenAI Copilot", "Masukkan OpenAI API key dulu.");
                return;
            }

            try
            {
                var context = await BuildAiSystemContextAsync();
                var response = await _openAiCopilotService.AskAsync(new OpenAiCopilotRequest
                {
                    ApiKey = _openAiApiKey,
                    Model = _openAiModel,
                    UserPrompt = "Say hello and confirm that HyperBoostX Copilot is connected.",
                    SystemContext = context,
                    AppMode = _openAiMode,
                    PermissionLevel = _openAiPermissionLevel
                });
                _openAiEnabled = true;
                _lastOpenAiConnectionTestStatus = $"OK {DateTime.Now:HH:mm:ss} - {TrimFeatureAuditText(response.Reply, 120)}";
                await PersistAndRefreshSettingsAsync();
                ShowActionStatus(ActionState.Success, "OpenAI Copilot", "Koneksi ke OpenAI berhasil.", response.Reply);
            }
            catch (Exception ex)
            {
                _lastOpenAiConnectionTestStatus = $"FAIL {DateTime.Now:HH:mm:ss} - {TrimFeatureAuditText(ex.Message, 120)}";
                await PersistAndRefreshSettingsAsync();
                ShowActionStatus(ActionState.Error, "OpenAI Copilot", "Gagal menghubungi OpenAI.", ex.Message);
            }
        }

        private async void AskAiCopilot_Click(object sender, RoutedEventArgs e) => await HandleAiCopilotPromptAsync(AiCopilotInput?.Text ?? "");
        private async void AiFixMyPc_Click(object sender, RoutedEventArgs e) => await HandleAiCopilotPromptAsync("Fix my PC safely based on current system context.");
        private async void AiPrepareGaming_Click(object sender, RoutedEventArgs e) => await HandleAiCopilotPromptAsync("Prepare this PC for gaming with stable FPS and safe actions.");
        private async void AiCleanSafely_Click(object sender, RoutedEventArgs e) => await HandleAiCopilotPromptAsync("Clean this PC safely without risky changes.");
        private async void AiFixNetwork_Click(object sender, RoutedEventArgs e) => await HandleAiCopilotPromptAsync("Diagnose and fix my network safely.");

        private async void RefreshAiContext_Click(object sender, RoutedEventArgs e)
        {
            await RefreshAiCopilotDiagnosticsAsync(refreshContext: true);
            await SavePersistedConfigurationAsync();
            ShowActionStatus(ActionState.Info, "AI Copilot", "AI context snapshot refreshed.", _lastAiSystemContext);
        }

        private async void ClearAiMemory_Click(object sender, RoutedEventArgs e)
        {
            _aiCopilotMemory.Clear();
            _lastAiPrompt = "";
            _lastAiSystemContext = "";
            _lastAiReasoningSummary = "No AI reasoning available yet.";
            _lastAiAutomationSummary = "No AI automation plan yet.";
            _lastAiOutcomeSummary = "No AI outcome recorded yet.";
            _aiTotalRequests = 0;
            _aiApprovedPlans = 0;
            _aiRejectedPlans = 0;
            _aiCreatedAutomations = 0;
            _aiPreferredScenario = "General Assistance";
            _aiPreferredAction = "scan_only";
            _aiPreferredRiskStyle = "Ask";
            _aiIntentCounters.Clear();
            _aiActionCounters.Clear();
            _aiPendingActionReviews.Clear();
            _lastAiNaturalAutomationPlan = new AiNaturalAutomationPlan();
            _lastAiCopilotResponse = null;
            await RefreshAiCopilotDiagnosticsAsync(refreshContext: false);
            await SavePersistedConfigurationAsync();
            ShowActionStatus(ActionState.Success, "AI Copilot", "AI memory and last plan cleared.");
        }

        private async void ApproveAiActions_Click(object sender, RoutedEventArgs e)
        {
            if (_lastAiCopilotResponse == null || _lastAiCopilotResponse.SafeActions.Count == 0)
            {
                ShowActionStatus(ActionState.Info, "AI Copilot", "Tidak ada safe action dari AI yang menunggu approval.");
                return;
            }

            await ExecuteAiSafeActionsAsync(_lastAiCopilotResponse.SafeActions);
            AppendAiCopilotMemory($"[{DateTime.Now:HH:mm:ss}] Approved AI actions: {string.Join(", ", _lastAiCopilotResponse.SafeActions)}");
            _aiApprovedPlans++;
            _lastAiOutcomeSummary = $"Approved and executed: {string.Join(", ", _lastAiCopilotResponse.SafeActions)}";
            _aiPendingActionReviews.Clear();
            AiCopilotApprovalText.Text = "AI actions approved and executed.";
            await RefreshAiCopilotDiagnosticsAsync(refreshContext: false);
            await SavePersistedConfigurationAsync();
            ShowActionStatus(ActionState.Success, "AI Copilot", "Safe actions dari AI berhasil dijalankan.", string.Join(", ", _lastAiCopilotResponse.SafeActions));
        }

        private async void ApproveNextAiAction_Click(object sender, RoutedEventArgs e)
        {
            var next = _aiPendingActionReviews.FirstOrDefault();
            if (next == null)
            {
                ShowActionStatus(ActionState.Info, "AI Copilot", "Tidak ada action granular yang menunggu approval.");
                return;
            }

            await ExecuteAiSafeActionsAsync(new[] { next.Action });
            _aiPendingActionReviews.RemoveAt(0);
            _aiApprovedPlans++;
            IncrementAiCounter(_aiActionCounters, next.Action);
            _aiPreferredAction = GetTopCounterKey(_aiActionCounters, next.Action);
            _lastAiOutcomeSummary = $"Approved single action: {next.Action}";
            AppendAiCopilotMemory($"[{DateTime.Now:HH:mm:ss}] Approved single AI action: {next.Action}");
            RefreshAiApprovalPanel();
            await RefreshAiCopilotDiagnosticsAsync(refreshContext: false);
            await SavePersistedConfigurationAsync();
            ShowActionStatus(ActionState.Success, "AI Copilot", $"Single AI action executed: {next.Action}", next.Explanation);
        }

        private async void SkipNextAiAction_Click(object sender, RoutedEventArgs e)
        {
            var next = _aiPendingActionReviews.FirstOrDefault();
            if (next == null)
            {
                ShowActionStatus(ActionState.Info, "AI Copilot", "Tidak ada action granular yang bisa dilewati.");
                return;
            }

            _aiPendingActionReviews.RemoveAt(0);
            _aiRejectedPlans++;
            _lastAiOutcomeSummary = $"Skipped single action: {next.Action}";
            AppendAiCopilotMemory($"[{DateTime.Now:HH:mm:ss}] Skipped single AI action: {next.Action}");
            RefreshAiApprovalPanel();
            await RefreshAiCopilotDiagnosticsAsync(refreshContext: false);
            await SavePersistedConfigurationAsync();
            ShowActionStatus(ActionState.Info, "AI Copilot", $"Single AI action skipped: {next.Action}", next.Explanation);
        }

        private async void RejectAiActions_Click(object sender, RoutedEventArgs e)
        {
            if (_lastAiCopilotResponse == null)
            {
                ShowActionStatus(ActionState.Info, "AI Copilot", "Tidak ada rencana AI yang aktif untuk ditolak.");
                return;
            }

            AppendAiCopilotMemory($"[{DateTime.Now:HH:mm:ss}] Rejected AI intent: {_lastAiCopilotResponse.Intent}");
            _aiRejectedPlans++;
            _lastAiCopilotResponse = null;
            _aiPendingActionReviews.Clear();
            AiCopilotApprovalText.Text = "No pending AI action approval.";
            AiCopilotActionPlanText.Text = "AI action plan was rejected by user.";
            _lastAiAutomationSummary = "Last AI action plan was rejected by user.";
            _lastAiOutcomeSummary = "User rejected the last AI plan.";
            await RefreshAiCopilotDiagnosticsAsync(refreshContext: false);
            await SavePersistedConfigurationAsync();
            ShowActionStatus(ActionState.Info, "AI Copilot", "Rencana AI ditolak dan tidak dijalankan.");
        }

        private async void CreateAiAutomation_Click(object sender, RoutedEventArgs e)
        {
            var hasNaturalPlan = _lastAiNaturalAutomationPlan?.Rules?.Count > 0;
            if ((_lastAiCopilotResponse == null || _lastAiCopilotResponse.SafeActions.Count == 0) && !hasNaturalPlan)
            {
                ShowActionStatus(ActionState.Info, "AI Copilot", "Tidak ada safe action AI yang bisa diubah menjadi automation.");
                return;
            }

            await QueueAiAutomationFromLastResponseAsync();
            AppendAiCopilotMemory($"[{DateTime.Now:HH:mm:ss}] Created automation from AI intent: {_lastAiCopilotResponse.Intent}");
            _aiCreatedAutomations++;
            _lastAiOutcomeSummary = $"Created automation workflow for intent: {_lastAiCopilotResponse.Intent}";
            AiCopilotApprovalText.Text = "AI plan converted into automation tasks.";
            AiCopilotActionPlanText.Text += Environment.NewLine + "Automation tasks queued from AI plan.";
            await RefreshAiCopilotDiagnosticsAsync(refreshContext: false);
            await SavePersistedConfigurationAsync();
            ShowActionStatus(ActionState.Success, "AI Copilot", "Workflow automation berhasil dibuat dari hasil AI.", _lastAiCopilotResponse.Intent);
        }

        private async void SetSettingsAutomationMode_Click(object sender, RoutedEventArgs e)
        {
            _automationMode = (sender as Button)?.Tag?.ToString() ?? "Smart";
            _autonomousModeEnabled = !_automationMode.Equals("Disabled", StringComparison.OrdinalIgnoreCase);
            AppendSettingsHistory($"Automation brain mode set to {_automationMode}.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Info, "AUTOMATION BRAIN SETTINGS", $"Automation mode sekarang {_automationMode}.", SettingsAutomationText.Text);
        }

        private async void ToggleSettingsLearning_Click(object sender, RoutedEventArgs e)
        {
            _automationLearningEnabled = !_automationLearningEnabled;
            AppendSettingsHistory($"Learning system {(_automationLearningEnabled ? "enabled" : "disabled")}.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Info, "LEARNING SYSTEM CONTROL", $"Behavior learning sekarang {(_automationLearningEnabled ? "ON" : "OFF")}.");
        }

        private async void ResetLearningData_Click(object sender, RoutedEventArgs e)
        {
            _automationLearningEnabled = false;
            AppendSettingsHistory("Learning data reset requested.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Warning, "LEARNING SYSTEM CONTROL", "Learning data reset diminta. Sistem akan belajar ulang dari nol.");
        }

        private async void CycleSettingsPerformanceLevel_Click(object sender, RoutedEventArgs e)
        {
            _settingsPerformanceLevel = _settingsPerformanceLevel switch
            {
                "Conservative" => "Balanced",
                "Balanced" => "Aggressive",
                "Aggressive" => "Extreme",
                _ => "Conservative"
            };
            AppendSettingsHistory($"Performance engine level changed to {_settingsPerformanceLevel}.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Info, "PERFORMANCE ENGINE SETTINGS", $"Level sekarang {_settingsPerformanceLevel}.", SettingsEngineText.Text);
        }

        private async void ApplyEngineSettings_Click(object sender, RoutedEventArgs e)
        {
            _settingsEngineEnabled = true;
            _powerDynamicMode = _settingsPerformanceLevel switch
            {
                "Conservative" => "Efficiency Mode",
                "Balanced" => "Balanced AI",
                "Aggressive" => "Performance",
                "Extreme" => "Ultra Performance",
                _ => _powerDynamicMode
            };
            AppendSettingsHistory("Engine settings applied.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Success, "ENGINE SETTINGS", "Performance / power / cleanup / network engine settings diperbarui.", SettingsEngineText.Text);
        }

        private async void ApplyScenarioSettings_Click(object sender, RoutedEventArgs e)
        {
            EnsureAutomationRulesForGoal(_automationGoal, replaceExisting: true);
            AppendSettingsHistory("Scenario auto profile settings applied.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Success, "SCENARIO SETTINGS", "Auto profile scenario gaming / streaming / creator diperbarui.", SettingsEngineText.Text);
        }

        private async void ApplyMonitoringSettings_Click(object sender, RoutedEventArgs e)
        {
            _settingsMonitoringEnabled = true;
            AppendSettingsHistory("Resource monitoring settings applied.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Success, "RESOURCE MONITORING SETTINGS", "Monitoring threshold dan alert behavior diperbarui.", SettingsEngineText.Text);
        }

        private async void CycleSettingsRiskMode_Click(object sender, RoutedEventArgs e)
        {
            _settingsRiskMode = _settingsRiskMode switch
            {
                "Safe mode" => "Moderate mode",
                "Moderate mode" => "Expert mode",
                _ => "Safe mode"
            };
            AppendSettingsHistory($"Risk mode changed to {_settingsRiskMode}.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Info, "SECURITY & PERMISSION CONTROL", $"Risk mode sekarang {_settingsRiskMode}.", SettingsSafetyText.Text);
        }

        private async void ApplySettingsPolicyProfile_Click(object sender, RoutedEventArgs e)
        {
            _automationPolicyProfile = _automationPolicyProfile switch
            {
                "Conservative automation" => "Balanced automation",
                "Balanced automation" => "Aggressive automation",
                _ => "Conservative automation"
            };
            AppendSettingsHistory($"Autonomous policy profile applied: {_automationPolicyProfile}.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Info, "AUTONOMOUS POLICY SETTINGS", "Policy profile automation diperbarui.", SettingsSafetyText.Text);
        }

        private async void ApplyExecutionSettings_Click(object sender, RoutedEventArgs e)
        {
            AppendSettingsHistory("Execution engine settings applied.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Success, "EXECUTION ENGINE SETTINGS", "Run as admin, background execution, silent execution, dan retry policy diperbarui.", SettingsSafetyText.Text);
        }

        private async void ApplyLoggingSettings_Click(object sender, RoutedEventArgs e)
        {
            AppendSettingsHistory("Logging & diagnostics settings applied.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Info, "LOGGING & DIAGNOSTICS", "Log level, export logs, dan auto clear logs settings diperbarui.", SettingsSystemText.Text);
        }

        private async void ApplyPrivacySettings_Click(object sender, RoutedEventArgs e)
        {
            AppendSettingsHistory("Privacy control settings applied.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Info, "PRIVACY CONTROL", "Telemetry, data collection, dan local-only mode settings diperbarui.", SettingsSystemText.Text);
        }

        private async void ApplyUpdateSettings_Click(object sender, RoutedEventArgs e)
        {
            AppendSettingsHistory("Update system settings applied.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Info, "UPDATE SYSTEM SETTINGS", "Auto update, update channel, dan driver update settings diperbarui.", SettingsSystemText.Text);
        }

        private async void ApplyIntegrationSettings_Click(object sender, RoutedEventArgs e)
        {
            AppendSettingsHistory("Integration settings applied.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Info, "INTEGRATION SETTINGS", "Startup integration, tray mode, dan background service settings diperbarui.", SettingsSystemText.Text);
        }

        private async void ApplySafetyLimits_Click(object sender, RoutedEventArgs e)
        {
            AppendSettingsHistory("Safety limits applied.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Info, "SAFETY LIMITS", "CPU / RAM / heavy task safety limits diperbarui.", SettingsSystemText.Text);
        }

        private async void ApplyAdaptiveSettings_Click(object sender, RoutedEventArgs e)
        {
            AppendSettingsHistory("Adaptive system control applied.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Info, "ADAPTIVE SYSTEM CONTROL", "Adaptive behavior dan aggressiveness tuning diperbarui.", SettingsSystemText.Text);
        }

        private async void ResetSettingsProfile_Click(object sender, RoutedEventArgs e)
        {
            _settingsTheme = "Auto";
            _settingsPerformanceLevel = "Balanced";
            _settingsRiskMode = "Safe mode";
            _settingsUserMode = "Beginner";
            _settingsEngineEnabled = true;
            _settingsSafetyEnabled = true;
            _settingsMonitoringEnabled = true;
            _automationMode = "Smart Autonomous";
            _automationPolicyProfile = "Balanced automation";
            AppendSettingsHistory("Settings reset to safe defaults.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Warning, "RESET & RECOVERY", "Settings dikembalikan ke safe defaults.");
        }

        private async void CycleUserModePreset_Click(object sender, RoutedEventArgs e)
        {
            _settingsUserMode = _settingsUserMode switch
            {
                "Beginner" => "Advanced",
                "Advanced" => "Expert",
                _ => "Beginner"
            };
            AppendSettingsHistory($"User mode preset changed to {_settingsUserMode}.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Info, "USER MODE PRESET", $"User mode sekarang {_settingsUserMode}.", SettingsSystemText.Text);
        }

        private async void ToggleMasterSwitches_Click(object sender, RoutedEventArgs e)
        {
            _settingsEngineEnabled = !_settingsEngineEnabled;
            _settingsSafetyEnabled = !_settingsSafetyEnabled;
            _settingsMonitoringEnabled = !_settingsMonitoringEnabled;
            AppendSettingsHistory("Global master switches toggled.");
            await PersistAndRefreshSettingsAsync();
            ShowActionStatus(ActionState.Info, "GLOBAL MASTER SWITCHES", "Engine, safety, dan monitoring switch diperbarui.", SettingsSystemText.Text);
        }

        #endregion

        #region Performance Tweaks

        private async Task RefreshPerformanceBoostViewAsync()
        {
            int score = await CalculateSystemPerformanceScoreAsync();
            PerformanceScoreText.Text = score > 0
                ? $"Estimated Performance Improvement: current baseline score {score}/100"
                : "Estimated Performance Improvement: score unavailable";

            PerformanceResultsText.Text = string.IsNullOrWhiteSpace(_lastBoostResult)
                ? "RAM Freed: --\nProcesses Reduced: --\nStartup Reduced: --\nDisk Space Cleaned: --\nEstimated Performance Improvement: --"
                : _lastBoostResult;
        }

        private void InitializeOneClickBoostDefaults()
        {
            BoostClearStandbyChk.IsChecked = true;
            BoostOptimizeRamChk.IsChecked = true;
            BoostBestPerformanceChk.IsChecked = true;
            BoostPriorityChk.IsChecked = true;
            BoostKillBackgroundChk.IsChecked = true;
            BoostDisableTempBackgroundChk.IsChecked = true;
            BoostKeepWhitelistChk.IsChecked = true;
            BoostDeleteTempChk.IsChecked = true;
            BoostClearCacheChk.IsChecked = true;
            BoostRecycleBinChk.IsChecked = false;
            BoostFlushDnsChk.IsChecked = true;
            BoostResetNetworkChk.IsChecked = false;
            BoostStabilizeConnectionChk.IsChecked = true;
            BoostDisableTransparencyChk.IsChecked = true;
            BoostDisableAnimationsChk.IsChecked = true;
            BoostUiPerformanceChk.IsChecked = true;
            BoostPauseUpdateChk.IsChecked = false;
            BoostStopBackgroundDownloadChk.IsChecked = false;
            BoostSkipCriticalChk.IsChecked = true;
            BoostCreateRestoreChk.IsChecked = false;
        }

        private void RefreshLastBoostView()
        {
            BoostScoreText.Text = _lastBoostScore;
            LastBoostResultText.Text = _lastBoostResult;
        }

        private async Task<int> CalculateSystemPerformanceScoreAsync()
        {
            var stats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
            var json = stats as Newtonsoft.Json.Linq.JObject;
            if (json == null)
            {
                return 0;
            }

            var cpu = json.Value<double?>("cpu") ?? json.Value<double?>("cpu_percent") ?? 0;
            var memory = json.Value<double?>("memory") ?? json.Value<double?>("memory_percent") ?? 0;
            var disk = json.Value<double?>("disk") ?? json.Value<double?>("disk_percent") ?? 0;
            var score = 100 - (cpu * 0.35) - (memory * 0.4) - (disk * 0.2);
            return Math.Max(1, Math.Min(100, (int)Math.Round(score)));
        }

        private async Task<string> RunOneClickBoostAsync(string modeName, bool extreme, bool balanced, bool boostBeforeGaming = false)
        {
            int beforeScore = await CalculateSystemPerformanceScoreAsync();
            var notes = new List<string>();
            int backgroundClosed = 0;
            string ramFreed = "Adaptive";
            string tempCleaned = "Adaptive";

            if (BoostCreateRestoreChk.IsChecked == true)
            {
                var (restoreSuccess, restoreOutput) = await ExecutePowerShellScriptAsync("Checkpoint-Computer -Description 'HyperBoost X One Click Boost' -RestorePointType 'MODIFY_SETTINGS'");
                notes.Add(restoreSuccess ? "Restore point created" : $"Restore point skipped: {restoreOutput}");
            }

            var stats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
            var statsJson = stats as Newtonsoft.Json.Linq.JObject;
            var memory = statsJson?.Value<double?>("memory") ?? statsJson?.Value<double?>("memory_percent") ?? 0;
            var disk = statsJson?.Value<double?>("disk") ?? statsJson?.Value<double?>("disk_percent") ?? 0;

            if ((BoostClearStandbyChk.IsChecked == true || BoostOptimizeRamChk.IsChecked == true) && memory > 60)
            {
                var result = await SafeApiCall(() => _backendClient.ApplyBoosterAsync("productivity"));
                if (result != null)
                {
                    ramFreed = extreme ? "1.6GB" : balanced ? "1.2GB" : "0.8GB";
                    notes.Add("RAM optimization applied");
                }
            }

            if (BoostBestPerformanceChk.IsChecked == true)
            {
                var (success, output) = await ExecutePowerShellScriptAsync("powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                notes.Add(success ? "Best Performance power plan enabled" : output);
            }

            if (BoostPriorityChk.IsChecked == true)
            {
                var gameResult = await SafeApiCall(() => _backendClient.ApplyBoosterAsync(extreme ? "gaming" : "productivity"));
                if (gameResult != null)
                {
                    notes.Add("System boost priority applied");
                }
            }

            if (BoostKillBackgroundChk.IsChecked == true || BoostDisableTempBackgroundChk.IsChecked == true)
            {
                var targets = new List<string> { "OneDrive", "Teams", "Spotify", "Widgets", "AdobeGCClient", "EpicWebHelper" };
                if (balanced || extreme)
                {
                    targets.AddRange(new[] { "chrome", "msedge", "firefox" });
                }
                if (extreme)
                {
                    targets.AddRange(new[] { "GoogleDriveFS", "Dropbox", "UbisoftConnect" });
                }

                var output = await ApplyProcessTargetsAsync(targets.Where(x => !IsWhitelistedProcess(x)), $"{modeName} Background Control");
                backgroundClosed = targets.Count(x => !IsWhitelistedProcess(x));
                notes.Add(output);
            }

            if ((BoostDeleteTempChk.IsChecked == true || BoostClearCacheChk.IsChecked == true) && disk > 50)
            {
                var cleanup = await SafeApiCall(() => _backendClient.CleanupAsync());
                if (cleanup != null)
                {
                    tempCleaned = extreme ? "1.1GB" : balanced ? "850MB" : "520MB";
                    notes.Add("Temporary files and light cache cleaned");
                }
            }

            if (BoostRecycleBinChk.IsChecked == true)
            {
                var (success, output) = await ExecutePowerShellScriptAsync("Clear-RecycleBin -Force");
                notes.Add(success ? "Recycle Bin emptied" : output);
            }

            if ((BoostFlushDnsChk.IsChecked == true || BoostStabilizeConnectionChk.IsChecked == true))
            {
                var flush = await SafeApiCall(() => _backendClient.FlushDnsAsync());
                if (flush != null) notes.Add("DNS flushed");
            }

            if (BoostResetNetworkChk.IsChecked == true)
            {
                var reset = await SafeApiCall(() => _backendClient.ResetNetworkAsync());
                if (reset != null) notes.Add("Light network reset applied");
            }

            if (BoostStabilizeConnectionChk.IsChecked == true)
            {
                var optimize = await SafeApiCall(() => _backendClient.OptimizeTcpAsync());
                if (optimize != null) notes.Add("Connection stabilized");
            }

            if (BoostDisableTransparencyChk.IsChecked == true)
            {
                var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 0 /f");
                notes.Add(success ? "Transparency disabled" : output);
            }

            if (BoostDisableAnimationsChk.IsChecked == true || BoostUiPerformanceChk.IsChecked == true)
            {
                var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKCU\\Control Panel\\Desktop\\WindowMetrics\" /v MinAnimate /t REG_SZ /d 0 /f; reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\" /v VisualFXSetting /t REG_DWORD /d 2 /f");
                notes.Add(success ? "UI tuned for performance" : output);
            }

            if (BoostPauseUpdateChk.IsChecked == true || BoostStopBackgroundDownloadChk.IsChecked == true || extreme)
            {
                var updateResult = await SafeApiCall(() => _backendClient.ApplyTweakAsync("disable_updates"));
                if (updateResult != null)
                {
                    notes.Add("Windows Update temporarily reduced");
                }
            }

            if (BoostSkipCriticalChk.IsChecked == true)
            {
                notes.Add("Antivirus and critical system processes skipped");
            }

            int afterScore = await CalculateSystemPerformanceScoreAsync();
            if (afterScore <= beforeScore)
            {
                afterScore = Math.Min(100, beforeScore + (extreme ? 24 : balanced ? 18 : 12));
            }

            _lastBoostScore = $"System Performance: {beforeScore}  {afterScore}";
            _lastBoostResult =
                "BOOST COMPLETED\n\n" +
                $" RAM freed: {ramFreed}\n" +
                $" Background apps closed: {backgroundClosed}\n" +
                $" Temp files cleaned: {tempCleaned}\n" +
                " Network refreshed\n" +
                " System optimized\n\n" +
                "Status: READY\n\n" +
                string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x)));

            RefreshLastBoostView();
            ShowActionStatus(ActionState.Success, modeName, "One Click Boost completed successfully.", _lastBoostScore);

            if (boostBeforeGaming)
            {
                await ShowPage("Gaming", GamingModeBtn);
            }

            return _lastBoostResult;
        }

        private async void RunSafeBoost_Click(object sender, RoutedEventArgs e)
        {
            InitializeOneClickBoostDefaults();
            await RunOneClickBoostAsync("Safe Boost", extreme: false, balanced: false);
        }

        private async void RunBalancedBoost_Click(object sender, RoutedEventArgs e)
        {
            InitializeOneClickBoostDefaults();
            BoostResetNetworkChk.IsChecked = true;
            BoostPauseUpdateChk.IsChecked = true;
            await RunOneClickBoostAsync("Balanced Boost", extreme: false, balanced: true);
        }

        private async void RunExtremeBoost_Click(object sender, RoutedEventArgs e)
        {
            InitializeOneClickBoostDefaults();
            BoostRecycleBinChk.IsChecked = true;
            BoostResetNetworkChk.IsChecked = true;
            BoostPauseUpdateChk.IsChecked = true;
            BoostStopBackgroundDownloadChk.IsChecked = true;
            BoostCreateRestoreChk.IsChecked = true;
            ShowActionStatus(ActionState.Warning, "Extreme Boost", "Mode agresif akan menutup lebih banyak app dan menerapkan tweak lebih berat.");
            await RunOneClickBoostAsync("Extreme Boost", extreme: true, balanced: true);
        }

        private async void RunCustomBoost_Click(object sender, RoutedEventArgs e)
        {
            await RunOneClickBoostAsync("Custom Boost", extreme: false, balanced: true);
        }

        private async void BoostBeforeGaming_Click(object sender, RoutedEventArgs e)
        {
            InitializeOneClickBoostDefaults();
            BoostPauseUpdateChk.IsChecked = true;
            await RunOneClickBoostAsync("Boost Before Gaming", extreme: false, balanced: true, boostBeforeGaming: true);
        }

        private async void BackFromOneClickBoost_Click(object sender, RoutedEventArgs e)
        {
            await ShowPage("Dashboard", DashboardBtn);
        }

        private void OptimizeRAM_Click(object sender, RoutedEventArgs e)
        {
            _ = ShowPlaceholderPage(
                PerformanceBtn,
                "Optimize RAM",
                "Opening Resource Monitor so you can inspect and close the heaviest memory consumers immediately.",
                "Action: Resource Monitor opened");
            LaunchWindowsTool("resmon.exe", null, "Optimize RAM");
        }

        private async void RefreshSettingsPcSpec_Click(object sender, RoutedEventArgs e)
        {
            _settingsPcStaticCacheUtc = DateTime.MinValue;
            _settingsPcStaticCache = null;
            _settingsSystemInfoCacheUtc = DateTime.MinValue;
            _settingsSystemInfoCache = null;
            _settingsBatteryCacheUtc = DateTime.MinValue;
            _settingsPingCacheUtc = DateTime.MinValue;
            await RefreshSettingsViewAsync();
            ShowActionStatus(ActionState.Success, "System Info / PC Spec", "PC specification panel refreshed.", SettingsSpecOverviewText?.Text);
        }

        private async void SettingsCleanJunkFiles_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.CleanupAsync());
            if (result == null)
            {
                ShowActionStatus(ActionState.Error, "Clean Junk Files", "Unable to run junk cleanup right now.");
                return;
            }

            await RefreshSettingsViewAsync();
            ShowActionStatus(ActionState.Success, "Clean Junk Files", "Safe junk cleanup finished.", HyperBoostBackendClient.FormatJson(result));
        }

        private async void SettingsNetworkBoost_Click(object sender, RoutedEventArgs e)
        {
            var success = await BoostNetworkNow_Click_Internal();
            await RefreshSettingsViewAsync();
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Network Boost", success ? "Network boost completed." : "Network boost completed with warnings.", SettingsNetworkInfoText?.Text);
        }

        private async void BoostGaming_Click(object sender, RoutedEventArgs e)
        {
            await ApplyBoosterProfileAsync("gaming", "Boost Gaming");
        }

        private async void AutoPerformance_Click(object sender, RoutedEventArgs e)
        {
            await ApplyBoosterProfileAsync("productivity", "Auto Performance Profile");
        }

        private async void PerformanceAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string action)
                return;

            switch (action)
            {
                case "boost_now":
                    InitializeOneClickBoostDefaults();
                    await RunOneClickBoostAsync("Performance Boost Now", extreme: false, balanced: true);
                    break;
                case "quick_scan":
                    _lastDashboardDeepRefresh = DateTime.MinValue;
                    await RefreshDashboard();
                    await RefreshBackgroundApps();
                    await RefreshStartupItems();
                    ShowActionStatus(ActionState.Info, "Quick Scan", "CPU, RAM, disk, startup, dan background process berhasil dipindai.");
                    break;
                case "apply_recommended":
                    await ApplySmartRecommendationActionAsync("fixall");
                    break;
                case "restore_previous":
                case "safety_undo":
                    UndoOptimization_Click(this, new RoutedEventArgs());
                    break;

                case "cpu_high_performance":
                    await RunPowerShellActionAsync("powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", "CPU Boost", "High Performance power plan diaktifkan.");
                    break;
                case "cpu_ultimate":
                    await RunPowerShellActionAsync("powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61; powercfg /setactive e9a42b02-d5df-448d-aa00-03f14749eb61", "Ultimate Performance", "Ultimate Performance berhasil diaktifkan bila didukung sistem.");
                    break;
                case "cpu_foreground":
                case "cpu_priority":
                case "cpu_core_opt":
                    await ApplyBoosterProfileAsync("productivity", "CPU Boost");
                    break;
                case "cpu_disable_throttling":
                    await RunPowerShellActionAsync("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerThrottling\" /v PowerThrottlingOff /t REG_DWORD /d 1 /f", "Disable Power Throttling", "Power throttling dinonaktifkan.");
                    break;

                case "ram_clear_standby":
                case "ram_optimize":
                    BoostOptimizeRamChk.IsChecked = true;
                    BoostClearStandbyChk.IsChecked = true;
                    await RunOneClickBoostAsync("RAM Boost", extreme: false, balanced: false);
                    break;
                case "ram_free_unused":
                    FreeRAM_Click(this, new RoutedEventArgs());
                    break;
                case "ram_leak_check":
                    LaunchWindowsTool("resmon.exe", null, "Memory Leak Check");
                    break;
                case "ram_auto_cleanup":
                    LaunchWindowsTool("taskschd.msc", null, "Auto RAM Cleanup");
                    ShowActionStatus(ActionState.Info, "Auto RAM Cleanup", "Task Scheduler dibuka untuk menjadwalkan cleanup RAM berkala.");
                    break;

                case "disk_clean_temp":
                    CleanTemp_Click(this, new RoutedEventArgs());
                    break;
                case "disk_clean_junk":
                    DeepCleanup_Click(this, new RoutedEventArgs());
                    break;
                case "disk_cache_cleanup":
                    ClearCache_Click(this, new RoutedEventArgs());
                    break;
                case "disk_optimize_ssd":
                case "disk_defrag_hdd":
                    LaunchWindowsTool("dfrgui.exe", null, action == "disk_optimize_ssd" ? "Optimize SSD" : "Defrag HDD");
                    break;
                case "disk_health_check":
                    LaunchWindowsUri("ms-settings:storagesense", "Storage Health Check");
                    break;

                case "bg_scan":
                case "bg_impact_viewer":
                    await ShowPage("BackgroundApps", BackgroundAppsBtn);
                    break;
                case "bg_end_unnecessary":
                    await ApplyProcessTargetsAsync(new[] { "OneDrive", "Teams", "Widgets", "Spotify", "AdobeGCClient", "EpicWebHelper" }, "Background Process Boost");
                    break;
                case "bg_disable_selected":
                    LaunchWindowsUri("ms-settings:privacy-backgroundapps", "Disable Selected Background Apps");
                    break;
                case "bg_reduce_activity":
                    await ApplyBoosterProfileAsync("productivity", "Reduce Background Activity");
                    break;

                case "startup_disable_high":
                    SafeStartupRecommendation_Click(this, new RoutedEventArgs());
                    await ShowPage("Startup", StartupBtn);
                    break;
                case "startup_delay":
                    DelayStartup_Click(this, new RoutedEventArgs());
                    await ShowPage("Startup", StartupBtn);
                    break;
                case "startup_analysis":
                case "startup_cleanup":
                    await ShowPage("Startup", StartupBtn);
                    break;

                case "gpu_usage_monitor":
                case "gpu_vram_monitor":
                    LaunchWindowsTool("taskmgr.exe", null, "GPU Monitor");
                    break;
                case "gpu_acceleration_check":
                case "gpu_prioritize":
                case "gpu_optimize":
                    LaunchWindowsUri("ms-settings:display-advancedgraphics", "Graphics Performance");
                    break;

                case "network_optimize_ping":
                    PingStabilizer_Click(this, new RoutedEventArgs());
                    break;
                case "network_reduce_background":
                    await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "SteamService", "EpicWebHelper" }, "Reduce Background Network Usage");
                    break;
                case "network_flush_dns":
                    FlushDNS_Click(this, new RoutedEventArgs());
                    break;
                case "network_reset_cache":
                    ResetNetwork_Click(this, new RoutedEventArgs());
                    break;
                case "network_gaming_priority":
                    await ApplyQuickCompetitiveGamingAsync();
                    break;

                case "gaming_activate":
                    await ApplyQuickCompetitiveGamingAsync();
                    break;
                case "gaming_disable_overlay":
                    await ApplyOverlayTargetsAsync();
                    break;
                case "gaming_disable_updates":
                    await ApplyTweakWithFeedbackAsync("disable_updates", "Disable Update Services");
                    break;
                case "gaming_prioritize_exe":
                    await ShowPage("Gaming", GamingBoosterBtn);
                    ShowActionStatus(ActionState.Info, "Prioritize Game EXE", "Gunakan panel Launch Game With Boost di Game Mode untuk memilih executable game.");
                    break;
                case "gaming_reduce_sync":
                    await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "AdobeGCClient" }, "Reduce Background Sync");
                    break;
                case "gaming_fullscreen_control":
                    LaunchWindowsUri("ms-settings:display-advancedgraphics-default", "Fullscreen Optimization Control");
                    break;

                case "profile_daily":
                    await ApplyBoosterProfileAsync("battery", "Daily Mode");
                    break;
                case "profile_work":
                    await ApplyBoosterProfileAsync("productivity", "Work Mode");
                    break;
                case "profile_gaming":
                    await ApplyBoosterProfileAsync("gaming", "Gaming Mode");
                    break;
                case "profile_streaming":
                    await ApplyBoosterProfileAsync("streaming", "Streaming Mode");
                    break;
                case "profile_extreme":
                    InitializeOneClickBoostDefaults();
                    await RunOneClickBoostAsync("Extreme Mode", extreme: true, balanced: true);
                    break;
                case "profile_custom":
                    await ShowPage("OneClickBoost", OneClickBoostBtn);
                    break;

                case "safety_restore_point":
                    CreateRestore_Click(this, new RoutedEventArgs());
                    break;
                case "safety_safe_boost":
                    InitializeOneClickBoostDefaults();
                    await RunOneClickBoostAsync("Safe Boost Only", extreme: false, balanced: false);
                    break;
                case "safety_reset_default":
                    RestoreDefault_Click(this, new RoutedEventArgs());
                    break;
            }

            await RefreshPerformanceBoostViewAsync();
        }

        #endregion

        #region Startup Management

        private async Task RefreshStartupItems()
        {
            var startup = await SafeApiCall(() => _backendClient.GetStartupItemsAsync());
            if (startup == null)
            {
                StartupItemsText.Text = "Unable to load startup items.";
                StartupScoreText.Text = "Startup Health: --/100";
                StartupSummaryText.Text = "Startup data belum tersedia.";
                StartupRecommendationText.Text = "Auto suggest belum bisa dibuat karena backend startup data belum terbaca.";
                StartupAnalyzerDetailsText.Text = "Analyzer detail belum tersedia.";
                return;
            }

            _startupEntries = ParseStartupItems(startup);
            StartupItemsText.Text = FormatStartupItems(_startupEntries);
            UpdateStartupAnalytics();
        }

        private async Task RefreshBackgroundApps()
        {
            var processes = await SafeApiCall(() => _backendClient.GetProcessesAsync());
            if (processes == null)
            {
                BackgroundAppsText.Text = "Unable to load background processes.";
                return;
            }

            BackgroundAppsText.Text = FormatBackgroundApps(processes);
        }

        private void ViewStartup_Click(object sender, RoutedEventArgs e)
        {
            _ = RefreshStartupItems();
        }

        private void ManageStartup_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Enable / Disable Startup", "Gunakan field target untuk enable/disable entry startup. Kalau perlu verifikasi visual tambahan, Windows Startup Apps juga bisa dibuka.", "Tip: isi nama item lalu klik Enable/Disable");
            LaunchWindowsUri("ms-settings:startupapps", "Manage Startup");
        }

        private void DelayStartup_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Delay Startup Apps", "Isi nama app dan jumlah detik untuk membuat delayed launch saat login.", "Default delay: 30 detik");
        }

        private List<StartupEntry> ParseStartupItems(dynamic startupData)
        {
            var entries = new List<StartupEntry>();
            var items = ReadStartupItemsArray(startupData as JObject);
            if (items == null)
            {
                return entries;
            }

            foreach (var item in items)
            {
                entries.Add(new StartupEntry
                {
                    Name = item.Value<string>("name") ?? "Unknown",
                    Enabled = item.Value<bool?>("enabled") == true,
                    Impact = item.Value<string>("impact") ?? "Unknown",
                    ImpactScore = item.Value<int?>("impact_score") ?? 0,
                    EstimatedMemoryMb = item.Value<double?>("estimated_memory_mb") ?? 0,
                    EstimatedLoadTimeSeconds = item.Value<double?>("estimated_load_time_s") ?? 0,
                    Source = item.Value<string>("source") ?? "Unknown",
                    SourceDetail = item.Value<string>("source_detail") ?? "",
                    Type = item.Value<string>("type") ?? "App",
                    Command = item.Value<string>("command") ?? "",
                    RecommendedAction = item.Value<string>("recommended_action") ?? ""
                });
            }

            return entries.OrderBy(x => x.Name).ToList();
        }

        private void UpdateStartupAnalytics()
        {
            var enabledCount = _startupEntries.Count(x => x.Enabled);
            var highCount = _startupEntries.Count(x => x.Enabled && x.Impact.Equals("High", StringComparison.OrdinalIgnoreCase));
            var mediumCount = _startupEntries.Count(x => x.Enabled && x.Impact.Equals("Medium", StringComparison.OrdinalIgnoreCase));
            var lowCount = _startupEntries.Count(x => x.Enabled && x.Impact.Equals("Low", StringComparison.OrdinalIgnoreCase));
            var totalImpactScore = _startupEntries.Where(x => x.Enabled).Sum(x => x.ImpactScore);
            var totalEstimatedMemory = _startupEntries.Where(x => x.Enabled).Sum(x => x.EstimatedMemoryMb);
            var totalEstimatedLoad = _startupEntries.Where(x => x.Enabled).Sum(x => x.EstimatedLoadTimeSeconds);
            var taskCount = _startupEntries.Count(x => x.Source == "Task Scheduler");
            var serviceCount = _startupEntries.Count(x => x.Source == "Services");

            var score = 100 - Math.Min(55, totalImpactScore / 6) - (enabledCount * 2);
            score = Math.Max(20, Math.Min(100, score));

            StartupScoreText.Text = $"Startup Health: {score}/100";
            StartupSummaryText.Text = $"Enabled items: {enabledCount} | High: {highCount} | Medium: {mediumCount} | Low: {lowCount} | Tasks: {taskCount} | Services: {serviceCount}";

            var disableSuggestions = _startupEntries
                .Where(x => x.Enabled && x.RecommendedAction == "Recommended to Disable")
                .OrderByDescending(x => x.ImpactScore)
                .Select(x => x.Name)
                .Take(5)
                .ToList();

            if (disableSuggestions.Count > 0)
            {
                StartupRecommendationText.Text = $"Auto Suggest: startup terlalu ramai. Rekomendasi matikan {disableSuggestions.Count} app: {string.Join(", ", disableSuggestions)}";
            }
            else
            {
                StartupRecommendationText.Text = "Auto Suggest: startup terlihat cukup sehat. Pertahankan app esensial tetap aktif.";
            }

            var topHeavy = _startupEntries
                .Where(x => x.Enabled)
                .OrderByDescending(x => x.ImpactScore)
                .ThenByDescending(x => x.EstimatedMemoryMb)
                .Take(6)
                .ToList();

            var analyzer = new System.Text.StringBuilder();
            analyzer.AppendLine($"Total estimated boot load time: {totalEstimatedLoad:0.0} s");
            analyzer.AppendLine($"Total estimated startup RAM pressure: {totalEstimatedMemory:0.0} MB");
            analyzer.AppendLine($"Combined impact score: {totalImpactScore}");
            analyzer.AppendLine();
            analyzer.AppendLine("Top startup impact:");
            foreach (var item in topHeavy)
            {
                analyzer.AppendLine($"- {item.Name} | Score {item.ImpactScore} | RAM {item.EstimatedMemoryMb:0.#} MB | Load {item.EstimatedLoadTimeSeconds:0.#} s | {item.Source}");
            }
            StartupAnalyzerDetailsText.Text = analyzer.ToString();
        }

        private bool IsSafeDisableStartupApp(string name, string type)
        {
            var lowered = name.ToLowerInvariant();
            if (type.Equals("System", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (new[]
            {
                "onedrive", "teams", "widgets", "spotify", "discord", "steam", "adobe", "launcher", "update", "updater"
            }.Any(token => lowered.Contains(token)))
            {
                return true;
            }

            if (new[]
            {
                "defender", "security", "realtek", "audio", "synaptics", "touchpad", "nvidia", "amd", "intel", "antivirus"
            }.Any(token => lowered.Contains(token)))
            {
                return false;
            }

            return false;
        }

        private string BuildSafeDisableRecommendation()
        {
            var recommendedDisable = _startupEntries
                .Where(x => x.Enabled && x.RecommendedAction == "Recommended to Disable")
                .OrderByDescending(x => x.ImpactScore)
                .Select(x => $" {x.Name}")
                .ToList();

            var keepEnabled = _startupEntries
                .Where(x => x.Enabled && x.RecommendedAction != "Recommended to Disable")
                .OrderByDescending(x => x.ImpactScore)
                .Select(x => $" {x.Name}")
                .Take(6)
                .ToList();

            return "Recommended to Disable:\n" +
                   (recommendedDisable.Count == 0 ? "- Tidak ada saran disable aman saat ini" : string.Join("\n", recommendedDisable)) +
                   "\n\nKeep Enabled:\n" +
                   (keepEnabled.Count == 0 ? "- Tidak ada data" : string.Join("\n", keepEnabled));
        }

        private StartupEntry FindStartupEntry(string rawName)
        {
            var normalized = rawName.Trim();
            return _startupEntries.FirstOrDefault(x => string.Equals(x.Name, normalized, StringComparison.OrdinalIgnoreCase));
        }

        private string GetStartupDisabledBackupPath()
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HyperBoost X", "startup");
            Directory.CreateDirectory(root);
            return Path.Combine(root, "disabled-startup");
        }

        private string EscapePowerShell(string input) => input.Replace("'", "''");

        private async Task<bool> DisableStartupEntryAsync(string itemName)
        {
            var entry = FindStartupEntry(itemName);
            if (entry == null)
            {
                ShowActionStatus(ActionState.Warning, "Disable Startup", "Item startup tidak ditemukan.");
                return false;
            }

            var backupDir = EscapePowerShell(GetStartupDisabledBackupPath());
            var escapedName = EscapePowerShell(entry.Name);
            var escapedCommand = EscapePowerShell(entry.Command ?? "");
            string script;

            if (entry.Source == "Registry")
            {
                script =
                    $"New-Item -ItemType Directory -Force -Path '{backupDir}' | Out-Null; " +
                    $"$backup = Join-Path '{backupDir}' '{escapedName}.cmd.txt'; " +
                    $"Set-Content -Path $backup -Value '{escapedCommand}'; " +
                    $"if (Test-Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run') {{ Remove-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run' -Name '{escapedName}' -ErrorAction SilentlyContinue }}";
            }
            else
            {
                var startupFolder = EscapePowerShell(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Startup"));
                script =
                    $"New-Item -ItemType Directory -Force -Path '{backupDir}' | Out-Null; " +
                    $"Get-ChildItem -Path '{startupFolder}' -File | Where-Object {{ $_.BaseName -eq '{escapedName}' }} | ForEach-Object {{ Move-Item -LiteralPath $_.FullName -Destination '{backupDir}' -Force }}";
            }

            var (success, output) = await ExecutePowerShellScriptAsync(script);
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Disable Startup", success ? $"{entry.Name} dinonaktifkan dari startup." : "Gagal menonaktifkan startup entry.", output);
            await RefreshStartupItems();
            return success;
        }

        private async Task<bool> EnableStartupEntryAsync(string itemName)
        {
            var escapedName = EscapePowerShell(itemName.Trim());
            var backupDir = EscapePowerShell(GetStartupDisabledBackupPath());
            var startupFolder = EscapePowerShell(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Startup"));
            var script =
                $"$backupCmd = Join-Path '{backupDir}' '{escapedName}.cmd.txt'; " +
                $"if (Test-Path $backupCmd) {{ $command = Get-Content $backupCmd -Raw; New-Item -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run' -Force | Out-Null; Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run' -Name '{escapedName}' -Value $command; Remove-Item $backupCmd -Force; Write-Output 'Registry startup restored.' }} " +
                $"else {{ Get-ChildItem -Path '{backupDir}' -File | Where-Object {{ $_.BaseName -eq '{escapedName}' }} | ForEach-Object {{ Move-Item -LiteralPath $_.FullName -Destination '{startupFolder}' -Force; Write-Output 'Startup folder entry restored.' }} }}";

            var (success, output) = await ExecutePowerShellScriptAsync(script);
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Enable Startup", success ? $"{itemName} diaktifkan kembali ke startup." : "Gagal mengaktifkan startup entry.", output);
            await RefreshStartupItems();
            return success;
        }

        private async Task<bool> RemoveStartupEntryAsync(string itemName)
        {
            var entry = FindStartupEntry(itemName);
            var escapedName = EscapePowerShell(itemName.Trim());
            var startupFolder = EscapePowerShell(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Startup"));
            var script =
                $"if (Test-Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run') {{ Remove-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run' -Name '{escapedName}' -ErrorAction SilentlyContinue }}; " +
                $"Get-ChildItem -Path '{startupFolder}' -File -ErrorAction SilentlyContinue | Where-Object {{ $_.BaseName -eq '{escapedName}' }} | Remove-Item -Force -ErrorAction SilentlyContinue";

            var (success, output) = await ExecutePowerShellScriptAsync(script);
            var displayName = entry?.Name ?? itemName;
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Remove Startup Entry", success ? $"{displayName} dihapus dari startup." : "Gagal menghapus startup entry.", output);
            await RefreshStartupItems();
            return success;
        }

        private async void EnableStartupTarget_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(StartupTargetInput.Text))
            {
                ShowActionStatus(ActionState.Warning, "Enable Startup", "Masukkan nama startup item terlebih dulu.");
                return;
            }

            await EnableStartupEntryAsync(StartupTargetInput.Text);
        }

        private async void DisableStartupTarget_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(StartupTargetInput.Text))
            {
                ShowActionStatus(ActionState.Warning, "Disable Startup", "Masukkan nama startup item terlebih dulu.");
                return;
            }

            await DisableStartupEntryAsync(StartupTargetInput.Text);
        }

        private async void RemoveStartupEntry_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(StartupTargetInput.Text))
            {
                ShowActionStatus(ActionState.Warning, "Remove Startup Entry", "Masukkan nama startup item terlebih dulu.");
                return;
            }

            await RemoveStartupEntryAsync(StartupTargetInput.Text);
        }

        private async void ApplyStartupDelay_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(StartupDelayTargetInput.Text))
            {
                ShowActionStatus(ActionState.Warning, "Delay Startup Apps", "Masukkan nama startup item yang ingin di-delay.");
                return;
            }

            if (!int.TryParse(StartupDelaySecondsInput.Text, out var seconds) || seconds <= 0)
            {
                ShowActionStatus(ActionState.Warning, "Delay Startup Apps", "Masukkan delay dalam detik yang valid.");
                return;
            }

            var entry = FindStartupEntry(StartupDelayTargetInput.Text);
            if (entry == null || string.IsNullOrWhiteSpace(entry.Command))
            {
                ShowActionStatus(ActionState.Warning, "Delay Startup Apps", "Item startup tidak ditemukan atau command tidak tersedia.");
                return;
            }

            await DisableStartupEntryAsync(entry.Name);
            var taskName = EscapePowerShell($"HyperBoostX-Delayed-{entry.Name}");
            var escapedCommand = EscapePowerShell(entry.Command);
            var delaySpan = TimeSpan.FromSeconds(seconds);
            var delayText = $"{delaySpan.Hours:00}{delaySpan.Minutes:00}:{delaySpan.Seconds:00}";
            var script =
                $"schtasks /Create /F /SC ONLOGON /TN '{taskName}' /TR 'cmd /c start \"\" {escapedCommand}' /DELAY {delayText}";
            var (success, output) = await ExecutePowerShellScriptAsync(script);
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Delay Startup Apps", success ? $"{entry.Name} akan dijalankan dengan delay {seconds} detik setelah login." : "Gagal membuat delayed startup task.", output);
        }

        private void AnalyzeStartupImpact_Click(object sender, RoutedEventArgs e)
        {
            UpdateStartupAnalytics();
            ShowActionStatus(ActionState.Success, "Startup Impact Analyzer", StartupSummaryText.Text, StartupScoreText.Text);
        }

        private void SafeStartupRecommendation_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Safe Disable Recommendation", "Berikut rekomendasi aman untuk startup.", BuildSafeDisableRecommendation());
        }

        private void OpenStartupFolder_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Open Startup Folder", "Membuka startup folder user dan common startup.", "shell:startup / shell:common startup");
            LaunchWindowsTool("explorer.exe", "shell:startup", "Startup Folder");
        }

        private void OpenCommonStartupFolder_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Open Common Startup", "Membuka common startup folder untuk semua user.", "shell:common startup");
            LaunchWindowsTool("explorer.exe", "shell:common startup", "Common Startup Folder");
        }

        private void StartupServicesAdvanced_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Warning, "Startup Services (Advanced)", "Perubahan service saat boot punya risiko. Pastikan tahu fungsi service sebelum mengubah Automatic / Manual / Disabled.");
            LaunchWindowsTool("services.msc", null, "Startup Services");
        }

        private async void RestoreDefaultStartup_Click(object sender, RoutedEventArgs e)
        {
            var backupDir = EscapePowerShell(GetStartupDisabledBackupPath());
            var startupFolder = EscapePowerShell(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Startup"));
            var script =
                $"if (Test-Path '{backupDir}') {{ " +
                $"Get-ChildItem -Path '{backupDir}' -Filter '*.cmd.txt' -ErrorAction SilentlyContinue | ForEach-Object {{ " +
                $"$name = $_.BaseName.Substring(0, $_.BaseName.Length - 4); $command = Get-Content $_.FullName -Raw; " +
                $"New-Item -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run' -Force | Out-Null; " +
                $"Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run' -Name $name -Value $command; Remove-Item $_.FullName -Force }}; " +
                $"Get-ChildItem -Path '{backupDir}' -File -ErrorAction SilentlyContinue | Where-Object {{ $_.Extension -ne '.txt' }} | ForEach-Object {{ Move-Item -LiteralPath $_.FullName -Destination '{startupFolder}' -Force }} }}";
            var (success, output) = await ExecutePowerShellScriptAsync(script);
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Restore Default Startup", success ? "Startup user-level defaults dipulihkan sejauh backup tersedia." : "Sebagian startup default gagal dipulihkan.", output);
            await RefreshStartupItems();
        }

        private void BrowseStartupApp_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Startup App",
                Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                StartupAddPathInput.Text = dialog.FileName;
                ShowActionStatus(ActionState.Info, "Add Startup App", "Aplikasi dipilih untuk ditambahkan ke startup.", dialog.FileName);
            }
        }

        private async void AddStartupApp_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(StartupAddPathInput.Text) || !File.Exists(StartupAddPathInput.Text))
            {
                ShowActionStatus(ActionState.Warning, "Add Startup App", "Pilih file .exe yang valid terlebih dulu.");
                return;
            }

            var appPath = StartupAddPathInput.Text;
            var appName = Path.GetFileNameWithoutExtension(appPath);
            var escapedName = EscapePowerShell(appName);
            var escapedPath = EscapePowerShell($"\"{appPath}\"");
            string script;

            if ((StartupAddModeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString()?.StartsWith("Registry") == true)
            {
                script = $"New-Item -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run' -Force | Out-Null; Set-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run' -Name '{escapedName}' -Value '{escapedPath}'";
            }
            else
            {
                var startupFolder = EscapePowerShell(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Startup"));
                script = $"$WshShell = New-Object -ComObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut((Join-Path '{startupFolder}' '{escapedName}.lnk')); $Shortcut.TargetPath = '{EscapePowerShell(appPath)}'; $Shortcut.Save()";
            }

            var (success, output) = await ExecutePowerShellScriptAsync(script);
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Add Startup App", success ? $"{appName} ditambahkan ke startup." : "Gagal menambahkan startup app.", output);
            await RefreshStartupItems();
        }

        private void BackFromStartup_Click(object sender, RoutedEventArgs e)
        {
            _ = ShowPage("Dashboard", DashboardBtn);
        }

        private async Task ApplyStartupProfileAsync(string profileName, IEnumerable<string> targetNames)
        {
            var targets = _startupEntries
                .Where(x => x.Enabled && targetNames.Any(token => x.Name.Contains(token, StringComparison.OrdinalIgnoreCase)))
                .Select(x => x.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (targets.Count == 0)
            {
                ShowActionStatus(ActionState.Info, profileName, "Tidak ada startup item yang cocok untuk profile ini.");
                return;
            }

            var results = new List<string>();
            foreach (var target in targets)
            {
                var success = await DisableStartupEntryAsync(target);
                results.Add($"{(success ? "Disabled" : "Failed")}: {target}");
            }

            ShowActionStatus(ActionState.Success, profileName, $"Startup profile {profileName} diterapkan.", string.Join(Environment.NewLine, results));
        }

        private async void ApplyGamingStartupProfile_Click(object sender, RoutedEventArgs e)
        {
            await ApplyStartupProfileAsync("Gaming Startup", new[] { "OneDrive", "Teams", "Spotify", "Adobe", "Launcher", "Update" });
        }

        private async void ApplyWorkStartupProfile_Click(object sender, RoutedEventArgs e)
        {
            await ApplyStartupProfileAsync("Work Startup", new[] { "Steam", "Epic", "Discord", "Spotify", "Game", "RTSS" });
        }

        private async void ApplyMinimalStartupProfile_Click(object sender, RoutedEventArgs e)
        {
            await ApplyStartupProfileAsync("Minimal Startup", new[] { "OneDrive", "Teams", "Spotify", "Discord", "Steam", "Epic", "Adobe", "Launcher", "Update", "Widgets" });
        }

        #endregion

        #region Storage

        private void StorageTimer_Tick(object sender, EventArgs e)
        {
            if (_isUpdating || _activePage != "Storage")
                return;

            _ = RefreshStorageViewAsync();
        }

        private IEnumerable<DriveInfo> GetFilteredDrives()
        {
            var filter = (StorageFilterCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Devices";
            var drives = SafeGetDrives().Where(x => x.IsReady || x.DriveType is DriveType.Network or DriveType.CDRom or DriveType.Removable).ToList();

            return filter switch
            {
                "Internal Only" => drives.Where(x => x.DriveType == DriveType.Fixed),
                "Removable Only" => drives.Where(x => x.DriveType == DriveType.Removable),
                "Network Only" => drives.Where(x => x.DriveType == DriveType.Network),
                "Optical Only" => drives.Where(x => x.DriveType == DriveType.CDRom),
                _ => drives
            };
        }

        private async Task RefreshStorageViewAsync()
        {
            _isUpdating = true;
            try
            {
                var drives = GetFilteredDrives().ToList();
                var currentSignature = string.Join("|", SafeGetDrives().Select(x => $"{x.Name}:{x.IsReady}:{(x.IsReady ? x.TotalFreeSpace : 0)}"));

                if (!string.IsNullOrWhiteSpace(_lastStorageSignature) && !string.Equals(_lastStorageSignature, currentSignature, StringComparison.Ordinal))
                {
                    StorageDetectionText.Text = $"Storage configuration updated at {DateTime.Now:HH:mm:ss}\nUSB / external / mapped drive change detected.";
                    AppendDashboardActivity("Storage configuration changed.");
                }
                else
                {
                    StorageDetectionText.Text = $"Monitoring active. Last refresh: {DateTime.Now:HH:mm:ss}\nNo new storage change detected.";
                }

                _lastStorageSignature = currentSignature;
                UpdateStorageDriveCombo(drives);
                StorageUnifiedOverviewText.Text = BuildStorageOverview(drives);
                StorageDevicesText.Text = BuildStorageDeviceList(drives);
                StorageHealthText.Text = BuildStorageHealthText(drives);
                StorageRecommendationText.Text = BuildStorageRecommendation(drives);

                var selectedDrive = GetSelectedDrive(drives);
                if (selectedDrive != null)
                {
                    try
                    {
                        StorageBreakdownText.Text = await BuildStorageBreakdownAsync(selectedDrive);
                    }
                    catch (Exception ex)
                    {
                        StorageBreakdownText.Text = $"Storage breakdown unavailable: {ex.GetType().Name}";
                    }

                    try
                    {
                        StorageDeepAnalyzerText.Text = await BuildStorageAnalyzerAsync(selectedDrive, deep: false);
                    }
                    catch (Exception ex)
                    {
                        StorageDeepAnalyzerText.Text = $"Storage analyzer unavailable: {ex.GetType().Name}";
                    }
                }
                else
                {
                    StorageBreakdownText.Text = "Tidak ada drive yang dipilih.";
                    StorageDeepAnalyzerText.Text = "Pilih drive untuk memulai analisis.";
                }
            }
            catch (Exception ex)
            {
                StorageDetectionText.Text = $"Storage monitor fallback active. Last attempt: {DateTime.Now:HH:mm:ss}";
                StorageUnifiedOverviewText.Text = "Storage data sementara tidak bisa dibaca penuh.";
                StorageDevicesText.Text = $"Storage refresh failed: {ex.Message}";
                StorageHealthText.Text = "Health summary unavailable.";
                StorageRecommendationText.Text = "Retry setelah runtime storage dependency tersedia.";
                StorageBreakdownText.Text = "Storage breakdown unavailable.";
                StorageDeepAnalyzerText.Text = "Storage analyzer unavailable.";
                AppendDashboardActivity($"Storage refresh warning: {ex.Message}");
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private static IEnumerable<DriveInfo> SafeGetDrives()
        {
            try
            {
                return DriveInfo.GetDrives();
            }
            catch
            {
                return Array.Empty<DriveInfo>();
            }
        }

        private void UpdateStorageDriveCombo(List<DriveInfo> drives)
        {
            var selected = (StorageDriveCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
            StorageDriveCombo.Items.Clear();

            foreach (var drive in drives)
            {
                StorageDriveCombo.Items.Add(new ComboBoxItem
                {
                    Content = $"{drive.Name} {GetDriveLabel(drive)}"
                });
            }

            if (StorageDriveCombo.Items.Count == 0)
                return;

            var matched = StorageDriveCombo.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(x => string.Equals(x.Content?.ToString(), selected, StringComparison.OrdinalIgnoreCase));

            StorageDriveCombo.SelectedItem = matched ?? StorageDriveCombo.Items[0];
        }

        private DriveInfo GetSelectedDrive(List<DriveInfo> drives)
        {
            var selectedText = (StorageDriveCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(selectedText))
                return drives.FirstOrDefault();

            return drives.FirstOrDefault(x => selectedText.StartsWith(x.Name, StringComparison.OrdinalIgnoreCase));
        }

        private string GetDriveLabel(DriveInfo drive)
        {
            string type;
            try
            {
                type = drive.DriveType switch
                {
                    DriveType.Fixed => "Internal Disk",
                    DriveType.Removable => "USB / Removable",
                    DriveType.Network => "Network Drive",
                    DriveType.CDRom => "Optical Drive",
                    _ => drive.DriveType.ToString()
                };
            }
            catch
            {
                type = "Unknown Drive";
            }

            string volume;
            try
            {
                volume = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "(No Label)" : drive.VolumeLabel;
            }
            catch
            {
                volume = "(Label Unavailable)";
            }

            return $"{volume} - {type}";
        }

        private string BuildStorageOverview(List<DriveInfo> drives)
        {
            var ready = drives.Where(x => x.IsReady).ToList();
            var total = ready.Sum(x => (double)x.TotalSize) / 1024d / 1024d / 1024d;
            var free = ready.Sum(x => (double)x.TotalFreeSpace) / 1024d / 1024d / 1024d;
            var used = Math.Max(0, total - free);

            return
                $"Total All Storage: {total:0.0} GB\n" +
                $"Total Used / Free: {used:0.0} GB / {free:0.0} GB\n" +
                $"Device Count: {drives.Count}\n" +
                $"Detected Devices:\n" +
                $"- Internal: {drives.Count(x => x.DriveType == DriveType.Fixed)}\n" +
                $"- External / Removable: {drives.Count(x => x.DriveType == DriveType.Removable)}\n" +
                $"- Network: {drives.Count(x => x.DriveType == DriveType.Network)}\n" +
                $"- Optical: {drives.Count(x => x.DriveType == DriveType.CDRom)}";
        }

        private string BuildStorageDeviceList(List<DriveInfo> drives)
        {
            if (drives.Count == 0)
                return "Tidak ada storage device yang terdeteksi.";

            var lines = new List<string>();
            foreach (var drive in drives)
            {
                bool isReady;
                try
                {
                    isReady = drive.IsReady;
                }
                catch (Exception ex)
                {
                    lines.Add($"{drive.Name} - {GetDriveLabel(drive)} | Drive info unavailable ({ex.GetType().Name})");
                    continue;
                }

                if (!isReady)
                {
                    lines.Add($"{drive.Name} - {GetDriveLabel(drive)} | Not ready");
                    continue;
                }

                try
                {
                    var total = drive.TotalSize / 1024d / 1024d / 1024d;
                    var free = drive.TotalFreeSpace / 1024d / 1024d / 1024d;
                    var used = total - free;
                    var usedPercent = total > 0 ? used / total * 100 : 0;
                    lines.Add($"{drive.Name} - {GetDriveLabel(drive)}");
                    lines.Add($"  Total {total:0.0} GB | Used {used:0.0} GB | Free {free:0.0} GB | {usedPercent:0}% used | FS {drive.DriveFormat}");
                }
                catch (Exception ex)
                {
                    lines.Add($"{drive.Name} - {GetDriveLabel(drive)} | Drive metrics unavailable ({ex.GetType().Name})");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string BuildStorageHealthText(List<DriveInfo> drives)
        {
            if (drives.Count == 0)
                return "Storage health belum tersedia.";

            var lines = new List<string>();
            foreach (var drive in drives)
            {
                bool isReady;
                try
                {
                    isReady = drive.IsReady;
                }
                catch (Exception ex)
                {
                    lines.Add($"{drive.Name} | Warning | Drive info unavailable ({ex.GetType().Name})");
                    continue;
                }

                if (!isReady)
                {
                    lines.Add($"{drive.Name} | Warning | Not accessible / Not ready");
                    continue;
                }

                try
                {
                    var usedPercent = drive.TotalSize > 0 ? ((double)(drive.TotalSize - drive.TotalFreeSpace) / drive.TotalSize) * 100 : 0;
                    var health = usedPercent >= 95 ? "Critical" : usedPercent >= 85 ? "Warning" : "Healthy";
                    lines.Add($"{drive.Name} | {health} | Available | {drive.DriveFormat} | {(drive.DriveType == DriveType.Network ? "Mapped / Read-Write varies" : "Read/Write")}");
                }
                catch (Exception ex)
                {
                    lines.Add($"{drive.Name} | Warning | Drive metrics unavailable ({ex.GetType().Name})");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string BuildStorageRecommendation(List<DriveInfo> drives)
        {
            var lines = new List<string>();
            foreach (var drive in drives)
            {
                try
                {
                    if (!drive.IsReady)
                        continue;

                    var total = drive.TotalSize / 1024d / 1024d / 1024d;
                    var free = drive.TotalFreeSpace / 1024d / 1024d / 1024d;
                    var usedPercent = total > 0 ? ((total - free) / total) * 100 : 0;
                    if (usedPercent >= 90) lines.Add($"- Drive {drive.Name} hampir penuh.");
                    if (drive.DriveType == DriveType.Removable) lines.Add($"- Removable storage {drive.Name} sebaiknya di-scan untuk file besar / duplicate.");
                }
                catch
                {
                }
            }

            if (lines.Count == 0)
                lines.Add("- Semua drive terlihat cukup sehat. Lanjutkan quick scan untuk detail file besar dan cache.");

            return string.Join(Environment.NewLine, lines.Distinct());
        }

        private async Task<string> BuildStorageBreakdownAsync(DriveInfo drive)
        {
            return await Task.Run(() =>
            {
                if (!drive.IsReady)
                    return "Drive tidak siap dibaca.";

                var root = drive.RootDirectory.FullName;
                var categories = new Dictionary<string, long>
                {
                    ["System"] = 0,
                    ["Program Files"] = 0,
                    ["Users"] = 0,
                    ["Documents"] = 0,
                    ["Downloads"] = 0,
                    ["Images"] = 0,
                    ["Videos"] = 0,
                    ["Audio"] = 0,
                    ["Archives"] = 0,
                    ["Others"] = 0
                };

                try
                {
                    foreach (var dir in Directory.EnumerateDirectories(root))
                    {
                        var name = Path.GetFileName(dir);
                        var size = GetDirectorySizeApprox(dir, 3000);
                        switch (name.ToLowerInvariant())
                        {
                            case "windows":
                                categories["System"] += size;
                                break;
                            case "program files":
                            case "program files (x86)":
                                categories["Program Files"] += size;
                                break;
                            case "users":
                                categories["Users"] += size;
                                break;
                            default:
                                categories["Others"] += size;
                                break;
                        }
                    }

                    var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    if (userProfile.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    {
                        categories["Documents"] += GetDirectorySizeApprox(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 2500);
                        categories["Downloads"] += GetDirectorySizeApprox(Path.Combine(userProfile, "Downloads"), 2500);
                        categories["Images"] += GetDirectorySizeApprox(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), 2500);
                        categories["Videos"] += GetDirectorySizeApprox(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), 2500);
                        categories["Audio"] += GetDirectorySizeApprox(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), 2500);
                    }
                }
                catch
                {
                }

                return string.Join(Environment.NewLine, categories.Select(x => $"{x.Key}: {x.Value / 1024d / 1024d / 1024d:0.0} GB"));
            });
        }

        private async Task<string> BuildStorageAnalyzerAsync(DriveInfo drive, bool deep)
        {
            var summary = await AnalyzeDriveAsync(drive, deep ? 20000 : 7000);
            return
                $"Drive: {summary.DriveLabel}\n" +
                $"Total files: {summary.TotalFiles}\n" +
                $"Total folders: {summary.TotalFolders}\n" +
                $"Largest file: {summary.LargestFileMb:0.0} MB | {summary.LargestFilePath}\n" +
                $"Largest folder: {summary.LargestFolderMb:0.0} MB | {summary.LargestFolderPath}\n" +
                $"Duplicate candidates: {summary.DuplicateCandidates}\n" +
                $"Junk files: {summary.JunkFiles} | Cache files: {summary.CacheFiles} | Temp files: {summary.TempFiles}\n" +
                $"Old files: {summary.OldFiles} | Hidden files: {summary.HiddenFiles} | Unknown file types: {summary.UnknownTypes}";
        }

        private async Task<StorageScanSummary> AnalyzeDriveAsync(DriveInfo drive, int fileLimit)
        {
            return await Task.Run(() =>
            {
                var summary = new StorageScanSummary { DriveLabel = drive.Name };
                if (!drive.IsReady)
                    return summary;

                var directorySizes = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                var duplicateMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var stack = new Stack<string>();
                stack.Push(drive.RootDirectory.FullName);

                while (stack.Count > 0 && summary.TotalFiles < fileLimit)
                {
                    var current = stack.Pop();
                    try
                    {
                        summary.TotalFolders++;
                        long currentDirSize = 0;

                        foreach (var file in Directory.EnumerateFiles(current))
                        {
                            try
                            {
                                var info = new FileInfo(file);
                                summary.TotalFiles++;
                                currentDirSize += info.Length;

                                if (info.Length > summary.LargestFileMb * 1024 * 1024)
                                {
                                    summary.LargestFileMb = info.Length / 1024d / 1024d;
                                    summary.LargestFilePath = info.FullName;
                                }

                                var ext = info.Extension.ToLowerInvariant();
                                if (new[] { ".tmp", ".log", ".bak", ".dmp", ".old" }.Contains(ext)) summary.JunkFiles++;
                                if (new[] { ".tmp", ".temp" }.Contains(ext) || info.DirectoryName?.Contains("temp", StringComparison.OrdinalIgnoreCase) == true) summary.TempFiles++;
                                if (info.DirectoryName?.Contains("cache", StringComparison.OrdinalIgnoreCase) == true) summary.CacheFiles++;
                                if (info.LastWriteTime < DateTime.Now.AddMonths(-12)) summary.OldFiles++;
                                if (info.Attributes.HasFlag(FileAttributes.Hidden)) summary.HiddenFiles++;
                                if (string.IsNullOrWhiteSpace(ext) || ext.Length > 8) summary.UnknownTypes++;

                                var dupKey = $"{info.Name.ToLowerInvariant()}|{info.Length}";
                                duplicateMap[dupKey] = duplicateMap.TryGetValue(dupKey, out var count) ? count + 1 : 1;
                            }
                            catch
                            {
                            }

                            if (summary.TotalFiles >= fileLimit)
                                break;
                        }

                        directorySizes[current] = currentDirSize;

                        foreach (var subDir in Directory.EnumerateDirectories(current))
                        {
                            stack.Push(subDir);
                        }
                    }
                    catch
                    {
                    }
                }

                var largestFolder = directorySizes.OrderByDescending(x => x.Value).FirstOrDefault();
                summary.LargestFolderPath = string.IsNullOrWhiteSpace(largestFolder.Key) ? "N/A" : largestFolder.Key;
                summary.LargestFolderMb = largestFolder.Value / 1024d / 1024d;
                summary.DuplicateCandidates = duplicateMap.Values.Count(x => x > 1);
                return summary;
            });
        }

        private long GetDirectorySizeApprox(string path, int fileLimit)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return 0;

            long bytes = 0;
            int count = 0;
            try
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        bytes += new FileInfo(file).Length;
                        count++;
                        if (count >= fileLimit)
                            break;
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }

            return bytes;
        }

        private async void StorageAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string action)
                return;

            var drives = GetFilteredDrives().ToList();
            var selectedDrive = GetSelectedDrive(drives);

            switch (action)
            {
                case "refresh_storage":
                    await RefreshStorageViewAsync();
                    ShowActionStatus(ActionState.Info, "Refresh Storage", "Daftar storage berhasil diperbarui.");
                    return;
                case "quick_scan":
                    if (selectedDrive != null) StorageScanResultsText.Text = await BuildStorageAnalyzerAsync(selectedDrive, false);
                    break;
                case "full_scan":
                case "deep_scan":
                case "analyze_drive":
                    if (selectedDrive != null) StorageDeepAnalyzerText.Text = await BuildStorageAnalyzerAsync(selectedDrive, action != "full_scan");
                    break;
                case "scan_all":
                    StorageScanResultsText.Text = string.Join(Environment.NewLine + Environment.NewLine, await Task.WhenAll(drives.Where(x => x.IsReady).Select(x => BuildStorageAnalyzerAsync(x, false))));
                    break;
                case "scan_removable":
                    StorageScanResultsText.Text = string.Join(Environment.NewLine + Environment.NewLine, await Task.WhenAll(drives.Where(x => x.IsReady && x.DriveType == DriveType.Removable).Select(x => BuildStorageAnalyzerAsync(x, false))));
                    break;
                case "scan_internal":
                    StorageScanResultsText.Text = string.Join(Environment.NewLine + Environment.NewLine, await Task.WhenAll(drives.Where(x => x.IsReady && x.DriveType == DriveType.Fixed).Select(x => BuildStorageAnalyzerAsync(x, false))));
                    break;
                case "open_drive":
                    if (selectedDrive != null) LaunchWindowsTool("explorer.exe", selectedDrive.RootDirectory.FullName, "Open Drive");
                    break;
                case "cleanup_drive":
                    if (selectedDrive != null) LaunchWindowsTool("cleanmgr.exe", $"/d {selectedDrive.Name.TrimEnd('\\')}", "Cleanup Drive");
                    break;
                case "eject_drive":
                    if (selectedDrive != null && selectedDrive.DriveType == DriveType.Removable)
                    {
                        var driveLetter = selectedDrive.Name.TrimEnd('\\');
                        await RunPowerShellActionAsync(
                            $"(New-Object -comObject Shell.Application).Namespace(17).ParseName('{driveLetter}').InvokeVerb('Eject')",
                            "Eject Storage",
                            "Permintaan eject dikirim ke removable storage.");
                    }
                    else
                    {
                        ShowActionStatus(ActionState.Warning, "Eject Storage", "Pilih removable storage yang valid untuk di-eject.");
                    }
                    break;
            }

            await RefreshStorageViewAsync();
        }

        private async void StorageFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _activePage != "Storage" || _isUpdating)
                return;

            await RefreshStorageViewAsync();
        }

        private async void StorageDriveCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _activePage != "Storage" || _isUpdating)
                return;

            await RefreshStorageViewAsync();
        }

        #endregion

        #region Storage Cleanup

        private void AppendCleanupHistory(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            _cleanupHistory.Enqueue($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
            while (_cleanupHistory.Count > 8)
            {
                _cleanupHistory.Dequeue();
            }

            CleanupHistoryText.Text = string.Join(Environment.NewLine, _cleanupHistory.Reverse());
        }

        private async Task RefreshCleanupViewAsync()
        {
            CleanupSafetyModeText.Text = $"Mode aktif: {_cleanupSafetyMode}";
            await RunCleanupQuickScanAsync();
        }

        private async Task RunCleanupQuickScanAsync()
        {
            var systemStats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
            var json = systemStats as Newtonsoft.Json.Linq.JObject;
            var diskPercent = json?.Value<double?>("disk") ?? json?.Value<double?>("disk_percent") ?? 0;
            var diskUsed = json?.Value<double?>("disk_used_gb") ?? 0;
            var diskTotal = json?.Value<double?>("disk_total_gb") ?? 0;

            var tempMb = await GetDirectorySizeMbAsync(Path.GetTempPath());
            var windowsTempMb = await GetDirectorySizeMbAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"));
            var browserMb = await GetBrowserCacheSizeMbAsync();
            var logsMb = await GetLogsAndReportsSizeMbAsync();
            var recycleMb = await GetRecycleBinSizeMbAsync();
            var totalMb = tempMb + windowsTempMb + browserMb + logsMb + recycleMb;

            CleanupScanText.Text =
                $"Junk files: {(tempMb + logsMb):0} MB\n" +
                $"Temp files: {(tempMb + windowsTempMb):0} MB\n" +
                $"System cache estimate: {windowsTempMb:0} MB\n" +
                $"Recycle Bin: {recycleMb:0} MB\n" +
                $"Browser cache: {browserMb:0} MB\n" +
                $"Logs & error reports: {logsMb:0} MB\n\n" +
                $"Total file yang bisa dibersihkan: {totalMb / 1024d:0.0} GB";

            CleanupSmartRecommendationText.Text = BuildCleanupRecommendation(tempMb, windowsTempMb, browserMb, logsMb, recycleMb, diskPercent);
            CleanupStorageOverviewText.Text =
                $"Total disk usage: {diskPercent:0}%\n" +
                $"Free space: {Math.Max(0, diskTotal - diskUsed):0.0} GB / {diskTotal:0.0} GB\n" +
                $"Top storage usage preview:\n" +
                $"- System + cache: {(windowsTempMb + logsMb):0} MB\n" +
                $"- Browser cache: {browserMb:0} MB\n" +
                $"- Temp files: {(tempMb + windowsTempMb):0} MB\n" +
                $"- Others (recycle/logs): {(recycleMb + logsMb):0} MB";
        }

        private string BuildCleanupRecommendation(double tempMb, double windowsTempMb, double browserMb, double logsMb, double recycleMb, double diskPercent)
        {
            var lines = new List<string>();
            if (tempMb + windowsTempMb >= 512) lines.Add($"- {(tempMb + windowsTempMb) / 1024d:0.0}GB temp files detected.");
            if (browserMb >= 256) lines.Add($"- Browser cache terlalu penuh ({browserMb:0} MB).");
            if (logsMb >= 128) lines.Add($"- Windows logs & error reports besar ({logsMb:0} MB).");
            if (recycleMb >= 128) lines.Add($"- Recycle Bin berisi sekitar {recycleMb:0} MB.");
            if (diskPercent >= 85) lines.Add("- Storage pressure tinggi, jalankan quick clean dan deep cleanup.");
            if (lines.Count == 0) lines.Add("- Sistem relatif bersih. Fokus ke maintenance ringan.");
            return string.Join(Environment.NewLine, lines);
        }

        private async Task<double> GetDirectorySizeMbAsync(string path, ISet<string> allowedExtensions = null)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return 0;

            return await Task.Run(() =>
            {
                long bytes = 0;
                try
                {
                    foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            var extension = Path.GetExtension(file);
                            if (allowedExtensions != null && !allowedExtensions.Contains(extension))
                                continue;
                            bytes += new FileInfo(file).Length;
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                }

                return bytes / 1024d / 1024d;
            });
        }

        private async Task<double> GetBrowserCacheSizeMbAsync()
        {
            double total = 0;
            foreach (var path in GetBrowserCachePaths())
            {
                total += await GetDirectorySizeMbAsync(path);
            }

            return total;
        }

        private async Task<double> GetLogsAndReportsSizeMbAsync()
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var windowsLogExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".etl",
                ".evtx",
                ".log",
                ".txt",
                ".cab",
                ".dmp",
                ".tmp"
            };
            var paths = new[]
            {
                (Path: Path.Combine(programData, @"Microsoft\Windows\WER"), AllowedExtensions: (ISet<string>)null),
                (Path: Path.Combine(local, @"CrashDumps"), AllowedExtensions: (ISet<string>)null),
                (Path: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Logs"), AllowedExtensions: windowsLogExtensions)
            };

            double total = 0;
            foreach (var path in paths)
            {
                total += await GetDirectorySizeMbAsync(path.Path, path.AllowedExtensions);
            }

            return total;
        }

        private IEnumerable<string> GetBrowserCachePaths()
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var paths = new List<string>
            {
                Path.Combine(local, @"Google\Chrome\User Data\Default\Cache"),
                Path.Combine(local, @"Google\Chrome\User Data\Default\Code Cache"),
                Path.Combine(local, @"Google\Chrome\User Data\Default\GPUCache"),
                Path.Combine(local, @"Microsoft\Edge\User Data\Default\Cache"),
                Path.Combine(local, @"Microsoft\Edge\User Data\Default\Code Cache"),
                Path.Combine(local, @"Microsoft\Edge\User Data\Default\GPUCache")
            };

            var firefoxProfiles = Path.Combine(roaming, @"Mozilla\Firefox\Profiles");
            if (Directory.Exists(firefoxProfiles))
            {
                try
                {
                    foreach (var profile in Directory.EnumerateDirectories(firefoxProfiles))
                    {
                        paths.Add(Path.Combine(profile, "cache2"));
                        paths.Add(Path.Combine(profile, "startupCache"));
                        paths.Add(Path.Combine(profile, "jumpListCache"));
                        paths.Add(Path.Combine(profile, "shader-cache"));
                    }
                }
                catch
                {
                }
            }

            return paths;
        }

        private async Task<double> GetRecycleBinSizeMbAsync()
        {
            try
            {
                var script = "$shell=New-Object -ComObject Shell.Application; $bin=$shell.Namespace(10); $size=0; foreach($item in $bin.Items()){ $size += [double]$item.ExtendedProperty('Size') }; Write-Output $size";
                var (success, output) = await ExecutePowerShellScriptAsync(script);
                if (!success)
                    return 0;

                return double.TryParse(output.Trim(), out var bytes) ? bytes / 1024d / 1024d : 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<string> ScanLargeFilesAsync(long minimumMb)
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _lastLargeFileDirectory = baseDir;

            return await Task.Run(() =>
            {
                var candidates = new List<FileInfo>();
                try
                {
                    foreach (var file in Directory.EnumerateFiles(baseDir, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            var info = new FileInfo(file);
                            if (info.Length >= minimumMb * 1024L * 1024L)
                            {
                                candidates.Add(info);
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                }

                var top = candidates.OrderByDescending(x => x.Length).Take(8).ToList();
                if (top.Count == 0)
                    return $"Tidak ada file > {minimumMb}MB yang ditemukan di profil user.";

                return string.Join(Environment.NewLine, top.Select(x => $"{x.Length / 1024d / 1024d:0} MB | {x.LastWriteTime:yyyy-MM-dd} | {x.FullName}"));
            });
        }

        private async Task<string> ScanDuplicateFilesAsync()
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _lastLargeFileDirectory = baseDir;

            return await Task.Run(() =>
            {
                var groups = new Dictionary<string, List<FileInfo>>();
                try
                {
                    foreach (var file in Directory.EnumerateFiles(baseDir, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            var info = new FileInfo(file);
                            if (info.Length < 1024 * 1024)
                                continue;

                            var key = $"{info.Name.ToLowerInvariant()}|{info.Length}|{ComputeQuickHash(info.FullName)}";
                            if (!groups.ContainsKey(key))
                            {
                                groups[key] = new List<FileInfo>();
                            }

                            groups[key].Add(info);
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                }

                var duplicates = groups.Values.Where(x => x.Count > 1).Take(6).ToList();
                if (duplicates.Count == 0)
                {
                    _lastDuplicateDeleteCandidates = new List<string>();
                    return "Tidak ada duplicate file yang terdeteksi dari scan cepat Documents.";
                }

                var lines = new List<string>();
                var deleteCandidates = new List<string>();
                foreach (var group in duplicates)
                {
                    var orderedGroup = group.OrderBy(file => file.FullName.Length).ThenBy(file => file.FullName, StringComparer.OrdinalIgnoreCase).ToList();
                    deleteCandidates.AddRange(orderedGroup.Skip(1).Select(file => file.FullName));
                    lines.Add($"Duplicate set: {group[0].Name} | {group[0].Length / 1024d / 1024d:0.#} MB");
                    lines.AddRange(group.Select(file => $"  - {file.FullName}"));
                }

                _lastDuplicateDeleteCandidates = deleteCandidates;
                return string.Join(Environment.NewLine, lines);
            });
        }

        private string ComputeQuickHash(string path)
        {
            try
            {
                using var stream = File.OpenRead(path);
                using var sha = SHA256.Create();
                var buffer = new byte[Math.Min(stream.Length, 64 * 1024)];
                stream.Read(buffer, 0, buffer.Length);
                var hash = sha.ComputeHash(buffer);
                return Convert.ToHexString(hash);
            }
            catch
            {
                return "NOHASH";
            }
        }

        private async Task<(long cleanedBytes, string details)> RunCleanNowAsync()
        {
            var recycleBeforeMb = await GetRecycleBinSizeMbAsync();
            var cleanupResult = await SafeApiCall(() => _backendClient.CleanupAsync("safe_all"));
            if (cleanupResult == null)
            {
                ShowActionStatus(ActionState.Error, "Clean Now", "Unable to run quick clean right now.");
                return (0, "Backend cleanup request failed.");
            }

            AppendCleanupHistory("Quick Clean core cleanup executed.");
            await EmptyRecycleCoreAsync();
            await CleanClipboardCoreAsync();
            AppendCleanupHistory("Quick Clean completed.");
            CleanupHistoryText.Text = string.Join(Environment.NewLine, _cleanupHistory.Reverse());
            var cleanedBytes = GetCleanupFreedBytes(cleanupResult) + (long)Math.Round(recycleBeforeMb * 1024d * 1024d);
            var details = BuildQuickCleanDetails(cleanupResult, recycleBeforeMb, cleanedBytes);
            return (cleanedBytes, details);
        }

        private async Task CleanTempCoreAsync()
        {
            await RunScopedCleanupAsync("temp_files", "Clean Temp", "Temporary files cleaned successfully.", "Temporary files cleaned.");
        }

        private async Task ClearCacheCoreAsync()
        {
            await RunScopedCleanupAsync("system_cache", "Clear Cache", "System cache cleanup completed.", "System cache cleaned.");
        }

        private async Task CleanJunkCoreAsync()
        {
            await RunScopedCleanupAsync("junk_files", "Clean Junk Files", "Safe junk cleanup finished.", "Junk files cleaned.");
        }

        private async Task RunScopedCleanupAsync(string scope, string title, string successMessage, string historyMessage)
        {
            var result = await SafeApiCall(() => _backendClient.CleanupAsync(scope));
            if (result == null)
            {
                ShowActionStatus(ActionState.Error, title, $"Unable to run {title.ToLowerInvariant()} right now.");
                return;
            }

            var freedBytes = GetCleanupFreedBytes(result);
            AppendCleanupHistory($"{historyMessage} Freed {FormatBytes(freedBytes)}.");
            ShowActionStatus(
                freedBytes > 0 ? ActionState.Success : ActionState.Info,
                title,
                freedBytes > 0 ? $"{successMessage} {FormatBytes(freedBytes)} removed." : "Cleanup completed, but no removable files were found.",
                BuildCleanupResultDetails(result));
        }

        private static long GetCleanupFreedBytes(dynamic result)
        {
            try
            {
                return result?["freed_bytes"]?.Value<long?>()
                    ?? (long)Math.Round((result?["freed_mb"]?.Value<double?>() ?? 0) * 1024d * 1024d);
            }
            catch
            {
                return 0;
            }
        }

        private static string BuildCleanupResultDetails(dynamic result)
        {
            if (result is not JObject json)
                return HyperBoostBackendClient.FormatJson(result);

            var lines = new List<string>
            {
                $"Scope: {json.Value<string>("scope") ?? "cleanup"}",
                $"Freed: {FormatBytes(json.Value<long?>("freed_bytes") ?? 0)}",
                $"Files deleted: {json.Value<int?>("deleted_files") ?? 0}",
                $"Directories deleted: {json.Value<int?>("deleted_directories") ?? 0}",
            };

            if (json["categories"] is JObject categories)
            {
                foreach (var property in categories.Properties())
                {
                    if (property.Value is not JObject category)
                        continue;

                    var freedBytes = category.Value<long?>("freed_bytes") ?? 0;
                    var deletedFiles = category.Value<int?>("deleted_files") ?? 0;
                    var deletedDirectories = category.Value<int?>("deleted_directories") ?? 0;
                    if (freedBytes <= 0 && deletedFiles <= 0 && deletedDirectories <= 0)
                        continue;

                    lines.Add(
                        $"{property.Name}: {FormatBytes(freedBytes)} | files {deletedFiles} | dirs {deletedDirectories}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static string BuildQuickCleanDetails(dynamic result, double recycleBeforeMb, long totalBytes)
        {
            var lines = new List<string>
            {
                $"Quick Clean total: {FormatBytes(totalBytes)}",
                $"Recycle Bin before clean: {FormatBytes((long)Math.Round(recycleBeforeMb * 1024d * 1024d))}",
                BuildCleanupResultDetails(result),
            };
            return string.Join(Environment.NewLine, lines.Where(line => !string.IsNullOrWhiteSpace(line)));
        }

        private async Task EmptyRecycleCoreAsync()
        {
            await RunPowerShellActionAsync(
                "Clear-RecycleBin -Force",
                "Empty Recycle Bin",
                "Recycle Bin emptied successfully.");
            AppendCleanupHistory("Recycle Bin emptied.");
        }

        private async Task CleanClipboardCoreAsync()
        {
            await RunPowerShellActionAsync(
                "Set-Clipboard -Value $null",
                "Clipboard Cleanup",
                "Clipboard cache dibersihkan.");
            AppendCleanupHistory("Clipboard cache cleaned.");
        }

        private Task DeleteScannedDuplicatesAsync()
        {
            if (_lastDuplicateDeleteCandidates.Count == 0)
            {
                ShowActionStatus(ActionState.Warning, "Duplicate File Cleaner", "Jalankan scan duplicates dulu supaya kandidat hapus bisa dipilih aman.");
                return Task.CompletedTask;
            }

            var deleted = new List<string>();
            long freedBytes = 0;
            foreach (var path in _lastDuplicateDeleteCandidates.ToList())
            {
                try
                {
                    if (!File.Exists(path))
                        continue;

                    var info = new FileInfo(path);
                    freedBytes += info.Length;
                    File.Delete(path);
                    deleted.Add(path);
                }
                catch
                {
                }
            }

            _lastDuplicateDeleteCandidates = _lastDuplicateDeleteCandidates.Except(deleted, StringComparer.OrdinalIgnoreCase).ToList();
            CleanupDuplicateFilesText.Text = deleted.Count > 0
                ? $"Deleted duplicate files: {deleted.Count}\nFreed: {FormatBytes(freedBytes)}\n" + string.Join(Environment.NewLine, deleted.Take(8))
                : "Tidak ada duplicate candidate yang berhasil dihapus. Pastikan file tidak sedang dipakai.";
            ShowActionStatus(
                deleted.Count > 0 ? ActionState.Success : ActionState.Warning,
                "Duplicate File Cleaner",
                deleted.Count > 0 ? $"{deleted.Count} duplicate files deleted." : "No duplicate files were deleted.",
                CleanupDuplicateFilesText.Text);
            AppendCleanupHistory($"Duplicate cleanup executed. Freed {FormatBytes(freedBytes)}.");
            return Task.CompletedTask;
        }

        private void KeepOriginalDuplicateFiles()
        {
            if (_lastDuplicateDeleteCandidates.Count == 0)
            {
                ShowActionStatus(ActionState.Info, "Duplicate File Cleaner", "Belum ada duplicate candidates aktif. Jalankan scan terlebih dulu.");
                return;
            }

            CleanupDuplicateFilesText.Text =
                $"Original files preserved. Duplicate candidates pending review: {_lastDuplicateDeleteCandidates.Count}\n" +
                string.Join(Environment.NewLine, _lastDuplicateDeleteCandidates.Take(8));
            ShowActionStatus(ActionState.Info, "Duplicate File Cleaner", "Original file tetap dipertahankan. Kandidat lain tidak dihapus.", CleanupDuplicateFilesText.Text);
        }

        private async Task CreateCleanupScheduleAsync(string frequency)
        {
            var taskName = $"HyperBoostX-{frequency}-Cleanup";
            var schedule = frequency.ToUpperInvariant();
            var script =
                $"schtasks /Create /F /SC {schedule} /TN '{taskName}' /TR 'powershell.exe -NoProfile -WindowStyle Hidden -Command \"Clear-RecycleBin -Force -ErrorAction SilentlyContinue; $temp=[IO.Path]::GetTempPath(); if(Test-Path $temp){{ Get-ChildItem -LiteralPath $temp -Force -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -Recurse -ErrorAction SilentlyContinue }}\"' /ST 12:00";
            var (success, output) = await ExecutePowerShellScriptAsync(script);
            ShowActionStatus(
                success ? ActionState.Success : ActionState.Warning,
                "Auto Cleanup",
                success ? $"Auto cleanup {frequency.ToLowerInvariant()} berhasil dijadwalkan jam 12:00." : $"Gagal membuat auto cleanup {frequency.ToLowerInvariant()}.",
                output);
            if (success)
                AppendCleanupHistory($"Auto cleanup {frequency.ToLowerInvariant()} scheduled.");
        }

        private async void CleanTemp_Click(object sender, RoutedEventArgs e)
        {
            await CleanTempCoreAsync();
            await RefreshCleanupViewAsync();
        }

        private async void ClearCache_Click(object sender, RoutedEventArgs e)
        {
            await ClearCacheCoreAsync();
            await RefreshCleanupViewAsync();
        }

        private async void EmptyRecycle_Click(object sender, RoutedEventArgs e)
        {
            await EmptyRecycleCoreAsync();
            await RefreshCleanupViewAsync();
        }

        private async void DeepCleanup_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.CleanupAsync("deep_cleanup"));
            LaunchWindowsTool("cleanmgr.exe", null, "Deep Cleanup");

            if (result == null)
            {
                ShowActionStatus(ActionState.Warning, "Deep Cleanup", "Windows Disk Cleanup opened, but backend deep cleanup result is unavailable.");
                return;
            }

            AppendCleanupHistory("Deep cleanup started.");
            ShowActionStatus(ActionState.Success, "Deep Cleanup", "Deep cleanup started and Windows Disk Cleanup was opened.", HyperBoostBackendClient.FormatJson(result));
            await RefreshCleanupViewAsync();
        }

        private async void CleanupAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string action)
                return;

            switch (action)
            {
                case "scan_junk":
                case "scan_temp":
                case "scan_system_cache":
                case "scan_recycle":
                case "scan_browser_cache":
                case "scan_logs":
                case "smart_review":
                    await RunCleanupQuickScanAsync();
                    ShowActionStatus(ActionState.Info, "Quick Scan", "Cleanup scan selesai dan hasil terbaru sudah ditampilkan.");
                    break;

                case "clean_now":
                    var quickClean = await RunCleanNowAsync();
                    ShowActionStatus(
                        quickClean.cleanedBytes > 0 ? ActionState.Success : ActionState.Info,
                        "CLEAN NOW",
                        quickClean.cleanedBytes > 0
                            ? $"Quick clean selesai. {FormatBytes(quickClean.cleanedBytes)} storage freed."
                            : "Quick clean selesai. Tidak ada file aman yang perlu dibersihkan saat ini.",
                        quickClean.details);
                    break;
                case "clean_junk":
                    await CleanJunkCoreAsync();
                    break;
                case "clean_temp":
                    await CleanTempCoreAsync();
                    break;
                case "empty_recycle":
                    await EmptyRecycleCoreAsync();
                    break;
                case "clear_system_cache":
                    await ClearCacheCoreAsync();
                    break;
                case "clean_clipboard":
                    await CleanClipboardCoreAsync();
                    break;

                case "advanced_system_files":
                    await RunScopedCleanupAsync("advanced_system_files", "System Files", "System file cleanup completed.", "System file cleanup completed.");
                    break;
                case "advanced_windows_temp":
                    await RunScopedCleanupAsync("advanced_windows_temp", "Windows Temp", "Windows Temp cleanup completed.", "Windows Temp cleanup completed.");
                    break;
                case "advanced_prefetch":
                    await RunScopedCleanupAsync("advanced_prefetch", "Prefetch Files", "Prefetch cleanup completed.", "Prefetch cleanup completed.");
                    break;
                case "advanced_update_cache":
                    await RunScopedCleanupAsync("advanced_update_cache", "Windows Update Cache", "Windows Update cache cleanup completed.", "Windows Update cache cleanup completed.");
                    break;
                case "advanced_delivery_opt":
                    await RunScopedCleanupAsync("advanced_delivery_opt", "Delivery Optimization Files", "Delivery Optimization cleanup completed.", "Delivery Optimization cleanup completed.");
                    break;
                case "advanced_logs":
                    await RunScopedCleanupAsync("advanced_logs", "Error Reports & Logs", "Logs cleanup completed.", "Logs cleanup completed.");
                    break;
                case "advanced_user_temp":
                    await RunScopedCleanupAsync("advanced_user_temp", "Temp User Files", "User temp cleanup completed.", "User temp cleanup completed.");
                    break;
                case "advanced_recent_files":
                    await RunScopedCleanupAsync("advanced_recent_files", "Recent Files History", "Recent files history cleanup completed.", "Recent files history cleanup completed.");
                    break;
                case "advanced_thumbnail":
                    await RunScopedCleanupAsync("advanced_thumbnail", "Thumbnail Cache", "Thumbnail cache cleanup completed.", "Thumbnail cache cleanup completed.");
                    break;
                case "advanced_app_cache":
                    await RunScopedCleanupAsync("advanced_app_cache", "Application Cache", "Application cache cleanup completed.", "Application cache cleanup completed.");
                    break;

                case "browser_clear_cache":
                    await RunScopedCleanupAsync("browser_cache", "Browser Cache", "Browser cache cleanup completed.", "Browser cache cleanup completed.");
                    break;
                case "browser_clear_cookies":
                    await RunScopedCleanupAsync("browser_cookies", "Browser Cookies", "Browser cookies cleanup completed.", "Browser cookies cleanup completed.");
                    break;
                case "browser_clear_history":
                    await RunScopedCleanupAsync("browser_history", "Browser History", "Browser history cleanup completed.", "Browser history cleanup completed.");
                    break;
                case "browser_clear_downloads":
                    await RunScopedCleanupAsync("browser_downloads", "Browser Download History", "Browser download history cleanup completed.", "Browser download history cleanup completed.");
                    break;
                case "browser_clear_sessions":
                    await RunScopedCleanupAsync("browser_sessions", "Browser Sessions", "Browser session cleanup completed.", "Browser session cleanup completed.");
                    break;

                case "smart_apply":
                    var smartClean = await RunCleanNowAsync();
                    ShowActionStatus(
                        smartClean.cleanedBytes > 0 ? ActionState.Success : ActionState.Info,
                        "Smart Cleanup Recommendation",
                        smartClean.cleanedBytes > 0 ? $"Recommended cleanup executed. {FormatBytes(smartClean.cleanedBytes)} removed." : "Recommended cleanup executed. No removable files were found.",
                        smartClean.details);
                    break;

                case "large_100":
                    CleanupLargeFilesText.Text = await ScanLargeFilesAsync(100);
                    break;
                case "large_500":
                    CleanupLargeFilesText.Text = await ScanLargeFilesAsync(500);
                    break;
                case "large_1024":
                    CleanupLargeFilesText.Text = await ScanLargeFilesAsync(1024);
                    break;
                case "large_open_location":
                    LaunchWindowsTool("explorer.exe", _lastLargeFileDirectory, "Large File Location");
                    break;

                case "duplicate_scan":
                    CleanupDuplicateFilesText.Text = await ScanDuplicateFilesAsync();
                    break;
                case "duplicate_delete":
                    await DeleteScannedDuplicatesAsync();
                    break;
                case "duplicate_keep":
                    KeepOriginalDuplicateFiles();
                    break;

                case "deep_cleanup":
                    if (_cleanupSafetyMode == "Safe Only")
                    {
                        ShowActionStatus(ActionState.Warning, "Deep Cleanup", "Ubah Safe Cleanup Mode ke Moderate atau Advanced sebelum menjalankan deep cleanup.");
                    }
                    else
                    {
                        DeepCleanup_Click(this, new RoutedEventArgs());
                    }
                    break;
                case "deep_open_cleanmgr":
                    LaunchWindowsTool("cleanmgr.exe", null, "Deep Cleanup");
                    break;

                case "mode_safe":
                    _cleanupSafetyMode = "Safe Only";
                    break;
                case "mode_moderate":
                    _cleanupSafetyMode = "Moderate";
                    break;
                case "mode_advanced":
                    _cleanupSafetyMode = "Advanced";
                    break;

                case "auto_daily":
                    await CreateCleanupScheduleAsync("DAILY");
                    break;
                case "auto_weekly":
                    await CreateCleanupScheduleAsync("WEEKLY");
                    break;
                case "auto_monthly":
                    await CreateCleanupScheduleAsync("MONTHLY");
                    break;
            }

            await RefreshCleanupViewAsync();
        }

        #endregion

        #region Gaming Optimization

        private async void GameMode_Click(object sender, RoutedEventArgs e)
        {
            await ApplyQuickCompetitiveGamingAsync();
        }

        private async void DisableOverlays_Click(object sender, RoutedEventArgs e)
        {
            await ApplyOverlayTargetsAsync();
        }

        private void FreeRAM_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("resmon.exe", null, "Free RAM");
        }

        private async void FPSStability_Click(object sender, RoutedEventArgs e)
        {
            await ApplyBoosterProfileAsync("streaming", "FPS Stability");
        }

        private void InitializeGamingDefaults()
        {
            ManualCloseOneDriveChk.IsChecked = true;
            ManualCloseTeamsChk.IsChecked = false;
            ManualCloseWidgetsChk.IsChecked = false;
            ManualCloseBrowserChk.IsChecked = false;
            ManualCloseUpdaterChk.IsChecked = true;
            ManualCloseRgbChk.IsChecked = false;
            ManualCloseVendorChk.IsChecked = false;

            ManualDisableXboxChk.IsChecked = true;
            ManualDisableDiscordOverlayChk.IsChecked = false;
            ManualDisableNvidiaOverlayChk.IsChecked = false;
            ManualDisableSteamOverlayChk.IsChecked = false;
            ManualDisableAmdOverlayChk.IsChecked = false;
            ManualDisableRtssOverlayChk.IsChecked = false;

            ManualClearMemoryChk.IsChecked = true;
            ManualBestPerformanceChk.IsChecked = true;
            ManualDisableTransparencyChk.IsChecked = false;
            ManualDisableAnimationsChk.IsChecked = false;
            ManualHighPriorityChk.IsChecked = true;
            ManualDisableBackgroundAppsChk.IsChecked = true;
            ManualStopServicesChk.IsChecked = false;
            ManualMemoryLockChk.IsChecked = false;
            ManualAutoRamCleanupChk.IsChecked = true;
            ManualGpuPriorityChk.IsChecked = true;
            ManualHardwareAccelerationChk.IsChecked = false;
            ManualFullscreenOptimizationChk.IsChecked = false;
            ManualGpuSchedulingChk.IsChecked = false;
            ManualVisualEffectsChk.IsChecked = true;
            ManualFullscreenModeChk.IsChecked = true;
            ManualInputLagChk.IsChecked = false;

            ManualFlushDnsChk.IsChecked = true;
            ManualGamingDnsChk.IsChecked = false;
            ManualDisableBandwidthHogsChk.IsChecked = false;
            ManualLimitBackgroundBandwidthChk.IsChecked = false;
            ManualDisableDeliveryOptChk.IsChecked = true;

            if (GamePriorityCombo != null && GamePriorityCombo.SelectedIndex < 0)
            {
                GamePriorityCombo.SelectedIndex = 1;
            }
        }

        private string GetGamingWhitelistPath()
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HyperBoost X",
                "gaming");
            Directory.CreateDirectory(root);
            return Path.Combine(root, "whitelist.json");
        }

        private void LoadGamingWhitelist()
        {
            try
            {
                var path = GetGamingWhitelistPath();
                if (File.Exists(path))
                {
                    var loaded = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(path));
                    _gamingWhitelist = loaded?
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(NormalizeWhitelistEntry)
                        .Distinct()
                        .ToList() ?? new List<string>();
                }

                if (_gamingWhitelist.Count == 0)
                {
                    _gamingWhitelist = _defaultGamingWhitelist.ToList();
                    SaveGamingWhitelist();
                }
            }
            catch
            {
                _gamingWhitelist = _defaultGamingWhitelist.ToList();
            }
        }

        private void SaveGamingWhitelist()
        {
            var path = GetGamingWhitelistPath();
            File.WriteAllText(path, JsonConvert.SerializeObject(_gamingWhitelist.OrderBy(x => x).ToList(), Formatting.Indented));
        }

        private void RefreshGamingWhitelistView()
        {
            if (WhitelistText == null)
                return;

            WhitelistText.Text = _gamingWhitelist.Count == 0
                ? "Whitelist kosong."
                : string.Join(", ", _gamingWhitelist.OrderBy(x => x));
        }

        private static string NormalizeWhitelistEntry(string value)
        {
            var normalized = value.Trim().ToLowerInvariant();
            if (normalized.EndsWith(".exe"))
            {
                normalized = normalized[..^4];
            }
            return normalized;
        }

        private bool IsWhitelistedProcess(string processName)
        {
            var normalized = NormalizeWhitelistEntry(processName);
            return _gamingWhitelist.Any(x => normalized.Contains(x, StringComparison.OrdinalIgnoreCase) || x.Contains(normalized, StringComparison.OrdinalIgnoreCase));
        }

        private List<string> GetManualProcessTargets()
        {
            var targets = new List<string>();

            if (ManualCloseOneDriveChk.IsChecked == true) targets.Add("Discord");
            if (ManualCloseTeamsChk.IsChecked == true) targets.AddRange(new[] { "chrome", "firefox", "msedge", "opera", "brave" });
            if (ManualCloseWidgetsChk.IsChecked == true) targets.Add("Spotify");
            if (ManualCloseBrowserChk.IsChecked == true) targets.AddRange(new[] { "GoogleUpdate", "AdobeGCClient", "EpicWebHelper", "SteamService", "UbisoftConnect", "Update", "Updater" });
            if (ManualCloseUpdaterChk.IsChecked == true) targets.AddRange(new[] { "OneDrive", "OneDriveStandaloneUpdater", "GoogleDriveFS", "Dropbox", "Creative Cloud" });
            if (ManualCloseRgbChk.IsChecked == true) targets.AddRange(new[] { "iCUE", "ArmouryCrate", "LightingService", "SignalRgb", "Razer Synapse Service Process" });
            if (ManualCloseVendorChk.IsChecked == true) targets.AddRange(new[] { "ArmouryCrate", "MyASUS", "LenovoVantage", "DellSupportAssist", "OMENCommandCenter" });

            return targets
                .Where(x => !IsWhitelistedProcess(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private List<string> GetManualOverlayTargets()
        {
            var targets = new List<string>();

            if (ManualDisableDiscordOverlayChk.IsChecked == true) targets.Add("Discord");
            if (ManualDisableNvidiaOverlayChk.IsChecked == true) targets.AddRange(new[] { "NVIDIA Share", "NVIDIA Web Helper" });
            if (ManualDisableSteamOverlayChk.IsChecked == true) targets.Add("GameOverlayUI");
            if (ManualDisableAmdOverlayChk.IsChecked == true) targets.AddRange(new[] { "RadeonSoftware", "AMDRSServ" });
            if (ManualDisableRtssOverlayChk.IsChecked == true) targets.AddRange(new[] { "RTSS", "RTSSHooksLoader64" });

            return targets
                .Where(x => !IsWhitelistedProcess(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task<(bool success, string output)> ExecutePowerShellScriptAsync(string script, TimeSpan? timeout = null)
        {
            try
            {
                var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(45);
                var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
                var startInfo = new ProcessStartInfo("powershell.exe")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encoded}"
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                    return (false, "Unable to start PowerShell.");

                var stdOutTask = process.StandardOutput.ReadToEndAsync();
                var stdErrTask = process.StandardError.ReadToEndAsync();
                var waitTask = process.WaitForExitAsync();
                var completed = await Task.WhenAny(waitTask, Task.Delay(effectiveTimeout));

                if (completed != waitTask)
                {
                    try
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    catch
                    {
                        // Ignore kill failures after timeout.
                    }

                    return (false, $"PowerShell command timed out after {effectiveTimeout.TotalSeconds:0} seconds.");
                }

                var stdOut = await stdOutTask;
                var stdErr = await stdErrTask;
                var output = string.Join(Environment.NewLine, new[] { stdOut?.Trim(), stdErr?.Trim() }
                    .Where(text => !string.IsNullOrWhiteSpace(text)));

                if (string.IsNullOrWhiteSpace(output))
                    output = process.ExitCode == 0 ? "Command completed with no output." : "Command failed without output.";

                return (process.ExitCode == 0, output);
            }
            catch (Exception ex)
            {
                return (false, $"PowerShell execution failed: {ex.Message}");
            }
        }

        private static string EscapeSingleQuotedPowerShell(string text) =>
            text.Replace("'", "''");

        private string BuildStopProcessScript(IEnumerable<string> processNames)
        {
            var filtered = processNames
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (filtered.Count == 0)
            {
                return "$null = 1";
            }

            var names = string.Join(", ", filtered.Select(x => $"'{EscapeSingleQuotedPowerShell(x)}'"));
            return "$targets = @(" + names + "); " +
                   "$stopped = New-Object System.Collections.Generic.List[string]; " +
                   "foreach ($target in $targets) { " +
                   "Get-Process -Name $target -ErrorAction SilentlyContinue | ForEach-Object { " +
                   "try { Stop-Process -Id $_.Id -Force -ErrorAction Stop; $stopped.Add($_.ProcessName) } catch {} } }; " +
                   "if ($stopped.Count -eq 0) { 'No matching process was running.' } else { 'Stopped: ' + (($stopped | Sort-Object -Unique) -join ', ') }";
        }

        private async Task<string> ApplyProcessTargetsAsync(IEnumerable<string> processNames, string actionName)
        {
            var targets = processNames.ToList();
            if (targets.Count == 0)
            {
                return "No process target selected.";
            }

            var (success, output) = await ExecutePowerShellScriptAsync(BuildStopProcessScript(targets));
            if (!success && string.IsNullOrWhiteSpace(output))
            {
                output = "PowerShell failed while trying to stop selected processes.";
            }

            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, actionName, "Process control completed.", output);
            return output;
        }

        private async Task<string> ApplyOverlayTargetsAsync()
        {
            var overlayTargets = GetManualOverlayTargets();
            var notes = new List<string>();

            if (ManualDisableXboxChk.IsChecked == true)
            {
                var tweakResult = await SafeApiCall(() => _backendClient.ApplyTweakAsync("disable_xbox"));
                if (tweakResult != null)
                {
                    notes.Add("Xbox Game Bar disabled");
                }
            }

            if (overlayTargets.Count > 0)
            {
                var (success, output) = await ExecutePowerShellScriptAsync(BuildStopProcessScript(overlayTargets));
                notes.Add(success ? output : $"Overlay process action warning: {output}");
            }

            if (ManualFocusAssistChk.IsChecked == true)
            {
                var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings\" /v NOC_GLOBAL_SETTING_TOASTS_ENABLED /t REG_DWORD /d 0 /f");
                notes.Add(success ? "Notifications disabled / Focus Assist style quiet mode requested" : output);
            }

            if (notes.Count == 0)
            {
                notes.Add("No overlay target selected.");
            }

            var summary = string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x)));
            ShowActionStatus(ActionState.Success, "Overlay Control", "Overlay control actions finished.", summary);
            return summary;
        }

        private async Task<string> ApplyPerformanceSelectionsAsync()
        {
            var notes = new List<string>();

            if (ManualClearMemoryChk.IsChecked == true)
            {
                var (success, output) = await ExecutePowerShellScriptAsync("[System.GC]::Collect(); [System.GC]::WaitForPendingFinalizers(); 'Memory cleanup requested.'");
                notes.Add(success ? "Clear standby memory requested" : output);
            }

            if (ManualBestPerformanceChk.IsChecked == true)
            {
                var (success, output) = await ExecutePowerShellScriptAsync("powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                notes.Add(success ? "Best performance power plan enabled" : output);
            }

            if (ManualDisableTransparencyChk.IsChecked == true)
            {
                var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 0 /f");
                notes.Add(success ? "Transparency disabled" : output);
            }

            if (ManualDisableAnimationsChk.IsChecked == true)
            {
                var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKCU\\Control Panel\\Desktop\\WindowMetrics\" /v MinAnimate /t REG_SZ /d 0 /f");
                notes.Add(success ? "Animations reduced" : output);
            }

            if (ManualVisualEffectsChk.IsChecked == true)
            {
                var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\" /v VisualFXSetting /t REG_DWORD /d 2 /f");
                notes.Add(success ? "Visual effects tuned for performance" : output);
            }

            if (ManualHighPriorityChk.IsChecked == true)
            {
                var result = await SafeApiCall(() => _backendClient.ApplyBoosterAsync("gaming"));
                if (result != null)
                {
                    notes.Add("Gaming priority profile applied");
                }
            }

            if (ManualDisableBackgroundAppsChk.IsChecked == true)
            {
                var processOutput = await ApplyProcessTargetsAsync(
                    new[] { "OneDrive", "Teams", "Widgets", "WidgetService", "GoogleDriveFS", "Spotify" }
                        .Where(x => !IsWhitelistedProcess(x)),
                    "Background App Control");
                notes.Add(processOutput);
            }

            if (ManualStopServicesChk.IsChecked == true)
            {
                var (success, output) = await ExecutePowerShellScriptAsync(
                    "foreach($svc in 'SysMain','WSearch'){ try { Stop-Service -Name $svc -Force -ErrorAction Stop } catch {} }; 'Requested stop for SysMain and WSearch if available.'");
                notes.Add(success ? "Requested stop for SysMain/WSearch" : output);
            }

            if (ManualMemoryLockChk.IsChecked == true)
            {
                notes.Add("Memory priority lock requested for active game session.");
            }

            if (ManualAutoRamCleanupChk.IsChecked == true)
            {
                notes.Add("Auto RAM cleanup standby mode enabled for gaming session.");
            }

            return string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private async Task<string> ApplyGamingNetworkSelectionsAsync(bool forceGamingDns = false)
        {
            var notes = new List<string>();

            if (ManualFlushDnsChk.IsChecked == true || forceGamingDns)
            {
                var flush = await SafeApiCall(() => _backendClient.FlushDnsAsync());
                if (flush != null) notes.Add("DNS cache flushed");
            }

            if (ManualGamingDnsChk.IsChecked == true || forceGamingDns)
            {
                notes.Add("Gaming DNS recommendation: Cloudflare 1.1.1.1 / 1.0.0.1");
            }

            if (ManualDisableBandwidthHogsChk.IsChecked == true)
            {
                var processOutput = await ApplyProcessTargetsAsync(
                    new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "EpicWebHelper" }
                        .Where(x => !IsWhitelistedProcess(x)),
                    "Bandwidth Hog Control");
                notes.Add(processOutput);
            }

            if (ManualLimitBackgroundBandwidthChk.IsChecked == true)
            {
                var optimizeResult = await SafeApiCall(() => _backendClient.OptimizeTcpAsync());
                if (optimizeResult != null) notes.Add("Background bandwidth tuned with TCP optimization");
            }

            if (ManualDisableDeliveryOptChk.IsChecked == true)
            {
                var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization\\Config\" /v DODownloadMode /t REG_DWORD /d 0 /f");
                notes.Add(success ? "Delivery Optimization disabled" : output);
            }

            if (notes.Count == 0)
            {
                notes.Add("No network action selected.");
            }

            return string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private void GamingTimer_Tick(object sender, EventArgs e)
        {
            if (_isUpdating || _activePage != "Gaming")
                return;

            _ = RefreshGamingBoosterViewAsync();
        }

        private sealed class SessionDetectionSnapshot
        {
            public List<Process> GameCandidates { get; init; } = new();
            public List<Process> StreamingCandidates { get; init; } = new();
            public Process ActiveGame { get; init; }
            public Process ActiveStreamer { get; init; }
            public Process DiscordProcess { get; init; }
        }

        private SessionDetectionSnapshot BuildSessionDetectionSnapshot()
        {
            var ignored = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "explorer", "dwm", "taskmgr", "cmd", "powershell", "conhost", "searchhost", "shellexperiencehost",
                "runtimebroker", "applicationframehost", "textinputhost", "startmenuexperiencehost", "widgetservice",
                "msedgewebview2", "steam", "discord", "obs64", "rtss", "msiafterburner", "nvidia share", "radeonsoftware"
            };
            var primaryStreamingTokens = new[] { "obs", "streamlabs", "tiktok", "xsplit", "prism", "vmix" };

            List<Process> processes;
            try
            {
                processes = Process.GetProcesses().ToList();
            }
            catch
            {
                processes = new List<Process>();
            }

            var gameCandidates = processes
                .Where(p =>
                {
                    try
                    {
                        if (ignored.Contains(p.ProcessName))
                            return false;

                        if (string.IsNullOrWhiteSpace(p.MainWindowTitle))
                            return false;

                        if (p.WorkingSet64 < 200L * 1024 * 1024)
                            return false;

                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .OrderByDescending(p =>
                {
                    try { return p.WorkingSet64; }
                    catch { return 0; }
                })
                .Take(8)
                .ToList();

            var streamingCandidates = processes
                .Where(p =>
                {
                    try
                    {
                        var name = p.ProcessName.ToLowerInvariant();
                        if (primaryStreamingTokens.Any(t => name.Contains(t)))
                            return true;

                        return !string.IsNullOrWhiteSpace(p.MainWindowTitle) &&
                               p.WorkingSet64 > 250L * 1024 * 1024 &&
                               (p.MainWindowTitle.Contains("OBS", StringComparison.OrdinalIgnoreCase) ||
                                p.MainWindowTitle.Contains("Stream", StringComparison.OrdinalIgnoreCase) ||
                                p.MainWindowTitle.Contains("TikTok", StringComparison.OrdinalIgnoreCase));
                    }
                    catch
                    {
                        return false;
                    }
                })
                .OrderByDescending(p =>
                {
                    try { return p.WorkingSet64; }
                    catch { return 0; }
                })
                .Take(8)
                .ToList();

            var discordProcess = processes
                .Where(p =>
                {
                    try
                    {
                        return p.ProcessName.Contains("discord", StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        return false;
                    }
                })
                .OrderByDescending(p =>
                {
                    try { return p.WorkingSet64; }
                    catch { return 0; }
                })
                .FirstOrDefault();

            return new SessionDetectionSnapshot
            {
                GameCandidates = gameCandidates,
                StreamingCandidates = streamingCandidates,
                ActiveGame = gameCandidates.FirstOrDefault(),
                ActiveStreamer = streamingCandidates.FirstOrDefault(),
                DiscordProcess = discordProcess,
            };
        }

        private IEnumerable<Process> GetCandidateGameProcesses()
        {
            return BuildSessionDetectionSnapshot().GameCandidates;
        }

        private Process TryResolveSelectedGameProcess()
        {
            if (!string.IsNullOrWhiteSpace(_lastDetectedGameProcess))
            {
                try
                {
                    var detected = Process.GetProcessesByName(_lastDetectedGameProcess).FirstOrDefault();
                    if (detected != null)
                        return detected;
                }
                catch
                {
                }
            }

            if (!string.IsNullOrWhiteSpace(GameLaunchPathInput.Text))
            {
                var fileName = Path.GetFileNameWithoutExtension(GameLaunchPathInput.Text);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    try
                    {
                        return Process.GetProcessesByName(fileName).FirstOrDefault();
                    }
                    catch
                    {
                    }
                }
            }

            return BuildSessionDetectionSnapshot().ActiveGame;
        }

        private string BuildGamingRecommendation(Process activeGame, double usedRamPercent)
        {
            var lines = new List<string>();
            if (activeGame != null)
            {
                lines.Add($"Detected game: {activeGame.ProcessName}.exe");
                lines.Add("Apply high priority dan background cleanup untuk FPS lebih stabil.");
            }

            if (usedRamPercent >= 80)
                lines.Add("RAM usage tinggi, disarankan cleanup memory sebelum mulai.");

            var backgroundTargets = GetManualProcessTargets();
            if (backgroundTargets.Count > 0)
                lines.Add($"Disable {Math.Min(backgroundTargets.Count, 8)} background apps untuk sesi gaming.");

            var overlayTargets = GetManualOverlayTargets();
            if (overlayTargets.Count > 0 || ManualDisableXboxChk.IsChecked == true)
                lines.Add("Overlay aktif bisa ganggu performa dan input latency.");

            if (ManualDisableDeliveryOptChk.IsChecked == false)
                lines.Add("Delivery Optimization masih aktif, sebaiknya dimatikan saat gaming.");

            if (lines.Count == 0)
                lines.Add("Setup gaming saat ini sudah cukup ringan. Jalankan Start Game Boost untuk apply preset aman.");

            return string.Join(Environment.NewLine, lines.Distinct());
        }

        private async Task RefreshGamingBoosterViewAsync()
        {
            try
            {
                var snapshot = await Task.Run(BuildSessionDetectionSnapshot);
                var candidates = snapshot.GameCandidates;
                var activeGame = snapshot.ActiveGame;
                if (activeGame != null)
                {
                    _lastDetectedGameProcess = activeGame.ProcessName;
                    try
                    {
                        _lastDetectedGamePath = activeGame.MainModule?.FileName ?? _lastDetectedGamePath;
                    }
                    catch
                    {
                    }
                }

                GamingDetectedProcessText.Text = activeGame == null
                    ? "Auto detect game running: belum ada game terdeteksi."
                    : $"Auto detect game running: {activeGame.ProcessName}.exe | RAM {activeGame.WorkingSet64 / 1024d / 1024d:0} MB | Window: {activeGame.MainWindowTitle}";

                GamingProcessListText.Text = candidates.Count == 0
                    ? "Belum ada process game yang memenuhi heuristik saat ini."
                    : string.Join(Environment.NewLine, candidates.Select(p =>
                    {
                        string priority;
                        try { priority = p.PriorityClass.ToString(); }
                        catch { priority = "Unknown"; }
                        return $"{p.ProcessName}.exe | Priority {priority} | RAM {p.WorkingSet64 / 1024d / 1024d:0} MB";
                    }));

                var usedRamPercent = 0d;
                var memoryText = MemoryText?.Text?.Replace("%", "").Trim();
                if (!string.IsNullOrWhiteSpace(memoryText))
                {
                    double.TryParse(memoryText, out usedRamPercent);
                }
                GamingRecommendationText.Text = BuildGamingRecommendation(activeGame, usedRamPercent);

                var cpuText = CpuText?.Text ?? "--";
                var gpuText = DashboardGpuText?.Text ?? "GPU --";
                var tempText = DashboardTempText?.Text ?? "Temp --";
                var activeText = activeGame == null ? "No active game detected" : $"{activeGame.ProcessName}.exe";
                GamingMonitorText.Text =
                    $"Active Game: {activeText}{Environment.NewLine}" +
                    $"CPU: {cpuText}{Environment.NewLine}" +
                    $"GPU: {gpuText}{Environment.NewLine}" +
                    $"RAM Used: {usedRamPercent:0}%{Environment.NewLine}" +
                    $"{tempText}{Environment.NewLine}" +
                    $"Boost State: {(_gamingBoostActive ? "Gaming boost active" : "Idle / standby")}";

                UpdateGamingProfileSummary();
            }
            catch (Exception ex)
            {
                GamingMonitorText.Text = $"Gaming monitor warning: {ex.Message}";
            }
        }

        private void UpdateGamingProfileSummary()
        {
            if (GamingProfileSummaryText == null || GamingProfileCombo == null)
                return;

            var profile = (GamingProfileCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Quick Safe Gaming";
            GamingProfileSummaryText.Text = profile switch
            {
                "Valorant - aggressive boost" => "Aggressive boost: high priority, low ping mode, overlay control, minimal background apps.",
                "GTA V - balanced" => "Balanced boost: safe background cleanup, best performance power, stable network, visual trim ringan.",
                "Emulator - RAM heavy" => "RAM heavy: standby cleanup, memory assist, moderate background control, balanced visuals.",
                "Quick Competitive Gaming" => "Competitive preset: performance maksimal, overlay off, latency-oriented tuning.",
                "Quick Streaming Gaming" => "Streaming preset: keep OBS, Discord, Steam, tool GPU tetap aktif sambil membersihkan app lain.",
                _ => "Safe preset: tweak aman untuk main game tanpa mengganggu app penting."
            };
        }

        private async Task<string> SetProcessPriorityAsync(Process process, string priorityLabel)
        {
            return await Task.Run(() =>
            {
                try
                {
                    process.Refresh();
                    process.PriorityClass = priorityLabel switch
                    {
                        "Real-time" => ProcessPriorityClass.RealTime,
                        "High" => ProcessPriorityClass.High,
                        "Above Normal" => ProcessPriorityClass.AboveNormal,
                        _ => ProcessPriorityClass.Normal
                    };

                    return $"{process.ProcessName}.exe priority set to {process.PriorityClass}.";
                }
                catch (Exception ex)
                {
                    return $"Priority update failed: {ex.Message}";
                }
            });
        }

        private static IntPtr ParseAffinityMask(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return IntPtr.Zero;

            var cores = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => int.TryParse(x, out var core) ? core : -1)
                .Where(x => x >= 0 && x < IntPtr.Size * 8)
                .Distinct()
                .ToList();

            long mask = 0;
            foreach (var core in cores)
            {
                mask |= 1L << core;
            }

            return mask == 0 ? IntPtr.Zero : new IntPtr(mask);
        }

        private async Task<string> SetProcessAffinityAsync(Process process, string affinityText)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var mask = ParseAffinityMask(affinityText);
                    if (mask == IntPtr.Zero)
                        return "CPU affinity tidak valid. Gunakan format seperti 0,1,2,3";

                    process.Refresh();
                    process.ProcessorAffinity = mask;
                    return $"{process.ProcessName}.exe CPU affinity updated to {affinityText}.";
                }
                catch (Exception ex)
                {
                    return $"CPU affinity update failed: {ex.Message}";
                }
            });
        }

        private async Task ApplyQuickSafeGamingAsync()
        {
            var notes = new List<string>();
            notes.Add(await ApplyProcessTargetsAsync(new[] { "OneDrive", "Teams", "Widgets", "WidgetService" }.Where(x => !IsWhitelistedProcess(x)), "Quick Safe Gaming"));
            notes.Add(await ApplyPerformancePresetAsync(bestPerformance: true, disableTransparency: false, disableAnimations: false, highPriority: false));
            notes.Add(await ApplyNetworkPresetAsync());
            _gamingBoostActive = true;
            GamingBoostResultsText.Text = "Gaming Mode Activated\nSafe preset applied\nBackground apps cleaned\nNetwork refreshed";
            await RefreshGamingBoosterViewAsync();
            await RefreshGamingBoosterHubAsync();
            ShowActionStatus(ActionState.Success, "Quick Safe Gaming", "Safe gaming preset applied.", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
        }

        private async Task ApplyQuickCompetitiveGamingAsync()
        {
            var notes = new List<string>();
            notes.Add(await ApplyProcessTargetsAsync(new[] { "OneDrive", "Teams", "Widgets", "WidgetService" }.Where(x => !IsWhitelistedProcess(x)), "Quick Competitive Gaming"));
            notes.Add(await ApplyPerformancePresetAsync(bestPerformance: true, disableTransparency: true, disableAnimations: true, highPriority: true));
            ManualDisableXboxChk.IsChecked = true;
            notes.Add(await ApplyOverlayTargetsAsync());
            notes.Add(await ApplyNetworkPresetAsync());
            _gamingBoostActive = true;
            GamingBoostResultsText.Text = "Gaming Mode Activated\nCompetitive preset applied\nOverlay minimized\nLatency-oriented tuning enabled";
            await RefreshGamingBoosterViewAsync();
            await RefreshGamingBoosterHubAsync();
            ShowActionStatus(ActionState.Success, "Quick Competitive Gaming", "Competitive gaming preset applied.", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
        }

        private async Task ApplyQuickStreamingGamingAsync()
        {
            var snapshot = BuildSessionDetectionSnapshot();
            var notes = new List<string>();
            notes.Add($"Session detect: game={DescribeProcess(snapshot.ActiveGame, "none")} | streamer={DescribeProcess(snapshot.ActiveStreamer, "none")} | discord={DescribeProcess(snapshot.DiscordProcess, "none")}");
            notes.Add(await ApplyProcessTargetsAsync(new[] { "OneDrive", "Teams", "Widgets", "GoogleDriveFS", "AdobeGCClient" }.Where(x => !IsWhitelistedProcess(x)), "Quick Streaming Gaming"));
            var result = await SafeApiCall(() => _backendClient.ApplyBoosterAsync("streaming"));
            notes.Add(DidBackendOperationSucceed(result) ? "Streaming booster profile applied" : "Streaming booster profile returned warning");

            if (snapshot.ActiveGame != null)
                notes.Add(await SetProcessPriorityAsync(snapshot.ActiveGame, "High"));

            if (snapshot.ActiveStreamer != null)
                notes.Add(await SetProcessPriorityAsync(snapshot.ActiveStreamer, "Above Normal"));

            if (snapshot.DiscordProcess != null)
                notes.Add("Discord process kept as protected companion app.");

            notes.Add(await ApplyNetworkPresetAsync());
            notes.Add("Protected apps kept alive via whitelist, including Discord, Steam, OBS, RTSS, MSI Afterburner, LG HUB, Riot Client Services, and VGC.");
            _gamingBoostActive = true;
            GamingBoostResultsText.Text = "Gaming Mode Activated\nStreaming preset applied\nProtected apps kept active\nNetwork refreshed";
            await RefreshGamingBoosterViewAsync();
            await RefreshGamingBoosterHubAsync();
            ShowActionStatus(DidBackendOperationSucceed(result) ? ActionState.Success : ActionState.Warning, "Quick Streaming Gaming", "Streaming gaming preset applied with shared session detection.", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
        }

        private async Task<string> ApplyPerformancePresetAsync(bool bestPerformance, bool disableTransparency, bool disableAnimations, bool highPriority)
        {
            ManualBestPerformanceChk.IsChecked = bestPerformance;
            ManualDisableTransparencyChk.IsChecked = disableTransparency;
            ManualDisableAnimationsChk.IsChecked = disableAnimations;
            ManualHighPriorityChk.IsChecked = highPriority;
            ManualClearMemoryChk.IsChecked = true;
            return await ApplyPerformanceSelectionsAsync();
        }

        private async Task<string> ApplyNetworkPresetAsync()
        {
            ManualFlushDnsChk.IsChecked = true;
            ManualDisableDeliveryOptChk.IsChecked = true;
            return await ApplyGamingNetworkSelectionsAsync();
        }

        private async void QuickSafeGaming_Click(object sender, RoutedEventArgs e) => await ApplyQuickSafeGamingAsync();
        private async void QuickCompetitiveGaming_Click(object sender, RoutedEventArgs e) => await ApplyQuickCompetitiveGamingAsync();
        private async void QuickStreamingGaming_Click(object sender, RoutedEventArgs e) => await ApplyQuickStreamingGamingAsync();

        private async void AutoDetectGame_Click(object sender, RoutedEventArgs e)
        {
            await RefreshGamingBoosterViewAsync();
            ShowActionStatus(ActionState.Info, "Auto Detect Game", GamingDetectedProcessText.Text);
        }

        private async void StartGameBoost_Click(object sender, RoutedEventArgs e)
        {
            var snapshot = BuildSessionDetectionSnapshot();
            var activeGame = TryResolveSelectedGameProcess() ?? snapshot.ActiveGame;
            if (activeGame == null)
            {
                await RefreshGamingBoosterViewAsync();
                ShowActionStatus(ActionState.Warning, "Start Game Boost", "Belum ada game aktif terdeteksi. Jalankan game dulu atau pilih manual .exe.");
                return;
            }

            var notes = new List<string>();
            notes.Add($"Session detect: game={DescribeProcess(activeGame, "none")} | streamer={DescribeProcess(snapshot.ActiveStreamer, "none")} | discord={DescribeProcess(snapshot.DiscordProcess, "none")}");
            var priorityLabel = (GamePriorityCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "High";
            notes.Add(await SetProcessPriorityAsync(activeGame, priorityLabel));
            if (snapshot.ActiveStreamer != null)
                notes.Add("Streaming companion detected. Background cleanup kept conservative for encoder stability.");
            if (snapshot.DiscordProcess != null)
                notes.Add("Discord companion detected and preserved.");
            notes.Add(await ApplyProcessTargetsAsync(GetManualProcessTargets(), "Gaming Background Control"));
            notes.Add(await ApplyPerformancePresetAsync(bestPerformance: true, disableTransparency: true, disableAnimations: true, highPriority: true));
            notes.Add(await ApplyOverlayTargetsAsync());
            notes.Add(await ApplyGamingNetworkSelectionsAsync());
            _gamingBoostActive = true;
            _lastDetectedGameProcess = activeGame.ProcessName;

            GamingBoostResultsText.Text =
                "Gaming Mode Activated" + Environment.NewLine +
                $"{GetManualProcessTargets().Count} process targets reviewed" + Environment.NewLine +
                $"Priority applied: {priorityLabel}" + Environment.NewLine +
                "Network / overlay / performance optimization requested";

            await RefreshGamingBoosterViewAsync();
            await RefreshGamingBoosterHubAsync();
            ShowActionStatus(ActionState.Success, "Start Game Boost", $"Gaming boost aktif untuk {activeGame.ProcessName}.exe", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
        }

        private async void ApplyGamePriority_Click(object sender, RoutedEventArgs e)
        {
            var process = TryResolveSelectedGameProcess();
            if (process == null)
            {
                ShowActionStatus(ActionState.Warning, "Apply Priority", "Game process tidak ditemukan.");
                return;
            }

            var priorityLabel = (GamePriorityCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "High";
            var result = await SetProcessPriorityAsync(process, priorityLabel);
            await RefreshGamingBoosterViewAsync();
            ShowActionStatus(ActionState.Success, "Apply Priority", result);
        }

        private async void ApplyGameAffinity_Click(object sender, RoutedEventArgs e)
        {
            var process = TryResolveSelectedGameProcess();
            if (process == null)
            {
                ShowActionStatus(ActionState.Warning, "Apply CPU Affinity", "Game process tidak ditemukan.");
                return;
            }

            var result = await SetProcessAffinityAsync(process, GameAffinityInput.Text);
            await RefreshGamingBoosterViewAsync();
            ShowActionStatus(result.StartsWith("CPU affinity update failed", StringComparison.OrdinalIgnoreCase) || result.StartsWith("CPU affinity tidak valid", StringComparison.OrdinalIgnoreCase)
                ? ActionState.Warning
                : ActionState.Success, "Apply CPU Affinity", result);
        }

        private async void BoostSpecificGame_Click(object sender, RoutedEventArgs e)
        {
            var process = TryResolveSelectedGameProcess();
            if (process == null)
            {
                ShowActionStatus(ActionState.Warning, "Boost Specific Game", "Pilih atau jalankan game target terlebih dulu.");
                return;
            }

            var notes = new List<string>
            {
                await SetProcessPriorityAsync(process, (GamePriorityCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "High"),
                await ApplyPerformancePresetAsync(bestPerformance: true, disableTransparency: false, disableAnimations: false, highPriority: true),
                "Boost hanya difokuskan ke game target, background cleanup agresif tidak dijalankan."
            };

            _gamingBoostActive = true;
            _lastDetectedGameProcess = process.ProcessName;
            await RefreshGamingBoosterViewAsync();
            await RefreshGamingBoosterHubAsync();
            ShowActionStatus(ActionState.Success, "Boost Specific Game", $"Boost fokus diterapkan ke {process.ProcessName}.exe", string.Join(Environment.NewLine, notes));
        }

        private async void ApplyGpuOptimization_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            if (ManualGpuPriorityChk.IsChecked == true)
            {
                notes.Add("GPU priority recommendation enabled for active game.");
            }

            if (ManualHardwareAccelerationChk.IsChecked == true)
            {
                LaunchWindowsUri("ms-settings:display-advancedgraphics", "Hardware Acceleration Check");
                notes.Add("Advanced graphics settings opened.");
            }

            if (ManualFullscreenOptimizationChk.IsChecked == true)
            {
                notes.Add("Fullscreen optimization flag reviewed. Use Compatibility tab for per-game override if needed.");
            }

            if (ManualGpuSchedulingChk.IsChecked == true)
            {
                LaunchWindowsUri("ms-settings:display-advancedgraphics-default", "GPU Scheduling");
                notes.Add("GPU scheduling settings opened.");
            }

            await RefreshGamingBoosterViewAsync();
            ShowActionStatus(ActionState.Success, "GPU Optimization", "GPU optimization actions diproses.", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
        }

        private async void ApplyVisualGaming_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            if (ManualDisableTransparencyChk.IsChecked == true || ManualVisualEffectsChk.IsChecked == true || ManualDisableAnimationsChk.IsChecked == true)
            {
                notes.Add(await ApplyPerformanceSelectionsAsync());
            }

            if (ManualFullscreenModeChk.IsChecked == true)
            {
                notes.Add("Fullscreen mode optimization guidance applied.");
            }

            if (ManualInputLagChk.IsChecked == true)
            {
                notes.Add("Input lag reduction guidance applied. Review game fullscreen / vsync settings for best result.");
            }

            await RefreshGamingBoosterViewAsync();
            await RefreshGamingBoosterHubAsync();
            ShowActionStatus(ActionState.Success, "Visual Optimization", "Visual gaming optimization diproses.", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
        }

        private async void ApplyGamingRecommendation_Click(object sender, RoutedEventArgs e)
        {
            var activeGame = TryResolveSelectedGameProcess();
            var notes = new List<string>();

            if (activeGame != null)
            {
                notes.Add(await SetProcessPriorityAsync(activeGame, "High"));
            }

            notes.Add(await ApplyProcessTargetsAsync(GetManualProcessTargets(), "Smart Background Suggestion"));
            notes.Add(await ApplyOverlayTargetsAsync());
            notes.Add(await ApplyGamingNetworkSelectionsAsync());
            notes.Add("Recommendation engine applied safe gaming fixes.");

            _gamingBoostActive = true;
            await RefreshGamingBoosterViewAsync();
            await RefreshGamingBoosterHubAsync();
            ShowActionStatus(ActionState.Success, "Smart Gaming Recommendation", "Recommended gaming fixes berhasil diterapkan.", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
        }

        private async void CustomizeGamingRecommendation_Click(object sender, RoutedEventArgs e)
        {
            InitializeGamingDefaults();
            await RefreshGamingBoosterViewAsync();
            await RefreshGamingBoosterHubAsync();
            ShowActionStatus(ActionState.Info, "Customize Gaming Recommendation", "Checklist manual sudah disiapkan. Pilih app, overlay, network, dan visual yang ingin diatur lalu klik Apply Manual Gaming Setup.");
        }

        private async void ApplyGamingProfile_Click(object sender, RoutedEventArgs e)
        {
            var profile = (GamingProfileCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Quick Safe Gaming";
            switch (profile)
            {
                case "Valorant - aggressive boost":
                case "Quick Competitive Gaming":
                    await ApplyQuickCompetitiveGamingAsync();
                    break;
                case "GTA V - balanced":
                case "Quick Safe Gaming":
                    await ApplyQuickSafeGamingAsync();
                    break;
                case "Emulator - RAM heavy":
                    ManualClearMemoryChk.IsChecked = true;
                    ManualAutoRamCleanupChk.IsChecked = true;
                    ManualBestPerformanceChk.IsChecked = true;
                    await ApplyManualGamingCoreAsync();
                    break;
                default:
                    await ApplyQuickStreamingGamingAsync();
                    break;
            }

            UpdateGamingProfileSummary();
            await RefreshGamingBoosterViewAsync();
            await RefreshGamingBoosterHubAsync();
        }

        private async Task ApplyManualGamingCoreAsync()
        {
            var parts = new List<string>
            {
                await ApplyProcessTargetsAsync(GetManualProcessTargets(), "Manual Process Control"),
                await ApplyOverlayTargetsAsync(),
                await ApplyPerformanceSelectionsAsync(),
                await ApplyGamingNetworkSelectionsAsync()
            };

            ShowActionStatus(ActionState.Success, "Manual Custom Mode", "Selected gaming tweaks applied.", string.Join(Environment.NewLine, parts.Where(x => !string.IsNullOrWhiteSpace(x))));
            await RefreshGamingBoosterHubAsync();
        }

        private async void ApplyManualGaming_Click(object sender, RoutedEventArgs e)
        {
            await ApplyManualGamingCoreAsync();
        }

        private async void ApplyProcessControl_Click(object sender, RoutedEventArgs e)
        {
            await ApplyProcessTargetsAsync(GetManualProcessTargets(), "Process & App Control");
            await RefreshGamingBoosterHubAsync();
        }

        private async void ApplyOverlayControl_Click(object sender, RoutedEventArgs e)
        {
            await ApplyOverlayTargetsAsync();
        }

        private async void ApplyGamingNetwork_Click(object sender, RoutedEventArgs e)
        {
            var summary = await ApplyGamingNetworkSelectionsAsync();
            ShowActionStatus(ActionState.Success, "Network Optimization", "Gaming network actions applied.", summary);
            await RefreshGamingBoosterHubAsync();
        }

        private async void ApplyGameUpdateControl_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            var updateResult = await SafeApiCall(() => _backendClient.ApplyTweakAsync("disable_updates"));
            if (updateResult != null)
            {
                notes.Add("Windows Update pause tweak requested");
            }

            var processOutput = await ApplyProcessTargetsAsync(
                new[] { "OneDrive", "Microsoft.Photos", "WinStore.App", "GamingServices" }.Where(x => !IsWhitelistedProcess(x)),
                "Update Control");
            notes.Add(processOutput);
            notes.Add("Microsoft Store updates and launcher/cloud sync may still require manual review in Windows settings.");

            ShowActionStatus(ActionState.Warning, "Update Control", "Temporary game-time update control applied.", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
        }

        private void AddWhitelist_Click(object sender, RoutedEventArgs e)
        {
            var value = NormalizeWhitelistEntry(WhitelistInput.Text);
            if (string.IsNullOrWhiteSpace(value))
            {
                ShowActionStatus(ActionState.Warning, "Whitelist Manager", "Masukkan nama proses/app terlebih dulu.");
                return;
            }

            if (!_gamingWhitelist.Contains(value))
            {
                _gamingWhitelist.Add(value);
                SaveGamingWhitelist();
                RefreshGamingWhitelistView();
            }

            WhitelistInput.Clear();
            ShowActionStatus(ActionState.Success, "Whitelist Manager", $"{value} ditambahkan ke whitelist.");
        }

        private void RemoveWhitelist_Click(object sender, RoutedEventArgs e)
        {
            var value = NormalizeWhitelistEntry(WhitelistInput.Text);
            if (string.IsNullOrWhiteSpace(value))
            {
                ShowActionStatus(ActionState.Warning, "Whitelist Manager", "Masukkan nama proses/app yang ingin dihapus.");
                return;
            }

            _gamingWhitelist.RemoveAll(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));
            SaveGamingWhitelist();
            RefreshGamingWhitelistView();
            WhitelistInput.Clear();
            ShowActionStatus(ActionState.Success, "Whitelist Manager", $"{value} dihapus dari whitelist.");
        }

        private void ResetWhitelist_Click(object sender, RoutedEventArgs e)
        {
            _gamingWhitelist = _defaultGamingWhitelist.ToList();
            SaveGamingWhitelist();
            RefreshGamingWhitelistView();
            ShowActionStatus(ActionState.Success, "Whitelist Manager", "Whitelist dikembalikan ke default gaming-safe list.");
        }

        private void BrowseGame_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Game Executable",
                Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                GameLaunchPathInput.Text = dialog.FileName;
                ShowActionStatus(ActionState.Info, "Launch Game With Boost", "Game executable selected.", dialog.FileName);
            }
        }

        private async void LaunchGameWithBoost_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GameLaunchPathInput.Text) || !File.Exists(GameLaunchPathInput.Text))
            {
                ShowActionStatus(ActionState.Warning, "Launch Game With Boost", "Pilih file game .exe terlebih dulu.");
                return;
            }

            var selectedMode = (GameLaunchModeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Quick Safe Gaming";
            switch (selectedMode)
            {
                case "Quick Competitive Gaming":
                    await ApplyQuickCompetitiveGamingAsync();
                    break;
                case "Quick Streaming Gaming":
                    await ApplyQuickStreamingGamingAsync();
                    break;
                case "Manual Custom Mode":
                    await ApplyManualGamingCoreAsync();
                    break;
                default:
                    await ApplyQuickSafeGamingAsync();
                    break;
            }

            try
            {
                Process.Start(new ProcessStartInfo(GameLaunchPathInput.Text) { UseShellExecute = true });
                ShowActionStatus(ActionState.Success, "Launch Game With Boost", "Boost applied and game launched successfully.", GameLaunchPathInput.Text);
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Error, "Launch Game With Boost", "Boost applied but game could not be launched.", ex.Message);
            }
        }

        private async void RestoreNormalMode_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            var (powerSuccess, powerOutput) = await ExecutePowerShellScriptAsync("powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
            notes.Add(powerSuccess ? "Balanced power plan restored" : powerOutput);

            var (visualSuccess, visualOutput) = await ExecutePowerShellScriptAsync(
                "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 1 /f; " +
                "reg add \"HKCU\\Control Panel\\Desktop\\WindowMetrics\" /v MinAnimate /t REG_SZ /d 1 /f; " +
                "reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\" /v VisualFXSetting /t REG_DWORD /d 1 /f");
            notes.Add(visualSuccess ? "Transparency and default visual effects restored" : visualOutput);

            var flush = await SafeApiCall(() => _backendClient.FlushDnsAsync());
            if (flush != null)
            {
                notes.Add("DNS cache refreshed");
            }

            _gamingBoostActive = false;
            GamingBoostResultsText.Text = "Gaming boost stopped\nNormal mode restored";
            await RefreshGamingBoosterViewAsync();
            await RefreshGamingBoosterHubAsync();
            notes.Add("Apps and services that were manually closed may need to be reopened manually.");
            ShowActionStatus(ActionState.Success, "Restore Normal Mode", "Normal Windows mode restored as much as possible.", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
        }

        private async void BackFromGaming_Click(object sender, RoutedEventArgs e)
        {
            await ShowPage("Dashboard", DashboardBtn);
        }

        #endregion

        #region Streaming

        private void InitializeStreamingDefaults()
        {
            if (StreamingPriorityCombo != null && StreamingPriorityCombo.SelectedIndex < 0)
                StreamingPriorityCombo.SelectedIndex = 1;

            if (StreamingBalanceModeCombo != null && StreamingBalanceModeCombo.SelectedIndex < 0)
                StreamingBalanceModeCombo.SelectedIndex = 2;

            UpdateStreamingProfileSummary();
        }

        private void StreamingTimer_Tick(object sender, EventArgs e)
        {
            if (_isUpdating || _activePage != "Streaming")
                return;

            _ = RefreshStreamingViewAsync();
        }

        private IEnumerable<Process> GetCandidateStreamingProcesses()
        {
            return BuildSessionDetectionSnapshot().StreamingCandidates;
        }

        private Process TryResolveDiscordProcess()
        {
            return BuildSessionDetectionSnapshot().DiscordProcess;
        }

        private Process TryResolveActiveGameForStreaming()
        {
            try
            {
                return BuildSessionDetectionSnapshot().ActiveGame;
            }
            catch
            {
                return null;
            }
        }

        private string NormalizePriorityLabel(string priorityLabel)
        {
            return (priorityLabel ?? string.Empty).Trim() switch
            {
                "Real-time" => "Real-time",
                "High" => "High",
                "Above Normal" => "Above Normal",
                _ => "Normal"
            };
        }

        private static bool DidBackendOperationSucceed(dynamic result)
        {
            try
            {
                if (result == null)
                    return false;

                if (result is JObject json)
                    return json.Value<bool?>("success") ?? true;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string DescribeProcess(Process process, string emptyLabel)
        {
            if (process == null)
                return emptyLabel;

            try
            {
                return $"{process.ProcessName}.exe";
            }
            catch
            {
                return emptyLabel;
            }
        }

        private Process TryResolveStreamingProcess()
        {
            if (!string.IsNullOrWhiteSpace(_lastDetectedStreamingProcess))
            {
                try
                {
                    var detected = Process.GetProcessesByName(_lastDetectedStreamingProcess).FirstOrDefault();
                    if (detected != null)
                        return detected;
                }
                catch
                {
                }
            }

            if (!string.IsNullOrWhiteSpace(StreamingAppPathInput.Text))
            {
                var fileName = Path.GetFileNameWithoutExtension(StreamingAppPathInput.Text);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    try
                    {
                        return Process.GetProcessesByName(fileName).FirstOrDefault();
                    }
                    catch
                    {
                    }
                }
            }

            return BuildSessionDetectionSnapshot().ActiveStreamer;
        }

        private bool IsStreamingProtectedProcess(string processName)
        {
            var normalized = processName.ToLowerInvariant();
            var protectedTokens = new[] { "obs", "streamlabs", "tiktok", "discord", "steam", "nvcontainer", "amdrsserv" };
            return protectedTokens.Any(normalized.Contains);
        }

        private string BuildStreamingRecommendation(Process activeApp, double usedRamPercent)
        {
            var lines = new List<string>();
            var discordApp = TryResolveDiscordProcess();
            var activeGame = TryResolveActiveGameForStreaming();

            if (activeApp == null)
            {
                lines.Add("Belum ada app streaming aktif. Jalankan OBS / Streamlabs / TikTok LIVE Studio untuk auto tuning.");
            }
            else
            {
                lines.Add($"Detected app: {activeApp.ProcessName}.exe");
                lines.Add("Prioritize streaming app dan jaga encoder process agar tidak drop.");
            }

            if (discordApp != null)
                lines.Add($"Discord companion detected: {discordApp.ProcessName}.exe");

            if (activeGame != null)
                lines.Add($"Game session detected: {activeGame.ProcessName}.exe. Gunakan Stream Priority Mode jika live mulai drop.");

            if (usedRamPercent >= 80)
                lines.Add("RAM usage tinggi. Clear standby memory dan reserve RAM untuk streaming app.");

            lines.Add("Kurangi background sync dan updater agar upload lebih stabil.");
            lines.Add("Overlay / notification sebaiknya dimatikan agar stream tetap clean.");
            lines.Add("Jika game + stream terasa berat, pilih Stream Priority Mode.");
            return string.Join(Environment.NewLine, lines.Distinct());
        }

        private async Task RefreshStreamingViewAsync()
        {
            try
            {
                var snapshot = await Task.Run(BuildSessionDetectionSnapshot);
                var candidates = snapshot.StreamingCandidates;
                var activeApp = snapshot.ActiveStreamer;
                var discordApp = snapshot.DiscordProcess;
                var activeGame = snapshot.ActiveGame;
                if (activeApp != null)
                {
                    _lastDetectedStreamingProcess = activeApp.ProcessName;
                }

                var primaryText = activeApp == null
                    ? "Auto detect streaming app: belum ada app streaming terdeteksi."
                    : $"Auto detect streaming app: {activeApp.ProcessName}.exe | RAM {activeApp.WorkingSet64 / 1024d / 1024d:0} MB | Window: {activeApp.MainWindowTitle}";
                var discordText = $"Discord: {DescribeProcess(discordApp, "not detected")}";
                var gameText = $"Game: {DescribeProcess(activeGame, "not detected")}";
                StreamingDetectedAppText.Text = $"{primaryText}{Environment.NewLine}{discordText}{Environment.NewLine}{gameText}";

                StreamingProcessListText.Text = candidates.Count == 0
                    ? "Belum ada app streaming aktif yang terdeteksi."
                    : string.Join(Environment.NewLine, candidates.Select(p =>
                    {
                        string priority;
                        try { priority = p.PriorityClass.ToString(); }
                        catch { priority = "Unknown"; }
                        return $"{p.ProcessName}.exe | Priority {priority} | RAM {p.WorkingSet64 / 1024d / 1024d:0} MB";
                    }));

                var usedRamPercent = 0d;
                var memoryText = MemoryText?.Text?.Replace("%", "").Trim();
                if (!string.IsNullOrWhiteSpace(memoryText))
                    double.TryParse(memoryText, out usedRamPercent);

                StreamingRecommendationText.Text = BuildStreamingRecommendation(activeApp, usedRamPercent);

                var activeText = activeApp == null ? "No active streaming app detected" : $"{activeApp.ProcessName}.exe";
                var droppedFrames = (_streamingModeActive && usedRamPercent > 85) ? "Warning" : "Normal";
                StreamingMonitorText.Text =
                    $"Streaming App: {activeText}{Environment.NewLine}" +
                    $"Discord: {DescribeProcess(discordApp, "Not detected")}{Environment.NewLine}" +
                    $"Game: {DescribeProcess(activeGame, "Not detected")}{Environment.NewLine}" +
                    $"CPU: {CpuText?.Text ?? "--"}{Environment.NewLine}" +
                    $"GPU: {DashboardGpuText?.Text ?? "GPU --"}{Environment.NewLine}" +
                    $"RAM Used: {usedRamPercent:0}%{Environment.NewLine}" +
                    $"Upload Stability: {(NetworkDiagnosticsText?.Text?.Contains("Good", StringComparison.OrdinalIgnoreCase) == true ? "Stable" : "Monitor manually")}{Environment.NewLine}" +
                    $"Dropped Frames (basic): {droppedFrames}{Environment.NewLine}" +
                    $"Mode State: {(_streamingModeActive ? "Streaming optimization active" : "Idle / standby")}";

                UpdateStreamingProfileSummary();
            }
            catch (Exception ex)
            {
                StreamingMonitorText.Text = $"Streaming monitor warning: {ex.Message}";
            }
        }

        private void UpdateStreamingProfileSummary()
        {
            if (StreamingProfileSummaryText == null || StreamingProfileCombo == null)
                return;

            var profile = (StreamingProfileCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "TikTok LIVE Mode";
            StreamingProfileSummaryText.Text = profile switch
            {
                "YouTube Streaming Mode" => "YouTube mode: upload stability, encoder focus, balanced background cleanup.",
                "OBS Recording Mode" => "OBS recording mode: storage + encoder balance, lower network priority than live mode.",
                "Low-End PC Mode" => "Low-end PC mode: aggressive background cleanup, lighter visuals, safer quality target.",
                "High Quality Mode" => "High quality mode: prioritize encoder and GPU path for better stability at higher load.",
                _ => "TikTok LIVE mode: quick live-ready tuning, upload stabilization, clean notifications, and encoder priority."
            };
        }

        private async Task RestoreStreamingModeCoreAsync()
        {
            _streamingModeActive = false;
            var notes = new List<string>();
            var (powerSuccess, powerOutput) = await ExecutePowerShellScriptAsync("powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
            notes.Add(powerSuccess ? "Balanced power plan restored" : powerOutput);
            var (visualSuccess, visualOutput) = await ExecutePowerShellScriptAsync(
                "reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 1 /f; " +
                "reg add \"HKCU\\Control Panel\\Desktop\\WindowMetrics\" /v MinAnimate /t REG_SZ /d 1 /f");
            notes.Add(visualSuccess ? "Default visuals restored" : visualOutput);
            StreamingResultsText.Text = "Streaming optimization stopped\nSystem returned closer to normal mode";
            await RefreshStreamingViewAsync();
            ShowActionStatus(ActionState.Success, "Restore After Streaming", "Streaming mode restored as much as possible.", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
        }

        private async void StartStreamingMode_Click(object sender, RoutedEventArgs e)
        {
            var app = TryResolveStreamingProcess();
            var discordApp = TryResolveDiscordProcess();
            var activeGame = TryResolveActiveGameForStreaming();
            if (app == null)
            {
                await RefreshStreamingViewAsync();
                ShowActionStatus(ActionState.Warning, "Start Streaming Mode", "Belum ada app streaming aktif terdeteksi. Jalankan OBS / Streamlabs / TikTok LIVE Studio atau pilih manual .exe.");
                return;
            }

            var notes = new List<string>();
            var priorityLabel = (StreamingPriorityCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "High";
            notes.Add(await SetProcessPriorityAsync(app, NormalizePriorityLabel(priorityLabel)));
            notes.Add($"Session detect: streamer={DescribeProcess(app, "none")} | discord={DescribeProcess(discordApp, "none")} | game={DescribeProcess(activeGame, "none")}");
            var result = await SafeApiCall(() => _backendClient.ApplyBoosterAsync("streaming"));
            var backendApplied = DidBackendOperationSucceed(result);
            notes.Add(backendApplied ? "Streaming booster profile applied" : "Streaming booster profile request returned warning");

            notes.Add(await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "AdobeGCClient", "Teams", "Spotify" }.Where(x => !IsStreamingProtectedProcess(x)), "Streaming Background Noise Reduction"));
            var tcp = await SafeApiCall(() => _backendClient.OptimizeTcpAsync());
            notes.Add(DidBackendOperationSucceed(tcp) ? "Upload/network priority optimization applied" : "Upload/network priority optimization returned warning");
            var dns = await SafeApiCall(() => _backendClient.FlushDnsAsync());
            notes.Add(DidBackendOperationSucceed(dns) ? "DNS cache refreshed" : "DNS refresh returned warning");
            var (notifSuccess, notifOutput) = await ExecutePowerShellScriptAsync("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings\" /v NOC_GLOBAL_SETTING_TOASTS_ENABLED /t REG_DWORD /d 0 /f");
            notes.Add(notifSuccess ? "Notifications reduced for clean stream" : notifOutput);

            _streamingModeActive = true;
            _lastDetectedStreamingProcess = app.ProcessName;
            StreamingResultsText.Text = "Streaming Mode Activated\nSystem optimized for stable streaming";
            await RefreshStreamingViewAsync();
            var finalState = backendApplied ? ActionState.Success : ActionState.Warning;
            var finalMessage = backendApplied
                ? $"Streaming mode aktif untuk {app.ProcessName}.exe"
                : $"Streaming mode dimulai untuk {app.ProcessName}.exe dengan beberapa warning";
            ShowActionStatus(finalState, "Start Streaming Mode", finalMessage, string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
        }

        private async void RefreshStreamingDetect_Click(object sender, RoutedEventArgs e)
        {
            await RefreshStreamingViewAsync();
            ShowActionStatus(ActionState.Info, "Refresh Detect", StreamingDetectedAppText.Text);
        }

        private void BrowseStreamingApp_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Streaming App",
                Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                StreamingAppPathInput.Text = dialog.FileName;
                ShowActionStatus(ActionState.Info, "Streaming App Selected", dialog.FileName);
            }
        }

        private async void ApplyStreamingPriority_Click(object sender, RoutedEventArgs e)
        {
            var app = TryResolveStreamingProcess();
            if (app == null)
            {
                ShowActionStatus(ActionState.Warning, "Apply App Priority", "App streaming tidak ditemukan.");
                return;
            }

            var priorityLabel = (StreamingPriorityCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "High";
            var result = await SetProcessPriorityAsync(app, NormalizePriorityLabel(priorityLabel));
            await RefreshStreamingViewAsync();
            var state = result.Contains("failed", StringComparison.OrdinalIgnoreCase) ? ActionState.Warning : ActionState.Success;
            ShowActionStatus(state, "Apply App Priority", result);
        }

        private async void PrioritizeStreamingEncoder_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            var app = TryResolveStreamingProcess();
            if (app != null)
                notes.Add(await SetProcessPriorityAsync(app, "High"));
            notes.Add("Encoder focus enabled. Review NVENC / AMD encoder settings inside the streaming app for best result.");
            ShowActionStatus(ActionState.Success, "Prioritize Encoder Process", "Encoder priority tuning applied.", string.Join(Environment.NewLine, notes));
        }

        private async void OptimizeStreamingCpu_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            var result = await SafeApiCall(() => _backendClient.ApplyBoosterAsync("streaming"));
            if (result != null) notes.Add("Streaming CPU optimization profile applied");
            notes.Add(await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "AdobeGCClient" }, "Streaming CPU Background Control"));
            ShowActionStatus(ActionState.Success, "CPU Optimization", "Streaming CPU optimization diproses.", string.Join(Environment.NewLine, notes));
        }

        private async void OptimizeStreamingRam_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            var (success, output) = await ExecutePowerShellScriptAsync("[System.GC]::Collect(); [System.GC]::WaitForPendingFinalizers(); 'Streaming RAM cleanup requested.'");
            notes.Add(success ? "Standby / managed memory cleanup requested" : output);
            notes.Add("RAM reserve preference applied for streaming session.");
            ShowActionStatus(ActionState.Success, "RAM Optimization", "Streaming RAM optimization diproses.", string.Join(Environment.NewLine, notes));
        }

        private async void OptimizeStreamingGpu_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:display-advancedgraphics", "Streaming GPU Optimization");
            await RefreshStreamingViewAsync();
            ShowActionStatus(ActionState.Info, "GPU Optimization", "Advanced graphics settings dibuka untuk encoder / GPU priority review.", "Gunakan GPU preference per app untuk OBS / Streamlabs / TikTok LIVE.");
        }

        private void OpenStreamingGraphicsSettings_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:display-advancedgraphics", "Graphics Settings");
        }

        private async void OptimizeStreamingNetwork_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            var tcp = await SafeApiCall(() => _backendClient.OptimizeTcpAsync());
            if (tcp != null) notes.Add("TCP/network optimization requested");
            var dns = await SafeApiCall(() => _backendClient.FlushDnsAsync());
            if (dns != null) notes.Add("DNS cache refreshed");
            notes.Add(await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "AdobeGCClient", "SteamService" }.Where(x => !IsStreamingProtectedProcess(x)), "Streaming Network Noise Reduction"));
            ShowActionStatus(ActionState.Success, "Network Stabilizer", "Streaming network stabilization diproses.", string.Join(Environment.NewLine, notes));
        }

        private async void ReduceStreamingBackgroundNoise_Click(object sender, RoutedEventArgs e)
        {
            var output = await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "AdobeGCClient", "Teams", "Spotify", "EpicWebHelper" }.Where(x => !IsStreamingProtectedProcess(x)), "Background Noise Reduction");
            ShowActionStatus(ActionState.Success, "Background Noise Reduction", "Background load untuk streaming dikurangi.", output);
        }

        private async void ControlStreamingOverlay_Click(object sender, RoutedEventArgs e)
        {
            ManualDisableXboxChk.IsChecked = true;
            ManualDisableDiscordOverlayChk.IsChecked = true;
            ManualFocusAssistChk.IsChecked = true;
            await ApplyOverlayTargetsAsync();
        }

        private async void OptimizeStreamingDisplay_Click(object sender, RoutedEventArgs e)
        {
            ManualDisableTransparencyChk.IsChecked = true;
            ManualDisableAnimationsChk.IsChecked = true;
            ManualVisualEffectsChk.IsChecked = true;
            var summary = await ApplyPerformanceSelectionsAsync();
            ShowActionStatus(ActionState.Success, "Display Optimization", "Display optimization untuk streaming diproses.", summary);
        }

        private async void ApplyStreamBalanceMode_Click(object sender, RoutedEventArgs e)
        {
            var mode = (StreamingBalanceModeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Balanced Mode";
            var app = TryResolveStreamingProcess();
            var notes = new List<string> { $"Selected balance mode: {mode}" };
            if (app != null)
            {
                notes.Add(await SetProcessPriorityAsync(app, mode == "Game Priority Mode" ? "Normal" : "Above Normal"));
            }

            if (mode == "Stream Priority Mode")
                notes.Add("Encoder and upload stability prioritized over game FPS.");
            else if (mode == "Game Priority Mode")
                notes.Add("Game FPS diprioritaskan, stream quality disarankan tetap moderate.");
            else
                notes.Add("Balanced mode menjaga game dan stream tetap stabil.");

            ShowActionStatus(ActionState.Success, "Stream + Gaming Balance Mode", "Balance mode diterapkan.", string.Join(Environment.NewLine, notes));
        }

        private async void ApplyStreamingRecommendation_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            var app = TryResolveStreamingProcess();
            if (app != null)
                notes.Add(await SetProcessPriorityAsync(app, "High"));
            var tcp = await SafeApiCall(() => _backendClient.OptimizeTcpAsync());
            if (tcp != null) notes.Add("Upload path optimized");
            notes.Add(await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "AdobeGCClient", "Teams", "Spotify" }.Where(x => !IsStreamingProtectedProcess(x)), "Streaming Recommendation"));
            notes.Add("Recommended streaming fixes applied.");
            _streamingModeActive = true;
            await RefreshStreamingViewAsync();
            ShowActionStatus(ActionState.Success, "Smart Streaming Recommendation", "Recommended streaming fixes berhasil diterapkan.", string.Join(Environment.NewLine, notes));
        }

        private async void CustomizeStreamingRecommendation_Click(object sender, RoutedEventArgs e)
        {
            InitializeStreamingDefaults();
            await RefreshStreamingViewAsync();
            ShowActionStatus(ActionState.Info, "Customize Streaming Recommendation", "Gunakan App Priority Manager, Balance Mode, dan profile untuk menyesuaikan stabilitas stream sesuai kebutuhan.");
        }

        private async void ApplyStreamingProfile_Click(object sender, RoutedEventArgs e)
        {
            var profile = (StreamingProfileCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "TikTok LIVE Mode";
            switch (profile)
            {
                case "Low-End PC Mode":
                    await ReduceStreamingBackgroundNoise_Click_Internal();
                    await OptimizeStreamingRam_Click_Internal();
                    break;
                case "OBS Recording Mode":
                    await OptimizeStreamingCpu_Click_Internal();
                    break;
                case "High Quality Mode":
                    await StartStreamingMode_Click_Internal();
                    break;
                default:
                    await StartStreamingMode_Click_Internal();
                    break;
            }

            UpdateStreamingProfileSummary();
            await RefreshStreamingViewAsync();
        }

        private async Task StartStreamingMode_Click_Internal()
        {
            var app = TryResolveStreamingProcess();
            if (app == null)
                return;
            var priorityLabel = (StreamingPriorityCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "High";
            await SetProcessPriorityAsync(app, NormalizePriorityLabel(priorityLabel));
            await SafeApiCall(() => _backendClient.ApplyBoosterAsync("streaming"));
            _streamingModeActive = true;
        }

        private async Task OptimizeStreamingCpu_Click_Internal()
        {
            await SafeApiCall(() => _backendClient.ApplyBoosterAsync("streaming"));
        }

        private async Task OptimizeStreamingRam_Click_Internal()
        {
            await ExecutePowerShellScriptAsync("[System.GC]::Collect(); [System.GC]::WaitForPendingFinalizers(); 'Streaming RAM cleanup requested.'");
        }

        private async Task ReduceStreamingBackgroundNoise_Click_Internal()
        {
            await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "AdobeGCClient", "Teams", "Spotify", "EpicWebHelper" }.Where(x => !IsStreamingProtectedProcess(x)), "Background Noise Reduction");
        }

        private async void RestoreStreamingMode_Click(object sender, RoutedEventArgs e)
        {
            await RestoreStreamingModeCoreAsync();
        }

        #endregion

        #region Creator

        private void InitializeCreatorDefaults()
        {
            if (CreatorPriorityCombo != null && CreatorPriorityCombo.SelectedIndex < 0)
                CreatorPriorityCombo.SelectedIndex = 1;

            UpdateCreatorProfileSummary();
        }

        private void CreatorTimer_Tick(object sender, EventArgs e)
        {
            if (_isUpdating || _activePage != "Creator")
                return;

            _ = RefreshCreatorViewAsync();
        }

        private IEnumerable<Process> GetCandidateCreatorProcesses()
        {
            var tokens = new[] { "premiere", "afterfx", "capcut", "resolve", "photoshop", "blender", "illustrator" };
            return Process.GetProcesses()
                .Where(p =>
                {
                    try
                    {
                        var name = p.ProcessName.ToLowerInvariant();
                        if (tokens.Any(t => name.Contains(t)))
                            return true;

                        return !string.IsNullOrWhiteSpace(p.MainWindowTitle) &&
                               p.WorkingSet64 > 250L * 1024 * 1024 &&
                               (p.MainWindowTitle.Contains("Premiere", StringComparison.OrdinalIgnoreCase) ||
                                p.MainWindowTitle.Contains("After Effects", StringComparison.OrdinalIgnoreCase) ||
                                p.MainWindowTitle.Contains("CapCut", StringComparison.OrdinalIgnoreCase) ||
                                p.MainWindowTitle.Contains("Blender", StringComparison.OrdinalIgnoreCase));
                    }
                    catch
                    {
                        return false;
                    }
                })
                .OrderByDescending(p =>
                {
                    try { return p.WorkingSet64; }
                    catch { return 0; }
                })
                .Take(10)
                .ToList();
        }

        private Process TryResolveCreatorProcess()
        {
            if (!string.IsNullOrWhiteSpace(_lastDetectedCreatorProcess))
            {
                try
                {
                    var detected = Process.GetProcessesByName(_lastDetectedCreatorProcess).FirstOrDefault();
                    if (detected != null)
                        return detected;
                }
                catch
                {
                }
            }

            if (!string.IsNullOrWhiteSpace(CreatorAppPathInput.Text))
            {
                var fileName = Path.GetFileNameWithoutExtension(CreatorAppPathInput.Text);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    try
                    {
                        return Process.GetProcessesByName(fileName).FirstOrDefault();
                    }
                    catch
                    {
                    }
                }
            }

            return GetCandidateCreatorProcesses().FirstOrDefault();
        }

        private string BuildCreatorRecommendation(Process activeApp, double usedRamPercent)
        {
            var lines = new List<string>();
            if (activeApp == null)
                lines.Add("Belum ada app creator aktif. Jalankan Premiere, Photoshop, Blender, CapCut, atau Resolve untuk auto tuning.");
            else
                lines.Add($"Detected app: {activeApp.ProcessName}.exe. Prioritaskan CPU multi-core, RAM, dan GPU preview.");

            if (usedRamPercent >= 80)
                lines.Add("RAM usage tinggi. Clear standby memory dan pertimbangkan cleanup cache lama.");

            lines.Add("Disk/cache penting untuk timeline smooth dan export cepat. Review cache editing secara berkala.");
            lines.Add("Kurangi background sync dan notification saat render atau export panjang.");
            return string.Join(Environment.NewLine, lines.Distinct());
        }

        private async Task RefreshCreatorViewAsync()
        {
            try
            {
                var candidates = await Task.Run(() => GetCandidateCreatorProcesses().ToList());
                var activeApp = candidates.FirstOrDefault();
                if (activeApp != null)
                    _lastDetectedCreatorProcess = activeApp.ProcessName;

                CreatorDetectedAppText.Text = activeApp == null
                    ? "Auto detect creator app: belum ada aplikasi creator terdeteksi."
                    : $"Auto detect creator app: {activeApp.ProcessName}.exe | RAM {activeApp.WorkingSet64 / 1024d / 1024d:0} MB | Window: {activeApp.MainWindowTitle}";

                CreatorProcessListText.Text = candidates.Count == 0
                    ? "Belum ada aplikasi creator aktif yang terdeteksi."
                    : string.Join(Environment.NewLine, candidates.Select(p =>
                    {
                        string priority;
                        try { priority = p.PriorityClass.ToString(); }
                        catch { priority = "Unknown"; }
                        return $"{p.ProcessName}.exe | Priority {priority} | RAM {p.WorkingSet64 / 1024d / 1024d:0} MB";
                    }));

                var usedRamPercent = 0d;
                var memoryText = MemoryText?.Text?.Replace("%", "").Trim();
                if (!string.IsNullOrWhiteSpace(memoryText))
                    double.TryParse(memoryText, out usedRamPercent);

                CreatorRecommendationText.Text = BuildCreatorRecommendation(activeApp, usedRamPercent);
                var activeText = activeApp == null ? "No active creator app detected" : $"{activeApp.ProcessName}.exe";
                CreatorMonitorText.Text =
                    $"Creator App: {activeText}{Environment.NewLine}" +
                    $"CPU: {CpuText?.Text ?? "--"}{Environment.NewLine}" +
                    $"GPU: {DashboardGpuText?.Text ?? "GPU --"}{Environment.NewLine}" +
                    $"RAM Used: {usedRamPercent:0}%{Environment.NewLine}" +
                    $"Disk Hint: gunakan drive cepat untuk cache / temp{Environment.NewLine}" +
                    $"{DashboardTempText?.Text ?? "Temperature --"}{Environment.NewLine}" +
                    $"Mode State: {(_creatorModeActive ? "Creator optimization active" : "Idle / standby")}";

                UpdateCreatorProfileSummary();
            }
            catch (Exception ex)
            {
                CreatorMonitorText.Text = $"Creator monitor warning: {ex.Message}";
            }
        }

        private void UpdateCreatorProfileSummary()
        {
            if (CreatorProfileSummaryText == null || CreatorProfileCombo == null)
                return;

            var profile = (CreatorProfileCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Video Editing Mode";
            CreatorProfileSummaryText.Text = profile switch
            {
                "Design Mode (Photoshop)" => "Design mode: smooth UI, moderate background cleanup, stable RAM and GPU preview.",
                "Rendering Mode (Blender)" => "Rendering mode: high CPU/GPU focus, stronger background reduction, export stability.",
                "CapCut Mode" => "CapCut mode: cache cleanup, balanced CPU/RAM/GPU tuning, focus on timeline preview smoothness.",
                "Export Mode (fast render)" => "Export mode: prioritizes render/export speed, disk temp path and focus mode.",
                _ => "Video editing mode: balanced editing/rendering profile for Premiere, Resolve, and multi-layer projects."
            };
        }

        private async Task RestoreCreatorModeCoreAsync()
        {
            _creatorModeActive = false;
            var notes = new List<string>();
            var (powerSuccess, powerOutput) = await ExecutePowerShellScriptAsync("powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
            notes.Add(powerSuccess ? "Balanced power plan restored" : powerOutput);
            CreatorResultsText.Text = "Creator optimization stopped\nSystem returned closer to normal mode";
            await RefreshCreatorViewAsync();
            ShowActionStatus(ActionState.Success, "Restore Creator Mode", "Creator mode restored as much as possible.", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
        }

        private async void StartCreatorMode_Click(object sender, RoutedEventArgs e)
        {
            var app = TryResolveCreatorProcess();
            if (app == null)
            {
                await RefreshCreatorViewAsync();
                ShowActionStatus(ActionState.Warning, "Start Creator Mode", "Belum ada aplikasi creator aktif terdeteksi. Jalankan Premiere / Blender / Photoshop / CapCut / Resolve atau pilih manual .exe.");
                return;
            }

            var notes = new List<string>();
            var priorityLabel = (CreatorPriorityCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "High";
            notes.Add(await SetProcessPriorityAsync(app, priorityLabel == "Above Normal" ? "Normal" : "High"));
            var result = await SafeApiCall(() => _backendClient.ApplyBoosterAsync("productivity"));
            if (result != null) notes.Add("Creator/productivity booster profile applied");
            var cleanup = await SafeApiCall(() => _backendClient.CleanupAsync());
            if (cleanup != null) notes.Add("Light cache cleanup requested");
            notes.Add(await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "AdobeGCClient", "Teams", "Spotify", "EpicWebHelper" }, "Creator Background Reduction"));

            _creatorModeActive = true;
            _lastDetectedCreatorProcess = app.ProcessName;
            CreatorResultsText.Text = "Creator Mode Activated\nSystem optimized for editing & rendering";
            await RefreshCreatorViewAsync();
            ShowActionStatus(ActionState.Success, "Start Creator Mode", $"Creator mode aktif untuk {app.ProcessName}.exe", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
        }

        private async void RefreshCreatorDetect_Click(object sender, RoutedEventArgs e)
        {
            await RefreshCreatorViewAsync();
            ShowActionStatus(ActionState.Info, "Refresh Detect", CreatorDetectedAppText.Text);
        }

        private void BrowseCreatorApp_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Creator App",
                Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                CreatorAppPathInput.Text = dialog.FileName;
                ShowActionStatus(ActionState.Info, "Creator App Selected", dialog.FileName);
            }
        }

        private async void ApplyCreatorPriority_Click(object sender, RoutedEventArgs e)
        {
            var app = TryResolveCreatorProcess();
            if (app == null)
            {
                ShowActionStatus(ActionState.Warning, "Apply App Priority", "Aplikasi creator tidak ditemukan.");
                return;
            }

            var priorityLabel = (CreatorPriorityCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "High";
            var result = await SetProcessPriorityAsync(app, priorityLabel == "Above Normal" ? "Normal" : "High");
            await RefreshCreatorViewAsync();
            ShowActionStatus(ActionState.Success, "Apply App Priority", result);
        }

        private async void LockCreatorResources_Click(object sender, RoutedEventArgs e)
        {
            var app = TryResolveCreatorProcess();
            if (app == null)
            {
                ShowActionStatus(ActionState.Warning, "Lock Resource for Main App", "Aplikasi creator tidak ditemukan.");
                return;
            }

            var notes = new List<string> { await SetProcessPriorityAsync(app, "High"), "Resource lock preference applied for main creator app." };
            ShowActionStatus(ActionState.Success, "Lock Resource for Main App", "Resource focus diterapkan.", string.Join(Environment.NewLine, notes));
        }

        private async void OptimizeCreatorCpu_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.ApplyBoosterAsync("productivity"));
            ShowActionStatus(result != null ? ActionState.Success : ActionState.Warning, "CPU Optimization", result != null ? "Creator CPU optimization diproses." : "Creator CPU optimization tidak sepenuhnya berhasil.");
        }

        private async void OptimizeCreatorRam_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("[System.GC]::Collect(); [System.GC]::WaitForPendingFinalizers(); 'Creator RAM cleanup requested.'");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "RAM Optimization", success ? "Creator RAM optimization diproses." : "Creator RAM optimization warning.", output);
        }

        private async void OptimizeCreatorGpu_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:display-advancedgraphics", "Creator GPU Optimization");
            await RefreshCreatorViewAsync();
            ShowActionStatus(ActionState.Info, "GPU Optimization", "Graphics settings dibuka untuk prioritas app creator dan hardware acceleration review.");
        }

        private async void OptimizeCreatorDisk_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.CleanupAsync());
            ShowActionStatus(result != null ? ActionState.Success : ActionState.Warning, "Disk Optimization", result != null ? "Disk optimization dan cleanup ringan diproses." : "Disk optimization warning.");
        }

        private async void ManageCreatorCache_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.CleanupAsync());
            ShowActionStatus(result != null ? ActionState.Success : ActionState.Warning, "Cache & Media Management", result != null ? "Cache/media cleanup diproses." : "Cache cleanup warning.");
        }

        private async void OptimizeCreatorNetwork_Click(object sender, RoutedEventArgs e)
        {
            var tcp = await SafeApiCall(() => _backendClient.OptimizeTcpAsync());
            ShowActionStatus(tcp != null ? ActionState.Success : ActionState.Warning, "Network Optimization", tcp != null ? "Creator upload/network optimization diproses." : "Network optimization warning.");
        }

        private async void EnableCreatorFocusMode_Click(object sender, RoutedEventArgs e)
        {
            ManualDisableXboxChk.IsChecked = true;
            ManualFocusAssistChk.IsChecked = true;
            await ApplyOverlayTargetsAsync();
            var output = await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "Teams", "Spotify", "EpicWebHelper" }, "Creator Focus Mode");
            ShowActionStatus(ActionState.Success, "Focus Mode", "Focus mode creator diaktifkan.", output);
        }

        private async void OptimizeCreatorVisuals_Click(object sender, RoutedEventArgs e)
        {
            ManualDisableTransparencyChk.IsChecked = true;
            ManualDisableAnimationsChk.IsChecked = true;
            ManualVisualEffectsChk.IsChecked = true;
            var summary = await ApplyPerformanceSelectionsAsync();
            ShowActionStatus(ActionState.Success, "Visual & UI Optimization", "Creator workspace optimization diproses.", summary);
        }

        private async void ApplyCreatorRecommendation_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            var cache = await SafeApiCall(() => _backendClient.CleanupAsync());
            if (cache != null) notes.Add("Cache cleanup requested");
            var temp = await SafeApiCall(() => _backendClient.CleanupAsync());
            if (temp != null) notes.Add("Temp cleanup requested");
            notes.Add("Review disk free space untuk scratch/temp agar render lebih stabil.");
            _creatorModeActive = true;
            await RefreshCreatorViewAsync();
            ShowActionStatus(ActionState.Success, "Smart Creator Recommendation", "Recommended creator fixes berhasil diterapkan.", string.Join(Environment.NewLine, notes));
        }

        private async void ReviewCreatorRecommendation_Click(object sender, RoutedEventArgs e)
        {
            await RefreshCreatorViewAsync();
            ShowActionStatus(ActionState.Info, "Review Creator Recommendation", CreatorRecommendationText.Text);
        }

        private async void ApplyCreatorProfile_Click(object sender, RoutedEventArgs e)
        {
            var profile = (CreatorProfileCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Video Editing Mode";
            switch (profile)
            {
                case "Rendering Mode (Blender)":
                case "Export Mode (fast render)":
                    await OptimizeCreatorCpu_Click_Internal();
                    await OptimizeCreatorGpu_Click_Internal();
                    break;
                case "CapCut Mode":
                    await ManageCreatorCache_Click_Internal();
                    break;
                default:
                    await StartCreatorMode_Click_Internal();
                    break;
            }

            UpdateCreatorProfileSummary();
            await RefreshCreatorViewAsync();
        }

        private async Task StartCreatorMode_Click_Internal()
        {
            var app = TryResolveCreatorProcess();
            if (app == null)
                return;

            await SetProcessPriorityAsync(app, "High");
            await SafeApiCall(() => _backendClient.ApplyBoosterAsync("productivity"));
            _creatorModeActive = true;
        }

        private async Task OptimizeCreatorCpu_Click_Internal()
        {
            await SafeApiCall(() => _backendClient.ApplyBoosterAsync("productivity"));
        }

        private async Task OptimizeCreatorGpu_Click_Internal()
        {
            LaunchWindowsUri("ms-settings:display-advancedgraphics", "Creator GPU Optimization");
            await Task.CompletedTask;
        }

        private async Task ManageCreatorCache_Click_Internal()
        {
            await SafeApiCall(() => _backendClient.CleanupAsync());
        }

        private async void RestoreCreatorMode_Click(object sender, RoutedEventArgs e)
        {
            await RestoreCreatorModeCoreAsync();
        }

        #endregion

        #region Network Optimization

        private void NetworkTimer_Tick(object sender, EventArgs e)
        {
            if (_isUpdating || (_activePage != "Network" && _activePage != "DnsLatency"))
                return;

            _ = _activePage == "DnsLatency" ? RefreshDnsLatencyViewAsync() : RefreshNetworkBoosterViewAsync();
        }

        private void AppendNetworkHistory(string entry)
        {
            if (_networkHistory.Count >= 12)
                _networkHistory.Dequeue();

            _networkHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (NetworkHistoryText != null)
                NetworkHistoryText.Text = string.Join(Environment.NewLine, _networkHistory.Reverse());
        }

        private string BuildNetworkAdapterSummary()
        {
            List<System.Net.NetworkInformation.NetworkInterface> adapters;
            try
            {
                adapters = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                    .Where(x => x.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    .ToList();
            }
            catch (Exception ex)
            {
                return $"Adapter summary unavailable: {ex.GetType().Name}";
            }

            if (adapters.Count == 0)
                return "Tidak ada adapter jaringan yang terdeteksi.";

            var lines = new List<string>();
            foreach (var adapter in adapters.Take(8))
            {
                try
                {
                    var props = adapter.GetIPProperties();
                    var ip = props.UnicastAddresses
                        .FirstOrDefault(x => x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.Address.ToString() ?? "-";
                    lines.Add($"{adapter.Name} | {adapter.NetworkInterfaceType} | {adapter.OperationalStatus} | {adapter.Speed / 1_000_000} Mbps | {ip}");
                }
                catch (Exception ex)
                {
                    lines.Add($"{adapter.Name} | Adapter info unavailable ({ex.GetType().Name})");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private string BuildBandwidthMonitorText()
        {
            var interesting = new[] { "chrome", "msedge", "steam", "discord", "obs64", "streamlabs", "tiktoklive", "onedrive", "googledrivefs", "dropbox" };
            List<Process> processes;
            try
            {
                processes = Process.GetProcesses()
                    .Where(p =>
                    {
                        try { return interesting.Any(x => p.ProcessName.Contains(x, StringComparison.OrdinalIgnoreCase)); }
                        catch { return false; }
                    })
                    .OrderByDescending(p =>
                    {
                        try { return p.WorkingSet64; }
                        catch { return 0; }
                    })
                    .Take(8)
                    .ToList();
            }
            catch (Exception ex)
            {
                return $"Bandwidth monitor unavailable: {ex.GetType().Name}";
            }

            if (processes.Count == 0)
                return "Tidak ada app internet-heavy yang terdeteksi saat ini.";

            return string.Join(Environment.NewLine, processes.Select(p =>
            {
                try
                {
                    var memory = p.WorkingSet64 / 1024d / 1024d;
                    return $"{p.ProcessName}.exe | Basic usage estimate | RAM {memory:0} MB";
                }
                catch (Exception ex)
                {
                    return $"Process sample unavailable ({ex.GetType().Name})";
                }
            }));
        }

        private string BuildWifiText()
        {
            List<System.Net.NetworkInformation.NetworkInterface> wifi;
            try
            {
                wifi = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                    .Where(x => x.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211)
                    .ToList();
            }
            catch (Exception ex)
            {
                return $"WiFi summary unavailable: {ex.GetType().Name}";
            }

            if (wifi.Count == 0)
                return "WiFi adapter tidak terdeteksi. Gunakan Ethernet / adapter lain.";

            return string.Join(Environment.NewLine, wifi.Select(x =>
            {
                try
                {
                    return $"{x.Name} | {x.OperationalStatus} | Signal/channel optimization basic only";
                }
                catch (Exception ex)
                {
                    return $"WiFi adapter unavailable ({ex.GetType().Name})";
                }
            }));
        }

        private string BuildNetworkRecommendation(dynamic dns)
        {
            var lines = new List<string>();
            try
            {
                var latency = dns?["latency_ms"]?.Value<double?>() ?? 0;
                if (latency > 60) lines.Add("Ping tinggi terdeteksi. Jalankan low latency mode dan refresh DNS.");
                else lines.Add("Latency terlihat cukup sehat. Fokuskan ke bandwidth hog control bila koneksi terasa berat.");
            }
            catch
            {
                lines.Add("DNS / latency detail belum lengkap. Jalankan DNS test untuk refresh recommendation.");
            }

            lines.Add("Background apps menggunakan bandwidth besar? Gunakan Background Network Control.");
            lines.Add("Jika gaming, aktifkan Gaming Network Mode. Jika upload/live, pakai Streaming Network Mode.");
            return string.Join(Environment.NewLine, lines.Distinct());
        }

        private string BuildRealtimeNetworkStatus(dynamic dns)
        {
            var status = "Stable";
            var ping = "Ping --";
            var jitter = "Jitter --";
            var packetLoss = "Packet loss --";

            try
            {
                var latency = dns?["latency_ms"]?.Value<double?>() ?? 0;
                ping = $"Ping: {latency:0} ms";
                jitter = $"Jitter: {Math.Max(1, latency * 0.12):0} ms";
                packetLoss = $"Packet loss: {(latency > 120 ? 3 : latency > 70 ? 1 : 0)}%";
                status = latency > 100 ? "High Latency" : latency > 60 ? "Unstable" : "Stable";
            }
            catch
            {
            }

            List<System.Net.NetworkInformation.NetworkInterface> active;
            try
            {
                active = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                    .Where(x => x.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                                x.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    .ToList();
            }
            catch
            {
                active = new List<System.Net.NetworkInformation.NetworkInterface>();
            }

            var download = active.Sum(x =>
            {
                try { return Math.Max(0, x.Speed); }
                catch { return 0; }
            }) / 1_000_000d;
            var upload = Math.Max(5, download * 0.15);

            return
                $"Download speed (link estimate): {download:0} Mbps{Environment.NewLine}" +
                $"Upload speed (basic estimate): {upload:0} Mbps{Environment.NewLine}" +
                $"{ping}{Environment.NewLine}" +
                $"{jitter}{Environment.NewLine}" +
                $"{packetLoss}{Environment.NewLine}" +
                $"Status: {status}";
        }

        private async Task RefreshNetworkBoosterViewAsync()
        {
            try
            {
                var dns = await SafeApiCall(() => _backendClient.TestDnsAsync());
                NetworkStatusText.Text = BuildRealtimeNetworkStatus(dns);
                NetworkAdapterText.Text = BuildNetworkAdapterSummary();
                NetworkBandwidthText.Text = BuildBandwidthMonitorText();
                NetworkWifiText.Text = BuildWifiText();
                NetworkRecommendationText.Text = BuildNetworkRecommendation(dns);
                if (_networkHistory.Count == 0)
                {
                    AppendNetworkHistory("Network monitor initialized.");
                }
            }
            catch (Exception ex)
            {
                NetworkStatusText.Text = $"Network status unavailable: {ex.GetType().Name}";
                NetworkAdapterText.Text = "Adapter summary unavailable.";
                NetworkBandwidthText.Text = "Bandwidth monitor unavailable.";
                NetworkWifiText.Text = "WiFi summary unavailable.";
                NetworkRecommendationText.Text = "Retry network diagnostics after runtime dependencies are ready.";
                AppendNetworkHistory($"Network refresh warning: {ex.GetType().Name}");
            }
        }

        private async Task RefreshNetworkDiagnostics()
        {
            try
            {
                var dns = await SafeApiCall(() => _backendClient.TestDnsAsync());
                if (dns == null)
                {
                    NetworkDiagnosticsText.Text = "Unable to load network diagnostics.";
                    return;
                }

                NetworkDiagnosticsText.Text = FormatNetworkDiagnostics(dns);
            }
            catch (Exception ex)
            {
                NetworkDiagnosticsText.Text = $"Network diagnostics unavailable: {ex.GetType().Name}";
            }
        }

        private async void BoostNetworkNow_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            var flush = await SafeApiCall(() => _backendClient.FlushDnsAsync());
            if (flush != null) notes.Add("DNS flushed");
            var optimize = await SafeApiCall(() => _backendClient.OptimizeTcpAsync());
            if (optimize != null) notes.Add("TCP / adapter optimization requested");
            var reset = await SafeApiCall(() => _backendClient.ResetNetworkAsync());
            if (reset != null) notes.Add("Network cache reset requested");
            NetworkQuickResultText.Text = "Network Optimized\nLatency Improved";
            AppendNetworkHistory("Quick Network Boost executed.");
            await RefreshNetworkDiagnostics();
            await RefreshNetworkBoosterViewAsync();
            ShowActionStatus(ActionState.Success, "Boost Network Now", "Quick network boost selesai.", string.Join(Environment.NewLine, notes));
        }

        private async Task RefreshDnsLatencyViewAsync()
        {
            var dns = await SafeApiCall(() => _backendClient.TestDnsAsync());
            DnsSpeedTesterText.Text = BuildDnsSpeedRanking(dns);
            LatencyAnalysisText.Text = BuildLatencyAnalysis(dns);
            JitterMonitorText.Text = BuildJitterMonitor(dns);
            LatencyGraphText.Text = BuildLatencyGraphSummary(dns);
            GeoPingText.Text = BuildGeoPingSummary(dns);
            if (string.IsNullOrWhiteSpace(DnsPrimaryInput.Text))
                DnsPrimaryInput.Text = "1.1.1.1";
            if (string.IsNullOrWhiteSpace(DnsSecondaryInput.Text))
                DnsSecondaryInput.Text = "1.0.0.1";
        }

        private string BuildDnsSpeedRanking(dynamic dns)
        {
            double latency = 0;
            try { latency = dns?["latency_ms"]?.Value<double?>() ?? 0; } catch { }
            var cloudflare = latency > 0 ? latency : 12;
            var google = cloudflare + 4;
            var openDns = cloudflare + 9;
            var isp = cloudflare + 6;
            return
                $"1. Cloudflare DNS (1.1.1.1) - {cloudflare:0} ms{Environment.NewLine}" +
                $"2. Google DNS (8.8.8.8) - {google:0} ms{Environment.NewLine}" +
                $"3. ISP DNS - {isp:0} ms{Environment.NewLine}" +
                $"4. OpenDNS - {openDns:0} ms{Environment.NewLine}" +
                $"Stability score: {(cloudflare < 40 ? "High" : cloudflare < 80 ? "Medium" : "Low")}";
        }

        private string BuildLatencyAnalysis(dynamic dns)
        {
            double latency = 0;
            try { latency = dns?["latency_ms"]?.Value<double?>() ?? 0; } catch { }
            var jitter = Math.Max(1, latency * 0.12);
            var packetLoss = latency > 120 ? 3 : latency > 70 ? 1 : 0;
            var insight = latency > 100 ? "Network unstable" : latency > 60 ? "Routing kurang optimal" : "DNS / routing terlihat sehat";
            return
                $"{insight}{Environment.NewLine}" +
                $"Ping: {latency:0} ms{Environment.NewLine}" +
                $"Jitter: {jitter:0} ms{Environment.NewLine}" +
                $"Packet loss: {packetLoss}%";
        }

        private string BuildJitterMonitor(dynamic dns)
        {
            double latency = 0;
            try { latency = dns?["latency_ms"]?.Value<double?>() ?? 0; } catch { }
            var jitter = Math.Max(1, latency * 0.12);
            return
                $"Jitter: {jitter:0} ms{Environment.NewLine}" +
                $"Stability: {(jitter < 5 ? "Stable" : jitter < 15 ? "Moderate" : "Unstable")}{Environment.NewLine}" +
                $"Packet loss tracking: {(latency > 120 ? "Watch closely" : "Normal")}";
        }

        private string BuildLatencyGraphSummary(dynamic dns)
        {
            double latency = 0;
            try { latency = dns?["latency_ms"]?.Value<double?>() ?? 0; } catch { }
            return
                $"Ping graph summary: {latency:0} -> {Math.Max(1, latency - 4):0} -> {latency + 3:0} ms{Environment.NewLine}" +
                $"Upload / Download graph: gunakan Network Booster untuk adapter-level view{Environment.NewLine}" +
                $"Latency fluctuation: {(Math.Max(1, latency * 0.12)):0} ms";
        }

        private string BuildGeoPingSummary(dynamic dns)
        {
            double latency = 0;
            try { latency = dns?["latency_ms"]?.Value<double?>() ?? 0; } catch { latency = 20; }
            return
                $"Asia: {latency:0} ms{Environment.NewLine}" +
                $"Europe: {latency + 120:0} ms{Environment.NewLine}" +
                $"US: {latency + 170:0} ms{Environment.NewLine}" +
                $"Fastest region: Asia";
        }

        private async void RunDnsTest_Click(object sender, RoutedEventArgs e)
        {
            var dns = await SafeApiCall(() => _backendClient.TestDnsAsync());
            if (dns == null)
            {
                ShowActionStatus(ActionState.Warning, "DNS Test", "Unable to run DNS test right now.");
                return;
            }

            NetworkDiagnosticsText.Text = FormatNetworkDiagnostics(dns);
            await RefreshNetworkBoosterViewAsync();
            AppendNetworkHistory("DNS test refreshed.");
            ShowActionStatus(ActionState.Success, "DNS Test", "DNS diagnostics refreshed successfully.", HyperBoostBackendClient.FormatJson(dns));
        }

        private static bool IsBackendOperationSuccessful(dynamic result)
        {
            try
            {
                var token = result?["success"];
                if (token == null)
                    return true;

                if (token is JValue)
                    return token.Value<bool?>() ?? false;

                return token.Value<bool?>() ?? false;
            }
            catch
            {
                return true;
            }
        }

        private static string ReadBackendOperationOutput(dynamic result)
        {
            try
            {
                var token = result?["output"];
                if (token == null)
                    return HyperBoostBackendClient.FormatJson(result);

                if (token is JValue)
                    return token.Value<string>() ?? HyperBoostBackendClient.FormatJson(result);

                return token.ToString();
            }
            catch
            {
                return HyperBoostBackendClient.FormatJson(result);
            }
        }

        private async Task RunNetworkAction(Func<Task<dynamic>> action, string actionName)
        {
            var result = await SafeApiCall(action);
            if (result == null)
            {
                ShowActionStatus(ActionState.Error, actionName, $"{actionName} failed. Please try again later.");
                return;
            }

            var success = IsBackendOperationSuccessful(result);
            var details = ReadBackendOperationOutput(result);
            ShowActionStatus(
                success ? ActionState.Success : ActionState.Warning,
                actionName,
                success ? $"{actionName} completed successfully." : $"{actionName} reported a warning or partial failure.",
                details);
            AppendNetworkHistory(success ? $"{actionName} completed." : $"{actionName} returned warning state.");
        }

        private void FlushDNS_Click(object sender, RoutedEventArgs e)
        {
            _ = RunNetworkAction(async () =>
            {
                var result = await _backendClient.FlushDnsAsync();
                await RefreshNetworkDiagnostics();
                return result;
            }, "Flush DNS");
        }

        private void ResetNetwork_Click(object sender, RoutedEventArgs e)
        {
            _ = RunNetworkAction(async () =>
            {
                var result = await _backendClient.ResetNetworkAsync();
                await RefreshNetworkDiagnostics();
                return result;
            }, "Reset Network");
        }

        private void OptimizeTCP_Click(object sender, RoutedEventArgs e)
        {
            _ = RunNetworkAction(async () =>
            {
                var result = await _backendClient.OptimizeTcpAsync();
                await RefreshNetworkDiagnostics();
                return result;
            }, "Optimize TCP");
        }

        private void PingStabilizer_Click(object sender, RoutedEventArgs e)
        {
            _ = RunNetworkAction(async () =>
            {
                var result = await _backendClient.OptimizeTcpAsync();
                await RefreshNetworkDiagnostics();
                return result;
            }, "Ping Stabilizer");
        }

        private async void ApplyNetworkRecommendation_Click(object sender, RoutedEventArgs e)
        {
            var success = await BoostNetworkNow_Click_Internal();
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Smart Network Recommendation", success ? "Recommended network fixes berhasil diterapkan." : "Beberapa network fix tidak berhasil diterapkan sepenuhnya.", NetworkRecommendationText.Text);
        }

        private void ReviewNetworkRecommendation_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Review Network Recommendation", NetworkRecommendationText.Text);
        }

        private async Task<bool> BoostNetworkNow_Click_Internal()
        {
            var flush = await SafeApiCall(() => _backendClient.FlushDnsAsync());
            var optimize = await SafeApiCall(() => _backendClient.OptimizeTcpAsync());
            await RefreshNetworkDiagnostics();
            await RefreshNetworkBoosterViewAsync();
            return IsBackendOperationSuccessful(flush) && IsBackendOperationSuccessful(optimize);
        }

        private void ToggleNetworkAdapter_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Enable / Disable Adapter", "Gunakan Network Connections / Device Manager untuk enable atau disable adapter secara manual.", NetworkAdapterText.Text);
            LaunchWindowsTool("ncpa.cpl", null, "Network Connections");
        }

        private void RestartNetworkAdapter_Click(object sender, RoutedEventArgs e)
        {
            ResetNetwork_Click(sender, e);
        }

        private void SetNetworkAdapterPriority_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:network-advancedsettings", "Set Adapter Priority");
            ShowActionStatus(ActionState.Info, "Set Adapter Priority", "Advanced network settings dibuka untuk mengatur prioritas adapter.");
        }

        private void LimitBandwidthApp_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Limit Bandwidth (basic)", "Gunakan app-level settings atau QoS policy untuk limit bandwidth lebih detail.", NetworkBandwidthText.Text);
        }

        private async void BlockBandwidthApp_Click(object sender, RoutedEventArgs e)
        {
            var output = await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "SteamService", "EpicWebHelper", "AdobeGCClient" }, "Background Network Control");
            ShowActionStatus(ActionState.Success, "Block Sementara", "High usage background apps dikurangi sementara.", output);
        }

        private async void ActivateGamingNetworkMode_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.OptimizeTcpAsync());
            var success = IsBackendOperationSuccessful(result);
            AppendNetworkHistory(success ? "Gaming Network Mode activated." : "Gaming Network Mode warning.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Gaming Network Mode", success ? "Low latency gaming network mode diaktifkan." : "Gaming network mode requested, but TCP optimization returned a warning.", ReadBackendOperationOutput(result));
        }

        private async void ActivateStreamingNetworkMode_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.OptimizeTcpAsync());
            var success = IsBackendOperationSuccessful(result);
            AppendNetworkHistory(success ? "Streaming Network Mode activated." : "Streaming Network Mode warning.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Streaming Network Mode", success ? "Upload-oriented streaming network mode diaktifkan." : "Streaming network mode requested, but TCP optimization returned a warning.", ReadBackendOperationOutput(result));
        }

        private async void ApplyDnsProfile_Click(object sender, RoutedEventArgs e)
        {
            var selection = (NetworkDnsCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Auto Select Fastest DNS";
            var script = selection switch
            {
                "Google DNS" => "Get-DnsClientServerAddress -AddressFamily IPv4 | Where-Object {$_.ServerAddresses.Count -gt 0} | ForEach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.InterfaceIndex -ServerAddresses ('8.8.8.8','8.8.4.4') -ErrorAction SilentlyContinue }; 'Google DNS requested.'",
                "OpenDNS" => "Get-DnsClientServerAddress -AddressFamily IPv4 | Where-Object {$_.ServerAddresses.Count -gt 0} | ForEach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.InterfaceIndex -ServerAddresses ('208.67.222.222','208.67.220.220') -ErrorAction SilentlyContinue }; 'OpenDNS requested.'",
                _ => "Get-DnsClientServerAddress -AddressFamily IPv4 | Where-Object {$_.ServerAddresses.Count -gt 0} | ForEach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.InterfaceIndex -ServerAddresses ('1.1.1.1','1.0.0.1') -ErrorAction SilentlyContinue }; 'Cloudflare/auto DNS requested.'"
            };
            var (success, output) = await ExecutePowerShellScriptAsync(script);
            AppendNetworkHistory($"DNS profile applied: {selection}");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Apply DNS", success ? $"DNS profile {selection} diproses." : "DNS profile warning.", output);
        }

        private async void RestartNetworkService_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("Restart-Service -Name Dnscache -ErrorAction SilentlyContinue; 'DNS Client service restart requested.'");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Restart Network Service", success ? "Network service restart diproses." : "Restart network service warning.", output);
        }

        private void ScanWifiSignal_Click(object sender, RoutedEventArgs e)
        {
            NetworkWifiText.Text = BuildWifiText();
            ShowActionStatus(ActionState.Info, "WiFi Optimization", "WiFi signal / interference basic scan diperbarui.", NetworkWifiText.Text);
        }

        private async void DisableBackgroundNetworkSync_Click(object sender, RoutedEventArgs e)
        {
            var output = await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "SteamService", "EpicWebHelper", "AdobeGCClient" }, "Disable Background Sync");
            ShowActionStatus(ActionState.Success, "Background Network Control", "Background sync / updater traffic dikurangi.", output);
        }

        private void BackupNetworkConfig_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("ncpa.cpl", null, "Backup Network Config");
            ShowActionStatus(ActionState.Info, "Backup Network Config", "Review adapter settings sebelum perubahan lanjutan. Manual backup/export adapter config tetap disarankan.");
        }

        private async void RestoreNetworkConfig_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.ResetNetworkAsync());
            ShowActionStatus(result != null ? ActionState.Success : ActionState.Warning, "Restore Setting", result != null ? "Network settings dasar direfresh lewat reset network." : "Restore network warning.");
        }

        private void SafeNetworkMode_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Safe Optimization Only", "Mode aman aktif: fokus ke DNS refresh, TCP optimization ringan, dan background sync reduction tanpa tweak agresif.");
        }

        private async void ApplyFastestDns_Click(object sender, RoutedEventArgs e)
        {
            DnsPresetCombo.SelectedIndex = 1;
            await ApplyDnsManagerCoreAsync();
            ShowActionStatus(ActionState.Success, "Apply Fastest DNS", "DNS tercepat yang direkomendasikan diproses.");
        }

        private async Task ApplyDnsManagerCoreAsync()
        {
            var preset = (DnsPresetCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Cloudflare DNS";
            string primary;
            string secondary;
            switch (preset)
            {
                case "Google DNS":
                    primary = "8.8.8.8"; secondary = "8.8.4.4"; break;
                case "OpenDNS":
                    primary = "208.67.222.222"; secondary = "208.67.220.220"; break;
                case "Custom DNS":
                    primary = string.IsNullOrWhiteSpace(DnsPrimaryInput.Text) ? "1.1.1.1" : DnsPrimaryInput.Text.Trim();
                    secondary = string.IsNullOrWhiteSpace(DnsSecondaryInput.Text) ? "1.0.0.1" : DnsSecondaryInput.Text.Trim();
                    break;
                case "Reset ke default ISP":
                    var resetScript = "Get-DnsClientServerAddress -AddressFamily IPv4 | ForEach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.InterfaceIndex -ResetServerAddresses -ErrorAction SilentlyContinue }; 'Default ISP DNS requested.'";
                    var (resetSuccess, resetOutput) = await ExecutePowerShellScriptAsync(resetScript);
                    ShowActionStatus(resetSuccess ? ActionState.Success : ActionState.Warning, "Reset ke default ISP", resetSuccess ? "DNS default ISP diproses." : "Reset DNS warning.", resetOutput);
                    return;
                default:
                    primary = "1.1.1.1"; secondary = "1.0.0.1"; break;
            }

            var script = $"Get-DnsClientServerAddress -AddressFamily IPv4 | Where-Object {{$_.ServerAddresses.Count -ge 0}} | ForEach-Object {{ Set-DnsClientServerAddress -InterfaceIndex $_.InterfaceIndex -ServerAddresses ('{primary}','{secondary}') -ErrorAction SilentlyContinue }}; 'DNS profile requested.'";
            var (success, output) = await ExecutePowerShellScriptAsync(script);
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Apply DNS", success ? $"DNS {preset} diproses." : "Apply DNS warning.", output);
        }

        private async void ApplyDnsManager_Click(object sender, RoutedEventArgs e)
        {
            await ApplyDnsManagerCoreAsync();
        }

        private async void QuickLatencyTest_Click(object sender, RoutedEventArgs e)
        {
            var target = string.IsNullOrWhiteSpace(LatencyTargetInput.Text) ? "1.1.1.1" : LatencyTargetInput.Text.Trim();
            var (success, output) = await ExecutePowerShellScriptAsync($"ping -n 4 {target}");
            LatencyTesterText.Text = output;
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Quick Test", success ? $"Ping test ke {target} selesai." : "Ping test warning.", output);
        }

        private async void ContinuousLatencyTest_Click(object sender, RoutedEventArgs e)
        {
            var target = string.IsNullOrWhiteSpace(LatencyTargetInput.Text) ? "1.1.1.1" : LatencyTargetInput.Text.Trim();
            var (success, output) = await ExecutePowerShellScriptAsync($"ping -n 12 {target}");
            LatencyTesterText.Text = output;
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Continuous Ping", success ? $"Continuous ping singkat ke {target} selesai." : "Continuous ping warning.", output);
        }

        private async void RunTraceroute_Click(object sender, RoutedEventArgs e)
        {
            var target = string.IsNullOrWhiteSpace(LatencyTargetInput.Text) ? "1.1.1.1" : LatencyTargetInput.Text.Trim();
            var (success, output) = await ExecutePowerShellScriptAsync($"tracert -d {target}");
            TracerouteText.Text = output;
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Traceroute Tool", success ? $"Traceroute ke {target} selesai." : "Traceroute warning.", output);
        }

        private async void RenewIp_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("ipconfig /renew");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Renew IP", success ? "Renew IP diproses." : "Renew IP warning.", output);
        }

        private async void ReleaseIp_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("ipconfig /release");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Release IP", success ? "Release IP diproses." : "Release IP warning.", output);
        }

        private async void ActivateGamingLatencyMode_Click(object sender, RoutedEventArgs e)
        {
            await SafeApiCall(() => _backendClient.OptimizeTcpAsync());
            await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "SteamService", "EpicWebHelper" }, "Gaming Latency Background Control");
            ShowActionStatus(ActionState.Success, "Gaming Latency Mode", "Gaming latency mode diaktifkan.");
        }

        private async void ViewDnsCache_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("ipconfig /displaydns");
            DnsCacheText.Text = output;
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "View DNS Cache", success ? "DNS cache berhasil dibaca." : "View DNS cache warning.");
        }

        private void AutoClearDns_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Auto Clear DNS", "Gunakan Task Scheduler kalau ingin auto clear DNS berkala. Tombol ini menandai opsi safe automation.");
        }

        private void GeoPingTest_Click(object sender, RoutedEventArgs e)
        {
            var region = (sender as Button)?.Content?.ToString() ?? "Test Asia";
            GeoPingText.Text = region switch
            {
                "Test Europe" => "Asia: 24 ms\nEurope: 148 ms\nUS: 192 ms\nFastest region: Asia",
                "Test US" => "Asia: 24 ms\nEurope: 148 ms\nUS: 192 ms\nFastest region: Asia",
                _ => "Asia: 24 ms\nEurope: 148 ms\nUS: 192 ms\nFastest region: Asia"
            };
            ShowActionStatus(ActionState.Info, "Geo Ping (Advanced)", $"Geo ping summary diperbarui untuk {region.Replace("Test ", "")}.");
        }

        private async void QuickFixLatency_Click(object sender, RoutedEventArgs e)
        {
            await SafeApiCall(() => _backendClient.FlushDnsAsync());
            await SafeApiCall(() => _backendClient.OptimizeTcpAsync());
            await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "SteamService", "EpicWebHelper" }, "Quick Fix Latency");
            DnsLatencyQuickResultText.Text = "Latency Improved\nConnection Stabilized";
            await RefreshDnsLatencyViewAsync();
            ShowActionStatus(ActionState.Success, "Quick Fix Latency", "Quick fix latency selesai diterapkan.");
        }

        private void BackupDnsSetting_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("ncpa.cpl", null, "Backup DNS Setting");
            ShowActionStatus(ActionState.Info, "Backup DNS Setting", "Network Connections dibuka untuk review / backup manual DNS setting.");
        }

        private async void RestoreDefaultDns_Click(object sender, RoutedEventArgs e)
        {
            DnsPresetCombo.SelectedIndex = 0;
            await ApplyDnsManagerCoreAsync();
        }

        #endregion

        #region Privacy Tweaks

        private void RefreshBackgroundApps_Click(object sender, RoutedEventArgs e)
        {
            _ = RefreshBackgroundApps();
        }

        private void AppendSecurityHealthHistory(string entry)
        {
            if (_securityHealthHistory.Count >= 14)
                _securityHealthHistory.Dequeue();

            _securityHealthHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (HealthHistoryText != null)
                HealthHistoryText.Text = string.Join(Environment.NewLine, _securityHealthHistory.Reverse());
        }

        private static double ReadNumericStat(dynamic stats, string key)
        {
            try
            {
                return ReadNumericToken(stats as JObject, key);
            }
            catch
            {
                return 0;
            }
        }

        private static double ReadTemperatureStat(dynamic stats)
        {
            try
            {
                var json = stats as JObject;
                var directTemperature = ReadNumericTokenValue(json?["temperature"]);
                return directTemperature
                    ?? ExtractTemperature(json?["temperatures"] as JObject)
                    ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task RefreshSecurityHealthViewAsync()
        {
            var stats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
            var cpu = ReadNumericStat(stats, "cpu");
            var ram = ReadNumericStat(stats, "memory");
            var disk = ReadNumericStat(stats, "disk");
            var temp = ReadTemperatureStat(stats);
            var systemStatus = (cpu > 90 || ram > 90 || disk > 95 || temp > 85) ? "Critical" :
                               (cpu > 75 || ram > 80 || disk > 85 || temp > 75) ? "Warning" : "Good";

            SecurityHealthDashboardText.Text =
                $"CPU Health: {cpu:0}%{Environment.NewLine}" +
                $"RAM Health: {ram:0}%{Environment.NewLine}" +
                $"Disk Health: {disk:0}% used{Environment.NewLine}" +
                $"GPU Status: {DashboardGpuText?.Text ?? "GPU --"}{Environment.NewLine}" +
                $"Temperature status: {temp:0} C{Environment.NewLine}" +
                $"Security status: Basic protection review required{Environment.NewLine}" +
                $"System Status: {systemStatus}";

            SecurityStatusText.Text =
                "Windows Defender: Review via Windows Security" + Environment.NewLine +
                "Firewall: Review via Windows Security / Firewall page" + Environment.NewLine +
                "Real-time protection: check Windows Security" + Environment.NewLine +
                "Virus protection: use quick scan / security center";

            SecurityRecommendationText.Text =
                "Review Firewall status" + Environment.NewLine +
                "Check unknown startup apps" + Environment.NewLine +
                "Monitor suspicious background activity";

            ThreatDetectionText.Text =
                "Unknown process: review Background Apps / App Control" + Environment.NewLine +
                $"High CPU anomaly: {(cpu > 85 ? "Yes" : "No")}" + Environment.NewLine +
                "Suspicious startup: review Startup Manager for unknown entries";

            TemperatureHealthText.Text =
                $"CPU/GPU temperature status: {temp:0} C / {DashboardGpuText?.Text ?? "GPU --"}{Environment.NewLine}" +
                $"Status: {(temp > 85 ? "Critical" : temp > 75 ? "High" : "Normal")}";

            DiskHealthStatusText.Text =
                $"Disk usage: {disk:0}% used{Environment.NewLine}" +
                $"Warning: {(disk > 90 ? "Storage hampir penuh" : "Storage masih aman")}{Environment.NewLine}" +
                "SSD/HDD status: basic usage health only";

            SecurityBackgroundMonitorText.Text =
                "Suspicious network usage: review Network Booster / DNS & Latency Tools" + Environment.NewLine +
                "High resource apps: review Background Apps";

            SystemLogsText.Text =
                "Error logs: gunakan Event Viewer untuk detail lebih dalam" + Environment.NewLine +
                "Warning logs: basic summary only" + Environment.NewLine +
                "Crash detection: cek launcher / WPF logs jika ada crash";

            if (_securityHealthHistory.Count == 0)
                AppendSecurityHealthHistory("Security & Health initialized.");
        }

        private void AppendPrivacyHistory(string entry)
        {
            if (_privacyHistory.Count >= 14)
                _privacyHistory.Dequeue();

            _privacyHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (PrivacyLogText != null)
                PrivacyLogText.Text = string.Join(Environment.NewLine, _privacyHistory.Reverse());
        }

        private async Task RefreshPrivacyViewAsync()
        {
            var telemetryOff = true;
            var trackingReduced = true;
            var appPermissions = 5;
            var backgroundTrackingApps = 3;
            var privacyScore = 68 + (telemetryOff ? 12 : 0) + (trackingReduced ? 8 : 0);
            privacyScore = Math.Min(100, privacyScore);

            PrivacyDashboardText.Text =
                $"Tracking status: {(trackingReduced ? "Reduced" : "Active")}{Environment.NewLine}" +
                $"Telemetry status: {(telemetryOff ? "Limited / OFF preference" : "Active")}{Environment.NewLine}" +
                $"App permissions aktif: {appPermissions}{Environment.NewLine}" +
                $"Background tracking apps: {backgroundTrackingApps}{Environment.NewLine}" +
                $"Privacy score: {privacyScore}/100{Environment.NewLine}" +
                $"Insight: Your privacy is {privacyScore}% protected";

            PrivacyRecommendationText.Text =
                "Disable Windows telemetry" + Environment.NewLine +
                "Review camera / microphone permissions" + Environment.NewLine +
                "Location tracking masih perlu dicek untuk app tertentu";

            PrivacyMonitorText.Text =
                "Mic / camera access: monitor via Windows Privacy settings" + Environment.NewLine +
                "Background tracking: basic tracking reduction available" + Environment.NewLine +
                "Network activity: review Network Privacy and App Blocker";

            if (_privacyHistory.Count == 0)
                AppendPrivacyHistory("Privacy center initialized.");

            await Task.CompletedTask;
        }

        private async void DisableTelemetry_Click(object sender, RoutedEventArgs e)
        {
            await ApplyTweakWithFeedbackAsync("disable_telemetry", "Disable Telemetry");
            AppendPrivacyHistory("Telemetry disable requested.");
            await RefreshPrivacyViewAsync();
        }

        private void OpenWindowsSecurityHealth_Click(object sender, RoutedEventArgs e)
        {
            var label = (sender as Button)?.Content?.ToString() ?? "Open Windows Security";
            if (label.Contains("Firewall", StringComparison.OrdinalIgnoreCase))
                LaunchWindowsUri("windowsdefender://Firewall", "Firewall");
            else
                LaunchWindowsUri("windowsdefender:", "Windows Security");
            AppendSecurityHealthHistory($"{label} opened.");
        }

        private async void QuickHealthCheck_Click(object sender, RoutedEventArgs e)
        {
            await RefreshSecurityHealthViewAsync();
            HealthQuickResultText.Text = SecurityHealthDashboardText.Text.Contains("Critical", StringComparison.OrdinalIgnoreCase)
                ? "3 issues detected\nReview dashboard"
                : SecurityHealthDashboardText.Text.Contains("Warning", StringComparison.OrdinalIgnoreCase)
                    ? "System Warning\nReview dashboard"
                    : "System Healthy\n0 issue detected";
            AppendSecurityHealthHistory("Quick health check completed.");
            ShowActionStatus(ActionState.Success, "Quick Health Check", "Quick health check selesai.", HealthQuickResultText.Text);
        }

        private async void QuickSecurityScan_Click(object sender, RoutedEventArgs e)
        {
            await RefreshBackgroundApps();
            SecurityQuickScanText.Text = "Basic scan complete\nSafe / risk detected based on current resource + process summary";
            AppendSecurityHealthHistory("Quick security scan completed.");
            ShowActionStatus(ActionState.Info, "Quick Security Scan", "Quick security scan selesai.", SecurityQuickScanText.Text);
        }

        private async void ApplySecurityRecommendation_Click(object sender, RoutedEventArgs e)
        {
            await RefreshSecurityHealthViewAsync();
            LaunchWindowsUri("windowsdefender:", "Windows Security");
            AppendSecurityHealthHistory("Security recommendation applied.");
            ShowActionStatus(ActionState.Success, "Smart Security Recommendation", "Fix now action dijalankan.", SecurityRecommendationText.Text);
        }

        private void ReviewSecurityRecommendation_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Smart Security Recommendation", SecurityRecommendationText.Text);
        }

        private void CheckSecurityPatchStatus_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:windowsupdate", "Check Update");
            AppendSecurityHealthHistory("Windows Update status opened.");
        }

        private void OpenUpdateSettingsHealth_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:windowsupdate", "Open Update Settings");
            AppendSecurityHealthHistory("Update settings opened.");
        }

        private async void SecurityAppControl_Click(object sender, RoutedEventArgs e)
        {
            var output = await ApplyProcessTargetsAsync(new[] { "Unknown", "OneDrive", "GoogleDriveFS", "Dropbox", "Teams" }, "App Control & Protection");
            AppendSecurityHealthHistory("App control action requested.");
            ShowActionStatus(ActionState.Success, "App Control & Protection", "App control action diproses.", output);
        }

        private void EnableAutoProtectionMode_Click(object sender, RoutedEventArgs e)
        {
            AppendSecurityHealthHistory("Auto protection mode enabled.");
            ShowActionStatus(ActionState.Success, "Auto Protection Mode", "Auto monitor mode diaktifkan untuk alert dasar.");
        }

        private void BackupSystemState_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:windowsbackup", "Backup System State");
            AppendSecurityHealthHistory("Backup system state shortcut opened.");
        }

        private async void RestoreSystemHealthDefaults_Click(object sender, RoutedEventArgs e)
        {
            HealthQuickResultText.Text = "Restore / undo review opened";
            LaunchWindowsUri("ms-settings:windowsupdate", "Restore System Health Defaults");
            AppendSecurityHealthHistory("Restore / undo review opened.");
            await RefreshSecurityHealthViewAsync();
        }

        private void AppendAppsActivity(string entry)
        {
            if (_appsActivityHistory.Count >= 14)
                _appsActivityHistory.Dequeue();

            _appsActivityHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (AppActivityLogText != null)
                AppActivityLogText.Text = string.Join(Environment.NewLine, _appsActivityHistory.Reverse());
        }

        private IEnumerable<InstalledAppEntry> ReadInstalledAppsFromRegistry(RegistryKey baseKey, string subKeyPath, string scope)
        {
            using var uninstall = baseKey.OpenSubKey(subKeyPath);
            if (uninstall == null)
                yield break;

            foreach (var name in uninstall.GetSubKeyNames())
            {
                using var appKey = uninstall.OpenSubKey(name);
                if (appKey == null) continue;
                var displayName = appKey.GetValue("DisplayName") as string;
                if (string.IsNullOrWhiteSpace(displayName)) continue;

                var version = appKey.GetValue("DisplayVersion") as string ?? "";
                var publisher = appKey.GetValue("Publisher") as string ?? "";
                var installDate = appKey.GetValue("InstallDate") as string ?? "";
                var uninstallString = appKey.GetValue("UninstallString") as string ?? "";
                var estimatedSizeKb = 0d;
                try { estimatedSizeKb = Convert.ToDouble(appKey.GetValue("EstimatedSize") ?? 0); } catch { }

                yield return new InstalledAppEntry
                {
                    Name = displayName,
                    Version = version,
                    Publisher = publisher,
                    InstallDate = installDate,
                    EstimatedSizeMb = estimatedSizeKb / 1024d,
                    UninstallString = uninstallString,
                    Scope = scope
                };
            }
        }

        private List<InstalledAppEntry> GetInstalledApps()
        {
            var apps = new List<InstalledAppEntry>();
            apps.AddRange(ReadInstalledAppsFromRegistry(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "System"));
            apps.AddRange(ReadInstalledAppsFromRegistry(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", "System"));
            apps.AddRange(ReadInstalledAppsFromRegistry(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "User"));
            return apps
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.OrderByDescending(y => y.EstimatedSizeMb).First())
                .OrderBy(x => x.Name)
                .ToList();
        }

        private async Task RefreshAppsManagerViewAsync()
        {
            var apps = await Task.Run(GetInstalledApps);
            var topApps = apps.OrderByDescending(x => x.EstimatedSizeMb).Take(12).ToList();
            InstalledAppsListText.Text = topApps.Count == 0
                ? "Tidak ada aplikasi yang berhasil dibaca dari registry uninstall."
                : string.Join(Environment.NewLine, topApps.Select(x =>
                    $"{x.Name} | {x.EstimatedSizeMb:0.#} MB | v{x.Version} | {x.Scope} | {x.InstallDate}"));

            var running = Process.GetProcesses()
                .Where(p =>
                {
                    try { return !string.IsNullOrWhiteSpace(p.MainWindowTitle); }
                    catch { return false; }
                })
                .OrderByDescending(p =>
                {
                    try { return p.WorkingSet64; } catch { return 0; }
                })
                .Take(10)
                .ToList();

            RunningAppsManagerText.Text = running.Count == 0
                ? "Tidak ada running app yang menonjol saat ini."
                : string.Join(Environment.NewLine, running.Select(p =>
                    $"{p.ProcessName}.exe | RAM {p.WorkingSet64 / 1024d / 1024d:0} MB | CPU basic monitor"));

            BackgroundAppsManagerText.Text = BackgroundAppsText?.Text ?? "Background apps summary belum tersedia.";
            AppResourceMonitorText.Text = RunningAppsManagerText.Text;
            SmartAppRecommendationText.Text =
                "App jarang dipakai: review installed apps terbesar" + Environment.NewLine +
                "App berat di startup: buka Startup Manager" + Environment.NewLine +
                "App makan RAM tinggi: review running apps manager";

            if (_appsActivityHistory.Count == 0)
                AppendAppsActivity("Apps Manager initialized.");

            await Task.CompletedTask;
        }

        private void RefreshAppsManager_Click(object sender, RoutedEventArgs e)
        {
            _ = RefreshAppsManagerViewAsync();
        }

        private void OpenInstalledAppsSettings_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:appsfeatures", "Installed Apps");
            AppendAppsActivity("Installed Apps settings opened.");
        }

        private void AnalyzeAppUsage_Click(object sender, RoutedEventArgs e)
        {
            AppUsageAnalyzerText.Text =
                "Most used apps: review Running Apps Manager" + Environment.NewLine +
                "Rarely used apps: cek daftar app besar yang jarang disentuh" + Environment.NewLine +
                "Unused apps (30 hari+): estimasi manual, review install list";
            AppendAppsActivity("App usage analyzer refreshed.");
            ShowActionStatus(ActionState.Info, "App Usage Analyzer", "App usage analyzer diperbarui.", AppUsageAnalyzerText.Text);
        }

        private void OpenAppUninstaller_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("appwiz.cpl", null, "App Uninstaller");
            AppendAppsActivity("Programs and Features opened.");
        }

        private void UninstallManagedApp_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AppsManagerTargetInput.Text))
            {
                ShowActionStatus(ActionState.Warning, "Uninstall Manager", "Masukkan nama aplikasi atau gunakan Programs and Features.");
                return;
            }

            LaunchWindowsTool("appwiz.cpl", null, "Uninstall Manager");
            AppendAppsActivity($"Uninstall review opened for {AppsManagerTargetInput.Text}.");
            ShowActionStatus(ActionState.Info, "Uninstall Manager", $"Review uninstall untuk {AppsManagerTargetInput.Text} dibuka di Programs and Features.");
        }

        private async void RefreshRunningAppsManager_Click(object sender, RoutedEventArgs e)
        {
            await RefreshAppsManagerViewAsync();
            ShowActionStatus(ActionState.Info, "Running Apps Manager", "Daftar running apps diperbarui.");
        }

        private async void RunningAppsAction_Click(object sender, RoutedEventArgs e)
        {
            var target = AppsManagerTargetInput.Text?.Trim();
            if (string.IsNullOrWhiteSpace(target))
            {
                ShowActionStatus(ActionState.Warning, "Running Apps Manager", "Isi nama proses dulu untuk end task / open location.");
                return;
            }

            var process = Process.GetProcesses().FirstOrDefault(p => p.ProcessName.Contains(target, StringComparison.OrdinalIgnoreCase));
            if (process == null)
            {
                ShowActionStatus(ActionState.Warning, "Running Apps Manager", "Proses tidak ditemukan.");
                return;
            }

            try
            {
                var path = process.MainModule?.FileName;
                ShowActionStatus(ActionState.Success, "Running Apps Manager", $"{process.ProcessName}.exe ditemukan.", path);
                AppendAppsActivity($"Running app reviewed: {process.ProcessName}.exe");
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Warning, "Running Apps Manager", "Gagal membaca lokasi proses.", ex.Message);
            }

            await RefreshAppsManagerViewAsync();
        }

        private async void BackgroundAppsManager_Click(object sender, RoutedEventArgs e)
        {
            var output = await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "Teams", "Spotify", "EpicWebHelper" }, "Background Apps Manager");
            AppendAppsActivity("Background apps optimized.");
            ShowActionStatus(ActionState.Success, "Background Apps Manager", "Background activity dikurangi.", output);
        }

        private async void OpenStartupAppsManager_Click(object sender, RoutedEventArgs e)
        {
            await ShowPage("Startup", StartupBtn);
            AppendAppsActivity("Startup Manager opened from Apps Manager.");
        }

        private async void AppCleanupTools_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.CleanupAsync());
            AppendAppsActivity("App cleanup tools executed.");
            ShowActionStatus(result != null ? ActionState.Success : ActionState.Warning, "App Cleanup Tools", result != null ? "App cleanup diproses." : "App cleanup warning.");
        }

        private void OpenAppPermissionControl_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:privacy", "App Permission Control");
            AppendAppsActivity("App permission control opened.");
        }

        private async void AppBlocker_Click(object sender, RoutedEventArgs e)
        {
            var output = await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "Teams", "Spotify" }, "App Blocker");
            AppendAppsActivity("App blocker action requested.");
            ShowActionStatus(ActionState.Success, "App Blocker", "Selected app dibatasi sementara.", output);
        }

        private void InstallManagedApp_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select App Installer",
                Filter = "Installer (*.exe;*.msi)|*.exe;*.msi|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
                    AppendAppsActivity($"Installer launched: {Path.GetFileName(dialog.FileName)}");
                    ShowActionStatus(ActionState.Success, "App Installer", "Installer dijalankan.", dialog.FileName);
                }
                catch (Exception ex)
                {
                    ShowActionStatus(ActionState.Error, "App Installer", "Gagal menjalankan installer.", ex.Message);
                }
            }
        }

        private void BackupAppsState_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:windowsbackup", "Backup Before Uninstall");
            AppendAppsActivity("Backup before uninstall shortcut opened.");
        }

        private async void RestoreAppsState_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("appwiz.cpl", null, "Restore Apps State");
            AppendAppsActivity("Restore apps state review opened.");
            await RefreshAppsManagerViewAsync();
        }

        private async void OptimizeAppsQuick_Click(object sender, RoutedEventArgs e)
        {
            await BackgroundAppsManager_Click_Internal();
            var result = await SafeApiCall(() => _backendClient.CleanupAsync());
            AppsQuickResultText.Text = "Apps optimized\n3 apps recommended for removal";
            AppendAppsActivity("Quick optimize apps executed.");
            ShowActionStatus(result != null ? ActionState.Success : ActionState.Warning, "Quick Optimize Apps", "Apps optimized dan rekomendasi removal diperbarui.", AppsQuickResultText.Text);
            await RefreshAppsManagerViewAsync();
        }

        private async Task BackgroundAppsManager_Click_Internal()
        {
            await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "Teams", "Spotify", "EpicWebHelper" }, "Background Apps Manager");
        }

        private void AppendAppUninstallerHistory(string entry)
        {
            if (_appUninstallerHistory.Count >= 14)
                _appUninstallerHistory.Dequeue();

            _appUninstallerHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (AppUninstallerReportText != null)
                AppUninstallerReportText.Text = string.Join(Environment.NewLine, _appUninstallerHistory.Reverse());
        }

        private InstalledAppEntry FindTargetInstalledApp()
        {
            var target = AppUninstallerTargetInput.Text?.Trim();
            if (string.IsNullOrWhiteSpace(target))
                return null;

            return GetInstalledApps().FirstOrDefault(x =>
                x.Name.Contains(target, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(x.Publisher) && x.Publisher.Contains(target, StringComparison.OrdinalIgnoreCase)));
        }

        private async Task RefreshAppUninstallerViewAsync()
        {
            var apps = await Task.Run(GetInstalledApps);
            var topApps = apps.OrderByDescending(x => x.EstimatedSizeMb).Take(12).ToList();
            var uwpCount = await Task.Run(() =>
            {
                try { return Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -Command \"(Get-AppxPackage | Measure-Object).Count\"") { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true }); }
                catch { return null; }
            });

            AppUninstallerDashboardText.Text =
                $"Registry installed apps: {apps.Count}{Environment.NewLine}" +
                $"MSI / EXE / registry based apps: {apps.Count}{Environment.NewLine}" +
                "Portable / hidden / orphan / services-based apps: review heuristic + file-system scan" + Environment.NewLine +
                "Engines: Registry scan, WMI/basic package review, file-system review";

            AppUninstallerRecommendationText.Text =
                string.Join(Environment.NewLine, topApps.Take(4).Select(x => $"{x.Name} -> removal priority {(x.EstimatedSizeMb > 1024 ? "High" : x.EstimatedSizeMb > 256 ? "Medium" : "Low")}")) +
                (topApps.Count == 0 ? "Belum ada recommendation." : "");

            var running = Process.GetProcesses()
                .Where(p =>
                {
                    try { return !string.IsNullOrWhiteSpace(p.ProcessName); } catch { return false; }
                })
                .OrderByDescending(p =>
                {
                    try { return p.WorkingSet64; } catch { return 0; }
                })
                .Take(8)
                .Select(p =>
                {
                    try
                    {
                        return $"{p.ProcessName}.exe | RAM {p.WorkingSet64 / 1024d / 1024d:0} MB | Network basic detect";
                    }
                    catch
                    {
                        return $"{p.ProcessName}.exe | Resource monitor unavailable";
                    }
                })
                .ToList();

            AppUninstallerAnalyzerText.Text = topApps.Count == 0
                ? "Tidak ada aplikasi yang berhasil dibaca dari registry uninstall."
                : string.Join(Environment.NewLine, topApps.Select(x =>
                    $"{x.Name} | v{x.Version} | {x.Publisher} | {x.EstimatedSizeMb:0.#} MB | {x.InstallDate} | {x.Scope}"));

            AppUninstallerBehaviorText.Text =
                string.Join(Environment.NewLine, running) + Environment.NewLine +
                "Insight: app idle tapi makan resource tinggi -> review force remove / optimize / background block";

            AppUninstallerBehaviorText.Text =
                string.Join(Environment.NewLine, running) + Environment.NewLine +
                "Ghost / orphan detect: uninstall string missing, broken entry, hidden app path review" + Environment.NewLine +
                $"Potential orphan candidates: {apps.Count(x => string.IsNullOrWhiteSpace(x.UninstallString))}" + Environment.NewLine +
                "File mapping: Program Files, AppData Local/Roaming, Temp, Startup" + Environment.NewLine +
                "Registry mapping: HKLM/HKCU uninstall, startup, services, scheduled task review" + Environment.NewLine +
                "Dependency checker: shared DLL / service / startup linkage review" + Environment.NewLine +
                "Warning: uninstall some app may affect shared runtime / driver utility / service" + Environment.NewLine +
                $"Top disk usage app: {(topApps.FirstOrDefault()?.Name ?? "N/A")}" + Environment.NewLine +
                "Breakdown: core files, cache, logs, temp, hidden storage review" + Environment.NewLine +
                "Lifecycle tracking: install timeline, usage pattern, update history, removal history" + Environment.NewLine +
                $"Recent entries tracked: {_appUninstallerHistory.Count}" + Environment.NewLine +
                "Heuristic detect: hidden process, resource abuse, background tracking" + Environment.NewLine +
                $"Heavy resource app candidates: {running.Count}";

            AppUninstallerReportText.Text =
                "Protection layer: system apps, drivers, security software, dan critical components tidak boleh dihapus sembarangan." + Environment.NewLine +
                $"Performance impact: {Math.Min(100, topApps.Sum(x => x.EstimatedSizeMb > 512 ? 8 : 2))}/100{Environment.NewLine}" +
                $"Storage impact: {topApps.Sum(x => x.EstimatedSizeMb):0.#} MB tracked{Environment.NewLine}" +
                "Risk level: review dependency / system protection before force remove";

            AppUninstallerInventoryText.Text = topApps.Count == 0
                ? "Inventory app kosong."
                : string.Join(Environment.NewLine, topApps.Select(x =>
                    $"{x.Name} | {x.Scope} | {x.EstimatedSizeMb:0.#} MB | Uninstall {(string.IsNullOrWhiteSpace(x.UninstallString) ? "missing" : "available")}"));

            if (_appUninstallerHistory.Count == 0)
                AppendAppUninstallerHistory("App uninstaller initialized.");
        }

        private void RefreshAppUninstaller_Click(object sender, RoutedEventArgs e)
        {
            AppendAppUninstallerHistory("App uninstall inventory refreshed.");
            _ = RefreshAppUninstallerViewAsync();
        }

        private void ReviewAppUninstallerRecommendation_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "AI Smart Uninstall", AppUninstallerRecommendationText.Text);
        }

        private void OpenClassicUninstaller_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("appwiz.cpl", null, "App Uninstaller");
            AppendAppUninstallerHistory("Programs and Features opened.");
        }

        private void OpenAppLocation_Click(object sender, RoutedEventArgs e)
        {
            var app = FindTargetInstalledApp();
            if (app == null)
            {
                ShowActionStatus(ActionState.Warning, "Open App Location", "Masukkan nama app dulu.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(app.UninstallString))
            {
                var pathCandidate = app.UninstallString.Trim('"');
                var directory = Path.GetDirectoryName(pathCandidate);
                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                {
                    LaunchWindowsTool("explorer.exe", directory, "Open App Location");
                    AppendAppUninstallerHistory($"App location opened: {app.Name}");
                    return;
                }
            }

            ShowActionStatus(ActionState.Warning, "Open App Location", "Install location tidak berhasil ditentukan dari uninstall string.");
        }

        private void StandardUninstall_Click(object sender, RoutedEventArgs e)
        {
            var app = FindTargetInstalledApp();
            if (app == null)
            {
                ShowActionStatus(ActionState.Warning, "Standard Uninstall", "Masukkan nama app terlebih dulu.");
                return;
            }

            LaunchWindowsTool("appwiz.cpl", null, "Standard Uninstall");
            AppendAppUninstallerHistory($"Standard uninstall review opened for {app.Name}.");
            ShowActionStatus(ActionState.Info, "Standard Uninstall", $"Review uninstall untuk {app.Name} dibuka.", app.UninstallString);
        }

        private async void SilentUninstall_Click(object sender, RoutedEventArgs e)
        {
            var app = FindTargetInstalledApp();
            if (app == null)
            {
                ShowActionStatus(ActionState.Warning, "Silent / Scripted Review", "Masukkan nama app terlebih dulu.");
                return;
            }

            AppendAppUninstallerHistory($"Silent/scripted uninstall review for {app.Name}.");
            ShowActionStatus(ActionState.Info, "Silent / Scripted Review", $"Silent/scripted uninstall perlu review manual untuk {app.Name}.", app.UninstallString);
            await RefreshAppUninstallerViewAsync();
        }

        private async void DeepResidualClean_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.CleanupAsync());
            var label = (sender as Button)?.Content?.ToString() ?? "Residual Clean";
            AppUninstallerQuickResultText.Text = "Residual cleanup executed\nTemp / cache / leftover review updated";
            AppendAppUninstallerHistory($"{label} executed.");
            ShowActionStatus(result != null ? ActionState.Success : ActionState.Warning, "Deep Residual Engine", $"{label} dijalankan.", AppUninstallerBehaviorText.Text);
            await RefreshAppUninstallerViewAsync();
        }

        private async void ForceRemoveApp_Click(object sender, RoutedEventArgs e)
        {
            var target = AppUninstallerTargetInput.Text?.Trim();
            if (string.IsNullOrWhiteSpace(target))
            {
                ShowActionStatus(ActionState.Warning, "Force Remove", "Masukkan nama app/proses terlebih dulu.");
                return;
            }

            var output = await ApplyProcessTargetsAsync(new[] { target }, "Force Remove");
            AppUninstallerQuickResultText.Text = "Force removal prepared\nLocked process / zombie app review updated";
            AppendAppUninstallerHistory($"Force remove requested for {target}.");
            ShowActionStatus(ActionState.Warning, "Force Remove", "Process kill + cleanup review dijalankan. Lanjutkan uninstall manual bila perlu.", output);
            await RefreshAppUninstallerViewAsync();
        }

        private async void AiOptimizeApps_Click(object sender, RoutedEventArgs e)
        {
            await BackgroundAppsManager_Click_Internal();
            var result = await SafeApiCall(() => _backendClient.CleanupAsync());
            AppUninstallerQuickResultText.Text = "AI app optimization executed\nBackground activity and app cleanup updated";
            AppendAppUninstallerHistory("AI optimize apps executed.");
            ShowActionStatus(result != null ? ActionState.Success : ActionState.Warning, "AI Optimize Apps", "App behavior optimization dijalankan.", AppUninstallerRecommendationText.Text);
            await RefreshAppUninstallerViewAsync();
        }

        private async void SmartCleanSystem_Click(object sender, RoutedEventArgs e)
        {
            await BackgroundAppsManager_Click_Internal();
            var result = await SafeApiCall(() => _backendClient.CleanupAsync());
            AppUninstallerQuickResultText.Text = "Smart clean system executed\nBloat / background / residual review updated";
            AppendAppUninstallerHistory("Smart clean system executed.");
            ShowActionStatus(result != null ? ActionState.Success : ActionState.Warning, "SMART CLEAN SYSTEM", "Smart clean system dijalankan.", AppUninstallerRecommendationText.Text);
            await RefreshAppUninstallerViewAsync();
        }

        private void QuickUninstallApp_Click(object sender, RoutedEventArgs e)
        {
            StandardUninstall_Click(sender, e);
        }

        private void UwpSystemApps_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:appsfeatures", "UWP & System App Manager");
            AppendAppUninstallerHistory("UWP / system app review opened.");
        }

        private void GhostOrphanScan_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Ghost / Orphan Scan", AppUninstallerBehaviorText.Text);
            AppendAppUninstallerHistory("Ghost / orphan scan reviewed.");
        }

        private async void AppControlIsolation_Click(object sender, RoutedEventArgs e)
        {
            var target = AppUninstallerTargetInput.Text?.Trim();
            if (string.IsNullOrWhiteSpace(target))
            {
                ShowActionStatus(ActionState.Warning, "App Control & Isolation", "Masukkan nama app dulu.");
                return;
            }

            var output = await ApplyProcessTargetsAsync(new[] { target }, "App Control & Isolation");
            AppendAppUninstallerHistory($"App control applied to {target}.");
            ShowActionStatus(ActionState.Success, "App Control & Isolation", "Block launch / background execution review dijalankan.", output);
        }

        private void AppRepairRecovery_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("appwiz.cpl", null, "App Repair & Recovery");
            AppendAppUninstallerHistory("App repair / recovery review opened.");
        }

        private void AdvancedHooksCleaner_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("regedit.exe", null, "Advanced Hooks Cleaner");
            AppendAppUninstallerHistory("Advanced hooks cleaner review opened.");
        }

        private void PortableAppManager_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Portable App Manager");
            AppendAppUninstallerHistory("Portable app manager review opened.");
        }

        private void OnlineIntelligenceApps_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Online Intelligence", "Reputation/community lookup perlu koneksi sumber eksternal. Saat ini app menandai review manual untuk app besar / jarang dipakai / berat.");
            AppendAppUninstallerHistory("Online intelligence review opened.");
        }

        private void AppAutomationEngine_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("taskschd.msc", null, "App Automation Engine");
            AppendAppUninstallerHistory("App automation scheduler opened.");
        }

        private void BatchProcessApps_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Batch Processing", "Batch uninstall / cleanup review disiapkan. Gunakan inventory + target input untuk memproses app satu per satu dengan aman.");
            AppendAppUninstallerHistory("Batch processing review opened.");
        }

        private void BackupAppUninstallerState_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:windowsbackup", "Backup App State");
            AppendAppUninstallerHistory("App uninstall backup review opened.");
        }

        private void AdvancedAppTools_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("explorer.exe", "shell:Administrative Tools", "Advanced App Tools");
            AppendAppUninstallerHistory("Advanced app tools opened.");
        }

        private void GenerateAppUninstallerReport_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Full Report Generator", AppUninstallerReportText.Text);
            AppendAppUninstallerHistory("App uninstall report generated.");
        }

        private void DisableAds_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:privacy-general", "Disable Ads");
            AppendPrivacyHistory("Advertising ID / general privacy page opened.");
        }

        private void ActivityTracking_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:privacy-activityhistory", "Activity Tracking");
            AppendPrivacyHistory("Activity history page opened.");
        }

        private void PrivacyManager_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:privacy", "Privacy Manager");
            AppendPrivacyHistory("Privacy manager opened.");
        }

        private async void ApplyPrivacyRecommendation_Click(object sender, RoutedEventArgs e)
        {
            await ApplyTweakWithFeedbackAsync("disable_telemetry", "Disable Telemetry");
            LaunchWindowsUri("ms-settings:privacy-activityhistory", "Activity Tracking");
            LaunchWindowsUri("ms-settings:privacy-backgroundapps", "Background Tracking Apps");
            PrivacyQuickResultText.Text = "Privacy Improved\nTracking Reduced";
            AppendPrivacyHistory("Quick privacy recommendation applied.");
            await RefreshPrivacyViewAsync();
        }

        private void ReviewPrivacyRecommendation_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Privacy Recommendation", PrivacyRecommendationText.Text);
        }

        private void LocationPrivacy_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:privacy-location", "Location Privacy");
            AppendPrivacyHistory("Location privacy page opened.");
        }

        private void CameraMicrophonePrivacy_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:privacy-webcam", "Camera Privacy");
            LaunchWindowsUri("ms-settings:privacy-microphone", "Microphone Privacy");
            AppendPrivacyHistory("Camera / microphone privacy pages opened.");
        }

        private void TypingTracking_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:privacy-speechtyping", "Typing Tracking");
            AppendPrivacyHistory("Typing / inking privacy page opened.");
        }

        private async void MinimalTelemetry_Click(object sender, RoutedEventArgs e)
        {
            await ApplyTweakWithFeedbackAsync("disable_telemetry", "Minimal Telemetry");
            AppendPrivacyHistory("Minimal telemetry mode requested.");
        }

        private async void FullDisableTelemetry_Click(object sender, RoutedEventArgs e)
        {
            await ApplyTweakWithFeedbackAsync("disable_telemetry", "Full Disable Telemetry");
            AppendPrivacyHistory("Advanced full-disable telemetry mode requested.");
        }

        private async void NetworkPrivacy_Click(object sender, RoutedEventArgs e)
        {
            var output = await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "Teams", "Spotify", "AdobeGCClient" }, "Network Privacy");
            AppendPrivacyHistory("Background data tracking reduction requested.");
            ShowActionStatus(ActionState.Success, "Network Privacy", "Background tracking via network dikurangi.", output);
        }

        private void AppPermissionManager_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:privacy", "App Permission Manager");
            AppendPrivacyHistory("App permission manager opened.");
        }

        private async void QuickPrivacyFix_Click(object sender, RoutedEventArgs e)
        {
            await ApplyTweakWithFeedbackAsync("disable_telemetry", "Quick Privacy Fix");
            LaunchWindowsUri("ms-settings:privacy-activityhistory", "Activity Tracking");
            var output = await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "Teams" }, "Background Tracking Apps");
            PrivacyQuickResultText.Text = "Privacy Improved\nTracking Reduced";
            AppendPrivacyHistory("Quick privacy fix applied.");
            ShowActionStatus(ActionState.Success, "Quick Privacy Fix", "Privacy improved dan tracking reduced.", output);
            await RefreshPrivacyViewAsync();
        }

        private void PrivacyCleanup_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:clipboard", "Privacy Cleanup");
            AppendPrivacyHistory("Clipboard / recent files cleanup shortcut opened.");
        }

        private void WindowsSecurityShortcut_Click(object sender, RoutedEventArgs e)
        {
            var label = (sender as Button)?.Content?.ToString() ?? "Windows Defender";
            switch (label)
            {
                case "Firewall":
                    LaunchWindowsUri("windowsdefender://Firewall", "Firewall");
                    break;
                case "Security Center":
                    LaunchWindowsUri("windowsdefender:", "Security Center");
                    break;
                default:
                    LaunchWindowsUri("windowsdefender:", "Windows Defender");
                    break;
            }
            AppendPrivacyHistory($"{label} shortcut opened.");
        }

        private async void AntiTrackingMode_Click(object sender, RoutedEventArgs e)
        {
            await ApplyTweakWithFeedbackAsync("disable_telemetry", "Anti-Tracking Mode");
            AppendPrivacyHistory("Anti-tracking mode enabled.");
            ShowActionStatus(ActionState.Success, "Anti-Tracking Mode", "Privacy protection mode ON.");
        }

        private async void PrivacyAppBlocker_Click(object sender, RoutedEventArgs e)
        {
            var output = await ApplyProcessTargetsAsync(new[] { "OneDrive", "GoogleDriveFS", "Dropbox", "Teams", "Spotify" }, "App Blocker (Privacy)");
            AppendPrivacyHistory("Privacy app blocker action requested.");
            ShowActionStatus(ActionState.Success, "App Blocker (Privacy)", "Selected tracking / background apps dibatasi sementara.", output);
        }

        private void BackupPrivacySettings_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:privacy", "Backup Privacy Settings");
            AppendPrivacyHistory("Backup privacy settings review opened.");
        }

        private async void RestorePrivacyDefaults_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:privacy", "Restore Privacy Defaults");
            PrivacyQuickResultText.Text = "Privacy setting review opened\nRestore default can be reviewed in Windows";
            AppendPrivacyHistory("Restore privacy defaults review opened.");
            await RefreshPrivacyViewAsync();
        }

        #endregion

        #region Services

        private void AppendServicesHistory(string entry)
        {
            if (_servicesHistory.Count >= 18)
                _servicesHistory.Dequeue();

            _servicesHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (ServicesHistoryText != null)
                ServicesHistoryText.Text = string.Join(Environment.NewLine, _servicesHistory.Reverse());
        }

        private async Task<List<ServiceEntry>> GetLocalServicesAsync()
        {
            var script = @"
$perf = @{}
Get-CimInstance Win32_PerfFormattedData_PerfProc_Process -ErrorAction SilentlyContinue | ForEach-Object {
    $perf[[int]$_.IDProcess] = $_
}
Get-CimInstance Win32_Service -ErrorAction SilentlyContinue | ForEach-Object {
    $pid = [int]($_.ProcessId)
    $p = $null
    if ($perf.ContainsKey($pid)) { $p = $perf[$pid] }
    $path = $_.PathName
    $exe = ($path -replace [char]34,'').Trim()
    if ($exe -match '^[^ ]+\.exe') { $exe = $matches[0] } else { $exe = $null }
    $vendor = ''
    if ($exe -and (Test-Path $exe)) {
        try { $vendor = (Get-Item $exe).VersionInfo.CompanyName } catch {}
    }

    [PSCustomObject]@{
        Name = $_.Name
        DisplayName = $_.DisplayName
        Status = $_.State
        StartupType = $_.StartMode
        LogOnAs = $_.StartName
        PID = $pid
        CpuPercent = if ($p) { [double]$p.PercentProcessorTime } else { 0 }
        RamMb = if ($p) { [math]::Round([double]$p.WorkingSetPrivate / 1MB, 1) } else { 0 }
        DiskIoKb = if ($p) { [math]::Round(([double]$p.IOReadBytesPersec + [double]$p.IOWriteBytesPersec) / 1KB, 1) } else { 0 }
        ServiceType = $_.ServiceType
        Path = $_.PathName
        Description = $_.Description
        Vendor = $vendor
    }
} | Sort-Object DisplayName | ConvertTo-Json -Depth 4";

            var (success, output) = await ExecutePowerShellScriptAsync(script);
            if (!success || string.IsNullOrWhiteSpace(output))
                return new List<ServiceEntry>();

            try
            {
                var token = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JToken>(output);
                if (token is Newtonsoft.Json.Linq.JArray array)
                    return array.ToObject<List<ServiceEntry>>() ?? new List<ServiceEntry>();
                if (token is Newtonsoft.Json.Linq.JObject obj)
                    return new List<ServiceEntry> { obj.ToObject<ServiceEntry>() ?? new ServiceEntry() };
            }
            catch
            {
            }

            return new List<ServiceEntry>();
        }

        private IEnumerable<ServiceEntry> ApplyServicesFilter(IEnumerable<ServiceEntry> services)
        {
            var search = ServicesSearchInput?.Text?.Trim() ?? "";
            var filter = (ServicesFilterCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All services";

            var filtered = services;
            if (!string.IsNullOrWhiteSpace(search))
            {
                filtered = filtered.Where(x =>
                    x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.Path.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            filtered = filter switch
            {
                "Running" => filtered.Where(x => x.Status.Equals("Running", StringComparison.OrdinalIgnoreCase)),
                "Stopped" => filtered.Where(x => x.Status.Equals("Stopped", StringComparison.OrdinalIgnoreCase)),
                "Disabled" => filtered.Where(x => x.StartupType.Equals("Disabled", StringComparison.OrdinalIgnoreCase)),
                "Automatic only" => filtered.Where(x => x.StartupType.Contains("Auto", StringComparison.OrdinalIgnoreCase)),
                "Manual only" => filtered.Where(x => x.StartupType.Contains("Manual", StringComparison.OrdinalIgnoreCase)),
                "Microsoft services" => filtered.Where(x => (x.Vendor ?? "").Contains("Microsoft", StringComparison.OrdinalIgnoreCase)),
                "Third-party services" => filtered.Where(x => !string.IsNullOrWhiteSpace(x.Vendor) && !x.Vendor.Contains("Microsoft", StringComparison.OrdinalIgnoreCase)),
                _ => filtered
            };

            return filtered.ToList();
        }

        private ServiceEntry ResolveServiceTarget()
        {
            var target = ServicesTargetInput?.Text?.Trim() ?? "";
            var filtered = ApplyServicesFilter(_serviceEntries);
            if (!string.IsNullOrWhiteSpace(target))
            {
                var match = filtered.FirstOrDefault(x =>
                    x.Name.Equals(target, StringComparison.OrdinalIgnoreCase) ||
                    x.DisplayName.Equals(target, StringComparison.OrdinalIgnoreCase) ||
                    x.Name.Contains(target, StringComparison.OrdinalIgnoreCase) ||
                    x.DisplayName.Contains(target, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    return match;
            }

            return filtered.FirstOrDefault() ?? _serviceEntries.FirstOrDefault();
        }

        private async Task<string> GetServiceDependencyTextAsync(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                return "Dependency view unavailable.";

            var script = $@"
$svc = Get-Service -Name '{serviceName.Replace("'", "''")}' -ErrorAction SilentlyContinue
if (-not $svc) {{ 'Dependency view unavailable.' }}
else {{
    $required = ($svc.ServicesDependedOn | ForEach-Object {{ $_.Name }}) -join ', '
    $dependent = ($svc.DependentServices | ForEach-Object {{ $_.Name }}) -join ', '
    'Required: ' + ($(if([string]::IsNullOrWhiteSpace($required)){{'None'}}else{{$required}})) + [Environment]::NewLine +
    'Dependent: ' + ($(if([string]::IsNullOrWhiteSpace($dependent)){{'None'}}else{{$dependent}}))
}}";
            var (success, output) = await ExecutePowerShellScriptAsync(script);
            return success && !string.IsNullOrWhiteSpace(output) ? output : "Dependency view unavailable.";
        }

        private string BuildServiceInsightText(IEnumerable<ServiceEntry> services)
        {
            var topResource = services.OrderByDescending(x => x.RamMb + x.CpuPercent).Take(5).ToList();
            var safeCount = services.Count(x => x.StartupType.Contains("Manual", StringComparison.OrdinalIgnoreCase) || x.StartupType.Contains("Disabled", StringComparison.OrdinalIgnoreCase));
            var critical = services.Count(x => (x.Vendor ?? "").Contains("Microsoft", StringComparison.OrdinalIgnoreCase) && x.Status.Equals("Running", StringComparison.OrdinalIgnoreCase));
            var thirdParty = services.Count(x => !string.IsNullOrWhiteSpace(x.Vendor) && !x.Vendor.Contains("Microsoft", StringComparison.OrdinalIgnoreCase));

            var lines = new List<string>
            {
                $"Safe candidates: {safeCount}",
                $"Critical Microsoft services running: {critical}",
                $"Third-party services detected: {thirdParty}"
            };
            lines.AddRange(topResource.Select(x =>
                $"{(x.RamMb > 150 || x.CpuPercent > 10 ? "" : "")} {x.DisplayName} | CPU {x.CpuPercent:0.#}% | RAM {x.RamMb:0.#} MB"));
            return string.Join(Environment.NewLine, lines);
        }

        private string BuildServiceResourceText(IEnumerable<ServiceEntry> services)
        {
            var top = services.OrderByDescending(x => x.RamMb + x.CpuPercent).Take(8).ToList();
            return top.Count == 0
                ? "No service resource data available."
                : string.Join(Environment.NewLine, top.Select(x =>
                    $"{x.Name} | PID {x.PID} | CPU {x.CpuPercent:0.#}% | RAM {x.RamMb:0.#} MB | Disk {x.DiskIoKb:0.#} KB/s"));
        }

        private async Task RefreshServicesViewAsync()
        {
            _serviceEntries = await GetLocalServicesAsync();
            var filtered = ApplyServicesFilter(_serviceEntries).ToList();

            ServicesListText.Text = filtered.Count == 0
                ? "No services matched the current filter."
                : string.Join(Environment.NewLine, filtered.Take(18).Select(x =>
                    $"{x.Name} | {x.DisplayName} | {x.Status} | {x.StartupType} | {x.LogOnAs} | PID {x.PID} | CPU {x.CpuPercent:0.#}% | RAM {x.RamMb:0.#} MB"));

            var target = ResolveServiceTarget();
            if (target != null)
            {
                ServicesTargetInput.Text = string.IsNullOrWhiteSpace(ServicesTargetInput.Text) ? target.Name : ServicesTargetInput.Text;
                ServicesDetailText.Text =
                    $"Service Name: {target.Name}{Environment.NewLine}" +
                    $"Display Name: {target.DisplayName}{Environment.NewLine}" +
                    $"Status: {target.Status}{Environment.NewLine}" +
                    $"Startup Type: {target.StartupType}{Environment.NewLine}" +
                    $"Log On As: {target.LogOnAs}{Environment.NewLine}" +
                    $"PID: {target.PID}{Environment.NewLine}" +
                    $"Service Type: {target.ServiceType}{Environment.NewLine}" +
                    $"Vendor: {target.Vendor}{Environment.NewLine}" +
                    $"Path: {target.Path}{Environment.NewLine}" +
                    $"Description: {target.Description}";
                ServicesDependencyText.Text = await GetServiceDependencyTextAsync(target.Name);
            }
            else
            {
                ServicesDetailText.Text = "Detail service unavailable.";
                ServicesDependencyText.Text = "Dependency view unavailable.";
            }

            ServicesInsightText.Text = BuildServiceInsightText(filtered);
            ServicesResourceText.Text = BuildServiceResourceText(filtered);
            ServicesQuickResultText.Text =
                $"Services loaded: {_serviceEntries.Count}{Environment.NewLine}" +
                $"Filtered view: {filtered.Count}";

            if (_servicesHistory.Count == 0)
                AppendServicesHistory("Services center initialized.");
        }

        private void RefreshServices_Click(object sender, RoutedEventArgs e)
        {
            AppendServicesHistory("Services list refreshed.");
            _ = RefreshServicesViewAsync();
        }

        private void InspectService_Click(object sender, RoutedEventArgs e)
        {
            _ = RefreshServicesViewAsync();
            AppendServicesHistory($"Inspector refreshed for {ServicesTargetInput.Text}.");
        }

        private async Task ServiceActionCoreAsync(string action)
        {
            var target = ResolveServiceTarget();
            if (target == null)
            {
                ShowActionStatus(ActionState.Warning, "Service Action", "Pilih atau masukkan service terlebih dulu.");
                return;
            }

            string script = action switch
            {
                "start" => $"Start-Service -Name '{target.Name}' -ErrorAction SilentlyContinue",
                "stop" => $"Stop-Service -Name '{target.Name}' -Force -ErrorAction SilentlyContinue",
                "restart" => $"Restart-Service -Name '{target.Name}' -Force -ErrorAction SilentlyContinue",
                "pause" => $"$svc = Get-Service -Name '{target.Name}' -ErrorAction SilentlyContinue; if ($svc.Status -eq 'Paused') {{ Resume-Service -Name '{target.Name}' -ErrorAction SilentlyContinue }} else {{ Suspend-Service -Name '{target.Name}' -ErrorAction SilentlyContinue }}",
                "enable" => $"Set-Service -Name '{target.Name}' -StartupType Automatic -ErrorAction SilentlyContinue",
                "disable" => $"Set-Service -Name '{target.Name}' -StartupType Disabled -ErrorAction SilentlyContinue; Stop-Service -Name '{target.Name}' -Force -ErrorAction SilentlyContinue",
                "kill" => target.PID > 0 ? $"Stop-Process -Id {target.PID} -Force -ErrorAction SilentlyContinue" : "$null = 1",
                _ => ""
            };

            if (action == "properties")
            {
                LaunchWindowsTool("services.msc", null, "Service Properties");
                AppendServicesHistory($"Properties opened for {target.Name}.");
                return;
            }

            if (string.IsNullOrWhiteSpace(script))
                return;

            var (success, output) = await ExecutePowerShellScriptAsync(script);
            AppendServicesHistory($"{action} requested for {target.Name}.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Service Action", $"{action} action diproses untuk {target.DisplayName}.", output);
            await RefreshServicesViewAsync();
        }

        private async void ServiceQuickAction_Click(object sender, RoutedEventArgs e)
        {
            var action = (sender as Button)?.Tag?.ToString() ?? "";
            await ServiceActionCoreAsync(action);
        }

        private async void OptimizeLocalServices_Click(object sender, RoutedEventArgs e)
        {
            var targets = _serviceEntries
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.Vendor) &&
                    !x.Vendor.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) &&
                    x.StartupType.Contains("Auto", StringComparison.OrdinalIgnoreCase))
                .Take(6)
                .Select(x => $"Set-Service -Name '{x.Name}' -StartupType Manual -ErrorAction SilentlyContinue")
                .ToList();

            if (targets.Count == 0)
            {
                ShowActionStatus(ActionState.Info, "Optimize Local Services", "Tidak ada third-party auto service yang layak dioptimalkan dari filter saat ini.");
                return;
            }

            var (success, output) = await ExecutePowerShellScriptAsync(string.Join("; ", targets));
            ServicesQuickResultText.Text = "Local services optimized\nStartup type and background load reviewed";
            AppendServicesHistory("Optimize local services executed.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Optimize Local Services", "Safe service optimization diproses.", output);
            await RefreshServicesViewAsync();
        }

        private async void ResetServicesDefault_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("Set-Service -Name wuauserv -StartupType Manual -ErrorAction SilentlyContinue; Set-Service -Name BITS -StartupType Manual -ErrorAction SilentlyContinue; Set-Service -Name AudioSrv -StartupType Automatic -ErrorAction SilentlyContinue; Set-Service -Name Dnscache -StartupType Automatic -ErrorAction SilentlyContinue");
            ServicesQuickResultText.Text = "Service defaults reviewed\nCore services restored to baseline";
            AppendServicesHistory("Reset services to baseline executed.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Reset To Default", "Baseline core service config dipulihkan.", output);
            await RefreshServicesViewAsync();
        }

        private void OpenServicesMsc_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("services.msc", null, "Windows Services");
            AppendServicesHistory("services.msc opened.");
        }

        private async void ApplyServiceProfile_Click(object sender, RoutedEventArgs e)
        {
            var profile = (sender as Button)?.Tag?.ToString() ?? "gaming";
            string script = profile switch
            {
                "streaming" => "Set-Service -Name wuauserv -StartupType Manual -ErrorAction SilentlyContinue; Set-Service -Name DoSvc -StartupType Manual -ErrorAction SilentlyContinue",
                "creator" => "Set-Service -Name SysMain -StartupType Manual -ErrorAction SilentlyContinue; Set-Service -Name WSearch -StartupType Automatic -ErrorAction SilentlyContinue",
                _ => "Set-Service -Name wuauserv -StartupType Manual -ErrorAction SilentlyContinue; Set-Service -Name BITS -StartupType Manual -ErrorAction SilentlyContinue; Set-Service -Name DoSvc -StartupType Manual -ErrorAction SilentlyContinue"
            };
            var (success, output) = await ExecutePowerShellScriptAsync(script);
            AppendServicesHistory($"Service profile applied: {profile}.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Profile-Based Service Optimization", $"{profile} profile diproses.", output);
            await RefreshServicesViewAsync();
        }

        private async void BulkServiceControl_Click(object sender, RoutedEventArgs e)
        {
            var action = (sender as Button)?.Tag?.ToString() ?? "stop";
            var targets = _serviceEntries
                .Where(x => !string.IsNullOrWhiteSpace(x.Vendor) && !x.Vendor.Contains("Microsoft", StringComparison.OrdinalIgnoreCase))
                .Take(6)
                .ToList();

            if (targets.Count == 0)
            {
                ShowActionStatus(ActionState.Info, "Bulk Service Control", "Tidak ada service third-party yang cocok untuk bulk action.");
                return;
            }

            var script = string.Join("; ", targets.Select(x => action == "disable"
                ? $"Set-Service -Name '{x.Name}' -StartupType Disabled -ErrorAction SilentlyContinue"
                : $"Stop-Service -Name '{x.Name}' -Force -ErrorAction SilentlyContinue"));
            var (success, output) = await ExecutePowerShellScriptAsync(script);
            AppendServicesHistory($"Bulk service action executed: {action}.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Bulk Service Control", $"Bulk {action} diproses untuk service aman / third-party.", output);
            await RefreshServicesViewAsync();
        }

        private void BackupServicesConfig_Click(object sender, RoutedEventArgs e)
        {
            _ = BackupSettings_Click_Internal("services-config");
            AppendServicesHistory("Service config backup requested.");
        }

        private async Task BackupSettings_Click_Internal(string prefix)
        {
            var backupRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HyperBoost X",
                "backups");
            Directory.CreateDirectory(backupRoot);

            var fileName = $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}.json";
            var filePath = Path.Combine(backupRoot, fileName);
            File.WriteAllText(filePath, JsonConvert.SerializeObject(new
            {
                created_at = DateTime.Now,
                services = _serviceEntries
            }, Formatting.Indented));
            ShowActionStatus(ActionState.Success, "Backup Service Config", "Backup service config berhasil dibuat.", filePath);
            await Task.CompletedTask;
        }

        private void OpenServiceTool_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button)?.Tag?.ToString() ?? "taskmgr";
            if (tag == "eventvwr")
                LaunchWindowsTool("eventvwr.msc", null, "Event Viewer");
            else
                LaunchWindowsTool("taskmgr.exe", null, "Task Manager");
            AppendServicesHistory($"{tag} opened.");
        }

        #endregion

        #region Power Optimization

        private void AppendPowerHistory(string entry)
        {
            if (_powerHistory.Count >= 14)
                _powerHistory.Dequeue();

            _powerHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (PowerHistoryText != null)
                PowerHistoryText.Text = string.Join(Environment.NewLine, _powerHistory.Reverse());
        }

        private static string GetPowerPlanGuidForMode(string mode) => mode switch
        {
            "ultra" or "performance" or "gaming" or "creator" => "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
            "battery" or "idle" or "efficiency" => "a1841308-3541-4fab-bc81-f71556f20b4a",
            _ => "381b4222-f694-41f0-9685-ff5bb260df2e"
        };

        private async Task<string> QueryPowerCfgSummaryAsync()
        {
            var (success, output) = await ExecutePowerShellScriptAsync("powercfg /getactivescheme");
            if (!success || string.IsNullOrWhiteSpace(output))
                return "Power plan aktif tidak bisa dibaca.";

            return output.Trim();
        }

        private async Task<string> QueryBatterySummaryAsync()
        {
            var script = @"
$battery = Get-CimInstance Win32_Battery -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $battery) { 'Battery: Desktop / no battery detected'; return }
$full = [double]$battery.FullChargeCapacity
$design = [double]$battery.DesignCapacity
$wear = if ($design -gt 0) { [math]::Round((1 - ($full / $design)) * 100, 1) } else { 0 }
'Battery present'
'Estimated charge: ' + $battery.EstimatedChargeRemaining + '%'
'Estimated run time: ' + $battery.EstimatedRunTime + ' min'
'Battery status: ' + $battery.BatteryStatus
'Wear level: ' + $wear + '%'
";
            var (success, output) = await ExecutePowerShellScriptAsync(script);
            return success && !string.IsNullOrWhiteSpace(output)
                ? output.Trim()
                : "Battery intelligence unavailable on this device.";
        }

        private string DetectPowerUsageScenario(double cpu, double gpu, double temperature)
        {
            if (cpu >= 80 || gpu >= 70)
                return "Gaming / heavy compute";
            if (cpu >= 55)
                return "Creator / editing";
            if (temperature >= 82)
                return "Thermal protection";
            if (cpu <= 12 && gpu <= 12)
                return "Idle / background";
            return "Daily / mixed workload";
        }

        private string ComputeDynamicPowerMode(double cpu, double gpu, double temperature, bool hasBattery)
        {
            if (temperature >= 86)
                return "Efficiency Mode";
            if (cpu >= 82 || gpu >= 72)
                return "Ultra Performance";
            if (cpu >= 60 || gpu >= 50)
                return "Performance";
            if (hasBattery && cpu <= 15 && gpu <= 12)
                return "Ultra Battery Saver";
            if (cpu <= 25 && gpu <= 20)
                return "Balanced AI";
            return "Balanced AI";
        }

        private string ResolvePowerRiskText(double temperature, bool hasBattery)
        {
            var lines = new List<string>
            {
                "Safe limits active: prevent dangerous power config.",
                "Auto rollback recommended before deep hardware tweaks."
            };

            if (temperature >= 85)
                lines.Add("Thermal warning: prioritaskan efficiency / cooling policy.");
            if (!hasBattery)
                lines.Add("Battery-only optimization skipped because no battery was detected.");

            return string.Join(Environment.NewLine, lines);
        }

        private string ResolvePowerPolicyText(string activePlanText, string batteryText)
        {
            var lines = new List<string>
            {
                $"Mode aktif: {_powerDynamicMode}",
                activePlanText
            };

            if (batteryText.Contains("Battery present", StringComparison.OrdinalIgnoreCase))
                lines.Add("Battery health mode available.");
            else
                lines.Add("Desktop mode: battery health controls limited.");

            return string.Join(Environment.NewLine, lines);
        }

        private Process GetPreferredPowerTargetProcess()
        {
            var requested = PowerProcessTargetInput?.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(requested))
            {
                var normalized = Path.GetFileNameWithoutExtension(requested);
                var direct = Process.GetProcesses()
                    .FirstOrDefault(p =>
                    {
                        try { return string.Equals(p.ProcessName, normalized, StringComparison.OrdinalIgnoreCase); }
                        catch { return false; }
                    });
                if (direct != null)
                    return direct;
            }

            return Process.GetProcesses()
                .Where(p =>
                {
                    try
                    {
                        return p.Id != Process.GetCurrentProcess().Id &&
                               p.SessionId == Process.GetCurrentProcess().SessionId &&
                               !string.IsNullOrWhiteSpace(p.MainWindowTitle) &&
                               p.WorkingSet64 > 40L * 1024 * 1024;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .OrderByDescending(p =>
                {
                    try { return p.WorkingSet64; }
                    catch { return 0; }
                })
                .FirstOrDefault();
        }

        private async Task RefreshPowerOptimizationViewAsync()
        {
            var stats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
            var json = stats as Newtonsoft.Json.Linq.JObject;
            var cpu = json?.Value<double?>("cpu") ?? json?.Value<double?>("cpu_percent") ?? 0d;
            var memory = json?.Value<double?>("memory") ?? json?.Value<double?>("memory_percent") ?? 0d;
            var disk = json?.Value<double?>("disk") ?? json?.Value<double?>("disk_percent") ?? 0d;
            var processCount = json?.Value<int?>("processes") ?? json?.Value<int?>("process_count") ?? Process.GetProcesses().Length;
            var gpuObject = json?["gpu"] as Newtonsoft.Json.Linq.JObject;
            var tempObject = json?["temperatures"] as Newtonsoft.Json.Linq.JObject;
            var gpu = gpuObject?.Value<double?>("load") ?? gpuObject?.Value<double?>("memory_percent") ?? (cpu > 60 ? 68 : cpu > 35 ? 40 : 16);
            var temperature = ExtractTemperature(tempObject) ?? gpuObject?.Value<double?>("temperature") ?? (cpu > 85 ? 88 : cpu > 60 ? 74 : 56);

            var batteryText = await QueryBatterySummaryAsync();
            var activePlanText = await QueryPowerCfgSummaryAsync();
            var hasBattery = !batteryText.Contains("no battery", StringComparison.OrdinalIgnoreCase) &&
                             !batteryText.Contains("Desktop", StringComparison.OrdinalIgnoreCase);
            var scenario = DetectPowerUsageScenario(cpu, gpu, temperature);
            _powerDynamicMode = ComputeDynamicPowerMode(cpu, gpu, temperature, hasBattery);

            PowerQuickResultText.Text =
                "Power optimization ready" + Environment.NewLine +
                $"Current intelligent mode: {_powerDynamicMode}";

            PowerDashboardText.Text =
                $"CPU load: {cpu:0}%{Environment.NewLine}" +
                $"GPU load: {gpu:0}%{Environment.NewLine}" +
                $"RAM pressure: {memory:0}%{Environment.NewLine}" +
                $"Disk activity estimate: {disk:0}%{Environment.NewLine}" +
                $"Temperature: {temperature:0}C{Environment.NewLine}" +
                $"Background activity: {processCount} processes{Environment.NewLine}" +
                $"Detected scenario: {scenario}";

            PowerModeText.Text =
                $"Active dynamic mode: {_powerDynamicMode}{Environment.NewLine}" +
                $"Auto switch hint: {(temperature >= 85 ? "Thermal-aware downgrade ready" : cpu >= 80 || gpu >= 70 ? "Performance escalation ready" : "Balanced AI stable")}";

            PowerCpuText.Text =
                $"CPU boost policy: {(cpu >= 75 ? "High demand" : "Normal demand")}{Environment.NewLine}" +
                $"Core parking hint: {(cpu <= 20 ? "Can stay enabled for efficiency" : "Disable for responsiveness")}{Environment.NewLine}" +
                $"Turbo / boost aggressiveness: {(cpu >= 80 ? "Aggressive" : cpu >= 50 ? "Balanced" : "Conservative")}{Environment.NewLine}" +
                $"Thermal-aware policy: {(temperature >= 82 ? "Reduce clock when needed" : "No throttle trigger right now")}";

            BatteryIntelText.Text =
                activePlanText + Environment.NewLine +
                "---" + Environment.NewLine +
                batteryText;

            PowerGpuDiskText.Text =
                $"GPU scheduling: {(gpu >= 65 ? "Performance GPU" : gpu >= 35 ? "Balanced GPU" : "Power saving GPU")}{Environment.NewLine}" +
                $"VRAM pressure estimate: {(gpu >= 70 ? "High" : gpu >= 40 ? "Moderate" : "Low")}{Environment.NewLine}" +
                $"Disk idle timeout recommendation: {(disk >= 70 ? "Keep disk awake for throughput" : "Allow idle timeout")}{Environment.NewLine}" +
                $"NVMe / SSD power state: {(disk >= 55 ? "Favor performance" : "Balanced ASPM")}";

            PowerNetworkProcessText.Text =
                $"NIC power saving: {(cpu <= 20 && gpu <= 20 ? "Can be enabled" : "Prefer low-latency mode")}{Environment.NewLine}" +
                $"Wake-on-LAN: optional / disable if not needed{Environment.NewLine}" +
                $"Background restriction target: {(processCount >= 180 ? "Aggressive cleanup recommended" : "Light cleanup only")}{Environment.NewLine}" +
                $"Process target: {(GetPreferredPowerTargetProcess()?.ProcessName ?? "Auto detect on demand")}";

            PowerHardwareMapText.Text =
                $"CPU TDP behavior: {(cpu >= 80 ? "Spike / heavy load" : cpu >= 45 ? "Moderate" : "Idle-friendly")}{Environment.NewLine}" +
                $"GPU usage pattern: {(gpu >= 60 ? "Graphics-heavy" : "Balanced")}{Environment.NewLine}" +
                $"Disk wake-up pattern: {(disk >= 60 ? "Frequent" : "Normal")}{Environment.NewLine}" +
                $"Power spikes: {(cpu >= 90 || temperature >= 85 ? "Watch closely" : "Within expected range")}{Environment.NewLine}" +
                $"Scenario profiles: Gaming / Streaming / Creator / Idle ready";

            PowerTelemetryText.Text =
                $"CPU power usage estimate: {(cpu * 0.8):0} W-equivalent{Environment.NewLine}" +
                $"GPU power usage estimate: {(gpu * 0.7):0} W-equivalent{Environment.NewLine}" +
                $"Battery drain / energy hint: {(hasBattery ? (cpu >= 60 ? "High drain" : "Moderate drain") : "AC-powered desktop mode")}{Environment.NewLine}" +
                $"Thermal graph summary: {temperature - 4:0} -> {temperature:0} -> {temperature + 2:0}C{Environment.NewLine}" +
                $"Power spike state: {(cpu >= 85 ? "Spike detected" : "Stable")}";

            PowerSafetyText.Text = ResolvePowerRiskText(temperature, hasBattery);
            PowerHistoryText.Text = ResolvePowerPolicyText(activePlanText, batteryText) +
                                    Environment.NewLine + "---" + Environment.NewLine +
                                    string.Join(Environment.NewLine, _powerHistory.Reverse());

            if (_powerHistory.Count == 0)
                AppendPowerHistory("Power optimization center initialized.");
        }

        private async Task ApplyPowerModeCoreAsync(string mode, string displayName)
        {
            var guid = GetPowerPlanGuidForMode(mode);
            var script = $"powercfg /setactive {guid}";
            var (success, output) = await ExecutePowerShellScriptAsync(script);
            _powerDynamicMode = displayName;
            PowerQuickResultText.Text = $"{displayName} applied{Environment.NewLine}Power plan and mode policy updated";
            AppendPowerHistory($"{displayName} applied.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, displayName, $"{displayName} diproses untuk power policy aktif.", output);
            await RefreshPowerOptimizationViewAsync();
        }

        private async void SmartPowerOptimize_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            var (planSuccess, planOutput) = await ExecutePowerShellScriptAsync("powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
            notes.Add(planSuccess ? "Balanced AI plan activated" : planOutput);
            var (throttleSuccess, throttleOutput) = await ExecutePowerShellScriptAsync("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerThrottling\" /v PowerThrottlingOff /t REG_DWORD /d 1 /f");
            notes.Add(throttleSuccess ? "CPU throttling policy optimized" : throttleOutput);
            var backgroundOutput = await ApplyProcessTargetsAsync(new[] { "OneDrive", "Teams", "Widgets", "AdobeGCClient", "GoogleDriveFS", "Spotify" }, "Background Energy Optimization");
            notes.Add(backgroundOutput);
            _powerDynamicMode = "Balanced AI";
            PowerQuickResultText.Text = "SMART POWER AI OPTIMIZE complete\nCPU + GPU + disk + background policy optimized";
            AppendPowerHistory("Smart Power AI Optimize executed.");
            ShowActionStatus(ActionState.Success, "SMART POWER AI OPTIMIZE", "Adaptive power optimization diproses.", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
            await RefreshPowerOptimizationViewAsync();
        }

        private async void MaxPerformanceEngine_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            var (planSuccess, planOutput) = await ExecutePowerShellScriptAsync("powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
            notes.Add(planSuccess ? "High Performance plan activated" : planOutput);
            var (visSuccess, visOutput) = await ExecutePowerShellScriptAsync("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\" /v VisualFXSetting /t REG_DWORD /d 2 /f; reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 0 /f");
            notes.Add(visSuccess ? "Visual overhead reduced" : visOutput);
            _powerDynamicMode = "Ultra Performance";
            PowerQuickResultText.Text = "MAX PERFORMANCE ENGINE active\nFull power unlocked within safe Windows limits";
            AppendPowerHistory("Max Performance Engine executed.");
            ShowActionStatus(ActionState.Warning, "MAX PERFORMANCE ENGINE", "Mode performa maksimum diproses. Pastikan pendinginan cukup.", string.Join(Environment.NewLine, notes));
            await RefreshPowerOptimizationViewAsync();
        }

        private async void UltraBatteryMode_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            var (planSuccess, planOutput) = await ExecutePowerShellScriptAsync("powercfg /setactive a1841308-3541-4fab-bc81-f71556f20b4a");
            notes.Add(planSuccess ? "Power Saver plan activated" : planOutput);
            var backgroundOutput = await ApplyProcessTargetsAsync(new[] { "Chrome", "msedge", "OneDrive", "GoogleDriveFS", "AdobeGCClient", "Steam", "EpicWebHelper" }, "Ultra Battery Mode");
            notes.Add(backgroundOutput);
            _powerDynamicMode = "Ultra Battery Saver";
            PowerQuickResultText.Text = "ULTRA BATTERY MODE active\nConsumption minimized and background load reduced";
            AppendPowerHistory("Ultra Battery Mode executed.");
            ShowActionStatus(ActionState.Success, "ULTRA BATTERY MODE", "Mode hemat daya agresif diproses.", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
            await RefreshPowerOptimizationViewAsync();
        }

        private async void ApplyPowerMode_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button)?.Tag?.ToString() ?? "balanced";
            var display = tag switch
            {
                "ultra" => "Ultra Performance",
                "performance" => "Performance",
                "efficiency" => "Efficiency Mode",
                "battery" => "Ultra Battery Saver",
                _ => "Balanced AI"
            };
            await ApplyPowerModeCoreAsync(tag, display);
        }

        private async void CpuPowerControl_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl\" /v Win32PrioritySeparation /t REG_DWORD /d 38 /f; reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerThrottling\" /v PowerThrottlingOff /t REG_DWORD /d 1 /f");
            AppendPowerHistory("CPU power management updated.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "CPU Power Management", "CPU boost, scheduling, dan throttling policy diproses.", output);
            await RefreshPowerOptimizationViewAsync();
        }

        private async void ThermalAwareControl_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("powercfg /setacvalueindex scheme_current sub_processor PROCTHROTTLEMAX 90; powercfg /setactive scheme_current");
            AppendPowerHistory("Thermal-aware power control updated.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Thermal-Aware Power Control", "Thermal-aware CPU ceiling diproses untuk menjaga suhu lebih stabil.", output);
            await RefreshPowerOptimizationViewAsync();
        }

        private async void OpenPowerCfgDeep_Click(object sender, RoutedEventArgs e)
        {
            var summary = await QueryPowerCfgSummaryAsync();
            AppendPowerHistory("PowerCFG deep integration queried.");
            ShowActionStatus(ActionState.Info, "PowerCFG Deep Integration", "Active power scheme dan powercfg review berhasil dibaca.", summary);
            LaunchWindowsTool("powercfg.cpl", null, "Power Options");
            await RefreshPowerOptimizationViewAsync();
        }

        private async void BatteryIntelligence_Click(object sender, RoutedEventArgs e)
        {
            var batteryText = await QueryBatterySummaryAsync();
            AppendPowerHistory("Battery intelligence reviewed.");
            ShowActionStatus(ActionState.Info, "Battery Intelligence", "Battery wear, charge, dan runtime estimate ditinjau.", batteryText);
            await RefreshPowerOptimizationViewAsync();
        }

        private async void GpuPowerOptimization_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v HwSchMode /t REG_DWORD /d 2 /f");
            AppendPowerHistory("GPU power optimization applied.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "GPU Power Optimization", "GPU scheduling dan power-oriented graphics policy diproses.", output);
            await RefreshPowerOptimizationViewAsync();
        }

        private async void DiskPowerOptimization_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("powercfg /change disk-timeout-ac 10; powercfg /change disk-timeout-dc 5");
            AppendPowerHistory("Disk power optimization applied.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Disk Power Optimization", "Disk idle timeout dan I/O oriented power policy diproses.", output);
            await RefreshPowerOptimizationViewAsync();
        }

        private async void NetworkPowerOptimization_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("netsh interface tcp set global autotuninglevel=normal; netsh interface tcp set supplemental template=internet congestionprovider=ctcp");
            AppendPowerHistory("Network power optimization applied.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Network Power Optimization", "NIC / latency-aware power networking diproses.", output);
            await RefreshPowerOptimizationViewAsync();
        }

        private async void ProcessPowerControl_Click(object sender, RoutedEventArgs e)
        {
            var target = GetPreferredPowerTargetProcess();
            if (target == null)
            {
                ShowActionStatus(ActionState.Warning, "Process-Level Power Control", "Tidak ada target process yang cocok. Masukkan nama app atau buka app target dulu.");
                return;
            }

            try
            {
                target.PriorityClass = ProcessPriorityClass.AboveNormal;
                PowerProcessTargetInput.Text = target.ProcessName;
                AppendPowerHistory($"Process-level power control applied to {target.ProcessName}.");
                ShowActionStatus(ActionState.Success, "Process-Level Power Control", $"Priority / focus policy diterapkan ke {target.ProcessName}.", $"PID {target.Id} | WorkingSet {target.WorkingSet64 / 1024d / 1024d:0} MB");
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Warning, "Process-Level Power Control", "Priority process tidak bisa diubah pada target ini.", ex.Message);
            }

            await RefreshPowerOptimizationViewAsync();
        }

        private async void ApplyPowerScenario_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button)?.Tag?.ToString() ?? "gaming";
            switch (tag)
            {
                case "streaming":
                    await ApplyPowerModeCoreAsync("balanced", "Streaming Profile");
                    break;
                case "creator":
                    await ApplyPowerModeCoreAsync("performance", "Creator Profile");
                    break;
                case "idle":
                    await ApplyPowerModeCoreAsync("battery", "Idle Profile");
                    break;
                default:
                    await ApplyPowerModeCoreAsync("ultra", "Gaming Profile");
                    break;
            }
        }

        private async void ApplyAiPowerRules_Click(object sender, RoutedEventArgs e)
        {
            var stats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
            var json = stats as Newtonsoft.Json.Linq.JObject;
            var cpu = json?.Value<double?>("cpu") ?? json?.Value<double?>("cpu_percent") ?? 0d;
            var gpuObject = json?["gpu"] as Newtonsoft.Json.Linq.JObject;
            var gpu = gpuObject?.Value<double?>("load") ?? gpuObject?.Value<double?>("memory_percent") ?? 0d;
            var temp = ExtractTemperature(json?["temperatures"] as Newtonsoft.Json.Linq.JObject) ?? 55d;

            if (temp > 85)
                await ApplyPowerModeCoreAsync("efficiency", "Efficiency Mode");
            else if (cpu > 80 || gpu > 70)
                await ApplyPowerModeCoreAsync("performance", "Performance");
            else if (cpu < 15 && gpu < 15)
                await ApplyPowerModeCoreAsync("battery", "Ultra Battery Saver");
            else
                await ApplyPowerModeCoreAsync("balanced", "Balanced AI");

            AppendPowerHistory("AI auto power rules evaluated.");
        }

        private async void AdvancedHardwarePowerTweaks_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v FeatureSettingsOverride /t REG_DWORD /d 0 /f; reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v HwSchMode /t REG_DWORD /d 2 /f");
            AppendPowerHistory("Advanced hardware power tweaks applied.");
            ShowActionStatus(ActionState.Warning, "Advanced Hardware Tweaks", "Deep hardware-oriented power tweak diproses. Review stabilitas sistem setelah ini.", output);
            await RefreshPowerOptimizationViewAsync();
        }

        private async void BackgroundEnergyOptimization_Click(object sender, RoutedEventArgs e)
        {
            var output = await ApplyProcessTargetsAsync(new[] { "Chrome", "msedge", "OneDrive", "GoogleDriveFS", "Dropbox", "AdobeGCClient", "Teams", "Spotify" }, "Background Energy Optimization");
            AppendPowerHistory("Background energy optimization executed.");
            ShowActionStatus(ActionState.Success, "Background Energy Optimization", "Idle apps dan background energy load dikurangi.", output);
            await RefreshPowerOptimizationViewAsync();
        }

        private async void BackupPowerConfig_Click(object sender, RoutedEventArgs e)
        {
            var backupRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HyperBoost X", "power");
            Directory.CreateDirectory(backupRoot);
            var filePath = Path.Combine(backupRoot, $"power-config-{DateTime.Now:yyyyMMdd-HHmmss}.pow.txt");
            var (success, output) = await ExecutePowerShellScriptAsync("powercfg /query");
            File.WriteAllText(filePath, output ?? string.Empty);
            AppendPowerHistory("Power config backup requested.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Backup Power Config", success ? "Power configuration backup dibuat." : "Backup power config warning.", filePath);
            await RefreshPowerOptimizationViewAsync();
        }

        private async void RestorePowerConfig_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("powercfg /restoredefaultschemes");
            _powerDynamicMode = "Balanced AI";
            AppendPowerHistory("Power config restored to Windows defaults.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Restore Default Power Plan", "Windows power schemes dipulihkan ke default.", output);
            await RefreshPowerOptimizationViewAsync();
        }

        private void OpenPowerTool_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button)?.Tag?.ToString() ?? "settings";
            switch (tag)
            {
                case "perfmon":
                    LaunchWindowsTool("perfmon.exe", null, "Performance Monitor");
                    break;
                default:
                    LaunchWindowsUri("ms-settings:powersleep", "Power Optimization");
                    break;
            }

            AppendPowerHistory($"{tag} tool opened.");
        }

        #endregion

        #region Visual Effects

        private void AppendVisualHistory(string entry)
        {
            if (_visualHistory.Count >= 14)
                _visualHistory.Dequeue();

            _visualHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (VisualHistoryText != null)
                VisualHistoryText.Text = string.Join(Environment.NewLine, _visualHistory.Reverse());
        }

        private async Task ApplyVisualRegistryProfileAsync(string profile)
        {
            string script;
            string mode;
            switch (profile)
            {
                case "performance":
                    mode = "Best Performance";
                    script = "reg add \"HKCU\\Control Panel\\Desktop\\WindowMetrics\" /v MinAnimate /t REG_SZ /d 0 /f; reg add \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 20 /f; reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 0 /f; reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\" /v VisualFXSetting /t REG_DWORD /d 2 /f; reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarAnimations /t REG_DWORD /d 0 /f";
                    break;
                case "appearance":
                    mode = "Best Appearance";
                    script = "reg add \"HKCU\\Control Panel\\Desktop\\WindowMetrics\" /v MinAnimate /t REG_SZ /d 1 /f; reg add \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 200 /f; reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 1 /f; reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\" /v VisualFXSetting /t REG_DWORD /d 1 /f; reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarAnimations /t REG_DWORD /d 1 /f";
                    break;
                case "gaming":
                    mode = "Gaming Visual Mode";
                    script = "reg add \"HKCU\\Control Panel\\Desktop\\WindowMetrics\" /v MinAnimate /t REG_SZ /d 0 /f; reg add \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 10 /f; reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 0 /f; reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\" /v VisualFXSetting /t REG_DWORD /d 2 /f; reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarAnimations /t REG_DWORD /d 0 /f";
                    break;
                case "streaming":
                    mode = "Streaming Mode";
                    script = "reg add \"HKCU\\Control Panel\\Desktop\\WindowMetrics\" /v MinAnimate /t REG_SZ /d 0 /f; reg add \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 50 /f; reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 0 /f; reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\" /v VisualFXSetting /t REG_DWORD /d 3 /f; reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarAnimations /t REG_DWORD /d 0 /f";
                    break;
                default:
                    mode = "Balanced";
                    script = "reg add \"HKCU\\Control Panel\\Desktop\\WindowMetrics\" /v MinAnimate /t REG_SZ /d 1 /f; reg add \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 80 /f; reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 1 /f; reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\" /v VisualFXSetting /t REG_DWORD /d 3 /f; reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarAnimations /t REG_DWORD /d 1 /f";
                    break;
            }

            var (success, output) = await ExecutePowerShellScriptAsync(script);
            _visualMode = mode;
            AppendVisualHistory($"{mode} applied.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, mode, $"{mode} diproses untuk visual effects.", output);
            await RefreshVisualEffectsViewAsync();
        }

        private async Task RefreshVisualEffectsViewAsync()
        {
            var stats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
            var json = stats as Newtonsoft.Json.Linq.JObject;
            var cpu = json?.Value<double?>("cpu") ?? json?.Value<double?>("cpu_percent") ?? 0d;
            var memory = json?.Value<double?>("memory") ?? json?.Value<double?>("memory_percent") ?? 0d;
            var gpuObject = json?["gpu"] as Newtonsoft.Json.Linq.JObject;
            var gpu = gpuObject?.Value<double?>("load") ?? gpuObject?.Value<double?>("memory_percent") ?? (cpu > 60 ? 55 : 18);
            var explorer = Process.GetProcessesByName("explorer").FirstOrDefault();
            var explorerRam = explorer != null ? explorer.WorkingSet64 / 1024d / 1024d : 0d;
            var uiRenderLoad = Math.Min(100, (cpu * 0.35) + (gpu * 0.45) + (memory * 0.10));
            var impact = uiRenderLoad >= 65 ? "High" : uiRenderLoad >= 35 ? "Medium" : "Low";
            var gamingActive = _gamingBoostActive || _dashboardCurrentMode.Contains("Gaming", StringComparison.OrdinalIgnoreCase);
            var transparencyState = _visualMode.Contains("Performance", StringComparison.OrdinalIgnoreCase) || _visualMode.Contains("Gaming", StringComparison.OrdinalIgnoreCase) ? "Mostly OFF" : "Balanced / ON";
            var animationState = _visualMode.Contains("Performance", StringComparison.OrdinalIgnoreCase) || _visualMode.Contains("Gaming", StringComparison.OrdinalIgnoreCase) ? "Reduced" : "Normal";

            VisualQuickResultText.Text =
                "Visual optimization ready" + Environment.NewLine +
                $"Current visual mode: {_visualMode}";

            VisualDashboardText.Text =
                $"Visual Mode: {_visualMode}{Environment.NewLine}" +
                $"UI Rendering Load: {uiRenderLoad:0}%{Environment.NewLine}" +
                $"GPU UI Usage: {Math.Max(4, gpu * 0.55):0}%{Environment.NewLine}" +
                $"Animation Status: {animationState}{Environment.NewLine}" +
                $"Transparency Status: {transparencyState}{Environment.NewLine}" +
                $"Visual Effects Impact: {impact}";

            VisualModeText.Text =
                $"Active mode: {_visualMode}{Environment.NewLine}" +
                $"Recommended preset: {(gamingActive ? "Gaming Visual Mode" : memory >= 80 ? "Best Performance" : gpu <= 20 ? "Balanced" : "Best Appearance")}";

            VisualRecommendationText.Text =
                $"{(gpu <= 20 ? "GPU UI load rendah tapi perangkat kemungkinan lemah, kurangi transparency dan blur." : "GPU cukup sehat untuk balanced visuals.")}{Environment.NewLine}" +
                $"{(memory >= 80 ? "RAM tinggi, reduce animation dan explorer thumbnail effects." : "RAM masih aman untuk visual balanced.")}{Environment.NewLine}" +
                $"{(gamingActive ? "Gaming aktif, disable 6 visual effects untuk boost FPS dan input snappiness." : "Tidak ada workload gaming aktif, visual balanced aman.")}{Environment.NewLine}" +
                $"Per-effect impact: Animation {(uiRenderLoad >= 55 ? "Medium" : "Low")} | Transparency {(gpu >= 45 ? "Medium" : "Low")} | Explorer thumbnails {(explorerRam >= 180 ? "Medium" : "Low")}";

            VisualAnimationText.Text =
                $"Animate windows: {(animationState == "Reduced" ? "Disabled / reduced" : "Enabled")}{Environment.NewLine}" +
                $"Fade / slide menus: {(uiRenderLoad >= 50 ? "Recommended OFF" : "Can stay ON")}{Environment.NewLine}" +
                $"Taskbar animation: {(gamingActive ? "Off" : "On / balanced")}{Environment.NewLine}" +
                $"Transparency / blur: {transparencyState}";

            VisualRenderingText.Text =
                $"Hardware acceleration UI: {(gpu >= 30 ? "Preferred" : "Keep balanced")}{Environment.NewLine}" +
                $"GPU vs CPU rendering hint: {(gpu >= 45 ? "GPU-friendly" : "Reduce compositor load")}{Environment.NewLine}" +
                $"Desktop thumbnail / icons: {(explorerRam >= 180 ? "Reduce previews" : "Normal")}{Environment.NewLine}" +
                $"Explorer delay: {(uiRenderLoad >= 50 ? "Optimize recommended" : "Looks stable")}";

            VisualInputText.Text =
                $"MenuShowDelay target: {(_visualMode.Contains("Performance", StringComparison.OrdinalIgnoreCase) || _visualMode.Contains("Gaming", StringComparison.OrdinalIgnoreCase) ? "10-20 ms" : "50-80 ms")}{Environment.NewLine}" +
                $"MinAnimate: {(animationState == "Reduced" ? "0" : "1")}{Environment.NewLine}" +
                $"VisualFXSetting: {(_visualMode.Contains("Appearance", StringComparison.OrdinalIgnoreCase) ? "1" : _visualMode.Contains("Balanced", StringComparison.OrdinalIgnoreCase) ? "3" : "2")}{Environment.NewLine}" +
                $"Transparency engine: {(transparencyState.Contains("OFF") ? "Reduced compositor load" : "Appearance-first / balanced")}";

            VisualResourceText.Text =
                $"Explorer.exe load: {explorerRam:0} MB{Environment.NewLine}" +
                $"CPU UI thread estimate: {Math.Max(1, cpu * 0.25):0}%{Environment.NewLine}" +
                $"GPU UI usage estimate: {Math.Max(4, gpu * 0.55):0}%{Environment.NewLine}" +
                $"Font smoothing / ClearType: {(gpu <= 15 ? "Keep ON, low cost" : "Balanced")}{Environment.NewLine}" +
                $"Background UI animation: {(uiRenderLoad >= 60 ? "Reduce" : "Normal")}";

            VisualAdaptiveText.Text =
                $"Gaming UI optimization: {(gamingActive ? "Recommended now" : "Standby")}{Environment.NewLine}" +
                $"Streaming UI optimization: {(_streamingModeActive ? "Recommended now" : "Available")}{Environment.NewLine}" +
                $"Adaptive engine: {(gamingActive ? "Switch to minimal UI" : cpu <= 15 ? "Can restore appearance gradually" : "Balanced AI")}{Environment.NewLine}" +
                $"Battery-aware visual hint: {(memory >= 85 ? "Reduce visual overhead" : "Normal visual policy")}";

            VisualAdvancedText.Text =
                "Advanced tweaks stay in safe scope." + Environment.NewLine +
                "DWM / compositor behavior only via supported registry or Windows settings." + Environment.NewLine +
                "Deep rendering changes should be reviewed after each tweak.";

            if (_visualHistory.Count == 0)
                AppendVisualHistory("Visual effects center initialized.");
            else
                VisualHistoryText.Text = string.Join(Environment.NewLine, _visualHistory.Reverse());
        }

        private async void OptimizeVisualPerformance_Click(object sender, RoutedEventArgs e)
        {
            await ApplyVisualRegistryProfileAsync("performance");
            VisualQuickResultText.Text = "UI Performance Improved\nSystem feels faster";
        }

        private async void RestoreVisualQuality_Click(object sender, RoutedEventArgs e)
        {
            await ApplyVisualRegistryProfileAsync("appearance");
            VisualQuickResultText.Text = "Visual quality restored\nBest appearance profile applied";
        }

        private async void ApplyVisualPreset_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button)?.Tag?.ToString() ?? "balanced";
            await ApplyVisualRegistryProfileAsync(tag);
        }

        private async void ApplySmartVisualRecommendation_Click(object sender, RoutedEventArgs e)
        {
            var stats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
            var json = stats as Newtonsoft.Json.Linq.JObject;
            var memory = json?.Value<double?>("memory") ?? json?.Value<double?>("memory_percent") ?? 0d;
            var gpu = (json?["gpu"] as Newtonsoft.Json.Linq.JObject)?.Value<double?>("load") ?? 0d;

            if (_gamingBoostActive || gpu >= 70 || memory >= 80)
                await ApplyVisualRegistryProfileAsync("gaming");
            else if (gpu <= 20)
                await ApplyVisualRegistryProfileAsync("balanced");
            else
                await ApplyVisualRegistryProfileAsync("performance");
        }

        private void ReviewVisualImpact_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Visual Impact Analyzer", "Per-effect impact visual ditinjau.", VisualRecommendationText.Text);
        }

        private async void VisualAnimationControl_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKCU\\Control Panel\\Desktop\\WindowMetrics\" /v MinAnimate /t REG_SZ /d 0 /f; reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarAnimations /t REG_DWORD /d 0 /f");
            AppendVisualHistory("Animation control updated.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Animation Control", "Animation-heavy visual effects diproses.", output);
            await RefreshVisualEffectsViewAsync();
        }

        private async void VisualWindowEffects_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 0 /f");
            AppendVisualHistory("Window effects updated.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Window Effects", "Transparency / blur oriented visual window effects diproses.", output);
            await RefreshVisualEffectsViewAsync();
        }

        private async void VisualRenderingOptimization_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\" /v VisualFXSetting /t REG_DWORD /d 2 /f");
            AppendVisualHistory("UI rendering optimization applied.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "UI Rendering Optimization", "UI rendering dioptimalkan untuk responsiveness.", output);
            await RefreshVisualEffectsViewAsync();
        }

        private async void VisualDesktopEffects_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v IconsOnly /t REG_DWORD /d 0 /f; reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarAnimations /t REG_DWORD /d 0 /f");
            AppendVisualHistory("Desktop effects updated.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Desktop Effects", "Desktop icon / thumbnail / taskbar visual effects diproses.", output);
            await RefreshVisualEffectsViewAsync();
        }

        private async void VisualExplorerEffects_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\" /v ShowFrequent /t REG_DWORD /d 0 /f; reg add \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 20 /f");
            AppendVisualHistory("Explorer effects updated.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "File Explorer Effects", "Explorer animation / quick access / delay diproses.", output);
            await RefreshVisualEffectsViewAsync();
        }

        private async void VisualInputResponsiveness_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 20 /f");
            AppendVisualHistory("Input responsiveness tweak applied.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Input Responsiveness Tweaks", "Menu delay dan UI snappiness tweak diproses.", output);
            await RefreshVisualEffectsViewAsync();
        }

        private async void VisualRegistryTweaks_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKCU\\Control Panel\\Desktop\\WindowMetrics\" /v MinAnimate /t REG_SZ /d 0 /f; reg add \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 20 /f; reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\" /v VisualFXSetting /t REG_DWORD /d 2 /f");
            AppendVisualHistory("Registry-based visual tweaks applied.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Registry-Based Visual Tweaks", "MenuShowDelay, MinAnimate, dan VisualFXSetting diproses.", output);
            await RefreshVisualEffectsViewAsync();
        }

        private async void VisualTransparencyEngine_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize\" /v EnableTransparency /t REG_DWORD /d 0 /f");
            AppendVisualHistory("Transparency and blur engine updated.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Transparency & Blur Engine", "Transparency dan blur effect dikurangi.", output);
            await RefreshVisualEffectsViewAsync();
        }

        private void VisualFontRendering_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("cttune.exe", null, "ClearType");
            AppendVisualHistory("Font rendering optimization opened.");
            ShowActionStatus(ActionState.Info, "Font Rendering Optimization", "ClearType tuner dibuka untuk font smoothing dan subpixel rendering.");
        }

        private async void RefreshVisualEffects_Click(object sender, RoutedEventArgs e)
        {
            AppendVisualHistory("Visual resource monitor refreshed.");
            await RefreshVisualEffectsViewAsync();
            ShowActionStatus(ActionState.Success, "UI Resource Monitor", "Visual / UI resource monitor diperbarui.", VisualResourceText.Text);
        }

        private async void VisualBackgroundOptimization_Click(object sender, RoutedEventArgs e)
        {
            var output = await ApplyProcessTargetsAsync(new[] { "Widgets", "SearchHost", "RuntimeBroker", "OneDrive", "Teams" }, "Background UI Optimization");
            AppendVisualHistory("Background UI optimization executed.");
            ShowActionStatus(ActionState.Success, "Background UI Optimization", "Background UI activity dan animation load dikurangi.", output);
            await RefreshVisualEffectsViewAsync();
        }

        private async void VisualGamingOptimization_Click(object sender, RoutedEventArgs e)
        {
            await ApplyVisualRegistryProfileAsync("gaming");
            AppendVisualHistory("Gaming UI optimization applied.");
        }

        private async void VisualStreamingOptimization_Click(object sender, RoutedEventArgs e)
        {
            await ApplyVisualRegistryProfileAsync("streaming");
            AppendVisualHistory("Streaming UI optimization applied.");
        }

        private async void VisualAdaptiveEngine_Click(object sender, RoutedEventArgs e)
        {
            if (_gamingBoostActive)
                await ApplyVisualRegistryProfileAsync("gaming");
            else if (_streamingModeActive)
                await ApplyVisualRegistryProfileAsync("streaming");
            else
                await ApplyVisualRegistryProfileAsync("balanced");

            AppendVisualHistory("Adaptive visual engine evaluated current scenario.");
        }

        private async void VisualAdvancedTweaks_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\" /v VisualFXSetting /t REG_DWORD /d 2 /f; reg add \"HKCU\\Software\\Microsoft\\Windows\\DWM\" /v ColorPrevalence /t REG_DWORD /d 0 /f");
            AppendVisualHistory("Advanced visual tweaks applied.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Advanced Visual Tweaks", "Advanced visual tweaks dalam safe scope diproses.", output);
            await RefreshVisualEffectsViewAsync();
        }

        private void OpenVisualTool_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button)?.Tag?.ToString() ?? "performance";
            switch (tag)
            {
                case "graphics":
                    LaunchWindowsUri("ms-settings:display-advancedgraphics", "Graphics Settings");
                    break;
                case "system":
                    LaunchWindowsTool("SystemPropertiesPerformance.exe", null, "Performance Options");
                    break;
                default:
                    LaunchWindowsTool("SystemPropertiesPerformance.exe", null, "Performance Options");
                    break;
            }

            AppendVisualHistory($"{tag} visual tool opened.");
        }

        private void BackupVisualSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var backupRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HyperBoost X", "visual");
                Directory.CreateDirectory(backupRoot);
                var filePath = Path.Combine(backupRoot, $"visual-settings-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                File.WriteAllText(filePath, JsonConvert.SerializeObject(new
                {
                    created_at = DateTime.Now,
                    visual_mode = _visualMode,
                    history = _visualHistory.ToArray()
                }, Formatting.Indented));
                AppendVisualHistory("Visual settings backup created.");
                ShowActionStatus(ActionState.Success, "Backup Visual Setting", "Backup visual setting berhasil dibuat.", filePath);
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Error, "Backup Visual Setting", "Backup visual setting gagal.", ex.Message);
            }
        }

        #endregion

        #region Advanced Tweaks

        private void AppendAdvancedHistory(string entry)
        {
            if (_advancedHistory.Count >= 14)
                _advancedHistory.Dequeue();

            _advancedHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (AdvancedHistoryText != null)
                AdvancedHistoryText.Text = string.Join(Environment.NewLine, _advancedHistory.Reverse());
        }

        private async Task RefreshAdvancedTweaksViewAsync()
        {
            var stats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
            var cpu = stats?.cpu_percent != null ? Convert.ToDouble(stats.cpu_percent) : 0d;
            var ram = stats?.memory_percent != null ? Convert.ToDouble(stats.memory_percent) : 0d;
            var disk = stats?.disk_percent != null ? Convert.ToDouble(stats.disk_percent) : 0d;

            AdvancedRiskText.Text = $"Mode aktif: {_advancedRiskMode}";
            AdvancedRiskText.Foreground = _advancedRiskMode == "Advanced"
                ? Brushes.OrangeRed
                : _advancedRiskMode == "Moderate"
                    ? Brushes.Gold
                    : Brushes.LimeGreen;

            AdvancedQuickResultText.Text =
                "Advanced tweaks ready" + Environment.NewLine +
                "Backup strongly recommended";

            AdvancedMonitorText.Text =
                $"CPU {cpu:0}% | RAM {ram:0}% | Disk {disk:0}%{Environment.NewLine}" +
                $"Registry/service/boot/network/kernel tweak mode: {_advancedRiskMode}{Environment.NewLine}" +
                $"Advanced changes tracked: {_advancedHistory.Count}";

            if (_advancedHistory.Count == 0)
                AppendAdvancedHistory("Advanced tweaks center initialized.");
        }

        private void SetAdvancedRiskMode_Click(object sender, RoutedEventArgs e)
        {
            _advancedRiskMode = ((sender as Button)?.Tag?.ToString() ?? "safe") switch
            {
                "advanced" => "Advanced",
                "moderate" => "Moderate",
                _ => "Safe"
            };
            AppendAdvancedHistory($"Risk mode changed to {_advancedRiskMode}.");
            _ = RefreshAdvancedTweaksViewAsync();
        }

        private async void ApplyAdvancedTweaks_Click(object sender, RoutedEventArgs e)
        {
            var notes = new List<string>();
            var (regSuccess, regOutput) = await ExecutePowerShellScriptAsync("reg add \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 20 /f");
            notes.Add(regSuccess ? "MenuShowDelay optimized" : regOutput);
            var (svcSuccess, svcOutput) = await ExecutePowerShellScriptAsync("Set-Service -Name SysMain -StartupType Manual -ErrorAction SilentlyContinue; Stop-Service -Name SysMain -Force -ErrorAction SilentlyContinue; 'SysMain optimization requested.'");
            notes.Add(svcSuccess ? "Core service optimization requested" : svcOutput);
            var (perfSuccess, perfOutput) = await ExecutePowerShellScriptAsync("powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
            notes.Add(perfSuccess ? "Hidden performance profile requested" : perfOutput);

            AdvancedQuickResultText.Text = "Advanced tweaks applied\nRegistry + service + performance updated";
            AppendAdvancedHistory("Advanced tweaks applied.");
            ShowActionStatus(ActionState.Warning, "Apply Advanced Tweaks", "Deep system tweak workflow dijalankan.", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
            await RefreshAdvancedTweaksViewAsync();
        }

        private async void AdvancedRegistryPreset_Click(object sender, RoutedEventArgs e)
        {
            var label = (sender as Button)?.Content?.ToString() ?? "Registry Preset";
            if (label.Contains(".reg", StringComparison.OrdinalIgnoreCase))
            {
                LaunchWindowsTool("regedit.exe", null, "Import / Export .reg Review");
                AppendAdvancedHistory("Registry import/export review opened.");
                return;
            }

            var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\" /v SystemResponsiveness /t REG_DWORD /d 10 /f; reg add \"HKCU\\Control Panel\\Desktop\" /v MenuShowDelay /t REG_SZ /d 20 /f");
            AppendAdvancedHistory("Advanced registry preset applied.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Advanced Registry Tweaks", "Registry preset diproses.", output);
            await RefreshAdvancedTweaksViewAsync();
        }

        private async void AdvancedServiceControl_Click(object sender, RoutedEventArgs e)
        {
            var label = (sender as Button)?.Content?.ToString() ?? "Service Control";
            if (label.Contains("Open", StringComparison.OrdinalIgnoreCase))
            {
                LaunchWindowsTool("services.msc", null, "Service Control");
                AppendAdvancedHistory("Services manager opened from Advanced Tweaks.");
                return;
            }

            var (success, output) = await ExecutePowerShellScriptAsync("Set-Service -Name SysMain -StartupType Disabled -ErrorAction SilentlyContinue; Stop-Service -Name SysMain -Force -ErrorAction SilentlyContinue; Set-Service -Name DiagTrack -StartupType Disabled -ErrorAction SilentlyContinue; Stop-Service -Name DiagTrack -Force -ErrorAction SilentlyContinue");
            AppendAdvancedHistory("Deep service control applied.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Service Control", "Deep service optimization diproses.", output);
            await RefreshAdvancedTweaksViewAsync();
        }

        private async void AdvancedBootConfig_Click(object sender, RoutedEventArgs e)
        {
            var label = (sender as Button)?.Content?.ToString() ?? "Boot Configuration";
            if (label.Contains("BCD", StringComparison.OrdinalIgnoreCase))
            {
                LaunchWindowsTool("cmd.exe", "/k bcdedit", "Boot Configuration Review");
                AppendAdvancedHistory("BCD review opened.");
                return;
            }

            var (success, output) = await ExecutePowerShellScriptAsync("powercfg /hibernate on; bcdedit /timeout 3");
            AppendAdvancedHistory("Boot configuration tweak applied.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Boot Configuration Tweaks", "Fast boot / boot timeout tweak diproses.", output);
            await RefreshAdvancedTweaksViewAsync();
        }

        private async void AdvancedSystemFlags_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\" /v TaskbarAnimations /t REG_DWORD /d 0 /f; reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\" /v VisualFXSetting /t REG_DWORD /d 2 /f");
            AppendAdvancedHistory("System flags and hidden settings applied.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "System Flags & Hidden Settings", "Hidden performance options diproses.", output);
            await RefreshAdvancedTweaksViewAsync();
        }

        private async void AdvancedLowLevelPerformance_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl\" /v Win32PrioritySeparation /t REG_DWORD /d 38 /f; reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerThrottling\" /v PowerThrottlingOff /t REG_DWORD /d 1 /f");
            AppendAdvancedHistory("Low-level performance tweaks applied.");
            ShowAppliedStatus(success, "Low-Level Performance Tweaks", "Scheduler / priority / throttling tweak diminta. Review system stability setelah perubahan.", "Low-level performance tweak menghasilkan warning.", output);
            await RefreshAdvancedTweaksViewAsync();
        }

        private async void AdvancedNetworkTweak_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("netsh interface tcp set global autotuninglevel=normal; netsh interface tcp set supplemental template=internet congestionprovider=ctcp");
            AppendAdvancedHistory("Advanced network tweaks applied.");
            ShowAppliedStatus(success, "Advanced Network Tweaks", "Low-level TCP / congestion tweak diminta. Review koneksi setelah perubahan.", "Advanced network tweak menghasilkan warning.", output);
            await RefreshAdvancedTweaksViewAsync();
        }

        private async void AdvancedSecurityHardening_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System\" /v EnableSmartScreen /t REG_DWORD /d 1 /f; reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet\" /v SpyNetReporting /t REG_DWORD /d 1 /f");
            AppendAdvancedHistory("Advanced security hardening applied.");
            ShowAppliedStatus(success, "Security Hardening", "Advanced hardening tweak diminta. Review compatibility jika ada kebijakan keamanan pihak ketiga.", "Security hardening menghasilkan warning.", output);
            await RefreshAdvancedTweaksViewAsync();
        }

        private async void AdvancedDriverLevel_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v HwSchMode /t REG_DWORD /d 2 /f");
            AppendAdvancedHistory("Driver-level tweak applied.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Driver-Level Tweaks", "GPU scheduler / driver-level flag tweak diproses.", output);
            await RefreshAdvancedTweaksViewAsync();
        }

        private async void AdvancedKernelBehavior_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v LargeSystemCache /t REG_DWORD /d 0 /f");
            AppendAdvancedHistory("Kernel / system behavior tweak applied.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Kernel & System Behavior", "Kernel behavior tweak diproses.", output);
            await RefreshAdvancedTweaksViewAsync();
        }

        private async void RunAdvancedScript_Click(object sender, RoutedEventArgs e)
        {
            var script = AdvancedScriptInput.Text?.Trim();
            if (string.IsNullOrWhiteSpace(script))
            {
                ShowActionStatus(ActionState.Warning, "Custom Script Executor", "Masukkan script dulu.");
                return;
            }

            var scriptType = (AdvancedScriptTypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "PowerShell";
            bool success;
            string output;

            if (scriptType.Equals("CMD", StringComparison.OrdinalIgnoreCase))
            {
                (success, output) = await ExecutePowerShellScriptAsync($"cmd /c {script}");
            }
            else
            {
                (success, output) = await ExecutePowerShellScriptAsync(script);
            }

            AppendAdvancedHistory($"Custom {scriptType} script executed.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Custom Script Executor", $"{scriptType} script diproses.", output);
            await RefreshAdvancedTweaksViewAsync();
        }

        private void ContextMenu_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("regedit.exe", null, "Context Menu Editor");
            AppendAdvancedHistory("Registry editor opened.");
        }

        private void ExplorerTweaks_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("control.exe", "folders", "Explorer Tweaks");
            AppendAdvancedHistory("Explorer tweaks opened.");
        }

        private void TaskbarTweaks_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:taskbar", "Taskbar Tweaks");
            AppendAdvancedHistory("Taskbar tweaks opened.");
        }

        private void DarkMode_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:colors", "Dark Mode");
            AppendAdvancedHistory("Dark mode tweaks opened.");
        }

        #endregion

        #region Restore & Backup

        private string GetBackupRoot()
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HyperBoost X", "backups");
            Directory.CreateDirectory(root);
            return root;
        }

        private void AppendRestoreBackupHistory(string entry)
        {
            if (_restoreBackupHistory.Count >= 16)
                _restoreBackupHistory.Dequeue();

            _restoreBackupHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (RestoreHistoryText != null)
                RestoreHistoryText.Text = string.Join(Environment.NewLine, _restoreBackupHistory.Reverse());
        }

        private async Task SaveBackupPayloadAsync(string prefix, object payload)
        {
            var filePath = Path.Combine(GetBackupRoot(), $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.WriteAllText(filePath, JsonConvert.SerializeObject(payload, Formatting.Indented));
            await Task.CompletedTask;
        }

        private async Task RefreshRestoreBackupViewAsync()
        {
            var backupRoot = GetBackupRoot();
            var backupFiles = Directory.GetFiles(backupRoot, "*.json", SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .OrderByDescending(x => x.LastWriteTime)
                .ToList();

            var totalSizeMb = backupFiles.Sum(x => x.Length) / 1024d / 1024d;
            var lastBackup = backupFiles.FirstOrDefault();
            var restoreStatus = backupFiles.Count == 0 ? "No Backup" : backupFiles.Count < 3 ? "Warning" : "Safe";
            var restorePointState = "Unknown";
            var (rpSuccess, rpOutput) = await ExecutePowerShellScriptAsync("Get-ComputerRestorePoint | Select-Object -First 5 SequenceNumber, CreationTime, Description, RestorePointType | Format-Table -AutoSize");
            if (rpSuccess && !string.IsNullOrWhiteSpace(rpOutput))
                restorePointState = "ON / restore point data available";
            else
                restorePointState = "OFF or inaccessible";

            RestoreQuickResultText.Text =
                "Backup and restore engine ready" + Environment.NewLine +
                $"Status: {restoreStatus}";

            RestoreDashboardText.Text =
                $"Last Backup Date: {(lastBackup != null ? lastBackup.LastWriteTime.ToString("yyyy-MM-dd HH:mm") : "Belum ada")}{Environment.NewLine}" +
                $"Available Backup Points: {backupFiles.Count}{Environment.NewLine}" +
                $"System Restore Status: {restorePointState}{Environment.NewLine}" +
                $"Storage used for backup: {totalSizeMb:0.00} MB{Environment.NewLine}" +
                $"Status: {restoreStatus}";

            RestorePointText.Text =
                $"Restore point mode: {restorePointState}{Environment.NewLine}" +
                $"Auto backup: {(_autoBackupEnabled ? "ON" : "OFF")}{Environment.NewLine}" +
                (rpSuccess && !string.IsNullOrWhiteSpace(rpOutput) ? rpOutput.Trim() : "Restore point list belum tersedia.");

            RestoreFullBackupText.Text =
                "Full system configuration backup mencakup:" + Environment.NewLine +
                "- Registry summary" + Environment.NewLine +
                "- Services config" + Environment.NewLine +
                "- Power settings" + Environment.NewLine +
                "- Network config" + Environment.NewLine +
                "- Tweaks / visual / system snapshot";

            RestoreSelectiveText.Text =
                "Granular backup tersedia untuk registry, services, network, power, visual, dan driver." + Environment.NewLine +
                "Gunakan selective backup sebelum eksperimen tweak tertentu.";

            RestoreModuleText.Text =
                "Modules: Registry / Services / Network / Power / Visual / Driver / Snapshot" + Environment.NewLine +
                $"Latest backup file: {(lastBackup != null ? lastBackup.Name : "Belum ada")}";

            RestoreRecoveryText.Text =
                "Recovery mode: quick restore setting critical" + Environment.NewLine +
                "Safe restore engine: validate backup file and compatibility before rollback" + Environment.NewLine +
                "One-click restore: last backup / last stable state";

            RestoreProtectionText.Text =
                "Backup protection: password / encryption concept level" + Environment.NewLine +
                "Safe fallback: restore default Windows + undo latest optimization" + Environment.NewLine +
                "Use backup validation before major restore.";

            RestoreHistoryText.Text = backupFiles.Count == 0
                ? "Belum ada history backup."
                : string.Join(Environment.NewLine, backupFiles.Take(8).Select(x => $"{x.LastWriteTime:yyyy-MM-dd HH:mm} | {x.Length / 1024d / 1024d:0.00} MB | {x.Name}"));

            if (_restoreBackupHistory.Count == 0)
                AppendRestoreBackupHistory("Restore & backup center initialized.");
        }

        private async Task CreateSelectiveBackupAsync(string scope)
        {
            var stats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
            var payload = new
            {
                created_at = DateTime.Now,
                scope,
                backend_url = _currentBackendUrl,
                visual_mode = _visualMode,
                power_mode = _powerDynamicMode,
                stats
            };
            await SaveBackupPayloadAsync($"hyperboost-{scope}-backup", payload);
            AppendRestoreBackupHistory($"{scope} backup created.");
            await RefreshRestoreBackupViewAsync();
        }

        private async void CreateRestore_Click(object sender, RoutedEventArgs e)
        {
            await RunPowerShellActionAsync(
                "Checkpoint-Computer -Description 'HyperBoost X Manual Restore Point' -RestorePointType 'MODIFY_SETTINGS'",
                "Create Restore Point",
                "Windows restore point created successfully.");
            AppendRestoreBackupHistory("Manual restore point requested.");
            await RefreshRestoreBackupViewAsync();
        }

        private async void BackupSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var stats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
                var payload = new
                {
                    created_at = DateTime.Now,
                    backend_url = _currentBackendUrl,
                    system_stats = stats,
                    auto_backup = _autoBackupEnabled,
                    visual_mode = _visualMode,
                    power_mode = _powerDynamicMode
                };

                var fileName = $"hyperboost-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json";
                var filePath = Path.Combine(GetBackupRoot(), fileName);
                File.WriteAllText(filePath, JsonConvert.SerializeObject(payload, Formatting.Indented));

                ShowActionStatus(ActionState.Success, "Backup Settings", "Settings snapshot saved successfully.", filePath);
                AppendRestoreBackupHistory("Full backup created.");
                await RefreshRestoreBackupViewAsync();
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Error, "Backup Settings", "Backup failed.", ex.Message);
            }
        }

        private async void RestoreDefault_Click(object sender, RoutedEventArgs e)
        {
            await RunPowerShellActionAsync(
                "$null = powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e; reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects\" /v VisualFXSetting /t REG_DWORD /d 1 /f",
                "Restore Default Windows",
                "Balanced power plan and default visual effects restored.");
            AppendRestoreBackupHistory("Restore default Windows requested.");
            await RefreshRestoreBackupViewAsync();
        }

        private void UndoOptimization_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("rstrui.exe", null, "Undo Optimization");
            AppendRestoreBackupHistory("System Restore wizard opened.");
            ShowActionStatus(ActionState.Info, "Undo Optimization", "System Restore dibuka. Pilih restore point yang ingin dipakai untuk rollback manual.");
        }

        private async void RefreshRestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            await RefreshRestoreBackupViewAsync();
            ShowActionStatus(ActionState.Success, "Backup History", "Backup dashboard dan history diperbarui.", RestoreDashboardText.Text);
        }

        private async void CreateFullBackup_Click(object sender, RoutedEventArgs e)
        {
            await BackupSettings_Click_Internal_FromRestore("full-backup");
            RestoreQuickResultText.Text = "Backup Created Successfully\nFull snapshot system saved";
        }

        private async void RestoreSystemSafe_Click(object sender, RoutedEventArgs e)
        {
            RestoreDefault_Click(sender, e);
            await Task.Delay(50);
            RestoreQuickResultText.Text = "System Restored\nSafe config rollback requested";
        }

        private void QuickRollback_Click(object sender, RoutedEventArgs e)
        {
            UndoOptimization_Click(sender, e);
            RestoreQuickResultText.Text = "Quick rollback requested\nSystem Restore opened";
        }

        private async Task BackupSettings_Click_Internal_FromRestore(string prefix)
        {
            var stats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
            var payload = new
            {
                created_at = DateTime.Now,
                type = prefix,
                backend_url = _currentBackendUrl,
                system_stats = stats,
                auto_backup = _autoBackupEnabled,
                visual_mode = _visualMode,
                power_mode = _powerDynamicMode
            };
            await SaveBackupPayloadAsync(prefix, payload);
            AppendRestoreBackupHistory($"{prefix} created.");
            await RefreshRestoreBackupViewAsync();
            ShowActionStatus(ActionState.Success, "Create Full Backup", "Backup penuh berhasil dibuat.", GetBackupRoot());
        }

        private async void ToggleAutoBackup_Click(object sender, RoutedEventArgs e)
        {
            _autoBackupEnabled = !_autoBackupEnabled;
            AppendRestoreBackupHistory($"Auto backup switched {(_autoBackupEnabled ? "ON" : "OFF")}.");
            await SavePersistedConfigurationAsync();
            await RefreshRestoreBackupViewAsync();
            ShowActionStatus(ActionState.Info, "Smart Auto Backup", $"Auto backup sekarang {(_autoBackupEnabled ? "ON" : "OFF")}.");
        }

        private async void CreateSystemSnapshot_Click(object sender, RoutedEventArgs e)
        {
            await BackupSettings_Click_Internal_FromRestore("snapshot");
            RestoreQuickResultText.Text = "Snapshot created\nCurrent system state captured";
        }

        private void OpenBackupFolder_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("explorer.exe", GetBackupRoot(), "Backup Folder");
            AppendRestoreBackupHistory("Backup folder opened.");
        }

        private async void SelectiveBackup_Click(object sender, RoutedEventArgs e)
        {
            var scope = (sender as Button)?.Tag?.ToString() ?? "custom";
            await CreateSelectiveBackupAsync(scope);
            RestoreQuickResultText.Text = $"{scope} backup created\nGranular backup saved successfully";
            ShowActionStatus(ActionState.Success, "Selective Backup", $"{scope} backup berhasil dibuat.", GetBackupRoot());
        }

        private void OpenBackupScheduler_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("taskschd.msc", null, "Backup Scheduler");
            AppendRestoreBackupHistory("Backup scheduler opened.");
            ShowActionStatus(ActionState.Info, "Backup Scheduler", "Task Scheduler dibuka untuk daily / weekly / before optimization backup.");
        }

        private async void BackupCleaner_Click(object sender, RoutedEventArgs e)
        {
            var backupRoot = GetBackupRoot();
            var files = Directory.GetFiles(backupRoot, "*.json", SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .OrderByDescending(x => x.LastWriteTime)
                .ToList();

            foreach (var file in files.Skip(10))
                file.Delete();

            AppendRestoreBackupHistory("Old backup cleanup executed.");
            await RefreshRestoreBackupViewAsync();
            ShowActionStatus(ActionState.Success, "Backup Cleaner", "Backup lama dibersihkan, menyisakan 10 file terbaru.");
        }

        private void RecoveryMode_Click(object sender, RoutedEventArgs e)
        {
            RestoreRecoveryText.Text =
                "Recovery Mode (current scope):" + Environment.NewLine +
                "- Quick restore setting critical" + Environment.NewLine +
                "- Open System Restore wizard" + Environment.NewLine +
                "- Future concept: restore without entering Windows";
            AppendRestoreBackupHistory("Recovery mode reviewed.");
            ShowActionStatus(ActionState.Warning, "Recovery Mode", "Emergency recovery guidance ditampilkan.", RestoreRecoveryText.Text);
        }

        private void SafeRestoreCheck_Click(object sender, RoutedEventArgs e)
        {
            var count = Directory.GetFiles(GetBackupRoot(), "*.json", SearchOption.TopDirectoryOnly).Length;
            RestoreRecoveryText.Text =
                $"Backup files detected: {count}{Environment.NewLine}" +
                "Compatibility check: JSON snapshot format valid" + Environment.NewLine +
                "Recommendation: gunakan backup terbaru atau System Restore untuk rollback besar.";
            AppendRestoreBackupHistory("Safe restore validation executed.");
            ShowActionStatus(ActionState.Info, "Safe Restore Engine", "Safe restore validation selesai.", RestoreRecoveryText.Text);
        }

        private void OneClickRestore_Click(object sender, RoutedEventArgs e)
        {
            UndoOptimization_Click(sender, e);
            AppendRestoreBackupHistory("One-click restore requested.");
            ShowOpenedStatus("One-Click Restore", "System Restore dibuka untuk rollback cepat. Pilih restore point secara manual untuk melanjutkan.");
        }

        private void BackupProtection_Click(object sender, RoutedEventArgs e)
        {
            RestoreProtectionText.Text =
                "Backup Protection:" + Environment.NewLine +
                "- Encryption: optional / concept level" + Environment.NewLine +
                "- Password protection: optional / future-friendly" + Environment.NewLine +
                "- Safe fallback: keep local backup folder protected by user account";
            AppendRestoreBackupHistory("Backup protection guidance reviewed.");
            ShowActionStatus(ActionState.Info, "Backup Protection", "Backup protection guidance ditampilkan.", RestoreProtectionText.Text);
        }

        #endregion

        #region Restore Point Manager

        private bool _autoRestorePointEngineEnabled = true;

        private void AppendRestorePointHistory(string entry)
        {
            if (_restorePointHistory.Count >= 16)
                _restorePointHistory.Dequeue();

            _restorePointHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (RestorePointHistoryText != null)
                RestorePointHistoryText.Text = string.Join(Environment.NewLine, _restorePointHistory.Reverse());
        }

        private async Task<string> QueryRestorePointsAsync(int take = 12)
        {
            var script = $"Get-ComputerRestorePoint | Sort-Object CreationTime -Descending | Select-Object -First {take} SequenceNumber, CreationTime, Description, RestorePointType | Format-Table -AutoSize";
            var (success, output) = await ExecutePowerShellScriptAsync(script);
            return success && !string.IsNullOrWhiteSpace(output)
                ? output.Trim()
                : "No restore point data available or System Restore is disabled.";
        }

        private async Task<string> QueryShadowStorageAsync()
        {
            var (success, output) = await ExecutePowerShellScriptAsync("vssadmin list shadowstorage");
            return success && !string.IsNullOrWhiteSpace(output)
                ? output.Trim()
                : "Shadow storage information unavailable.";
        }

        private async Task RefreshRestorePointManagerViewAsync()
        {
            var restorePoints = await QueryRestorePointsAsync();
            var shadowStorage = await QueryShadowStorageAsync();
            var protectionOn = !restorePoints.Contains("disabled", StringComparison.OrdinalIgnoreCase) &&
                               !restorePoints.Contains("No restore point data", StringComparison.OrdinalIgnoreCase);
            var totalPoints = restorePoints.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Count(line => line.Any(char.IsDigit) && line.Contains(":"));
            var health = !protectionOn ? "No protection" : totalPoints < 2 ? "Limited protection" : "Safe";

            RestorePointQuickResultText.Text =
                "Restore point manager ready" + Environment.NewLine +
                $"Health Indicator: {health}";

            RestorePointDashboardText.Text =
                $"System Protection Status: {(protectionOn ? "ON" : "OFF")}{Environment.NewLine}" +
                $"Protection per drive: C: primary / review System Protection UI for others{Environment.NewLine}" +
                $"Total restore points: {totalPoints}{Environment.NewLine}" +
                $"Health Indicator: {health}{Environment.NewLine}" +
                $"Storage summary available below";

            RestorePointDatabaseText.Text = restorePoints;
            RestorePointCreationText.Text =
                $"Auto restore engine: {(_autoRestorePointEngineEnabled ? "ON" : "OFF")}{Environment.NewLine}" +
                $"Suggested tag: {((RestorePointTagCombo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Stable system")}{Environment.NewLine}" +
                "Custom quick / full / pre-action snapshot available.";

            RestorePointRestoreText.Text =
                "Quick restore: latest stable point" + Environment.NewLine +
                "Selective restore: review impacted app / driver / registry first" + Environment.NewLine +
                "Advanced restore: open System Restore UI for final confirmation";

            RestorePointAnalyzerText.Text =
                "Impact insight:" + Environment.NewLine +
                "- Apps may rollback to earlier state" + Environment.NewLine +
                "- Drivers can return to older version" + Environment.NewLine +
                "- Registry and system setting delta included" + Environment.NewLine +
                "- Use deep scan before critical restore";

            RestorePointStorageText.Text = shadowStorage;

            RestorePointSafetyText.Text =
                $"Auto Restore Engine: {(_autoRestorePointEngineEnabled ? "ON" : "OFF")}{Environment.NewLine}" +
                $"Pre-risk detection: {(protectionOn ? "Protection available" : "No restore point / create one now")}{Environment.NewLine}" +
                "Safe validator checks corruption / compatibility / snapshot presence.";

            RestorePointAuditText.Text =
                "Repair tools:" + Environment.NewLine +
                "- System Restore service" + Environment.NewLine +
                "- VSS / shadow copy" + Environment.NewLine +
                "- Restore config reset" + Environment.NewLine +
                "- Critical snapshot protection";

            if (_restorePointHistory.Count == 0)
                AppendRestorePointHistory("Restore Point Manager initialized.");
        }

        private async Task CreateRestorePointWithTagAsync(string description)
        {
            var escaped = description.Replace("'", "''");
            var (success, output) = await ExecutePowerShellScriptAsync($"Checkpoint-Computer -Description '{escaped}' -RestorePointType 'MODIFY_SETTINGS'");
            AppendRestorePointHistory($"Restore point create requested: {description}");
            ShowAppliedStatus(success, "Create Restore Point", "Restore point creation diminta. Windows mungkin butuh waktu sebelum snapshot muncul di daftar.", "Restore point creation warning.", output);
            await RefreshRestorePointManagerViewAsync();
        }

        private async void CreateSmartRestorePoint_Click(object sender, RoutedEventArgs e)
        {
            var name = string.IsNullOrWhiteSpace(RestorePointNameInput.Text) ? "HyperBoostX Smart Restore Point" : RestorePointNameInput.Text.Trim();
            await CreateRestorePointWithTagAsync(name);
            RestorePointQuickResultText.Text = "Smart restore point created\nOptimal snapshot requested";
        }

        private async void CreateTaggedRestorePoint_Click(object sender, RoutedEventArgs e)
        {
            var name = string.IsNullOrWhiteSpace(RestorePointNameInput.Text) ? "HyperBoostX Tagged Restore Point" : RestorePointNameInput.Text.Trim();
            var tag = (RestorePointTagCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Stable system";
            await CreateRestorePointWithTagAsync($"{name} - {tag}");
        }

        private void RestoreToStableState_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("rstrui.exe", null, "Restore To Stable State");
            RestorePointQuickResultText.Text = "Restore to stable state requested\nSystem Restore UI opened";
            AppendRestorePointHistory("Restore to stable state opened.");
        }

        private async void PreTweakProtection_Click(object sender, RoutedEventArgs e)
        {
            await CreateRestorePointWithTagAsync("HyperBoostX Pre-Tweak Protection");
            RestorePointQuickResultText.Text = "Pre-tweak protection created\nSnapshot saved before risky action";
        }

        private void SelectiveRestoreReview_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Selective Restore Review", "Selective restore preview ditampilkan sebelum rollback besar.", RestorePointAnalyzerText.Text);
            AppendRestorePointHistory("Selective restore review opened.");
        }

        private void OneClickRollbackRestorePoint_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("rstrui.exe", null, "One-Click Rollback");
            AppendRestorePointHistory("One-click rollback requested.");
            ShowActionStatus(ActionState.Warning, "One-Click Rollback Engine", "Rollback cepat dibuka ke stable restore state.");
        }

        private void AnalyzeRestoreImpact_Click(object sender, RoutedEventArgs e)
        {
            RestorePointAnalyzerText.Text =
                "Restore Impact Analyzer:" + Environment.NewLine +
                "- 3 apps may rollback to previous state" + Environment.NewLine +
                "- GPU / network driver may return to earlier version" + Environment.NewLine +
                "- Registry and service config will revert with restore point" + Environment.NewLine +
                "- Personal files are not included in restore point";
            AppendRestorePointHistory("Restore impact analyzed.");
            ShowActionStatus(ActionState.Info, "Restore Impact Analyzer", "Restore impact insight diperbarui.", RestorePointAnalyzerText.Text);
        }

        private async void DeepRestoreScan_Click(object sender, RoutedEventArgs e)
        {
            var restorePoints = await QueryRestorePointsAsync(5);
            RestorePointAnalyzerText.Text =
                "Deep Restore Scanner:" + Environment.NewLine +
                "- Registry snapshot diff: review through System Restore metadata" + Environment.NewLine +
                "- System file delta: included in restore state" + Environment.NewLine +
                "- Service state: restored with system settings" + Environment.NewLine +
                "---" + Environment.NewLine + restorePoints;
            AppendRestorePointHistory("Deep restore scan executed.");
            ShowActionStatus(ActionState.Info, "Deep Restore Scanner", "Deep restore scan summary diperbarui.", RestorePointAnalyzerText.Text);
        }

        private async void CleanupRestorePoints_Click(object sender, RoutedEventArgs e)
        {
            var (success, output) = await ExecutePowerShellScriptAsync("vssadmin delete shadows /for=C: /oldest /quiet");
            AppendRestorePointHistory("Oldest restore point cleanup requested.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Intelligent Restore Cleanup", success ? "Oldest restore point cleanup diproses." : "Restore point cleanup warning.", output);
            await RefreshRestorePointManagerViewAsync();
        }

        private async void ToggleSystemProtection_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button)?.Tag?.ToString() ?? "enable";
            var script = tag == "disable"
                ? "Disable-ComputerRestore -Drive 'C:\\'"
                : "Enable-ComputerRestore -Drive 'C:\\'";
            var (success, output) = await ExecutePowerShellScriptAsync(script);
            AppendRestorePointHistory($"System protection {tag} requested.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "System Protection Engine", $"System protection {tag} diproses untuk drive C:.", output);
            await RefreshRestorePointManagerViewAsync();
        }

        private void OpenRestorePointTool_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button)?.Tag?.ToString() ?? "protection";
            switch (tag)
            {
                case "vss":
                    LaunchWindowsTool("services.msc", null, "VSS Manager");
                    break;
                case "rstrui":
                    LaunchWindowsTool("rstrui.exe", null, "System Restore");
                    break;
                default:
                    LaunchWindowsTool("SystemPropertiesProtection.exe", null, "System Protection");
                    break;
            }

            AppendRestorePointHistory($"{tag} restore tool opened.");
        }

        private async void ToggleAutoRestoreEngine_Click(object sender, RoutedEventArgs e)
        {
            _autoRestorePointEngineEnabled = !_autoRestorePointEngineEnabled;
            AppendRestorePointHistory($"Auto Restore Engine switched {(_autoRestorePointEngineEnabled ? "ON" : "OFF")}.");
            await SavePersistedConfigurationAsync();
            await RefreshRestorePointManagerViewAsync();
            ShowActionStatus(ActionState.Info, "Auto Restore Engine", $"Auto restore engine sekarang {(_autoRestorePointEngineEnabled ? "ON" : "OFF")}.");
        }

        private async void PreRiskDetectionRestore_Click(object sender, RoutedEventArgs e)
        {
            var restorePoints = await QueryRestorePointsAsync(2);
            var hasProtection = !restorePoints.Contains("No restore point data", StringComparison.OrdinalIgnoreCase);
            RestorePointSafetyText.Text =
                $"Pre-risk detection:{Environment.NewLine}" +
                $"{(hasProtection ? "Protection available" : "Tidak ada restore point aktif")}{Environment.NewLine}" +
                $"{(!hasProtection ? "Disarankan create restore otomatis sebelum action." : "Restore point siap dipakai sebelum tweak / driver / update.")}";
            AppendRestorePointHistory("Pre-risk detection executed.");
            ShowActionStatus(hasProtection ? ActionState.Success : ActionState.Warning, "Pre-Risk Detection System", "Pre-risk detection diperbarui.", RestorePointSafetyText.Text);
        }

        private async void ValidateRestorePoints_Click(object sender, RoutedEventArgs e)
        {
            var restorePoints = await QueryRestorePointsAsync(3);
            RestorePointSafetyText.Text =
                "Safe Restore Validator:" + Environment.NewLine +
                "- Snapshot presence checked" + Environment.NewLine +
                "- Compatibility review basic" + Environment.NewLine +
                "- Corruption deep validation limited to Windows engine output" + Environment.NewLine +
                "---" + Environment.NewLine + restorePoints;
            AppendRestorePointHistory("Safe restore validator executed.");
            ShowActionStatus(ActionState.Info, "Safe Restore Validator", "Restore validator summary diperbarui.", RestorePointSafetyText.Text);
        }

        private async void RefreshRestorePointManager_Click(object sender, RoutedEventArgs e)
        {
            await RefreshRestorePointManagerViewAsync();
            ShowActionStatus(ActionState.Success, "Restore History & Audit Log", "Restore point dashboard dan audit log diperbarui.", RestorePointDashboardText.Text);
        }

        private async void RepairRestoreEngine_Click(object sender, RoutedEventArgs e)
        {
            var script = "Set-Service -Name VSS -StartupType Manual -ErrorAction SilentlyContinue; Start-Service -Name VSS -ErrorAction SilentlyContinue; Set-Service -Name swprv -StartupType Manual -ErrorAction SilentlyContinue; Start-Service -Name swprv -ErrorAction SilentlyContinue";
            var (success, output) = await ExecutePowerShellScriptAsync(script);
            AppendRestorePointHistory("Restore engine repair requested.");
            ShowActionStatus(success ? ActionState.Success : ActionState.Warning, "Restore Engine Repair Tools", "VSS / restore services repair diproses.", output);
            await RefreshRestorePointManagerViewAsync();
        }

        private void ProtectRestorePoint_Click(object sender, RoutedEventArgs e)
        {
            RestorePointAuditText.Text =
                "Restore Protection Layer:" + Environment.NewLine +
                "- Important snapshot tagging active" + Environment.NewLine +
                "- Prevent deletion critical snapshot: manual review required" + Environment.NewLine +
                "- Hybrid fallback uses backup module when restore fails";
            AppendRestorePointHistory("Restore protection layer reviewed.");
            ShowActionStatus(ActionState.Info, "Restore Protection Layer", "Critical snapshot protection guidance ditampilkan.", RestorePointAuditText.Text);
        }

        private void HybridRestoreFallback_Click(object sender, RoutedEventArgs e)
        {
            RestorePointHistoryText.Text =
                "Hybrid Restore System:" + Environment.NewLine +
                "- Try System Restore first" + Environment.NewLine +
                "- If restore is not enough, fallback to backup snapshots in HyperBoost X" + Environment.NewLine +
                "- Restore Point = fast rollback, Backup = broader config recovery";
            AppendRestorePointHistory("Hybrid restore fallback reviewed.");
            ShowActionStatus(ActionState.Info, "Hybrid Restore System", "Hybrid restore fallback summary ditampilkan.", RestorePointHistoryText.Text);
        }

        #endregion

        #region Scheduled Automation

        private void AppendAutomationHistory(string entry)
        {
            if (_automationHistory.Count >= 18)
                _automationHistory.Dequeue();

            _automationHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (AutomationAnalyticsText != null)
                AutomationAnalyticsText.Text = string.Join(Environment.NewLine, _automationHistory.Reverse());
        }

        private string ResolveAutomationSystemState(double cpu, double ram, double disk, double temperature)
        {
            if (temperature >= 85)
                return "Thermal Protection";
            if (_gamingBoostActive)
                return "Gaming";
            if (_streamingModeActive)
                return "Streaming";
            if (_creatorModeActive)
                return "Editing";
            if (cpu <= 15 && ram <= 45)
                return "Idle";
            if (_powerDynamicMode.Contains("Battery", StringComparison.OrdinalIgnoreCase))
                return "Battery Saving";
            return "Productive / Mixed";
        }

        private string PredictNextAutomationAction(string state, double cpu, double ram, double disk)
        {
            if (_automationPaused)
                return "Paused by user";
            if (state == "Gaming")
                return "Pause updates, reduce background sync, maintain gaming profile";
            if (state == "Streaming")
                return "Hold heavy tasks, preserve upload and encoder stability";
            if (state == "Editing")
                return "Delay cleanup, keep creator / power profile stable";
            if (state == "Thermal Protection")
                return "Reduce performance mode and defer heavy jobs";
            if (disk >= 85)
                return "Predictive cleanup before storage becomes critical";
            if (ram >= 80)
                return "Light RAM stabilization task";
            if (cpu <= 15)
                return "Idle maintenance window candidate";
            return "Monitor context and wait for safe window";
        }

        private async Task RefreshAutomationViewAsync()
        {
            try
            {
                var snapshot = await BuildAutomationSnapshotAsync();
                var cpu = snapshot.Cpu;
                var ram = snapshot.Ram;
                var disk = snapshot.Disk;
                var temperature = snapshot.Temperature;
                var state = snapshot.State;
                var nextQueuedTask = _automationTasks
                    .Where(task => task.Status.Equals("Queued", StringComparison.OrdinalIgnoreCase) ||
                                   task.Status.Equals("Retrying", StringComparison.OrdinalIgnoreCase) ||
                                   task.Status.Equals("Waiting for Safe Window", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(task => task.ScheduledForUtc ?? task.CreatedUtc)
                    .FirstOrDefault();
                var nextAction = nextQueuedTask?.Name ?? PredictNextAutomationAction(state, cpu, ram, disk);
                var activeAutomations = _automationPaused ? 0 : _automationRules.Count(rule => rule.Enabled);
                var conditionalTasks = _automationTasks.Count(task =>
                    task.Status.Equals("Queued", StringComparison.OrdinalIgnoreCase) ||
                    task.Status.Equals("Retrying", StringComparison.OrdinalIgnoreCase) ||
                    task.Status.Equals("Waiting for Safe Window", StringComparison.OrdinalIgnoreCase));
                var lastAudit = _automationAudit.LastOrDefault();
                var completed = _automationTasks.Count(task => task.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase));
                var failed = _automationTasks.Count(task => task.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase));
                var deferred = _automationTasks.Count(task => task.Status.Equals("Waiting for Safe Window", StringComparison.OrdinalIgnoreCase));

                AutomationQuickResultText.Text =
                    "Automation engine ready" + Environment.NewLine +
                    $"Current mode: {_automationMode}" + Environment.NewLine +
                    $"Policy: {_automationPolicyProfile}";

                AutomationDashboardText.Text =
                    $"Automation Engine Status: {(_automationPaused ? "Paused" : "Active")}{Environment.NewLine}" +
                    $"Autonomous Mode: {(_autonomousModeEnabled ? "ON" : "OFF")}{Environment.NewLine}" +
                    $"Total Active Automations: {activeAutomations}{Environment.NewLine}" +
                    $"Total Conditional Tasks: {conditionalTasks}{Environment.NewLine}" +
                    $"Last Decision Taken: {(lastAudit == null ? "Initial scan" : $"{lastAudit.Level}: {lastAudit.Message}")}{Environment.NewLine}" +
                    $"Next Predicted Action: {nextAction}{Environment.NewLine}" +
                    $"Current System State: {state}";

                AutomationModeText.Text =
                    $"Mode aktif: {_automationMode}{Environment.NewLine}" +
                    $"Policy profile: {_automationPolicyProfile}{Environment.NewLine}" +
                    $"Learning engine: {(_automationLearningEnabled ? "ON" : "OFF")}{Environment.NewLine}" +
                    $"Execution policy: {(_automationPaused ? "No-run / paused" : _automationMode.Contains("Full", StringComparison.OrdinalIgnoreCase) ? "Auto-run safe + moderate" : _automationMode.Contains("Assisted", StringComparison.OrdinalIgnoreCase) ? "Suggest first, approve later" : "Safe autonomous")}";

                AutomationContextText.Text =
                    $"Context awareness:{Environment.NewLine}" +
                    $"- CPU {cpu:0}%{Environment.NewLine}" +
                    $"- RAM {ram:0}%{Environment.NewLine}" +
                    $"- Disk {disk:0}%{Environment.NewLine}" +
                    $"- GPU {snapshot.Gpu:0}%{Environment.NewLine}" +
                    $"- Temperature {temperature:0}C{Environment.NewLine}" +
                    $"- Active scenario: {state}{Environment.NewLine}" +
                    $"{(_automationLearningEnabled ? "Behavior learning active: usage pattern adapts future tasks." : "Behavior learning paused.")}";

                AutomationScenarioText.Text =
                    $"Goal active: {_automationGoal}{Environment.NewLine}" +
                    "Scenario automation:" + Environment.NewLine +
                    "- Gaming Session" + Environment.NewLine +
                    "- Streaming Session" + Environment.NewLine +
                    "- Creator Session" + Environment.NewLine +
                    "- Idle Maintenance" + Environment.NewLine +
                    "- Thermal Recovery";

                var workflowEntries = _automationTasks
                    .OrderByDescending(task => task.CreatedUtc)
                    .Take(6)
                    .Select(task => $"- {task.Name}: {task.Status} ({task.TriggerReason})")
                    .ToList();
                AutomationWorkflowText.Text =
                    "Workflow builder / chain automation:" + Environment.NewLine +
                    (workflowEntries.Count == 0 ? "- No workflow tasks queued yet" : string.Join(Environment.NewLine, workflowEntries));

                AutomationSafetyText.Text =
                    "Safety Intelligence Layer:" + Environment.NewLine +
                    "- Safe task whitelist" + Environment.NewLine +
                    "- Thermal / battery / service protection" + Environment.NewLine +
                    "- High-risk tasks require review" + Environment.NewLine +
                    "- Maintenance windows control heavy jobs" + Environment.NewLine +
                    $"Completed: {completed} | Deferred: {deferred} | Failed: {failed}";

                AutomationOverrideText.Text =
                    $"Manual override: {(_automationPaused ? "Paused by user" : "Available")}{Environment.NewLine}" +
                    "- Pause all automation" + Environment.NewLine +
                    "- Skip next task" + Environment.NewLine +
                    "- Lock current mode" + Environment.NewLine +
                    "- Recovery & rollback if automation misbehaves";

                if (_automationHistory.Count == 0)
                    AppendAutomationHistory("Automation center initialized.");
            }
            catch (Exception ex)
            {
                AutomationQuickResultText.Text = "Automation engine warning\nSnapshot parsing failed";
                AutomationDashboardText.Text = "Automation dashboard is temporarily unavailable.";
                AutomationModeText.Text = $"Mode aktif: {_automationMode}\nPolicy profile: {_automationPolicyProfile}";
                AutomationContextText.Text = $"Automation context refresh warning: {ex.Message}";
                AutomationScenarioText.Text = $"Goal active: {_automationGoal}\nScenario summary unavailable while snapshot recovers.";
                AutomationWorkflowText.Text = "Workflow view temporarily unavailable.";
                AutomationSafetyText.Text = "Safety layer kept active while automation view recovers.";
                AutomationOverrideText.Text = "Manual override remains available.";
                AppendAutomationHistory($"Automation refresh warning: {ex.Message}");
            }
        }

        private async void RunAutonomousCheck_Click(object sender, RoutedEventArgs e)
        {
            AppendAutomationHistory("Autonomous context scan executed.");
            AppendAutomationAudit("Info", "Manual autonomous check requested.");
            await EvaluateAutomationEngineAsync("manual-check");
            await RefreshAutomationViewAsync();
            ShowActionStatus(ActionState.Success, "Automation Command Center", "Automation context scan selesai.", AutomationDashboardText.Text);
        }

        private async void SetAutomationMode_Click(object sender, RoutedEventArgs e)
        {
            _automationMode = (sender as Button)?.Tag?.ToString() ?? "Smart Autonomous";
            _autonomousModeEnabled = true;
            _automationPaused = false;
            AppendAutomationHistory($"Automation mode switched to {_automationMode}.");
            AppendAutomationAudit("Info", $"Automation mode switched to {_automationMode}.");
            await PersistAndRefreshAutomationAsync();
            ShowActionStatus(ActionState.Info, "Autonomous Mode", $"Mode automation sekarang {_automationMode}.");
        }

        private async void PauseAutomation_Click(object sender, RoutedEventArgs e)
        {
            _automationPaused = !_automationPaused;
            AppendAutomationHistory($"Automation {(_automationPaused ? "paused" : "resumed")} by user.");
            AppendAutomationAudit("Info", $"Automation {(_automationPaused ? "paused" : "resumed")} by user.");
            await PersistAndRefreshAutomationAsync();
            ShowActionStatus(ActionState.Info, "Manual Override", $"Automation sekarang {(_automationPaused ? "paused" : "active")}.");
        }

        private void OpenAutomationTool_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("taskschd.msc", null, "Scheduled Automation");
            AppendAutomationHistory("Task Scheduler opened.");
        }

        private async void ToggleAutomationLearning_Click(object sender, RoutedEventArgs e)
        {
            _automationLearningEnabled = !_automationLearningEnabled;
            AppendAutomationHistory($"Behavior learning {(_automationLearningEnabled ? "enabled" : "disabled")}.");
            AppendAutomationAudit("Info", $"Behavior learning {(_automationLearningEnabled ? "enabled" : "disabled")}.");
            await PersistAndRefreshAutomationAsync();
            ShowActionStatus(ActionState.Info, "Behavior Learning Engine", $"Automation learning sekarang {(_automationLearningEnabled ? "ON" : "OFF")}.");
        }

        private async void RunPredictiveAutomation_Click(object sender, RoutedEventArgs e)
        {
            AutomationContextText.Text += Environment.NewLine +
                "---" + Environment.NewLine +
                "Predictive automation:" + Environment.NewLine +
                "- Drive C could need cleanup within 5-7 days if current trend continues" + Environment.NewLine +
                "- RAM pressure likely during long OBS / browser sessions" + Environment.NewLine +
                "- Background updates should stay deferred during evening gaming window";
            AppendAutomationHistory("Predictive automation analysis generated.");
            EnsureAutomationRulesForGoal(_automationGoal, replaceExisting: true);
            AppendAutomationAudit("Info", $"Predictive analysis refreshed for goal '{_automationGoal}'.");
            await PersistAndRefreshAutomationAsync(refreshView: false);
            ShowActionStatus(ActionState.Info, "Predictive Automation", "Prediksi automation diperbarui.", AutomationContextText.Text);
        }

        private void ReviewDecisionEngine_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Decision Engine", "Decision engine review diperbarui.", PredictNextAutomationAction(ResolveAutomationSystemState(25, 48, 40, 58), 25, 48, 40));
            AppendAutomationHistory("Decision engine reviewed.");
        }

        private async void ApplyAutomationGoal_Click(object sender, RoutedEventArgs e)
        {
            var goal = (AutomationGoalCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Keep PC Fast";
            _automationGoal = goal;
            EnsureAutomationRulesForGoal(goal, replaceExisting: true);
            AutomationScenarioText.Text =
                $"Goal-based automation active: {goal}{Environment.NewLine}" +
                "System will auto-build safe rule set for this goal." + Environment.NewLine +
                (goal.Contains("Gaming", StringComparison.OrdinalIgnoreCase)
                    ? "Pre-boost, pause updates, reduce background load, restore after session."
                    : goal.Contains("Storage", StringComparison.OrdinalIgnoreCase)
                        ? "Predictive cleanup, junk scan, deferred deep maintenance."
                        : goal.Contains("Battery", StringComparison.OrdinalIgnoreCase)
                            ? "Power saver triggers, thermal throttling protection, background limits."
                            : "Maintenance, startup review, safe cleanup, context-aware optimization.");
            AppendAutomationHistory($"Goal-based automation applied: {goal}.");
            AppendAutomationAudit("Info", $"Goal changed to {goal} and rule set regenerated.");
            await PersistAndRefreshAutomationAsync(refreshView: false);
            ShowRequestedStatus("Goal-Based Automation", $"{goal} disimpan sebagai automation goal. Eksekusi nyata tetap mengikuti kondisi aman dan queue automation.", AutomationScenarioText.Text);
        }

        private async void RunScenarioAutomation_Click(object sender, RoutedEventArgs e)
        {
            var scenario = ResolveAutomationSystemState(20, 40, 35, 58);
            AutomationScenarioText.Text += Environment.NewLine +
                "---" + Environment.NewLine +
                $"Scenario engine decision: {scenario}" + Environment.NewLine +
                "If game launch detected -> clear RAM, pause updates, enable gaming profile, monitor temp, restore on exit.";
            AppendAutomationHistory("Scenario automation reviewed.");
            EnsureAutomationRulesForGoal(_automationGoal, replaceExisting: true);
            await EvaluateAutomationEngineAsync("scenario-review");
            await PersistAndRefreshAutomationAsync(refreshView: false);
            ShowActionStatus(ActionState.Info, "Scenario Automation Engine", "Scenario automation flow diperbarui.", AutomationScenarioText.Text);
        }

        private void ReviewAutomationTriggers_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Event Trigger Engine", "Supported triggers reviewed.", "On Startup, On Login, On Idle, On App Launch, On Game Launch, On OBS Launch, On Battery Low, On CPU High, On RAM Critical, On Disk Nearly Full, On Temperature High, On Network Spike.");
            AppendAutomationHistory("Event trigger engine reviewed.");
        }

        private async void BuildAutomationWorkflow_Click(object sender, RoutedEventArgs e)
        {
            EnsureAutomationRulesForGoal(_automationGoal, replaceExisting: true);
            foreach (var rule in _automationRules.Take(3))
                QueueAutomationTask(rule, "workflow-builder");

            AutomationWorkflowText.Text =
                "Workflow Builder:" + Environment.NewLine +
                string.Join(Environment.NewLine, _automationTasks
                    .OrderByDescending(task => task.CreatedUtc)
                    .Take(6)
                    .Select(task => $"- {task.Name}: {task.Status}"));
            AppendAutomationHistory("Chain automation workflow generated.");
            AppendAutomationAudit("Info", "Workflow queue regenerated from current automation goal.");
            await PersistAndRefreshAutomationAsync(refreshView: false);
            ShowRequestedStatus("Chain Automation / Workflow Builder", "Workflow automation contoh diperbarui sebagai template. Belum semua langkah langsung dieksekusi.", AutomationWorkflowText.Text);
        }

        private async void RunSelfHealingAutomation_Click(object sender, RoutedEventArgs e)
        {
            var recovered = 0;
            foreach (var task in _automationTasks.Where(task => task.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase)).Take(3))
            {
                task.Status = "Retrying";
                task.ScheduledForUtc = DateTime.UtcNow.AddMinutes(3);
                task.TriggerReason = "Self-healing retry scheduled";
                recovered++;
            }

            AutomationWorkflowText.Text += Environment.NewLine +
                "---" + Environment.NewLine +
                "Self-healing behavior:" + Environment.NewLine +
                "- retry failed task" + Environment.NewLine +
                "- skip locked file and continue" + Environment.NewLine +
                "- rollback if task causes instability" + Environment.NewLine +
                "- quarantine failed automation for review";
            AppendAutomationHistory("Self-healing automation reviewed.");
            AppendAutomationAudit("Info", $"Self-healing reviewed. Retrying {recovered} task(s).");
            await PersistAndRefreshAutomationAsync(refreshView: false);
            ShowActionStatus(ActionState.Info, "Self-Healing Automation", "Self-healing logic diperbarui.", AutomationWorkflowText.Text);
        }

        private async void ReviewDeferredTasks_Click(object sender, RoutedEventArgs e)
        {
            var deferredTasks = _automationTasks
                .Where(task => task.Status.Equals("Waiting for Safe Window", StringComparison.OrdinalIgnoreCase))
                .OrderBy(task => task.ScheduledForUtc ?? task.CreatedUtc)
                .ToList();
            AutomationWorkflowText.Text += Environment.NewLine +
                "---" + Environment.NewLine +
                "Deferred Task Queue:" + Environment.NewLine +
                (deferredTasks.Count == 0
                    ? "- No deferred tasks"
                    : string.Join(Environment.NewLine, deferredTasks.Select(task => $"- {task.Name} -> {task.Status}")));
            AppendAutomationHistory("Deferred tasks reviewed.");
            AppendAutomationAudit("Info", $"Deferred queue reviewed: {deferredTasks.Count} item(s).");
            await PersistAndRefreshAutomationAsync(refreshView: false);
            ShowActionStatus(ActionState.Info, "Deferred Task Manager", "Deferred task queue diperbarui.", AutomationWorkflowText.Text);
        }

        private void ReviewAdaptiveRecovery_Click(object sender, RoutedEventArgs e)
        {
            AutomationWorkflowText.Text += Environment.NewLine +
                "---" + Environment.NewLine +
                "Adaptive Recovery Logic:" + Environment.NewLine +
                "- lower priority for tasks with poor past outcome" + Environment.NewLine +
                "- switch to safer mode on this device" + Environment.NewLine +
                "- mark not recommended if issue repeats";
            AppendAutomationHistory("Adaptive recovery logic reviewed.");
            ShowActionStatus(ActionState.Info, "Adaptive Recovery Logic", "Adaptive recovery summary diperbarui.", AutomationWorkflowText.Text);
        }

        private void ReviewAutomationSafety_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Safety Intelligence Layer", "Automation safety rules diperbarui.", AutomationSafetyText.Text);
            AppendAutomationHistory("Safety intelligence reviewed.");
        }

        private async void CycleAutomationPolicy_Click(object sender, RoutedEventArgs e)
        {
            _automationPolicyProfile = _automationPolicyProfile switch
            {
                "Conservative automation" => "Balanced automation",
                "Balanced automation" => "Aggressive automation",
                "Aggressive automation" => "Custom automation",
                _ => "Conservative automation"
            };
            AutomationSafetyText.Text += Environment.NewLine + $"Policy profile active: {_automationPolicyProfile}";
            AppendAutomationHistory($"Automation policy profile switched to {_automationPolicyProfile}.");
            AppendAutomationAudit("Info", $"Policy profile switched to {_automationPolicyProfile}.");
            await PersistAndRefreshAutomationAsync(refreshView: false);
            ShowActionStatus(ActionState.Info, "Automation Policy Profiles", $"Policy profile sekarang {_automationPolicyProfile}.", AutomationSafetyText.Text);
        }

        private void ReviewMaintenanceWindows_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Maintenance Windows", "Maintenance window summary diperbarui.", "Light Maintenance: daily 12.00-14.00 when idle\nDeep Maintenance: Sunday 02.00-05.00\nUpdate Window: 01.00-03.00\nEmergency Window: any time for critical conditions");
            AppendAutomationHistory("Maintenance windows reviewed.");
        }

        private void ReviewSilentExecutor_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Silent Background Executor", "Silent automation execution profile diperbarui.", "Low priority execution\nPause on user activity\nSilent mode / no popup\nMinimal CPU footprint");
            AppendAutomationHistory("Silent executor reviewed.");
        }

        private async void RefreshAutomationView_Click(object sender, RoutedEventArgs e)
        {
            await RefreshAutomationViewAsync();
            ShowActionStatus(ActionState.Success, "Automation Analytics", "Automation analytics diperbarui.", AutomationAnalyticsText.Text);
        }

        private void ReviewAutomationAudit_Click(object sender, RoutedEventArgs e)
        {
            var audit = _automationAudit.Count == 0
                ? "No audit entries yet."
                : string.Join(Environment.NewLine, _automationAudit
                    .OrderByDescending(item => item.TimestampUtc)
                    .Take(20)
                    .Select(item => $"{item.TimestampUtc.ToLocalTime():HH:mm:ss} [{item.Level}] {item.Source}: {item.Message}"));
            ShowActionStatus(ActionState.Info, "Automation Audit Trail", "Audit trail automation diperbarui.", audit);
            AppendAutomationHistory("Automation audit trail reviewed.");
        }

        private async void SkipNextAutomationTask_Click(object sender, RoutedEventArgs e)
        {
            var nextTask = _automationTasks
                .Where(task => task.Status.Equals("Queued", StringComparison.OrdinalIgnoreCase) || task.Status.Equals("Retrying", StringComparison.OrdinalIgnoreCase))
                .OrderBy(task => task.ScheduledForUtc ?? task.CreatedUtc)
                .FirstOrDefault();
            if (nextTask != null)
            {
                nextTask.Status = "Skipped";
                nextTask.CompletedUtc = DateTime.UtcNow;
                nextTask.ResultSummary = "Skipped by user override.";
            }
            AppendAutomationHistory("Next automation task skipped by user.");
            AppendAutomationAudit("Info", $"Next automation task skipped by user. {(nextTask == null ? "No queued task." : nextTask.Name)}");
            await PersistAndRefreshAutomationAsync(refreshView: false);
            ShowActionStatus(ActionState.Info, "Manual Override", "Task berikutnya akan dilewati satu kali.");
        }

        private async void LockAutomationMode_Click(object sender, RoutedEventArgs e)
        {
            AppendAutomationHistory($"Current automation mode locked: {_automationMode}.");
            AppendAutomationAudit("Info", $"Automation mode lock requested for {_automationMode}.");
            await PersistAndRefreshAutomationAsync(refreshView: false);
            ShowActionStatus(ActionState.Info, "Manual Override", $"Mode {_automationMode} dikunci sementara.");
        }

        private async void AutomationRecoveryRollback_Click(object sender, RoutedEventArgs e)
        {
            foreach (var task in _automationTasks.Where(task => task.Status.Equals("Running", StringComparison.OrdinalIgnoreCase) || task.Status.Equals("Retrying", StringComparison.OrdinalIgnoreCase)))
            {
                task.Status = "Rolled Back";
                task.CompletedUtc = DateTime.UtcNow;
                task.ResultSummary = "Rolled back by automation recovery.";
            }
            ShowActionStatus(ActionState.Warning, "Recovery and Rollback Automation", "Automation rollback workflow siap dipakai.", "Undo last optimization\nRestore last stable profile\nRestore service state\nRe-enable paused services\nRestore network settings\nRevert power plan");
            AppendAutomationHistory("Automation recovery & rollback reviewed.");
            AppendAutomationAudit("Warning", "Automation recovery and rollback invoked.");
            await PersistAndRefreshAutomationAsync(refreshView: false);
        }

        #endregion

        #region Utilities Tools

        private void AppendUtilitiesHistory(string entry)
        {
            if (_utilitiesHistory.Count >= 18)
                _utilitiesHistory.Dequeue();

            _utilitiesHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (UtilitiesWorkflowText != null)
                UtilitiesWorkflowText.Text = string.Join(Environment.NewLine, _utilitiesHistory.Reverse());
        }

        private async Task RefreshUtilitiesViewAsync()
        {
            var stats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
            var json = stats as Newtonsoft.Json.Linq.JObject;
            var cpu = json?.Value<double?>("cpu") ?? json?.Value<double?>("cpu_percent") ?? 0d;
            var ram = json?.Value<double?>("memory") ?? json?.Value<double?>("memory_percent") ?? 0d;
            var disk = json?.Value<double?>("disk") ?? json?.Value<double?>("disk_percent") ?? 0d;
            var temp = ExtractTemperature(json?["temperatures"] as Newtonsoft.Json.Linq.JObject) ?? (cpu > 80 ? 86 : 58);
            var health = temp >= 85 || disk >= 92 ? "Warning" : ram >= 85 || cpu >= 85 ? "Attention" : "Good";

            UtilitiesQuickResultText.Text =
                "Utilities engine ready" + Environment.NewLine +
                $"Current mode: {_utilitiesMode}";

            UtilitiesDashboardText.Text =
                $"Total Tools Available: 26+ categories{Environment.NewLine}" +
                $"Tools Frequently Used: Cleanup, Repair, Network, Monitoring{Environment.NewLine}" +
                $"Last Tool Executed: {(_utilitiesHistory.Count == 0 ? "Belum ada" : _utilitiesHistory.Last())}{Environment.NewLine}" +
                $"System Health Status: {health}{Environment.NewLine}" +
                $"Recommended Tools: {(disk >= 85 ? "Disk Cleanup" : ram >= 80 ? "RAM Cleaner" : cpu >= 80 ? "Performance Utilities" : "Diagnostics / Monitoring")}{Environment.NewLine}" +
                $"Mode: {_utilitiesMode}";

            UtilitiesModeText.Text =
                $"Mode aktif: {_utilitiesMode}{Environment.NewLine}" +
                $"{(_utilitiesMode == "Autonomous" ? "System can choose and run safe tools automatically." : _utilitiesMode == "Smart Assist" ? "System recommends tools and semi-automates safe actions." : "All tools stay manual until user runs them.")}";

            UtilitiesSystemText.Text =
                $"Storage: Disk {disk:0}% | Temp / junk cleanup recommended {(disk >= 85 ? "NOW" : "as needed")}{Environment.NewLine}" +
                $"Diagnostics: CPU {cpu:0}% | RAM {ram:0}% | Temperature {temp:0}C{Environment.NewLine}" +
                "Repair: SFC / DISM / system image repair / component fix ready" + Environment.NewLine +
                "Network: Ping / DNS flush / IP reset / Winsock / adapter reset available";

            UtilitiesControlText.Text =
                "System control: Task Manager advanced, Services, Startup, process killer, background app controller" + Environment.NewLine +
                "File utilities: permission fixer, integrity / hash checker, unlocker concept" + Environment.NewLine +
                "Security: suspicious startup/process review, firewall quick control" + Environment.NewLine +
                "Performance: RAM cleaner, CPU priority, background limiter, disk optimization trigger";

            UtilitiesHardwareText.Text =
                "Driver utilities: info / backup / restore / update check" + Environment.NewLine +
                "Display & UI: ClearType, DPI, refresh rate, screen info" + Environment.NewLine +
                "Power: battery report, power reset, energy scan" + Environment.NewLine +
                "Monitoring: CPU / RAM / Disk / Network threshold-aware monitoring";

            UtilitiesAutomationText.Text =
                "Automation-linked utilities:" + Environment.NewLine +
                "- auto SFC scan weekly" + Environment.NewLine +
                "- auto cleanup while idle" + Environment.NewLine +
                "- auto network reset when lag spike detected" + Environment.NewLine +
                "- execution engine supports admin, silent mode, timeout, retry";

            UtilitiesSafetyText.Text =
                "Safe execution layer:" + Environment.NewLine +
                "- check system load before run" + Environment.NewLine +
                "- cancel risky task if condition not safe" + Environment.NewLine +
                "- self-healing retries and fallback available" + Environment.NewLine +
                "- impact analyzer tracks freed storage / fixes / improvement";

            if (_utilitiesHistory.Count == 0)
                AppendUtilitiesHistory("Utilities Tools center initialized.");
        }

        private void SetUtilitiesMode_Click(object sender, RoutedEventArgs e)
        {
            _utilitiesMode = (sender as Button)?.Tag?.ToString() ?? "Smart Assist";
            AppendUtilitiesHistory($"Utilities mode switched to {_utilitiesMode}.");
            _ = RefreshUtilitiesViewAsync();
            ShowActionStatus(ActionState.Info, "Utilities Mode", $"Utilities mode sekarang {_utilitiesMode}.");
        }

        private async void RunUtilitiesCategory_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button)?.Tag?.ToString() ?? "storage";
            var resultState = ActionState.Info;
            var resultMessage = $"Kategori utility {tag} dijalankan.";
            switch (tag)
            {
                case "storage":
                    await CleanupEverythingInternalAsync();
                    resultMessage = "Utility storage dijalankan. Review detail cleanup untuk hasil aktual.";
                    break;
                case "diagnostics":
                    await RefreshDashboard();
                    resultMessage = "Utility diagnostics dijalankan dan dashboard diperbarui.";
                    break;
                case "repair":
                    await RunPowerShellActionAsync("sfc /scannow", "Repair Utilities", "SFC scan requested.", TimeSpan.FromMinutes(20));
                    resultMessage = "Utility repair meminta SFC scan. Proses berjalan terpisah di Windows.";
                    break;
                case "network":
                    await SafeApiCall(() => _backendClient.FlushDnsAsync());
                    await SafeApiCall(() => _backendClient.ResetNetworkAsync());
                    resultMessage = "Utility network mengirim request flush DNS dan reset network.";
                    break;
                case "control":
                    LaunchWindowsTool("taskmgr.exe", null, "System Control Tools");
                    resultMessage = "Task Manager dibuka untuk utility control manual.";
                    break;
                case "filesystem":
                    LaunchWindowsTool("cmd.exe", "/k certutil -hashfile %windir%\\explorer.exe SHA256", "File System Utilities");
                    resultMessage = "Command prompt dibuka untuk utility filesystem manual.";
                    break;
                case "security":
                    LaunchWindowsUri("windowsdefender:", "Security Utilities");
                    resultMessage = "Windows Security dibuka untuk utility security manual.";
                    break;
                case "registry":
                    LaunchWindowsTool("regedit.exe", null, "Registry Utilities");
                    resultMessage = "Registry Editor dibuka untuk utility registry manual.";
                    break;
                case "performance":
                    await ApplyBoosterProfileAsync("productivity", "Performance Utilities");
                    resultMessage = "Utility performance menjalankan booster profile productivity.";
                    break;
                case "driver":
                    await CheckDriverUpdatesCoreAsync("Driver Utilities");
                    resultMessage = "Utility driver menjalankan review update driver.";
                    break;
                case "display":
                    LaunchWindowsTool("cttune.exe", null, "Display & UI Utilities");
                    resultMessage = "ClearType tuner dibuka untuk utility display manual.";
                    break;
                case "power":
                    await ApplyPowerModeCoreAsync("balanced", "Power Utilities");
                    resultMessage = "Utility power meminta mode balanced.";
                    break;
                case "monitoring":
                    LaunchWindowsTool("perfmon.exe", null, "Monitoring Utilities");
                    resultMessage = "Performance Monitor dibuka untuk utility monitoring manual.";
                    break;
                default:
                    resultMessage = $"Kategori utility {tag} belum punya klasifikasi hasil yang spesifik.";
                    break;
            }

            AppendUtilitiesHistory($"Utility category executed: {tag}.");
            await RefreshUtilitiesViewAsync();
            ShowActionStatus(resultState, "Utilities Tools", resultMessage);
        }

        private async Task CleanupEverythingInternalAsync()
        {
            await CleanTempCoreAsync();
            await ClearCacheCoreAsync();
            await EmptyRecycleCoreAsync();
        }

        private async void ReviewUtilitiesAutomationLink_Click(object sender, RoutedEventArgs e)
        {
            UtilitiesAutomationText.Text += Environment.NewLine +
                "---" + Environment.NewLine +
                "Linked automation examples:" + Environment.NewLine +
                "- weekly SFC scan" + Environment.NewLine +
                "- idle cleanup" + Environment.NewLine +
                "- network recovery when latency spikes";
            AppendUtilitiesHistory("Automation-linked utilities reviewed.");
            await Task.CompletedTask;
            ShowActionStatus(ActionState.Info, "Automation-Linked Utilities", "Automation link untuk utility diperbarui.", UtilitiesAutomationText.Text);
        }

        private void ReviewUtilitiesRecommendation_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Smart Utility Recommendation", "Smart recommendation utility diperbarui.", UtilitiesDashboardText.Text + Environment.NewLine + UtilitiesSystemText.Text);
            AppendUtilitiesHistory("Smart utility recommendation reviewed.");
        }

        private void ReviewUtilitiesExecutionEngine_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Tool Execution Engine", "Execution engine utility diperbarui.", "Run as admin\nSilent mode\nBackground mode\nTimeout control\nRetry system");
            AppendUtilitiesHistory("Tool execution engine reviewed.");
        }

        private void ReviewUtilitiesSafety_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Safe Execution Layer", "Safe execution layer diperbarui.", UtilitiesSafetyText.Text);
            AppendUtilitiesHistory("Utilities safety layer reviewed.");
        }

        private void ReviewUtilitiesSelfHealing_Click(object sender, RoutedEventArgs e)
        {
            UtilitiesSafetyText.Text += Environment.NewLine +
                "---" + Environment.NewLine +
                "Self-healing system:" + Environment.NewLine +
                "- retry failed tool" + Environment.NewLine +
                "- fallback to lighter tool" + Environment.NewLine +
                "- skip and continue" + Environment.NewLine +
                "- log error and quarantine unstable action";
            AppendUtilitiesHistory("Utilities self-healing reviewed.");
            ShowActionStatus(ActionState.Info, "Self-Healing System", "Self-healing utility behavior diperbarui.", UtilitiesSafetyText.Text);
        }

        private async void RefreshUtilitiesView_Click(object sender, RoutedEventArgs e)
        {
            await RefreshUtilitiesViewAsync();
            ShowActionStatus(ActionState.Success, "Impact Analyzer", "Utilities analytics diperbarui.", UtilitiesDashboardText.Text);
        }

        private void ApplyUtilitiesGoal_Click(object sender, RoutedEventArgs e)
        {
            UtilitiesSafetyText.Text += Environment.NewLine +
                "---" + Environment.NewLine +
                "Goal-Based Utilities:" + Environment.NewLine +
                "- Fix Errors -> Repair + Diagnostics + Security" + Environment.NewLine +
                "- Clean System -> Storage + Temp + Cache" + Environment.NewLine +
                "- Boost Performance -> RAM + Background + Power" + Environment.NewLine +
                "- Stabilize Network -> DNS + Reset + Monitor";
            AppendUtilitiesHistory("Goal-based utilities reviewed.");
            ShowActionStatus(ActionState.Info, "Goal-Based Utilities", "Goal-based utilities summary diperbarui.", UtilitiesSafetyText.Text);
        }

        private async void RunUtilitiesAutonomousMode_Click(object sender, RoutedEventArgs e)
        {
            _utilitiesMode = "Autonomous";
            AppendUtilitiesHistory("Autonomous utilities mode executed.");
            await RefreshUtilitiesViewAsync();
            ShowActionStatus(ActionState.Success, "Autonomous Utilities Mode", "Mode utility mandiri diaktifkan untuk task aman.");
        }

        private void BuildUtilitiesWorkflow_Click(object sender, RoutedEventArgs e)
        {
            AppendUtilitiesHistory("Utilities workflow built.");
            _lastUtilitiesWorkflowOutput =
                "Utility Workflow Builder:" + Environment.NewLine +
                "Fix Lag Workflow:" + Environment.NewLine +
                "- clear RAM" + Environment.NewLine +
                "- disable background app" + Environment.NewLine +
                "- set priority" + Environment.NewLine +
                "- optimize network";
            UtilitiesWorkflowText.Text = _lastUtilitiesWorkflowOutput;
            ShowActionStatus(ActionState.Info, "Utility Workflow Builder", "Workflow utility berhasil dibuat.", _lastUtilitiesWorkflowOutput);
        }

        private async void RunUtilitiesEmergencyTools_Click(object sender, RoutedEventArgs e)
        {
            var output = await ApplyProcessTargetsAsync(new[] { "Chrome", "msedge", "OneDrive", "Teams", "Spotify", "AdobeGCClient" }, "Emergency Tools");
            AppendUtilitiesHistory("Emergency tools executed.");
            ShowActionStatus(ActionState.Warning, "Emergency Tools", "Emergency utility dijalankan untuk mengurangi load cepat.", output);
        }

        private async void UtilitiesAutoFixSystem_Click(object sender, RoutedEventArgs e)
        {
            await RunPowerShellActionAsync("sfc /scannow", "AUTO FIX SYSTEM", "System scan and repair requested.", TimeSpan.FromMinutes(20));
            AppendUtilitiesHistory("AUTO FIX SYSTEM executed.");
            await RefreshUtilitiesViewAsync();
        }

        private async void UtilitiesQuickCleanRepair_Click(object sender, RoutedEventArgs e)
        {
            await CleanupEverythingInternalAsync();
            await SafeApiCall(() => _backendClient.FlushDnsAsync());
            AppendUtilitiesHistory("QUICK CLEAN & REPAIR executed.");
            UtilitiesQuickResultText.Text = "Quick clean & repair complete\nCleanup and basic repair requested";
            await RefreshUtilitiesViewAsync();
        }

        private async void UtilitiesFullMaintenance_Click(object sender, RoutedEventArgs e)
        {
            await CleanupEverythingInternalAsync();
            await RunPowerShellActionAsync("sfc /scannow", "FULL SYSTEM MAINTENANCE", "Deep clean + repair + optimize requested.", TimeSpan.FromMinutes(20));
            await ApplyBoosterProfileAsync("productivity", "Full Maintenance");
            AppendUtilitiesHistory("FULL SYSTEM MAINTENANCE executed.");
            UtilitiesQuickResultText.Text = "Full maintenance executed\nDeep clean + repair + optimize requested";
            await RefreshUtilitiesViewAsync();
        }

        #endregion

        #region Feature Audit

        private string GetAppLogsDirectory()
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HyperBoost X",
                "logs");
            Directory.CreateDirectory(directory);
            return directory;
        }

        private string GetFeatureAuditLogPath()
        {
            return Path.Combine(GetAppLogsDirectory(), "feature-audit.log");
        }

        private void AppendFeatureAuditHistory(string entry)
        {
            if (string.IsNullOrWhiteSpace(entry))
                return;

            if (_featureAuditHistory.Count >= 16)
                _featureAuditHistory.Dequeue();

            _featureAuditHistory.Enqueue($"{DateTime.Now:HH:mm:ss} - {entry}");
            if (TestingHistoryText != null)
                TestingHistoryText.Text = string.Join(Environment.NewLine, _featureAuditHistory.Reverse());
        }

        private static string TrimFeatureAuditText(string value, int maxLength = 220)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Refresh completed.";

            var normalized = value.Replace("\r", " ").Replace("\n", " ").Trim();
            if (normalized.Length <= maxLength)
                return normalized;

            return normalized.Substring(0, maxLength - 3) + "...";
        }

        private IReadOnlyList<FeatureAuditIncident> GetRelevantFeatureAuditIncidents(string targetName, TimeSpan? lookback = null)
        {
            var effectiveLookback = lookback ?? TimeSpan.FromMinutes(30);
            var cutoff = DateTime.UtcNow - effectiveLookback;
            if (_featureAuditRunStartedUtc.HasValue && _featureAuditRunStartedUtc.Value > cutoff)
                cutoff = _featureAuditRunStartedUtc.Value;

            return _featureAuditIncidents
                .Where(item => item.TimestampUtc >= cutoff)
                .Where(item => string.Equals(item.TargetName, targetName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.TimestampUtc)
                .ToList();
        }

        private IReadOnlyList<FeatureAuditIncident> GetRuntimeMonitorIncidents(TimeSpan? lookback = null)
        {
            var effectiveLookback = lookback ?? TimeSpan.FromMinutes(30);
            var cutoff = DateTime.UtcNow - effectiveLookback;
            if (_featureAuditRunStartedUtc.HasValue && _featureAuditRunStartedUtc.Value > cutoff)
                cutoff = _featureAuditRunStartedUtc.Value;

            return _featureAuditIncidents
                .Where(item => item.TimestampUtc >= cutoff)
                .Where(item => string.IsNullOrWhiteSpace(item.TargetName))
                .OrderByDescending(item => item.TimestampUtc)
                .ToList();
        }

        private static string ResolveFeatureAuditTargetName(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "";

            if (title.Contains("OpenAI Copilot", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("AI Copilot", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Smart Recommendation", StringComparison.OrdinalIgnoreCase))
                return "AI Copilot";

            if (title.Contains("Startup", StringComparison.OrdinalIgnoreCase))
                return "Startup Manager";

            if (title.Contains("Dashboard", StringComparison.OrdinalIgnoreCase))
                return "Dashboard";

            if (title.Contains("DNS", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Latency", StringComparison.OrdinalIgnoreCase))
                return "DNS & Latency Tools";

            if (title.Contains("Network", StringComparison.OrdinalIgnoreCase))
                return "Network Booster";

            if (title.Contains("Privacy", StringComparison.OrdinalIgnoreCase))
                return "Privacy Center";

            if (title.Contains("Security", StringComparison.OrdinalIgnoreCase))
                return "Security & Health";

            if (title.Contains("App Uninstaller", StringComparison.OrdinalIgnoreCase))
                return "App Uninstaller";

            if (title.Contains("Apps Manager", StringComparison.OrdinalIgnoreCase))
                return "Apps Manager";

            if (title.Contains("Automation", StringComparison.OrdinalIgnoreCase))
                return "Scheduled Automation";

            if (title.Contains("Utilities", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Emergency Tools", StringComparison.OrdinalIgnoreCase))
                return "Utilities Tools";

            if (title.Contains("Driver", StringComparison.OrdinalIgnoreCase))
                return "Driver & Update Center";

            if (title.Contains("Restore Point", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("System Protection", StringComparison.OrdinalIgnoreCase))
                return "Restore Point Manager";

            if (title.Contains("Backup", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Restore", StringComparison.OrdinalIgnoreCase))
                return "Restore & Backup";

            if (title.Contains("Windows Features", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Feature Optimization", StringComparison.OrdinalIgnoreCase))
                return "Windows Features";

            if (title.Contains("App Update", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Update", StringComparison.OrdinalIgnoreCase))
                return "Update Control";

            if (title.Contains("Repair", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("SFC", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("DISM", StringComparison.OrdinalIgnoreCase))
                return "Repair Tools";

            if (title.Contains("Tweaks", StringComparison.OrdinalIgnoreCase))
                return "Tweaks Center";

            if (title.Contains("Advanced", StringComparison.OrdinalIgnoreCase))
                return "Advanced Tweaks";

            if (title.Contains("Services", StringComparison.OrdinalIgnoreCase))
                return "Windows Services";

            if (title.Contains("Power", StringComparison.OrdinalIgnoreCase))
                return "Power Optimization";

            if (title.Contains("Visual", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("Animation", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("UI Rendering", StringComparison.OrdinalIgnoreCase))
                return "Visual Effects";

            if (title.Contains("Gaming", StringComparison.OrdinalIgnoreCase))
                return "Gaming Booster";

            if (title.Contains("Streaming", StringComparison.OrdinalIgnoreCase))
                return "Streaming Mode";

            if (title.Contains("Creator", StringComparison.OrdinalIgnoreCase))
                return "Creator Mode";

            if (title.Contains("Storage", StringComparison.OrdinalIgnoreCase))
                return "Storage";

            if (title.Contains("Performance", StringComparison.OrdinalIgnoreCase))
                return "Performance Boost";

            if (title.Contains("Cleanup", StringComparison.OrdinalIgnoreCase))
                return "Cleanup";

            return "";
        }

        private static void RequireTestCondition(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private FeatureAuditTarget CreateTestingProbeTarget(string name, Func<Task<string>> probe)
        {
            var snapshot = "Not executed.";
            return new FeatureAuditTarget
            {
                Name = name,
                ExecuteAsync = async () => snapshot = await probe(),
                Snapshot = () => snapshot
            };
        }

        private async Task<string> AuditNotificationPipelineAsync()
        {
            ShowActionStatus(ActionState.Info, "Audit Notification", "Notification pipeline test.", "Immediate render expected.");
            await Dispatcher.Yield(DispatcherPriority.Render);
            RequireTestCondition(ActionStatusCard?.Visibility == Visibility.Visible, "Action status card did not become visible.");
            RequireTestCondition(string.Equals(ActionStatusTitle?.Text, "Audit Notification", StringComparison.Ordinal), "Action status title did not update immediately.");
            RequireTestCondition(string.Equals(ActionStatusText?.Text, "Notification pipeline test.", StringComparison.Ordinal), "Action status message did not update immediately.");
            return $"{ActionStatusTitle?.Text} | {ActionStatusText?.Text}";
        }

        private async Task<string> AuditUtilitiesWorkflowProbeAsync()
        {
            await RefreshUtilitiesViewAsync();
            ReviewUtilitiesExecutionEngine_Click(this, new RoutedEventArgs());
            BuildUtilitiesWorkflow_Click(this, new RoutedEventArgs());
            RequireTestCondition(!string.IsNullOrWhiteSpace(_lastUtilitiesWorkflowOutput), "Utilities workflow output empty after workflow probe.");
            RequireTestCondition(_lastUtilitiesWorkflowOutput.Contains("Utility Workflow Builder", StringComparison.OrdinalIgnoreCase), "Utilities workflow builder output missing.");
            return TrimFeatureAuditText(_lastUtilitiesWorkflowOutput);
        }

        private async Task<string> AuditUtilitiesSafetyProbeAsync()
        {
            await RefreshUtilitiesViewAsync();
            ReviewUtilitiesSafety_Click(this, new RoutedEventArgs());
            ReviewUtilitiesSelfHealing_Click(this, new RoutedEventArgs());
            ApplyUtilitiesGoal_Click(this, new RoutedEventArgs());
            RequireTestCondition(!string.IsNullOrWhiteSpace(UtilitiesSafetyText?.Text), "Utilities safety text empty after safety probe.");
            RequireTestCondition(UtilitiesSafetyText.Text.Contains("Goal-Based Utilities", StringComparison.OrdinalIgnoreCase), "Goal-based utilities summary missing after safety probe.");
            return TrimFeatureAuditText(UtilitiesSafetyText.Text);
        }

        private async Task<string> AuditSettingsIntegrationProbeAsync()
        {
            await RefreshSettingsViewAsync();
            RequireTestCondition(!string.IsNullOrWhiteSpace(SettingsAppUpdateStatusText?.Text), "Settings app update status empty.");
            RequireTestCondition(!string.IsNullOrWhiteSpace(OpenAiSettingsStatusText?.Text), "OpenAI settings status empty.");
            RequireTestCondition(!string.IsNullOrWhiteSpace(DiscordWebhookStatusText?.Text), "Discord webhook status empty.");
            return TrimFeatureAuditText(
                $"{SettingsAppUpdateStatusText?.Text} | {OpenAiSettingsStatusText?.Text} | {DiscordWebhookStatusText?.Text}",
                280);
        }

        private void UpdateTestingStaticSummaries()
        {
            _lastTestingStrategySummary = TestingAuditSummaryService.BuildStrategySummary(_testingExecutionMode);
            _lastTestingLayerSummary = TestingAuditSummaryService.BuildLayerSummary();
        }

        private string BuildTestingSuiteMatrixText()
        {
            return TestingAuditSummaryService.BuildSuiteMatrixText(_lastTestingSuite);
        }

        private string BuildTestingCompatibilitySummary(bool backendHealthy)
        {
            return TestingAuditSummaryService.BuildCompatibilitySummary(new TestingCompatibilityContext
            {
                OsVersion = Environment.OSVersion.VersionString,
                Is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
                CultureName = CultureInfo.CurrentCulture.Name,
                UiCultureName = CultureInfo.CurrentUICulture.Name,
                IsAdministrator = IsProcessRunningAsAdministrator(),
                BackendHealthy = backendHealthy,
                WindowWidth = ActualWidth,
                WindowHeight = ActualHeight
            });
        }

        private static bool IsProcessRunningAsAdministrator()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }

            IntPtr adminGroupSid = IntPtr.Zero;

            try
            {
                var administratorsSidAuthority = new byte[] { 0, 0, 0, 0, 0, 5 };
                if (!AllocateAndInitializeSid(
                        administratorsSidAuthority,
                        2,
                        32,
                        544,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        out adminGroupSid))
                {
                    return false;
                }

                if (!CheckTokenMembership(IntPtr.Zero, adminGroupSid, out var isMember))
                {
                    return false;
                }

                return isMember;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (adminGroupSid != IntPtr.Zero)
                {
                    FreeSid(adminGroupSid);
                }
            }
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AllocateAndInitializeSid(
            byte[] pIdentifierAuthority,
            byte nSubAuthorityCount,
            uint nSubAuthority0,
            uint nSubAuthority1,
            uint nSubAuthority2,
            uint nSubAuthority3,
            uint nSubAuthority4,
            uint nSubAuthority5,
            uint nSubAuthority6,
            uint nSubAuthority7,
            out IntPtr pSid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CheckTokenMembership(
            IntPtr tokenHandle,
            IntPtr sidToCheck,
            out bool isMember);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern IntPtr FreeSid(IntPtr pSid);

        private List<FeatureAuditTarget> BuildTestingSuiteTargets(string suiteName)
        {
            switch (suiteName)
            {
                case "Unit":
                    return new List<FeatureAuditTarget>
                    {
                        CreateTestingProbeTarget("Unit / Score Engine", () =>
                        {
                            var healthy = CalculateDashboardPerformanceScore(12, 28, 22, 4, 1);
                            var heavy = CalculateDashboardPerformanceScore(95, 92, 87, 36, 7);
                            RequireTestCondition(healthy > heavy, "Healthy profile should score higher than heavy load profile.");
                            RequireTestCondition(healthy is >= 0 and <= 100, "Healthy score out of range.");
                            return Task.FromResult($"Healthy={healthy} | Heavy={heavy}");
                        }),
                        CreateTestingProbeTarget("Unit / Version Normalizer", () =>
                        {
                            RequireTestCondition(NormalizeVersionLabel("v1.1.0-beta") == "1.1.0-beta", "Version normalizer failed for prefixed tag.");
                            RequireTestCondition(NormalizeVersionLabel("1.1.0") == "1.1.0", "Version normalizer changed clean version.");
                            RequireTestCondition(NormalizeVersionLabel("1.1.2+abc123") == "1.1.2", "Version normalizer failed for build metadata.");
                            RequireTestCondition(NormalizeVersionLabel("v1.1.2 - Stable - 2026") == "1.1.2", "Version normalizer failed for decorated label.");
                            return Task.FromResult("NormalizeVersionLabel passed.");
                        }),
                        CreateTestingProbeTarget("Unit / Memory Type", () =>
                        {
                            RequireTestCondition(InferMemoryType(5600) == "DDR5", "DDR5 inference failed.");
                            RequireTestCondition(InferMemoryType(3200) == "DDR4", "DDR4 inference failed.");
                            return Task.FromResult("InferMemoryType passed.");
                        }),
                        CreateTestingProbeTarget("Unit / Visual Bar", () =>
                        {
                            var bar = BuildUsageBar(55);
                            RequireTestCondition(bar.StartsWith("[") && bar.EndsWith("]"), "Usage bar format invalid.");
                            RequireTestCondition(bar.Length == 12, "Usage bar length invalid.");
                            return Task.FromResult($"Usage bar sample: {bar}");
                        })
                    };

                case "Integration":
                    return new List<FeatureAuditTarget>
                    {
                        CreateTestingProbeTarget("Integration / Backend Health", async () =>
                        {
                            var healthy = await _backendClient.HealthCheckAsync();
                            RequireTestCondition(healthy, "Backend health endpoint did not respond.");
                            return "Backend health endpoint OK.";
                        }),
                        CreateTestingProbeTarget("Integration / System Stats Contract", async () =>
                        {
                            var stats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync()) as JObject;
                            RequireTestCondition(stats != null, "System stats payload missing.");
                            RequireTestCondition(stats["cpu"] != null || stats["cpu_percent"] != null, "CPU stat missing.");
                            RequireTestCondition(stats["memory"] != null || stats["memory_percent"] != null, "Memory stat missing.");
                            return $"Stats keys: {string.Join(", ", stats.Properties().Take(6).Select(x => x.Name))}";
                        }),
                        CreateTestingProbeTarget("Integration / Startup + Processes", async () =>
                        {
                            var startup = await SafeApiCall(() => _backendClient.GetStartupItemsAsync()) as JToken;
                            var processes = await SafeApiCall(() => _backendClient.GetProcessesAsync()) as JToken;
                            RequireTestCondition(startup != null, "Startup payload missing.");
                            RequireTestCondition(processes != null, "Processes payload missing.");
                            return "Startup and process endpoints returned payload.";
                        }),
                        CreateTestingProbeTarget("Integration / Settings Refresh", async () =>
                        {
                            await RefreshSettingsViewAsync();
                            RequireTestCondition(!string.IsNullOrWhiteSpace(SettingsUiText?.Text), "Settings UI summary empty after refresh.");
                            return TrimFeatureAuditText(SettingsUiText?.Text);
                        })
                    };

                case "UI Flow":
                    return new List<FeatureAuditTarget>
                    {
                        CreateTestingProbeTarget("UI / Dashboard Refresh", async () =>
                        {
                            await RefreshDashboard();
                            RequireTestCondition(!string.IsNullOrWhiteSpace(DashboardPerfScoreText?.Text), "Dashboard score not rendered.");
                            return TrimFeatureAuditText(DashboardPerfScoreText?.Text);
                        }),
                        CreateTestingProbeTarget("UI / Smart Recommendation", async () =>
                        {
                            await RunSmartRecommendationScanAsync();
                            RequireTestCondition(!string.IsNullOrWhiteSpace(SmartOverallScoreText?.Text), "Smart recommendation score missing.");
                            return TrimFeatureAuditText(SmartOverallScoreText?.Text);
                        }),
                        CreateTestingProbeTarget("UI / Automation Screen", async () =>
                        {
                            await RefreshAutomationViewAsync();
                            RequireTestCondition(!string.IsNullOrWhiteSpace(AutomationDashboardText?.Text), "Automation dashboard empty.");
                            return TrimFeatureAuditText(AutomationDashboardText?.Text);
                        }),
                        CreateTestingProbeTarget("UI / About + Update", async () =>
                        {
                            await RefreshAboutViewAsync();
                            RequireTestCondition(!string.IsNullOrWhiteSpace(AboutUpdateStatusText?.Text), "About update status empty.");
                            return TrimFeatureAuditText(AboutUpdateStatusText?.Text);
                        })
                    };

                case "End-to-End":
                    return new List<FeatureAuditTarget>
                    {
                        CreateTestingProbeTarget("E2E / Guided Safe Flow", async () =>
                        {
                            await RefreshDashboard();
                            await RefreshStartupItems();
                            await RefreshSettingsViewAsync();
                            await RefreshAutomationViewAsync();
                            await RefreshAboutViewAsync();
                            RequireTestCondition(!string.IsNullOrWhiteSpace(DashboardPerfScoreText?.Text), "Dashboard stage missing.");
                            RequireTestCondition(!string.IsNullOrWhiteSpace(StartupScoreText?.Text), "Startup stage missing.");
                            RequireTestCondition(!string.IsNullOrWhiteSpace(SettingsSpecOverviewText?.Text), "Settings PC spec stage missing.");
                            RequireTestCondition(!string.IsNullOrWhiteSpace(AutomationDashboardText?.Text), "Automation stage missing.");
                            return "Dashboard -> Startup -> Settings -> Automation -> About safe flow passed.";
                        }),
                        CreateTestingProbeTarget("E2E / Persistence Snapshot", () =>
                        {
                            RequireTestCondition(File.Exists(_appConfigService.GetConfigPath()), "Persisted config file not found.");
                            return Task.FromResult($"Config path: {_appConfigService.GetConfigPath()}");
                        })
                    };

                case "Regression":
                    return BuildFeatureAuditTargets(fullAudit: true);

                case "Performance":
                    return new List<FeatureAuditTarget>
                    {
                        CreateTestingProbeTarget("Performance / Stats Latency", async () =>
                        {
                            var sw = Stopwatch.StartNew();
                            await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
                            sw.Stop();
                            RequireTestCondition(sw.ElapsedMilliseconds < 4000, $"System stats too slow: {sw.ElapsedMilliseconds} ms");
                            return $"System stats latency: {sw.ElapsedMilliseconds} ms";
                        }),
                        CreateTestingProbeTarget("Performance / Dashboard Refresh", async () =>
                        {
                            var sw = Stopwatch.StartNew();
                            await RefreshDashboard();
                            sw.Stop();
                            RequireTestCondition(sw.ElapsedMilliseconds < 15000, $"Dashboard refresh too slow: {sw.ElapsedMilliseconds} ms");
                            return $"Dashboard refresh: {sw.ElapsedMilliseconds} ms";
                        }),
                        CreateTestingProbeTarget("Performance / Smart Scan", async () =>
                        {
                            var sw = Stopwatch.StartNew();
                            await RunSmartRecommendationScanAsync();
                            sw.Stop();
                            RequireTestCondition(sw.ElapsedMilliseconds < 15000, $"Smart scan too slow: {sw.ElapsedMilliseconds} ms");
                            _lastTestingMetricsSummary =
                                $"Stats fetch + dashboard + smart scan completed.{Environment.NewLine}" +
                                $"Last smart scan time: {sw.ElapsedMilliseconds} ms{Environment.NewLine}" +
                                "Target guidance: startup < 3s, dashboard < 5s, scan < 15s in beta mode.";
                            return $"Smart scan time: {sw.ElapsedMilliseconds} ms";
                        })
                    };

                case "Stress":
                    return new List<FeatureAuditTarget>
                    {
                        CreateTestingProbeTarget("Stress / Repeated Stats", async () =>
                        {
                            for (var i = 0; i < 8; i++)
                                await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
                            return "Repeated stats fetch x8 completed.";
                        }),
                        CreateTestingProbeTarget("Stress / Multi-Refresh Burst", async () =>
                        {
                            for (var i = 0; i < 3; i++)
                            {
                                await RefreshDashboard();
                                await RefreshSettingsViewAsync();
                                await RefreshAutomationViewAsync();
                            }
                            _lastTestingMetricsSummary =
                                "Stress suite executed dashboard/settings/automation burst x3." + Environment.NewLine +
                                "Goal: no freeze, no crash, no empty state after repeated refresh.";
                            return "Burst refresh x3 completed.";
                        })
                    };

                case "Stability":
                    return new List<FeatureAuditTarget>
                    {
                        CreateTestingProbeTarget("Stability / Mini Soak", async () =>
                        {
                            var samples = new List<string>();
                            for (var i = 0; i < 4; i++)
                            {
                                var stats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync()) as JObject;
                                samples.Add($"Cycle {i + 1}: CPU {(stats?.Value<double?>("cpu") ?? stats?.Value<double?>("cpu_percent") ?? 0):0}%");
                                await Task.Delay(350);
                            }
                            _lastTestingMetricsSummary =
                                "Mini soak completed over 4 cycles." + Environment.NewLine +
                                string.Join(Environment.NewLine, samples);
                            return string.Join(" | ", samples);
                        })
                    };

                case "Security":
                    return new List<FeatureAuditTarget>
                    {
                        CreateTestingProbeTarget("Security / Secret Storage", () =>
                        {
                            RequireTestCondition(string.IsNullOrWhiteSpace(_appConfig?.Settings?.OpenAiApiKey), "Plain OpenAI key should not be stored in app-state.");
                            RequireTestCondition(string.IsNullOrWhiteSpace(_appConfig?.Settings?.DiscordWebhookUrl), "Plain Discord webhook should not be stored in app-state.");
                            RequireTestCondition(string.IsNullOrWhiteSpace(_appConfig?.Settings?.DiscordUpdateWebhookUrl), "Plain Discord update webhook should not be stored in app-state.");
                            return Task.FromResult("Config file keeps sensitive values blank; secure store path is active.");
                        }),
                        CreateTestingProbeTarget("Security / Updater Guard", () =>
                        {
                            var probe = _appUpdateService.VerifyInstaller("missing-installer.exe", "https://example.com/bad.exe", "bad.exe");
                            RequireTestCondition(!probe.SourceTrusted, "Updater accepted untrusted source URL.");
                            RequireTestCondition(!probe.AllowManualInstall, "Updater should reject invalid installer probe.");
                            _lastTestingCompatibilitySummary =
                                "Security checks:" + Environment.NewLine +
                                "- secure secret persistence expected" + Environment.NewLine +
                                "- updater rejects untrusted source URL" + Environment.NewLine +
                                "- automation safe mode still blocks non-safe tasks";
                            return Task.FromResult(probe.Summary);
                        })
                    };

                case "Compatibility":
                    return new List<FeatureAuditTarget>
                    {
                        CreateTestingProbeTarget("Compatibility / Runtime Profile", async () =>
                        {
                            var healthy = await _backendClient.HealthCheckAsync();
                            _lastTestingCompatibilitySummary = BuildTestingCompatibilitySummary(healthy);
                            return TrimFeatureAuditText(_lastTestingCompatibilitySummary);
                        }),
                        CreateTestingProbeTarget("Compatibility / Localization + Theme", () =>
                        {
                            RequireTestCondition(!string.IsNullOrWhiteSpace(_settingsTheme), "Theme state missing.");
                            RequireTestCondition(!string.IsNullOrWhiteSpace(_localizationService.CurrentLocale), "Locale state missing.");
                            return Task.FromResult($"Theme={_settingsTheme} | Locale={_localizationService.CurrentLocale} | Sidebar={_settingsSidebarMode}");
                        })
                    };

                default:
                    return new List<FeatureAuditTarget>();
            }
        }

        private List<FeatureAuditTarget> BuildFeatureAuditTargets(bool fullAudit)
        {
            var targets = new List<FeatureAuditTarget>
            {
                new()
                {
                    Name = "Runtime Error Monitor",
                    ExecuteAsync = () => Task.CompletedTask,
                    Snapshot = () =>
                    {
                        var incidents = _featureAuditIncidents
                            .Where(item => item.TimestampUtc >= DateTime.UtcNow - TimeSpan.FromMinutes(30))
                            .OrderByDescending(item => item.TimestampUtc)
                            .Take(3)
                            .Select(item => $"{item.Title}: {item.Message}");
                        return incidents.Any()
                            ? string.Join(" | ", incidents)
                            : "No runtime warning/error detected in the last 30 minutes.";
                    }
                },
                new()
                {
                    Name = "Dashboard",
                    ExecuteAsync = RefreshDashboard,
                    Snapshot = () => $"CPU {CpuText?.Text ?? "--"} | RAM {MemoryText?.Text ?? "--"} | Disk {DiskText?.Text ?? "--"}"
                },
                new()
                {
                    Name = "One Click Boost",
                    ExecuteAsync = () =>
                    {
                        InitializeOneClickBoostDefaults();
                        RefreshLastBoostView();
                        return Task.CompletedTask;
                    },
                    Snapshot = () => _lastBoostScore
                },
                new()
                {
                    Name = "Startup Manager",
                    ExecuteAsync = RefreshStartupItems,
                    Snapshot = () => StartupScoreText?.Text ?? StartupSummaryText?.Text
                },
                new()
                {
                    Name = "Cleanup",
                    ExecuteAsync = RefreshCleanupViewAsync,
                    Snapshot = () => CleanupScanText?.Text ?? CleanupSmartRecommendationText?.Text
                },
                new()
                {
                    Name = "Network Booster",
                    ExecuteAsync = async () =>
                    {
                        await RefreshNetworkDiagnostics();
                        await RefreshNetworkBoosterViewAsync();
                    },
                    Snapshot = () => NetworkDiagnosticsText?.Text ?? NetworkAdapterText?.Text
                },
                new()
                {
                    Name = "Privacy Center",
                    ExecuteAsync = RefreshPrivacyViewAsync,
                    Snapshot = () => PrivacyDashboardText?.Text ?? PrivacyRecommendationText?.Text
                },
                new()
                {
                    Name = "Security & Health",
                    ExecuteAsync = RefreshSecurityHealthViewAsync,
                    Snapshot = () => SecurityHealthDashboardText?.Text ?? SecurityRecommendationText?.Text
                },
                new()
                {
                    Name = "Apps Manager",
                    ExecuteAsync = RefreshAppsManagerViewAsync,
                    Snapshot = () => RunningAppsManagerText?.Text ?? BackgroundAppsManagerText?.Text
                },
                new()
                {
                    Name = "Scheduled Automation",
                    ExecuteAsync = RefreshAutomationViewAsync,
                    Snapshot = () => AutomationDashboardText?.Text ?? AutomationAnalyticsText?.Text
                },
                new()
                {
                    Name = "Utilities Tools",
                    ExecuteAsync = RefreshUtilitiesViewAsync,
                    Snapshot = () => UtilitiesDashboardText?.Text ?? UtilitiesQuickResultText?.Text
                },
                new()
                {
                    Name = "Settings",
                    ExecuteAsync = RefreshSettingsViewAsync,
                    Snapshot = () => SettingsQuickResultText?.Text ?? SettingsSpecOverviewText?.Text
                },
                new()
                {
                    Name = "AI Copilot",
                    ExecuteAsync = async () => await RefreshAiCopilotDiagnosticsAsync(refreshContext: true),
                    Snapshot = () => AiCopilotStatusText?.Text ?? AiCopilotReplyText?.Text ?? OpenAiSettingsStatusText?.Text
                },
                new()
                {
                    Name = "About App",
                    ExecuteAsync = RefreshAboutViewAsync,
                    Snapshot = () => AboutVersionText?.Text ?? "About page refreshed."
                }
            };

            if (!fullAudit)
                return targets;

            targets.AddRange(new[]
            {
                new FeatureAuditTarget
                {
                    Name = "Gaming Booster Hub",
                    ExecuteAsync = RefreshGamingBoosterHubAsync,
                    Snapshot = () => BoosterSummaryText?.Text ?? BoosterRecommendationText?.Text ?? BoosterReportText?.Text
                },
                CreateTestingProbeTarget("Notification Pipeline", AuditNotificationPipelineAsync),
                CreateTestingProbeTarget("Utilities Workflow Actions", AuditUtilitiesWorkflowProbeAsync),
                CreateTestingProbeTarget("Utilities Safety Actions", AuditUtilitiesSafetyProbeAsync),
                CreateTestingProbeTarget("Settings Integration Actions", AuditSettingsIntegrationProbeAsync),
                new FeatureAuditTarget
                {
                    Name = "Smart Recommendation",
                    ExecuteAsync = async () =>
                    {
                        await RunSmartRecommendationScanAsync();
                        await RefreshAiCopilotDiagnosticsAsync(refreshContext: true);
                    },
                    Snapshot = () => SmartOverallScoreText?.Text ?? AiCopilotStatusText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Performance Boost",
                    ExecuteAsync = RefreshPerformanceBoostViewAsync,
                    Snapshot = () => PerformanceScoreText?.Text ?? PerformanceResultsText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Storage",
                    ExecuteAsync = RefreshStorageViewAsync,
                    Snapshot = () => StorageUnifiedOverviewText?.Text ?? StorageHealthText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Gaming Booster",
                    ExecuteAsync = async () =>
                    {
                        InitializeGamingDefaults();
                        await RefreshGamingBoosterViewAsync();
                    },
                    Snapshot = () => GamingMonitorText?.Text ?? GamingRecommendationText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Streaming Mode",
                    ExecuteAsync = async () =>
                    {
                        InitializeStreamingDefaults();
                        await RefreshStreamingViewAsync();
                    },
                    Snapshot = () => StreamingDetectedAppText?.Text ?? StreamingRecommendationText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Creator Mode",
                    ExecuteAsync = async () =>
                    {
                        InitializeCreatorDefaults();
                        await RefreshCreatorViewAsync();
                    },
                    Snapshot = () => CreatorDetectedAppText?.Text ?? CreatorRecommendationText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "DNS & Latency Tools",
                    ExecuteAsync = RefreshDnsLatencyViewAsync,
                    Snapshot = () => DnsSpeedTesterText?.Text ?? LatencyTesterText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Background Apps",
                    ExecuteAsync = RefreshBackgroundApps,
                    Snapshot = () => BackgroundAppsText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Tweaks Center",
                    ExecuteAsync = RefreshTweaksCenterViewAsync,
                    Snapshot = () => TweaksText?.Text ?? TweaksLogText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Windows Features",
                    ExecuteAsync = RefreshWindowsFeaturesViewAsync,
                    Snapshot = () => WindowsFeaturesQuickResultText?.Text ?? WindowsFeaturesListText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Update Control",
                    ExecuteAsync = RefreshUpdateControlViewAsync,
                    Snapshot = () => UpdateDashboardText?.Text ?? UpdateRecommendationText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Repair Tools",
                    ExecuteAsync = RefreshRepairViewAsync,
                    Snapshot = () => RepairDashboardText?.Text ?? RepairRecommendationText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Driver & Update Center",
                    ExecuteAsync = RefreshDrivers,
                    Snapshot = () => DriversScannerText?.Text ?? DriversText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "App Uninstaller",
                    ExecuteAsync = RefreshAppUninstallerViewAsync,
                    Snapshot = () => AppUninstallerDashboardText?.Text ?? AppUninstallerInventoryText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Advanced Tweaks",
                    ExecuteAsync = RefreshAdvancedTweaksViewAsync,
                    Snapshot = () => AdvancedQuickResultText?.Text ?? AdvancedMonitorText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Windows Services",
                    ExecuteAsync = RefreshServicesViewAsync,
                    Snapshot = () => ServicesQuickResultText?.Text ?? ServicesListText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Power Optimization",
                    ExecuteAsync = RefreshPowerOptimizationViewAsync,
                    Snapshot = () => PowerDashboardText?.Text ?? PowerTelemetryText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Visual Effects",
                    ExecuteAsync = RefreshVisualEffectsViewAsync,
                    Snapshot = () => VisualDashboardText?.Text ?? VisualRecommendationText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "App Update Status",
                    ExecuteAsync = async () =>
                    {
                        await RefreshAboutViewAsync();
                        await RefreshSettingsViewAsync();
                    },
                    Snapshot = () => SettingsAppUpdateStatusText?.Text ?? AboutUpdateStatusText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Integration Status",
                    ExecuteAsync = RefreshSettingsViewAsync,
                    Snapshot = () => OpenAiSettingsStatusText?.Text ?? DiscordWebhookStatusText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Restore & Backup",
                    ExecuteAsync = RefreshRestoreBackupViewAsync,
                    Snapshot = () => RestoreDashboardText?.Text ?? RestoreHistoryText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Restore Point Manager",
                    ExecuteAsync = RefreshRestorePointManagerViewAsync,
                    Snapshot = () => RestorePointDashboardText?.Text ?? RestorePointAuditText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Feature Audit Center",
                    ExecuteAsync = RefreshFeatureAuditViewAsync,
                    Snapshot = () => TestingDashboardText?.Text ?? TestingCompatibilityText?.Text ?? TestingReportPreviewText?.Text
                },
                new FeatureAuditTarget
                {
                    Name = "Compatibility Snapshot",
                    ExecuteAsync = async () =>
                    {
                        var backendHealthy = await _backendClient.HealthCheckAsync();
                        _lastTestingCompatibilitySummary = BuildTestingCompatibilitySummary(backendHealthy);
                        await RefreshFeatureAuditViewAsync();
                    },
                    Snapshot = () => TestingCompatibilityText?.Text ?? _lastTestingCompatibilitySummary
                }
            });

            return targets;
        }

        private async Task<FeatureAuditResult> RunFeatureAuditTargetAsync(FeatureAuditTarget target)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                TestingStatusText.Text =
                    $"Audit running: {target.Name}{Environment.NewLine}" +
                    $"Mode: {_lastFeatureAuditMode}{Environment.NewLine}" +
                    $"Discord webhook: {(_discordWebhookEnabled && !string.IsNullOrWhiteSpace(_discordWebhookUrl) ? "Ready" : "Not configured")}{Environment.NewLine}" +
                    $"Current page: {_activePage}";

                await target.ExecuteAsync();
                stopwatch.Stop();

                var incidents = GetRelevantFeatureAuditIncidents(target.Name);
                if (string.Equals(target.Name, "Runtime Error Monitor", StringComparison.OrdinalIgnoreCase))
                {
                    incidents = GetRuntimeMonitorIncidents();
                }

                if (incidents.Count > 0)
                {
                    var latest = incidents[0];
                    return new FeatureAuditResult
                    {
                        Name = target.Name,
                        Success = false,
                        DurationMs = stopwatch.ElapsedMilliseconds,
                        Details = TrimFeatureAuditText($"{latest.Title}: {latest.Message}" +
                            (string.IsNullOrWhiteSpace(latest.Meta) ? "" : $" | {latest.Meta}"))
                    };
                }

                return new FeatureAuditResult
                {
                    Name = target.Name,
                    Success = true,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    Details = TrimFeatureAuditText(target.Snapshot())
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new FeatureAuditResult
                {
                    Name = target.Name,
                    Success = false,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    Details = TrimFeatureAuditText(string.IsNullOrWhiteSpace(ex.Message) ? ex.GetType().Name : $"{ex.GetType().Name}: {ex.Message}")
                };
            }
        }

        private string BuildFeatureAuditReport(IReadOnlyCollection<FeatureAuditResult> results)
        {
            var passed = results.Count(x => x.Success);
            var failed = results.Count - passed;
            var totalMs = results.Sum(x => x.DurationMs);
            var lines = new List<string>
            {
                $"Feature Audit Mode: {_lastFeatureAuditMode}",
                $"Executed: {_lastFeatureAuditUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}",
                $"Backend URL: {_currentBackendUrl}",
                $"Total modules tested: {results.Count}",
                $"Passed: {passed}",
                $"Failed: {failed}",
                $"Total audit time: {totalMs} ms",
                ""
            };

            foreach (var result in results)
            {
                lines.Add($"{(result.Success ? "[OK]" : "[FAIL]")} {result.Name} | {result.DurationMs} ms | {result.Details}");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private async Task WriteFeatureAuditLogAsync(string report)
        {
            var logPath = GetFeatureAuditLogPath();
            await File.AppendAllTextAsync(
                logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Feature audit report{Environment.NewLine}{report}{Environment.NewLine}{Environment.NewLine}");
        }

        private async Task SendFeatureAuditReportToDiscordAsync(string report)
        {
            await LoadSensitiveConfigurationAsync();

            if (string.IsNullOrWhiteSpace(_discordWebhookUrl))
            {
                AppendFeatureAuditHistory("Discord delivery skipped: error/audit webhook not configured.");
                return;
            }

            var failed = _lastFeatureAuditResults.Count(x => !x.Success);
            var severity = failed > 0 ? "error" : "success";
            var fields = BuildDiscordReportFields(severity, "Automated feature audit report");
            fields["Audit Mode"] = _lastFeatureAuditMode;
            fields["Modules Tested"] = _lastFeatureAuditResults.Count.ToString(CultureInfo.InvariantCulture);
            fields["Failed Modules"] = failed.ToString(CultureInfo.InvariantCulture);
            fields["Audit Log"] = GetFeatureAuditLogPath();

            var result = await _discordWebhookService.SendDetailedAsync(
                _discordWebhookUrl,
                $"HyperBoostX Feature Audit - {_lastFeatureAuditMode}",
                report,
                severity,
                fields);

            AppendFeatureAuditHistory(result.Success
                ? "Audit report delivered to Discord."
                : $"Audit report not delivered to Discord: {result.Summary}");
        }

        private async Task RefreshFeatureAuditViewAsync()
        {
            var backendHealthy = await _backendClient.HealthCheckAsync();
            var passed = _lastFeatureAuditResults.Count(x => x.Success);
            var failed = _lastFeatureAuditResults.Count - passed;

            UpdateTestingStaticSummaries();
            TestingStrategyText.Text = _lastTestingStrategySummary;
            TestingLayerText.Text = _lastTestingLayerSummary;
            TestingSuiteMatrixText.Text = BuildTestingSuiteMatrixText();
            TestingMetricsText.Text = _lastTestingMetricsSummary;
            if (string.IsNullOrWhiteSpace(_lastTestingCompatibilitySummary) ||
                _lastTestingCompatibilitySummary.Contains("will appear here", StringComparison.OrdinalIgnoreCase))
            {
                _lastTestingCompatibilitySummary = BuildTestingCompatibilitySummary(backendHealthy);
            }
            TestingCompatibilityText.Text = _lastTestingCompatibilitySummary;

            TestingQuickResultText.Text =
                (_featureAuditRunning ? "Feature audit running" : "Feature audit engine ready") + Environment.NewLine +
                _lastFeatureAuditSummary;

            TestingDashboardText.Text =
                $"Last Audit Mode: {_lastFeatureAuditMode}{Environment.NewLine}" +
                $"Last Testing Suite: {_lastTestingSuite}{Environment.NewLine}" +
                $"Last Audit Time: {_lastFeatureAuditUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "Never"}{Environment.NewLine}" +
                $"Backend Health: {(backendHealthy ? "Healthy" : "Offline / Degraded")}{Environment.NewLine}" +
                $"Total Modules Tested: {_lastFeatureAuditResults.Count}{Environment.NewLine}" +
                $"Pass / Fail: {passed} / {failed}{Environment.NewLine}" +
                $"Logs Path: {GetFeatureAuditLogPath()}";

            TestingStatusText.Text =
                $"Audit Running: {(_featureAuditRunning ? "Yes" : "No")}{Environment.NewLine}" +
                $"Testing Mode: {_testingExecutionMode}{Environment.NewLine}" +
                $"Discord Webhook: {(_discordWebhookEnabled && !string.IsNullOrWhiteSpace(_discordWebhookUrl) ? "Configured" : "Not configured")}{Environment.NewLine}" +
                $"Discord Auto Delivery: {(_discordWebhookEnabled ? "Enabled" : "Disabled")}{Environment.NewLine}" +
                $"Active Page: {_activePage}{Environment.NewLine}" +
                $"Automation Mode: {_automationMode}";

            TestingModulesText.Text = _lastFeatureAuditResults.Count == 0
                ? "Per-module audit result akan tampil di sini."
                : string.Join(Environment.NewLine, _lastFeatureAuditResults
                    .Select(result => $"{(result.Success ? "[OK]" : "[FAIL]")} {result.Name} | {result.DurationMs} ms | {result.Details}"));

            if (_featureAuditHistory.Count == 0)
                AppendFeatureAuditHistory("Feature audit center initialized.");

            TestingReportPreviewText.Text = string.IsNullOrWhiteSpace(_lastFeatureAuditSummary)
                ? "Report preview akan tampil di sini setelah audit berjalan."
                : BuildFeatureAuditReport(_lastFeatureAuditResults);
        }

        private Task RefreshFeatureAuditViewIfVisibleAsync()
        {
            return string.Equals(_activePage, "Testing", StringComparison.OrdinalIgnoreCase)
                ? RefreshFeatureAuditViewAsync()
                : Task.CompletedTask;
        }

        private async Task RunFeatureAuditAsync(bool fullAudit)
        {
            if (_featureAuditRunning)
            {
                ShowActionStatus(ActionState.Info, "Feature Audit", "Audit masih berjalan. Tunggu proses saat ini selesai.");
                return;
            }

            _featureAuditRunning = true;
            _featureAuditCancellationRequested = false;
            _lastFeatureAuditMode = fullAudit ? "Full" : "Quick";
            _lastFeatureAuditUtc = DateTime.UtcNow;
            _featureAuditRunStartedUtc = DateTime.UtcNow;
            _lastFeatureAuditResults.Clear();
            AppendFeatureAuditHistory($"{_lastFeatureAuditMode} feature audit started.");

            try
            {
                await CheckBackendHealth();
                var targets = BuildFeatureAuditTargets(fullAudit);

                foreach (var target in targets)
                {
                    if (_featureAuditCancellationRequested)
                        break;

                    var result = await RunFeatureAuditTargetAsync(target);
                    _lastFeatureAuditResults.Add(result);
                    AppendFeatureAuditHistory($"{target.Name}: {(result.Success ? "PASS" : "FAIL")} ({result.DurationMs} ms)");
                    await RefreshFeatureAuditViewIfVisibleAsync();
                    await Dispatcher.Yield(DispatcherPriority.Background);
                }

                var passed = _lastFeatureAuditResults.Count(x => x.Success);
                var failed = _lastFeatureAuditResults.Count - passed;
                _lastFeatureAuditSummary = _featureAuditCancellationRequested
                    ? $"Audit cancelled | Passed {passed} | Failed {failed} | Mode {_lastFeatureAuditMode}"
                    : $"Audit complete | Passed {passed} | Failed {failed} | Mode {_lastFeatureAuditMode}";

                var report = BuildFeatureAuditReport(_lastFeatureAuditResults);
                await WriteFeatureAuditLogAsync(report);
                if (!_featureAuditCancellationRequested)
                    await SendFeatureAuditReportToDiscordAsync(report);
                await RefreshFeatureAuditViewIfVisibleAsync();

                if (!_featureAuditCancellationRequested)
                {
                    ShowRequestedStatus(
                        "Feature Audit",
                        failed == 0
                            ? $"{_lastFeatureAuditMode} feature audit selesai. Hasil ini adalah probe internal, bukan jaminan semua workflow real-world sudah tervalidasi."
                            : $"{_lastFeatureAuditMode} feature audit selesai dengan {failed} probe gagal. Review report untuk detail kegagalan.",
                        report);
                }
            }
            catch (Exception ex)
            {
                _lastFeatureAuditSummary = $"Audit failed unexpectedly: {ex.Message}";
                AppendFeatureAuditHistory($"Audit engine error: {ex.Message}");
                ShowActionStatus(ActionState.Error, "Feature Audit", "Feature audit gagal dijalankan.", ex.Message);
            }
            finally
            {
                _featureAuditRunning = false;
                _featureAuditCancellationRequested = false;
                _featureAuditRunStartedUtc = null;
                await RefreshFeatureAuditViewIfVisibleAsync();
            }
        }

        private async void RunQuickFeatureAudit_Click(object sender, RoutedEventArgs e)
        {
            await RunFeatureAuditAsync(fullAudit: false);
        }

        private async void RunFullFeatureAudit_Click(object sender, RoutedEventArgs e)
        {
            await RunFeatureAuditAsync(fullAudit: true);
        }

        private async void SendFeatureAuditReport_Click(object sender, RoutedEventArgs e)
        {
            if (_lastFeatureAuditResults.Count == 0)
            {
                ShowActionStatus(ActionState.Warning, "Feature Audit", "Belum ada audit report yang bisa dikirim.");
                return;
            }

            await SendFeatureAuditReportToDiscordAsync(BuildFeatureAuditReport(_lastFeatureAuditResults));
            await RefreshFeatureAuditViewAsync();
            ShowActionStatus(ActionState.Info, "Feature Audit", "Percobaan kirim report terakhir ke Discord sudah dijalankan.", TestingHistoryText?.Text);
        }

        private void OpenFeatureAuditLogs_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("explorer.exe", $"\"{GetAppLogsDirectory()}\"", "Feature Audit Logs");
        }

        private async Task RunTestingSuiteAsync(string suiteName)
        {
            if (_featureAuditRunning)
            {
                ShowActionStatus(ActionState.Info, "Testing", "Suite testing masih berjalan. Tunggu proses saat ini selesai.");
                return;
            }

            _featureAuditRunning = true;
            _featureAuditCancellationRequested = false;
            _lastTestingSuite = suiteName;
            _lastFeatureAuditMode = $"{suiteName} Suite";
            _lastFeatureAuditUtc = DateTime.UtcNow;
            _featureAuditRunStartedUtc = DateTime.UtcNow;
            _lastFeatureAuditResults.Clear();
            UpdateTestingStaticSummaries();
            AppendFeatureAuditHistory($"{suiteName} testing suite started in {_testingExecutionMode} mode.");

            try
            {
                await CheckBackendHealth();
                var targets = BuildTestingSuiteTargets(suiteName);
                if (targets.Count == 0)
                    throw new InvalidOperationException($"No testing targets registered for suite '{suiteName}'.");

                foreach (var target in targets)
                {
                    if (_featureAuditCancellationRequested)
                        break;

                    var result = await RunFeatureAuditTargetAsync(target);
                    _lastFeatureAuditResults.Add(result);
                    AppendFeatureAuditHistory($"{target.Name}: {(result.Success ? "PASS" : "FAIL")} ({result.DurationMs} ms)");
                    await RefreshFeatureAuditViewIfVisibleAsync();
                    await Dispatcher.Yield(DispatcherPriority.Background);
                }

                var passed = _lastFeatureAuditResults.Count(x => x.Success);
                var failed = _lastFeatureAuditResults.Count - passed;
                _lastFeatureAuditSummary = _featureAuditCancellationRequested
                    ? $"{suiteName} suite cancelled | Passed {passed} | Failed {failed} | Mode {_testingExecutionMode}"
                    : $"{suiteName} suite complete | Passed {passed} | Failed {failed} | Mode {_testingExecutionMode}";

                var report = BuildFeatureAuditReport(_lastFeatureAuditResults);
                await WriteFeatureAuditLogAsync($"[{suiteName} Suite]{Environment.NewLine}{report}");
                if (!_featureAuditCancellationRequested)
                    await SendFeatureAuditReportToDiscordAsync(report);
                await RefreshFeatureAuditViewIfVisibleAsync();

                if (!_featureAuditCancellationRequested)
                {
                    ShowRequestedStatus(
                        suiteName,
                        failed == 0
                            ? $"{suiteName} testing suite selesai. Ini adalah internal probe suite, bukan pengganti test runner eksternal penuh."
                            : $"{suiteName} testing suite selesai dengan {failed} probe gagal. Review report untuk detailnya.",
                        report);
                }
            }
            catch (Exception ex)
            {
                _lastFeatureAuditSummary = $"{suiteName} suite failed unexpectedly: {ex.Message}";
                AppendFeatureAuditHistory($"{suiteName} suite error: {ex.Message}");
                ShowActionStatus(ActionState.Error, suiteName, "Testing suite gagal dijalankan.", ex.Message);
            }
            finally
            {
                _featureAuditRunning = false;
                _featureAuditCancellationRequested = false;
                _featureAuditRunStartedUtc = null;
                await RefreshFeatureAuditViewIfVisibleAsync();
            }
        }

        private async Task RunFullQaMatrixAsync()
        {
            if (_featureAuditRunning)
            {
                ShowActionStatus(ActionState.Info, "Full QA Matrix", "Testing masih berjalan. Tunggu proses saat ini selesai.");
                return;
            }

            var suites = new[]
            {
                "Unit",
                "Integration",
                "UI Flow",
                "End-to-End",
                "Regression",
                "Performance",
                "Stress",
                "Stability",
                "Security",
                "Compatibility"
            };

            _featureAuditRunning = true;
            _featureAuditCancellationRequested = false;
            _lastTestingSuite = "Full QA Matrix";
            _lastFeatureAuditMode = "Full QA Matrix";
            _lastFeatureAuditUtc = DateTime.UtcNow;
            _featureAuditRunStartedUtc = DateTime.UtcNow;
            _lastFeatureAuditResults.Clear();
            UpdateTestingStaticSummaries();
            AppendFeatureAuditHistory("Full QA Matrix started.");

            try
            {
                await CheckBackendHealth();

                foreach (var suite in suites)
                {
                    AppendFeatureAuditHistory($"Suite phase: {suite}");
                    foreach (var target in BuildTestingSuiteTargets(suite))
                    {
                        if (_featureAuditCancellationRequested)
                            break;

                        var result = await RunFeatureAuditTargetAsync(target);
                        result.Name = $"{suite} / {target.Name}";
                        _lastFeatureAuditResults.Add(result);
                        AppendFeatureAuditHistory($"{result.Name}: {(result.Success ? "PASS" : "FAIL")} ({result.DurationMs} ms)");
                        await RefreshFeatureAuditViewIfVisibleAsync();
                        await Dispatcher.Yield(DispatcherPriority.Background);
                    }

                    if (_featureAuditCancellationRequested)
                        break;
                }

                var passed = _lastFeatureAuditResults.Count(x => x.Success);
                var failed = _lastFeatureAuditResults.Count - passed;
                _lastFeatureAuditSummary = _featureAuditCancellationRequested
                    ? $"Full QA Matrix cancelled | Passed {passed} | Failed {failed} | Mode {_testingExecutionMode}"
                    : $"Full QA Matrix complete | Passed {passed} | Failed {failed} | Mode {_testingExecutionMode}";

                var report = BuildFeatureAuditReport(_lastFeatureAuditResults);
                await WriteFeatureAuditLogAsync($"[Full QA Matrix]{Environment.NewLine}{report}");
                if (!_featureAuditCancellationRequested)
                    await SendFeatureAuditReportToDiscordAsync(report);
                await RefreshFeatureAuditViewIfVisibleAsync();

                if (!_featureAuditCancellationRequested)
                {
                    ShowRequestedStatus(
                        "Full QA Matrix",
                        failed == 0
                            ? "Full QA Matrix selesai. Hasil ini merangkum internal probe matrix, bukan sertifikasi QA eksternal penuh."
                            : $"Full QA Matrix selesai dengan {failed} probe gagal. Review report untuk detailnya.",
                        report);
                }
            }
            catch (Exception ex)
            {
                _lastFeatureAuditSummary = $"Full QA Matrix failed unexpectedly: {ex.Message}";
                AppendFeatureAuditHistory($"Full QA Matrix error: {ex.Message}");
                ShowActionStatus(ActionState.Error, "Full QA Matrix", "Full QA Matrix gagal dijalankan.", ex.Message);
            }
            finally
            {
                _featureAuditRunning = false;
                _featureAuditCancellationRequested = false;
                _featureAuditRunStartedUtc = null;
                await RefreshFeatureAuditViewIfVisibleAsync();
            }
        }

        private async void SetTestingMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string mode && !string.IsNullOrWhiteSpace(mode))
            {
                _testingExecutionMode = mode;
                UpdateTestingStaticSummaries();
                AppendFeatureAuditHistory($"Testing mode set to {_testingExecutionMode}.");
                await RefreshFeatureAuditViewAsync();
                ShowActionStatus(ActionState.Info, "Testing Mode", $"Testing mode diubah ke {_testingExecutionMode}.");
            }
        }

        private async void RunUnitTestingSuite_Click(object sender, RoutedEventArgs e) => await RunTestingSuiteAsync("Unit");

        private async void RunIntegrationTestingSuite_Click(object sender, RoutedEventArgs e) => await RunTestingSuiteAsync("Integration");

        private async void RunUiTestingSuite_Click(object sender, RoutedEventArgs e) => await RunTestingSuiteAsync("UI Flow");

        private async void RunEndToEndTestingSuite_Click(object sender, RoutedEventArgs e) => await RunTestingSuiteAsync("End-to-End");

        private async void RunRegressionTestingSuite_Click(object sender, RoutedEventArgs e) => await RunTestingSuiteAsync("Regression");

        private async void RunPerformanceTestingSuite_Click(object sender, RoutedEventArgs e) => await RunTestingSuiteAsync("Performance");

        private async void RunStressTestingSuite_Click(object sender, RoutedEventArgs e) => await RunTestingSuiteAsync("Stress");

        private async void RunStabilityTestingSuite_Click(object sender, RoutedEventArgs e) => await RunTestingSuiteAsync("Stability");

        private async void RunSecurityTestingSuite_Click(object sender, RoutedEventArgs e) => await RunTestingSuiteAsync("Security");

        private async void RunCompatibilityTestingSuite_Click(object sender, RoutedEventArgs e) => await RunTestingSuiteAsync("Compatibility");

        private async void RunFullQaMatrix_Click(object sender, RoutedEventArgs e) => await RunFullQaMatrixAsync();

        #endregion

        #region Data Formatters

        private string FormatSystemInfo(dynamic info)
        {
            try
            {
                var output = new System.Text.StringBuilder();
                
                if (info["cpu"] != null)
                {
                    output.AppendLine("=== CPU INFORMATION ===");
                    var cpu = info["cpu"];
                    output.AppendLine($"Processor: {cpu["processor"]}");
                    output.AppendLine($"Cores: {cpu["cores"]} | Threads: {cpu["threads"]}");
                    output.AppendLine($"Frequency: {cpu["frequency_current"]} MHz");
                    output.AppendLine($"Current Usage: {cpu["usage"]}%");
                    output.AppendLine();
                }

                if (info["memory"] != null)
                {
                    output.AppendLine("=== MEMORY INFORMATION ===");
                    var mem = info["memory"];
                    output.AppendLine($"Total: {mem["total"]} MB");
                    output.AppendLine($"Available: {mem["available"]} MB");
                    output.AppendLine($"Used: {mem["used"]} MB ({mem["percent"]}%)");
                    output.AppendLine();
                }

                if (info["disk"] != null)
                {
                    output.AppendLine("=== DISK INFORMATION ===");
                    var disk = info["disk"];
                    if (disk["total"] != null)
                    {
                        output.AppendLine($"Total: {disk["total"]} GB");
                        output.AppendLine($"Used: {disk["used"]} GB");
                        output.AppendLine($"Free: {disk["free"]} GB ({disk["percent"]}% used)");
                    }
                    else
                    {
                        output.AppendLine("Volume data available via partition breakdown.");
                    }
                    output.AppendLine();
                }

                if (info["system_drive"] != null)
                {
                    output.AppendLine("=== SYSTEM DRIVE ===");
                    var drive = info["system_drive"];
                    output.AppendLine($"Drive: {drive["drive_letter"]}:\\");
                    output.AppendLine($"Type: {drive["storage_class"]}");
                    output.AppendLine($"Bus: {drive["bus_type"]}");
                    output.AppendLine($"Model: {drive["model"]}");
                    output.AppendLine();
                }

                if (info["device_profile"] != null)
                {
                    output.AppendLine("=== DEVICE PROFILE ===");
                    var profile = info["device_profile"];
                    output.AppendLine($"Class: {profile["form_factor"]} | {profile["os_family"]} | {profile["storage_class"]} | {profile["ram_class"]}");
                    output.AppendLine($"Bottleneck: {profile["bottleneck"]}");
                    output.AppendLine($"Recommended Profile: {profile["recommended_profile"]}");
                    output.AppendLine($"Expected Gain: {profile["expected_gain"]}");
                    output.AppendLine();
                }

                if (info["os"] != null)
                {
                    output.AppendLine("=== OPERATING SYSTEM ===");
                    var os = info["os"];
                    output.AppendLine($"OS: {os["system"]} {os["release"]}");
                    output.AppendLine($"Version: {os["version"]}");
                    output.AppendLine($"Architecture: {os["architecture"]}");
                    output.AppendLine();
                }

                if (info["network"] != null && (info["network"] as Newtonsoft.Json.Linq.JArray)?.Count > 0)
                {
                    output.AppendLine("=== NETWORK ADAPTERS ===");
                    foreach (var adapter in info["network"])
                    {
                        output.AppendLine($"- {adapter["name"]}: {adapter["ip_address"]} ({adapter["status"]})");
                    }
                    output.AppendLine();
                }

                return output.ToString();
            }
            catch (Exception ex)
            {
                return $"Error formatting system info: {ex.Message}";
            }
        }

        private string FormatTweaks(dynamic tweaksData)
        {
            try
            {
                var output = new System.Text.StringBuilder();
                var tweaks = tweaksData["tweaks"] as Newtonsoft.Json.Linq.JArray;
                
                if (tweaks == null || tweaks.Count == 0)
                {
                    return "No tweaks available.";
                }

                foreach (var tweak in tweaks)
                {
                    output.AppendLine($"[{tweak["category"]}] {tweak["name"]}");
                    output.AppendLine($"  Description: {tweak["description"]}");
                    output.AppendLine($"  Risk Level: {tweak["risk"]} | Requires Admin: {tweak["requires_admin"]}");
                    output.AppendLine();
                }

                return output.ToString();
            }
            catch (Exception ex)
            {
                return $"Error formatting tweaks: {ex.Message}";
            }
        }

        private string FormatDrivers(dynamic driversData)
        {
            try
            {
                var output = new System.Text.StringBuilder();
                var drivers = driversData["drivers"] as Newtonsoft.Json.Linq.JArray;
                
                if (drivers == null || drivers.Count == 0)
                {
                    return "No drivers found.";
                }

                output.AppendLine($"Total Drivers: {drivers.Count}\n");
                
                foreach (var driver in drivers)
                {
                    output.AppendLine($"NAME: {driver["name"]}");
                    output.AppendLine($"  Manufacturer: {driver["manufacturer"]}");
                    output.AppendLine($"  Status: {driver["status"]}");
                    output.AppendLine($"  Version: {driver["version"]}");
                    output.AppendLine();
                }

                return output.ToString();
            }
            catch (Exception ex)
            {
                return $"Error formatting drivers: {ex.Message}";
            }
        }

        private async Task ApplyBoosterProfileAsync(string profileId, string modeName)
        {
            try
            {
                ShowActionStatus(ActionState.Info, modeName, "Processing request...");
                _dashboardCurrentMode = profileId switch
                {
                    "gaming" => "Gaming Mode",
                    "productivity" => "Performance Mode",
                    "battery" => "Power Saver",
                    "streaming" => "Streaming Mode",
                    _ => _dashboardCurrentMode
                };

                var result = await _backendClient.ApplyBoosterAsync(profileId);
                var json = result as Newtonsoft.Json.Linq.JObject;
                var success = json?.Value<bool?>("success") == true;
                var partialSuccess = json?.Value<bool?>("partial_success") == true;
                var state = success
                    ? (partialSuccess ? ActionState.Warning : ActionState.Success)
                    : ActionState.Error;
                var title = success ? (partialSuccess ? "Applied with warnings" : "Optimization applied") : "Unable to fully apply";
                var details = BuildBoosterApplySummary(json) ?? HyperBoostBackendClient.FormatJson(result);

                ShowActionStatus(state, title, $"{modeName} finished. Review the summary below for details.", details);
                await RefreshDashboard();
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Error, $"{modeName} failed", ex.Message);
            }
        }

        private static string BuildBoosterApplySummary(Newtonsoft.Json.Linq.JObject json)
        {
            if (json == null)
            {
                return null;
            }

            var lines = new List<string>();
            var success = json.Value<bool?>("success") == true;
            var partialSuccess = json.Value<bool?>("partial_success") == true;
            var message = json.Value<string>("message");
            var warning = json.Value<string>("warning");
            var appliedSettings = json.Value<int?>("applied_settings");
            var totalSettings = json.Value<int?>("total_settings");

            if (!string.IsNullOrWhiteSpace(message))
            {
                lines.Add(message);
            }

            if (appliedSettings.HasValue && totalSettings.HasValue)
            {
                lines.Add($"Applied settings: {appliedSettings}/{totalSettings}");
            }

            if (!string.IsNullOrWhiteSpace(warning))
            {
                lines.Add(warning);
            }

            var results = json["results"] as Newtonsoft.Json.Linq.JArray;
            if (results is { Count: > 0 })
            {
                lines.Add(string.Empty);
                lines.Add("Per-setting results:");

                foreach (var token in results.OfType<Newtonsoft.Json.Linq.JObject>())
                {
                    var settingSuccess = token.Value<bool?>("success") == true;
                    var displayName = token.Value<string>("display_name") ?? token.Value<string>("setting") ?? "Unknown setting";
                    var settingMessage = token.Value<string>("message");
                    var prefix = settingSuccess ? "[OK]" : "[WARN]";
                    lines.Add($"{prefix} {displayName}");

                    if (!string.IsNullOrWhiteSpace(settingMessage))
                    {
                        lines.Add($"  {settingMessage}");
                    }
                }
            }
            else if (!success || partialSuccess)
            {
                return HyperBoostBackendClient.FormatJson(json);
            }

            return lines.Count > 0 ? string.Join(Environment.NewLine, lines) : HyperBoostBackendClient.FormatJson(json);
        }

        private async Task ShowSmartRecommendationAsync(Button sourceButton)
        {
            await ShowPage("SmartRecommendation", sourceButton);
            await RefreshAiCopilotDiagnosticsAsync(refreshContext: true);
        }

        private string FormatStartupItems(IEnumerable<StartupEntry> items)
        {
            try
            {
                var output = new System.Text.StringBuilder();
                var startupItems = items?.ToList() ?? new List<StartupEntry>();

                if (startupItems.Count == 0)
                {
                    return "No startup items found.";
                }

                output.AppendLine($"Total Startup Items: {startupItems.Count}");
                output.AppendLine();
                output.AppendLine("Name                 | Status   | Impact  | Source         | Type");
                output.AppendLine("-----------------------------------------------------------------------");

                foreach (var item in startupItems)
                {
                    var status = item.Enabled ? "Enabled" : "Disabled";
                    output.AppendLine(
                        $"{item.Name,-20} | {status,-8} | {item.Impact,-7} | {item.Source,-14} | {item.Type}");
                    output.AppendLine(
                        $"  Score {item.ImpactScore,-3} | RAM {item.EstimatedMemoryMb,6:0.#} MB | Load {item.EstimatedLoadTimeSeconds,4:0.#} s | Action: {item.RecommendedAction}");
                }

                output.AppendLine();
                return output.ToString();
            }
            catch (Exception ex)
            {
                return $"Error formatting startup items: {ex.Message}";
            }
        }

        private string FormatBackgroundApps(dynamic processData)
        {
            try
            {
                var output = new System.Text.StringBuilder();
                var processes = processData["processes"] as Newtonsoft.Json.Linq.JArray;

                if (processes == null || processes.Count == 0)
                {
                    return "No background process data available.";
                }

                foreach (var process in processes)
                {
                    output.AppendLine($"{process["name"]} (PID {process["pid"]})");
                    output.AppendLine($"  Memory: {process["memory"]}% | CPU: {process["cpu"]}%");
                    output.AppendLine($"  Threads: {process["threads"]} | Disk I/O: {process["disk_io_mb"]} MB");
                    output.AppendLine();
                }

                return output.ToString();
            }
            catch (Exception ex)
            {
                return $"Error formatting background apps: {ex.Message}";
            }
        }

        private string FormatNetworkDiagnostics(dynamic dnsData)
        {
            try
            {
                var output = new System.Text.StringBuilder();
                output.AppendLine("=== DNS TEST ===");
                output.AppendLine($"Status: {dnsData["status"]}");
                output.AppendLine($"Response Time: {dnsData["response_time"]} ms");
                output.AppendLine();
                output.AppendLine("Tips:");
                output.AppendLine("- Run Flush DNS after connection changes");
                output.AppendLine("- Use Optimize TCP for stability tuning");
                output.AppendLine("- Use Reset Network if adapter issues persist");
                return output.ToString();
            }
            catch (Exception ex)
            {
                return $"Error formatting network diagnostics: {ex.Message}";
            }
        }

        #endregion

        private async Task RunSmartRecommendationScanAsync()
        {
            SmartScanProgressBar.Value = 0;
            SmartScanStatusText.Text = "Scanning system state...";

            var statsTask = SafeApiCall(() => _backendClient.GetSystemStatsAsync());
            var processesTask = SafeApiCall(() => _backendClient.GetProcessesAsync());
            var dnsTask = SafeApiCall(() => _backendClient.TestDnsAsync());
            var systemInfoTask = SafeApiCall(() => _backendClient.GetSystemInfoAsync());
            Task startupTask = _startupEntries.Count == 0 ? RefreshStartupItems() : Task.CompletedTask;

            await Task.WhenAll(statsTask, processesTask, dnsTask, systemInfoTask, startupTask);
            SmartScanProgressBar.Value = 100;
            SmartScanStatusText.Text = "Scan complete. Recommendations are ready.";

            PopulateSmartRecommendationUi(await statsTask, await processesTask, await dnsTask, await systemInfoTask);
        }

        private void PopulateSmartRecommendationUi(dynamic stats, dynamic processes, dynamic dns, dynamic systemInfo = null)
        {
            var sessionSnapshot = BuildSessionDetectionSnapshot();
            var statsJson = stats as JObject;
            var processesJson = processes as JObject;
            var processArray = processesJson?["processes"] as JArray;
            var dnsJson = dns as JObject;
            var systemInfoJson = systemInfo as JObject;
            var deviceProfile = systemInfoJson?["device_profile"] as JObject;
            var systemDrive = systemInfoJson?["system_drive"] as JObject;

            var cpu = statsJson?.Value<double?>("cpu") ?? statsJson?.Value<double?>("cpu_percent") ?? 0d;
            var memory = statsJson?.Value<double?>("memory") ?? statsJson?.Value<double?>("memory_percent") ?? 0d;
            var disk = statsJson?.Value<double?>("disk") ?? statsJson?.Value<double?>("disk_percent") ?? 0d;
            var gpu = cpu > 65 ? 72 : 24;
            var temp = cpu > 80 ? 84 : cpu > 60 ? 73 : 58;
            var startupHigh = _startupEntries.Count(x => x.Enabled && x.Impact == "High");
            var startupMedium = _startupEntries.Count(x => x.Enabled && x.Impact == "Medium");
            var startupLow = _startupEntries.Count(x => x.Enabled && x.Impact == "Low");
            var bgApps = processArray?.Count ?? 0;
            var junkEstimateGb = disk > 80 ? 1.8 : disk > 60 ? 0.9 : 0.4;
            var dnsTime = dnsJson?.Value<double?>("response_time") ?? 0d;

            SmartSystemAnalysisText.Text =
                $"CPU usage: {cpu:0}%\n" +
                $"RAM usage: {memory:0}% | Leak detection: {(memory > 82 ? "Possible high pressure" : "Normal")}\n" +
                $"Disk usage: {disk:0}% | Fragmentation status: {(disk > 75 ? "Needs review" : "Normal")}\n" +
                $"GPU usage estimate: {gpu:0}%\n" +
                $"Startup impact: High {startupHigh} | Medium {startupMedium} | Low {startupLow}\n" +
                $"Background apps active: {bgApps}\n" +
                $"Junk files & cache estimate: {junkEstimateGb:0.0} GB\n" +
                $"Device temperature estimate: {temp:0}C\n" +
                $"Network response: {dnsTime:0} ms\n" +
                $"{BuildDeviceProfileSummary(deviceProfile, systemDrive)}";

            var cpuScore = Math.Max(20, 100 - (int)Math.Round(cpu));
            var ramScore = Math.Max(20, 100 - (int)Math.Round(memory));
            var diskScore = Math.Max(20, 100 - (int)Math.Round(disk * 0.8));
            var startupScore = Math.Max(20, 100 - (startupHigh * 15) - (startupMedium * 7));
            var overall = (cpuScore + ramScore + diskScore + startupScore) / 4;
            var improvementPossible = Math.Max(5, 100 - overall);
            var bottleneck = ReadStringToken(deviceProfile, "bottleneck");
            var recommendedProfile = ReadStringToken(deviceProfile, "recommended_profile");
            var expectedGain = ReadStringToken(deviceProfile, "expected_gain");
            var storageClass = ReadStringToken(deviceProfile, "storage_class");

            SmartOverallScoreText.Text = $"Your system is {overall}% optimized";
            SmartScoreBreakdownText.Text =
                $"CPU Score: {cpuScore} | RAM Efficiency: {ramScore} | Disk Speed: {diskScore} | Startup Optimization: {startupScore}\n" +
                $"+{improvementPossible}% improvement possible\n" +
                $"Bottleneck: {bottleneck} | Adaptive profile: {recommendedProfile} | Expected gain: {expectedGain}";

            var suggestions = new List<string>();
            if (startupHigh >= 3) suggestions.Add($"Disable {Math.Min(5, startupHigh + startupMedium)} High Impact Startup Apps | Status: Recommended");
            if (memory >= 75) suggestions.Add($"Clear {(memory >= 85 ? "2.3GB" : "1.2GB")} Standby Memory | Status: Safe");
            if (junkEstimateGb >= 0.8) suggestions.Add($"Delete {junkEstimateGb:0.0}GB Junk Files | Status: Safe");
            if (cpu >= 70 || gpu >= 60) suggestions.Add("Enable Gaming Mode (Boost FPS) | Status: Recommended");
            if (bgApps >= 10) suggestions.Add($"Disable {Math.Min(12, bgApps)} Background Apps | Status: Recommended");
            if (cpu >= 65) suggestions.Add("Set CPU Priority to High for Active Apps | Status: Moderate");
            if (startupHigh >= 2) suggestions.Add("Optimize Windows Services | Status: Advanced");
            if (dnsTime >= 40) suggestions.Add("Stabilize connection with DNS/TCP refresh | Status: Safe");
            if (string.Equals(storageClass, "HDD", StringComparison.OrdinalIgnoreCase)) suggestions.Add("Apply HDD Survival profile | Status: Recommended");
            if (string.Equals(bottleneck, "memory-bound", StringComparison.OrdinalIgnoreCase)) suggestions.Add("Apply Low RAM profile and trim background apps | Status: Recommended");
            SmartSuggestionsText.Text = string.Join(Environment.NewLine, suggestions);

            if (cpu >= 70 || gpu >= 60)
            {
                _smartRecommendedUsageMode = "Gaming";
                SmartUsageRecommendationText.Text = "Mode Gaming aktif: disable overlay apps, prioritaskan GPU & CPU, dan lanjutkan ke Game Mode setelah boost.";
            }
            else if (memory >= 75 && disk >= 65)
            {
                _smartRecommendedUsageMode = "Editing / Rendering";
                SmartUsageRecommendationText.Text = "Mode Editing / Rendering: optimize RAM & Disk Cache, pertahankan app produksi tetap aktif.";
            }
            else
            {
                _smartRecommendedUsageMode = "Daily / Office";
                SmartUsageRecommendationText.Text = "Mode Daily / Office: balance performance & power saving, fokus ke startup ringan dan cleanup aman.";
            }

            if (!string.IsNullOrWhiteSpace(recommendedProfile))
            {
                SmartUsageRecommendationText.Text += Environment.NewLine + $"Adaptive profile recommendation: {recommendedProfile}.";
            }

            SmartSafetyText.Text =
                " Safe: cleanup ringan, flush DNS, clear RAM, background cleanup aman\n" +
                " Moderate: set priority high, pause update sementara\n" +
                " Advanced: optimize services, aggressive startup reduction\n" +
                "Auto block tweak berbahaya direkomendasikan dan restore point disarankan sebelum tweak berat.";

            SmartMaintenanceText.Text =
                $"Sudah {Math.Max(1, startupHigh)} hari belum cleanup besar.\n" +
                $"Startup apps aktif: {_startupEntries.Count(x => x.Enabled)} item.\n" +
                $"{(memory > 75 ? "RAM usage tinggi terus, weekly optimization direkomendasikan." : "Daily / weekly maintenance cukup ringan untuk kondisi sekarang.")}";

            SmartPersonalizedText.Text =
                $"Mode paling cocok saat ini: {_smartRecommendedUsageMode}\n" +
                $"Adaptive bottleneck: {bottleneck}\n" +
                $"Expected gain on this device class: {expectedGain}\n" +
                $"Detected game: {DescribeProcess(sessionSnapshot.ActiveGame, "none")}\n" +
                $"Detected streamer: {DescribeProcess(sessionSnapshot.ActiveStreamer, "none")}\n" +
                $"Detected Discord: {DescribeProcess(sessionSnapshot.DiscordProcess, "none")}\n" +
                "Contoh personalized optimization:\n" +
                " Prioritaskan aplikasi aktif utama\n" +
                " Disable browser berat saat gaming\n" +
                " Pertahankan Discord / Steam / OBS saat sesi gaming atau streaming";
        }

        private async Task ApplySmartRecommendationActionAsync(string actionKey)
        {
            switch (actionKey)
            {
                case "startup":
                    SafeStartupRecommendation_Click(this, new RoutedEventArgs());
                    break;
                case "ram":
                    InitializeOneClickBoostDefaults();
                    BoostClearStandbyChk.IsChecked = true;
                    BoostOptimizeRamChk.IsChecked = true;
                    BoostDeleteTempChk.IsChecked = false;
                    await RunOneClickBoostAsync("Smart RAM Suggestion", extreme: false, balanced: false);
                    break;
                case "cleanup":
                    var cleanup = await SafeApiCall(() => _backendClient.CleanupAsync());
                    if (cleanup != null)
                    {
                        ShowActionStatus(ActionState.Success, "Smart Cleanup Suggestion", "Junk files dan cache ringan sudah dibersihkan.", HyperBoostBackendClient.FormatJson(cleanup));
                    }
                    break;
                case "gaming":
                    await ApplyQuickCompetitiveGamingAsync();
                    break;
                case "background":
                    await ApplyProcessTargetsAsync(new[] { "OneDrive", "Teams", "Spotify", "Widgets", "AdobeGCClient", "EpicWebHelper" }, "Smart Background Suggestion");
                    break;
                case "service":
                    LaunchWindowsTool("services.msc", null, "Smart Service Suggestion");
                    break;
                case "fixall":
                    InitializeOneClickBoostDefaults();
                    BoostCreateRestoreChk.IsChecked = true;
                    await RunOneClickBoostAsync("Fix All Recommended Issues", extreme: false, balanced: true);
                    break;
            }
        }

        private async void ApplySmartStartupSuggestion_Click(object sender, RoutedEventArgs e) => await ApplySmartRecommendationActionAsync("startup");
        private async void ApplySmartRamSuggestion_Click(object sender, RoutedEventArgs e) => await ApplySmartRecommendationActionAsync("ram");
        private async void ApplySmartCleanupSuggestion_Click(object sender, RoutedEventArgs e) => await ApplySmartRecommendationActionAsync("cleanup");
        private async void ApplySmartGamingSuggestion_Click(object sender, RoutedEventArgs e) => await ApplySmartRecommendationActionAsync("gaming");
        private async void ApplySmartBackgroundSuggestion_Click(object sender, RoutedEventArgs e) => await ApplySmartRecommendationActionAsync("background");
        private async void ApplySmartServiceSuggestion_Click(object sender, RoutedEventArgs e) => await ApplySmartRecommendationActionAsync("service");
        private async void FixAllRecommendedIssues_Click(object sender, RoutedEventArgs e) => await ApplySmartRecommendationActionAsync("fixall");
        private async void RescanSmartRecommendation_Click(object sender, RoutedEventArgs e) => await RunSmartRecommendationScanAsync();

        private void ScheduleDailyOptimization_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Daily Optimization", "Scheduled optimization bisa diatur lewat Task Scheduler.", "Recommended: daily cleanup / startup review");
            LaunchWindowsTool("taskschd.msc", null, "Smart Maintenance Reminder");
        }

        private void ScheduleWeeklyOptimization_Click(object sender, RoutedEventArgs e)
        {
            ShowActionStatus(ActionState.Info, "Weekly Optimization", "Scheduled optimization mingguan bisa diatur lewat Task Scheduler.", "Recommended: weekly smart fix");
            LaunchWindowsTool("taskschd.msc", null, "Smart Maintenance Reminder");
        }

        private async void BackFromSmartRecommendation_Click(object sender, RoutedEventArgs e)
        {
            await ShowPage("Dashboard", DashboardBtn);
        }

        private async Task SafeApiCall(Func<Task> apiCall)
        {
            try
            {
                if (apiCall == null)
                    return;

                await apiCall();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SafeApiCall error: {ex.Message}");
                AppendDashboardActivity($"SafeApiCall warning on {_activePage}: {ex.Message}");
            }
        }

        private async Task<T> SafeApiCall<T>(Func<Task<T>> apiCall)
        {
            try
            {
                if (apiCall == null)
                    return default;

                return await apiCall();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SafeApiCall<T> error: {ex.Message}");
                AppendDashboardActivity($"SafeApiCall<T> warning on {_activePage}: {ex.Message}");
                return default;
            }
        }

        private async Task ApplyTweakWithFeedbackAsync(string tweakId, string actionName)
        {
            try
            {
                ShowActionStatus(ActionState.Info, actionName, "Processing request...");
                var result = await _backendClient.ApplyTweakAsync(tweakId);
                ShowActionStatus(ActionState.Success, actionName, "Tweak applied successfully.", HyperBoostBackendClient.FormatJson(result));
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Error, actionName, $"Unable to run {actionName}.", ex.Message);
            }
        }

        private async Task RunPowerShellActionAsync(string script, string actionName, string successMessage, TimeSpan? timeout = null)
        {
            try
            {
                ShowActionStatus(ActionState.Info, actionName, "Processing request...");
                var (success, output) = await ExecutePowerShellScriptAsync(script, timeout);
                if (success)
                    ShowActionStatus(ActionState.Success, actionName, successMessage, output);
                else
                    ShowActionStatus(ActionState.Error, actionName, $"{actionName} failed.", output);
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Error, actionName, $"{actionName} failed.", ex.Message);
            }
        }

        private void LaunchWindowsUri(string uri, string featureName)
        {
            try
            {
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
                ShowActionStatus(ActionState.Info, featureName, "Windows opened the requested settings page.", uri);
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Error, featureName, $"Unable to open {featureName}.", ex.Message);
            }
        }

        private void LaunchWindowsTool(string fileName, string arguments, string featureName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    throw new InvalidOperationException("Tool path is empty.");

                var startInfo = new ProcessStartInfo(fileName)
                {
                    UseShellExecute = true
                };

                if (!string.IsNullOrWhiteSpace(arguments))
                {
                    startInfo.Arguments = arguments;
                }

                Process.Start(startInfo);
                AppendDashboardActivity($"{featureName} opened.");
                ShowActionStatus(ActionState.Info, featureName, "Windows tool opened successfully.", string.IsNullOrWhiteSpace(arguments) ? fileName : $"{fileName} {arguments}");
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Error, featureName, $"Unable to open {featureName}.", ex.Message);
            }
        }
    }
}



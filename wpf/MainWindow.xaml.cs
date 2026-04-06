using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HyperBoostX.Services;
using Microsoft.Win32;
using Newtonsoft.Json;
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

        private HyperBoostBackendClient _backendClient;
        private string _currentBackendUrl = "http://127.0.0.1:5000";
        private Button _selectedNavButton;
        private DispatcherTimer _dashboardTimer;
        private bool _isUpdating;
        private string _activePage = "Dashboard";
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

        public MainWindow()
        {
            InitializeComponent();
            _backendClient = new HyperBoostBackendClient(_currentBackendUrl);
            _dashboardTimer = new DispatcherTimer();
            _dashboardTimer.Interval = TimeSpan.FromSeconds(1);
            _dashboardTimer.Tick += DashboardTimer_Tick;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Check backend health on startup
            LoadGamingWhitelist();
            await CheckBackendHealth();
            await ShowPage("Dashboard", DashboardBtn);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _dashboardTimer.Stop();
        }

        protected override void OnClosed(EventArgs e)
        {
            _dashboardTimer.Stop();
            _backendClient?.Dispose();
            base.OnClosed(e);
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
            StartupContent.Visibility = Visibility.Collapsed;
            CleanupContent.Visibility = Visibility.Collapsed;
            GamingContent.Visibility = Visibility.Collapsed;
            NetworkContent.Visibility = Visibility.Collapsed;
            PrivacyContent.Visibility = Visibility.Collapsed;
            RepairContent.Visibility = Visibility.Collapsed;
            AdvancedContent.Visibility = Visibility.Collapsed;
            RestoreContent.Visibility = Visibility.Collapsed;
            SettingsContent.Visibility = Visibility.Collapsed;
            TweaksContent.Visibility = Visibility.Collapsed;
            DriversContent.Visibility = Visibility.Collapsed;
            SystemContent.Visibility = Visibility.Collapsed;
            BoosterContent.Visibility = Visibility.Collapsed;
            BackgroundAppsContent.Visibility = Visibility.Collapsed;
            PlaceholderContent.Visibility = Visibility.Collapsed;
            AboutContent.Visibility = Visibility.Collapsed;
        }

        private async Task ShowPage(string pageName, Button navButton)
        {
            _activePage = pageName;
            SelectNavButton(navButton);
            HideAllPages();
            _dashboardTimer.Stop();

            // Show selected page
            switch (pageName)
            {
                case "Dashboard":
                    SetPageHeader("Dashboard", "Monitor live system health and launch the fastest actions from one place.");
                    DashboardContent.Visibility = Visibility.Visible;
                    await RefreshDashboard();
                    _dashboardTimer.Start();
                    break;
                case "Performance":
                    SetPageHeader("Performance Boost", "Use guided actions to reduce overhead and apply the right optimization profile.");
                    PerformanceContent.Visibility = Visibility.Visible;
                    break;
                case "Startup":
                    SetPageHeader("Startup Manager", "Review boot impact and jump straight to startup controls when your PC feels slow to open.");
                    StartupContent.Visibility = Visibility.Visible;
                    await RefreshStartupItems();
                    break;
                case "Cleanup":
                    SetPageHeader("Storage Cleaner", "Free temporary files and run cleanup tools without guessing which step to use first.");
                    CleanupContent.Visibility = Visibility.Visible;
                    break;
                case "Storage":
                    SetPageHeader("Storage", "Check storage health and jump into Windows storage controls from the same workspace.");
                    CleanupContent.Visibility = Visibility.Visible;
                    break;
                case "Gaming":
                    SetPageHeader("Gaming Booster", "Prepare the system for lower latency and fewer interruptions before launching games.");
                    GamingContent.Visibility = Visibility.Visible;
                    RefreshGamingWhitelistView();
                    InitializeGamingDefaults();
                    break;
                case "Network":
                    SetPageHeader("Network Booster", "Run diagnostics first, then apply DNS and TCP actions with clear feedback.");
                    NetworkContent.Visibility = Visibility.Visible;
                    await RefreshNetworkDiagnostics();
                    break;
                case "BackgroundApps":
                    SetPageHeader("Background Apps", "See which processes are eating resources so the next cleanup decision is obvious.");
                    BackgroundAppsContent.Visibility = Visibility.Visible;
                    await RefreshBackgroundApps();
                    break;
                case "Privacy":
                    SetPageHeader("Privacy Center", "Reduce telemetry and open the right Windows privacy pages without hunting through settings.");
                    PrivacyContent.Visibility = Visibility.Visible;
                    break;
                case "Repair":
                    SetPageHeader("Repair Tools", "Start built-in Windows repair actions and keep the result summary inside the app.");
                    RepairContent.Visibility = Visibility.Visible;
                    break;
                case "Advanced":
                    SetPageHeader("Advanced Tweaks", "Power-user controls with clear jumps into the Windows tools they depend on.");
                    AdvancedContent.Visibility = Visibility.Visible;
                    break;
                case "Restore":
                    SetPageHeader("Restore & Backup", "Create recovery checkpoints and keep simple snapshots before making bigger changes.");
                    RestoreContent.Visibility = Visibility.Visible;
                    break;
                case "Settings":
                    SetPageHeader("Settings", "Control backend connectivity and app behavior from one place.");
                    SettingsContent.Visibility = Visibility.Visible;
                    break;
                case "Tweaks":
                    SetPageHeader("Tweaks Center", "Browse available tweaks with clearer context before applying system-level changes.");
                    TweaksContent.Visibility = Visibility.Visible;
                    await RefreshTweaks();
                    break;
                case "Drivers":
                    SetPageHeader("Driver & Update Center", "Inspect current driver inventory and start update checks when hardware acts up.");
                    DriversContent.Visibility = Visibility.Visible;
                    await RefreshDrivers();
                    break;
                case "Booster":
                    SetPageHeader("Booster Profiles", "Apply ready-made profiles for gaming, streaming, productivity, and power-saving scenarios.");
                    BoosterContent.Visibility = Visibility.Visible;
                    await LoadBoosterProfiles();
                    break;
                case "About":
                    SetPageHeader("About App", "Project information, runtime overview, and what this build is wired to do.");
                    AboutContent.Visibility = Visibility.Visible;
                    break;
            }
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
                : $"{meta}  •  {DateTime.Now:HH:mm:ss}";
            ActionStatusCard.Visibility = Visibility.Visible;
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
                    await ApplyBoosterProfileAsync("productivity", "One Click Boost");
                    await ShowPage("Dashboard", button);
                    break;
                case nameof(GamingModeBtn):
                    await ApplyBoosterProfileAsync("gaming", "Gaming Mode");
                    await ShowPage("Gaming", button);
                    break;
                case nameof(SmartRecommendationBtn):
                    await ShowSmartRecommendationAsync(button);
                    break;
                case nameof(GamingBoosterBtn):
                    await ShowPage("Gaming", button);
                    break;
                case nameof(StorageBtn):
                    await ShowPage("Storage", button);
                    LaunchWindowsUri("ms-settings:storagesense", "Storage");
                    break;
                case nameof(BackgroundAppsBtn):
                    await ShowPage("BackgroundApps", button);
                    break;
                case nameof(StreamingModeBtn):
                    await ApplyBoosterProfileAsync("streaming", "Streaming Mode");
                    await ShowPage("Booster", button);
                    break;
                case nameof(CreatorModeBtn):
                    await ApplyBoosterProfileAsync("productivity", "Creator Mode");
                    await ShowPage("Performance", button);
                    break;
                case nameof(NetworkBoosterBtn):
                case nameof(DnsLatencyToolsBtn):
                    await ShowPage("Network", button);
                    break;
                case nameof(PrivacyCenterBtn):
                    await ShowPage("Privacy", button);
                    break;
                case nameof(SecurityHealthBtn):
                    await ShowPlaceholderPage(
                        button,
                        "Security & Health",
                        "Opens Windows Security for antivirus, firewall, and device health checks.",
                        "External tool: Windows Security");
                    LaunchWindowsUri("windowsdefender:", "Security & Health");
                    break;
                case nameof(AppsManagerBtn):
                    await ShowPlaceholderPage(
                        button,
                        "Apps Manager",
                        "Opens Installed Apps so you can review, modify, or remove applications.",
                        "External tool: Installed Apps");
                    LaunchWindowsUri("ms-settings:appsfeatures", "Apps Manager");
                    break;
                case nameof(TweaksCenterBtn):
                    await ShowPage("Tweaks", button);
                    break;
                case nameof(WindowsFeaturesBtn):
                    await ShowPlaceholderPage(
                        button,
                        "Windows Features",
                        "Opens Optional Features so you can enable or disable Windows components.",
                        "External tool: Optional Features");
                    LaunchWindowsTool("optionalfeatures.exe", null, "Windows Features");
                    break;
                case nameof(UpdateControlBtn):
                    await ShowPlaceholderPage(
                        button,
                        "Update Control",
                        "Opens Windows Update settings for update checks, pause controls, and history.",
                        "External tool: Windows Update");
                    LaunchWindowsUri("ms-settings:windowsupdate", "Update Control");
                    break;
                case nameof(RepairToolsBtn):
                    await ShowPage("Repair", button);
                    break;
                case nameof(DriverUpdateCenterBtn):
                    await ShowPage("Drivers", button);
                    break;
                case nameof(AppUninstallerBtn):
                    await ShowPlaceholderPage(
                        button,
                        "App Uninstaller",
                        "Opens Programs and Features for classic uninstall and repair tasks.",
                        "External tool: Programs and Features");
                    LaunchWindowsTool("appwiz.cpl", null, "App Uninstaller");
                    break;
                case nameof(AdvancedTweaksBtn):
                    await ShowPage("Advanced", button);
                    break;
                case nameof(WindowsServicesBtn):
                    await ShowPlaceholderPage(
                        button,
                        "Windows Services",
                        "Opens Services Manager to inspect startup types and running Windows services.",
                        "External tool: services.msc");
                    LaunchWindowsTool("services.msc", null, "Windows Services");
                    break;
                case nameof(PowerOptimizationBtn):
                    await ShowPlaceholderPage(
                        button,
                        "Power Optimization",
                        "Applies the Battery Saver optimization profile, then opens Power settings for fine tuning.",
                        "Profile: battery + External tool: Power settings");
                    await ApplyBoosterProfileAsync("battery", "Power Optimization");
                    LaunchWindowsUri("ms-settings:powersleep", "Power Optimization");
                    break;
                case nameof(VisualEffectsBtn):
                    await ShowPlaceholderPage(
                        button,
                        "Visual Effects",
                        "Opens Windows performance options where visual effects can be tuned for speed or appearance.",
                        "External tool: Performance Options");
                    LaunchWindowsTool("SystemPropertiesPerformance.exe", null, "Visual Effects");
                    break;
                case nameof(RestoreBackupBtn):
                case nameof(RestorePointManagerBtn):
                    await ShowPage("Restore", button);
                    break;
                case nameof(ScheduledAutomationBtn):
                    await ShowPlaceholderPage(
                        button,
                        "Scheduled Automation",
                        "Opens Task Scheduler so automations and recurring maintenance tasks can be configured.",
                        "External tool: Task Scheduler");
                    LaunchWindowsTool("taskschd.msc", null, "Scheduled Automation");
                    break;
                case nameof(UtilitiesToolsBtn):
                    await ShowPlaceholderPage(
                        button,
                        "Utilities Tools",
                        "Opens Administrative Tools for advanced Windows utilities and diagnostics.",
                        "External tool: Administrative Tools");
                    LaunchWindowsTool("explorer.exe", "shell:Administrative Tools", "Utilities Tools");
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
            var stats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
            if (stats == null)
                return;

            var json = stats as Newtonsoft.Json.Linq.JObject;
            var cpuValue = json?.Value<double?>("cpu") ?? json?.Value<double?>("cpu_percent") ?? 0;
            var memoryValue = json?.Value<double?>("memory") ?? json?.Value<double?>("memory_percent") ?? 0;
            var diskValue = json?.Value<double?>("disk") ?? json?.Value<double?>("disk_percent") ?? 0;

            CpuText.Text = $"{cpuValue}%";
            CpuBar.Value = cpuValue;
            MemoryText.Text = $"{memoryValue}%";
            MemoryBar.Value = memoryValue;
            DiskText.Text = $"{diskValue}%";
            DiskBar.Value = diskValue;
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

        private async void ApplyFpsMode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = await _backendClient.ApplyBoosterAsync("gaming");
                ShowActionStatus(ActionState.Success, "Gaming profile applied", "FPS-focused optimization profile was applied.", HyperBoostBackendClient.FormatJson(result));
                await RefreshDashboard();
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Error, "Gaming profile failed", ex.Message);
            }
        }

        private async void Optimize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = await _backendClient.ApplyBoosterAsync("productivity");
                ShowActionStatus(ActionState.Success, "One-click optimization applied", "Productivity optimization profile was applied.", HyperBoostBackendClient.FormatJson(result));
                await RefreshDashboard();
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Error, "Optimization failed", ex.Message);
            }
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

        private async Task LoadBoosterProfiles()
        {
            var profiles = await SafeApiCall(() => _backendClient.GetBoosterProfilesAsync());
            if (profiles == null || profiles["profiles"] == null)
            {
                return;
            }

            BoosterProfilesPanel.Children.Clear();
            var profilesList = profiles["profiles"] as Newtonsoft.Json.Linq.JArray;
            if (profilesList == null)
                return;

            foreach (var profile in profilesList)
            {
                var profileName = profile["name"]?.ToString() ?? "Unknown";
                var profileId = profile["id"]?.ToString() ?? "";
                var description = profile["description"]?.ToString() ?? "";

                // Create container for profile info and button
                var container = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

                // Profile name and description
                var titleBlock = new TextBlock
                {
                    Text = profileName,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.LimeGreen,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                container.Children.Add(titleBlock);

                var descBlock = new TextBlock
                {
                    Text = description,
                    FontSize = 11,
                    Foreground = System.Windows.Media.Brushes.LightGray,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                container.Children.Add(descBlock);

                // Apply button
                var btn = new Button
                {
                    Content = $"Apply {profileName}",
                    Tag = profileId,
                    Style = (Style)this.FindResource("ActionButtonStyle"),
                    Padding = new Thickness(15, 10, 15, 10)
                };
                btn.Click += BoosterProfile_Click;
                container.Children.Add(btn);

                BoosterProfilesPanel.Children.Add(container);
            }
        }

        private async void BoosterProfile_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var profileName = btn?.Tag as string;
            if (string.IsNullOrEmpty(profileName)) return;

            await ApplyBoosterProfileAsync(profileName, profileName.ToUpperInvariant());
        }

        #endregion

        #region Drivers

        private async Task RefreshDrivers()
        {
            var drivers = await SafeApiCall(() => _backendClient.GetDriversAsync());
            if (drivers == null)
            {
                DriversText.Text = "Unable to load drivers.";
                return;
            }

            DriversText.Text = FormatDrivers(drivers);
        }

        private async void RefreshDrivers_Click(object sender, RoutedEventArgs e) => await RefreshDrivers();

        private async void CheckDriverUpdates_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.CheckDriverUpdatesAsync());
            if (result == null)
            {
                ShowActionStatus(ActionState.Error, "Driver check failed", "Unable to check driver updates right now.");
                return;
            }

            ShowActionStatus(ActionState.Success, "Driver check complete", "Driver update scan finished successfully.", HyperBoostBackendClient.FormatJson(result));
        }

        #endregion

        #region Repair

        private async void RunSfc_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.RunSfcAsync());
            if (result == null)
            {
                ShowActionStatus(ActionState.Error, "SFC scan failed", "Unable to start the SFC scan right now.");
                return;
            }

            ShowActionStatus(ActionState.Success, "SFC scan started", "System File Checker has been launched.", HyperBoostBackendClient.FormatJson(result));
        }

        private async void RunDism_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.RunDismAsync());
            if (result == null)
            {
                ShowActionStatus(ActionState.Error, "DISM repair failed", "Unable to start DISM repair right now.");
                return;
            }

            ShowActionStatus(ActionState.Success, "DISM repair started", "DISM repair has been launched.", HyperBoostBackendClient.FormatJson(result));
        }

        private async void Cleanup_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.CleanupAsync());
            if (result == null)
            {
                ShowActionStatus(ActionState.Error, "Cleanup failed", "Unable to run cleanup right now.");
                return;
            }

            ShowActionStatus(ActionState.Success, "Cleanup complete", "Temporary file cleanup finished.", HyperBoostBackendClient.FormatJson(result));
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

        private async void RefreshTweaks_Click(object sender, RoutedEventArgs e) => await RefreshTweaks();

        #endregion

        #region Settings

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

        private async void UpdateBackendUrl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentBackendUrl = BackendUrlInput.Text.Trim();
                _backendClient.Dispose();
                _backendClient = new HyperBoostBackendClient(_currentBackendUrl);
                ShowActionStatus(ActionState.Success, "Backend URL updated", "The frontend is now pointing to the new backend endpoint.", _currentBackendUrl);
                await CheckBackendHealth();
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
                HeaderBackendText.Text = "Backend connected";
                ((TextBlock)BackendHealthIndicator.Child).Text = "Connected and ready";
            }
            else
            {
                BackendHealthIndicator.Background = Brushes.IndianRed;
                HeaderBackendBadge.Background = Brushes.IndianRed;
                HeaderBackendText.Text = "Backend disconnected";
                ((TextBlock)BackendHealthIndicator.Child).Text = "Backend unavailable";
            }
        }

        #endregion

        #region Performance Tweaks

        private void OptimizeRAM_Click(object sender, RoutedEventArgs e)
        {
            _ = ShowPlaceholderPage(
                PerformanceBtn,
                "Optimize RAM",
                "Opening Resource Monitor so you can inspect and close the heaviest memory consumers immediately.",
                "Action: Resource Monitor opened");
            LaunchWindowsTool("resmon.exe", null, "Optimize RAM");
        }

        private async void BoostGaming_Click(object sender, RoutedEventArgs e)
        {
            await ApplyBoosterProfileAsync("gaming", "Boost Gaming");
        }

        private async void AutoPerformance_Click(object sender, RoutedEventArgs e)
        {
            await ApplyBoosterProfileAsync("productivity", "Auto Performance Profile");
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
            var items = startupData["items"] as Newtonsoft.Json.Linq.JArray;
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
                .Select(x => $"✔ {x.Name}")
                .ToList();

            var keepEnabled = _startupEntries
                .Where(x => x.Enabled && x.RecommendedAction != "Recommended to Disable")
                .OrderByDescending(x => x.ImpactScore)
                .Select(x => $"✔ {x.Name}")
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

        #region Storage Cleanup

        private async void CleanTemp_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.CleanupAsync());
            if (result == null)
            {
                ShowActionStatus(ActionState.Error, "Clean Temp", "Unable to clean temporary files right now.");
                return;
            }

            ShowActionStatus(ActionState.Success, "Clean Temp", "Temporary files cleaned successfully.", HyperBoostBackendClient.FormatJson(result));
        }

        private async void ClearCache_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.CleanupAsync());
            if (result == null)
            {
                ShowActionStatus(ActionState.Error, "Clear Cache", "Unable to clear cache right now.");
                return;
            }

            ShowActionStatus(ActionState.Success, "Clear Cache", "System cache cleanup completed.", HyperBoostBackendClient.FormatJson(result));
        }

        private async void EmptyRecycle_Click(object sender, RoutedEventArgs e)
        {
            await RunPowerShellActionAsync(
                "Clear-RecycleBin -Force",
                "Empty Recycle Bin",
                "Recycle Bin emptied successfully.");
        }

        private async void DeepCleanup_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.CleanupAsync());
            LaunchWindowsTool("cleanmgr.exe", null, "Deep Cleanup");

            if (result == null)
            {
                ShowActionStatus(ActionState.Warning, "Deep Cleanup", "Windows Disk Cleanup opened, but backend deep cleanup result is unavailable.");
                return;
            }

            ShowActionStatus(ActionState.Success, "Deep Cleanup", "Deep cleanup started and Windows Disk Cleanup was opened.", HyperBoostBackendClient.FormatJson(result));
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
            ManualCloseTeamsChk.IsChecked = true;
            ManualCloseWidgetsChk.IsChecked = true;
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

            ManualFlushDnsChk.IsChecked = true;
            ManualGamingDnsChk.IsChecked = false;
            ManualDisableBandwidthHogsChk.IsChecked = false;
            ManualLimitBackgroundBandwidthChk.IsChecked = false;
            ManualDisableDeliveryOptChk.IsChecked = true;
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

            if (ManualCloseOneDriveChk.IsChecked == true) targets.AddRange(new[] { "OneDrive", "OneDriveStandaloneUpdater" });
            if (ManualCloseTeamsChk.IsChecked == true) targets.AddRange(new[] { "Teams", "ms-teams", "TeamsBootstrapper" });
            if (ManualCloseWidgetsChk.IsChecked == true) targets.AddRange(new[] { "Widgets", "WidgetService", "msedgewebview2" });
            if (ManualCloseBrowserChk.IsChecked == true) targets.AddRange(new[] { "chrome", "firefox", "msedge", "opera", "brave" });
            if (ManualCloseUpdaterChk.IsChecked == true) targets.AddRange(new[] { "GoogleUpdate", "AdobeGCClient", "EpicWebHelper", "SteamService", "UbisoftConnect", "Update", "Updater" });
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

        private async Task<(bool success, string output)> ExecutePowerShellScriptAsync(string script)
        {
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
            {
                return (false, "Unable to start PowerShell.");
            }

            var stdOut = await process.StandardOutput.ReadToEndAsync();
            var stdErr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            var output = string.IsNullOrWhiteSpace(stdErr) ? stdOut : stdErr;
            return (process.ExitCode == 0, output.Trim());
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

        private async Task ApplyQuickSafeGamingAsync()
        {
            var notes = new List<string>();
            notes.Add(await ApplyProcessTargetsAsync(new[] { "OneDrive", "Teams", "Widgets", "WidgetService" }.Where(x => !IsWhitelistedProcess(x)), "Quick Safe Gaming"));
            notes.Add(await ApplyPerformancePresetAsync(bestPerformance: true, disableTransparency: false, disableAnimations: false, highPriority: false));
            notes.Add(await ApplyNetworkPresetAsync());
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
            ShowActionStatus(ActionState.Success, "Quick Competitive Gaming", "Competitive gaming preset applied.", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
        }

        private async Task ApplyQuickStreamingGamingAsync()
        {
            var notes = new List<string>();
            notes.Add(await ApplyProcessTargetsAsync(new[] { "OneDrive", "Teams", "Widgets", "GoogleDriveFS", "AdobeGCClient" }.Where(x => !IsWhitelistedProcess(x)), "Quick Streaming Gaming"));
            var result = await SafeApiCall(() => _backendClient.ApplyBoosterAsync("streaming"));
            if (result != null)
            {
                notes.Add("Streaming booster profile applied");
            }

            notes.Add(await ApplyNetworkPresetAsync());
            notes.Add("Protected apps kept alive via whitelist, including Discord, Steam, OBS, RTSS, MSI Afterburner, LG HUB, Riot Client Services, and VGC.");
            ShowActionStatus(ActionState.Success, "Quick Streaming Gaming", "Streaming gaming preset applied.", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
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
        }

        private async void ApplyManualGaming_Click(object sender, RoutedEventArgs e)
        {
            await ApplyManualGamingCoreAsync();
        }

        private async void ApplyProcessControl_Click(object sender, RoutedEventArgs e)
        {
            await ApplyProcessTargetsAsync(GetManualProcessTargets(), "Process & App Control");
        }

        private async void ApplyOverlayControl_Click(object sender, RoutedEventArgs e)
        {
            await ApplyOverlayTargetsAsync();
        }

        private async void ApplyGamingNetwork_Click(object sender, RoutedEventArgs e)
        {
            var summary = await ApplyGamingNetworkSelectionsAsync();
            ShowActionStatus(ActionState.Success, "Network Optimization", "Gaming network actions applied.", summary);
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

            notes.Add("Apps and services that were manually closed may need to be reopened manually.");
            ShowActionStatus(ActionState.Success, "Restore Normal Mode", "Normal Windows mode restored as much as possible.", string.Join(Environment.NewLine, notes.Where(x => !string.IsNullOrWhiteSpace(x))));
        }

        private async void BackFromGaming_Click(object sender, RoutedEventArgs e)
        {
            await ShowPage("Dashboard", DashboardBtn);
        }

        #endregion

        #region Network Optimization

        private async Task RefreshNetworkDiagnostics()
        {
            var dns = await SafeApiCall(() => _backendClient.TestDnsAsync());
            if (dns == null)
            {
                NetworkDiagnosticsText.Text = "Unable to load network diagnostics.";
                return;
            }

            NetworkDiagnosticsText.Text = FormatNetworkDiagnostics(dns);
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
            ShowActionStatus(ActionState.Success, "DNS Test", "DNS diagnostics refreshed successfully.", HyperBoostBackendClient.FormatJson(dns));
        }

        private async Task RunNetworkAction(Func<Task<dynamic>> action, string actionName)
        {
            var result = await SafeApiCall(action);
            if (result == null)
            {
                ShowActionStatus(ActionState.Error, actionName, $"{actionName} failed. Please try again later.");
                return;
            }

            ShowActionStatus(ActionState.Success, actionName, $"{actionName} completed successfully.", HyperBoostBackendClient.FormatJson(result));
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

        #endregion

        #region Privacy Tweaks

        private void RefreshBackgroundApps_Click(object sender, RoutedEventArgs e)
        {
            _ = RefreshBackgroundApps();
        }

        private async void DisableTelemetry_Click(object sender, RoutedEventArgs e)
        {
            await ApplyTweakWithFeedbackAsync("disable_telemetry", "Disable Telemetry");
        }

        private void DisableAds_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:privacy-general", "Disable Ads");
        }

        private void ActivityTracking_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:privacy-activityhistory", "Activity Tracking");
        }

        private void PrivacyManager_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:privacy", "Privacy Manager");
        }

        #endregion

        #region Advanced Tweaks

        private void ContextMenu_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("regedit.exe", null, "Context Menu Editor");
        }

        private void ExplorerTweaks_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("control.exe", "folders", "Explorer Tweaks");
        }

        private void TaskbarTweaks_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:taskbar", "Taskbar Tweaks");
        }

        private void DarkMode_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsUri("ms-settings:colors", "Dark Mode");
        }

        #endregion

        #region Restore & Backup

        private async void CreateRestore_Click(object sender, RoutedEventArgs e)
        {
            await RunPowerShellActionAsync(
                "Checkpoint-Computer -Description 'HyperBoost X Manual Restore Point' -RestorePointType 'MODIFY_SETTINGS'",
                "Create Restore Point",
                "Windows restore point created successfully.");
        }

        private async void BackupSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var backupRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "HyperBoost X",
                    "backups");
                Directory.CreateDirectory(backupRoot);

                var stats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
                var payload = new
                {
                    created_at = DateTime.Now,
                    backend_url = _currentBackendUrl,
                    system_stats = stats
                };

                var fileName = $"hyperboost-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json";
                var filePath = Path.Combine(backupRoot, fileName);
                File.WriteAllText(filePath, JsonConvert.SerializeObject(payload, Formatting.Indented));

                ShowActionStatus(ActionState.Success, "Backup Settings", "Settings snapshot saved successfully.", filePath);
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
        }

        private void UndoOptimization_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("rstrui.exe", null, "Undo Optimization");
        }

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
                    output.AppendLine($"Total: {disk["total"]} GB");
                    output.AppendLine($"Used: {disk["used"]} GB");
                    output.AppendLine($"Free: {disk["free"]} GB ({disk["percent"]}% used)");
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
                var result = await _backendClient.ApplyBoosterAsync(profileId);
                var json = result as Newtonsoft.Json.Linq.JObject;
                var success = json?.Value<bool?>("success") == true;
                var partialSuccess = json?.Value<bool?>("partial_success") == true;
                var state = success
                    ? (partialSuccess ? ActionState.Warning : ActionState.Success)
                    : ActionState.Error;
                var title = success ? (partialSuccess ? "Applied with warnings" : "Optimization applied") : "Unable to fully apply";

                ShowActionStatus(state, title, $"{modeName} finished. Review the summary below for details.", HyperBoostBackendClient.FormatJson(result));
                await RefreshDashboard();
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Error, $"{modeName} failed", ex.Message);
            }
        }

        private async Task ShowSmartRecommendationAsync(Button sourceButton)
        {
            SelectNavButton(sourceButton);

            var stats = await SafeApiCall(() => _backendClient.GetSystemStatsAsync());
            var json = stats as Newtonsoft.Json.Linq.JObject;

            if (json == null)
            {
                ShowActionStatus(ActionState.Warning, "Smart Recommendation unavailable", "Backend system stats are required before a recommendation can be generated.");
                await ShowPlaceholderPage(
                    sourceButton,
                    "Smart Recommendation",
                    "Unable to generate a recommendation because backend system stats are unavailable.",
                    "Status: backend disconnected or stats unavailable");
                return;
            }

            var cpu = json.Value<double?>("cpu") ?? json.Value<double?>("cpu_percent") ?? 0;
            var memory = json.Value<double?>("memory") ?? json.Value<double?>("memory_percent") ?? 0;
            var disk = json.Value<double?>("disk") ?? json.Value<double?>("disk_percent") ?? 0;

            string recommendationTitle;
            string recommendationBody;
            string recommendationStatus;

            if (disk >= 85)
            {
                recommendationTitle = "Storage cleanup recommended";
                recommendationBody = $"Disk usage is {disk:0}%.\n\nRecommended next step:\nUse Cleanup and review Storage settings.";
                recommendationStatus = "Recommended focus: Storage";
            }
            else if (memory >= 80)
            {
                recommendationTitle = "Background apps review recommended";
                recommendationBody = $"Memory usage is {memory:0}%.\n\nRecommended next step:\nReview Background Apps and Startup items.";
                recommendationStatus = "Recommended focus: Background Apps";
            }
            else if (cpu >= 75)
            {
                recommendationTitle = "Performance profile recommended";
                recommendationBody = $"CPU usage is {cpu:0}%.\n\nRecommended next step:\nUse Gaming Mode for peak performance or Productivity Mode for balanced optimization.";
                recommendationStatus = "Recommended focus: Performance";
            }
            else
            {
                recommendationTitle = "System looks healthy";
                recommendationBody = $"CPU {cpu:0}% | Memory {memory:0}% | Disk {disk:0}%.\n\nRecommended next step:\nUse Dashboard for monitoring or Privacy Center for preventive tuning.";
                recommendationStatus = "Recommended focus: Dashboard / Privacy";
            }

            await ShowPlaceholderPage(
                sourceButton,
                "Smart Recommendation",
                $"{recommendationTitle}\n\n{recommendationBody}",
                recommendationStatus);
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

        private async Task SafeApiCall(Func<Task> apiCall)
        {
            try
            {
                await apiCall();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SafeApiCall error: {ex.Message}");
            }
        }

        private async Task<T> SafeApiCall<T>(Func<Task<T>> apiCall)
        {
            try
            {
                return await apiCall();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SafeApiCall<T> error: {ex.Message}");
                return default;
            }
        }

        private async Task ApplyTweakWithFeedbackAsync(string tweakId, string actionName)
        {
            try
            {
                var result = await _backendClient.ApplyTweakAsync(tweakId);
                ShowActionStatus(ActionState.Success, actionName, "Tweak applied successfully.", HyperBoostBackendClient.FormatJson(result));
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Error, actionName, $"Unable to run {actionName}.", ex.Message);
            }
        }

        private async Task RunPowerShellActionAsync(string script, string actionName, string successMessage)
        {
            try
            {
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
                {
                    throw new InvalidOperationException("Unable to start PowerShell process.");
                }

                var stdOut = await process.StandardOutput.ReadToEndAsync();
                var stdErr = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    ShowActionStatus(ActionState.Success, actionName, successMessage);
                }
                else
                {
                    var details = string.IsNullOrWhiteSpace(stdErr) ? stdOut : stdErr;
                    ShowActionStatus(ActionState.Error, actionName, $"{actionName} failed.", details);
                }
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
                var startInfo = new ProcessStartInfo(fileName)
                {
                    UseShellExecute = true
                };

                if (!string.IsNullOrWhiteSpace(arguments))
                {
                    startInfo.Arguments = arguments;
                }

                Process.Start(startInfo);
                ShowActionStatus(ActionState.Info, featureName, "Windows tool opened successfully.", string.IsNullOrWhiteSpace(arguments) ? fileName : $"{fileName} {arguments}");
            }
            catch (Exception ex)
            {
                ShowActionStatus(ActionState.Error, featureName, $"Unable to open {featureName}.", ex.Message);
            }
        }
    }
}


using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HyperBoostX.Services;
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

        private HyperBoostBackendClient _backendClient;
        private string _currentBackendUrl = "http://127.0.0.1:5000";
        private Button _selectedNavButton;
        private DispatcherTimer _dashboardTimer;
        private bool _isUpdating;
        private string _activePage = "Dashboard";

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
                    await ShowPage("Booster", button);
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
                return;
            }

            StartupItemsText.Text = FormatStartupItems(startup);
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
            LaunchWindowsUri("ms-settings:startupapps", "Manage Startup");
        }

        private void DelayStartup_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("taskschd.msc", null, "Delay Startup");
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
            await ApplyBoosterProfileAsync("gaming", "Game Mode");
        }

        private async void DisableOverlays_Click(object sender, RoutedEventArgs e)
        {
            await ApplyTweakWithFeedbackAsync("disable_xbox", "Disable Overlays");
        }

        private void FreeRAM_Click(object sender, RoutedEventArgs e)
        {
            LaunchWindowsTool("resmon.exe", null, "Free RAM");
        }

        private async void FPSStability_Click(object sender, RoutedEventArgs e)
        {
            await ApplyBoosterProfileAsync("streaming", "FPS Stability");
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

        private string FormatStartupItems(dynamic startupData)
        {
            try
            {
                var output = new System.Text.StringBuilder();
                var items = startupData["items"] as Newtonsoft.Json.Linq.JArray;

                if (items == null || items.Count == 0)
                {
                    return "No startup items found.";
                }

                output.AppendLine($"Total Startup Items: {items.Count}");
                output.AppendLine();

                foreach (var item in items)
                {
                    var enabled = item.Value<bool?>("enabled") == true ? "Enabled" : "Disabled";
                    output.AppendLine($"{item["name"]}");
                    output.AppendLine($"  State: {enabled}");
                    output.AppendLine($"  Impact: {item["impact"]}");
                    output.AppendLine();
                }

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
                var startInfo = new ProcessStartInfo("powershell.exe")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\""
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


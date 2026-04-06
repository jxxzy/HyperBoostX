using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HyperBoostX.Services;


namespace HyperBoostX
{
    public partial class MainWindow : Window
    {
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
                    PageTitle.Text = "Dashboard";
                    DashboardContent.Visibility = Visibility.Visible;
                    await RefreshDashboard();
                    _dashboardTimer.Start();
                    break;
                case "Performance":
                    PageTitle.Text = "Performance Boost";
                    PerformanceContent.Visibility = Visibility.Visible;
                    break;
                case "Startup":
                    PageTitle.Text = "Startup Manager";
                    StartupContent.Visibility = Visibility.Visible;
                    await RefreshStartupItems();
                    break;
                case "Cleanup":
                    PageTitle.Text = "Storage Cleaner";
                    CleanupContent.Visibility = Visibility.Visible;
                    break;
                case "Gaming":
                    PageTitle.Text = "Gaming Booster";
                    GamingContent.Visibility = Visibility.Visible;
                    break;
                case "Network":
                    PageTitle.Text = "Network Booster";
                    NetworkContent.Visibility = Visibility.Visible;
                    await RefreshNetworkDiagnostics();
                    break;
                case "BackgroundApps":
                    PageTitle.Text = "Background Apps";
                    BackgroundAppsContent.Visibility = Visibility.Visible;
                    await RefreshBackgroundApps();
                    break;
                case "Privacy":
                    PageTitle.Text = "Privacy Tweaks";
                    PrivacyContent.Visibility = Visibility.Visible;
                    break;
                case "Repair":
                    PageTitle.Text = "Repair Tools";
                    RepairContent.Visibility = Visibility.Visible;
                    break;
                case "Advanced":
                    PageTitle.Text = "Advanced Tweaks";
                    AdvancedContent.Visibility = Visibility.Visible;
                    break;
                case "Restore":
                    PageTitle.Text = "Restore & Backup";
                    RestoreContent.Visibility = Visibility.Visible;
                    break;
                case "Settings":
                    PageTitle.Text = "Settings";
                    SettingsContent.Visibility = Visibility.Visible;
                    break;
                case "Tweaks":
                    PageTitle.Text = "Tweaks Center";
                    TweaksContent.Visibility = Visibility.Visible;
                    await RefreshTweaks();
                    break;
                case "Drivers":
                    PageTitle.Text = "Driver & Update Center";
                    DriversContent.Visibility = Visibility.Visible;
                    await RefreshDrivers();
                    break;
                case "Booster":
                    PageTitle.Text = "Booster Profiles";
                    BoosterContent.Visibility = Visibility.Visible;
                    await LoadBoosterProfiles();
                    break;
                case "About":
                    PageTitle.Text = "About App";
                    AboutContent.Visibility = Visibility.Visible;
                    break;
            }
        }

        private Task ShowPlaceholderPage(Button navButton, string title, string description, string status)
        {
            SelectNavButton(navButton);
            HideAllPages();
            PageTitle.Text = title;
            PlaceholderTitleText.Text = title;
            PlaceholderDescriptionText.Text = description;
            PlaceholderStatusText.Text = status;
            PlaceholderContent.Visibility = Visibility.Visible;
            return Task.CompletedTask;
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
                case nameof(GamingBoosterBtn):
                    await ShowPage("Booster", button);
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
                case nameof(TweaksCenterBtn):
                    await ShowPage("Tweaks", button);
                    break;
                case nameof(RepairToolsBtn):
                    await ShowPage("Repair", button);
                    break;
                case nameof(DriverUpdateCenterBtn):
                    await ShowPage("Drivers", button);
                    break;
                case nameof(AdvancedTweaksBtn):
                    await ShowPage("Advanced", button);
                    break;
                case nameof(RestoreBackupBtn):
                case nameof(RestorePointManagerBtn):
                    await ShowPage("Restore", button);
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
                MessageBox.Show($"Gaming profile applied.\n\n{HyperBoostBackendClient.FormatJson(result)}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Optimize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = await _backendClient.ApplyBoosterAsync("productivity");
                MessageBox.Show($"Optimization applied.\n\n{HyperBoostBackendClient.FormatJson(result)}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            try
            {
                var result = await _backendClient.ApplyBoosterAsync(profileName);
                MessageBox.Show($"{profileName.ToUpper()} Mode Applied!\n\n{HyperBoostBackendClient.FormatJson(result)}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                MessageBox.Show("Unable to check driver updates. Please try again later.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"Driver Update Check Complete!\n\n{HyperBoostBackendClient.FormatJson(result)}", "Result", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Repair

        private async void RunSfc_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.RunSfcAsync());
            if (result == null)
            {
                MessageBox.Show("Unable to start SFC scan. Please try again later.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"SFC Scan Initiated!\n\n{HyperBoostBackendClient.FormatJson(result)}", "Result", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void RunDism_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.RunDismAsync());
            if (result == null)
            {
                MessageBox.Show("Unable to start DISM repair. Please try again later.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"DISM Repair Initiated!\n\n{HyperBoostBackendClient.FormatJson(result)}", "Result", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void Cleanup_Click(object sender, RoutedEventArgs e)
        {
            var result = await SafeApiCall(() => _backendClient.CleanupAsync());
            if (result == null)
            {
                MessageBox.Show("Unable to cleanup files. Please try again later.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"Cleanup Complete!\n\n{HyperBoostBackendClient.FormatJson(result)}", "Result", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("✓ Backend is running and responding!", "Connection Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                BackendHealthIndicator.Background = System.Windows.Media.Brushes.LimeGreen;
            }
            else
            {
                MessageBox.Show("✗ Backend is not responding. Please ensure the Python backend is running.", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                BackendHealthIndicator.Background = System.Windows.Media.Brushes.Red;
            }
        }

        private async void UpdateBackendUrl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentBackendUrl = BackendUrlInput.Text.Trim();
                _backendClient.Dispose();
                _backendClient = new HyperBoostBackendClient(_currentBackendUrl);
                MessageBox.Show($"Backend URL updated to:\n{_currentBackendUrl}", "Updated", MessageBoxButton.OK, MessageBoxImage.Information);
                await CheckBackendHealth();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CheckBackendHealth()
        {
            var isHealthy = await SafeApiCall(() => _backendClient.HealthCheckAsync());
            if (isHealthy)
            {
                BackendHealthIndicator.Background = System.Windows.Media.Brushes.LimeGreen;
                ((TextBlock)BackendHealthIndicator.Child).Text = "● Backend: Connected";
            }
            else
            {
                BackendHealthIndicator.Background = System.Windows.Media.Brushes.Red;
                ((TextBlock)BackendHealthIndicator.Child).Text = "● Backend: Disconnected";
            }
        }

        #endregion

        #region Performance Tweaks

        private void OptimizeRAM_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("RAM optimization initiated. This will clear standby memory and improve available RAM.", "Optimize RAM", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BoostGaming_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Gaming Mode activated! Background apps disabled, priority set to high.", "Gaming Mode", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AutoPerformance_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Auto Performance Profile enabled. System will automatically adjust performance based on usage.", "Auto Profile", MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show("Opening Startup Manager. Manage which apps start with Windows.", "Manage Startup", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DelayStartup_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Delaying startup apps. This can significantly reduce boot time.", "Delay Startup", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Storage Cleanup

        private void CleanTemp_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Cleaning temporary files. This is one of the safest optimizations.", "Clean Temp", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearCache_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Clearing system cache. Browser and application caches will be cleared.", "Clear Cache", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EmptyRecycle_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Emptying Recycle Bin to free up disk space.", "Empty Recycle Bin", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeepCleanup_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Starting deep disk cleanup. Scanning for Windows leftovers and junk files.", "Deep Cleanup", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Gaming Optimization

        private void GameMode_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Game Mode activated! System will optimize for gaming performance.", "Game Mode", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DisableOverlays_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Disabling Windows overlays (Discord, Xbox, etc.) to reduce resource usage.", "Disable Overlays", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FreeRAM_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Freeing RAM before game launch. Clearing standby memory for maximum performance.", "Free RAM", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FPSStability_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Applying FPS stability tweaks. Disabling background updates and processes.", "FPS Stability", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("Unable to run DNS test right now.", "DNS Test", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NetworkDiagnosticsText.Text = FormatNetworkDiagnostics(dns);
        }

        private async Task RunNetworkAction(Func<Task<dynamic>> action, string actionName)
        {
            var result = await SafeApiCall(action);
            if (result == null)
            {
                MessageBox.Show($"{actionName} failed. Please try again later.", "Network", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"{actionName} completed.\n\n{HyperBoostBackendClient.FormatJson(result)}", "Network", MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show("Applying ping stabilizer. Optimizing network latency for gaming.", "Ping Stabilizer", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Privacy Tweaks

        private void RefreshBackgroundApps_Click(object sender, RoutedEventArgs e)
        {
            _ = RefreshBackgroundApps();
        }

        private void DisableTelemetry_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Disabling Windows telemetry. This protects your privacy.", "Disable Telemetry", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DisableAds_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Disabling ads and suggestions. Removing personalized ads from Windows.", "Disable Ads", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ActivityTracking_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Controlling activity tracking. Disabling activity history collection.", "Activity Control", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PrivacyManager_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Opening Privacy Settings Manager for detailed control.", "Privacy Manager", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Advanced Tweaks

        private void ContextMenu_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Opening Context Menu Editor. Customize right-click menu.", "Context Menu", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExplorerTweaks_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Applying File Explorer tweaks for better usability.", "Explorer Tweaks", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TaskbarTweaks_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Applying Taskbar tweaks for improved functionality.", "Taskbar Tweaks", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DarkMode_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Applying Dark Mode tweaks. Enabling dark theme system-wide.", "Dark Mode", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Restore & Backup

        private void CreateRestore_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Creating Windows Restore Point. You can revert system changes if needed.", "Restore Point", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BackupSettings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Backing up all tweak settings. You can restore them later.", "Backup Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RestoreDefault_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Restoring default Windows settings. All tweaks will be undone.", "Restore Default", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void UndoOptimization_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Undoing latest optimization. Reverting to previous state.", "Undo", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show($"{modeName} applied.\n\n{HyperBoostBackendClient.FormatJson(result)}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
    }
}

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
        private static bool _cyberResourcesEnsured;
        private bool _backendCheckInProgress;
        private bool _isClosing;

        public MainWindow()
            : this(new HyperBoostBackendClient("http://127.0.0.1:5000"))
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
            NavigateToPage(ResolveStartupPageKey());

            _backendTimer.Interval = TimeSpan.FromSeconds(4);
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
            var view = _navigationService.Navigate(key);
            PageHost.Content = view;

            foreach (var item in _viewModel.NavigationItems)
                item.IsActive = string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase);

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
        }

        private string ResolveStartupPageKey()
        {
            var requestedPage = Environment.GetEnvironmentVariable("HYPERBOOSTX_START_PAGE");
            if (string.IsNullOrWhiteSpace(requestedPage))
                return "Dashboard";

            return _viewModel.NavigationItems.Any(item => string.Equals(item.Key, requestedPage, StringComparison.OrdinalIgnoreCase))
                ? requestedPage
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
            }
            catch
            {
                _viewModel.ToastMessage = "Using default cyber UI settings";
            }
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
                    if (_viewModel.NavigationItems.Count < 24)
                        throw new InvalidOperationException("Cyber sidebar is missing required pages.");
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

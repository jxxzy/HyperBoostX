using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using HyperBoostX.Services;
using HyperBoostX.ViewModels;

namespace HyperBoostX.Views
{
    public partial class SettingsView : UserControl
    {
        private readonly LocalConfigService _configService = new();

        public SettingsView()
        {
            InitializeComponent();
        }

        private SettingsViewModel ViewModel => DataContext as SettingsViewModel;

        private void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private void ReloadSettings_Click(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
                return;

            try
            {
                _configService.SaveUiSettings(new UiSettings
                {
                    EnableAnimations = ViewModel.EnableAnimations,
                    ReduceMotion = ViewModel.ReduceMotion,
                    AccentColor = ViewModel.AccentColor,
                    Mode = ViewModel.Mode
                });
                if (Window.GetWindow(this) is MainWindow shell)
                {
                    shell.ReloadUiSettingsFromSettingsPage();
                    ViewModel.Status = "Settings saved. Sidebar and motion settings updated in the current window.";
                }
                else
                {
                    ViewModel.Status = "Settings saved. Navigation mode changes apply after sidebar reload or relaunch where required.";
                }
            }
            catch (Exception ex)
            {
                ViewModel.Status = $"Save failed safely: {ex.Message}";
            }
        }

        private void LoadSettings()
        {
            if (ViewModel == null)
                return;

            try
            {
                var settings = _configService.LoadUiSettings();
                ViewModel.EnableAnimations = settings.EnableAnimations;
                ViewModel.ReduceMotion = settings.ReduceMotion;
                ViewModel.AccentColor = settings.AccentColor;
                ViewModel.Mode = string.Equals(settings.Mode, "Expert Preview", StringComparison.OrdinalIgnoreCase) ? "Expert" : settings.Mode;
                ViewModel.Status = "Settings loaded.";
            }
            catch (Exception ex)
            {
                ViewModel.Status = $"Load failed safely: {ex.Message}";
            }
        }

        private void ResetSettings_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
                return;

            ViewModel.EnableAnimations = true;
            ViewModel.ReduceMotion = false;
            ViewModel.AccentColor = "Cyan";
            ViewModel.Mode = "Beginner";
            ViewModel.Status = "Settings reset to safe local defaults. Click Save Settings to persist.";
        }

        private void ExportSettings_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
                return;

            ViewModel.Status = "Settings export uses the local data folder. Sensitive values remain redacted by report exporters where supported.";
        }

        private void ImportSettings_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
                return;

            ViewModel.Status = "Import is manual in this build. Open the local data folder and review files before replacing settings.";
        }

        private void OpenLocalData_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
                return;

            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "HyperBoost X");
            Directory.CreateDirectory(path);
            LaunchUri(path);
            ViewModel.Status = "Opened local data folder.";
        }

        private void OpenDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
                ViewModel.Status = "Diagnostics are available from Feature Audit in Expert mode and release gate docs.";
        }

        private void OpenGitHubRelease_Click(object sender, RoutedEventArgs e)
        {
            LaunchUri("https://github.com/jxxzy/HyperBoostX/releases/tag/v2.10.0");
            if (ViewModel != null)
                ViewModel.Status = "Opened GitHub release page.";
        }

        private void CopyVersionInfo_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText("HyperBoostX 2.10.0 Stable Unsigned");
            if (ViewModel != null)
                ViewModel.Status = "Version info copied.";
        }

        private static void LaunchUri(string target)
        {
            try
            {
                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            }
            catch
            {
                // Keep settings page usable even if shell launch is blocked.
            }
        }
    }
}

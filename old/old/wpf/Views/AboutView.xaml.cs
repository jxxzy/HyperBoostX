using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using HyperBoostX.ViewModels;

namespace HyperBoostX.Views
{
    public partial class AboutView : UserControl
    {
        public AboutView() => InitializeComponent();

        private AboutViewModel ViewModel => DataContext as AboutViewModel;

        private void OpenRelease_Click(object sender, RoutedEventArgs e) =>
            LaunchUri("https://github.com/jxxzy/HyperBoostX/releases/tag/v2.10.0");

        private void OpenRepository_Click(object sender, RoutedEventArgs e) =>
            LaunchUri("https://github.com/jxxzy/HyperBoostX");

        private void OpenDocumentation_Click(object sender, RoutedEventArgs e) =>
            LaunchUri("https://github.com/jxxzy/HyperBoostX/tree/main/docs");

        private void ReportIssue_Click(object sender, RoutedEventArgs e) =>
            LaunchUri("https://github.com/jxxzy/HyperBoostX/issues");

        private void CopyVersionInfo_Click(object sender, RoutedEventArgs e)
        {
            var version = ViewModel?.Version ?? "2.10.0";
            Clipboard.SetText($"HyperBoostX {version} Owner Lab / Release Candidate");
        }

        private void ExportAppInfo_Click(object sender, RoutedEventArgs e)
        {
            var version = ViewModel?.Version ?? "2.10.0";
            Clipboard.SetText($"HyperBoostX {version}\nChannel: Owner Lab / Release Candidate\nBackend: Local 127.0.0.1\nInstaller: Unsigned\nPublic Stable: gated until release evidence passes");
        }

        private static void LaunchUri(string target)
        {
            try
            {
                Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            }
            catch
            {
                // Link buttons remain non-destructive if the shell blocks opening URLs.
            }
        }
    }
}

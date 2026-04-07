using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using HyperBoostX.Services;


namespace HyperBoostX
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HyperBoost X",
            "logs");
        private static readonly string LogFile = Path.Combine(LogDirectory, "hyperboost-wpf.log");
        private readonly DiscordWebhookService _discordWebhookService = new DiscordWebhookService();

        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log($"DispatcherUnhandledException: {e.Exception}");
            TryReportCriticalError("DispatcherUnhandledException", e.Exception.ToString());
            MessageBox.Show(
                "Terjadi error tak terduga. Detail sudah disimpan ke hyperboost-wpf.log.",
                "HyperBoost X",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
            Current.Shutdown(-1);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log($"UnhandledException: {e.ExceptionObject}");
            TryReportCriticalError("UnhandledException", e.ExceptionObject?.ToString() ?? "Unknown fatal exception");
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Log($"UnobservedTaskException: {e.Exception}");
            TryReportCriticalError("UnobservedTaskException", e.Exception.ToString());
            e.SetObserved();
        }

        private void TryReportCriticalError(string source, string details)
        {
            try
            {
                var configService = new AppConfigService();
                var config = configService.LoadAsync().GetAwaiter().GetResult();
                if (config?.Settings?.DiscordWebhookEnabled != true || string.IsNullOrWhiteSpace(config.Settings.DiscordWebhookUrl))
                    return;

                var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
                _discordWebhookService.SendAsync(
                    config.Settings.DiscordWebhookUrl,
                    "HyperBoostX critical error",
                    details,
                    "critical",
                    new Dictionary<string, string>
                    {
                        ["Source"] = source,
                        ["App Version"] = version,
                        ["Timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    }).GetAwaiter().GetResult();
            }
            catch
            {
                // Never let error reporting crash the app.
            }
        }

        private static void Log(string message)
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(LogFile, line);
            }
            catch
            {
                // Avoid recursive failures while logging.
            }
        }
    }
}

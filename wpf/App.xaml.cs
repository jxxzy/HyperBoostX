using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;


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

        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log($"DispatcherUnhandledException: {e.Exception}");
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
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Log($"UnobservedTaskException: {e.Exception}");
            e.SetObserved();
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

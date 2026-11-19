using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
        private static readonly Regex StructuredLogSeverityRegex = new Regex(@"\s-\s(?<level>DEBUG|INFO|WARNING|ERROR|CRITICAL)\s-\s", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HyperBoost X",
            "logs");
        private static readonly string LogFile = Path.Combine(LogDirectory, "hyperboost-wpf.log");
        private readonly DiscordWebhookService _discordWebhookService = new DiscordWebhookService();
        private readonly SecureSecretStoreService _secureSecretStoreService = new SecureSecretStoreService();
        private readonly DispatcherTimer _logWatcherTimer = new DispatcherTimer();
        private readonly Dictionary<string, long> _logOffsets = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DateTime> _discordReportCooldown = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private bool _logScanInProgress;

        private static string GetPublicAppVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var informational = assembly
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                .OfType<AssemblyInformationalVersionAttribute>()
                .FirstOrDefault()?.InformationalVersion;

            var version = string.IsNullOrWhiteSpace(informational)
                ? assembly.GetName().Version?.ToString()
                : informational.Split('+')[0].Trim();

            if (string.IsNullOrWhiteSpace(version))
                return "unknown";

            return version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : $"v{version}";
        }

        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            _logWatcherTimer.Interval = TimeSpan.FromSeconds(45);
            _logWatcherTimer.Tick += LogWatcherTimer_Tick;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            InitializeLogOffsets();
            _logWatcherTimer.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _logWatcherTimer.Stop();
            base.OnExit(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log($"DispatcherUnhandledException: {e.Exception}");
            TryReportCriticalError("DispatcherUnhandledException", e.Exception.ToString());
            MessageBox.Show(
                "Terjadi error tak terduga. Detail sudah disimpan ke hyperboost-wpf.log.",
                "HyperBoostX",
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

        private async void LogWatcherTimer_Tick(object sender, EventArgs e)
        {
            if (_logScanInProgress)
                return;

            _logScanInProgress = true;
            try
            {
                await ScanLogsAndReportAsync();
            }
            catch
            {
                // Never let background reporting crash the app.
            }
            finally
            {
                _logScanInProgress = false;
            }
        }

        private void TryReportCriticalError(string source, string details)
        {
            try
            {
                var settings = LoadDiscordReportingSettingsAsync().GetAwaiter().GetResult();
                if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.WebhookUrl))
                    return;

                if (!ShouldSendForSeverity("critical", settings.MinimumLevel))
                    return;

                var version = GetPublicAppVersion();
                var signature = $"critical|{source}|{details}";
                if (IsWithinDiscordCooldown(signature, settings.CooldownSeconds))
                    return;

                var result = _discordWebhookService.SendDetailedAsync(
                    settings.WebhookUrl,
                    "HyperBoostX critical error",
                    details,
                    "critical",
                    new Dictionary<string, string>
                    {
                        ["Source"] = source,
                        ["App Version"] = version,
                        ["Timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    }).GetAwaiter().GetResult();

                if (result.Success)
                    MarkDiscordSent(signature);
                else
                    Log($"Discord webhook delivery not completed: {result.Summary}");
            }
            catch
            {
                // Never let error reporting crash the app.
            }
        }

        private void InitializeLogOffsets()
        {
            foreach (var file in GetMonitoredLogFiles())
            {
                try
                {
                    _logOffsets[file] = File.Exists(file) ? new FileInfo(file).Length : 0L;
                }
                catch
                {
                    _logOffsets[file] = 0L;
                }
            }
        }

        private IEnumerable<string> GetMonitoredLogFiles()
        {
            yield return Path.Combine(LogDirectory, "hyperboost-wpf.log");
            yield return Path.Combine(LogDirectory, "hyperboost-launcher.log");
            yield return Path.Combine(LogDirectory, "hyperboost.log");
        }

        private async Task ScanLogsAndReportAsync()
        {
            var settings = await LoadDiscordReportingSettingsAsync();
            if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.WebhookUrl))
                return;

            foreach (var logPath in GetMonitoredLogFiles())
            {
                if (!File.Exists(logPath))
                    continue;

                var newEntries = ReadNewLogLines(logPath);
                if (newEntries.Count == 0)
                    continue;

                var recentContext = new Queue<string>();
                foreach (var entry in newEntries)
                {
                    if (recentContext.Count >= 6)
                        recentContext.Dequeue();
                    recentContext.Enqueue(entry);

                    var severity = DetectLogSeverity(Path.GetFileName(logPath), entry);
                    if (severity == null || !ShouldSendForSeverity(severity, settings.MinimumLevel))
                        continue;

                    var signature = $"{Path.GetFileName(logPath)}|{severity}|{entry.Trim()}";
                    if (IsWithinDiscordCooldown(signature, settings.CooldownSeconds))
                        continue;

                    var contextBlock = string.Join(Environment.NewLine, recentContext.Where(x => !string.IsNullOrWhiteSpace(x)));
                    var result = await _discordWebhookService.SendDetailedAsync(
                        settings.WebhookUrl,
                        $"HyperBoostX auto log alert ({Path.GetFileName(logPath)})",
                        entry,
                        severity,
                        new Dictionary<string, string>
                        {
                            ["Source Log"] = Path.GetFileName(logPath),
                            ["Severity"] = severity,
                            ["Recent Context"] = contextBlock,
                            ["App Version"] = GetPublicAppVersion(),
                            ["Timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        });

                    if (result.Success)
                        MarkDiscordSent(signature);
                    else
                        Log($"Discord webhook delivery not completed: {result.Summary}");
                }
            }
        }

        private List<string> ReadNewLogLines(string logPath)
        {
            var lines = new List<string>();
            try
            {
                var previousOffset = _logOffsets.TryGetValue(logPath, out var existingOffset) ? existingOffset : 0L;
                using var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (previousOffset > stream.Length)
                    previousOffset = 0L;

                stream.Seek(previousOffset, SeekOrigin.Begin);
                using var reader = new StreamReader(stream);
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line))
                        lines.Add(line);
                }

                _logOffsets[logPath] = stream.Length;
            }
            catch
            {
                // Ignore transient log-read failures.
            }

            return lines;
        }

        private static string DetectLogSeverity(string sourceLogName, string line)
        {
            var text = line?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var structuredSeverity = StructuredLogSeverityRegex.Match(text);
            if (structuredSeverity.Success)
            {
                return structuredSeverity.Groups["level"].Value.Trim().ToLowerInvariant() switch
                {
                    "warning" => "warning",
                    "error" => "error",
                    "critical" => "critical",
                    _ => null
                };
            }

            var upper = text.ToUpperInvariant();
            if (upper.Contains("UNHANDLEDEXCEPTION") || upper.Contains("DISPATCHERUNHANDLEDEXCEPTION") || upper.Contains("TRACEBACK") || upper.Contains("CRITICAL"))
                return "critical";
            if (upper.Contains("ERROR") || upper.Contains("EXCEPTION") || upper.Contains("FAILED"))
                return "error";
            if (upper.Contains("WARNING") || upper.Contains("WARN"))
                return "warning";

            return null;
        }

        private async Task<(bool Enabled, string WebhookUrl, string MinimumLevel, int CooldownSeconds)> LoadDiscordReportingSettingsAsync()
        {
            try
            {
                var configService = new AppConfigService();
                var config = await configService.LoadAsync();
                var secrets = await _secureSecretStoreService.LoadAsync();
                var envWebhook = Environment.GetEnvironmentVariable("HYPERBOOSTX_DISCORD_WEBHOOK_URL")?.Trim() ?? "";

                var webhookUrl = !string.IsNullOrWhiteSpace(envWebhook)
                    ? envWebhook
                    : !string.IsNullOrWhiteSpace(secrets.DiscordWebhookUrl)
                        ? secrets.DiscordWebhookUrl
                        : config?.Settings?.DiscordWebhookUrl ?? "";

                return (
                    config?.Settings?.DiscordWebhookEnabled == true,
                    webhookUrl,
                    string.IsNullOrWhiteSpace(config?.Settings?.DiscordWebhookMinimumLevel) ? "Error" : config.Settings.DiscordWebhookMinimumLevel,
                    Math.Max(15, config?.Settings?.DiscordWebhookCooldownSeconds ?? 120));
            }
            catch
            {
                return (false, "", "Error", 120);
            }
        }

        private bool IsWithinDiscordCooldown(string signature, int cooldownSeconds)
        {
            return _discordReportCooldown.TryGetValue(signature, out var lastSentUtc) &&
                DateTime.UtcNow - lastSentUtc < TimeSpan.FromSeconds(Math.Max(15, cooldownSeconds));
        }

        private void MarkDiscordSent(string signature)
        {
            _discordReportCooldown[signature] = DateTime.UtcNow;
        }

        private static bool ShouldSendForSeverity(string severity, string minimumLevel)
        {
            return GetSeverityRank(severity) >= GetSeverityRank(minimumLevel);
        }

        private static int GetSeverityRank(string severity)
        {
            return severity?.Trim().ToLowerInvariant() switch
            {
                "warning" => 1,
                "error" => 2,
                "critical" => 3,
                _ => 2
            };
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

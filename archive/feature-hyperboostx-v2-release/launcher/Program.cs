using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HyperBoostLauncher
{
    internal class Program
    {
        private const string SingleInstanceMutexPrefix = @"Global\HyperBoostXLauncherSingleInstance_";
        private static readonly string AppRoot = Path.GetDirectoryName(AppContext.BaseDirectory) ?? "";
        private static readonly string InstallRoot = Directory.GetParent(AppRoot)?.FullName ?? AppRoot;
        private static readonly string SingleInstanceMutexName = BuildSingleInstanceMutexName();
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HyperBoost X",
            "logs");
        private static readonly string LogFile = Path.Combine(LogDirectory, "hyperboost-launcher.log");
        private static readonly string BackendDir = LauncherRuntimeLayout.ResolveDirectory(AppRoot, InstallRoot, @"runtime\backend", "backend");
        private static readonly string WpfDir = LauncherRuntimeLayout.ResolveDirectory(AppRoot, InstallRoot, @"runtime\wpf", "wpf");
        private static readonly string BackendExe = LauncherRuntimeLayout.ResolveFile(AppRoot, InstallRoot, "hyperboost_backend.exe", @"runtime\backend", "backend");
        private static readonly string WpfExe = LauncherRuntimeLayout.ResolveFileFromCandidates(
            AppRoot,
            InstallRoot,
            new[] { "HyperBoostX.exe", "HyperBoostUI.exe" },
            @"runtime\wpf",
            "wpf");
        private static readonly int BackendPort = ResolveBackendPort();
        private static readonly string BackendBaseUrl = $"http://127.0.0.1:{BackendPort}";
        private static Process? _managedBackendProcess;
        private static bool _backendStartedByLauncher;
        private static Mutex? _singleInstanceMutex;
        private static readonly string SessionToken = GenerateSessionToken();

        static async Task Main(string[] args)
        {
            try
            {
                if (!AcquireSingleInstance())
                {
                    Environment.ExitCode = 0;
                    return;
                }

                AppDomain.CurrentDomain.ProcessExit += (_, _) => StopManagedBackend();
                Log("Launcher started.");

                if (!CheckFiles())
                {
                    Environment.ExitCode = 1;
                    return;
                }

                if (!StartBackend())
                {
                    Environment.ExitCode = 1;
                    return;
                }

                var backendReady = await WaitForBackend();
                if (!backendReady)
                {
                    Log("Backend health check did not pass before launching WPF.");
                }

                if (!await StartWpfClient())
                {
                    Environment.ExitCode = 1;
                }

                StopManagedBackend();
            }
            catch (Exception ex)
            {
                Log($"Launcher error: {ex}");
                StopManagedBackend();
                Environment.ExitCode = 1;
            }
            finally
            {
                ReleaseSingleInstance();
            }
        }

        private static bool AcquireSingleInstance()
        {
            try
            {
                _singleInstanceMutex = new Mutex(initiallyOwned: true, name: SingleInstanceMutexName, createdNew: out var createdNew);
                if (createdNew)
                {
                    return true;
                }

                Log("Launcher already running. Ignoring duplicate launch request.");
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
                return false;
            }
            catch (Exception ex)
            {
                Log($"WARNING: Failed to acquire single-instance mutex: {ex}");
                return true;
            }
        }

        private static void ReleaseSingleInstance()
        {
            try
            {
                _singleInstanceMutex?.ReleaseMutex();
            }
            catch
            {
                // Ignore release failures when mutex ownership is uncertain.
            }
            finally
            {
                _singleInstanceMutex?.Dispose();
                _singleInstanceMutex = null;
            }
        }

        private static bool CheckFiles()
        {
            if (string.IsNullOrEmpty(BackendExe) || !File.Exists(BackendExe))
            {
                Log($"ERROR: backend executable not found: {BackendExe}");
                return false;
            }

            if (string.IsNullOrEmpty(WpfExe) || !File.Exists(WpfExe))
            {
                Log($"ERROR: WPF executable not found: {WpfExe}");
                return false;
            }

            return true;
        }

        private static bool StartBackend()
        {
            try
            {
                StopExistingBackendProcessesFromThisRuntime();
                Thread.Sleep(250);

                if (!IsPortAvailable(BackendPort))
                {
                    Log($"ERROR: selected backend port {BackendPort} is already in use. Refusing to attach WPF to an unknown backend.");
                    return false;
                }

                _managedBackendProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = BackendExe,
                        WorkingDirectory = BackendDir,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };
                _managedBackendProcess.StartInfo.Environment["HYPERBOOSTX_SESSION_TOKEN"] = SessionToken;
                _managedBackendProcess.StartInfo.Environment["HYPERBOOSTX_BACKEND_PORT"] = BackendPort.ToString();

                if (!_managedBackendProcess.Start())
                {
                    Log("ERROR: Process.Start returned false for backend.");
                    return false;
                }

                _backendStartedByLauncher = true;
                Log($"Backend started: {BackendExe} on {BackendBaseUrl}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"ERROR: Failed to start backend: {ex}");
                return false;
            }
        }

        private static async Task<bool> WaitForBackend()
        {
            for (int i = 0; i < 15; i++)
            {
                if (await IsBackendHealthy())
                {
                    Log($"Backend healthy after {i + 1} checks.");
                    return true;
                }

                await Task.Delay(1000);
            }

            return false;
        }

        private static async Task<bool> StartWpfClient()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = WpfExe,
                        WorkingDirectory = WpfDir,
                        UseShellExecute = false,
                        CreateNoWindow = false,
                        WindowStyle = ProcessWindowStyle.Normal
                    }
                };
                process.StartInfo.Environment["HYPERBOOSTX_SESSION_TOKEN"] = SessionToken;
                process.StartInfo.Environment["HYPERBOOSTX_BACKEND_URL"] = BackendBaseUrl;

                if (!process.Start())
                {
                    Log("ERROR: Process.Start returned false for WPF client.");
                    return false;
                }

                Log($"WPF started: {WpfExe} (PID {process.Id}) using {BackendBaseUrl}");
                await process.WaitForExitAsync();
                Log($"WPF exited with code {process.ExitCode}.");
                return true;
            }
            catch (Exception ex)
            {
                Log($"ERROR: Failed to start WPF client: {ex}");
                return false;
            }
        }

        private static async Task<bool> IsBackendHealthy()
        {
            var health = await GetBackendHealth();
            return health.Healthy;
        }

        private static async Task<(bool Healthy, bool SessionTokenRequired)> GetBackendHealth()
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(2);

            try
            {
                var response = await client.GetAsync($"{BackendBaseUrl}/api/health");
                if (!response.IsSuccessStatusCode)
                    return (false, false);

                var json = await response.Content.ReadAsStringAsync();
                var tokenRequired = false;
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("session_token_required", out var property))
                        tokenRequired = property.ValueKind == JsonValueKind.True;
                }
                catch (JsonException ex)
                {
                    Log($"WARNING: Could not parse backend health JSON: {ex.Message}");
                }

                return (true, tokenRequired);
            }
            catch
            {
                return (false, false);
            }
        }

        private static void StopExistingBackendProcessesFromThisRuntime()
        {
            var killed = 0;
            var expectedBackendPath = Path.GetFullPath(BackendExe);
            var processName = Path.GetFileNameWithoutExtension(BackendExe);

            foreach (var process in Process.GetProcessesByName(processName))
            {
                try
                {
                    string? modulePath;
                    try
                    {
                        modulePath = process.MainModule?.FileName;
                    }
                    catch
                    {
                        continue;
                    }

                    if (!string.Equals(Path.GetFullPath(modulePath ?? string.Empty), expectedBackendPath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5000);
                    killed++;
                    Log($"Stopped stale backend process {process.Id} from current runtime.");
                }
                catch (Exception ex)
                {
                    Log($"WARNING: Failed to stop stale backend process {process.Id}: {ex.Message}");
                }
                finally
                {
                    process.Dispose();
                }
            }

            if (killed == 0)
                Log("No stale current-runtime backend process found.");
        }

        private static int ResolveBackendPort()
        {
            var configured = Environment.GetEnvironmentVariable("HYPERBOOSTX_BACKEND_PORT");
            if (int.TryParse(configured, out var configuredPort)
                && configuredPort is >= 1024 and <= 65535
                && IsPortAvailable(configuredPort))
            {
                return configuredPort;
            }

            return 5000;
        }

        private static bool IsPortAvailable(int port)
        {
            try
            {
                using var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        private static string BuildSingleInstanceMutexName()
        {
            var runtimeIdentity = Path.GetFullPath(InstallRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToUpperInvariant();
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(runtimeIdentity));
            return SingleInstanceMutexPrefix + BitConverter.ToString(hash, 0, 8).Replace("-", string.Empty, StringComparison.Ordinal);
        }

        private static string GenerateSessionToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
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
                // Avoid crashing the launcher because of log file issues.
            }
        }

        private static void StopManagedBackend()
        {
            if (!_backendStartedByLauncher)
            {
                return;
            }

            try
            {
                if (_managedBackendProcess != null && !_managedBackendProcess.HasExited)
                {
                    _managedBackendProcess.Kill(entireProcessTree: true);
                    _managedBackendProcess.WaitForExit(5000);
                    Log("Managed backend stopped.");
                }
            }
            catch (Exception ex)
            {
                Log($"WARNING: Failed to stop managed backend cleanly: {ex}");
            }
            finally
            {
                _managedBackendProcess?.Dispose();
                _managedBackendProcess = null;
                _backendStartedByLauncher = false;
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HyperBoostLauncher
{
    internal class Program
    {
        private const string SingleInstanceMutexName = @"Global\HyperBoostXLauncherSingleInstance";
        private static readonly string AppRoot = Path.GetDirectoryName(AppContext.BaseDirectory) ?? "";
        private static readonly string InstallRoot = Directory.GetParent(AppRoot)?.FullName ?? AppRoot;
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HyperBoost X",
            "logs");
        private static readonly string LogFile = Path.Combine(LogDirectory, "hyperboost-launcher.log");
        private static readonly string BackendDir = ResolveDirectory(@"runtime\backend", "backend");
        private static readonly string WpfDir = ResolveDirectory(@"runtime\wpf", "wpf");
        private static readonly string BackendExe = ResolveFile("hyperboost_backend.exe", @"runtime\backend", "backend");
        private static readonly string WpfExe = ResolveFile("HyperBoostUI.exe", @"runtime\wpf", "wpf", "HyperBoostX.exe");
        private static Process? _managedBackendProcess;
        private static bool _backendStartedByLauncher;
        private static Mutex? _singleInstanceMutex;

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
                if (IsBackendHealthy().GetAwaiter().GetResult())
                {
                    Log("Backend already healthy.");
                    return true;
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

                if (!_managedBackendProcess.Start())
                {
                    Log("ERROR: Process.Start returned false for backend.");
                    return false;
                }

                _backendStartedByLauncher = true;
                Log($"Backend started: {BackendExe}");
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
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Normal
                    }
                };

                if (!process.Start())
                {
                    Log("ERROR: Process.Start returned false for WPF client.");
                    return false;
                }

                Log($"WPF started: {WpfExe} (PID {process.Id})");
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
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(2);

            try
            {
                var response = await client.GetAsync("http://127.0.0.1:5000/api/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static string ResolveDirectory(params string[] candidateSubdirs)
        {
            foreach (var subdir in candidateSubdirs)
            {
                var preferred = Path.Combine(AppRoot, subdir);
                if (Directory.Exists(preferred))
                {
                    return preferred;
                }

                var sibling = Path.Combine(InstallRoot, subdir);
                if (Directory.Exists(sibling))
                {
                    return sibling;
                }
            }

            return AppRoot;
        }

        private static string ResolveFile(string fileName, params string[] candidateSubdirs)
        {
            foreach (var subdir in candidateSubdirs)
            {
                var nested = Path.Combine(AppRoot, subdir, fileName);
                if (File.Exists(nested))
                {
                    return nested;
                }

                var sibling = Path.Combine(InstallRoot, subdir, fileName);
                if (File.Exists(sibling))
                {
                    return sibling;
                }
            }

            var root = Path.Combine(AppRoot, fileName);
            if (File.Exists(root))
            {
                return root;
            }

            return Path.Combine(AppRoot, candidateSubdirs.Length > 0 ? candidateSubdirs[0] : string.Empty, fileName);
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

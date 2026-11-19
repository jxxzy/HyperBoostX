using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace HyperBoostX.Services
{
    /// <summary>
    /// C# WPF client for HyperBoostX Python backend API
    /// Communicates with the launcher-provided or locally configured Flask REST API.
    /// </summary>
    public class HyperBoostBackendClient : IHyperBoostBackendClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private const string SessionHeaderName = "X-HyperBoostX-Session";
        private readonly SemaphoreSlim _healthCheckLock = new(1, 1);
        private readonly SemaphoreSlim _systemStatsLock = new(1, 1);
        private readonly SemaphoreSlim _systemInfoLock = new(1, 1);
        private readonly SemaphoreSlim _startupItemsLock = new(1, 1);
        private readonly SemaphoreSlim _processesLock = new(1, 1);
        private bool? _cachedBackendHealth;
        private DateTime _cachedBackendHealthUtc = DateTime.MinValue;
        private JObject _cachedSystemStats;
        private DateTime _cachedSystemStatsUtc = DateTime.MinValue;
        private JObject _cachedSystemInfo;
        private DateTime _cachedSystemInfoUtc = DateTime.MinValue;
        private JObject _cachedStartupItems;
        private DateTime _cachedStartupItemsUtc = DateTime.MinValue;
        private JObject _cachedProcesses;
        private DateTime _cachedProcessesUtc = DateTime.MinValue;
        private static readonly TimeSpan HealthCheckCacheLifetime = TimeSpan.FromMilliseconds(1500);
        private static readonly TimeSpan SystemStatsCacheLifetime = TimeSpan.FromMilliseconds(900);
        private static readonly TimeSpan SystemInfoCacheLifetime = TimeSpan.FromSeconds(12);
        private static readonly TimeSpan StartupItemsCacheLifetime = TimeSpan.FromSeconds(6);
        private static readonly TimeSpan ProcessesCacheLifetime = TimeSpan.FromSeconds(2);

        public HyperBoostBackendClient(string baseUrl = null)
        {
            _baseUrl = ResolveBaseUrl(baseUrl);
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            var sessionToken = Environment.GetEnvironmentVariable("HYPERBOOSTX_SESSION_TOKEN");
            if (!string.IsNullOrWhiteSpace(sessionToken))
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(SessionHeaderName, sessionToken.Trim());
        }

        public string BaseUrl => _baseUrl;

        private static string ResolveBaseUrl(string baseUrl)
        {
            var configuredUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? Environment.GetEnvironmentVariable("HYPERBOOSTX_BACKEND_URL")
                : baseUrl;

            if (string.IsNullOrWhiteSpace(configuredUrl))
                configuredUrl = DiscoverCompatibleLocalBackendUrl();

            return configuredUrl.Trim().TrimEnd('/');
        }

        private static string DiscoverCompatibleLocalBackendUrl()
        {
            var configuredPort = Environment.GetEnvironmentVariable("HYPERBOOSTX_BACKEND_PORT");
            var candidates = new List<int>();
            if (int.TryParse(configuredPort, out var port) && port is >= 1024 and <= 65535)
                candidates.Add(port);

            for (var candidatePort = 5055; candidatePort <= 5065; candidatePort++)
                candidates.Add(candidatePort);

            candidates.Add(5000);

            foreach (var candidatePort in candidates)
            {
                var candidateUrl = $"http://127.0.0.1:{candidatePort}";
                if (IsCompatibleBackend(candidateUrl))
                    return candidateUrl;
            }

            var legacyDefault = "http://127.0.0.1:5000";
            return IsBackendHealthy(legacyDefault)
                ? "http://127.0.0.1:5055"
                : legacyDefault;
        }

        private static bool IsCompatibleBackend(string candidateUrl)
        {
            return ProbeBackend(candidateUrl, "/api/feature-audit/matrix");
        }

        private static bool IsBackendHealthy(string candidateUrl)
        {
            return ProbeBackend(candidateUrl, "/api/health");
        }

        private static bool ProbeBackend(string candidateUrl, string path)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(220) };
                var response = client.GetAsync($"{candidateUrl.TrimEnd('/')}{path}").GetAwaiter().GetResult();
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Health check - verify backend is running
        /// </summary>
        public async Task<bool> HealthCheckAsync()
        {
            if (_cachedBackendHealth.HasValue && DateTime.UtcNow - _cachedBackendHealthUtc <= HealthCheckCacheLifetime)
                return _cachedBackendHealth.Value;

            await _healthCheckLock.WaitAsync();
            try
            {
                if (_cachedBackendHealth.HasValue && DateTime.UtcNow - _cachedBackendHealthUtc <= HealthCheckCacheLifetime)
                    return _cachedBackendHealth.Value;

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/health");
                _cachedBackendHealth = response.IsSuccessStatusCode;
                _cachedBackendHealthUtc = DateTime.UtcNow;
                return _cachedBackendHealth.Value;
            }
            catch
            {
                _cachedBackendHealth = false;
                _cachedBackendHealthUtc = DateTime.UtcNow;
                return false;
            }
            finally
            {
                _healthCheckLock.Release();
            }
        }

        /// <summary>
        /// Get complete system information (CPU, GPU, memory, disk, network, OS, temps)
        /// </summary>
        public async Task<dynamic> GetSystemInfoAsync()
        {
            if (TryGetFreshCache(_cachedSystemInfoUtc, SystemInfoCacheLifetime, _cachedSystemInfo, out var cachedInfo))
                return cachedInfo;

            await _systemInfoLock.WaitAsync();
            try
            {
                if (TryGetFreshCache(_cachedSystemInfoUtc, SystemInfoCacheLifetime, _cachedSystemInfo, out cachedInfo))
                    return cachedInfo;

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/system/info");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var parsed = JObject.Parse(json);
                _cachedSystemInfo = parsed;
                _cachedSystemInfoUtc = DateTime.UtcNow;
                return CloneToken(parsed);
            }
            catch (Exception ex)
            {
                if (_cachedSystemInfo != null)
                    return CloneToken(_cachedSystemInfo);

                throw new InvalidOperationException($"Failed to get system info: {ex.Message}", ex);
            }
            finally
            {
                _systemInfoLock.Release();
            }
        }

        /// <summary>
        /// Get real-time system statistics (CPU%, memory%, disk%, process count, boot time, CPU freq)
        /// </summary>
        public async Task<dynamic> GetSystemStatsAsync()
        {
            if (TryGetFreshCache(_cachedSystemStatsUtc, SystemStatsCacheLifetime, _cachedSystemStats, out var cachedStats))
                return cachedStats;

            await _systemStatsLock.WaitAsync();
            try
            {
                if (TryGetFreshCache(_cachedSystemStatsUtc, SystemStatsCacheLifetime, _cachedSystemStats, out cachedStats))
                    return cachedStats;

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/system/stats");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var parsed = JObject.Parse(json);
                _cachedSystemStats = parsed;
                _cachedSystemStatsUtc = DateTime.UtcNow;
                return CloneToken(parsed);
            }
            catch (Exception ex)
            {
                if (_cachedSystemStats != null)
                    return CloneToken(_cachedSystemStats);

                throw new InvalidOperationException($"Failed to get system stats: {ex.Message}", ex);
            }
            finally
            {
                _systemStatsLock.Release();
            }
        }

        private static bool TryGetFreshCache(DateTime cachedUtc, TimeSpan lifetime, JObject cache, out JObject clone)
        {
            if (cache != null && DateTime.UtcNow - cachedUtc <= lifetime)
            {
                clone = (JObject)cache.DeepClone();
                return true;
            }

            clone = null;
            return false;
        }

        private static JObject CloneToken(JObject source)
        {
            return source == null ? null : (JObject)source.DeepClone();
        }

        private static JToken ParseJsonToken(string json)
        {
            return string.IsNullOrWhiteSpace(json) ? JValue.CreateNull() : JToken.Parse(json);
        }

        public async Task<dynamic> GetJsonAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("API path is required.", nameof(path));

            var normalizedPath = path.StartsWith("/", StringComparison.Ordinal) ? path : "/" + path;
            var response = await _httpClient.GetAsync($"{_baseUrl}{normalizedPath}");
            await EnsureJsonSuccessAsync(response, normalizedPath);
            var json = await response.Content.ReadAsStringAsync();
            return ParseJsonToken(json);
        }

        public async Task<dynamic> PostJsonRouteAsync(string path, object payload = null)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("API path is required.", nameof(path));

            var normalizedPath = path.StartsWith("/", StringComparison.Ordinal) ? path : "/" + path;
            return await PostJsonAsync(normalizedPath, payload ?? new { });
        }

        private async Task<dynamic> PostJsonAsync(string path, object payload)
        {
            var content = new StringContent(
                JsonConvert.SerializeObject(payload ?? new { }),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync($"{_baseUrl}{path}", content);
            await EnsureJsonSuccessAsync(response, path);
            var json = await response.Content.ReadAsStringAsync();
            return ParseJsonToken(json);
        }

        private static async Task EnsureJsonSuccessAsync(HttpResponseMessage response, string path)
        {
            if (response.IsSuccessStatusCode)
                return;

            var body = await response.Content.ReadAsStringAsync();
            if (body.Length > 2000)
                body = body[..2000] + " ... response truncated";

            throw new HttpRequestException(
                $"Backend request {path} failed with {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
        }

        private static JObject NormalizeNamedArrayPayload(JToken payload, params string[] preferredKeys)
        {
            if (payload is JObject obj)
            {
                if (preferredKeys.Length >= 2 && obj[preferredKeys[0]] is JArray primary && obj[preferredKeys[1]] == null)
                    obj[preferredKeys[1]] = primary.DeepClone();
                else if (preferredKeys.Length >= 2 && obj[preferredKeys[1]] is JArray secondary && obj[preferredKeys[0]] == null)
                    obj[preferredKeys[0]] = secondary.DeepClone();

                if (preferredKeys.Length >= 1 && obj[preferredKeys[0]] == null)
                    obj[preferredKeys[0]] = new JArray();

                return obj;
            }

            var array = payload as JArray ?? new JArray();
            var normalized = new JObject();
            foreach (var key in preferredKeys)
                normalized[key] = array.DeepClone();

            normalized["count"] = array.Count;
            return normalized;
        }

        /// <summary>
        /// Get list of available Windows tweaks
        /// </summary>
        public async Task<dynamic> GetTweaksAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/tweaks/list");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return ParseJsonToken(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get tweaks: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Apply a specific tweak by ID
        /// </summary>
        public async Task<dynamic> ApplyTweakAsync(string tweakId, bool expertMode = false, bool confirmed = false)
        {
            try
            {
                var content = new StringContent(
                    JsonConvert.SerializeObject(new { tweak_id = tweakId, expert_mode = expertMode, confirmed = confirmed }),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/tweaks/apply", content);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return ParseJsonToken(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to apply tweak: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get available booster profiles (FPS, Latency, Streaming, Balanced)
        /// </summary>
        public async Task<dynamic> GetBoosterProfilesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/booster/profiles");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return ParseJsonToken(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get booster profiles: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Apply a specific booster profile (fps, latency, streaming, balanced)
        /// </summary>
        public async Task<dynamic> ApplyBoosterAsync(string profile)
        {
            try
            {
                var content = new StringContent(
                    JsonConvert.SerializeObject(new { profile = profile }),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/booster/apply", content);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return ParseJsonToken(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to apply booster: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get list of installed drivers with version and status
        /// </summary>
        public async Task<dynamic> GetDriversAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/drivers/list");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return ParseJsonToken(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get drivers: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Check for driver updates
        /// </summary>
        public async Task<dynamic> CheckDriverUpdatesAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/drivers/check-updates", null);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return ParseJsonToken(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to check driver updates: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Run System File Checker (SFC) scan
        /// </summary>
        public async Task<dynamic> RunSfcAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/repair/run-sfc", null);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return ParseJsonToken(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to run SFC: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Cleanup temporary files
        /// </summary>
        public async Task<dynamic> CleanupAsync(string scope = null)
        {
            try
            {
                HttpContent content = null;
                if (!string.IsNullOrWhiteSpace(scope))
                {
                    content = new StringContent(
                        JsonConvert.SerializeObject(new { scope }),
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );
                }

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/repair/cleanup", content);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return ParseJsonToken(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to cleanup: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Run DISM repair operation
        /// </summary>
        public async Task<dynamic> RunDismAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/repair/run-dism", null);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return ParseJsonToken(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to run DISM: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get startup items
        /// </summary>
        public async Task<dynamic> GetStartupItemsAsync()
        {
            if (TryGetFreshCache(_cachedStartupItemsUtc, StartupItemsCacheLifetime, _cachedStartupItems, out var cachedStartupItems))
                return cachedStartupItems;

            await _startupItemsLock.WaitAsync();
            try
            {
                if (TryGetFreshCache(_cachedStartupItemsUtc, StartupItemsCacheLifetime, _cachedStartupItems, out cachedStartupItems))
                    return cachedStartupItems;

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/startup/list");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var parsed = NormalizeNamedArrayPayload(ParseJsonToken(json), "startup_items", "items");
                _cachedStartupItems = parsed;
                _cachedStartupItemsUtc = DateTime.UtcNow;
                return CloneToken(parsed);
            }
            catch (Exception ex)
            {
                if (_cachedStartupItems != null)
                    return CloneToken(_cachedStartupItems);

                throw new InvalidOperationException($"Failed to get startup items: {ex.Message}", ex);
            }
            finally
            {
                _startupItemsLock.Release();
            }
        }

        /// <summary>
        /// Get running processes sorted by memory impact
        /// </summary>
        public async Task<dynamic> GetProcessesAsync()
        {
            if (TryGetFreshCache(_cachedProcessesUtc, ProcessesCacheLifetime, _cachedProcesses, out var cachedProcesses))
                return cachedProcesses;

            await _processesLock.WaitAsync();
            try
            {
                if (TryGetFreshCache(_cachedProcessesUtc, ProcessesCacheLifetime, _cachedProcesses, out cachedProcesses))
                    return cachedProcesses;

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/system/processes");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var parsed = NormalizeNamedArrayPayload(ParseJsonToken(json), "processes");
                _cachedProcesses = parsed;
                _cachedProcessesUtc = DateTime.UtcNow;
                return CloneToken(parsed);
            }
            catch (Exception ex)
            {
                if (_cachedProcesses != null)
                    return CloneToken(_cachedProcesses);

                throw new InvalidOperationException($"Failed to get process list: {ex.Message}", ex);
            }
            finally
            {
                _processesLock.Release();
            }
        }

        public async Task<dynamic> GetHardwareGpuAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/hardware/gpu");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return ParseJsonToken(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get GPU Center data: {ex.Message}", ex);
            }
        }

        public async Task<dynamic> GetHardwareVendorsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/hardware/vendors");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return ParseJsonToken(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get vendor software data: {ex.Message}", ex);
            }
        }

        public async Task<dynamic> GetHardwareOverlaysAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/hardware/overlays");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return ParseJsonToken(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get overlay data: {ex.Message}", ex);
            }
        }

        public async Task<dynamic> GetHardwareProfileAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/hardware/profile");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return ParseJsonToken(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get hardware profile: {ex.Message}", ex);
            }
        }

        public async Task<dynamic> CreateBoostPlanAsync(string goal = "gaming", string mode = "balanced")
        {
            try
            {
                return await PostJsonAsync("/api/boost/plan", new
                {
                    goal = string.IsNullOrWhiteSpace(goal) ? "gaming" : goal,
                    mode = string.IsNullOrWhiteSpace(mode) ? "balanced" : mode,
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create safe boost plan: {ex.Message}", ex);
            }
        }

        public async Task<dynamic> ApplyBoostPlanAsync(IReadOnlyList<string> approvedActionIds = null, bool userApproved = false)
        {
            try
            {
                return await PostJsonAsync("/api/boost/apply", new
                {
                    user_approved = userApproved,
                    approved_action_ids = approvedActionIds ?? Array.Empty<string>(),
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to apply safe boost plan: {ex.Message}", ex);
            }
        }

        public async Task<dynamic> UndoBoostPlanAsync()
        {
            try
            {
                return await PostJsonAsync("/api/boost/undo", new { });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to undo safe boost plan: {ex.Message}", ex);
            }
        }

        public async Task<dynamic> ExportReportAsync(string format = "md")
        {
            try
            {
                var content = new StringContent(
                    JsonConvert.SerializeObject(new { format = string.IsNullOrWhiteSpace(format) ? "md" : format }),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/reports/export", content);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return ParseJsonToken(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to export report: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Test DNS resolution
        /// </summary>
        public async Task<dynamic> TestDnsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/network/dns-test");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return ParseJsonToken(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to test DNS: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Flush DNS cache
        /// </summary>
        public async Task<dynamic> FlushDnsAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/network/flush-dns", null);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return ParseJsonToken(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to flush DNS: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Optimize TCP settings
        /// </summary>
        public async Task<dynamic> OptimizeTcpAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/network/optimize-tcp", null);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return ParseJsonToken(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to optimize TCP: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reset network stack
        /// </summary>
        public async Task<dynamic> ResetNetworkAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/repair/reset-network", null);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return ParseJsonToken(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to reset network: {ex.Message}", ex);
            }
        }
        public async Task<dynamic> RunSafePlanFlowAsync(string userGoal = "gaming", string game = "")
        {
            try
            {
                return await CreateBoostPlanAsync(userGoal, "balanced");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to run safe plan flow: {ex.Message}", ex);
            }
        }

        public async Task<dynamic> ApplySafePlanActionsAsync(JArray approvedActions, bool userApproved = false)
        {
            try
            {
                var approvedIds = new List<string>();
                if (approvedActions != null)
                {
                    foreach (var item in approvedActions)
                    {
                        var id = item.Type == JTokenType.String ? item.ToString() : item["id"]?.ToString() ?? item["action_id"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(id))
                            approvedIds.Add(id);
                    }
                }

                return await ApplyBoostPlanAsync(approvedIds, userApproved);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to apply safe plan actions: {ex.Message}", ex);
            }
        }

        public async Task<dynamic> RevertSafePlanActionsAsync(string backupId = "", IReadOnlyList<string> actionIds = null)
        {
            try
            {
                return await UndoBoostPlanAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to undo safe plan actions: {ex.Message}", ex);
            }
        }

        [Obsolete("Use RunSafePlanFlowAsync. This wrapper is kept only for v1.x compatibility.")]
        public Task<dynamic> RunTripleAiFlowAsync(string userGoal = "gaming", string game = "")
        {
            return RunSafePlanFlowAsync(userGoal, game);
        }

        [Obsolete("Use ApplySafePlanActionsAsync. This wrapper is kept only for v1.x compatibility.")]
        public Task<dynamic> ApplyTripleAiTweaksAsync(JArray approvedTweaks, bool userApproved = false)
        {
            return ApplySafePlanActionsAsync(approvedTweaks, userApproved);
        }

        [Obsolete("Use RevertSafePlanActionsAsync. This wrapper is kept only for v1.x compatibility.")]
        public Task<dynamic> RevertTripleAiTweaksAsync(string backupId = "", IReadOnlyList<string> tweakIds = null)
        {
            return RevertSafePlanActionsAsync(backupId, tweakIds);
        }

        /// <summary>
        /// Format complex objects to readable strings
        /// </summary>
        public static string FormatJson(dynamic obj, int indent = 0)
        {
            try
            {
                return JsonConvert.SerializeObject(obj, Formatting.Indented);
            }
            catch
            {
                return obj?.ToString() ?? "null";
            }
        }

        public void Dispose()
        {
            _healthCheckLock.Dispose();
            _systemStatsLock.Dispose();
            _systemInfoLock.Dispose();
            _startupItemsLock.Dispose();
            _processesLock.Dispose();
            _httpClient?.Dispose();
        }
    }
}



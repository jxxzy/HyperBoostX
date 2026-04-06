using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace HyperBoostX.Services
{
    /// <summary>
    /// C# WPF client for HyperBoost X Python backend API
    /// Communicates with Flask REST API server running on localhost:5000
    /// </summary>
    public class HyperBoostBackendClient : IHyperBoostBackendClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public HyperBoostBackendClient(string baseUrl = "http://127.0.0.1:5000")
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        /// <summary>
        /// Health check - verify backend is running
        /// </summary>
        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get complete system information (CPU, GPU, memory, disk, network, OS, temps)
        /// </summary>
        public async Task<dynamic> GetSystemInfoAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/system/info");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get system info: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get real-time system statistics (CPU%, memory%, disk%, process count, boot time, CPU freq)
        /// </summary>
        public async Task<dynamic> GetSystemStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/system/stats");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get system stats: {ex.Message}", ex);
            }
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
                return JsonConvert.DeserializeObject(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get tweaks: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Apply a specific tweak by ID
        /// </summary>
        public async Task<dynamic> ApplyTweakAsync(string tweakId)
        {
            try
            {
                var content = new StringContent(
                    JsonConvert.SerializeObject(new { tweak_id = tweakId }),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/tweaks/apply", content);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject(json);
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
                return JsonConvert.DeserializeObject(json);
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
                return JsonConvert.DeserializeObject(json);
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
                return JsonConvert.DeserializeObject(json);
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
                return JsonConvert.DeserializeObject(json);
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
                return JsonConvert.DeserializeObject(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to run SFC: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Cleanup temporary files
        /// </summary>
        public async Task<dynamic> CleanupAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/repair/cleanup", null);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject(json);
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
                return JsonConvert.DeserializeObject(json);
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
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/startup/list");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get startup items: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get running processes sorted by memory impact
        /// </summary>
        public async Task<dynamic> GetProcessesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/system/processes");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get process list: {ex.Message}", ex);
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
                return JsonConvert.DeserializeObject(json);
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
                return JsonConvert.DeserializeObject(json);
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
                return JsonConvert.DeserializeObject(json);
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
                return JsonConvert.DeserializeObject(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to reset network: {ex.Message}", ex);
            }
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
            _httpClient?.Dispose();
        }
    }
}

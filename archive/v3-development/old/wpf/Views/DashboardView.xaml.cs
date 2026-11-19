using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HyperBoostX.Services;
using HyperBoostX.ViewModels;
using Newtonsoft.Json.Linq;

namespace HyperBoostX.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly HyperBoostBackendClient _client = new();
        private bool _loadedOnce;

        public DashboardView()
        {
            InitializeComponent();
            Unloaded += (_, _) => _client.Dispose();
        }

        private DashboardViewModel ViewModel => DataContext as DashboardViewModel;

        private async void StartSmartScan_Click(object sender, RoutedEventArgs e)
        {
            await RunSmartScanAsync();
        }

        private async void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_loadedOnce)
                return;

            _loadedOnce = true;
            await RefreshDashboardStatusAsync(updateRecommendation: false);
        }

        private async void RefreshStatus_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDashboardStatusAsync(updateRecommendation: true);
        }

        private async void OneClickSafeBoost_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
                return;

            try
            {
                ViewModel.AiRecommendation = "Building a safe boost plan. No changes are applied without approval.";
                var plan = await _client.CreateBoostPlanAsync("gaming", "balanced");
                ViewModel.AiRecommendation = plan == null
                    ? "Backend returned no plan; run Smart Scan first."
                    : "Safe boost plan created. Review actions before applying anything.";
                NavigateShell("OneClickBoost");
            }
            catch (Exception ex)
            {
                ViewModel.AiRecommendation = $"Safe boost plan unavailable: {ex.Message}";
            }
        }

        private void AutoGamingMode_Click(object sender, RoutedEventArgs e) => NavigateShell("AutoGamingMode");
        private void ViewLastReport_Click(object sender, RoutedEventArgs e) => NavigateShell("PerformanceReport");
        private void RestoreChanges_Click(object sender, RoutedEventArgs e) => NavigateShell("RestoreBackup");
        private void OpenBoostPlan_Click(object sender, RoutedEventArgs e) => NavigateShell("OneClickBoost");
        private void OpenSettings_Click(object sender, RoutedEventArgs e) => NavigateShell("Settings");
        private void ViewPerformanceHistory_Click(object sender, RoutedEventArgs e) => NavigateShell("PerformanceHistory");

        private async void ExportDashboardReport_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null || ViewModel.IsBusy)
                return;

            SetDashboardBusy(true);
            try
            {
                await _client.PostJsonRouteAsync("/api/reports/export", new { format = "json", source = "dashboard" });
                ViewModel.AiRecommendation = "Dashboard report export completed. Open Reports to inspect the local redacted output.";
            }
            catch (Exception ex)
            {
                ViewModel.AiRecommendation = BuildFriendlyDashboardError("Export Report", ex);
            }
            finally
            {
                SetDashboardBusy(false);
            }
        }

        public async Task RunSmartScanAsync()
        {
            if (ViewModel == null)
                return;

            SetDashboardBusy(true);

            try
            {
                ViewModel.BackendStatus = "Checking";
                var online = await _client.HealthCheckAsync();
                ViewModel.BackendStatus = online ? "Online" : "Offline";
                UpdateMetric("Backend", online ? "Online" : "Offline", online ? "Local API is reachable" : "UI remains safe offline", online ? 100 : 20);

                if (!online)
                {
                    ViewModel.AiRecommendation = "Backend is offline. Start through the HyperBoostX launcher to enable live scan data.";
                    return;
                }

                await RefreshDashboardStatusAsync(updateRecommendation: false, skipHealthCheck: true, manageBusy: false);

                var scan = await _client.PostJsonRouteAsync("/api/scan/smart", new { goal = "gaming", mode = "balanced" });
                if (scan is JToken scanToken)
                {
                    UpdateScoresFromScan(scanToken);
                    UpdateScanRecommendation(scanToken);
                }

                await RefreshDashboardStatusAsync(updateRecommendation: false, skipHealthCheck: true, manageBusy: false);
            }
            catch (Exception ex)
            {
                ViewModel.BackendStatus = "Offline";
                ViewModel.AiRecommendation = BuildFriendlyDashboardError("Smart Scan", ex);
            }
            finally
            {
                SetDashboardBusy(false);
            }
        }

        private async Task RefreshDashboardStatusAsync(bool updateRecommendation, bool skipHealthCheck = false, bool manageBusy = true)
        {
            if (ViewModel == null || (manageBusy && ViewModel.IsBusy))
                return;

            if (manageBusy)
                SetDashboardBusy(true);

            try
            {
                var online = skipHealthCheck || await _client.HealthCheckAsync();
                ViewModel.BackendStatus = online ? "Online" : "Offline";
                UpdateMetric("Backend", online ? "Online" : "Offline", online ? "Local API is reachable" : "Start through HyperBoostX launcher", online ? 100 : 20);

                if (!online)
                {
                    if (updateRecommendation)
                        ViewModel.AiRecommendation = "Backend offline. Start HyperBoostX through the launcher, then click Refresh Status.";
                    return;
                }

                var stats = await _client.GetSystemStatsAsync();
                if (stats is JObject obj)
                    UpdateSystemStats(obj);

                try
                {
                    var gpu = await _client.GetHardwareGpuAsync();
                    if (gpu is JToken gpuToken)
                        UpdateGpuStatus(gpuToken);
                }
                catch
                {
                    ViewModel.ActiveGpu = "Unknown GPU fallback";
                    UpdateMetric("GPU", "Fallback", "GPU sensor unavailable; safe generic guidance is active", 64);
                    UpdateMetric("VRAM", "Unavailable", "Sensor unavailable on this device/API", 50);
                }

                try
                {
                    var overlays = await _client.GetHardwareOverlaysAsync();
                    UpdateOverlayStatus(overlays as JToken);
                }
                catch
                {
                    UpdateMetric("Overlays", "Review", "Overlay detection unavailable; open GPU Center", 50);
                }

                try
                {
                    var restore = await _client.GetJsonAsync("/api/restore/sessions");
                    UpdateRestoreStatus(restore as JToken);
                }
                catch
                {
                    UpdateMetric("Restore", "Unknown", "Restore session status unavailable", 50);
                }

                try
                {
                    var game = await _client.GetJsonAsync("/api/games/running");
                    UpdateActiveGame(game as JToken);
                }
                catch
                {
                    UpdateMetric("Active Game", "Detect", "Game detection unavailable; Auto Gaming remains safe", 50);
                }

                if (updateRecommendation)
                    ViewModel.AiRecommendation = "Status refreshed. Use Smart Scan for scores, then review GPU, startup, cleanup, and restore before applying any approved change.";
            }
            catch (Exception ex)
            {
                ViewModel.BackendStatus = "Offline";
                ViewModel.AiRecommendation = BuildFriendlyDashboardError("Refresh Status", ex);
            }
            finally
            {
                if (manageBusy)
                    SetDashboardBusy(false);
            }
        }

        private void NavigateShell(string key)
        {
            if (Window.GetWindow(this) is MainWindow shell)
                shell.NavigateToPage(key);
        }

        private void UpdatePercentMetric(string title, JObject source, params string[] keys)
        {
            var value = FindNumber(source, keys);
            if (!value.HasValue)
                return;

            var percent = Math.Clamp(value.Value, 0, 100);
            UpdateMetric(title, $"{percent:0}%", "Live backend metric", (int)Math.Round(percent));
        }

        private void UpdateSystemStats(JObject obj)
        {
            UpdatePercentMetric("CPU", obj, "cpu_percent", "cpu", "cpu_usage", "cpu_usage_percent");
            UpdatePercentMetric("RAM", obj, "memory_percent", "ram_percent", "memory", "ram_usage");
            var disk = FindNumber(obj, "disk", "disk_usage", "disk_usage_percent");
            if (disk.HasValue)
            {
                var used = FindNumber(obj, "disk_used_gb");
                var total = FindNumber(obj, "disk_total_gb");
                var detail = used.HasValue && total.HasValue
                    ? $"{used.Value:0.0}/{total.Value:0.0} GB used on system drive"
                    : "System drive usage from backend";
                UpdateMetric("Storage", $"{Math.Clamp(disk.Value, 0, 100):0}%", detail, (int)Math.Round(Math.Clamp(disk.Value, 0, 100)));
            }

            var download = FindNumber(obj, "network_download_mb_s");
            var upload = FindNumber(obj, "network_upload_mb_s");
            if (download.HasValue || upload.HasValue)
            {
                UpdateMetric(
                    "Network",
                    $"{(download ?? 0):0.0}/{(upload ?? 0):0.0} MB/s",
                    "Download/upload throughput; use Network Tools for ping/DNS",
                    80);
            }

        }

        private void UpdateGpuStatus(JToken token)
        {
            var model = FindString(token, "model", "active_display_gpu", "name") ?? "Unknown GPU";
            var vendor = FindString(token, "vendor") ?? "Unknown";
            var usage = FindNumber(token, "gpu_usage_percent", "load");
            var vramPercent = FindNumber(token, "vram_usage_percent", "memory_percent");
            var vramTotal = FindNumber(token, "vram_total_mb", "memory_total_mb");
            var vramUsed = FindNumber(token, "vram_used_mb", "memory_used_mb");
            var driver = FindString(token, "driver_version") ?? "Unknown";

            ViewModel.ActiveGpu = model;
            if (usage.HasValue)
                UpdateMetric("GPU", $"{Math.Clamp(usage.Value, 0, 100):0}%", $"{vendor} - {model}", (int)Math.Round(Math.Clamp(usage.Value, 0, 100)));
            else
                UpdateMetric("GPU", vendor, $"{model}; usage sensor unavailable", 70);

            if (vramPercent.HasValue && vramTotal.HasValue && vramTotal.Value > 0)
                UpdateMetric("VRAM", $"{Math.Clamp(vramPercent.Value, 0, 100):0}%", $"{FormatMb(vramUsed ?? 0)}/{FormatMb(vramTotal.Value)}", (int)Math.Round(Math.Clamp(vramPercent.Value, 0, 100)));
            else if (vramTotal.HasValue && vramTotal.Value > 0)
                UpdateMetric("VRAM", FormatMb(vramTotal.Value), "Total VRAM detected; usage sensor unavailable", 75);
            else
                UpdateMetric("VRAM", "Unavailable", "Sensor unavailable on this GPU/API", 50);

            UpdateMetric("GPU", usage.HasValue ? $"{Math.Clamp(usage.Value, 0, 100):0}%" : vendor, $"{model}; driver {driver}", usage.HasValue ? (int)Math.Round(Math.Clamp(usage.Value, 0, 100)) : 70);
        }

        private void UpdateScoresFromScan(JToken scanToken)
        {
            var scores = scanToken.SelectToken("scores") ?? scanToken.SelectToken("recommended_safe_plan.plan.score_engine.scores");
            if (scores == null)
                return;

            UpdateScoreMetric("PC Health", scores, "health_score", "pc_health");
            UpdateScoreMetric("Gaming Readiness", scores, "gaming_score", "gaming_readiness");
            UpdateScoreMetric("Streaming Readiness", scores, "streaming_score", "streaming_readiness");
            UpdateScoreMetric("Storage Score", scores, "storage_score");
            UpdateScoreMetric("Network Score", scores, "network_score");
            UpdateScoreMetric("Safety Score", scores, "security_score", "safety_score");
        }

        private void UpdateScanRecommendation(JToken scanToken)
        {
            var finding = scanToken.SelectToken("bottleneck_analysis[0].message")?.ToString();
            var safeActions = scanToken.SelectToken("recommended_safe_plan.safe_actions") as JArray;
            ViewModel.AiRecommendation = string.IsNullOrWhiteSpace(finding)
                ? "Smart Scan complete. Review the safe plan before applying anything. No FPS gain is guaranteed."
                : $"Smart Scan complete: {finding} Review {safeActions?.Count ?? 0} safe action(s) before apply.";
        }

        private void UpdateScoreMetric(string title, JToken scores, params string[] keys)
        {
            var value = FindNumber(scores, keys);
            if (!value.HasValue)
                return;

            var score = Math.Clamp(value.Value, 0, 100);
            var metric = ViewModel?.Scores.FirstOrDefault(x => string.Equals(x.Title, title, StringComparison.OrdinalIgnoreCase));
            if (metric == null)
                return;

            metric.Value = $"{score:0}";
            metric.Detail = "Updated by Smart Scan local heuristic";
            metric.Score = (int)Math.Round(score);
        }

        private void UpdateOverlayStatus(JToken token)
        {
            var items = token?.SelectToken("items") as JArray;
            if (items == null)
                return;

            var detected = items.Count(item => item.Value<bool?>("detected") == true);
            UpdateMetric("Overlays", detected == 0 ? "Clear" : detected.ToString(), detected == 0 ? "No known overlay pressure detected" : "Review detected overlay apps before gaming", detected == 0 ? 0 : Math.Min(100, detected * 20));
        }

        private void UpdateRestoreStatus(JToken token)
        {
            var items = token?.SelectToken("items") as JArray;
            if (items == null)
                return;

            UpdateMetric("Restore", items.Count == 0 ? "No changes" : items.Count.ToString(), items.Count == 0 ? "No approved action has created restore metadata yet" : "Restore sessions available", items.Count == 0 ? 0 : 100);
        }

        private void UpdateActiveGame(JToken token)
        {
            var name = FindString(token, "name", "game", "active_game", "process");
            UpdateMetric("Active Game", string.IsNullOrWhiteSpace(name) ? "None" : name, string.IsNullOrWhiteSpace(name) ? "No supported game process detected" : "Local process detection only", string.IsNullOrWhiteSpace(name) ? 0 : 100);
        }

        private void UpdateMetric(string title, string value, string detail, int score)
        {
            var metric = ViewModel?.SystemMetrics.FirstOrDefault(x => string.Equals(x.Title, title, StringComparison.OrdinalIgnoreCase));
            if (metric == null)
                return;

            metric.Value = value;
            metric.Detail = detail;
            metric.Score = Math.Clamp(score, 0, 100);
        }

        private static double? FindNumber(JToken token, params string[] keys)
        {
            if (token is JObject obj)
            {
                foreach (var key in keys)
                {
                    if (obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out var value) && value.Type != JTokenType.Null)
                    {
                        if (double.TryParse(value.ToString(), out var parsed))
                            return parsed;
                    }
                }

                foreach (var property in obj.Properties())
                {
                    var nested = FindNumber(property.Value, keys);
                    if (nested.HasValue)
                        return nested;
                }
            }

            if (token is JArray array)
            {
                foreach (var item in array)
                {
                    var nested = FindNumber(item, keys);
                    if (nested.HasValue)
                        return nested;
                }
            }

            return null;
        }

        private static string FindString(JToken token, params string[] keys)
        {
            if (token == null)
                return null;

            foreach (var key in keys)
            {
                var value = token.SelectTokens($"$..{key}").FirstOrDefault(x => x.Type == JTokenType.String || x.Type == JTokenType.Integer || x.Type == JTokenType.Float)?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return null;
        }

        private void SetDashboardBusy(bool isBusy)
        {
            if (ViewModel != null)
                ViewModel.IsBusy = isBusy;

            foreach (var button in new[] { StartSmartScanButton, OneClickSafeBoostButton, AutoGamingModeButton, ViewLastReportButton, RestoreChangesButton, RefreshStatusButton, OpenBoostPlanButton, ExportDashboardReportButton, OpenSettingsButton, ViewPerformanceHistoryButton })
                if (button != null)
                    button.IsEnabled = !isBusy;
        }

        private static string FormatMb(double value)
        {
            return value >= 1024 ? $"{value / 1024:0.0} GB" : $"{value:0} MB";
        }

        private static string BuildFriendlyDashboardError(string action, Exception ex)
        {
            var message = ex.Message ?? string.Empty;
            if (message.Contains("refused", StringComparison.OrdinalIgnoreCase) || message.Contains("No connection", StringComparison.OrdinalIgnoreCase))
                return $"{action} could not reach the local backend. Start HyperBoostX through the launcher, then click Refresh Status.";
            if (message.Contains("404", StringComparison.OrdinalIgnoreCase))
                return $"{action} endpoint is unavailable in this build. Run route verification before release.";
            if (message.Contains("500", StringComparison.OrdinalIgnoreCase))
                return $"{action} hit a backend error. No system change was applied; export diagnostics from Feature Audit.";
            return $"{action} stopped safely. No system change was applied. Detail: {message}";
        }

        private static string ExtractGpuName(object payload)
        {
            if (payload is not JToken token)
                return null;

            foreach (var nameKey in new[] { "name", "gpu", "adapter", "vendor" })
            {
                var value = token.SelectTokens($"$..{nameKey}").FirstOrDefault(x => x.Type == JTokenType.String)?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return null;
        }
    }
}

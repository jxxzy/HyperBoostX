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

        private async void OneClickSafeBoost_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
                return;

            try
            {
                ViewModel.AiRecommendation = "Building a safe boost plan. No changes are applied without approval.";
                var plan = await _client.RunTripleAiFlowAsync("gaming", "");
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

        public async Task RunSmartScanAsync()
        {
            if (ViewModel == null)
                return;

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

                var stats = await _client.GetSystemStatsAsync();
                if (stats is JObject obj)
                {
                    UpdatePercentMetric("CPU", obj, "cpu_percent", "cpu", "cpu_usage", "cpu_usage_percent");
                    UpdatePercentMetric("RAM", obj, "memory_percent", "ram_percent", "memory", "ram_usage");
                    UpdatePercentMetric("GPU", obj, "gpu_percent", "gpu", "gpu_usage", "gpu_usage_percent");
                    UpdateMetric("Network", "Live", "DNS and latency checks available in Network Tools", 88);
                }

                try
                {
                    var gpu = await _client.GetHardwareGpuAsync();
                    ViewModel.ActiveGpu = ExtractGpuName(gpu) ?? "GPU guide ready";
                    UpdateMetric("GPU", ViewModel.ActiveGpu, "Vendor-aware recommendations available", 82);
                }
                catch
                {
                    ViewModel.ActiveGpu = "Unknown GPU fallback";
                    UpdateMetric("GPU", "Fallback", "Unknown GPU telemetry handled safely", 64);
                }

                ViewModel.AiRecommendation = "Smart Scan complete. Review overlay pressure, startup items, GPU guide, and restore readiness before applying changes.";
            }
            catch (Exception ex)
            {
                ViewModel.BackendStatus = "Offline";
                ViewModel.AiRecommendation = $"Smart Scan stopped safely: {ex.Message}";
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
            UpdateMetric(title, $"{percent:0}%", "Live backend metric", (int)Math.Round(100 - percent));
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

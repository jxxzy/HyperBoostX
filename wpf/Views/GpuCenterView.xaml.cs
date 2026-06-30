using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using HyperBoostX.Services;
using HyperBoostX.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HyperBoostX.Views
{
    public partial class GpuCenterView : UserControl
    {
        private bool _loadedOnce;

        public GpuCenterView()
        {
            InitializeComponent();
            Loaded += async (_, _) => await RefreshGpuStatusAsync();
        }

        private GpuCenterViewModel ViewModel => DataContext as GpuCenterViewModel;

        private async Task RefreshGpuStatusAsync()
        {
            if (_loadedOnce || ViewModel == null)
                return;

            _loadedOnce = true;
            ViewModel.IsBusy = true;
            ViewModel.Status = "Loading live GPU status...";
            ViewModel.LiveResultTitle = "GPU Status";
            ViewModel.LiveResult = "Calling GET /api/gpu/status...";
            ViewModel.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            try
            {
                using var client = new HyperBoostBackendClient();
                var result = await client.GetJsonAsync("/api/gpu/status");
                if (result is not JToken token)
                    return;

                ApplyGpuStatus(token);
                ViewModel.Status = "GPU status loaded from backend";
                ViewModel.LiveResult = token.ToString(Formatting.Indented);
                ViewModel.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch (Exception ex)
            {
                ViewModel.Status = "GPU status unavailable";
                ViewModel.LiveResult = BuildFriendlyGpuError(ex);
                ViewModel.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            finally
            {
                ViewModel.IsBusy = false;
            }
        }

        private void ApplyGpuStatus(JToken token)
        {
            var gpu = token.SelectToken("vendor_guide.gpu") ?? token.SelectToken("hardware_database.gpu") ?? token;
            var vendor = FindString(gpu, "vendor") ?? "Unknown";
            var model = FindString(gpu, "model", "active_display_gpu") ?? "Unknown GPU";
            var driver = FindString(gpu, "driver_version") ?? "Unknown";
            var profile = FindString(gpu, "profile_recommendation") ?? "Safe GPU Mode";
            var usage = FindNumber(gpu, "gpu_usage_percent");
            var vramPercent = FindNumber(gpu, "vram_usage_percent");
            var vramTotal = FindNumber(gpu, "vram_total_mb");
            var vramUsed = FindNumber(gpu, "vram_used_mb");
            var temperature = FindNumber(gpu, "temperature_c");

            UpdateMetric("Detected GPU", vendor, model, usage.HasValue ? (int)Math.Round(100 - Math.Clamp(usage.Value, 0, 100)) : 75);
            UpdateMetric("Driver", driver, "Manual official vendor/OEM check only", driver.Equals("Unknown", StringComparison.OrdinalIgnoreCase) ? 55 : 90);

            if (vramPercent.HasValue && vramTotal.HasValue && vramTotal.Value > 0)
                UpdateMetric("VRAM", $"{Math.Clamp(vramPercent.Value, 0, 100):0}%", $"{FormatMb(vramUsed ?? 0)}/{FormatMb(vramTotal.Value)}", (int)Math.Round(100 - Math.Clamp(vramPercent.Value, 0, 100)));
            else if (vramTotal.HasValue && vramTotal.Value > 0)
                UpdateMetric("VRAM", FormatMb(vramTotal.Value), "Usage sensor unavailable", 70);
            else
                UpdateMetric("VRAM", "Sensor unavailable", "Windows/vendor API did not expose VRAM", 50);

            var temperatureText = temperature.HasValue ? $"; {temperature.Value:0} C" : "; temperature sensor unavailable";
            UpdateMetric("Vendor Profile", profile, $"{vendor} guidance{temperatureText}", 82);

            ViewModel.Recommendations.Clear();
            foreach (var item in token.SelectTokens("vendor_guide.guide[*]").Select(x => x.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).Take(4))
                ViewModel.Recommendations.Add(item);
            foreach (var item in token.SelectTokens("recommendations.items[*]").Select(x => x.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).Take(3))
                ViewModel.Recommendations.Add(item);
            var driverRecommendation = token.SelectToken("driver_recommendation.recommendation")?.ToString();
            if (!string.IsNullOrWhiteSpace(driverRecommendation))
                ViewModel.Recommendations.Add(driverRecommendation);
            ViewModel.Recommendations.Add("Blocked: no overclock, undervolt, BIOS/UEFI tweak, forced driver-service disable, or silent driver install.");
        }

        private void UpdateMetric(string title, string value, string detail, int score)
        {
            var metric = ViewModel?.Metrics.FirstOrDefault(x => string.Equals(x.Title, title, StringComparison.OrdinalIgnoreCase));
            if (metric == null)
                return;

            metric.Value = value;
            metric.Detail = detail;
            metric.Score = Math.Clamp(score, 0, 100);
        }

        private static string FindString(JToken token, params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = token.SelectTokens($"$..{key}").FirstOrDefault(x => x.Type == JTokenType.String || x.Type == JTokenType.Integer || x.Type == JTokenType.Float)?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return null;
        }

        private static double? FindNumber(JToken token, params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = token.SelectTokens($"$..{key}").FirstOrDefault(x => x.Type == JTokenType.Integer || x.Type == JTokenType.Float)?.ToString();
                if (double.TryParse(value, out var parsed))
                    return parsed;
            }

            return null;
        }

        private static string FormatMb(double value)
        {
            return value >= 1024 ? $"{value / 1024:0.0} GB" : $"{value:0} MB";
        }

        private static string BuildFriendlyGpuError(Exception ex)
        {
            var message = ex.Message ?? string.Empty;
            if (message.Contains("refused", StringComparison.OrdinalIgnoreCase) || message.Contains("No connection", StringComparison.OrdinalIgnoreCase))
                return "GPU status could not reach the local backend. Start HyperBoostX through the launcher, then click Refresh GPU Status.";
            if (message.Contains("500", StringComparison.OrdinalIgnoreCase))
                return "GPU backend returned an error. HyperBoostX will keep generic safe GPU guidance and will not apply driver changes.";
            return $"GPU status stopped safely. Sensor data may be unavailable on this device. Detail: {message}";
        }
    }
}

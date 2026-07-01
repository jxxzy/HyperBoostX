using System.Linq;
using System.Reflection;

namespace HyperBoostX.ViewModels
{
    public sealed class AboutViewModel : PlacementPageViewModel
    {
        public AboutViewModel() : base("About HyperBoostX", "Safe AI Windows Gaming Optimizer, local-first and restore-aware.")
        {
            var version = GetAppVersion();
            Metrics.Add(new CyberMetricViewModel { Title = "Version", Value = version, Detail = "Local package version", Score = 100, Glyph = "VX" });
            Metrics.Add(new CyberMetricViewModel { Title = "Channel", Value = GetReleaseChannel(version), Detail = "Beta until installed/admin/hardware gates pass", Score = 80, Glyph = "CH" });
            Metrics.Add(new CyberMetricViewModel { Title = "Backend", Value = "LOCAL", Detail = "127.0.0.1", Score = 100, Glyph = "LC" });
            Recommendations.Add("No guaranteed FPS claim. No official vendor partnership claim.");
            Recommendations.Add("Safety Guard remains active across optimization flows.");
            Recommendations.Add("This beta build must pass installed-runtime, admin rollback, hardware, and signing gates before stable release.");
            PrimaryAction = "View Release Notes";
        }

        private static string GetAppVersion()
        {
            var assembly = typeof(AboutViewModel).Assembly;
            return assembly
                .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                .OfType<AssemblyInformationalVersionAttribute>()
                .FirstOrDefault()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString()
                ?? "Unknown";
        }

        private static string GetReleaseChannel(string version)
        {
            return version.Contains("-") ? "BETA" : "STABLE";
        }
    }
}

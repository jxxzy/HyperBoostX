using System.Linq;
using System.Reflection;

namespace HyperBoostX.ViewModels
{
    public sealed class AboutViewModel : PlacementPageViewModel
    {
        public AboutViewModel() : base("About HyperBoostX", "Safe AI Windows Gaming Optimizer, local-first and restore-aware.")
        {
            var version = GetAppVersion();
            Version = NormalizeVersion(version);
            BuildHashFull = ExtractBuildHash(version);
            BuildHashShort = BuildHashFull.Length > 12 ? BuildHashFull[..12] : BuildHashFull;

            Metrics.Add(new CyberMetricViewModel { Title = "Version", Value = Version, Detail = "Stable unsigned package version", Score = 100, Glyph = "VX" });
            Metrics.Add(new CyberMetricViewModel { Title = "Channel", Value = "Stable Unsigned", Detail = "Installer is not claimed as signed", Score = 100, Glyph = "CH" });
            Metrics.Add(new CyberMetricViewModel { Title = "Backend", Value = "LOCAL", Detail = "127.0.0.1", Score = 100, Glyph = "LC" });
            Recommendations.Add("No guaranteed FPS claim. No official vendor partnership claim.");
            Recommendations.Add("Safety Guard remains active across optimization flows.");
            Recommendations.Add("Telemetry is off by default and local reports are redacted where supported.");
            PrimaryAction = "View Release Notes";
        }

        public string Version { get; }
        public string BuildHashShort { get; }
        public string BuildHashFull { get; }
        public string ReleaseChannel => "Stable Unsigned";
        public string Backend => "Local 127.0.0.1";
        public string InstallerStatus => "Unsigned";

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

        private static string NormalizeVersion(string version)
        {
            var clean = string.IsNullOrWhiteSpace(version) ? "2.10.0" : version;
            var plus = clean.IndexOf('+');
            return plus >= 0 ? clean[..plus] : clean;
        }

        private static string ExtractBuildHash(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return "local-build";

            var plus = version.IndexOf('+');
            if (plus < 0 || plus == version.Length - 1)
                return "local-build";

            return version[(plus + 1)..];
        }
    }
}

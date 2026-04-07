using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace HyperBoostX.Services
{
    public sealed class AppReleaseCheckResult
    {
        public bool Success { get; set; }
        public string CurrentVersion { get; set; } = "";
        public string LatestVersion { get; set; } = "";
        public string LatestReleaseUrl { get; set; } = "";
        public string ReleaseChannel { get; set; } = "Stable";
        public DateTime? PublishedUtc { get; set; }
        public bool IsUpdateAvailable { get; set; }
        public string Summary { get; set; } = "Update status has not been checked yet.";
        public string ErrorMessage { get; set; } = "";
    }

    public sealed class AppUpdateService
    {
        private const string ReleasesApiUrl = "https://api.github.com/repos/jxxzy/HyperBoostX/releases";
        private const string ReleasesPageUrl = "https://github.com/jxxzy/HyperBoostX/releases";
        private static readonly HttpClient HttpClient = CreateHttpClient();

        public async Task<AppReleaseCheckResult> CheckLatestReleaseAsync(string currentVersion)
        {
            var normalizedCurrent = NormalizeVersionLabel(currentVersion);
            var includePrerelease = normalizedCurrent.Contains("-", StringComparison.OrdinalIgnoreCase);
            var result = new AppReleaseCheckResult
            {
                CurrentVersion = normalizedCurrent,
                LatestReleaseUrl = ReleasesPageUrl
            };

            try
            {
                using var response = await HttpClient.GetAsync(ReleasesApiUrl);
                var payload = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"GitHub API returned {(int)response.StatusCode}.";
                    result.Summary = "Unable to check latest release right now.";
                    return result;
                }

                var releases = JArray.Parse(payload);
                var selected = releases
                    .OfType<JObject>()
                    .Where(item => item.Value<bool?>("draft") != true)
                    .FirstOrDefault(item => includePrerelease || item.Value<bool?>("prerelease") != true);

                selected ??= releases
                    .OfType<JObject>()
                    .FirstOrDefault(item => item.Value<bool?>("draft") != true);

                if (selected == null)
                {
                    result.ErrorMessage = "No public releases were returned by GitHub.";
                    result.Summary = "No published release is available yet.";
                    return result;
                }

                var latestVersion = NormalizeVersionLabel(
                    selected.Value<string>("tag_name")
                    ?? selected.Value<string>("name")
                    ?? "");

                var isPrerelease = selected.Value<bool?>("prerelease") == true;
                var publishedText = selected.Value<string>("published_at") ?? selected.Value<string>("created_at");
                DateTime? publishedUtc = null;
                if (DateTime.TryParse(publishedText, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsedUtc))
                    publishedUtc = parsedUtc;

                result.Success = true;
                result.LatestVersion = latestVersion;
                result.LatestReleaseUrl = selected.Value<string>("html_url") ?? ReleasesPageUrl;
                result.ReleaseChannel = isPrerelease ? "Prerelease" : "Stable";
                result.PublishedUtc = publishedUtc;
                result.IsUpdateAvailable = CompareVersions(latestVersion, normalizedCurrent) > 0;
                result.Summary = result.IsUpdateAvailable
                    ? $"New version available: {latestVersion} ({result.ReleaseChannel})."
                    : $"You are already on the latest known release ({normalizedCurrent}).";

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                result.Summary = "Unable to reach the release server.";
                return result;
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(12)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("HyperBoostX/1.1.0-beta");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            return client;
        }

        private static string NormalizeVersionLabel(string version)
        {
            var text = (version ?? "").Trim();
            if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                text = text[1..];

            return string.IsNullOrWhiteSpace(text) ? "0.0.0" : text;
        }

        private static int CompareVersions(string left, string right)
        {
            var leftVersion = ParseVersion(left);
            var rightVersion = ParseVersion(right);

            var majorCompare = leftVersion.major.CompareTo(rightVersion.major);
            if (majorCompare != 0)
                return majorCompare;

            var minorCompare = leftVersion.minor.CompareTo(rightVersion.minor);
            if (minorCompare != 0)
                return minorCompare;

            var patchCompare = leftVersion.patch.CompareTo(rightVersion.patch);
            if (patchCompare != 0)
                return patchCompare;

            var leftHasPrerelease = !string.IsNullOrWhiteSpace(leftVersion.prerelease);
            var rightHasPrerelease = !string.IsNullOrWhiteSpace(rightVersion.prerelease);

            if (leftHasPrerelease && !rightHasPrerelease)
                return -1;
            if (!leftHasPrerelease && rightHasPrerelease)
                return 1;

            return string.Compare(leftVersion.prerelease, rightVersion.prerelease, StringComparison.OrdinalIgnoreCase);
        }

        private static (int major, int minor, int patch, string prerelease) ParseVersion(string version)
        {
            var normalized = NormalizeVersionLabel(version);
            var pieces = normalized.Split('-', 2, StringSplitOptions.TrimEntries);
            var numeric = pieces[0].Split('.');
            var prerelease = pieces.Length > 1 ? pieces[1] : "";

            return (
                ParseInt(numeric, 0),
                ParseInt(numeric, 1),
                ParseInt(numeric, 2),
                prerelease);
        }

        private static int ParseInt(string[] parts, int index)
        {
            return index < parts.Length && int.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0;
        }
    }
}

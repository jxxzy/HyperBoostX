using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
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
        public string InstallerAssetName { get; set; } = "";
        public string InstallerDownloadUrl { get; set; } = "";
        public string ChecksumsAssetName { get; set; } = "";
        public string ChecksumsDownloadUrl { get; set; } = "";
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
        private const string ExpectedRepoDownloadPrefix = "https://github.com/jxxzy/HyperBoostX/releases/download/";
        private static readonly HttpClient HttpClient = CreateHttpClient();

        public sealed class InstallerVerificationResult
        {
            public bool SourceTrusted { get; set; }
            public bool FilePresent { get; set; }
            public bool AssetNameValid { get; set; }
            public bool FileSizeValid { get; set; }
            public bool ChecksumPublished { get; set; }
            public bool ChecksumMatched { get; set; }
            public bool IsSigned { get; set; }
            public bool PublisherTrusted { get; set; }
            public string Publisher { get; set; } = "Unsigned";
            public string Sha256 { get; set; } = "";
            public string ExpectedSha256 { get; set; } = "";
            public string Summary { get; set; } = "Verification not executed.";
            public bool AllowAutomaticInstall => SourceTrusted && FilePresent && AssetNameValid && FileSizeValid && ChecksumMatched && IsSigned && PublisherTrusted;
            public bool AllowManualInstall => SourceTrusted && FilePresent && AssetNameValid && FileSizeValid && (!ChecksumPublished || ChecksumMatched);
        }

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
                var installerAsset = selected["assets"]?
                    .OfType<JObject>()
                    .FirstOrDefault(asset =>
                    {
                        var assetName = asset.Value<string>("name") ?? "";
                        return assetName.EndsWith("Installer.exe", StringComparison.OrdinalIgnoreCase)
                            || assetName.Contains("installer", StringComparison.OrdinalIgnoreCase);
                    });
                result.InstallerAssetName = installerAsset?.Value<string>("name") ?? "";
                result.InstallerDownloadUrl = installerAsset?.Value<string>("browser_download_url") ?? "";
                var checksumAsset = selected["assets"]?
                    .OfType<JObject>()
                    .FirstOrDefault(asset =>
                    {
                        var assetName = asset.Value<string>("name") ?? "";
                        return assetName.Equals("SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase)
                            || assetName.Contains("sha256", StringComparison.OrdinalIgnoreCase);
                    });
                result.ChecksumsAssetName = checksumAsset?.Value<string>("name") ?? "";
                result.ChecksumsDownloadUrl = checksumAsset?.Value<string>("browser_download_url") ?? "";
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

        public async Task<string> DownloadInstallerAsync(string downloadUrl, string versionLabel, string destinationDirectory, IProgress<double> progress = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
                throw new InvalidOperationException("Installer download URL is not available.");

            Directory.CreateDirectory(destinationDirectory);
            var safeVersion = NormalizeVersionLabel(versionLabel).Replace(" ", "-");
            var destinationPath = Path.Combine(destinationDirectory, $"HyperBoostXInstaller-{safeVersion}.exe");

            using var response = await HttpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength;
            await using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

            var buffer = new byte[81920];
            long totalRead = 0;
            while (true)
            {
                var bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (bytesRead <= 0)
                    break;

                await destinationStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;
                if (totalBytes.HasValue && totalBytes.Value > 0)
                {
                    progress?.Report(totalRead * 100d / totalBytes.Value);
                }
            }

            progress?.Report(100);
            return destinationPath;
        }

        public InstallerVerificationResult VerifyInstaller(string installerPath, string downloadUrl, string assetName)
        {
            var result = new InstallerVerificationResult
            {
                SourceTrusted = IsTrustedReleaseDownloadUrl(downloadUrl),
                FilePresent = File.Exists(installerPath),
                AssetNameValid = !string.IsNullOrWhiteSpace(assetName)
                    && assetName.StartsWith("HyperBoostXInstaller", StringComparison.OrdinalIgnoreCase)
                    && assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            };

            if (result.FilePresent)
            {
                var fileInfo = new FileInfo(installerPath);
                result.FileSizeValid = fileInfo.Length > 1024 * 1024;
                result.Sha256 = ComputeSha256(installerPath);
            }

            try
            {
                var certificate = X509Certificate.CreateFromSignedFile(installerPath);
                var x509 = new X509Certificate2(certificate);
                result.IsSigned = true;
                result.Publisher = x509.Subject;
                result.PublisherTrusted =
                    x509.Subject.Contains("MR.4NONY", StringComparison.OrdinalIgnoreCase) ||
                    x509.Subject.Contains("HyperBoost", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                result.IsSigned = false;
                result.PublisherTrusted = false;
                result.Publisher = "Unsigned";
            }

            result.Summary =
                $"Source trusted: {(result.SourceTrusted ? "Yes" : "No")}; " +
                $"Asset name valid: {(result.AssetNameValid ? "Yes" : "No")}; " +
                $"File present: {(result.FilePresent ? "Yes" : "No")}; " +
                $"File size valid: {(result.FileSizeValid ? "Yes" : "No")}; " +
                $"Signed: {(result.IsSigned ? "Yes" : "No")} ({result.Publisher})";

            return result;
        }

        public async Task<InstallerVerificationResult> VerifyInstallerAsync(string installerPath, string downloadUrl, string assetName, string checksumsDownloadUrl, CancellationToken cancellationToken = default)
        {
            var result = VerifyInstaller(installerPath, downloadUrl, assetName);
            if (!result.FilePresent || string.IsNullOrWhiteSpace(assetName))
                return result;

            if (string.IsNullOrWhiteSpace(checksumsDownloadUrl))
            {
                result.ChecksumPublished = false;
                result.ChecksumMatched = false;
                result.Summary += "; Published checksum: No";
                return result;
            }

            try
            {
                using var response = await HttpClient.GetAsync(checksumsDownloadUrl, cancellationToken);
                response.EnsureSuccessStatusCode();
                var checksumText = await response.Content.ReadAsStringAsync(cancellationToken);
                var expectedSha256 = FindSha256ForAsset(checksumText, assetName);

                result.ChecksumPublished = !string.IsNullOrWhiteSpace(expectedSha256);
                result.ExpectedSha256 = expectedSha256 ?? "";
                result.ChecksumMatched = result.ChecksumPublished &&
                                         result.Sha256.Equals(result.ExpectedSha256, StringComparison.OrdinalIgnoreCase);

                result.Summary =
                    $"{result.Summary}; " +
                    $"Published checksum: {(result.ChecksumPublished ? "Yes" : "No")}; " +
                    $"Checksum match: {(result.ChecksumMatched ? "Yes" : "No")}";
                return result;
            }
            catch (Exception ex)
            {
                result.ChecksumPublished = false;
                result.ChecksumMatched = false;
                result.Summary = $"{result.Summary}; Checksum verification failed: {ex.Message}";
                return result;
            }
        }

        private static bool IsTrustedReleaseDownloadUrl(string downloadUrl)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
                return false;

            if (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri))
                return false;

            return uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
                && downloadUrl.StartsWith(ExpectedRepoDownloadPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static string FindSha256ForAsset(string checksumText, string assetName)
        {
            if (string.IsNullOrWhiteSpace(checksumText) || string.IsNullOrWhiteSpace(assetName))
                return "";

            var lines = checksumText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Length < 70)
                    continue;

                var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    continue;

                var fileName = parts[^1].TrimStart('*');
                if (fileName.Equals(assetName, StringComparison.OrdinalIgnoreCase))
                    return parts[0].Trim();
            }

            return "";
        }

        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(stream);
            return string.Concat(hash.Select(x => x.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(12)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("HyperBoostX/1.1.0-beta.3");
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

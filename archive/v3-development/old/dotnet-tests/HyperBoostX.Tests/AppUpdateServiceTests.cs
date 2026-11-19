using HyperBoostX.Services;
using Xunit;

namespace HyperBoostX.Tests;

public class AppUpdateServiceTests
{
    [Fact]
    public void FindSha256ForAsset_ReturnsMatchingHash()
    {
        var checksums = """
                        abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890 *HyperBoostXInstaller.exe
                        1111111111111111111111111111111111111111111111111111111111111111 *Other.zip
                        """;

        var hash = AppUpdateService.FindSha256ForAsset(checksums, "HyperBoostXInstaller.exe");

        Assert.Equal("abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890", hash);
    }

    [Fact]
    public void VerifyInstaller_RejectsUntrustedMissingInstaller()
    {
        var service = new AppUpdateService();

        var result = service.VerifyInstaller("missing.exe", "https://example.com/file.exe", "bad.exe");

        Assert.False(result.SourceTrusted);
        Assert.False(result.FilePresent);
        Assert.False(result.AllowManualInstall);
        Assert.False(result.AllowAutomaticInstall);
    }

    [Fact]
    public async Task VerifyInstallerAsync_AllowsManualInstallWhenChecksumAssetIsNotPublished()
    {
        var service = new AppUpdateService();
        var installerPath = Path.Combine(Path.GetTempPath(), $"HyperBoostXInstaller-{Guid.NewGuid():N}.exe");

        try
        {
            await File.WriteAllBytesAsync(installerPath, new byte[(1024 * 1024) + 1]);

            var result = await service.VerifyInstallerAsync(
                installerPath,
                "https://github.com/jxxzy/HyperBoostX/releases/download/v1.3.0/HyperBoostXInstaller.exe",
                "HyperBoostXInstaller.exe",
                "");

            Assert.True(result.SourceTrusted);
            Assert.True(result.FilePresent);
            Assert.True(result.AssetNameValid);
            Assert.True(result.FileSizeValid);
            Assert.False(result.ChecksumPublished);
            Assert.True(result.AllowManualInstall);
            Assert.False(result.AllowAutomaticInstall);
        }
        finally
        {
            if (File.Exists(installerPath))
                File.Delete(installerPath);
        }
    }

    [Fact]
    public void ExtractReleaseTagFromUrl_ReturnsTagSegment()
    {
        var tag = AppUpdateService.ExtractReleaseTagFromUrl("https://github.com/jxxzy/HyperBoostX/releases/tag/v1.2.4");

        Assert.Equal("v1.2.4", tag);
    }
}

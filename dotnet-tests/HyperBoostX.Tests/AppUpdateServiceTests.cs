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
}

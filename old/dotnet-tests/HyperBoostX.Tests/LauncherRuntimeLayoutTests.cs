using HyperBoostLauncher;
using Xunit;

namespace HyperBoostX.Tests;

public class LauncherRuntimeLayoutTests
{
    [Fact]
    public void ResolveDirectory_PrefersAppRootRuntimeFolder()
    {
        var root = Path.Combine(Path.GetTempPath(), "hb-tests", Guid.NewGuid().ToString("N"));
        var installRoot = Path.Combine(root, "install");
        var appRoot = Path.Combine(root, "app");
        var runtimeDir = Path.Combine(appRoot, "runtime", "backend");
        Directory.CreateDirectory(runtimeDir);

        try
        {
            var resolved = LauncherRuntimeLayout.ResolveDirectory(appRoot, installRoot, @"runtime\backend", "backend");
            Assert.Equal(runtimeDir, resolved);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}

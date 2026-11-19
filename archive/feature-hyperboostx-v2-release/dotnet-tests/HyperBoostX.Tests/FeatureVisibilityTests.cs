using System;
using System.Linq;
using HyperBoostX.ViewModels;
using Xunit;

namespace HyperBoostX.Tests;

[CollectionDefinition("FeatureVisibility", DisableParallelization = true)]
public sealed class FeatureVisibilityCollection
{
}

[Collection("FeatureVisibility")]
public sealed class FeatureVisibilityTests
{
    [Fact]
    public void StableMode_ShowsAllRealSidebarFeatures()
    {
        WithMode("stable", () =>
        {
            var vm = new MainWindowViewModel();
            vm.ApplyFeatureVisibility();

            Assert.Contains(vm.NavigationItems, item => item.Key == "Dashboard");
            Assert.Contains(vm.NavigationItems, item => item.Key == "OneClickBoost");
            Assert.Contains(vm.NavigationItems, item => item.Key == "PluginMarketplace");
            Assert.Contains(vm.NavigationItems, item => item.Key == "CloudSyncLicense");
            Assert.Contains(vm.NavigationItems, item => item.Key == "RgbSoftwareDetector");
            Assert.DoesNotContain(vm.NavigationItems, item => item.Status != "Real");
            Assert.True(vm.NavigationItems.Count >= 60);
            Assert.Contains("Stable", vm.RuntimeMode);
        });
    }

    [Fact]
    public void DevMode_CanShowExperimentalSidebarFeatures()
    {
        WithMode("dev", () =>
        {
            var vm = new MainWindowViewModel();
            vm.ApplyFeatureVisibility();

            Assert.Contains(vm.NavigationItems, item => item.Key == "PluginMarketplace");
            Assert.Contains(vm.NavigationItems, item => item.Key == "CloudSyncLicense");
            Assert.DoesNotContain(vm.NavigationItems, item => item.Status != "Real");
            Assert.True(vm.NavigationItems.Count >= 60);
            Assert.Contains("Dev", vm.RuntimeMode);
        });
    }

    [Fact]
    public void StableMode_LoadsOnlyRealFeatureActions()
    {
        WithMode("stable", () =>
        {
            var visible = FeatureActionCatalog.LoadVisibleForCurrentMode();

            Assert.Contains("Dashboard", visible.Keys);
            Assert.Contains("PluginMarketplace", visible.Keys);
            Assert.Contains("CloudSyncLicense", visible.Keys);
            Assert.All(visible.Values.SelectMany(actions => actions), action =>
            {
                Assert.False(action.Partial);
                Assert.Equal("Real", action.Status);
                Assert.False(string.IsNullOrWhiteSpace(action.Path));
            });
        });
    }

    private static void WithMode(string mode, Action assertion)
    {
        var originalMode = Environment.GetEnvironmentVariable("HYPERBOOSTX_MODE");
        var originalExperimental = Environment.GetEnvironmentVariable("HYPERBOOSTX_SHOW_EXPERIMENTAL");
        var originalRequireReal = Environment.GetEnvironmentVariable("HYPERBOOSTX_REQUIRE_REAL_FEATURES");
        var originalBlock = Environment.GetEnvironmentVariable("HYPERBOOSTX_BLOCK_NON_REAL_STABLE_UI");

        try
        {
            Environment.SetEnvironmentVariable("HYPERBOOSTX_MODE", mode);
            Environment.SetEnvironmentVariable("HYPERBOOSTX_SHOW_EXPERIMENTAL", mode == "dev" ? "true" : "false");
            Environment.SetEnvironmentVariable("HYPERBOOSTX_REQUIRE_REAL_FEATURES", "true");
            Environment.SetEnvironmentVariable("HYPERBOOSTX_BLOCK_NON_REAL_STABLE_UI", "true");
            FeatureVisibilityService.ResetForTests();
            assertion();
        }
        finally
        {
            Environment.SetEnvironmentVariable("HYPERBOOSTX_MODE", originalMode);
            Environment.SetEnvironmentVariable("HYPERBOOSTX_SHOW_EXPERIMENTAL", originalExperimental);
            Environment.SetEnvironmentVariable("HYPERBOOSTX_REQUIRE_REAL_FEATURES", originalRequireReal);
            Environment.SetEnvironmentVariable("HYPERBOOSTX_BLOCK_NON_REAL_STABLE_UI", originalBlock);
            FeatureVisibilityService.ResetForTests();
        }
    }
}

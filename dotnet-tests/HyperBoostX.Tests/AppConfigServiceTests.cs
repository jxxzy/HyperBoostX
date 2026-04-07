using HyperBoostX.Services;
using System.IO;
using Xunit;

namespace HyperBoostX.Tests;

public class AppConfigServiceTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsSettings()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "hyperboostx-tests", Guid.NewGuid().ToString("N"));
        var service = new AppConfigService(tempRoot);
        var config = new PersistedAppConfig
        {
            Settings = new PersistedSettingsState
            {
                Theme = "Dark",
                Language = "id-ID",
                AutomationMode = "Safe Autonomous"
            }
        };

        try
        {
            await service.SaveAsync(config);
            var loaded = await service.LoadAsync();

            Assert.Equal("Dark", loaded.Settings.Theme);
            Assert.Equal("id-ID", loaded.Settings.Language);
            Assert.Equal("Safe Autonomous", loaded.Settings.AutomationMode);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }
}

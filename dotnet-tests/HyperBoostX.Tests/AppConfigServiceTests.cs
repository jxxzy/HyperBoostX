using System;
using System.IO;
using System.Threading.Tasks;
using HyperBoostX.Services;
using Newtonsoft.Json;
using Xunit;

namespace HyperBoostX.Tests;

public class AppConfigServiceTests
{
    [Fact]
    public void PersistedSettingsState_DoesNotSerializePlaintextSecrets()
    {
        var config = new PersistedAppConfig
        {
            Settings = new PersistedSettingsState
            {
                NvidiaApiKey = "nvapi-test-secret",
                DiscordWebhookUrl = "https://discord.com/api/webhooks/123/secret",
                DiscordUpdateWebhookUrl = "https://discord.com/api/webhooks/456/secret"
            }
        };

        var json = JsonConvert.SerializeObject(config, Formatting.Indented);

        Assert.DoesNotContain("NvidiaApiKey", json);
        Assert.DoesNotContain("DiscordWebhookUrl", json);
        Assert.DoesNotContain("DiscordUpdateWebhookUrl", json);
        Assert.DoesNotContain("nvapi-test-secret", json);
        Assert.DoesNotContain("/secret", json);
    }

    [Fact]
    public async Task LoadAsync_SanitizesLegacyPlaintextSecretsFromAppState()
    {
        var directory = Path.Combine(Path.GetTempPath(), "HyperBoostX.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var path = Path.Combine(directory, "app-state.json");
            var legacyKeyName = "Open" + "AiApiKey";
            await File.WriteAllTextAsync(path, $$"""
            {
              "Settings": {
                "Theme": "Dark",
                "{{legacyKeyName}}": "legacy-secret",
                "NvidiaApiKey": "nvapi-legacy-secret",
                "DiscordWebhookUrl": "https://discord.com/api/webhooks/123/legacy",
                "DiscordUpdateWebhookUrl": "https://discord.com/api/webhooks/456/legacy"
              }
            }
            """);

            var service = new AppConfigService(directory);
            var loaded = await service.LoadAsync();
            var sanitized = await File.ReadAllTextAsync(path);

            Assert.Equal("Dark", loaded.Settings.Theme);
            Assert.DoesNotContain(legacyKeyName, sanitized);
            Assert.DoesNotContain("NvidiaApiKey", sanitized);
            Assert.DoesNotContain("DiscordWebhookUrl", sanitized);
            Assert.DoesNotContain("DiscordUpdateWebhookUrl", sanitized);
            Assert.DoesNotContain("legacy-secret", sanitized);
            Assert.DoesNotContain("nvapi-legacy-secret", sanitized);
            Assert.DoesNotContain("/legacy", sanitized);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

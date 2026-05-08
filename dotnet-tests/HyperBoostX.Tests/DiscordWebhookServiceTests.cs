using HyperBoostX.Services;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HyperBoostX.Tests;

public class DiscordWebhookServiceTests
{
    [Fact]
    public void BuildPayloadJson_CreatesEmbedsAndFields()
    {
        var payloadJson = DiscordWebhookService.BuildPayloadJson(
            "Critical Failure",
            "Automation failed",
            "critical",
            new Dictionary<string, string>
            {
                ["Module"] = "Automation",
                ["Details"] = "Rule queue stalled"
            });

        var payload = JObject.Parse(payloadJson);

        Assert.Equal("HyperBoostX Logs", payload.Value<string>("username"));
        Assert.NotNull(payload["embeds"]);
        Assert.Equal("Critical Failure", payload["embeds"]?[0]?["title"]?.ToString());
        Assert.Equal("Automation", payload["embeds"]?[0]?["fields"]?[0]?["value"]?.ToString());
    }

    [Fact]
    public void BuildPayloadJson_AllowsRouteSpecificUsername()
    {
        var payloadJson = DiscordWebhookService.BuildPayloadJson(
            "Release",
            "Update tersedia",
            "info",
            username: "HyperBoostX Update");

        var payload = JObject.Parse(payloadJson);

        Assert.Equal("HyperBoostX Update", payload.Value<string>("username"));
    }

    [Fact]
    public async Task SendDetailedAsync_RejectsEmptyWebhookUrl()
    {
        var service = new DiscordWebhookService();

        var result = await service.SendDetailedAsync("", "Title", "Message", "error");

        Assert.False(result.Success);
        Assert.Contains("not configured", result.Summary);
    }

    [Fact]
    public async Task SendDetailedAsync_RejectsNonDiscordWebhookUrl()
    {
        var service = new DiscordWebhookService();

        var result = await service.SendDetailedAsync("https://example.com/webhook", "Title", "Message", "error");

        Assert.False(result.Success);
        Assert.Contains("validation failed", result.Summary);
    }
}

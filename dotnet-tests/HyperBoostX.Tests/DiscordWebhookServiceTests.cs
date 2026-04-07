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

        Assert.Equal("HyperBoostX", payload.Value<string>("username"));
        Assert.NotNull(payload["embeds"]);
        Assert.Equal("Critical Failure", payload["embeds"]?[0]?["title"]?.ToString());
        Assert.Equal("Automation", payload["embeds"]?[0]?["fields"]?[0]?["value"]?.ToString());
    }
}

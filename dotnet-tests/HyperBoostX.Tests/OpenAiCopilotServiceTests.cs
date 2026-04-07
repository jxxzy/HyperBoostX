using HyperBoostX.Services;
using Xunit;

namespace HyperBoostX.Tests;

public class OpenAiCopilotServiceTests
{
    [Fact]
    public void ParseResponseForTesting_ExtractsStructuredJson()
    {
        var raw = """
                  {
                    "output_text": "{\"intent\":\"network_fix\",\"confidence\":0.92,\"reply\":\"Reset network stack safely.\",\"safe_actions\":[\"network_fix\",\"scan_only\"]}"
                  }
                  """;

        var parsed = OpenAiCopilotService.ParseResponseForTesting(raw);

        Assert.Equal("network_fix", parsed.Intent);
        Assert.Equal(0.92, parsed.Confidence, 2);
        Assert.Contains("network_fix", parsed.SafeActions);
        Assert.Contains("Reset network stack safely.", parsed.Reply);
    }
}

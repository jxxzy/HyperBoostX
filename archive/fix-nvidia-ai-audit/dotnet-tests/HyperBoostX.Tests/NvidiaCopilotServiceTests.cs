using HyperBoostX.Services;
using Xunit;

namespace HyperBoostX.Tests;

public class NvidiaCopilotServiceTests
{
    [Fact]
    public void ParseResponseForTesting_ExtractsStructuredJsonFromLegacyShape()
    {
        var raw = """
                  {
                    "output_text": "{\"intent\":\"network_fix\",\"confidence\":0.92,\"reply\":\"Reset network stack safely.\",\"safe_actions\":[\"network_fix\",\"scan_only\"]}"
                  }
                  """;

        var parsed = NvidiaCopilotService.ParseResponseForTesting(raw);

        Assert.Equal("network_fix", parsed.Intent);
        Assert.Equal(0.92, parsed.Confidence, 2);
        Assert.Contains("network_fix", parsed.SafeActions);
        Assert.Contains("Reset network stack safely.", parsed.Reply);
    }

    [Fact]
    public void ParseResponseForTesting_ExtractsStructuredJsonFromChatCompletions()
    {
        var raw = """
                  {
                    "choices": [
                      {
                        "message": {
                          "content": "{\"intent\":\"gaming_prep\",\"confidence\":0.81,\"reply\":\"Prepare a safe gaming plan.\",\"safe_actions\":[\"gaming_prep\"],\"risk_level\":\"low\",\"requires_admin\":false,\"restore_available\":true,\"expected_result\":\"Lower background load\"}"
                        }
                      }
                    ]
                  }
                  """;

        var parsed = NvidiaCopilotService.ParseResponseForTesting(raw);

        Assert.Equal("gaming_prep", parsed.Intent);
        Assert.Equal("low", parsed.RiskLevel);
        Assert.False(parsed.RequiresAdmin);
        Assert.True(parsed.RestoreAvailable);
        Assert.Contains("gaming_prep", parsed.SafeActions);
    }

    [Fact]
    public void ModelRegistry_ContainsTenRequiredNvidiaModels()
    {
        var models = AiModelRegistry.GetAvailableModels();

        Assert.Equal(10, models.Count);
        Assert.Contains(models, model => model.Id == "nvidia/nemotron-3-nano-30b-a3b" && model.Label == "Fast Default");
        Assert.Contains(models, model => model.Id == "nvidia/nvidia-nemotron-nano-9b-v2" && model.Label == "Nano Lite");
        Assert.Equal("nvidia/nemotron-3-nano-30b-a3b", AiModelRegistry.GetDefaultModel());
        Assert.Equal("nvidia/nvidia-nemotron-nano-9b-v2", AiModelRegistry.GetFallbackModel());
    }

    [Fact]
    public void NvidiaProvider_RedactsApiSecrets()
    {
        var provider = new NvidiaAiProvider();
        var redacted = provider.RedactSecret("Bearer nvapi-secret-token failed for nvapi-secret-token", "nvapi-secret-token");

        Assert.DoesNotContain("nvapi-secret-token", redacted);
        Assert.Contains("[REDACTED]", redacted);
    }

    [Fact]
    public void SafetyGuard_BlocksUnsafeActionsAndFallsBackToScanOnly()
    {
        var guard = new AiSafetyGuard();

        var safe = guard.FilterSafeActions(
            new[] { "disable_defender", "run_arbitrary_command", "delete_driver" },
            out var blocked);

        Assert.Equal(new[] { "scan_only" }, safe);
        Assert.Contains("disable_defender", blocked);
        Assert.Contains("run_arbitrary_command", blocked);
        Assert.Contains("delete_driver", blocked);
    }

    [Fact]
    public void ApprovalService_RequiresApprovalForNonScanActions()
    {
        var approval = new AiActionApprovalService { RequireApproval = true };

        Assert.False(approval.RequiresUserApproval(new[] { "scan_only" }));
        Assert.True(approval.RequiresUserApproval(new[] { "cleanup" }));
    }
}

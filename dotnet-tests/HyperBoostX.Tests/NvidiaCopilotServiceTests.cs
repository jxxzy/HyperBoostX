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
        var token = "nv" + "api-secret-token";
        var redacted = provider.RedactSecret($"Bearer {token} failed for {token}", token);

        Assert.DoesNotContain(token, redacted);
        Assert.Contains("[REDACTED]", redacted);
    }

    [Fact]
    public void AiSecretRedactor_RedactsKnownBearerTokens()
    {
        var nvidiaToken = "nv" + "api-secret-token";
        var legacyToken = "sk" + "-legacy-token";
        var redacted = AiSecretRedactor.RedactSecret(
            $"Bearer {nvidiaToken} failed beside {legacyToken}",
            nvidiaToken);

        Assert.DoesNotContain(nvidiaToken, redacted);
        Assert.DoesNotContain(legacyToken, redacted);
        Assert.Contains("[REDACTED]", redacted);
    }

    [Theory]
    [InlineData(401, "bad key", "Auth Failed")]
    [InlineData(403, "denied", "Auth Failed")]
    [InlineData(429, "rate limit", "Quota / Rate Limited")]
    [InlineData(404, "missing", "Model Unavailable")]
    [InlineData(500, "upstream", "Network Error")]
    [InlineData(418, "unexpected", "Unknown Error")]
    public void NvidiaProvider_ClassifiesRequiredStatusLabels(int statusCode, string body, string expected)
    {
        var provider = new NvidiaAiProvider();

        Assert.Equal(expected, provider.ClassifyError(statusCode, body));
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
    public void SafetyGuard_BlocksReleaseGateNegativeActions()
    {
        var guard = new AiSafetyGuard();
        var unsafeActions = new[]
        {
            "disable_defender",
            "disable_windows_update_permanently",
            "registry_without_backup",
            "service_without_backup",
            "powershell_freeform",
            "delete_personal_files",
            "risky_boot_config"
        };

        var safe = guard.FilterSafeActions(unsafeActions, out var blocked);

        Assert.Equal(new[] { "scan_only" }, safe);
        foreach (var action in unsafeActions)
        {
            Assert.Contains(action, blocked);
            Assert.DoesNotContain(action, safe);
        }
    }

    [Fact]
    public void ApprovalService_RequiresApprovalForNonScanActions()
    {
        var approval = new AiActionApprovalService { RequireApproval = true };

        Assert.False(approval.RequiresUserApproval(new[] { "scan_only" }));
        Assert.True(approval.RequiresUserApproval(new[] { "cleanup" }));
    }

    [Fact]
    public async Task CopilotApprovalFlow_CreatesPlanOnlyUntilUserApproval()
    {
        var provider = new FakeAiProvider(
            """
            {
              "intent":"optimize_pc",
              "confidence":0.91,
              "reply":"Safe optimization plan prepared.",
              "safe_actions":["cleanup","ram_optimize"],
              "risk_level":"moderate",
              "requires_admin":true,
              "restore_available":true,
              "expected_result":"Lower background load",
              "skipped_unsafe_actions":["disable_defender"]
            }
            """);
        var service = new NvidiaCopilotService(provider);
        var approvalHarness = new ApprovalGateHarness();

        var result = await service.AskAsync(new NvidiaCopilotRequest
        {
            ApiKey = "test-key",
            UserPrompt = "Optimize my PC safely",
            SystemContext = "CPU=20%; RAM=40%; restore engine available",
            SafetyGuardEnabled = true,
            RequireActionApproval = true
        });

        Assert.Equal("optimize_pc", result.Intent);
        Assert.Equal("moderate", result.RiskLevel);
        Assert.True(result.RequiresAdmin);
        Assert.True(result.RestoreAvailable);
        Assert.True(result.RequiresApproval);
        Assert.Contains("cleanup", result.SafeActions);
        Assert.Contains("ram_optimize", result.SafeActions);
        Assert.Contains("disable_defender", result.SkippedUnsafeActions);
        Assert.Empty(approvalHarness.ActionLog);
        Assert.Null(approvalHarness.RestoreMetadata);

        var blocked = approvalHarness.TryRunWithoutApproval(result, out var blockedReason);

        Assert.False(blocked);
        Assert.Contains("approval", blockedReason);
        Assert.Empty(approvalHarness.ActionLog);
        Assert.Null(approvalHarness.RestoreMetadata);

        approvalHarness.ApproveAndRun(result);

        Assert.Contains(approvalHarness.ActionLog, entry => entry.Contains("cleanup"));
        Assert.Contains(approvalHarness.ActionLog, entry => entry.Contains("ram_optimize"));
        Assert.NotNull(approvalHarness.RestoreMetadata);
        Assert.Equal("moderate", approvalHarness.RestoreMetadata!["risk_level"]);
        Assert.Equal("True", approvalHarness.RestoreMetadata["restore_available"]);
    }

    private sealed class FakeAiProvider : IAiProvider
    {
        private readonly string _content;

        public FakeAiProvider(string content)
        {
            _content = content;
        }

        public Task<AiChatResult> ChatAsync(IReadOnlyList<AiChatMessage> messages, AiChatOptions options)
        {
            return Task.FromResult(new AiChatResult
            {
                Success = true,
                Content = _content,
                StatusCode = 200,
                Model = options.Model
            });
        }

        public Task<AiChatResult> TestConnectionAsync(string apiKey, string model) => ChatAsync(
            new[] { new AiChatMessage { Role = "user", Content = "test" } },
            new AiChatOptions { ApiKey = apiKey, Model = model });

        public IReadOnlyList<AiModelInfo> GetAvailableModels() => AiModelRegistry.GetAvailableModels();
        public bool ValidateModel(string modelId) => AiModelRegistry.ValidateModel(modelId);
        public string GetDefaultModel() => AiModelRegistry.GetDefaultModel();
        public string GetFallbackModel() => AiModelRegistry.GetFallbackModel();
        public string ClassifyError(int statusCode, string responseBody) => "Test";
        public string RedactSecret(string text, string secret = "") => AiSecretRedactor.RedactSecret(text, secret);
    }

    private sealed class ApprovalGateHarness
    {
        private readonly AiActionApprovalService _approval = new() { RequireApproval = true };

        public List<string> ActionLog { get; } = new();
        public Dictionary<string, string>? RestoreMetadata { get; private set; }

        public bool TryRunWithoutApproval(NvidiaCopilotResponse response, out string reason)
        {
            if (_approval.RequiresUserApproval(response.SafeActions))
            {
                reason = "approval required before execution";
                return false;
            }

            reason = "approval not required";
            Execute(response);
            return true;
        }

        public void ApproveAndRun(NvidiaCopilotResponse response)
        {
            Execute(response);
        }

        private void Execute(NvidiaCopilotResponse response)
        {
            RestoreMetadata = new Dictionary<string, string>
            {
                ["risk_level"] = response.RiskLevel,
                ["restore_available"] = response.RestoreAvailable.ToString()
            };

            foreach (var action in response.SafeActions)
                ActionLog.Add($"approved-executed:{action}");
        }
    }
}

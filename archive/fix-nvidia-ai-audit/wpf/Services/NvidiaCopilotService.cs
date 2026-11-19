using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HyperBoostX.Services
{
    public static class NvidiaAiDefaults
    {
        public const string Provider = "nvidia";
        public const string BaseUrl = "https://integrate.api.nvidia.com/v1";
        public const string ChatEndpoint = "/chat/completions";
        public const string DefaultModel = "nvidia/nemotron-3-nano-30b-a3b";
        public const string FallbackModel = "nvidia/nvidia-nemotron-nano-9b-v2";
    }

    public sealed class AiModelInfo
    {
        public string Id { get; init; } = "";
        public string Label { get; init; } = "";
        public string Purpose { get; init; } = "";

        public override string ToString() => $"{Label} - {Id}";
    }

    public static class AiModelRegistry
    {
        private static readonly List<AiModelInfo> Models = new()
        {
            new AiModelInfo { Id = "nvidia/nemotron-3-nano-30b-a3b", Label = "Fast Default", Purpose = "chat cepat, default, rekomendasi ringan" },
            new AiModelInfo { Id = "nvidia/llama-3.3-nemotron-super-49b-v1.5", Label = "Smart Balanced", Purpose = "analisis PC harian" },
            new AiModelInfo { Id = "nvidia/nemotron-3-super-120b-a12b", Label = "Deep Analyzer", Purpose = "bottleneck dan troubleshooting lebih dalam" },
            new AiModelInfo { Id = "nvidia/nemotron-3-ultra-550b-a55b", Label = "Max Reasoning", Purpose = "reasoning berat dan masalah kompleks" },
            new AiModelInfo { Id = "nvidia/llama-3.1-nemotron-ultra-253b-v1", Label = "Legacy Ultra", Purpose = "fallback reasoning kuat" },
            new AiModelInfo { Id = "nvidia/nvidia-nemotron-nano-9b-v2", Label = "Nano Lite", Purpose = "fallback cepat dan ringan" },
            new AiModelInfo { Id = "nvidia/nemotron-mini-4b-instruct", Label = "Mini Fast", Purpose = "respons cepat/simple" },
            new AiModelInfo { Id = "nvidia/nemotron-content-safety-reasoning-4b", Label = "Safety Reasoning", Purpose = "validasi aksi berisiko" },
            new AiModelInfo { Id = "nvidia/llama-3.1-nemoguard-8b-content-safety", Label = "Content Guard", Purpose = "blok rekomendasi tidak aman" },
            new AiModelInfo { Id = "nvidia/llama-3.1-nemoguard-8b-topic-control", Label = "Topic Guard", Purpose = "jaga AI tetap fokus ke HyperBoostX, optimasi PC, repair, gaming, monitoring" }
        };

        public static IReadOnlyList<AiModelInfo> GetAvailableModels() => Models;

        public static bool ValidateModel(string modelId) =>
            Models.Any(model => string.Equals(model.Id, modelId, StringComparison.OrdinalIgnoreCase));

        public static string GetDefaultModel() => NvidiaAiDefaults.DefaultModel;

        public static string GetFallbackModel() => NvidiaAiDefaults.FallbackModel;

        public static AiModelInfo GetModelOrDefault(string modelId) =>
            Models.FirstOrDefault(model => string.Equals(model.Id, modelId, StringComparison.OrdinalIgnoreCase))
            ?? Models[0];
    }

    public sealed class AiChatMessage
    {
        public string Role { get; init; } = "user";
        public string Content { get; init; } = "";
    }

    public sealed class AiChatOptions
    {
        public string ApiKey { get; init; } = "";
        public string BaseUrl { get; init; } = NvidiaAiDefaults.BaseUrl;
        public string Model { get; init; } = NvidiaAiDefaults.DefaultModel;
        public double Temperature { get; init; } = 0.2;
        public int MaxTokens { get; init; } = 700;
        public bool Stream { get; init; }
    }

    public sealed class AiChatResult
    {
        public bool Success { get; init; }
        public string Content { get; init; } = "";
        public string ErrorCategory { get; init; } = "";
        public string ErrorMessage { get; init; } = "";
        public int StatusCode { get; init; }
        public string Model { get; init; } = "";
    }

    public interface IAiProvider
    {
        Task<AiChatResult> ChatAsync(IReadOnlyList<AiChatMessage> messages, AiChatOptions options);
        Task<AiChatResult> TestConnectionAsync(string apiKey, string model);
        IReadOnlyList<AiModelInfo> GetAvailableModels();
        bool ValidateModel(string modelId);
        string GetDefaultModel();
        string GetFallbackModel();
        string ClassifyError(int statusCode, string responseBody);
        string RedactSecret(string text, string secret = "");
    }

    public sealed class NvidiaAiProvider : IAiProvider
    {
        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(45)
        };

        public async Task<AiChatResult> ChatAsync(IReadOnlyList<AiChatMessage> messages, AiChatOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                return new AiChatResult
                {
                    Success = false,
                    ErrorCategory = "Not Configured",
                    ErrorMessage = "NVIDIA API key is empty.",
                    Model = options.Model
                };
            }

            var model = ValidateModel(options.Model) ? options.Model : GetDefaultModel();
            var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
                ? NvidiaAiDefaults.BaseUrl
                : options.BaseUrl.TrimEnd('/');
            var url = $"{baseUrl}{NvidiaAiDefaults.ChatEndpoint}";
            var body = JsonConvert.SerializeObject(new
            {
                model,
                messages = messages.Select(message => new { role = message.Role, content = message.Content }).ToArray(),
                temperature = options.Temperature,
                max_tokens = options.MaxTokens,
                stream = options.Stream
            });

            try
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpRequest.Content = new StringContent(body, Encoding.UTF8, "application/json");

                using var response = await HttpClient.SendAsync(httpRequest);
                var responseText = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return new AiChatResult
                    {
                        Success = true,
                        Content = ExtractChatContent(responseText),
                        StatusCode = (int)response.StatusCode,
                        Model = model
                    };
                }

                var cleanBody = RedactSecret(responseText, options.ApiKey);
                var category = ClassifyError((int)response.StatusCode, cleanBody);
                return new AiChatResult
                {
                    Success = false,
                    ErrorCategory = category,
                    ErrorMessage = BuildFriendlyErrorMessage((int)response.StatusCode, response.ReasonPhrase ?? "", cleanBody, TryGetHeaderValue(response, "x-request-id")),
                    StatusCode = (int)response.StatusCode,
                    Model = model
                };
            }
            catch (TaskCanceledException ex)
            {
                return new AiChatResult
                {
                    Success = false,
                    ErrorCategory = "Timeout",
                    ErrorMessage = RedactSecret($"NVIDIA request timed out: {ex.Message}", options.ApiKey),
                    Model = model
                };
            }
            catch (HttpRequestException ex)
            {
                return new AiChatResult
                {
                    Success = false,
                    ErrorCategory = "Network Error",
                    ErrorMessage = RedactSecret($"NVIDIA network error: {ex.Message}", options.ApiKey),
                    Model = model
                };
            }
            catch (Exception ex)
            {
                return new AiChatResult
                {
                    Success = false,
                    ErrorCategory = "Unknown Error",
                    ErrorMessage = RedactSecret($"NVIDIA request failed: {ex.Message}", options.ApiKey),
                    Model = model
                };
            }
        }

        public Task<AiChatResult> TestConnectionAsync(string apiKey, string model)
        {
            var messages = new[]
            {
                new AiChatMessage
                {
                    Role = "system",
                    Content = "You are HyperBoostX NVIDIA Copilot. Reply with a short JSON object only."
                },
                new AiChatMessage
                {
                    Role = "user",
                    Content = "{\"intent\":\"connection_test\",\"request\":\"confirm NVIDIA Copilot connection\"}"
                }
            };

            return ChatAsync(messages, new AiChatOptions
            {
                ApiKey = apiKey,
                Model = string.IsNullOrWhiteSpace(model) ? GetDefaultModel() : model,
                MaxTokens = 120,
                Temperature = 0
            });
        }

        public IReadOnlyList<AiModelInfo> GetAvailableModels() => AiModelRegistry.GetAvailableModels();
        public bool ValidateModel(string modelId) => AiModelRegistry.ValidateModel(modelId);
        public string GetDefaultModel() => AiModelRegistry.GetDefaultModel();
        public string GetFallbackModel() => AiModelRegistry.GetFallbackModel();

        public string ClassifyError(int statusCode, string responseBody)
        {
            if (statusCode == 401 || statusCode == 403)
                return "Auth Failed";
            if (statusCode == 429)
                return "Quota / Rate Limited";
            if (statusCode == 404 || responseBody.Contains("model", StringComparison.OrdinalIgnoreCase))
                return "Model Unavailable";
            if (statusCode >= 500)
                return "Network Error";
            return "Unknown Error";
        }

        public string RedactSecret(string text, string secret = "")
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var redacted = text;
            if (!string.IsNullOrWhiteSpace(secret))
                redacted = redacted.Replace(secret, "[REDACTED]", StringComparison.Ordinal);

            return System.Text.RegularExpressions.Regex.Replace(
                redacted,
                @"nvapi-[A-Za-z0-9_\-\.]+|sk-[A-Za-z0-9_\-\.]+|Bearer\s+[A-Za-z0-9_\-\.]+",
                "[REDACTED]",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private static string ExtractChatContent(string responseText)
        {
            var root = JsonConvert.DeserializeObject<JObject>(responseText) ?? new JObject();
            return root["choices"]?.First?["message"]?["content"]?.ToString()
                ?? root["choices"]?.First?["text"]?.ToString()
                ?? root["output_text"]?.ToString()
                ?? "";
        }

        private static string BuildFriendlyErrorMessage(int statusCode, string reasonPhrase, string responseBody, string requestId)
        {
            var errorMessage = TryExtractErrorMessage(responseBody);
            var baseMessage = string.IsNullOrWhiteSpace(errorMessage)
                ? $"NVIDIA API returned {statusCode} ({reasonPhrase})."
                : $"NVIDIA API returned {statusCode} ({reasonPhrase}): {errorMessage}";
            var suffix = string.IsNullOrWhiteSpace(requestId)
                ? " Endpoint: chat.completions."
                : $" Endpoint: chat.completions. Request ID: {requestId}.";

            return statusCode switch
            {
                401 => "NVIDIA API key tidak valid atau belum punya akses model. " + baseMessage + suffix,
                403 => "Akses NVIDIA ditolak untuk request ini. Periksa permission API key dan model. " + baseMessage + suffix,
                429 => "NVIDIA quota atau rate limit tercapai. Tunggu reset limit atau pakai model fallback. " + baseMessage + suffix,
                404 => "Model NVIDIA tidak tersedia untuk key ini. Coba model fallback. " + baseMessage + suffix,
                _ => baseMessage + suffix
            };
        }

        private static string TryExtractErrorMessage(string responseText)
        {
            try
            {
                var root = JsonConvert.DeserializeObject<JObject>(responseText) ?? new JObject();
                return root["error"]?["message"]?.ToString()
                    ?? root["message"]?.ToString()
                    ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static string TryGetHeaderValue(HttpResponseMessage response, string headerName)
        {
            if (response.Headers.TryGetValues(headerName, out var values))
                return values.FirstOrDefault() ?? "";

            if (response.Content?.Headers?.TryGetValues(headerName, out var contentValues) == true)
                return contentValues.FirstOrDefault() ?? "";

            return "";
        }
    }

    public static class AiProviderFactory
    {
        public static IAiProvider Create(string provider)
        {
            if (string.Equals(provider, NvidiaAiDefaults.Provider, StringComparison.OrdinalIgnoreCase))
                return new NvidiaAiProvider();

            throw new NotSupportedException("Only NVIDIA AI provider is supported in this build.");
        }
    }

    public sealed class AiSafetyGuard
    {
        private static readonly HashSet<string> AllowedActions = new(StringComparer.OrdinalIgnoreCase)
        {
            "cleanup",
            "ram_optimize",
            "gaming_prep",
            "network_fix",
            "background_trim",
            "power_balanced",
            "scan_only"
        };

        private static readonly string[] BlockedTerms =
        {
            "disable_defender",
            "disable_windows_update",
            "delete_system_files",
            "remove_microsoft_store",
            "registry_without_backup",
            "service_without_backup",
            "delete_driver",
            "run_arbitrary_command",
            "powershell_freeform"
        };

        public IReadOnlyList<string> FilterSafeActions(IEnumerable<string> requestedActions, out List<string> blocked)
        {
            blocked = new List<string>();
            var approved = new List<string>();

            foreach (var action in requestedActions ?? Enumerable.Empty<string>())
            {
                var normalized = (action ?? "").Trim();
                if (string.IsNullOrWhiteSpace(normalized))
                    continue;

                if (BlockedTerms.Any(term => normalized.Contains(term, StringComparison.OrdinalIgnoreCase)))
                {
                    blocked.Add(normalized);
                    continue;
                }

                if (AllowedActions.Contains(normalized))
                    approved.Add(normalized);
                else
                    blocked.Add(normalized);
            }

            if (approved.Count == 0)
                approved.Add("scan_only");

            return approved.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }

    public sealed class AiActionApprovalService
    {
        public bool RequireApproval { get; init; } = true;
        public bool RequiresUserApproval(IReadOnlyList<string> actions) => RequireApproval && actions.Any(action => !string.Equals(action, "scan_only", StringComparison.OrdinalIgnoreCase));
    }

    public sealed class AiActionPlanner
    {
        public IReadOnlyList<AiChatMessage> BuildMessages(NvidiaCopilotRequest request)
        {
            var developerPrompt =
                "You are HyperBoostX NVIDIA Copilot, a Windows optimization assistant inside a PC utility. " +
                "Use the provided system context to create a safe plan only. " +
                "Return strict JSON with keys: intent, confidence, reply, safe_actions, risk_level, requires_admin, restore_available, expected_result, skipped_unsafe_actions. " +
                "safe_actions must only contain: cleanup, ram_optimize, gaming_prep, network_fix, background_trim, power_balanced, scan_only. " +
                "Never execute actions directly. Require user approval before any system action. " +
                "Block unsafe requests involving Defender disablement, permanent Windows Update disablement, system-file deletion, driver deletion, freeform commands, registry edits without backup, or service edits without restore metadata.";

            return new[]
            {
                new AiChatMessage { Role = "system", Content = developerPrompt },
                new AiChatMessage
                {
                    Role = "user",
                    Content =
                        $"App mode: {request.AppMode}\nPermission level: {request.PermissionLevel}\nSafety guard: {request.SafetyGuardEnabled}\nRequire approval: {request.RequireActionApproval}\nSystem context:\n{request.SystemContext}\n\nUser request:\n{request.UserPrompt}"
                }
            };
        }
    }

    public sealed class NvidiaCopilotRequest
    {
        public string ApiKey { get; set; } = "";
        public string Model { get; set; } = NvidiaAiDefaults.DefaultModel;
        public string FallbackModel { get; set; } = NvidiaAiDefaults.FallbackModel;
        public string BaseUrl { get; set; } = NvidiaAiDefaults.BaseUrl;
        public string UserPrompt { get; set; } = "";
        public string SystemContext { get; set; } = "";
        public string AppMode { get; set; } = "Assistant";
        public string PermissionLevel { get; set; } = "Ask";
        public bool AutoFallback { get; set; } = true;
        public bool SafetyGuardEnabled { get; set; } = true;
        public bool RequireActionApproval { get; set; } = true;
    }

    public sealed class NvidiaCopilotResponse
    {
        public string Intent { get; set; } = "general_help";
        public double Confidence { get; set; } = 0.5;
        public string Reply { get; set; } = "No response.";
        public List<string> SafeActions { get; set; } = new();
        public List<string> SkippedUnsafeActions { get; set; } = new();
        public string RiskLevel { get; set; } = "low";
        public bool RequiresAdmin { get; set; }
        public bool RestoreAvailable { get; set; } = true;
        public bool RequiresApproval { get; set; } = true;
        public string ExpectedResult { get; set; } = "";
        public string Model { get; set; } = NvidiaAiDefaults.DefaultModel;
        public string RawContent { get; set; } = "";
    }

    public sealed class NvidiaCopilotService
    {
        private readonly IAiProvider _provider;
        private readonly AiActionPlanner _planner = new();
        private readonly AiSafetyGuard _safetyGuard = new();
        private readonly AiActionApprovalService _approvalService = new();

        public NvidiaCopilotService() : this(AiProviderFactory.Create(NvidiaAiDefaults.Provider))
        {
        }

        public NvidiaCopilotService(IAiProvider provider)
        {
            _provider = provider;
        }

        public IReadOnlyList<AiModelInfo> GetAvailableModels() => _provider.GetAvailableModels();
        public string GetDefaultModel() => _provider.GetDefaultModel();
        public string GetFallbackModel() => _provider.GetFallbackModel();
        public bool ValidateModel(string modelId) => _provider.ValidateModel(modelId);

        public async Task<NvidiaCopilotResponse> AskAsync(NvidiaCopilotRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ApiKey))
                throw new InvalidOperationException("NVIDIA API key is empty.");

            var model = _provider.ValidateModel(request.Model) ? request.Model : _provider.GetDefaultModel();
            var result = await _provider.ChatAsync(_planner.BuildMessages(request), new AiChatOptions
            {
                ApiKey = request.ApiKey,
                BaseUrl = request.BaseUrl,
                Model = model,
                MaxTokens = 700,
                Temperature = 0.2
            });

            if (!result.Success && request.AutoFallback && !string.Equals(model, request.FallbackModel, StringComparison.OrdinalIgnoreCase))
            {
                var fallbackModel = _provider.ValidateModel(request.FallbackModel) ? request.FallbackModel : _provider.GetFallbackModel();
                result = await _provider.ChatAsync(_planner.BuildMessages(request), new AiChatOptions
                {
                    ApiKey = request.ApiKey,
                    BaseUrl = request.BaseUrl,
                    Model = fallbackModel,
                    MaxTokens = 700,
                    Temperature = 0.2
                });
            }

            if (!result.Success)
                throw new InvalidOperationException($"{result.ErrorCategory}: {_provider.RedactSecret(result.ErrorMessage, request.ApiKey)}");

            var response = ParseStructuredOrPlainText(result.Content, result.Content);
            response.Model = result.Model;
            if (request.SafetyGuardEnabled)
            {
                response.SafeActions = _safetyGuard.FilterSafeActions(response.SafeActions, out var blocked).ToList();
                response.SkippedUnsafeActions = response.SkippedUnsafeActions
                    .Concat(blocked)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            response.RequiresApproval = request.RequireActionApproval && _approvalService.RequiresUserApproval(response.SafeActions);
            return response;
        }

        public async Task<NvidiaCopilotResponse> TestConnectionAsync(string apiKey, string model)
        {
            var result = await _provider.TestConnectionAsync(apiKey, string.IsNullOrWhiteSpace(model) ? _provider.GetDefaultModel() : model);
            if (!result.Success)
                throw new InvalidOperationException($"{result.ErrorCategory}: {_provider.RedactSecret(result.ErrorMessage, apiKey)}");

            return ParseStructuredOrPlainText(result.Content, result.Content);
        }

        public static NvidiaCopilotResponse ParseResponseForTesting(string responseText)
        {
            var root = JsonConvert.DeserializeObject<JObject>(responseText) ?? new JObject();
            var outputText = root["choices"]?.First?["message"]?["content"]?.ToString()
                ?? root["choices"]?.First?["text"]?.ToString()
                ?? root["output_text"]?.ToString()
                ?? ExtractOutputText(root);

            if (string.IsNullOrWhiteSpace(outputText))
            {
                return new NvidiaCopilotResponse
                {
                    Reply = "AI returned an empty response.",
                    RawContent = responseText,
                    SafeActions = new List<string> { "scan_only" }
                };
            }

            return ParseStructuredOrPlainText(outputText, responseText);
        }

        private static NvidiaCopilotResponse ParseStructuredOrPlainText(string outputText, string rawContent)
        {
            var cleaned = outputText.Trim();
            var jsonStart = cleaned.IndexOf('{');
            var jsonEnd = cleaned.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
                cleaned = cleaned.Substring(jsonStart, jsonEnd - jsonStart + 1);

            try
            {
                var parsed = JsonConvert.DeserializeObject<JObject>(cleaned) ?? new JObject();
                return new NvidiaCopilotResponse
                {
                    Intent = parsed.Value<string>("intent") ?? "general_help",
                    Confidence = parsed.Value<double?>("confidence") ?? 0.5,
                    Reply = parsed.Value<string>("reply") ?? outputText,
                    SafeActions = parsed["safe_actions"]?.ToObject<List<string>>() ?? new List<string>(),
                    SkippedUnsafeActions = parsed["skipped_unsafe_actions"]?.ToObject<List<string>>() ?? new List<string>(),
                    RiskLevel = parsed.Value<string>("risk_level") ?? "low",
                    RequiresAdmin = parsed.Value<bool?>("requires_admin") ?? false,
                    RestoreAvailable = parsed.Value<bool?>("restore_available") ?? true,
                    ExpectedResult = parsed.Value<string>("expected_result") ?? "",
                    RawContent = outputText
                };
            }
            catch
            {
                return new NvidiaCopilotResponse
                {
                    Reply = outputText,
                    RawContent = rawContent,
                    SafeActions = new List<string> { "scan_only" }
                };
            }
        }

        private static string ExtractOutputText(JObject root)
        {
            var items = root["output"] as JArray;
            if (items == null)
                return "";

            foreach (var item in items)
            {
                var content = item["content"] as JArray;
                if (content == null)
                    continue;

                foreach (var block in content)
                {
                    var text = block["text"]?.ToString()
                        ?? block["output_text"]?.ToString()
                        ?? block["content"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
            }

            return "";
        }
    }
}

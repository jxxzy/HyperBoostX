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
    public sealed class OpenAiCopilotRequest
    {
        public string ApiKey { get; set; } = "";
        public string Model { get; set; } = "gpt-4.1-mini";
        public string UserPrompt { get; set; } = "";
        public string SystemContext { get; set; } = "";
        public string AppMode { get; set; } = "Assistant";
        public string PermissionLevel { get; set; } = "Ask";
    }

    public sealed class OpenAiCopilotResponse
    {
        public string Intent { get; set; } = "general_help";
        public double Confidence { get; set; } = 0.5;
        public string Reply { get; set; } = "No response.";
        public List<string> SafeActions { get; set; } = new();
        public string RawContent { get; set; } = "";
    }

    public sealed class OpenAiCopilotService
    {
        private static readonly HttpClient HttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(45)
        };

        public async Task<OpenAiCopilotResponse> AskAsync(OpenAiCopilotRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ApiKey))
                throw new InvalidOperationException("OpenAI API key is empty.");

            var developerPrompt =
                "You are HyperBoostX Copilot, a Windows optimization assistant inside a PC utility. " +
                "Use the provided system context to help the user. " +
                "Return strict JSON with keys: intent, confidence, reply, safe_actions. " +
                "safe_actions must be an array containing only these values when relevant: cleanup, ram_optimize, gaming_prep, network_fix, background_trim, power_balanced, scan_only. " +
                "Do not recommend risky registry, service, or driver changes unless explicitly asked. " +
                "Keep reply concise, practical, and user-friendly.";

            var model = string.IsNullOrWhiteSpace(request.Model) ? "gpt-4.1-mini" : request.Model;

            var responsesPayload = new
            {
                model,
                max_output_tokens = 450,
                input = new object[]
                {
                    new
                    {
                        role = "developer",
                        content = new object[]
                        {
                            new { type = "input_text", text = developerPrompt }
                        }
                    },
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "input_text",
                                text =
                                    $"App mode: {request.AppMode}\nPermission level: {request.PermissionLevel}\nSystem context:\n{request.SystemContext}\n\nUser request:\n{request.UserPrompt}"
                            }
                        }
                    }
                }
            };

            var responsesBody = JsonConvert.SerializeObject(responsesPayload);
            var (responsesSuccess, responsesText, responsesError) = await SendOpenAiRequestAsync(
                "https://api.openai.com/v1/responses",
                request.ApiKey,
                responsesBody);

            if (responsesSuccess)
                return ParseResponse(responsesText);

            var chatPayload = new
            {
                model,
                max_tokens = 450,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = developerPrompt
                    },
                    new
                    {
                        role = "user",
                        content =
                            $"App mode: {request.AppMode}\nPermission level: {request.PermissionLevel}\nSystem context:\n{request.SystemContext}\n\nUser request:\n{request.UserPrompt}"
                    }
                }
            };

            var chatBody = JsonConvert.SerializeObject(chatPayload);
            var (chatSuccess, chatText, chatError) = await SendOpenAiRequestAsync(
                "https://api.openai.com/v1/chat/completions",
                request.ApiKey,
                chatBody);

            if (chatSuccess)
                return ParseChatCompletionsResponse(chatText);

            var errorMessage = string.Join(
                " | ",
                new[]
                {
                    responsesError,
                    chatError
                }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase));
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(errorMessage)
                ? "OpenAI request failed."
                : errorMessage);
        }

        public static OpenAiCopilotResponse ParseResponseForTesting(string responseText)
        {
            return ParseResponse(responseText);
        }

        private static OpenAiCopilotResponse ParseResponse(string responseText)
        {
            var root = JsonConvert.DeserializeObject<JObject>(responseText) ?? new JObject();
            var outputText = ExtractOpenAiText(root);
            if (string.IsNullOrWhiteSpace(outputText))
            {
                return new OpenAiCopilotResponse
                {
                    Reply = "AI returned an empty response.",
                    RawContent = responseText,
                    SafeActions = new List<string> { "scan_only" }
                };
            }

            return ParseStructuredOrPlainText(outputText, responseText);
        }

        private static OpenAiCopilotResponse ParseChatCompletionsResponse(string responseText)
        {
            var root = JsonConvert.DeserializeObject<JObject>(responseText) ?? new JObject();
            var outputText =
                root["choices"]?.First?["message"]?["content"]?.ToString()
                ?? root["choices"]?.First?["text"]?.ToString()
                ?? ExtractOpenAiText(root);

            if (string.IsNullOrWhiteSpace(outputText))
            {
                return new OpenAiCopilotResponse
                {
                    Reply = "AI returned an empty response.",
                    RawContent = responseText,
                    SafeActions = new List<string> { "scan_only" }
                };
            }

            return ParseStructuredOrPlainText(outputText, responseText);
        }

        private static OpenAiCopilotResponse ParseStructuredOrPlainText(string outputText, string rawContent)
        {
            var cleaned = outputText.Trim();
            var jsonStart = cleaned.IndexOf('{');
            var jsonEnd = cleaned.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
                cleaned = cleaned.Substring(jsonStart, jsonEnd - jsonStart + 1);

            try
            {
                var parsed = JsonConvert.DeserializeObject<JObject>(cleaned) ?? new JObject();
                return new OpenAiCopilotResponse
                {
                    Intent = parsed.Value<string>("intent") ?? "general_help",
                    Confidence = parsed.Value<double?>("confidence") ?? 0.5,
                    Reply = parsed.Value<string>("reply") ?? outputText,
                    SafeActions = parsed["safe_actions"]?.ToObject<List<string>>() ?? new List<string>(),
                    RawContent = outputText
                };
            }
            catch
            {
                return new OpenAiCopilotResponse
                {
                    Reply = outputText,
                    RawContent = rawContent,
                    SafeActions = new List<string> { "scan_only" }
                };
            }
        }

        private static string ExtractOpenAiText(JObject root)
        {
            var outputText = root["output_text"]?.ToString();
            if (!string.IsNullOrWhiteSpace(outputText))
                return outputText;

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

        private static async Task<(bool Success, string ResponseText, string ErrorMessage)> SendOpenAiRequestAsync(string url, string apiKey, string body)
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpRequest.Content = new StringContent(body, Encoding.UTF8, "application/json");

            using var response = await HttpClient.SendAsync(httpRequest);
            var responseText = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                return (true, responseText, "");

            var errorMessage = TryExtractErrorMessage(responseText);
            var requestId = TryGetHeaderValue(response, "x-request-id");
            var summary = BuildFriendlyErrorMessage(url, (int)response.StatusCode, response.ReasonPhrase ?? "", errorMessage, requestId);
            return (false, responseText, summary);
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

        private static string BuildFriendlyErrorMessage(string url, int statusCode, string reasonPhrase, string errorMessage, string requestId)
        {
            var baseMessage = string.IsNullOrWhiteSpace(errorMessage)
                ? $"OpenAI API returned {statusCode} ({reasonPhrase})."
                : $"OpenAI API returned {statusCode} ({reasonPhrase}): {errorMessage}";

            var endpointLabel = url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
                ? "chat.completions"
                : url.EndsWith("/responses", StringComparison.OrdinalIgnoreCase)
                    ? "responses"
                    : url;

            var diagnosticSuffix = string.IsNullOrWhiteSpace(requestId)
                ? $" Endpoint: {endpointLabel}."
                : $" Endpoint: {endpointLabel}. Request ID: {requestId}.";

            if (statusCode == 429 && !string.IsNullOrWhiteSpace(errorMessage) &&
                (errorMessage.Contains("quota", StringComparison.OrdinalIgnoreCase) ||
                 errorMessage.Contains("billing", StringComparison.OrdinalIgnoreCase) ||
                 errorMessage.Contains("rate limit", StringComparison.OrdinalIgnoreCase)))
            {
                return "OpenAI quota atau billing limit tercapai. Periksa plan, billing, atau tunggu limit reset. " + baseMessage + diagnosticSuffix;
            }

            if (statusCode == 401)
                return "OpenAI API key tidak valid atau tidak punya akses ke model yang dipilih. " + baseMessage + diagnosticSuffix;

            if (statusCode == 403)
                return "Akses ke OpenAI ditolak untuk request ini. Periksa project, model, atau permission API key. " + baseMessage + diagnosticSuffix;

            return baseMessage + diagnosticSuffix;
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
}

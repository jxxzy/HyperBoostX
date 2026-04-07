using System;
using System.Collections.Generic;
using System.Net.Http;
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

            var payload = new
            {
                model = string.IsNullOrWhiteSpace(request.Model) ? "gpt-4.1-mini" : request.Model,
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

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
            httpRequest.Headers.Add("Authorization", $"Bearer {request.ApiKey}");
            httpRequest.Headers.Add("Accept", "application/json");
            httpRequest.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            using var response = await HttpClient.SendAsync(httpRequest);
            var responseText = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            return ParseResponse(responseText);
        }

        private static OpenAiCopilotResponse ParseResponse(string responseText)
        {
            var root = JsonConvert.DeserializeObject<JObject>(responseText) ?? new JObject();
            var outputText = root["output_text"]?.ToString();

            if (string.IsNullOrWhiteSpace(outputText))
            {
                var items = root["output"] as JArray;
                if (items != null)
                {
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
                            {
                                outputText = text;
                                break;
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(outputText))
            {
                return new OpenAiCopilotResponse
                {
                    Reply = "AI returned an empty response.",
                    RawContent = responseText,
                    SafeActions = new List<string> { "scan_only" }
                };
            }

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
                    RawContent = responseText,
                    SafeActions = new List<string> { "scan_only" }
                };
            }
        }
    }
}

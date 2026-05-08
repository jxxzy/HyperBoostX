using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HyperBoostX.Services
{
    public sealed class DiscordWebhookSendResult
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string Summary { get; set; } = "Not sent.";
    }

    public sealed class DiscordWebhookService
    {
        private static readonly HttpClient HttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        public async Task<bool> SendAsync(string webhookUrl, string title, string message, string severity, IDictionary<string, string> fields = null, string username = "HyperBoostX Logs")
        {
            var result = await SendDetailedAsync(webhookUrl, title, message, severity, fields, username);
            return result.Success;
        }

        public async Task<DiscordWebhookSendResult> SendDetailedAsync(string webhookUrl, string title, string message, string severity, IDictionary<string, string> fields = null, string username = "HyperBoostX Logs")
        {
            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                return new DiscordWebhookSendResult
                {
                    ErrorMessage = "Webhook URL is empty.",
                    Summary = "Discord webhook URL is not configured."
                };
            }

            if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri) ||
                !uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ||
                (!uri.Host.EndsWith("discord.com", StringComparison.OrdinalIgnoreCase) &&
                 !uri.Host.EndsWith("discordapp.com", StringComparison.OrdinalIgnoreCase)))
            {
                return new DiscordWebhookSendResult
                {
                    ErrorMessage = "Webhook URL is not a valid Discord HTTPS webhook.",
                    Summary = "Discord webhook URL validation failed."
                };
            }

            try
            {
                var response = await HttpClient.PostAsync(
                    webhookUrl,
                    new StringContent(BuildPayloadJson(title, message, severity, fields, username), Encoding.UTF8, "application/json"));

                return new DiscordWebhookSendResult
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    ErrorMessage = response.IsSuccessStatusCode ? "" : response.ReasonPhrase ?? "",
                    Summary = response.IsSuccessStatusCode
                        ? "Discord webhook delivered."
                        : $"Discord webhook returned HTTP {(int)response.StatusCode}."
                };
            }
            catch (Exception ex)
            {
                return new DiscordWebhookSendResult
                {
                    ErrorMessage = ex.Message,
                    Summary = "Discord webhook request could not be completed."
                };
            }
        }

        public static string BuildPayloadJson(string title, string message, string severity, IDictionary<string, string> fields = null, string username = "HyperBoostX Logs")
        {
            var embedFields = new List<object>();
            if (fields != null)
            {
                foreach (var pair in fields)
                {
                    if (string.IsNullOrWhiteSpace(pair.Value))
                        continue;

                    embedFields.Add(new
                    {
                        name = pair.Key,
                        value = Trim(pair.Value, 900),
                        inline = false
                    });
                }
            }

            var payload = new
            {
                username = Trim(username, 80),
                embeds = new[]
                {
                    new
                    {
                        title = Trim(title, 200),
                        description = Trim(message, 1800),
                        color = GetSeverityColor(severity),
                        timestamp = DateTime.UtcNow.ToString("o"),
                        fields = embedFields
                    }
                }
            };

            return JsonConvert.SerializeObject(payload);
        }

        private static string Trim(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "-";

            return value.Length <= maxLength
                ? value
                : value.Substring(0, maxLength - 3) + "...";
        }

        private static int GetSeverityColor(string severity)
        {
            return severity?.ToLowerInvariant() switch
            {
                "critical" => 0xB91C1C,
                "success" => 0x16A34A,
                "info" => 0x2563EB,
                "warning" => 0xD97706,
                _ => 0xDC2626
            };
        }
    }
}

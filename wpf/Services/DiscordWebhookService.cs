using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HyperBoostX.Services
{
    public sealed class DiscordWebhookService
    {
        private static readonly HttpClient HttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        public async Task<bool> SendAsync(string webhookUrl, string title, string message, string severity, IDictionary<string, string> fields = null)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl))
                return false;

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
                username = "HyperBoostX",
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

            var response = await HttpClient.PostAsync(
                webhookUrl,
                new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));

            return response.IsSuccessStatusCode;
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
                "warning" => 0xD97706,
                _ => 0xDC2626
            };
        }
    }
}

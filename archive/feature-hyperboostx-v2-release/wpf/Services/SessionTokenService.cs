using System;

namespace HyperBoostX.Services
{
    public sealed class SessionTokenService
    {
        public string Token => Environment.GetEnvironmentVariable("HYPERBOOSTX_SESSION_TOKEN")?.Trim() ?? string.Empty;
        public bool IsEnabled => !string.IsNullOrWhiteSpace(Token);
    }
}

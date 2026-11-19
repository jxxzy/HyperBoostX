using System.Threading.Tasks;

namespace HyperBoostX.Services
{
    public sealed class BackendStatusService
    {
        private readonly IHyperBoostBackendClient _client;
        public BackendStatusService(IHyperBoostBackendClient client) => _client = client;
        public async Task<bool> IsOnlineAsync() => await _client.HealthCheckAsync();
    }
}

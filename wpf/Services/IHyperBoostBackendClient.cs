using System.Threading.Tasks;

namespace HyperBoostX.Services
{
    public interface IHyperBoostBackendClient
    {
        Task<bool> HealthCheckAsync();
        Task<dynamic> GetSystemInfoAsync();
        Task<dynamic> GetSystemStatsAsync();
        Task<dynamic> GetTweaksAsync();
        Task<dynamic> ApplyTweakAsync(string tweakId);
        Task<dynamic> GetBoosterProfilesAsync();
        Task<dynamic> ApplyBoosterAsync(string profile);
        Task<dynamic> GetDriversAsync();
        Task<dynamic> CheckDriverUpdatesAsync();
        Task<dynamic> RunSfcAsync();
        Task<dynamic> CleanupAsync(string scope = null);
        Task<dynamic> RunDismAsync();
        Task<dynamic> GetStartupItemsAsync();
        Task<dynamic> GetProcessesAsync();
        Task<dynamic> TestDnsAsync();
        Task<dynamic> FlushDnsAsync();
        Task<dynamic> OptimizeTcpAsync();
        Task<dynamic> ResetNetworkAsync();
    }
}

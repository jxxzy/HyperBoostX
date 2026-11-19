using System.Threading.Tasks;

namespace HyperBoostX.Services
{
    public interface IHyperBoostBackendClient
    {
        Task<bool> HealthCheckAsync();
        Task<dynamic> GetSystemInfoAsync();
        Task<dynamic> GetSystemStatsAsync();
        Task<dynamic> GetTweaksAsync();
        Task<dynamic> ApplyTweakAsync(string tweakId, bool expertMode = false, bool confirmed = false);
        Task<dynamic> GetBoosterProfilesAsync();
        Task<dynamic> ApplyBoosterAsync(string profile);
        Task<dynamic> GetDriversAsync();
        Task<dynamic> CheckDriverUpdatesAsync();
        Task<dynamic> RunSfcAsync();
        Task<dynamic> CleanupAsync(string scope = "");
        Task<dynamic> RunDismAsync();
        Task<dynamic> GetStartupItemsAsync();
        Task<dynamic> GetProcessesAsync();
        Task<dynamic> TestDnsAsync();
        Task<dynamic> FlushDnsAsync();
        Task<dynamic> OptimizeTcpAsync();
        Task<dynamic> ResetNetworkAsync();
        Task<dynamic> RunTripleAiFlowAsync(string userGoal = "gaming", string game = "");
        Task<dynamic> ApplyTripleAiTweaksAsync(dynamic approvedTweaks, bool userApproved);
        Task<dynamic> RevertTripleAiTweaksAsync(string backupId, object tweakIds);
    }
}

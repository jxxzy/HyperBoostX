using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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
        Task<dynamic> CleanupAsync(string scope = null);
        Task<dynamic> RunDismAsync();
        Task<dynamic> GetStartupItemsAsync();
        Task<dynamic> GetProcessesAsync();
        Task<dynamic> GetHardwareGpuAsync();
        Task<dynamic> GetHardwareVendorsAsync();
        Task<dynamic> GetHardwareOverlaysAsync();
        Task<dynamic> GetHardwareProfileAsync();
        Task<dynamic> ExportReportAsync(string format = "md");
        Task<dynamic> TestDnsAsync();
        Task<dynamic> FlushDnsAsync();
        Task<dynamic> OptimizeTcpAsync();
        Task<dynamic> ResetNetworkAsync();
        Task<dynamic> RunTripleAiFlowAsync(string userGoal = "gaming", string game = "");
        Task<dynamic> ApplyTripleAiTweaksAsync(JArray approvedTweaks, bool userApproved = false);
        Task<dynamic> RevertTripleAiTweaksAsync(string backupId = "", IReadOnlyList<string> tweakIds = null);
    }
}


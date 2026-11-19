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
        Task<dynamic> CreateBoostPlanAsync(string goal = "gaming", string mode = "balanced");
        Task<dynamic> ApplyBoostPlanAsync(IReadOnlyList<string> approvedActionIds = null, bool userApproved = false);
        Task<dynamic> UndoBoostPlanAsync();
        Task<dynamic> ExportReportAsync(string format = "md");
        Task<dynamic> TestDnsAsync();
        Task<dynamic> FlushDnsAsync();
        Task<dynamic> OptimizeTcpAsync();
        Task<dynamic> ResetNetworkAsync();
        Task<dynamic> RunSafePlanFlowAsync(string userGoal = "gaming", string game = "");
        Task<dynamic> ApplySafePlanActionsAsync(JArray approvedActions, bool userApproved = false);
        Task<dynamic> RevertSafePlanActionsAsync(string backupId = "", IReadOnlyList<string> actionIds = null);
        [System.Obsolete("Use RunSafePlanFlowAsync. This wrapper is kept only for v1.x compatibility.")]
        Task<dynamic> RunTripleAiFlowAsync(string userGoal = "gaming", string game = "");
        [System.Obsolete("Use ApplySafePlanActionsAsync. This wrapper is kept only for v1.x compatibility.")]
        Task<dynamic> ApplyTripleAiTweaksAsync(JArray approvedTweaks, bool userApproved = false);
        [System.Obsolete("Use RevertSafePlanActionsAsync. This wrapper is kept only for v1.x compatibility.")]
        Task<dynamic> RevertTripleAiTweaksAsync(string backupId = "", IReadOnlyList<string> tweakIds = null);
    }
}


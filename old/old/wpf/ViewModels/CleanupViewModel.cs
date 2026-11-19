namespace HyperBoostX.ViewModels
{
    public sealed class CleanupViewModel : PlacementPageViewModel
    {
        public CleanupViewModel() : base("Cleanup", "Safe temp cleanup preview without touching personal files.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Scope", Value = "SAFE", Detail = "Temp allowlist", Score = 100, Glyph = "CL" });
            Metrics.Add(new CyberMetricViewModel { Title = "Documents", Value = "BLOCK", Detail = "Never deleted", Score = 100, Glyph = "BL" });
            Recommendations.Add("Preview cleanup before applying.");
            Recommendations.Add("Downloads, Desktop, game saves, and system files are blocked.");
            PrimaryAction = "Scan Cleanup";
        }
    }
}

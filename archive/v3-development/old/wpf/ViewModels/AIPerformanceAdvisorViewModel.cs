namespace HyperBoostX.ViewModels
{
    public sealed class AIPerformanceAdvisorViewModel : PlacementPageViewModel
    {
        public AIPerformanceAdvisorViewModel() : base("AI Performance Advisor", "Local diagnosis for bottlenecks, stutter, overlays, startup, and GPU pressure.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Diagnosis", Value = "LOCAL", Detail = "No shell execution", Score = 100, Glyph = "AI" });
            Metrics.Add(new CyberMetricViewModel { Title = "Risk", Value = "LOW", Detail = "Allowlisted actions", Score = 96, Glyph = "RX" });
            Recommendations.Add("GPU-bound PCs should tune in-game GPU-heavy settings first.");
            Recommendations.Add("CPU-bound PCs should review heavy background apps and game simulation settings.");
            PrimaryAction = "Start Smart Scan";
        }
    }
}

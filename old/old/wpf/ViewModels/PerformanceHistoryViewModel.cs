namespace HyperBoostX.ViewModels
{
    public sealed class PerformanceHistoryViewModel : PlacementPageViewModel
    {
        public PerformanceHistoryViewModel() : base("Performance History", "Track scan history, before/after timeline, and local score trends.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "History", Value = "LOCAL", Detail = "Stored on device", Score = 90, Glyph = "PH" });
            Metrics.Add(new CyberMetricViewModel { Title = "Timeline", Value = "READY", Detail = "Before/after", Score = 86, Glyph = "TL" });
            Recommendations.Add("Compare local trend over time instead of fabricated averages.");
            Recommendations.Add("Export history with redaction when sharing.");
            PrimaryAction = "Open Timeline";
        }
    }
}

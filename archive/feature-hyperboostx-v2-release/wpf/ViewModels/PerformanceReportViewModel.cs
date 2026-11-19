namespace HyperBoostX.ViewModels
{
    public sealed class PerformanceReportViewModel : CyberPageViewModel
    {
        public PerformanceReportViewModel() : base("Performance Report", "Before/after local counters with export and no FPS guarantees.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Reports", Value = "JSON", Detail = "TXT / MD too", Score = 90, Glyph = "RP" });
            Metrics.Add(new CyberMetricViewModel { Title = "Redaction", Value = "ON", Detail = "Privacy-safe", Score = 100, Glyph = "RD" });
            Recommendations.Add("Export reports after scan and boost.");
            Recommendations.Add("Share only redacted report content.");
            PrimaryAction = "View Last Report";
        }
    }
}

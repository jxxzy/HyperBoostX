namespace HyperBoostX.ViewModels
{
    public sealed class StartupManagerViewModel : PlacementPageViewModel
    {
        public StartupManagerViewModel() : base("Startup Manager", "Preview, approve, and restore startup changes safely.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Preview", Value = "ON", Detail = "Approval required", Score = 95, Glyph = "PV" });
            Metrics.Add(new CyberMetricViewModel { Title = "Restore", Value = "ON", Detail = "Session metadata", Score = 92, Glyph = "RS" });
            Recommendations.Add("Disable only low-risk apps after reading impact.");
            Recommendations.Add("Driver, audio, security, and anti-cheat items remain protected.");
            PrimaryAction = "Preview Startup Plan";
        }
    }
}

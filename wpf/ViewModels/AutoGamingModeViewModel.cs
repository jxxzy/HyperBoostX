namespace HyperBoostX.ViewModels
{
    public sealed class AutoGamingModeViewModel : PlacementPageViewModel
    {
        public AutoGamingModeViewModel() : base("Auto Gaming Mode", "Detect games, apply safe profile metadata, and auto-restore after close.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Auto Restore", Value = "ON", Detail = "After game closes", Score = 100, Glyph = "AR" });
            Metrics.Add(new CyberMetricViewModel { Title = "Game Detection", Value = "READY", Detail = "Local process scan", Score = 86, Glyph = "GD" });
            Recommendations.Add("Preview safe actions before enabling automation.");
            Recommendations.Add("Protected processes stay locked while gaming.");
            PrimaryAction = "Preview Auto Mode";
        }
    }
}

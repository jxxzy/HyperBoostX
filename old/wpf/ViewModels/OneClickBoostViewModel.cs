namespace HyperBoostX.ViewModels
{
    public sealed class OneClickBoostViewModel : PlacementPageViewModel
    {
        public OneClickBoostViewModel() : base("One Click Boost", "Plan-first safe boost with approval, report, and undo visibility.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Safety Guard", Value = "ACTIVE", Detail = "Risky actions blocked", Score = 100, Glyph = "SG" });
            Metrics.Add(new CyberMetricViewModel { Title = "Undo", Value = "READY", Detail = "Restore metadata", Score = 90, Glyph = "UN" });
            Recommendations.Add("Run Start Smart Scan before applying any safe boost.");
            Recommendations.Add("Approve only reviewed actions; no free-form shell command is created.");
            PrimaryAction = "Create Boost Plan";
        }
    }
}

namespace HyperBoostX.ViewModels
{
    public sealed class HyperBalanceViewModel : CyberPageViewModel
    {
        public HyperBalanceViewModel() : base("HyperBalance", "Smart balance between foreground game, streaming apps, and protected processes.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Foreground", Value = "SAFE", Detail = "No forced priority hacks", Score = 88, Glyph = "HB" });
            Metrics.Add(new CyberMetricViewModel { Title = "Protection", Value = "LOCK", Detail = "Do-not-touch list", Score = 100, Glyph = "LK" });
            Recommendations.Add("Prefer explainable pressure reduction over aggressive process control.");
            Recommendations.Add("External plugin and kernel controls are disabled.");
            PrimaryAction = "Analyze Balance";
        }
    }
}

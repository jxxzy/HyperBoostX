namespace HyperBoostX.ViewModels
{
    public sealed class AboutViewModel : CyberPageViewModel
    {
        public AboutViewModel() : base("About HyperBoostX", "Safe AI Windows Gaming Optimizer, local-first and restore-aware.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Version", Value = "1.4.0", Detail = "Feature Expansion Stable", Score = 100, Glyph = "VX" });
            Metrics.Add(new CyberMetricViewModel { Title = "Backend", Value = "LOCAL", Detail = "127.0.0.1", Score = 100, Glyph = "LC" });
            Recommendations.Add("No guaranteed FPS claim. No official vendor partnership claim.");
            Recommendations.Add("Safety Guard remains active across optimization flows.");
            PrimaryAction = "View Release Notes";
        }
    }
}

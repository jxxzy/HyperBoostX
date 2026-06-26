namespace HyperBoostX.ViewModels
{
    public sealed class GamingEssentialsViewModel : CyberPageViewModel
    {
        public GamingEssentialsViewModel() : base("Gaming Essentials", "Official-source helper for runtimes, launchers, OBS, and safe setup checks.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Install", Value = "MANUAL", Detail = "No silent installers", Score = 100, Glyph = "GE" });
            Metrics.Add(new CyberMetricViewModel { Title = "Sources", Value = "OFFICIAL", Detail = "User approval", Score = 94, Glyph = "OS" });
            Recommendations.Add("Use official download links or trusted package sources only.");
            Recommendations.Add("HyperBoostX never bundles third-party installers silently.");
            PrimaryAction = "Check Essentials";
        }
    }
}

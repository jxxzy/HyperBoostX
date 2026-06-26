namespace HyperBoostX.ViewModels
{
    public sealed class ProtectedAppsViewModel : CyberPageViewModel
    {
        public ProtectedAppsViewModel() : base("Protected Apps", "Anti-cheat, security, driver, audio, and network safety boundaries.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Protection", Value = "ON", Detail = "Always guarded", Score = 100, Glyph = "PA" });
            Metrics.Add(new CyberMetricViewModel { Title = "Blocks", Value = "7", Detail = "Danger classes", Score = 100, Glyph = "BK" });
            Recommendations.Add("Never disable anti-cheat, Defender, driver services, or audio/network services for FPS.");
            Recommendations.Add("Add custom protected apps for streaming and work tools.");
            PrimaryAction = "Review Protection";
        }
    }
}

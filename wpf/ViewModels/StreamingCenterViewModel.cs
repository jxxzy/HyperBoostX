namespace HyperBoostX.ViewModels
{
    public sealed class StreamingCenterViewModel : CyberPageViewModel
    {
        public StreamingCenterViewModel() : base("Streaming Center", "OBS, Discord, Broadcast, voice tools, network, and background pressure checks.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Streaming", Value = "92", Detail = "Readiness score", Score = 92, Glyph = "ST" });
            Metrics.Add(new CyberMetricViewModel { Title = "OBS", Value = "PROTECT", Detail = "Do not close", Score = 100, Glyph = "OB" });
            Recommendations.Add("Protect OBS and Discord when streaming.");
            Recommendations.Add("Pause duplicate overlays only after approval.");
            PrimaryAction = "Check Streaming";
        }
    }
}

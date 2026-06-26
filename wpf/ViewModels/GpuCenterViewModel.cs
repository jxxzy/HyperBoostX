namespace HyperBoostX.ViewModels
{
    public sealed class GpuCenterViewModel : CyberPageViewModel
    {
        public GpuCenterViewModel() : base("GPU Center", "Vendor-aware NVIDIA, AMD, Intel, Microsoft Basic, and unknown fallback guidance.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Vendor Mode", Value = "AUTO", Detail = "Safe detection", Score = 88, Glyph = "GPU" });
            Metrics.Add(new CyberMetricViewModel { Title = "Driver Actions", Value = "MANUAL", Detail = "No auto-download", Score = 100, Glyph = "DRV" });
            Recommendations.Add("Keep GPU driver services enabled.");
            Recommendations.Add("Use official vendor apps for driver downloads and GPU features.");
            PrimaryAction = "Refresh GPU Guide";
        }
    }
}

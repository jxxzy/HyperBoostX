namespace HyperBoostX.ViewModels
{
    public sealed class GpuCenterViewModel : PlacementPageViewModel
    {
        public GpuCenterViewModel() : base("GPU Center", "Vendor-aware NVIDIA, AMD, Intel, Microsoft Basic, and unknown fallback guidance.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Detected GPU", Value = "Refresh", Detail = "Load live GPU status", Score = 0, Glyph = "GPU" });
            Metrics.Add(new CyberMetricViewModel { Title = "Driver", Value = "Unknown", Detail = "No auto-download or silent install", Score = 80, Glyph = "DRV" });
            Metrics.Add(new CyberMetricViewModel { Title = "VRAM", Value = "Unknown", Detail = "Sensor shown when available", Score = 0, Glyph = "VR" });
            Metrics.Add(new CyberMetricViewModel { Title = "Vendor Profile", Value = "Pending", Detail = "NVIDIA/AMD/Intel/fallback guidance", Score = 0, Glyph = "VP" });
            Recommendations.Add("Refresh GPU Status to load detected model, vendor, driver, VRAM, and safe profile guidance.");
            Recommendations.Add("HyperBoostX never overclocks, undervolts, disables driver services, or auto-installs GPU drivers.");
            PrimaryAction = "Refresh GPU Status";
        }
    }
}

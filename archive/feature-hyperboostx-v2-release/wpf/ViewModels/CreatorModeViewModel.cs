namespace HyperBoostX.ViewModels
{
    public sealed class CreatorModeViewModel : CyberPageViewModel
    {
        public CreatorModeViewModel() : base("Creator Mode", "RAM, disk, GPU, and background app guidance for editing and rendering.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "RAM", Value = "WATCH", Detail = "Render pressure", Score = 76, Glyph = "CR" });
            Metrics.Add(new CyberMetricViewModel { Title = "Disk", Value = "CACHE", Detail = "Scratch space", Score = 82, Glyph = "DS" });
            Recommendations.Add("Keep project files and caches on healthy storage.");
            Recommendations.Add("Avoid aggressive cleanup during render/export sessions.");
            PrimaryAction = "Analyze Creator Mode";
        }
    }
}

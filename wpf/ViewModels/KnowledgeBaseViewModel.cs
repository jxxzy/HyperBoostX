namespace HyperBoostX.ViewModels
{
    public sealed class KnowledgeBaseViewModel : PlacementPageViewModel
    {
        public KnowledgeBaseViewModel() : base("Knowledge Base", "Beginner-friendly explanations for DLSS, FSR, XeSS, VRR, Reflex, AFMF, and more.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Terms", Value = "13+", Detail = "GPU/game tech", Score = 80, Glyph = "KB" });
            Metrics.Add(new CyberMetricViewModel { Title = "Mode", Value = "LEARN", Detail = "No mutation", Score = 100, Glyph = "LR" });
            Recommendations.Add("Read pros/cons before changing game or driver settings.");
            Recommendations.Add("Vendor features depend on GPU, driver, display, and game support.");
            PrimaryAction = "Open Knowledge Base";
        }
    }
}

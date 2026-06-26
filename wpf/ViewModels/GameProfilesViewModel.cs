namespace HyperBoostX.ViewModels
{
    public sealed class GameProfilesViewModel : CyberPageViewModel
    {
        public GameProfilesViewModel() : base("Game Profiles", "Local game database, safe profile previews, and restore metadata.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Known Games", Value = "4", Detail = "Built-in database", Score = 64, Glyph = "DB" });
            Metrics.Add(new CyberMetricViewModel { Title = "Profiles", Value = "SAFE", Detail = "No fake FPS claims", Score = 92, Glyph = "GP" });
            Recommendations.Add("Use vendor-supported in-game options such as DLSS, FSR, XeSS, Reflex, or VRR where available.");
            Recommendations.Add("Expected FPS depends on hardware and is not guaranteed.");
            PrimaryAction = "Scan Game Library";
        }
    }
}

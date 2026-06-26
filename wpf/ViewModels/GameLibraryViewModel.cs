namespace HyperBoostX.ViewModels
{
    public sealed class GameLibraryViewModel : CyberPageViewModel
    {
        public GameLibraryViewModel() : base("Game Library", "Steam, Epic, Xbox, Battle.net, EA, Ubisoft, and Riot discovery foundation.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Launchers", Value = "7", Detail = "Supported categories", Score = 78, Glyph = "GL" });
            Metrics.Add(new CyberMetricViewModel { Title = "Running Game", Value = "SCAN", Detail = "Local process only", Score = 80, Glyph = "RG" });
            Recommendations.Add("Launch integration stays local and explicit.");
            Recommendations.Add("No game setting is changed silently.");
            PrimaryAction = "Scan Library";
        }
    }
}

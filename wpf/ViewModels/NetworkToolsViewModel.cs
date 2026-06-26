namespace HyperBoostX.ViewModels
{
    public sealed class NetworkToolsViewModel : CyberPageViewModel
    {
        public NetworkToolsViewModel() : base("Network Tools", "DNS, ping, diagnostics, and admin-aware network actions.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Ping", Value = "17 ms", Detail = "Excellent route", Score = 94, Glyph = "PN" });
            Metrics.Add(new CyberMetricViewModel { Title = "DNS", Value = "GOOD", Detail = "Local diagnostic", Score = 88, Glyph = "DN" });
            Recommendations.Add("Flush DNS only when needed and with approval.");
            Recommendations.Add("Admin-required actions return structured warnings.");
            PrimaryAction = "Run Diagnostics";
        }
    }
}

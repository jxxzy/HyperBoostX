namespace HyperBoostX.ViewModels
{
    public sealed class NetworkToolsViewModel : CyberPageViewModel
    {
        public NetworkToolsViewModel() : base("Network Tools", "DNS, ping, diagnostics, and admin-aware network actions.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Ping", Value = "Run test", Detail = "Use diagnostics before showing latency", Score = 0, Glyph = "PN" });
            Metrics.Add(new CyberMetricViewModel { Title = "DNS", Value = "Run test", Detail = "Local diagnostic", Score = 0, Glyph = "DN" });
            Recommendations.Add("Flush DNS only when needed and with approval.");
            Recommendations.Add("Admin-required actions return structured warnings.");
            PrimaryAction = "Run Diagnostics";
        }
    }
}

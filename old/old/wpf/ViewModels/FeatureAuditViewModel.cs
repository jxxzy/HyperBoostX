namespace HyperBoostX.ViewModels
{
    public sealed class FeatureAuditViewModel : PlacementPageViewModel
    {
        public FeatureAuditViewModel() : base("Feature Audit", "Read-only release health, docs sync, and Safety Guard checks.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Mode", Value = "READ", Detail = "No destructive actions", Score = 100, Glyph = "FA" });
            Metrics.Add(new CyberMetricViewModel { Title = "Release Gate", Value = "SCAN", Detail = "Exportable", Score = 85, Glyph = "GT" });
            Recommendations.Add("Run audit before packaging release artifacts.");
            Recommendations.Add("Feature Audit must never mutate Windows state.");
            PrimaryAction = "Run Read-only Audit";
        }
    }
}

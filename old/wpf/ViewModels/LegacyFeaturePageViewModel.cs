namespace HyperBoostX.ViewModels
{
    public sealed class LegacyFeaturePageViewModel : PlacementPageViewModel
    {
        public LegacyFeaturePageViewModel(
            string featureKey,
            string title,
            string subtitle,
            string primaryAction,
            string firstMetricTitle,
            string firstMetricValue,
            string firstMetricDetail,
            string secondMetricTitle,
            string secondMetricValue,
            string secondMetricDetail,
            params string[] recommendations)
            : base(title, subtitle, featureKey)
        {
            PrimaryAction = primaryAction;
            Status = "Ready - safe preview first";
            LiveResultTitle = title;
            LiveResult = "Run a feature action to load live backend data. Mutating actions require preview, approval, Safety Guard, restore metadata where applicable, and report output.";

            Metrics.Add(new CyberMetricViewModel { Title = firstMetricTitle, Value = firstMetricValue, Detail = firstMetricDetail, Score = 80, Glyph = "LIVE" });
            Metrics.Add(new CyberMetricViewModel { Title = secondMetricTitle, Value = secondMetricValue, Detail = secondMetricDetail, Score = 80, Glyph = "SAFE" });

            foreach (var recommendation in recommendations)
                Recommendations.Add(recommendation);
        }
    }
}

namespace HyperBoostX.ViewModels
{
    public sealed class BenchmarkLabViewModel : PlacementPageViewModel
    {
        public BenchmarkLabViewModel() : base("Benchmark Lab", "Manual FPS, CSV import, local history, and frametime report foundation.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Manual Input", Value = "ON", Detail = "FPS / 1% low", Score = 90, Glyph = "BM" });
            Metrics.Add(new CyberMetricViewModel { Title = "Cloud Average", Value = "ROAD", Detail = "No fake dataset", Score = 55, Glyph = "CA" });
            Recommendations.Add("Import only verified local benchmark CSV data.");
            Recommendations.Add("Similar-hardware comparisons require a trusted dataset first.");
            PrimaryAction = "Import Benchmark";
        }
    }
}

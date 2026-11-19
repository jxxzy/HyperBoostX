namespace HyperBoostX.ViewModels
{
    public sealed class ProcessAnalyzerViewModel : PlacementPageViewModel
    {
        public ProcessAnalyzerViewModel() : base("Process Analyzer", "Read-only pressure view for CPU, RAM, startup, and heavy apps.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Mode", Value = "READ", Detail = "No kill action", Score = 100, Glyph = "RO" });
            Metrics.Add(new CyberMetricViewModel { Title = "Pressure", Value = "SCAN", Detail = "Backend /api/processes", Score = 72, Glyph = "PR" });
            Recommendations.Add("Review high-memory apps before gaming.");
            Recommendations.Add("Protected processes and anti-cheat stay locked.");
            PrimaryAction = "Analyze Processes";
        }
    }
}

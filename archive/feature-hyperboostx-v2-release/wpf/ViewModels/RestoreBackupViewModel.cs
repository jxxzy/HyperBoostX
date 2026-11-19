namespace HyperBoostX.ViewModels
{
    public sealed class RestoreBackupViewModel : CyberPageViewModel
    {
        public RestoreBackupViewModel() : base("Restore & Backup", "Restore sessions, integrity check, export, and rollback guidance.")
        {
            Metrics.Add(new CyberMetricViewModel { Title = "Sessions", Value = "READY", Detail = "Local metadata", Score = 92, Glyph = "RS" });
            Metrics.Add(new CyberMetricViewModel { Title = "Integrity", Value = "OK", Detail = "Verify before apply", Score = 96, Glyph = "OK" });
            Recommendations.Add("Review restore preview before applying rollback.");
            Recommendations.Add("Crash recovery reviews incomplete jobs before any action.");
            PrimaryAction = "Open Restore Sessions";
        }
    }
}

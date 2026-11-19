using HyperBoostX.Core;
using HyperBoostX.Windows;

namespace HyperBoostX.App;

public sealed partial class ShellViewModel
{
    private DiagnosticReport? _readiness;
    private Fact? ScoreFact=>_readiness?.Facts.FirstOrDefault(x=>x.Name=="HyperBoost Score");
    public string DashboardReadinessValue=>ScoreFact is {Availability:"Available" or "Limited"} score?score.Value:Localization["NotScored"];
    public string DashboardReadinessCoverage
    {
        get
        {
            if(_readiness is not {} report||ScoreFact is not {} score||score.Availability=="Unavailable")return Localization["ReadinessNoEvidence"];
            var count=report.Facts.Count(x=>x.Name.EndsWith(" component",StringComparison.Ordinal)&&x.Availability is "Available" or "Limited");
            return $"{count}/6 · {report.ObservedAt.ToLocalTime():g}";
        }
    }
    private void SetDashboardReadiness(DiagnosticReport? report)
    {
        _readiness=report is {Center:"HyperBoost Score"}&&report.Facts.Any(x=>x.Name=="Method"&&x.Source=="HBX-HEALTH-SCORE-2")?report:null;
        Notify(nameof(DashboardReadinessValue));Notify(nameof(DashboardReadinessCoverage));
    }
    private void RestoreDashboardReadiness()
    {
        try{SetDashboardReadiness(Runtime.Journal.Reports().FirstOrDefault(x=>x.Center=="HyperBoost Score"&&x.Facts.Any(f=>f.Name=="Method"&&f.Source=="HBX-HEALTH-SCORE-2")));}
        catch(Exception){SetDashboardReadiness(null);}
    }
    private async Task RefreshDashboardReadiness(CancellationToken token)
    {
        var report=await SystemHealthScore.MeasureAsync(token);
        token.ThrowIfCancellationRequested();
        Runtime.Journal.SaveReport(report);
        SetDashboardReadiness(report);
        Status=Localization["ReadinessMeasurementSaved"];
    }
}

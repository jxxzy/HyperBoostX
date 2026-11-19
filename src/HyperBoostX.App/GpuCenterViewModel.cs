using System.Collections.ObjectModel;
using HyperBoostX.Core;
using HyperBoostX.Windows;

namespace HyperBoostX.App;

public sealed partial class ShellViewModel
{
    private DiagnosticReport? _selectedGpuReport;
    private string _gpuVendorFilter="auto";

    public ObservableCollection<DiagnosticReport> GpuHistory {get;}=[];
    public string[] GpuVendorFilters {get;}=["auto","nvidia","amd","intel"];
    public bool IsGpuCenter=>Title=="GPU Center";
    public bool HasGpuResults=>IsGpuCenter&&_pageReport?.Center=="GPU";
    public bool HasGpuHistory=>GpuHistory.Count>0;
    public string GpuAdapterSummary
    {
        get
        {
            if(!HasGpuResults)return Localization["GpuNoEvidence"];
            var adapters=GpuAdapterFacts().Select(fact=>fact.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            return adapters.Length==0?Localization["GpuNoAdapters"]:string.Join(Environment.NewLine,adapters);
        }
    }
    public string GpuDriverSummary
    {
        get
        {
            if(!HasGpuResults)return Localization["GpuDriverUnavailable"];
            var drivers=_pageReport!.Facts
                .Where(fact=>fact.Name.EndsWith("· DriverVersion",StringComparison.OrdinalIgnoreCase)&&MatchesGpuVendor(fact))
                .Select(fact=>fact.Value)
                .Where(value=>!string.IsNullOrWhiteSpace(value)&&!string.Equals(value,"Unavailable",StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return drivers.Length==0?Localization["GpuDriverUnavailable"]:string.Join(" · ",drivers);
        }
    }
    public string GpuScanSummary=>HasGpuResults
        ?$"{_pageReport!.Facts.Count} · {Localization["Observations"]}"
        :Localization["GpuNoEvidence"];
    public string GpuVendorFilter
    {
        get=>_gpuVendorFilter;
        set
        {
            var filter=GpuVendorFilters.Contains(value,StringComparer.OrdinalIgnoreCase)?value:"auto";
            if(_gpuVendorFilter==filter)return;
            _gpuVendorFilter=filter;
            Notify();
            FactsView.Refresh();
            if(SelectedFact is {} selected&&!MatchesFact(selected))SelectedFact=null;
            Notify(nameof(GpuAdapterSummary));
            Notify(nameof(GpuDriverSummary));
        }
    }
    public DiagnosticReport? SelectedGpuReport
    {
        get=>_selectedGpuReport;
        set
        {
            if(value==null||value==_selectedGpuReport)return;
            _selectedGpuReport=value;
            Notify();
            Show(value);
        }
    }

    private IEnumerable<Fact> GpuAdapterFacts()=>_pageReport?.Facts
        .Where(fact=>fact.Name=="Name"&&fact.Availability=="Available"&&MatchesGpuVendor(fact))
        ??[];

    private bool MatchesGpuVendor(Fact fact)
    {
        if(_gpuVendorFilter=="auto")return true;
        // Counter instance names cannot be attributed to a vendor by string matching.
        if(fact.Source==GpuCounters.EngineClass||fact.Source==GpuCounters.MemoryClass||fact.Name=="GPU counter methodology")return true;
        var vendor=_gpuVendorFilter switch{"nvidia"=>"NVIDIA","amd"=>"AMD","intel"=>"Intel",_=>""};
        return $"{fact.Name} {fact.Value} {fact.Source}".Contains(vendor,StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshGpuHistory(DateTimeOffset? selectedAt=null)
    {
        GpuHistory.Clear();
        foreach(var report in Runtime.Journal.Reports().Where(report=>report.Center=="GPU").OrderByDescending(report=>report.ObservedAt).Take(100))
            GpuHistory.Add(report);
        _selectedGpuReport=GpuHistory.FirstOrDefault(report=>report.ObservedAt==selectedAt)??GpuHistory.FirstOrDefault();
        Notify(nameof(SelectedGpuReport));
        Notify(nameof(HasGpuHistory));
    }
}

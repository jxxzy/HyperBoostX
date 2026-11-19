using System.Globalization;

namespace HyperBoostX.Core;

public sealed record ResearchTrial(string Id,DateTimeOffset CreatedAt,string Experiment,string Workload,string Variant,string Metric,double Value,string Unit,string Notes)
{
    public override string ToString()=>$"{Experiment} · {Variant} · {Metric} {Value:G6} {Unit} · {CreatedAt.ToLocalTime():g}";
}

public sealed class ResearchExperiments(DocumentStore store)
{
    private static readonly string[] Variants=["A","B","C","D"];
    public IReadOnlyList<ResearchTrial> History()=>store.Read("research-trials",new List<ResearchTrial>()).OrderByDescending(t=>t.CreatedAt).ToArray();
    public ResearchTrial Add(string experiment,string workload,string variant,string metric,double value,string unit,string notes)
    {
        foreach(var (text,label,max) in new[]{(experiment,"experiment",120),(workload,"workload",160),(metric,"metric",80),(unit,"unit",24)})
            if(string.IsNullOrWhiteSpace(text)||text.Length>max||text.Any(char.IsControl))throw new HbxException($"Enter a valid {label}.");
        if(!Variants.Contains(variant,StringComparer.Ordinal))throw new HbxException("Variant must be A, B, C, or D.");
        if(!double.IsFinite(value)||Math.Abs(value)>1e12)throw new HbxException("Research value must be finite and within the supported range.");
        if(notes.Length>4000)throw new HbxException("Research notes exceed 4,000 characters.");
        var trial=new ResearchTrial(Guid.NewGuid().ToString("N"),DateTimeOffset.UtcNow,experiment.Trim(),workload.Trim(),variant,metric.Trim(),value,unit.Trim(),notes);
        store.Update("research-trials",new List<ResearchTrial>(),items=>items.Append(trial).TakeLast(2000).ToList());return trial;
    }
    public DiagnosticReport Report(string experiment)
    {
        var trials=History().Where(t=>string.Equals(t.Experiment,experiment,StringComparison.OrdinalIgnoreCase)).OrderBy(t=>t.CreatedAt).ToArray();
        if(trials.Length==0)throw new HbxException("No trials exist for this experiment.");
        var facts=new List<Fact>{new("Experiment",trials[0].Experiment,"Local encrypted research history"),new("Workload",string.Join(", ",trials.Select(t=>t.Workload).Distinct(StringComparer.OrdinalIgnoreCase)),"User supplied","User Reported")};
        foreach(var metricGroup in trials.GroupBy(t=>$"{t.Metric}\u001f{t.Unit}",StringComparer.OrdinalIgnoreCase))
        {
            var parts=metricGroup.Key.Split('\u001f');var baseline=metricGroup.Where(t=>t.Variant=="A").Select(t=>t.Value).ToArray();var baselineMean=baseline.Length==0?(double?)null:baseline.Average();
            foreach(var variant in Variants)
            {
                var values=metricGroup.Where(t=>t.Variant==variant).Select(t=>t.Value).ToArray();if(values.Length==0)continue;var mean=values.Average();var delta=baselineMean is null||variant=="A"?"":$" · Δ vs A {mean-baselineMean.Value:+0.####;-0.####;0}";
                facts.Add(new($"{parts[0]} · variant {variant}",$"mean {mean.ToString("G6",CultureInfo.InvariantCulture)} {parts[1]} · n={values.Length}{delta}","User-supplied repeat trials","Calculated","Available"));
            }
        }
        facts.Add(new("Methodology","Use the same workload, measurement tool, duration, environment and run order. A/B/C/D labels do not prove causation; hardware-upgrade variants must be measured on actual hardware.","Research Mode guardrail","Guidance","Available"));
        return new("Research Mode",DateTimeOffset.UtcNow,facts,[]);
    }
}

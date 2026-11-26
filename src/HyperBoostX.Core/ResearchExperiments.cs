using System.Globalization;

namespace HyperBoostX.Core;

public sealed record ResearchTrial(string Id,DateTimeOffset CreatedAt,string Experiment,string Workload,string Variant,string Metric,double Value,string Unit,string Notes)
{
    public override string ToString()=>$"{Experiment} · {Variant} · {Metric} {Value:G6} {Unit} · {CreatedAt.ToLocalTime():g}";
}

public sealed class ResearchExperiments(DocumentStore store)
{
    private static readonly string[] Variants=["A","B","C","D"];
    public const int MaximumTrials=2000;
    public IReadOnlyList<ResearchTrial> History()=>store.Read("research-trials",new List<ResearchTrial>()).OrderByDescending(t=>t.CreatedAt).ToArray();
    public ResearchTrial Add(string experiment,string workload,string variant,string metric,double value,string unit,string notes)
    {
        foreach(var (text,label,max) in new[]{(experiment,"experiment",120),(workload,"workload",160),(metric,"metric",80),(unit,"unit",24)})
            if(string.IsNullOrWhiteSpace(text)||text.Length>max||text.Any(char.IsControl))throw new HbxException($"Enter a valid {label}.");
        if(!Variants.Contains(variant,StringComparer.Ordinal))throw new HbxException("Variant must be A, B, C, or D.");
        if(!double.IsFinite(value)||Math.Abs(value)>1e12)throw new HbxException("Research value must be finite and within the supported range.");
        if(notes.Length>4000)throw new HbxException("Research notes exceed 4,000 characters.");
        var trial=new ResearchTrial(Guid.NewGuid().ToString("N"),DateTimeOffset.UtcNow,experiment.Trim(),workload.Trim(),variant,metric.Trim(),value,unit.Trim(),notes);
        store.Update("research-trials",new List<ResearchTrial>(),items=>
        {
            if(items.Count>=MaximumTrials)throw new HbxException("Research history is full (2,000 trials). Existing evidence was preserved; no new trial saved.");
            items.Add(trial);return items;
        });return trial;
    }
    public DiagnosticReport Report(string experiment)
    {
        var trials=History().Where(t=>string.Equals(t.Experiment,experiment,StringComparison.OrdinalIgnoreCase)).OrderBy(t=>t.CreatedAt).ToArray();
        if(trials.Length==0)throw new HbxException("No trials exist for this experiment.");
        var facts=new List<Fact>{new("Experiment",trials[0].Experiment,"Local encrypted research history"),new("Workload",string.Join(", ",trials.Select(t=>t.Workload).Distinct(StringComparer.OrdinalIgnoreCase)),"User supplied","User Reported")};
        var multipleWorkloads=trials.Select(t=>t.Workload).Distinct(StringComparer.OrdinalIgnoreCase).Skip(1).Any();
        foreach(var metricGroup in trials.GroupBy(t=>$"{t.Workload}\u001f{t.Metric}\u001f{t.Unit}",StringComparer.OrdinalIgnoreCase))
        {
            var parts=metricGroup.Key.Split('\u001f');var baseline=metricGroup.Where(t=>t.Variant=="A").Select(t=>t.Value).ToArray();var baselineMean=baseline.Length==0?(double?)null:baseline.Average();
            foreach(var variant in Variants)
            {
                var values=metricGroup.Where(t=>t.Variant==variant).Select(t=>t.Value).ToArray();if(values.Length==0)continue;var mean=values.Average();var delta=variant=="A"?"":baselineMean is null?" · no baseline A for this workload":$" · Δ vs A {(mean-baselineMean.Value).ToString("+0.####;-0.####;0",CultureInfo.InvariantCulture)}";
                var label=multipleWorkloads?$"{parts[0]} / {parts[1]}":parts[1];
                var ordered=values.Order().ToArray();var middle=ordered.Length/2;
                var median=ordered.Length%2==0?(ordered[middle-1]+ordered[middle])/2:ordered[middle];
                var sd=values.Length<2?(double?)null:Math.Sqrt(values.Sum(v=>Math.Pow(v-mean,2))/(values.Length-1));
                string F(double number)=>number.ToString("G6",CultureInfo.InvariantCulture);
                var spread=$" · median {F(median)} · min {F(ordered[0])} · max {F(ordered[^1])} · sample SD {(sd is {} deviation?F(deviation):"Unavailable (n<2)")}";
                var percent="";
                if(variant!="A"&&baselineMean is {} reference)
                {
                    var relative=reference==0?double.NaN:(mean-reference)/Math.Abs(reference)*100;
                    percent=double.IsFinite(relative)?$" · relative Δ {relative.ToString("+0.####;-0.####;0",CultureInfo.InvariantCulture)}%":" · relative Δ unavailable (zero baseline or overflow)";
                }
                facts.Add(new($"{label} · variant {variant}",$"mean {F(mean)} {parts[2]} · n={values.Length}{delta}{percent}{spread}","User-supplied repeat trials","Calculated","Available"));
                var groupTrials=metricGroup.Where(t=>t.Variant==variant).OrderBy(t=>t.CreatedAt).ToArray();
                facts.Add(new($"{label} · variant {variant} · recorded period",$"{groupTrials[0].CreatedAt:O} / {groupTrials[^1].CreatedAt:O}","Local research recording timestamps (not measurement timestamps)","Direct","Available"));
            }
        }
        facts.Add(new("Methodology","Use the same workload, measurement tool, duration, environment and run order. A/B/C/D labels do not prove causation; hardware-upgrade variants must be measured on actual hardware. Relative delta uses the absolute baseline mean as denominator; positive or negative changes are not automatically improvements. Sample SD requires at least two observations.","Research Mode guardrail","Guidance","Available"));
        return new("Research Mode",DateTimeOffset.UtcNow,facts,[]);
    }
}

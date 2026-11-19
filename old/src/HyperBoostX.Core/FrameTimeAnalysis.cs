using System.Globalization;

namespace HyperBoostX.Core;

public static class FrameTimeAnalysis
{
    public const int MaximumRows=500_000;
    private sealed record Sample(double FrameTime,double? Cpu,double? Gpu,double? Disk,double? Thermal);

    public static DiagnosticReport AnalyzeCsv(string csv,string sourceName)
    {
        if(string.IsNullOrWhiteSpace(csv))throw new HbxException("Frametime CSV is empty.");
        var lines=csv.Split(['\r','\n'],StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        if(lines.Length>MaximumRows+1)throw new HbxException($"Frametime CSV exceeds {MaximumRows:N0} samples.");
        var first=Split(lines[0]);var hasHeader=first.Any(value=>!double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out _));
        var headers=(hasHeader?first:["frametime_ms"]).Select(Normalize).ToArray();
        var frameIndex=Array.FindIndex(headers,value=>value is "frametimems" or "frametime" or "ms" or "frametimeinms");
        if(frameIndex<0)throw new HbxException("CSV requires a frametime_ms column or a single numeric frametime column.");
        int Index(params string[] names)=>Array.FindIndex(headers,value=>names.Contains(value,StringComparer.Ordinal));
        int cpuIndex=Index("cpu","cpuusage","cpupercent"),gpuIndex=Index("gpu","gpuusage","gpupercent"),diskIndex=Index("disk","diskio","diskiombs"),thermalIndex=Index("thermal","temperature","tempc","gputempc","cputempc");
        var samples=new List<Sample>();var start=hasHeader?1:0;
        for(var row=start;row<lines.Length;row++)
        {
            var fields=Split(lines[row]);if(frameIndex>=fields.Length||!Try(fields[frameIndex],out var frame)||frame<=0||frame>60_000)throw new HbxException($"Invalid frametime at CSV row {row+1}.");
            samples.Add(new(frame,Optional(fields,cpuIndex),Optional(fields,gpuIndex),Optional(fields,diskIndex),Optional(fields,thermalIndex)));
        }
        if(samples.Count<10)throw new HbxException("At least 10 valid frametime samples are required.");
        var ordered=samples.Select(s=>s.FrameTime).Order().ToArray();var mean=ordered.Average();var median=Percentile(ordered,.5);var p95=Percentile(ordered,.95);var p99=Percentile(ordered,.99);var p999=Percentile(ordered,.999);
        var oneCount=Math.Max(1,(int)Math.Ceiling(ordered.Length*.01));var pointOneCount=Math.Max(1,(int)Math.Ceiling(ordered.Length*.001));
        var oneLow=1000/ordered.TakeLast(oneCount).Average();var pointOneLow=1000/ordered.TakeLast(pointOneCount).Average();var threshold=Math.Max(33.333,median*1.5);var spikes=ordered.Count(value=>value>=threshold);
        var facts=new List<Fact>
        {
            new("Samples",samples.Count.ToString("N0"),sourceName,"Direct","Available"),
            new("Average FPS",(1000/mean).ToString("F2",CultureInfo.InvariantCulture),sourceName,"Calculated","Available"),
            new("1% low FPS",oneLow.ToString("F2",CultureInfo.InvariantCulture),sourceName,"Calculated","Available"),
            new("0.1% low FPS",pointOneLow.ToString("F2",CultureInfo.InvariantCulture),sourceName,"Calculated","Available"),
            new("Frame time",$"mean {mean:F3} ms · median {median:F3} ms · P95 {p95:F3} ms · P99 {p99:F3} ms · P99.9 {p999:F3} ms",sourceName,"Calculated","Available"),
            new("Frame-time spikes",$"{spikes:N0} samples ≥ {threshold:F3} ms ({spikes*100d/samples.Count:F2}%)","max(33.333 ms, 1.5 × median)","Calculated","Available"),
        };
        AddCorrelation(facts,samples,"CPU load",s=>s.Cpu,sourceName);AddCorrelation(facts,samples,"GPU load",s=>s.Gpu,sourceName);AddCorrelation(facts,samples,"Disk I/O",s=>s.Disk,sourceName);AddCorrelation(facts,samples,"Thermal data",s=>s.Thermal,sourceName);
        var findings=spikes>0?[new Finding("Frame-time spikes observed",$"{spikes:N0} of {samples.Count:N0} samples crossed the documented threshold.","Compare the same workload and capture method. Correlation fields are descriptive and do not prove causation.","Calculated from imported frametime samples")]:Array.Empty<Finding>();
        return new("Stutter Analyzer",DateTimeOffset.UtcNow,facts,findings);
    }

    private static void AddCorrelation(List<Fact> facts,List<Sample> samples,string name,Func<Sample,double?> selector,string source)
    {
        var pairs=samples.Select(s=>(Frame:s.FrameTime,Metric:selector(s))).Where(p=>p.Metric.HasValue).Select(p=>(p.Frame,Metric:p.Metric!.Value)).ToArray();
        if(pairs.Length<10){facts.Add(new(name+" correlation","Unavailable",source,"Unavailable","Unavailable"));return;}
        var xMean=pairs.Average(p=>p.Frame);var yMean=pairs.Average(p=>p.Metric);var numerator=pairs.Sum(p=>(p.Frame-xMean)*(p.Metric-yMean));var x=Math.Sqrt(pairs.Sum(p=>Math.Pow(p.Frame-xMean,2)));var y=Math.Sqrt(pairs.Sum(p=>Math.Pow(p.Metric-yMean,2)));
        facts.Add(x==0||y==0?new(name+" correlation","Unavailable: zero variance",source,"Calculated","Unavailable"):new(name+" correlation",(numerator/(x*y)).ToString("F4",CultureInfo.InvariantCulture),$"Pearson r across {pairs.Length:N0} paired samples · {source}","Calculated","Available"));
    }
    private static double Percentile(double[] ordered,double percentile){var position=(ordered.Length-1)*percentile;var lower=(int)Math.Floor(position);var upper=(int)Math.Ceiling(position);return lower==upper?ordered[lower]:ordered[lower]+(ordered[upper]-ordered[lower])*(position-lower);}
    private static double? Optional(string[] fields,int index)=>index>=0&&index<fields.Length&&Try(fields[index],out var value)?value:null;
    private static bool Try(string text,out double value)=>double.TryParse(text.Trim(),NumberStyles.Float,CultureInfo.InvariantCulture,out value)&&double.IsFinite(value);
    private static string Normalize(string value)=>new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    private static string[] Split(string line)=>line.Split(',').Select(value=>value.Trim().Trim('"')).ToArray();
}

using System.Globalization;
using System.IO;
using System.Text;

namespace HyperBoostX.Core;

public static class FrameTimeAnalysis
{
    public const int MaximumRows=500_000;
    public const int MaximumCaptureSize=64*1024*1024;
    private sealed record Sample(double FrameTime,double? Cpu,double? Gpu,double? Disk,double? Thermal);

    public static DiagnosticReport AnalyzeFile(string path,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        if(stream.Length>MaximumCaptureSize)throw new HbxException("Frametime CSV maximum size is 64 MiB.");
        using var reader=new StreamReader(stream);
        var buffer=new char[8192];var csv=new StringBuilder();int count;
        while((count=reader.Read(buffer,0,buffer.Length))>0)
        {
            token.ThrowIfCancellationRequested();
            if(csv.Length+count>MaximumCaptureSize)throw new HbxException("Frametime CSV exceeds the capture size limit.");
            csv.Append(buffer,0,count);
        }
        return AnalyzeCsv(csv.ToString(),Path.GetFileName(path),token);
    }

    public static DiagnosticReport AnalyzeCsv(string csv,string sourceName,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();
        if(string.IsNullOrWhiteSpace(csv))throw new HbxException("Frametime CSV is empty.");
        if(csv.Length>MaximumCaptureSize)throw new HbxException("Frametime CSV exceeds the capture size limit.");
        using var reader=new StringReader(csv);
        string? line;int row=0;
        do{token.ThrowIfCancellationRequested();line=reader.ReadLine();row++;}while(line is not null&&string.IsNullOrWhiteSpace(line));
        if(line is null)throw new HbxException("Frametime CSV is empty.");
        var first=Split(line.TrimStart('\uFEFF'),row);
        var hasHeader=first.Any(value=>!double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out _));
        var headers=(hasHeader?first:["frametime_ms"]).Select(Normalize).ToArray();
        if(headers.Distinct(StringComparer.Ordinal).Count()!=headers.Length)throw new HbxException("Duplicate CSV column names.");
        int Index(params string[] names)
        {
            var matches=Enumerable.Range(0,headers.Length).Where(i=>names.Contains(headers[i],StringComparer.Ordinal)).ToArray();
            if(matches.Length>1)throw new HbxException("Ambiguous CSV measurement columns: "+string.Join(", ",names));
            return matches.Length==0?-1:matches[0];
        }
        var frameIndex=Index("frametimems","frametime","ms","frametimeinms","msbetweenpresents");
        if(frameIndex<0)throw new HbxException("CSV requires frametime_ms, MsBetweenPresents, or a single numeric frametime column.");
        bool presents=headers[frameIndex]=="msbetweenpresents";
        int appIndex=Index("application"),pidIndex=Index("processid"),swapIndex=Index("swapchainaddress");
        if(presents&&(appIndex<0||pidIndex<0||swapIndex<0))throw new HbxException("PresentMon CSV requires Application, ProcessID and SwapChainAddress to identify a single presentation stream.");
        int cpuIndex=Index("cpu","cpuusage","cpupercent"),gpuIndex=Index("gpu","gpuusage","gpupercent"),diskIndex=Index("disk","diskio","diskiombs"),thermalIndex=Index("thermal","temperature","tempc","gputempc","cputempc");
        var samples=new List<Sample>();(string App,uint Pid,string Swap)? identity=null;
        void AddSample(string[] fields,int lineNumber)
        {
            token.ThrowIfCancellationRequested();
            if(samples.Count>=MaximumRows)throw new HbxException($"Frametime CSV exceeds {MaximumRows:N0} samples.");
            if(fields.Length!=headers.Length||!Try(fields[frameIndex],out var frame)||frame<=0||frame>60_000||!double.IsFinite(1000/frame))
                throw new HbxException($"Invalid frametime or column count at CSV row {lineNumber}.");
            if(presents)
            {
                if(string.IsNullOrWhiteSpace(fields[appIndex])||!uint.TryParse(fields[pidIndex],NumberStyles.None,CultureInfo.InvariantCulture,out var pid)||pid==0||string.IsNullOrWhiteSpace(fields[swapIndex]))
                    throw new HbxException($"Invalid presentation stream identity at CSV row {lineNumber}.");
                var current=(fields[appIndex],pid,fields[swapIndex]);
                if(identity is {} previous&&(!string.Equals(previous.App,current.Item1,StringComparison.OrdinalIgnoreCase)||previous.Pid!=current.Item2||!string.Equals(previous.Swap,current.Item3,StringComparison.OrdinalIgnoreCase)))
                    throw new HbxException("Multiple presentation streams detected. Export a single application, process and swap chain before analysis.");
                identity=current;
            }
            samples.Add(new(frame,Optional(fields,cpuIndex,0,100),Optional(fields,gpuIndex,0,100),Optional(fields,diskIndex,0,double.MaxValue),Optional(fields,thermalIndex,-273.15,double.MaxValue)));
        }
        if(!hasHeader)AddSample(first,row);
        while((line=reader.ReadLine()) is not null)
        {
            row++;token.ThrowIfCancellationRequested();
            if(!string.IsNullOrWhiteSpace(line))AddSample(Split(line,row),row);
        }
        token.ThrowIfCancellationRequested();
        if(samples.Count<10)throw new HbxException("At least 10 valid frametime samples are required.");
        var ordered=samples.Select(s=>s.FrameTime).Order().ToArray();var mean=ordered.Average();var median=Percentile(ordered,.5);var p95=Percentile(ordered,.95);var p99=Percentile(ordered,.99);var p999=Percentile(ordered,.999);
        var oneCount=Math.Max(1,(int)Math.Ceiling(ordered.Length*.01));var pointOneCount=Math.Max(1,(int)Math.Ceiling(ordered.Length*.001));
        var oneLow=1000/ordered.TakeLast(oneCount).Average();var pointOneLow=1000/ordered.TakeLast(pointOneCount).Average();var threshold=Math.Max(33.333,median*1.5);var spikes=ordered.Count(value=>value>=threshold);
        var facts=new List<Fact>
        {
            new("Samples",samples.Count.ToString("N0"),sourceName,"Direct","Available"),
            new("Capture method",presents?"PresentMon presentation intervals (not display FPS or input latency)":"Imported frame intervals (user-supplied capture)",sourceName,"Direct","Available"),
            new("Average FPS",(1000/mean).ToString("F2",CultureInfo.InvariantCulture),sourceName,"Calculated","Available"),
            new("1% low FPS",samples.Count>=100?oneLow.ToString("F2",CultureInfo.InvariantCulture):"Unavailable: fewer than 100 samples",sourceName,"Calculated",samples.Count>=100?"Available":"Unavailable"),
            new("0.1% low FPS",samples.Count>=1000?pointOneLow.ToString("F2",CultureInfo.InvariantCulture):"Unavailable: fewer than 1,000 samples",sourceName,"Calculated",samples.Count>=1000?"Available":"Unavailable"),
            new("Frame time",$"mean {mean:F3} ms · median {median:F3} ms · P95 {p95:F3} ms · P99 {p99:F3} ms · P99.9 {p999:F3} ms",sourceName,"Calculated","Available"),
            new("Frame-time spikes",$"{spikes:N0} samples ≥ {threshold:F3} ms ({spikes*100d/samples.Count:F2}%)","max(33.333 ms, 1.5 × median)","Calculated","Available"),
        };
        token.ThrowIfCancellationRequested();
        AddCorrelation(facts,samples,"CPU load",s=>s.Cpu,sourceName);token.ThrowIfCancellationRequested();
        AddCorrelation(facts,samples,"GPU load",s=>s.Gpu,sourceName);token.ThrowIfCancellationRequested();
        AddCorrelation(facts,samples,"Disk I/O",s=>s.Disk,sourceName);token.ThrowIfCancellationRequested();
        AddCorrelation(facts,samples,"Thermal data",s=>s.Thermal,sourceName);token.ThrowIfCancellationRequested();
        var findings=spikes>0?[new Finding("Frame-time spikes observed",$"{spikes:N0} of {samples.Count:N0} samples crossed the documented threshold.","Compare the same workload and capture method. Correlation fields are descriptive and do not prove causation.","Calculated from imported frametime samples")]:Array.Empty<Finding>();
        return new("Stutter Analyzer",DateTimeOffset.UtcNow,facts,findings);
    }

    private static void AddCorrelation(List<Fact> facts,List<Sample> samples,string name,Func<Sample,double?> selector,string source)
    {
        var pairs=samples.Select(s=>(Frame:s.FrameTime,Metric:selector(s))).Where(p=>p.Metric.HasValue).Select(p=>(p.Frame,Metric:p.Metric!.Value)).ToArray();
        if(pairs.Length<10){facts.Add(new(name+" correlation","Unavailable",source,"Unavailable","Unavailable"));return;}
        var scale=pairs.Max(p=>Math.Abs(p.Metric));
        var xMean=pairs.Average(p=>p.Frame);var yMean=scale==0?0:pairs.Average(p=>p.Metric/scale);
        var numerator=0d;var xx=0d;var yy=0d;
        if(scale>0)foreach(var p in pairs){var dx=p.Frame-xMean;var dy=p.Metric/scale-yMean;numerator+=dx*dy;xx+=dx*dx;yy+=dy*dy;}
        var r=xx>0&&yy>0?numerator/(Math.Sqrt(xx)*Math.Sqrt(yy)):double.NaN;
        facts.Add(!double.IsFinite(r)?new(name+" correlation","Unavailable: zero variance or invalid calculation",source,"Calculated","Unavailable"):new(name+" correlation",Math.Clamp(r,-1,1).ToString("F4",CultureInfo.InvariantCulture),$"Pearson r across {pairs.Length:N0} paired samples · {source}","Calculated","Available"));
    }
    private static double Percentile(double[] ordered,double percentile){var position=(ordered.Length-1)*percentile;var lower=(int)Math.Floor(position);var upper=(int)Math.Ceiling(position);return lower==upper?ordered[lower]:ordered[lower]+(ordered[upper]-ordered[lower])*(position-lower);}
    private static double? Optional(string[] fields,int index,double min,double max)=>index>=0&&index<fields.Length&&Try(fields[index],out var value)&&value>=min&&value<=max?value:null;
    private static bool Try(string text,out double value)=>double.TryParse(text.Trim(),NumberStyles.Float,CultureInfo.InvariantCulture,out value)&&double.IsFinite(value);
    private static string Normalize(string value)=>new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    private static string[] Split(string line,int row)
    {
        if(line.Length>65536)throw new HbxException($"CSV row {row} exceeds the line size limit.");
        var fields=new List<string>();var value=new StringBuilder();bool quoted=false,closed=false;
        void Finish(){fields.Add(value.ToString().Trim());value.Clear();closed=false;if(fields.Count>256)throw new HbxException($"CSV row {row} has too many columns.");}
        for(int i=0;i<line.Length;i++)
        {
            char ch=line[i];
            if(quoted)
            {
                if(ch=='"'){if(i+1<line.Length&&line[i+1]=='"'){value.Append('"');i++;}else{quoted=false;closed=true;}}
                else value.Append(ch);
            }
            else if(ch==',')Finish();
            else if(ch=='"'&&!closed&&value.ToString().All(char.IsWhiteSpace)){value.Clear();quoted=true;}
            else if(closed&&char.IsWhiteSpace(ch))continue;
            else if(ch=='"'||closed)throw new HbxException($"Invalid CSV quoting at row {row}.");
            else value.Append(ch);
        }
        if(quoted)throw new HbxException($"Unclosed CSV field at row {row}; multiline fields are unsupported.");
        Finish();return fields.ToArray();
    }
}

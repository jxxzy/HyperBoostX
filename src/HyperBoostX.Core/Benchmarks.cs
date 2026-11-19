using System.Globalization;
using Microsoft.VisualBasic.FileIO;

namespace HyperBoostX.Core;

public sealed record BenchmarkEntry(string Id,DateTimeOffset CreatedAt,string Game,double AverageFps,double OnePercentLowFps,double AverageFrametimeMs,string Notes,string Source)
{
    public override string ToString()=>$"{Game} · {CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss} · {AverageFps:F1} FPS ({Source})";
}
public sealed class Benchmarks(DocumentStore store)
{
    public IReadOnlyList<BenchmarkEntry> History()=>store.Read("benchmarks",new List<BenchmarkEntry>()).OrderByDescending(x=>x.CreatedAt).ToArray();
    public static BenchmarkEntry Validate(string game,double average,double low,double frametime,string notes,string source="Manual input")
    {
        if(string.IsNullOrWhiteSpace(game)||game.Length>160||game.Any(char.IsControl))throw new HbxException("HBX-BENCH-0001: Enter a valid game or workload name.");
        if(!double.IsFinite(average)||!double.IsFinite(low)||!double.IsFinite(frametime)||average<=0||average>10000||low<=0||low>average||frametime<=0||frametime>60000)
            throw new HbxException("HBX-BENCH-0002: FPS/frametime must be finite and positive; 1% low cannot exceed average FPS.");
        if(notes.Length>4000)throw new HbxException("HBX-BENCH-0003: Notes exceed 4,000 characters.");
        return new(Guid.NewGuid().ToString("N"),DateTimeOffset.UtcNow,game.Trim(),average,low,frametime,notes,source);
    }
    public BenchmarkEntry Add(string game,double average,double low,double frametime,string notes)
    {
        var entry=Validate(game,average,low,frametime,notes);Save([entry]);return entry;
    }
    private void Save(IEnumerable<BenchmarkEntry> entries)=>store.Update("benchmarks",new List<BenchmarkEntry>(),history=>history.Concat(entries).TakeLast(200).ToList());
    public int ImportCsv(string content)
    {
        if(content.Length>4*1024*1024)throw new HbxException("HBX-BENCH-0004: CSV limit: 4 MiB.");
        using var parser=new TextFieldParser(new StringReader(content)){HasFieldsEnclosedInQuotes=true};parser.SetDelimiters(",");
        var header=parser.ReadFields()??throw new HbxException("HBX-BENCH-0005: CSV header is missing.");
        var required=new[]{"game","avg_fps","one_percent_low_fps","average_frametime_ms"};
        if(required.Any(key=>header.Count(h=>h.Equals(key,StringComparison.OrdinalIgnoreCase))!=1))throw new HbxException("HBX-BENCH-0005: CSV requires game,avg_fps,one_percent_low_fps,average_frametime_ms columns.");
        var pending=new List<BenchmarkEntry>();
        while(!parser.EndOfData)
        {
            var fields=parser.ReadFields()!;
            if(fields.Length!=header.Length)throw new HbxException("HBX-BENCH-0006: CSV row has an incorrect column count.");
            string Field(string name){var index=Array.FindIndex(header,h=>h.Equals(name,StringComparison.OrdinalIgnoreCase));return index<0?"":fields[index];}
            double Number(string name)=>double.TryParse(Field(name),NumberStyles.Float,CultureInfo.InvariantCulture,out var number)?number:throw new HbxException("HBX-BENCH-0007: CSV numbers use a decimal point.");
            pending.Add(Validate(Field("game"),Number("avg_fps"),Number("one_percent_low_fps"),Number("average_frametime_ms"),Field("notes"),"CSV import (user supplied)"));
            if(pending.Count>200)throw new HbxException("HBX-BENCH-0008: Import at most 200 benchmarks per file.");
        }
        Save(pending);return pending.Count;
    }
    public static DiagnosticReport Report(BenchmarkEntry entry)=>new("Benchmark Lab",entry.CreatedAt,
        [new("Game / workload",entry.Game,entry.Source,"User Reported"),new("Average FPS",entry.AverageFps.ToString("F2",CultureInfo.InvariantCulture),entry.Source,"User Reported"),
         new("1% low FPS",entry.OnePercentLowFps.ToString("F2",CultureInfo.InvariantCulture),entry.Source,"User Reported"),new("Average frametime (ms)",entry.AverageFrametimeMs.ToString("F3",CultureInfo.InvariantCulture),entry.Source,"User Reported"),
         new("Notes",entry.Notes,entry.Source,"User Reported")],[]);
    public static DiagnosticReport Compare(BenchmarkEntry before,BenchmarkEntry after)
    {
        if(!string.Equals(before.Game,after.Game,StringComparison.OrdinalIgnoreCase))throw new HbxException("HBX-BENCH-0009: Select results from the same game/workload.");
        return new("Benchmark comparison",DateTimeOffset.UtcNow,
            [new("Workload",before.Game,"Local history"),new("Before / after",$"{before.CreatedAt:O} / {after.CreatedAt:O}","Local history"),
             new("Average FPS change",$"{after.AverageFps-before.AverageFps:F2} FPS ({(after.AverageFps/before.AverageFps-1)*100:F2}%)","User supplied measurements","Derived"),
             new("1% low change",$"{after.OnePercentLowFps-before.OnePercentLowFps:F2} FPS","User supplied measurements","Derived"),
             new("Frametime change",$"{after.AverageFrametimeMs-before.AverageFrametimeMs:F3} ms","User supplied measurements","Derived"),
             new("Interpretation","Use matching settings, workload and capture method. Differences do not prove that a HyperBoostX change caused an improvement.","Methodology")],[]);
    }
}

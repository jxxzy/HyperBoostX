using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

// Formatted WDDM counters are snapshots, not live polling or vendor thermal data.
// The engine instances are per-process/per-engine; never sum them as total GPU usage.
public static class GpuCounters
{
    public const string EngineClass="Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine";
    public const string MemoryClass="Win32_PerfFormattedData_GPUPerformanceCounters_GPUAdapterMemory";
    public const int MaximumRows=400;
    private static readonly SemaphoreSlim Gate=new(1);
    public sealed record EngineSample(string Name,ulong? Percent);
    public sealed record MemorySample(string Name,ulong? DedicatedBytes,ulong? SharedBytes);

    public static IReadOnlyList<Fact> DescribeEngines(IEnumerable<EngineSample> samples)
    {
        var facts=new List<Fact>();
        var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var sample in samples.Take(MaximumRows))
        {
            if(!ValidName(sample.Name)||!seen.Add(sample.Name))continue;
            var valid=sample.Percent is <=100;
            facts.Add(new($"GPU engine · {sample.Name}",valid?$"{sample.Percent.GetValueOrDefault()}%":"Unavailable",
                EngineClass,valid?"Measured snapshot":"Unavailable",valid?"Available":"Unavailable"));
        }
        if(facts.Count==0)facts.Add(Missing("GPU engine utilization",EngineClass,"No named engine instances reported"));
        return facts;
    }

    public static IReadOnlyList<Fact> DescribeMemory(IEnumerable<MemorySample> samples)
    {
        var facts=new List<Fact>();
        var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var sample in samples.Take(MaximumRows))
        {
            if(!ValidName(sample.Name)||!seen.Add(sample.Name))continue;
            facts.Add(MemoryFact($"GPU dedicated memory usage · {sample.Name}",sample.DedicatedBytes));
            facts.Add(MemoryFact($"GPU shared memory usage · {sample.Name}",sample.SharedBytes));
        }
        if(facts.Count==0)facts.Add(Missing("GPU memory usage",MemoryClass,"No named adapter instances reported"));
        return facts;
    }

    private static bool ValidName(string name)=>!string.IsNullOrWhiteSpace(name)&&name.Length<=512&&
        !name.Any(char.IsControl)&&!name.Equals("_Total",StringComparison.OrdinalIgnoreCase)&&!name.Equals("Total",StringComparison.OrdinalIgnoreCase);
    private static Fact MemoryFact(string name,ulong? bytes)=>new(name,
        bytes is {} value?$"{value.ToString(CultureInfo.InvariantCulture)} bytes ({(value/1073741824d).ToString("F3",CultureInfo.InvariantCulture)} GiB)":"Unavailable",
        MemoryClass,bytes.HasValue?"Measured snapshot":"Unavailable",bytes.HasValue?"Available":"Unavailable");
    private static Fact Missing(string name,string source,string reason)=>new(name,reason,source,"Unavailable","Unavailable");
    private static ulong? ReadUInt(object? value)=>value switch
    {
        ulong u=>u,uint u=>u,ushort u=>u,byte u=>u,
        long i when i>=0=>(ulong)i,int i when i>=0=>(ulong)i,
        _=>null
    };
    private static bool ProviderError(Exception e)=>e is ManagementException or UnauthorizedAccessException
        or COMException or System.Security.SecurityException or InvalidOperationException;

    public static async Task<IReadOnlyList<Fact>> ScanAsync(CancellationToken token)
    {
        await Gate.WaitAsync(token);
        try
        {
            return await Task.Run<IReadOnlyList<Fact>>(()=>{
                var facts=new List<Fact>();
                var engines=new List<EngineSample>();
                try
                {
                    var truncated=Read(EngineClass,"Name,UtilizationPercentage",row=>
                        engines.Add(new(row["Name"] as string??"",ReadUInt(row["UtilizationPercentage"]))),token);
                    facts.AddRange(DescribeEngines(engines));
                    if(truncated)facts.Add(new("GPU engine coverage","First 400 instances; additional rows omitted",EngineClass,"Limited","Limited"));
                }
                catch(Exception e) when(ProviderError(e))
                {facts.Add(Missing("GPU engine utilization",EngineClass,$"Provider unavailable: {e.GetType().Name}"));}
                token.ThrowIfCancellationRequested();
                var memory=new List<MemorySample>();
                try
                {
                    var truncated=Read(MemoryClass,"Name,DedicatedUsage,SharedUsage",row=>
                        memory.Add(new(row["Name"] as string??"",ReadUInt(row["DedicatedUsage"]),ReadUInt(row["SharedUsage"]))),token);
                    facts.AddRange(DescribeMemory(memory));
                    if(truncated)facts.Add(new("GPU adapter coverage","First 400 instances; additional rows omitted",MemoryClass,"Limited","Limited"));
                }
                catch(Exception e) when(ProviderError(e))
                {facts.Add(Missing("GPU memory usage",MemoryClass,$"Provider unavailable: {e.GetType().Name}"));}
                facts.Add(new("GPU counter methodology","Per-process/engine and adapter snapshots; no aggregate utilization, VRAM capacity, device-name mapping or thermal data inferred.","Windows WDDM performance counters","Guidance"));
                return facts;
            },token);
        }
        finally{Gate.Release();}
    }

    private static bool Read(string className,string fields,Action<ManagementObject> consume,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var search=new ManagementObjectSearcher(new ManagementScope(@"root\cimv2"),
            new ObjectQuery($"SELECT {fields} FROM {className}"),
            new System.Management.EnumerationOptions{Timeout=TimeSpan.FromSeconds(8),ReturnImmediately=true,Rewindable=false});
        using var result=search.Get();
        var count=0;
        foreach(ManagementObject row in result)
        {
            using(row)
            {
                token.ThrowIfCancellationRequested();
                if(++count>MaximumRows)return true;
                consume(row);
            }
        }
        return false;
    }
}

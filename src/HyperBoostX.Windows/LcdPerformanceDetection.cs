using System.Diagnostics;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public sealed record LcdUtilitySignature(string Vendor,string Product,bool CoolingCritical,IReadOnlyList<string> Patterns);

public static class LcdPerformanceDetection
{
    private static readonly LcdUtilitySignature[] Signatures=
    [
        new("Corsair","iCUE",true,["icue","corsair.service"]),
        new("NZXT","CAM / Kraken",true,["nzxt cam","nzxtcam","kraken"]),
        new("Lian Li","L-Connect",true,["l-connect","lconnect"]),
        new("Thermaltake","TT RGB Plus",true,["ttrgbplus","tt rgb plus"]),
        new("ASUS","Armoury Crate",true,["armourycrate","armoury crate"]),
        new("MSI","MSI Center",true,["msicenter","msi center"]),
        new("AIDA64","SensorPanel",false,["aida64"]),
        new("HWiNFO","Sensor monitoring",false,["hwinfo"]),
        new("Logitech","G HUB display",false,["lghub"]),
    ];
    public static LcdUtilitySignature? Classify(string? value)=>string.IsNullOrWhiteSpace(value)?null:Signatures.FirstOrDefault(s=>s.Patterns.Any(p=>value.Contains(p,StringComparison.OrdinalIgnoreCase)));

    public static async Task<DiagnosticReport> ScanAsync(double cpuThreshold,long memoryThresholdMiB,bool gamingOnly,bool gamingActive,CancellationToken token)
    {
        if(cpuThreshold<0||cpuThreshold>100)throw new HbxException("CPU threshold must be between 0 and 100 percent.");
        if(memoryThresholdMiB<1||memoryThresholdMiB>1_048_576)throw new HbxException("Memory threshold must be between 1 MiB and 1 TiB.");
        var samples=new List<(int Id,string Name,LcdUtilitySignature Signature,TimeSpan Cpu,long Stamp)>();var facts=new List<Fact>();var findings=new List<Finding>();
        foreach(var process in Process.GetProcesses())using(process)
        {
            token.ThrowIfCancellationRequested();
            try{if(Classify(process.ProcessName) is {} signature)samples.Add((process.Id,process.ProcessName,signature,process.TotalProcessorTime,Stopwatch.GetTimestamp()));}
            catch(Exception e) when(e is InvalidOperationException or System.ComponentModel.Win32Exception){facts.Add(new("Process inspection issue",ReportExport.Redact(e.Message),"Process API","Unavailable","Unavailable"));}
        }
        if(samples.Count>0)await Task.Delay(500,token);
        foreach(var sample in samples)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                using var process=Process.GetProcessById(sample.Id);var elapsed=Stopwatch.GetElapsedTime(sample.Stamp);var cpu=Math.Clamp((process.TotalProcessorTime-sample.Cpu).TotalMilliseconds/(elapsed.TotalMilliseconds*Math.Max(1,Environment.ProcessorCount))*100,0,100);var memory=process.WorkingSet64/1048576d;
                var safety=sample.Signature.CoolingCritical?"Cooling/fan/pump exclusion enforced":"Monitor only";
                facts.Add(new(sample.Signature.Product,$"PID {sample.Id} · CPU {cpu:F2}% · working set {memory:F1} MiB · {safety}",$"Running process · {sample.Signature.Vendor}","Measured","Available"));
                var thresholdExceeded=cpu>=cpuThreshold||memory>=memoryThresholdMiB;
                if(thresholdExceeded&&(!gamingOnly||gamingActive))findings.Add(new($"LCD utility overhead threshold: {sample.Signature.Product}",$"CPU {cpu:F2}% (threshold {cpuThreshold:F2}%) · memory {memory:F1} MiB (threshold {memoryThresholdMiB} MiB)",sample.Signature.CoolingCritical?"Review the vendor utility manually. HyperBoostX will not stop a process that may control cooling, fans, or pumps.":"Review the utility's display refresh rate or sensor polling in its own settings. HyperBoostX did not stop it.","Two-sample process measurement"));
            }
            catch(Exception e) when(e is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception){facts.Add(new("LCD utility ended during sampling",ReportExport.Redact(e.Message),$"PID {sample.Id}","Unavailable","Unavailable"));}
        }
        if(samples.Count==0)facts.Insert(0,new("LCD utility processes","No supported signature observed","Running processes","Direct","Available"));
        facts.Insert(0,new("Guard policy",$"{(gamingOnly?"Gaming sessions only":"Always monitor")} · CPU ≥ {cpuThreshold:F2}% · memory ≥ {memoryThresholdMiB} MiB · cooling exclusions enforced","User-configured thresholds","Direct","Available"));
        if(gamingOnly&&!gamingActive)facts.Insert(1,new("Gaming guard","Inactive: no HyperBoostX gaming session is active","Gaming session state","Direct","Available"));
        return new("LCD Performance Guard",DateTimeOffset.UtcNow,facts,findings);
    }
}

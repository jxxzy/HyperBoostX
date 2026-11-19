using System.Diagnostics.Eventing.Reader;
using System.Management;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public sealed record HealthScoreResult(double Score,int AvailableComponents,int TotalComponents,IReadOnlyList<Fact> Facts,IReadOnlyList<Finding> Findings);
public sealed record HealthScoreEvidence(double? GpuReadiness,string GpuSource,double? StorageReadiness,string StorageSource,double? Reliability,string ReliabilitySource);

public static class SystemHealthScore
{
    public static HealthScoreResult Calculate(TelemetrySample sample)=>Calculate(sample,null);

    public static HealthScoreResult Calculate(TelemetrySample sample,HealthScoreEvidence? evidence)
    {
        ArgumentNullException.ThrowIfNull(sample);
        var components=new List<double>();var facts=new List<Fact>();
        if(sample.CpuAvailability==MeasurementAvailability.Available&&sample.CpuPercent is {} cpu&&double.IsFinite(cpu)&&cpu>=0&&cpu<=100){var value=Math.Clamp(100-cpu,0,100);components.Add(value);facts.Add(new("CPU component",$"{value:F1} / 100",$"GetSystemTimes interval · {cpu:F1}% measured activity","Calculated","Available"));}
        else facts.Add(new("CPU component","Not scored",$"GetSystemTimes · {sample.CpuAvailability}","Unavailable","Unavailable"));
        if(sample.TotalMemory>0&&sample.AvailableMemory<=sample.TotalMemory){var value=Math.Clamp(100-sample.MemoryPercent,0,100);components.Add(value);facts.Add(new("Memory component",$"{value:F1} / 100",$"GlobalMemoryStatusEx · {sample.MemoryPercent:F1}% measured use","Calculated","Available"));}
        else facts.Add(new("Memory component","Not scored","GlobalMemoryStatusEx returned no total memory","Unavailable","Unavailable"));
        if(sample.NetworkAvailability is MeasurementAvailability.Available or MeasurementAvailability.Limited){var value=sample.NetworkAvailability==MeasurementAvailability.Available?100d:60d;components.Add(value);facts.Add(new("Network component",$"{value:F1} / 100",$"NetworkInterface/IP statistics · {sample.NetworkStatus}","Calculated",sample.NetworkAvailability.ToString()));}
        else facts.Add(new("Network component","Not scored",$"NetworkInterface/IP statistics · {sample.NetworkAvailability}","Unavailable","Unavailable"));
        AddEvidence("GPU component",evidence?.GpuReadiness,evidence?.GpuSource);AddEvidence("Storage component",evidence?.StorageReadiness,evidence?.StorageSource);AddEvidence("Reliability component",evidence?.Reliability,evidence?.ReliabilitySource);
        var total=evidence is null?3:6;var score=components.Count==0?0:components.Average();
        facts.Insert(0,new("HyperBoost Score",components.Count==0?"Not scored":$"{score:F1} / 100",$"Equal-weight mean of {components.Count}/{total} available components; snapshot at {sample.ObservedAt:O}",components.Count==total?"Calculated":"Limited",components.Count==0?"Unavailable":components.Count==total?"Available":"Limited"));
        facts.Add(new("Method","CPU and memory are measured headroom; network is sampling readiness; GPU is Windows driver/device readiness; storage is system-volume free-space readiness; reliability is the bounded 24-hour System error-event rate. Missing components are excluded, never replaced with assumed values.","HBX-HEALTH-SCORE-2","Methodology","Available"));
        var findings=new List<Finding>();if(components.Count<total)findings.Add(new("Score coverage is incomplete",$"Only {components.Count} of {total} components were available. Compare scores only when coverage and method match.","Compare only like-for-like score coverage.","Measured"));
        return new(score,components.Count,total,facts,findings);
        void AddEvidence(string name,double? raw,string? source){if(raw is {} value&&double.IsFinite(value)&&value>=0&&value<=100){components.Add(value);facts.Add(new(name,$"{value:F1} / 100",source??"Windows provider","Calculated","Available"));}else facts.Add(new(name,"Not scored",source??"Windows provider unavailable","Unavailable","Unavailable"));}
    }

    public static async Task<DiagnosticReport> MeasureAsync(CancellationToken token)
    {
        var telemetry=new Telemetry();await Task.Run(telemetry.Sample,token);await Task.Delay(500,token);var sample=await Task.Run(telemetry.Sample,token);var result=Calculate(sample,await CollectEvidenceAsync(token));token.ThrowIfCancellationRequested();return new("HyperBoost Score",sample.ObservedAt,result.Facts,result.Findings);
    }

    private static Task<HealthScoreEvidence> CollectEvidenceAsync(CancellationToken token)=>Task.Run(()=>
    {
        token.ThrowIfCancellationRequested();double? gpu=null,storage=null,reliability=null;var gpuSource="Win32_VideoController";var storageSource="DriveInfo system volume";var reliabilitySource="System event log, level 1-2, trailing 24 hours";
        try{using var searcher=new ManagementObjectSearcher(new ManagementScope(@"root\cimv2"),new ObjectQuery("SELECT Status,DriverVersion FROM Win32_VideoController"),new System.Management.EnumerationOptions{Timeout=TimeSpan.FromSeconds(8),ReturnImmediately=true,Rewindable=false});using var results=searcher.Get();var count=0;var ready=0;var truncated=false;foreach(ManagementObject device in results){using(device){token.ThrowIfCancellationRequested();if(++count>64){truncated=true;break;}if(string.Equals(device["Status"]?.ToString(),"OK",StringComparison.OrdinalIgnoreCase)&&!string.IsNullOrWhiteSpace(device["DriverVersion"]?.ToString()))ready++;}}if(count>0&&!truncated){gpu=ready>0?100:60;gpuSource+=$" · {ready}/{count} reported ready";}else if(truncated)gpuSource+=" · inventory limit exceeded";}
        catch(Exception exception) when(exception is ManagementException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException){gpuSource+=$" unavailable: {exception.GetType().Name}";}
        try{var root=Path.GetPathRoot(Environment.SystemDirectory);var drive=DriveInfo.GetDrives().FirstOrDefault(item=>item.IsReady&&string.Equals(item.RootDirectory.FullName,root,StringComparison.OrdinalIgnoreCase));if(drive is not null&&drive.TotalSize>0){var free=100d*drive.AvailableFreeSpace/drive.TotalSize;storage=Math.Clamp(free*2,0,100);storageSource+=$" · {free:F1}% free";}}
        catch(Exception exception) when(exception is IOException or UnauthorizedAccessException){storageSource+=$" unavailable: {exception.GetType().Name}";}
        try{var cutoff=DateTime.UtcNow.AddHours(-24).ToString("o");var query=new EventLogQuery("System",PathType.LogName,$"*[System[(Level=1 or Level=2) and TimeCreated[@SystemTime >= '{cutoff}']]]") {ReverseDirection=true};using var reader=new EventLogReader(query);var errors=0;var start=System.Diagnostics.Stopwatch.StartNew();var complete=false;while(errors<20&&start.Elapsed<TimeSpan.FromSeconds(8)){token.ThrowIfCancellationRequested();using var entry=reader.ReadEvent(TimeSpan.FromSeconds(2));if(entry is null){complete=true;break;}errors++;}if(complete||errors==20){reliability=Math.Clamp(100-errors*5,0,100);reliabilitySource+=$" · {errors} error/critical event(s), capped at 20";}else reliabilitySource+=" · time limit reached";}
        catch(Exception exception) when(exception is EventLogException or UnauthorizedAccessException){reliabilitySource+=$" unavailable: {exception.GetType().Name}";}
        return new HealthScoreEvidence(gpu,gpuSource,storage,storageSource,reliability,reliabilitySource);
    },token);
}

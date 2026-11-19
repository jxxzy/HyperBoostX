using System.Windows;
using System.Windows.Media;
using System.IO;
using HyperBoostX.Core;
using HyperBoostX.Windows;

namespace HyperBoostX.App;

public sealed partial class ShellViewModel
{
    private readonly Telemetry _telemetry=new();
    private readonly Queue<double> _cpuPoints=new();
    private CancellationTokenSource? _telemetryStop;
    private TelemetrySample? _liveSample;
    private GpuCounters.LoadSample? _liveGpu;
    private string? _telemetryError;
    private DateTimeOffset _lastMemoryNotice;
    private bool _memoryGuard;
    private int _memoryGuardThresholdPercent=90;
    private int _memoryGuardCooldownMinutes=5;
    public string LiveCpu=>_liveSample?.CpuPercent is {} cpu?$"{cpu:F1}%":Localization[_liveSample is {} sample?AvailabilityKey(sample.CpuAvailability):"Sampling"];
    public string LiveGpu=>_liveGpu is {Percent:{} value} sample&&DateTimeOffset.UtcNow-sample.ObservedAt<=TimeSpan.FromSeconds(30)
        ?$"{value:F0}% · {Localization[AvailabilityKey(sample.Availability)]}"
        :Localization[_liveGpu is null?"Sampling":"Unavailable"];
    public string LiveGpuDetail=>_liveGpu is {} sample&&DateTimeOffset.UtcNow-sample.ObservedAt<=TimeSpan.FromSeconds(30)&&sample.Percent.HasValue
        ?$"{Localization["GpuBusiestEngine"]} · {sample.EngineCount} · {sample.ObservedAt.ToLocalTime():HH:mm:ss}"
        :Localization["GpuLoadUnavailableDetail"];
    public string LiveMemory=>_liveSample is {} s?$"{(s.TotalMemory-s.AvailableMemory)/1073741824d:F1} / {s.TotalMemory/1073741824d:F1} GiB ({s.MemoryPercent:F1}%)":Localization["Unavailable"];
    public string LiveMemoryPressure=>Localization[_liveSample?.LowMemorySignaled switch{true=>"MemoryLowSignaled",false=>"MemoryLowNotSignaled",_=>"MemoryPressureUnavailable"}];
    public string LiveCommit=>_liveSample is {} sample&&sample.CommitUsedBytes is {} used&&sample.CommitLimitBytes is {} limit
        ?$"{Localization["CommitCharge"]}: {used/1073741824d:F1} / {limit/1073741824d:F1} GiB"
        :$"{Localization["CommitCharge"]}: {Localization[_liveSample is null?"Sampling":AvailabilityKey(_liveSample.CommitAvailability)]}";
    public string LiveStorage
    {
        get
        {
            try
            {
                var root=Path.GetPathRoot(Environment.SystemDirectory);
                if(string.IsNullOrEmpty(root))return Localization["Unavailable"];
                var drive=new DriveInfo(root);
                if(!drive.IsReady||drive.TotalSize<=0)return Localization["Unavailable"];
                var available=Math.Clamp(drive.AvailableFreeSpace,0,drive.TotalSize);
                return $"{100d*(drive.TotalSize-available)/drive.TotalSize:F0}%";
            }
            catch(Exception exception) when(exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return Localization["Unavailable"];
            }
        }
    }
    public string LiveNetwork
    {
        get
        {
            if(_liveSample is not {} sample)return Localization["Sampling"];
            var rate=sample.ReceivedBytesPerSecond is {} rx&&sample.SentBytesPerSecond is {} tx
                ?$"↓ {rx/1024:F1} KiB/s · ↑ {tx/1024:F1} KiB/s"
                :null;
            var availability=Localization[AvailabilityKey(sample.NetworkAvailability)];
            return rate is null?availability:sample.NetworkAvailability==MeasurementAvailability.Limited?$"{rate} · {availability}":rate;
        }
    }
    public string LiveUptime=>_liveSample is {} s?TimeSpan.FromMilliseconds(s.UptimeMilliseconds).ToString(@"d\.hh\:mm\:ss"):Localization["Unavailable"];
    public string UptimeText=>$"{Localization["Uptime"]}: {LiveUptime}";
    public string TelemetryStatus
    {
        get
        {
            if(_telemetryError is not null)return $"{Localization["Limited"]}: {ReportExport.Redact(_telemetryError)}";
            if(_liveSample is not {} sample)return Localization["Sampling"];
            var network=Localization["Network"]+": "+Localization[AvailabilityKey(sample.NetworkAvailability)];
            if(sample.NetworkFailureCount>0)network+=$" ({sample.NetworkFailureCount})";
            var cpu=Localization["CPU"]+": "+Localization[AvailabilityKey(sample.CpuAvailability)];
            if(sample.CpuWin32Error is {} error)cpu+=$" (Win32 {error})";
            if(sample.CpuProcessorGroups!=1)cpu+=$" ({sample.CpuProcessorGroups} processor groups; aggregate unavailable)";
            var commit=Localization["CommitCharge"]+": "+Localization[AvailabilityKey(sample.CommitAvailability)];
            if(sample.CommitWin32Error is {} commitError)commit+=$" (Win32 {commitError})";
            var pressure=LiveMemoryPressure;if(sample.MemoryNotificationWin32Error is {} pressureError)pressure+=$" (Win32 {pressureError})";
            return $"{sample.ObservedAt.ToLocalTime():HH:mm:ss} · {network} · {cpu} · {commit} · {pressure}";
        }
    }
    private static string AvailabilityKey(MeasurementAvailability availability)=>availability switch
    {
        MeasurementAvailability.Available=>"Available",
        MeasurementAvailability.Limited=>"Limited",
        MeasurementAvailability.Unavailable=>"Unavailable",
        _=>"Sampling"
    };
    public PointCollection CpuChart {get;private set;}=[];
    public bool HasCpuSamples=>_cpuPoints.Count>1;
    public bool MemoryGuardEnabled{get=>_memoryGuard;set{Runtime.Journal.SetSetting("memory-guard",value.ToString());_memoryGuard=value;Notify();}}
    public int MemoryGuardThresholdPercent
    {
        get=>_memoryGuardThresholdPercent;
        set
        {
            var threshold=Math.Clamp(value,70,95);
            if(_memoryGuardThresholdPercent==threshold)return;
            _memoryGuardThresholdPercent=threshold;
            Runtime.Journal.SetSetting("memory-guard-threshold",threshold.ToString());
            Notify();Notify(nameof(MemoryGuardThresholdText));
        }
    }
    public string MemoryGuardThresholdText=>$"{Localization["MemoryGuardThreshold"]}: {MemoryGuardThresholdPercent}%";
    public int MemoryGuardCooldownMinutes
    {
        get=>_memoryGuardCooldownMinutes;
        set
        {
            var cooldown=Math.Clamp(value,1,60);
            if(_memoryGuardCooldownMinutes==cooldown)return;
            _memoryGuardCooldownMinutes=cooldown;
            Runtime.Journal.SetSetting("memory-guard-cooldown-minutes",cooldown.ToString());
            Notify();Notify(nameof(MemoryGuardCooldownText));
        }
    }
    public string MemoryGuardCooldownText=>$"{Localization["MemoryGuardCooldown"]}: {MemoryGuardCooldownMinutes} min";
    public static bool ShouldNotifyMemoryGuard(double memoryPercent,int thresholdPercent,DateTimeOffset lastNotice,DateTimeOffset now,int cooldownMinutes)
    {
        var threshold=Math.Clamp(thresholdPercent,70,95);
        var cooldown=Math.Clamp(cooldownMinutes,1,60);
        return memoryPercent>=threshold&&now-lastNotice>=TimeSpan.FromMinutes(cooldown);
    }
    public void StartTelemetry()
    {
        if(_telemetryStop!=null)return;
        _memoryGuard=bool.TryParse(Runtime.Journal.GetSetting("memory-guard"),out var enabled)&&enabled;Notify(nameof(MemoryGuardEnabled));
        _memoryGuardThresholdPercent=int.TryParse(Runtime.Journal.GetSetting("memory-guard-threshold"),out var threshold)?Math.Clamp(threshold,70,95):90;
        Notify(nameof(MemoryGuardThresholdPercent));Notify(nameof(MemoryGuardThresholdText));
        _memoryGuardCooldownMinutes=int.TryParse(Runtime.Journal.GetSetting("memory-guard-cooldown-minutes"),out var cooldown)?Math.Clamp(cooldown,1,60):5;
        Notify(nameof(MemoryGuardCooldownMinutes));Notify(nameof(MemoryGuardCooldownText));
        _telemetryStop=new CancellationTokenSource();_liveGpu=null;Notify(nameof(LiveGpu));Notify(nameof(LiveGpuDetail));_ = PollTelemetry(_telemetryStop.Token);_ = PollGpu(_telemetryStop.Token);
    }
    private async Task PollGpu(CancellationToken token)
    {
        try
        {
            while(!token.IsCancellationRequested)
            {
                if(IsDashboard&&!Busy)
                {
                    try
                    {
                        var sample=await GpuCounters.SampleLoadAsync(token);
                        token.ThrowIfCancellationRequested();
                        _liveGpu=sample;
                    }
                    catch(OperationCanceledException) when(token.IsCancellationRequested){break;}
                    catch(Exception){_liveGpu=new(DateTimeOffset.UtcNow,null,MeasurementAvailability.Unavailable,0,0);}
                    Notify(nameof(LiveGpu));Notify(nameof(LiveGpuDetail));
                }
                else if(_liveGpu is not null){_liveGpu=null;Notify(nameof(LiveGpu));Notify(nameof(LiveGpuDetail));}
                await Task.Delay(TimeSpan.FromSeconds(10),token);
            }
        }
        catch(OperationCanceledException) when(token.IsCancellationRequested){}
    }
    private async Task PollTelemetry(CancellationToken token)
    {
        try
        {
            using var timer=new PeriodicTimer(TimeSpan.FromSeconds(2));var ticks=0;
            while(await timer.WaitForNextTickAsync(token))
            {
                if(!IsDashboard&&!IsCpuOptimization&&!MemoryGuardEnabled){_telemetry.ResetBaseline();continue;}
                try
                {
                    var sample=await Task.Run(_telemetry.Sample,token);token.ThrowIfCancellationRequested();_liveSample=sample;_telemetryError=null;
                    if(sample.CpuPercent is {} cpu){_cpuPoints.Enqueue(cpu);while(_cpuPoints.Count>60)_cpuPoints.Dequeue();CpuChart=new PointCollection(_cpuPoints.Select((v,i)=>new Point(i*5,100-v)));}
                    var observedAt=DateTimeOffset.UtcNow;
                    if(MemoryGuardEnabled&&ShouldNotifyMemoryGuard(sample.MemoryPercent,MemoryGuardThresholdPercent,_lastMemoryNotice,observedAt,MemoryGuardCooldownMinutes))
                    {Status=$"Memory Guard: RAM {sample.MemoryPercent:F1}%. Simpan pekerjaan dan tinjau Processes. Tidak ada proses yang ditutup atau RAM yang dipurge otomatis.";_lastMemoryNotice=observedAt;}
                    if(++ticks%15==0)await Task.Run(()=>Runtime.Documents.Update("telemetry",new List<TelemetrySample>(),items=>items.Append(sample).TakeLast(600).ToList()),token);
                    foreach(var name in new[]{nameof(LiveCpu),nameof(LiveMemory),nameof(LiveMemoryPressure),nameof(LiveCommit),nameof(LiveGpu),nameof(LiveGpuDetail),nameof(LiveStorage),nameof(LiveNetwork),nameof(LiveUptime),nameof(TelemetryStatus),nameof(CpuChart),nameof(HasCpuSamples)})Notify(name);
                }
                catch(OperationCanceledException)when(token.IsCancellationRequested){break;}
                catch(Exception e){_liveSample=null;_telemetry.ResetBaseline();_telemetryError=ReportExport.Redact(e.Message);foreach(var name in new[]{nameof(LiveCpu),nameof(LiveMemory),nameof(LiveMemoryPressure),nameof(LiveCommit),nameof(LiveNetwork),nameof(LiveUptime),nameof(TelemetryStatus)})Notify(name);}
            }
        }
        catch(OperationCanceledException)when(token.IsCancellationRequested){}
    }
    public void StopTelemetry(){_telemetryStop?.Cancel();_telemetryStop?.Dispose();_telemetryStop=null;}
}

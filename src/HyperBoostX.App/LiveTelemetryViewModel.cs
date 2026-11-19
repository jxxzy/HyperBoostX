using System.Windows;
using System.Windows.Media;
using HyperBoostX.Core;
using HyperBoostX.Windows;

namespace HyperBoostX.App;

public sealed partial class ShellViewModel
{
    private readonly Telemetry _telemetry=new();
    private readonly Queue<double> _cpuPoints=new();
    private CancellationTokenSource? _telemetryStop;
    private TelemetrySample? _liveSample;
    private string? _telemetryError;
    private DateTimeOffset _lastMemoryNotice;
    private bool _memoryGuard;
    public string LiveCpu=>_liveSample?.CpuPercent is {} cpu?$"{cpu:F1}%":Localization[_liveSample?.CpuAvailability==MeasurementAvailability.Unavailable?"Unavailable":"Sampling"];
    public string LiveMemory=>_liveSample is {} s?$"{(s.TotalMemory-s.AvailableMemory)/1073741824d:F1} / {s.TotalMemory/1073741824d:F1} GiB ({s.MemoryPercent:F1}%)":Localization["Unavailable"];
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
            return $"{sample.ObservedAt.ToLocalTime():HH:mm:ss} · {network} · {cpu}";
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
    public void StartTelemetry()
    {
        if(_telemetryStop!=null)return;
        _memoryGuard=bool.TryParse(Runtime.Journal.GetSetting("memory-guard"),out var enabled)&&enabled;Notify(nameof(MemoryGuardEnabled));
        _telemetryStop=new CancellationTokenSource();_ = PollTelemetry(_telemetryStop.Token);
    }
    private async Task PollTelemetry(CancellationToken token)
    {
        try
        {
            using var timer=new PeriodicTimer(TimeSpan.FromSeconds(2));var ticks=0;
            while(await timer.WaitForNextTickAsync(token))
            {
                if(!IsDashboard&&!MemoryGuardEnabled){_telemetry.ResetBaseline();continue;}
                try
                {
                    var sample=await Task.Run(_telemetry.Sample,token);_liveSample=sample;_telemetryError=null;
                    if(sample.CpuPercent is {} cpu){_cpuPoints.Enqueue(cpu);while(_cpuPoints.Count>60)_cpuPoints.Dequeue();CpuChart=new PointCollection(_cpuPoints.Select((v,i)=>new Point(i*5,100-v)));}
                    if(MemoryGuardEnabled&&sample.MemoryPercent>=90&&DateTimeOffset.UtcNow-_lastMemoryNotice>TimeSpan.FromMinutes(5))
                    {Status=$"Memory Guard: RAM {sample.MemoryPercent:F1}%. Simpan pekerjaan dan tinjau Processes. Tidak ada proses yang ditutup atau RAM yang dipurge otomatis.";_lastMemoryNotice=DateTimeOffset.UtcNow;}
                    if(++ticks%15==0)await Task.Run(()=>Runtime.Documents.Update("telemetry",new List<TelemetrySample>(),items=>items.Append(sample).TakeLast(600).ToList()),token);
                    foreach(var name in new[]{nameof(LiveCpu),nameof(LiveMemory),nameof(LiveNetwork),nameof(LiveUptime),nameof(TelemetryStatus),nameof(CpuChart),nameof(HasCpuSamples)})Notify(name);
                }
                catch(OperationCanceledException)when(token.IsCancellationRequested){break;}
                catch(Exception e){_telemetryError=ReportExport.Redact(e.Message);Notify(nameof(TelemetryStatus));}
            }
        }
        catch(OperationCanceledException)when(token.IsCancellationRequested){}
    }
    public void StopTelemetry(){_telemetryStop?.Cancel();_telemetryStop?.Dispose();_telemetryStop=null;}
}

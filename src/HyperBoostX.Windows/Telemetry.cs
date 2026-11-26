using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using HyperBoostX.Core;
using Microsoft.Win32.SafeHandles;

namespace HyperBoostX.Windows;

public enum MeasurementAvailability{Sampling,Available,Limited,Unavailable}
public sealed record TelemetrySample(DateTimeOffset ObservedAt,double? CpuPercent,ulong TotalMemory,ulong AvailableMemory,double? ReceivedBytesPerSecond,double? SentBytesPerSecond,long UptimeMilliseconds,string NetworkStatus)
{
    public MeasurementAvailability CpuAvailability{get;init;}=MeasurementAvailability.Sampling;
    public int? CpuWin32Error{get;init;}
    public MeasurementAvailability NetworkAvailability{get;init;}=MeasurementAvailability.Sampling;
    public int NetworkFailureCount{get;init;}
    public ulong? CommitUsedBytes{get;init;}
    public ulong? CommitLimitBytes{get;init;}
    public MeasurementAvailability CommitAvailability{get;init;}=MeasurementAvailability.Unavailable;
    public int? CommitWin32Error{get;init;}
    public bool? LowMemorySignaled{get;init;}
    public int? MemoryNotificationWin32Error{get;init;}
    public int CpuProcessorGroups{get;init;}=1;
    public double MemoryPercent=>TotalMemory==0?0:100d*(TotalMemory-AvailableMemory)/TotalMemory;
}
public sealed record MemoryPressureAssessment(double? PhysicalPercent,double? CommitPercent,bool? WindowsLowMemory,int ThresholdPercent)
{
    public bool PhysicalHigh=>PhysicalPercent is double value&&value>=ThresholdPercent;
    public bool CommitHigh=>CommitPercent is double value&&value>=ThresholdPercent;
    public bool ShouldAlert=>PhysicalHigh||CommitHigh||WindowsLowMemory==true;

    public static MemoryPressureAssessment FromSample(TelemetrySample sample,int thresholdPercent)
    {
        ArgumentNullException.ThrowIfNull(sample);
        double? physical=sample.TotalMemory>0&&sample.AvailableMemory<=sample.TotalMemory
            ?100d*(sample.TotalMemory-sample.AvailableMemory)/sample.TotalMemory:null;
        double? commit=sample.CommitAvailability==MeasurementAvailability.Available&&
            sample.CommitUsedBytes is ulong used&&sample.CommitLimitBytes is ulong limit&&limit>0&&used<=limit
            ?100d*used/limit:null;
        return new(physical,commit,sample.LowMemorySignaled,Math.Clamp(thresholdPercent,70,95));
    }
}

public sealed class MemoryGuardEvidence
{
    private DateTimeOffset? _first;
    private DateTimeOffset? _last;
    private int _count;
    private int _threshold;
    public void Reset(){_first=null;_last=null;_count=0;}
    public DiagnosticReport? Observe(TelemetrySample sample,int thresholdPercent)
    {
        var pressure=MemoryPressureAssessment.FromSample(sample,thresholdPercent);
        if(!pressure.ShouldAlert){Reset();return null;}
        if(_last is {} previous&&(sample.ObservedAt<=previous||sample.ObservedAt-previous>TimeSpan.FromSeconds(6)||_threshold!=pressure.ThresholdPercent))Reset();
        _first??=sample.ObservedAt;_last=sample.ObservedAt;_threshold=pressure.ThresholdPercent;
        if(_count<int.MaxValue)_count++;
        if(_count<3&&pressure.WindowsLowMemory!=true)return null;
        Fact Percent(string name,double? value)=>new(name,value is double v?FormattableString.Invariant($"{v:F2}%"):"Unavailable","Windows telemetry","Direct",value.HasValue?"Available":"Unavailable");
        return new DiagnosticReport("Memory Guard pressure evidence",sample.ObservedAt,
            [Percent("Physical memory usage",pressure.PhysicalPercent),Percent("Commit charge usage",pressure.CommitPercent),
             new("Windows low-memory signal",pressure.WindowsLowMemory?.ToString()??"Unavailable","Windows memory resource notification","Direct",pressure.WindowsLowMemory.HasValue?"Available":"Unavailable"),
             new("Threshold",pressure.ThresholdPercent.ToString(System.Globalization.CultureInfo.InvariantCulture)+"%","User setting"),
             new("Consecutive pressure observations",_count.ToString(System.Globalization.CultureInfo.InvariantCulture),"Memory Guard"),
             new("First observation",_first.Value.ToString("O"),"Windows telemetry"),
             new("Latest observation",sample.ObservedAt.ToString("O"),"Windows telemetry"),
             new("Observation span",FormattableString.Invariant($"{(sample.ObservedAt-_first.Value).TotalSeconds:F2} seconds"),"Sample timestamps"),
             new("Response","Review workload and memory usage; save work before closing applications. No processes stopped, memory trimmed, or system configuration changed.","Safety policy")],[]);
    }
}

public sealed class Telemetry
{
    private (ulong Idle,ulong Kernel,ulong User)? _cpu;
    private readonly Dictionary<string,(long Received,long Sent)> _network=[];
    private long _tick;
    public static (ulong UsedBytes,ulong LimitBytes) ConvertCommitPages(ulong usedPages,ulong limitPages,ulong pageSizeBytes)
    {
        if(pageSizeBytes==0)throw new ArgumentOutOfRangeException(nameof(pageSizeBytes));
        return(checked(usedPages*pageSizeBytes),checked(limitPages*pageSizeBytes));
    }
    public static double? CpuUsage((ulong Idle,ulong Kernel,ulong User) before,(ulong Idle,ulong Kernel,ulong User) after)
    {
        if(after.Kernel<before.Kernel||after.User<before.User||after.Idle<before.Idle)return null;
        var total=(after.Kernel-before.Kernel)+(after.User-before.User);var idle=after.Idle-before.Idle;
        return total==0?null:Math.Clamp(100d*(total-Math.Min(total,idle))/total,0,100);
    }
    public void ResetBaseline(){_cpu=null;_network.Clear();_tick=0;}
    public TelemetrySample Sample()
    {
        var memory=new MemoryStatus{Length=(uint)Marshal.SizeOf<MemoryStatus>()};
        if(!GlobalMemoryStatusEx(ref memory))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        if(memory.TotalPhysical==0||memory.AvailablePhysical>memory.TotalPhysical)throw new HbxException("HBX-TELEMETRY-MEMORY: Invalid physical-memory counters.");
        double? usage=null;var cpuAvailability=MeasurementAvailability.Sampling;int? cpuWin32Error=null;
        var processorGroups=GetActiveProcessorGroupCount();
        if(processorGroups!=1){_cpu=null;cpuAvailability=MeasurementAvailability.Limited;}
        else if(GetSystemTimes(out var idle,out var kernel,out var user))
        {
            var current=(idle.Value,kernel.Value,user.Value);
            if(_cpu is {} previous){usage=CpuUsage(previous,current);if(usage.HasValue)cpuAvailability=MeasurementAvailability.Available;}
            _cpu=current;
        }
        else{cpuWin32Error=Marshal.GetLastWin32Error();_cpu=null;cpuAvailability=MeasurementAvailability.Unavailable;}
        var now=Stopwatch.GetTimestamp();var seconds=_tick==0?0:Stopwatch.GetElapsedTime(_tick,now).TotalSeconds;_tick=now;
        long receive=0,send=0;var sampled=0;var failures=0;var seen=new HashSet<string>();
        foreach(var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if(adapter.OperationalStatus!=OperationalStatus.Up||adapter.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)continue;
            try
            {
                var statistics=adapter.GetIPStatistics();var current=(statistics.BytesReceived,statistics.BytesSent);seen.Add(adapter.Id);
                if(_network.TryGetValue(adapter.Id,out var previous)&&current.BytesReceived>=previous.Received&&current.BytesSent>=previous.Sent)
                {receive+=current.BytesReceived-previous.Received;send+=current.BytesSent-previous.Sent;sampled++;}
                _network[adapter.Id]=current;
            }
            catch(NetworkInformationException){failures++;}
        }
        foreach(var key in _network.Keys.Where(k=>!seen.Contains(k)).ToArray())_network.Remove(key);
        var networkAvailability=failures>0&&(sampled>0||seen.Count==0)
            ?MeasurementAvailability.Limited
            :seen.Count==0?MeasurementAvailability.Unavailable
            :sampled>0?MeasurementAvailability.Available:MeasurementAvailability.Sampling;
        var commit=ReadCommitUsage(out var commitWin32Error);
        var lowMemory=ReadLowMemorySignal(out var memoryNotificationError);
        return new TelemetrySample(DateTimeOffset.UtcNow,usage,memory.TotalPhysical,memory.AvailablePhysical,seconds>0&&sampled>0?receive/seconds:null,seconds>0&&sampled>0?send/seconds:null,Environment.TickCount64,
            failures>0?$"Limited: {failures} adapter(s) unavailable":seen.Count==0?"No active supported adapter":"Active interfaces; aggregate traffic may include virtual adapters")
        {CpuAvailability=cpuAvailability,CpuWin32Error=cpuWin32Error,NetworkAvailability=networkAvailability,NetworkFailureCount=failures,
            CommitUsedBytes=commit?.UsedBytes,CommitLimitBytes=commit?.LimitBytes,CommitAvailability=commit is null?MeasurementAvailability.Unavailable:MeasurementAvailability.Available,CommitWin32Error=commitWin32Error,LowMemorySignaled=lowMemory,MemoryNotificationWin32Error=memoryNotificationError,CpuProcessorGroups=processorGroups};
    }
    private static bool? ReadLowMemorySignal(out int? win32Error)
    {
        using var signal=CreateMemoryResourceNotification(0); // LowMemoryResourceNotification
        if(signal.IsInvalid){win32Error=Marshal.GetLastWin32Error();return null;}
        if(!QueryMemoryResourceNotification(signal,out var lowMemory))
        {win32Error=Marshal.GetLastWin32Error();return null;}
        win32Error=null;return lowMemory;
    }
    [DllImport("kernel32.dll",SetLastError=true)]private static extern SafeWaitHandle CreateMemoryResourceNotification(int type);
    [DllImport("kernel32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool QueryMemoryResourceNotification(SafeWaitHandle signal,[MarshalAs(UnmanagedType.Bool)]out bool state);
    [DllImport("kernel32.dll")]private static extern ushort GetActiveProcessorGroupCount();
    private static CommitUsage? ReadCommitUsage(out int? win32Error)
    {
        var info=new PerformanceInformation{Size=(uint)Marshal.SizeOf<PerformanceInformation>()};
        if(!GetPerformanceInfo(ref info,info.Size)){win32Error=Marshal.GetLastWin32Error();return null;}
        win32Error=null;
        try
        {
            var bytes=ConvertCommitPages((ulong)info.CommitTotal,(ulong)info.CommitLimit,(ulong)info.PageSize);
            return bytes.LimitBytes==0||bytes.UsedBytes>bytes.LimitBytes?null:new CommitUsage(bytes.UsedBytes,bytes.LimitBytes);
        }
        catch(ArgumentOutOfRangeException){return null;}
        catch(OverflowException){return null;}
    }
    public sealed record CommitUsage(ulong UsedBytes,ulong LimitBytes);
    [StructLayout(LayoutKind.Sequential)]private struct FileTime{public uint Low,High;public readonly ulong Value=>((ulong)High<<32)|Low;}
    [StructLayout(LayoutKind.Sequential)]private struct MemoryStatus{public uint Length,Load;public ulong TotalPhysical,AvailablePhysical,TotalPageFile,AvailablePageFile,TotalVirtual,AvailableVirtual,AvailableExtended;}
    [StructLayout(LayoutKind.Sequential)]private struct PerformanceInformation
    {
        public uint Size;
        public nuint CommitTotal,CommitLimit,CommitPeak,PhysicalTotal,PhysicalAvailable,SystemCache,KernelTotal,KernelPaged,KernelNonpaged,PageSize;
        public uint HandleCount,ProcessCount,ThreadCount;
    }
    [DllImport("kernel32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool GetSystemTimes(out FileTime idle,out FileTime kernel,out FileTime user);
    [DllImport("kernel32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
    [DllImport("psapi.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool GetPerformanceInfo(ref PerformanceInformation info,uint size);
}

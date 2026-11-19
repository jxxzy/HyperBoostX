using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public enum MeasurementAvailability{Sampling,Available,Limited,Unavailable}
public sealed record TelemetrySample(DateTimeOffset ObservedAt,double? CpuPercent,ulong TotalMemory,ulong AvailableMemory,double? ReceivedBytesPerSecond,double? SentBytesPerSecond,long UptimeMilliseconds,string NetworkStatus)
{
    public MeasurementAvailability CpuAvailability{get;init;}=MeasurementAvailability.Sampling;
    public int? CpuWin32Error{get;init;}
    public MeasurementAvailability NetworkAvailability{get;init;}=MeasurementAvailability.Sampling;
    public int NetworkFailureCount{get;init;}
    public double MemoryPercent=>TotalMemory==0?0:100d*(TotalMemory-AvailableMemory)/TotalMemory;
}
public sealed class Telemetry
{
    private (ulong Idle,ulong Kernel,ulong User)? _cpu;
    private readonly Dictionary<string,(long Received,long Sent)> _network=[];
    private long _tick;
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
        double? usage=null;var cpuAvailability=MeasurementAvailability.Sampling;int? cpuWin32Error=null;
        if(GetSystemTimes(out var idle,out var kernel,out var user))
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
        return new TelemetrySample(DateTimeOffset.UtcNow,usage,memory.TotalPhysical,memory.AvailablePhysical,seconds>0&&sampled>0?receive/seconds:null,seconds>0&&sampled>0?send/seconds:null,Environment.TickCount64,
            failures>0?$"Limited: {failures} adapter(s) unavailable":seen.Count==0?"No active supported adapter":"Active interfaces; aggregate traffic may include virtual adapters")
        {CpuAvailability=cpuAvailability,CpuWin32Error=cpuWin32Error,NetworkAvailability=networkAvailability,NetworkFailureCount=failures};
    }
    [StructLayout(LayoutKind.Sequential)]private struct FileTime{public uint Low,High;public readonly ulong Value=>((ulong)High<<32)|Low;}
    [StructLayout(LayoutKind.Sequential)]private struct MemoryStatus{public uint Length,Load;public ulong TotalPhysical,AvailablePhysical,TotalPageFile,AvailablePageFile,TotalVirtual,AvailableVirtual,AvailableExtended;}
    [DllImport("kernel32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool GetSystemTimes(out FileTime idle,out FileTime kernel,out FileTime user);
    [DllImport("kernel32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
}

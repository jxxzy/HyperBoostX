using System.Diagnostics;
using System.Management;
using System.Security;
using Microsoft.Win32;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public sealed record RgbSoftwareSignature(string Vendor,string Product,IReadOnlyList<string> Patterns);

public static class VendorSoftwareDetection
{
    private static readonly RgbSoftwareSignature[] RgbSignatures=
    [
        new("Corsair","iCUE",["icue","corsair.service"]),
        new("ASUS","Armoury Crate / Aura",["armoury crate","armourycrate","lightingservice","aura service"]),
        new("MSI","MSI Center / Mystic Light",["msi center","msicenter","mystic light","ledkeeper","lightkeeperservice"]),
        new("Razer","Synapse / Chroma",["razer synapse","razersynapse","razer chroma","razersynapseservice"]),
        new("Logitech","G HUB",["logitech g hub","lghub"]),
        new("OpenRGB","OpenRGB",["openrgb"]),
        new("Whirlwind FX","SignalRGB",["signalrgb","whirlwindfx"]),
        new("GIGABYTE","RGB Fusion",["rgb fusion","rgbfusion"]),
        new("ASRock","Polychrome RGB",["polychrome","asrpolychrome"]),
        new("NZXT","CAM",["nzxt cam","nzxtcam"]),
        new("Cooler Master","MasterPlus+",["masterplus","coolermaster masterplus"]),
        new("Lian Li","L-Connect",["l-connect","lconnect","l-connect 3","lconnect3"])
    ];

    public static RgbSoftwareSignature? ClassifyRgb(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))return null;
        return RgbSignatures.FirstOrDefault(signature=>signature.Patterns.Any(pattern=>value.Contains(pattern,StringComparison.OrdinalIgnoreCase)));
    }

    public static RgbSoftwareSignature? ClassifyRgbProcess(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))return null;
        var name=value.Trim();if(name.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))name=name[..^4];
        return RgbSignatures.FirstOrDefault(s=>s.Patterns.Any(p=>name.Equals(p,StringComparison.OrdinalIgnoreCase)));
    }

    public static Task<DiagnosticReport> ScanRgbAsync(CancellationToken token)=>Task.Run(()=>ScanRgb(token),token);

    private static async Task<DiagnosticReport> ScanRgb(CancellationToken token)
    {
        var observations=new List<(RgbSoftwareSignature Signature,string Source,string Identity,string State)>();var issues=new List<Fact>();
        var processSamples=new List<(int ProcessId,RgbSoftwareSignature Signature,string Identity,DateTime Started,TimeSpan Cpu,long Timestamp)>();
        void Observe(string source,string identity,string state){var signature=ClassifyRgb(identity);if(signature is not null)observations.Add((signature,source,identity,state));}
        foreach(var process in Process.GetProcesses())
        {
            using(process)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    var signature=ClassifyRgbProcess(process.ProcessName);
                    if(signature is not null)processSamples.Add((process.Id,signature,process.ProcessName,process.StartTime.ToUniversalTime(),process.TotalProcessorTime,Stopwatch.GetTimestamp()));
                }
                catch(Exception exception) when(exception is InvalidOperationException or System.ComponentModel.Win32Exception){issues.Add(new("Process inspection issue",ReportExport.Redact(exception.Message),"System.Diagnostics.Process","Unavailable","Unavailable"));}
            }
        }
        if(processSamples.Count>0)
        {
            await Task.Delay(500,token);
            foreach(var sample in processSamples)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    using var process=Process.GetProcessById(sample.ProcessId);var started=process.StartTime.ToUniversalTime();var after=process.TotalProcessorTime;var elapsed=Stopwatch.GetElapsedTime(sample.Timestamp);
                    var cpu=OverlaySoftwareDetection.MeasureCpuPercent(sample.Started,started,sample.Cpu,after,elapsed,Environment.ProcessorCount);
                    if(cpu is null||!string.Equals(process.ProcessName,sample.Identity,StringComparison.OrdinalIgnoreCase))
                    {issues.Add(new("RGB process sample rejected","Process identity changed or CPU sample was inconsistent",$"PID {sample.ProcessId}","Unavailable","Unavailable"));continue;}
                    observations.Add((sample.Signature,"Running process",sample.Identity,$"Running · PID {sample.ProcessId} · interval CPU {cpu.Value:F2}% · working set {FileAnalysisFormatting.Size(process.WorkingSet64)}"));
                }
                catch(Exception exception) when(exception is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
                {issues.Add(new("RGB process ended during sampling",ReportExport.Redact(exception.Message),$"PID {sample.ProcessId}","Unavailable","Unavailable"));}
            }
        }
        try
        {
            using var searcher=new ManagementObjectSearcher(new ManagementScope(@"\\.\root\cimv2"),new ObjectQuery("SELECT Name,DisplayName,State,PathName FROM Win32_Service"),new System.Management.EnumerationOptions{ReturnImmediately=false,Rewindable=false,Timeout=TimeSpan.FromSeconds(10)});using var rows=searcher.Get();
            foreach(ManagementObject row in rows){using(row){token.ThrowIfCancellationRequested();var name=$"{row["Name"]} {row["DisplayName"]} {row["PathName"]}";Observe("Windows service",name,$"{row["State"]??"Unknown"} · {row["DisplayName"]??row["Name"]}");}}
        }
        catch(Exception exception) when(exception is ManagementException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
        {issues.Add(new("Service inventory unavailable",ReportExport.Redact(exception.Message),"Win32_Service","Unavailable","Unavailable"));}
        foreach(var hive in new[]{RegistryHive.LocalMachine,RegistryHive.CurrentUser})foreach(var view in new[]{RegistryView.Registry64,RegistryView.Registry32})
        {
            token.ThrowIfCancellationRequested();
            try
            {
                using var baseKey=RegistryKey.OpenBaseKey(hive,view);using var uninstall=baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");if(uninstall is null)continue;
                foreach(var subName in uninstall.GetSubKeyNames()){token.ThrowIfCancellationRequested();using var sub=uninstall.OpenSubKey(subName);var display=sub?.GetValue("DisplayName")?.ToString();if(display is not null)Observe("Installed application",display,$"Installed · {display}");}
            }
            catch(Exception exception) when(exception is UnauthorizedAccessException or SecurityException or IOException)
            {issues.Add(new("Application inventory issue",ReportExport.Redact(exception.Message),$"{hive} {view} uninstall registry","Unavailable","Unavailable"));}
        }
        var distinct=observations.GroupBy(item=>$"{item.Signature.Vendor}|{item.Signature.Product}|{item.Source}|{item.Identity}",StringComparer.OrdinalIgnoreCase).Select(group=>group.First()).OrderBy(item=>item.Signature.Vendor).ThenBy(item=>item.Signature.Product).ToArray();
        var facts=distinct.Select(item=>new Fact(item.Signature.Product,item.State,$"{item.Source} · {item.Signature.Vendor}","Direct","Available")).Concat(issues).ToList();
        if(distinct.Length==0)facts.Insert(0,new("RGB control software",issues.Count==0?"No supported signature observed":"No supported signature observed in incomplete inventory","Processes, Win32_Service and uninstall registry",issues.Count==0?"Direct":"Limited",issues.Count==0?"Available":"Limited"));
        var activeControllers=distinct.Where(item=>(item.Source=="Running process"||item.Source=="Windows service")&&item.State.StartsWith("Running",StringComparison.OrdinalIgnoreCase)).Select(item=>item.Signature.Product).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var findings=activeControllers.Length>1?new[]{new Finding("Multiple RGB controllers observed",string.Join(", ",activeControllers),"Review vendor utilities for overlapping device ownership. HyperBoostX did not stop or reconfigure them.","Direct process/service inventory")}:Array.Empty<Finding>();
        return new("RGB Software Detector",DateTimeOffset.UtcNow,facts,findings);
    }
}

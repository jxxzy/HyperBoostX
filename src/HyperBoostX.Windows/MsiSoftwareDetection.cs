using System.Diagnostics;
using System.Management;
using System.Security;
using Microsoft.Win32;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public static class MsiSoftwareDetection
{
    private static readonly string[] Patterns=["msi center","msicenter","mystic light","mysticlight","ledkeeper","lightkeeperservice","msi.centralserver","msi companion","dragon center"];
    private static readonly string[] CoolingPatterns=["fan","cooler","cooling","pump","frozr","thermal","service"];
    public static bool IsMsiUtility(string? value)=>!string.IsNullOrWhiteSpace(value)&&Patterns.Any(pattern=>value.Contains(pattern,StringComparison.OrdinalIgnoreCase));
    public static bool IsCoolingCritical(string? value)=>!string.IsNullOrWhiteSpace(value)&&CoolingPatterns.Any(pattern=>value.Contains(pattern,StringComparison.OrdinalIgnoreCase));
    // Win32_Service does not expose a Dependencies property. Read the SCM's
    // configured dependency lists instead; failure is distinct from an empty list.
    public static string DescribeDependencies(string[]? services,string[]? groups)
    {
        if(services is null||groups is null)return "Unavailable";
        var values=services.Where(s=>!string.IsNullOrWhiteSpace(s)).Select(s=>"service: "+s)
            .Concat(groups.Where(s=>!string.IsNullOrWhiteSpace(s)).Select(s=>"group: "+s)).ToArray();
        return values.Length==0?"none configured":string.Join(", ",values);
    }
    private static string ReadDependencies(string? name)
    {
        if(string.IsNullOrWhiteSpace(name)||name.Contains('\\')||name.Contains('/'))return "Unavailable";
        try
        {
            using var key=Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\"+name);
            if(key is null)return "Unavailable";
            static string[]? Read(RegistryKey key,string value)=>key.GetValue(value) switch
            {
                null=>Array.Empty<string>(),string[] items=>items,_=>null
            };
            return DescribeDependencies(Read(key,"DependOnService"),Read(key,"DependOnGroup"));
        }
        catch(Exception e) when(e is UnauthorizedAccessException or SecurityException or IOException){return "Unavailable";}
    }
    public static async Task<DiagnosticReport> ScanAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var facts=new List<Fact>();var samples=new List<(int Id,string Name,DateTime Started,TimeSpan Cpu,long Stamp)>();int services=0,installed=0;
        foreach(var process in Process.GetProcesses())using(process)
        {
            token.ThrowIfCancellationRequested();
            try{if(IsMsiUtility(process.ProcessName))samples.Add((process.Id,process.ProcessName,process.StartTime.ToUniversalTime(),process.TotalProcessorTime,Stopwatch.GetTimestamp()));}
            catch(Exception e) when(e is InvalidOperationException or System.ComponentModel.Win32Exception){facts.Add(new("Process inspection issue",ReportExport.Redact(e.Message),"Process API","Unavailable","Unavailable"));}
        }
        if(samples.Count>0)await Task.Delay(500,token);
        foreach(var sample in samples)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                using var process=Process.GetProcessById(sample.Id);
                var started=process.StartTime.ToUniversalTime();var after=process.TotalProcessorTime;var elapsed=Stopwatch.GetElapsedTime(sample.Stamp);
                var measured=OverlaySoftwareDetection.MeasureCpuPercent(sample.Started,started,sample.Cpu,after,elapsed,Environment.ProcessorCount);
                if(measured is not double cpu||!string.Equals(process.ProcessName,sample.Name,StringComparison.OrdinalIgnoreCase))
                {facts.Add(new("MSI process sample rejected","Process identity changed or CPU interval was invalid",$"PID {sample.Id}","Unavailable","Unavailable"));continue;}
                facts.Add(new(sample.Name,$"Running · PID {sample.Id} · CPU {cpu:F2}% · working set {FileAnalysisFormatting.Size(process.WorkingSet64)} · protected from termination","MSI process signature","Measured","Available"));
            }
            catch(Exception e) when(e is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception){facts.Add(new("MSI process ended during sampling",ReportExport.Redact(e.Message),$"PID {sample.Id}","Unavailable","Unavailable"));}
        }
        try
        {
            token.ThrowIfCancellationRequested();
            using var searcher=new ManagementObjectSearcher(new ManagementScope(@"root\cimv2"),new ObjectQuery("SELECT Name,DisplayName,State,StartMode,PathName FROM Win32_Service"),new System.Management.EnumerationOptions{Timeout=TimeSpan.FromSeconds(8),ReturnImmediately=false,Rewindable=false});
            using var rows=searcher.Get();
            token.ThrowIfCancellationRequested();
            foreach(ManagementObject row in rows)using(row)
            {
                token.ThrowIfCancellationRequested();var identity=$"{row["Name"]} {row["DisplayName"]} {row["PathName"]}";if(!IsMsiUtility(identity))continue;services++;var dependencies=ReadDependencies(row["Name"]?.ToString());var exclusion=IsCoolingCritical(identity)||IsCoolingCritical(dependencies)?"cooling/fan/pump exclusion enforced":"protected from automatic changes";
                facts.Add(new(row["DisplayName"]?.ToString()??row["Name"]?.ToString()??"MSI service",$"{row["State"]} · start {row["StartMode"]} · dependencies {dependencies} · {exclusion}","Win32_Service / SCM registry",dependencies=="Unavailable"?"Limited":"Direct",dependencies=="Unavailable"?"Limited":"Available"));
            }
        }
        catch(Exception e) when(e is ManagementException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException){facts.Add(new("MSI service inventory","Unavailable",$"Win32_Service: {e.GetType().Name}","Unavailable","Unavailable"));}
        foreach(var hive in new[]{RegistryHive.LocalMachine,RegistryHive.CurrentUser})foreach(var view in new[]{RegistryView.Registry64,RegistryView.Registry32})
        {
            try{using var root=RegistryKey.OpenBaseKey(hive,view);using var uninstall=root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");if(uninstall is null)continue;foreach(var subName in uninstall.GetSubKeyNames()){token.ThrowIfCancellationRequested();using var sub=uninstall.OpenSubKey(subName);var name=sub?.GetValue("DisplayName")?.ToString();if(!IsMsiUtility(name))continue;installed++;facts.Add(new(name!,$"Installed · version {sub?.GetValue("DisplayVersion")??"Unavailable"}",$"{hive} {view} uninstall registry","Direct","Available"));}}
            catch(Exception e) when(e is UnauthorizedAccessException or SecurityException or IOException){facts.Add(new("MSI application inventory","Unavailable",$"{hive} {view}: {e.GetType().Name}","Unavailable","Unavailable"));}
        }
        token.ThrowIfCancellationRequested();
        if(samples.Count+services+installed==0)facts.Insert(0,new("MSI utilities",facts.Count==0?"No supported MSI Center, Mystic Light, or Dragon Center signature observed":"No supported signature observed in incomplete inventory","Processes, Win32_Service, uninstall registry",facts.Count==0?"Direct":"Limited",facts.Count==0?"Available":"Limited"));
        facts.Insert(0,new("Safety policy","Monitor only · service/process termination disabled · cooling, fan, pump, thermal, and security components excluded","HyperBoostX MSI guard","Direct","Available"));
        return new("MSI Safe Optimizer",DateTimeOffset.UtcNow,facts,[]);
    }
}

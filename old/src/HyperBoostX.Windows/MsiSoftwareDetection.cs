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
    public static async Task<DiagnosticReport> ScanAsync(CancellationToken token)
    {
        var facts=new List<Fact>();var samples=new List<(int Id,string Name,TimeSpan Cpu,long Stamp)>();int services=0,installed=0;
        foreach(var process in Process.GetProcesses())using(process)
        {
            token.ThrowIfCancellationRequested();
            try{if(IsMsiUtility(process.ProcessName))samples.Add((process.Id,process.ProcessName,process.TotalProcessorTime,Stopwatch.GetTimestamp()));}
            catch(Exception e) when(e is InvalidOperationException or System.ComponentModel.Win32Exception){facts.Add(new("Process inspection issue",ReportExport.Redact(e.Message),"Process API","Unavailable","Unavailable"));}
        }
        if(samples.Count>0)await Task.Delay(500,token);
        foreach(var sample in samples)
        {
            try{using var process=Process.GetProcessById(sample.Id);var elapsed=Stopwatch.GetElapsedTime(sample.Stamp);var cpu=Math.Clamp((process.TotalProcessorTime-sample.Cpu).TotalMilliseconds/(elapsed.TotalMilliseconds*Math.Max(1,Environment.ProcessorCount))*100,0,100);facts.Add(new(sample.Name,$"Running · PID {sample.Id} · CPU {cpu:F2}% · working set {FileAnalysisFormatting.Size(process.WorkingSet64)} · protected from termination","MSI process signature","Measured","Available"));}
            catch(Exception e) when(e is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception){facts.Add(new("MSI process ended during sampling",ReportExport.Redact(e.Message),$"PID {sample.Id}","Unavailable","Unavailable"));}
        }
        try
        {
            using var searcher=new ManagementObjectSearcher("SELECT Name,DisplayName,State,StartMode,Dependencies,PathName FROM Win32_Service");using var rows=searcher.Get();
            foreach(ManagementObject row in rows)using(row)
            {
                token.ThrowIfCancellationRequested();var identity=$"{row["Name"]} {row["DisplayName"]} {row["PathName"]}";if(!IsMsiUtility(identity))continue;services++;var dependencies=row["Dependencies"] as string[]??[];var exclusion=IsCoolingCritical(identity)||dependencies.Any(IsCoolingCritical)?"cooling/fan/pump exclusion enforced":"protected from automatic changes";
                facts.Add(new(row["DisplayName"]?.ToString()??row["Name"]?.ToString()??"MSI service",$"{row["State"]} · start {row["StartMode"]} · dependencies {(dependencies.Length==0?"none reported":string.Join(", ",dependencies))} · {exclusion}","Win32_Service","Direct","Available"));
            }
        }
        catch(Exception e) when(e is ManagementException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException){facts.Add(new("MSI service inventory","Unavailable",$"Win32_Service: {e.GetType().Name}","Unavailable","Unavailable"));}
        foreach(var hive in new[]{RegistryHive.LocalMachine,RegistryHive.CurrentUser})foreach(var view in new[]{RegistryView.Registry64,RegistryView.Registry32})
        {
            try{using var root=RegistryKey.OpenBaseKey(hive,view);using var uninstall=root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");if(uninstall is null)continue;foreach(var subName in uninstall.GetSubKeyNames()){token.ThrowIfCancellationRequested();using var sub=uninstall.OpenSubKey(subName);var name=sub?.GetValue("DisplayName")?.ToString();if(!IsMsiUtility(name))continue;installed++;facts.Add(new(name!,$"Installed · version {sub?.GetValue("DisplayVersion")??"Unavailable"}",$"{hive} {view} uninstall registry","Direct","Available"));}}
            catch(Exception e) when(e is UnauthorizedAccessException or SecurityException or IOException){facts.Add(new("MSI application inventory","Unavailable",$"{hive} {view}: {e.GetType().Name}","Unavailable","Unavailable"));}
        }
        if(samples.Count+services+installed==0)facts.Insert(0,new("MSI utilities","No supported MSI Center, Mystic Light, or Dragon Center signature observed","Processes, Win32_Service, uninstall registry","Direct","Available"));
        facts.Insert(0,new("Safety policy","Monitor only · service/process termination disabled · cooling, fan, pump, thermal, and security components excluded","HyperBoostX MSI guard","Direct","Available"));
        return new("MSI Safe Optimizer",DateTimeOffset.UtcNow,facts,[]);
    }
}

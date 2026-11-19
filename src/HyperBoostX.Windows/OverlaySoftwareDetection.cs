using System.Diagnostics;
using Microsoft.Win32;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public sealed record OverlaySoftwareSignature(string Vendor,string Product,IReadOnlyList<string> Patterns);

public static class OverlaySoftwareDetection
{
    private static readonly OverlaySoftwareSignature[] Signatures=
    [
        new("Discord","Discord Overlay",["discord"]),
        new("Valve","Steam Overlay",["gameoverlayui","steam"]),
        new("NVIDIA","NVIDIA Overlay",["nvidia share","nvsphelper64","geforce experience","nvidia app"]),
        new("AMD","AMD Software Overlay",["radeonsoftware","amdow","amdrsserv","amddvr","amd software"]),
        new("RivaTuner","RTSS",["rtss","rivatuner","msiafterburner"]),
        new("Microsoft","Xbox Game Bar",["gamebar","gamebarftserver","xbox game bar","xboxgamebar"]),
        new("OBS Project","OBS",["obs64","obs studio"]),
        new("Overwolf","Overwolf",["overwolf"]),
        new("Medal","Medal",["medal"]),
        new("SteelSeries","SteelSeries GG / Moments",["steelseries gg","steelseriesgg","steelseries moments"])
    ];

    public static OverlaySoftwareSignature? Classify(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))return null;
        return Signatures.FirstOrDefault(signature=>signature.Patterns.Any(pattern=>value.Contains(pattern,StringComparison.OrdinalIgnoreCase)));
    }

    public static Task<DiagnosticReport> ScanAsync(CancellationToken token)=>Task.Run(()=>Scan(token),token);

    private static async Task<DiagnosticReport> Scan(CancellationToken token)
    {
        var samples=new List<(int Id,OverlaySoftwareSignature Signature,string Identity,TimeSpan Cpu,long Timestamp)>();var installed=new List<(OverlaySoftwareSignature Signature,string Name)>();var issues=new List<Fact>();
        foreach(var process in Process.GetProcesses())using(process)
        {
            token.ThrowIfCancellationRequested();
            try{var signature=Classify(process.ProcessName);if(signature is not null)samples.Add((process.Id,signature,process.ProcessName,process.TotalProcessorTime,Stopwatch.GetTimestamp()));}
            catch(Exception exception) when(exception is InvalidOperationException or System.ComponentModel.Win32Exception){issues.Add(new("Process inspection issue",ReportExport.Redact(exception.Message),"System.Diagnostics.Process","Unavailable","Unavailable"));}
        }
        foreach(var hive in new[]{RegistryHive.LocalMachine,RegistryHive.CurrentUser})foreach(var view in new[]{RegistryView.Registry64,RegistryView.Registry32})
        {
            try
            {
                using var baseKey=RegistryKey.OpenBaseKey(hive,view);using var uninstall=baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");if(uninstall is null)continue;
                foreach(var subName in uninstall.GetSubKeyNames()){token.ThrowIfCancellationRequested();using var sub=uninstall.OpenSubKey(subName);var name=sub?.GetValue("DisplayName")?.ToString();var signature=Classify(name);if(name is not null&&signature is not null)installed.Add((signature,name));}
            }
            catch(Exception exception) when(exception is UnauthorizedAccessException or System.Security.SecurityException or IOException){issues.Add(new("Application inventory issue",ReportExport.Redact(exception.Message),$"{hive} {view} uninstall registry","Unavailable","Unavailable"));}
        }
        var facts=new List<Fact>();double combinedCpu=0;long combinedWorkingSet=0;var activeProducts=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if(samples.Count>0)
        {
            await Task.Delay(500,token);
            foreach(var sample in samples)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    using var process=Process.GetProcessById(sample.Id);var elapsed=Stopwatch.GetElapsedTime(sample.Timestamp);var cpuDelta=process.TotalProcessorTime-sample.Cpu;
                    var cpu=elapsed.TotalMilliseconds<=0?0:Math.Clamp(cpuDelta.TotalMilliseconds/(elapsed.TotalMilliseconds*Math.Max(1,Environment.ProcessorCount))*100,0,100);var workingSet=process.WorkingSet64;
                    combinedCpu+=cpu;combinedWorkingSet=checked(combinedWorkingSet+workingSet);activeProducts.Add(sample.Signature.Product);
                    facts.Add(new(sample.Signature.Product,$"Running · PID {sample.Id} · interval CPU {cpu:F2}% · working set {FileAnalysisFormatting.Size(workingSet)}",$"Process · {sample.Signature.Vendor}; presence does not prove overlay enabled","Measured","Available"));
                }
                catch(Exception exception) when(exception is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception){issues.Add(new("Overlay process ended during sampling",ReportExport.Redact(exception.Message),$"PID {sample.Id}","Unavailable","Unavailable"));}
            }
        }
        foreach(var item in installed.DistinctBy(item=>$"{item.Signature.Product}|{item.Name}",StringComparer.OrdinalIgnoreCase))facts.Add(new(item.Signature.Product,$"Installed · {item.Name}",$"Uninstall registry · {item.Signature.Vendor}","Direct","Available"));
        facts.Add(new("Combined observed process load",$"CPU {combinedCpu:F2}% · working set {FileAnalysisFormatting.Size(combinedWorkingSet)}","Single 500 ms interval across recognized running processes; correlation does not prove performance impact","Measured","Available"));
        facts.AddRange(issues);
        if(samples.Count==0&&installed.Count==0)facts.Insert(0,new("Overlay-capable software","No supported signature observed","Processes and uninstall registry","Direct","Available"));
        var findings=activeProducts.Count>1?new[]{new Finding("Multiple overlay-capable processes observed",string.Join(", ",activeProducts),"Review overlay settings inside each application and compare controlled game sessions. HyperBoostX did not disable any overlay.","Direct process inventory and measured resource snapshot")}:Array.Empty<Finding>();
        return new("Overlay Conflict Detector",DateTimeOffset.UtcNow,facts,findings);
    }
}

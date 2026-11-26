using System.Diagnostics;
using Microsoft.Win32;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public sealed record OverlaySoftwareSignature(string Vendor,string Product,IReadOnlyList<string> Patterns);

public static class OverlaySoftwareDetection
{
    private static readonly OverlaySoftwareSignature[] Signatures=
    [
        new("Discord","Discord Overlay",["discordhookhelper","discord"]),
        new("Valve","Steam Overlay",["gameoverlayui64","gameoverlayui","steam"]),
        new("NVIDIA","NVIDIA Broadcast",["nvidia broadcast","nvidiabroadcast"]),
        new("NVIDIA","NVIDIA Overlay",["nvidia share","nvsphelper64","nvsphelper","nvidia overlay","geforce experience","nvidia app","nvidiaapp","shadowplay"]),
        new("AMD","AMD Software Overlay",["radeonsoftware","amdow","amdrsserv","amddvr","amd software","radeon overlay","radeon relive"]),
        new("Intel","Intel Arc Overlay-capable Software",["intel arc control","intelarccontrol","arc control","arccontrol","intel overlay"]),
        new("RivaTuner","RTSS",["rtss","rtsshooksloader","rivatuner","msiafterburner"]),
        new("Microsoft","Xbox Game Bar",["gamebar","gamebarftserver","gamebarpresencewriter","xbox game bar","xboxgamebar"]),
        new("OBS Project","OBS",["obs64","obs32","obs studio"]),
        new("TikTok","TikTok LIVE Studio",["tiktok live studio","tiktoklivestudio"]),
        new("Overwolf","Overwolf",["overwolf"]),
        new("Medal","Medal",["medal"]),
        new("SteelSeries","SteelSeries GG / Moments",["steelseries gg","steelseriesgg","steelseries moments"])
    ];

    public static OverlaySoftwareSignature? Classify(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))return null;
        return Signatures.FirstOrDefault(signature=>signature.Patterns.Any(pattern=>value.Contains(pattern,StringComparison.OrdinalIgnoreCase)));
    }

    // Process names are exact aliases; installed display names may contain version suffixes.
    // Do not attribute arbitrary processes containing short tokens such as "steam" or "medal".
    public static OverlaySoftwareSignature? ClassifyProcess(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))return null;
        var name=value.Trim();
        if(name.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))name=name[..^4];
        if(name.Equals("obs",StringComparison.OrdinalIgnoreCase))return Signatures.Single(s=>s.Product=="OBS");
        return Signatures.FirstOrDefault(s=>s.Patterns.Any(p=>name.Equals(p,StringComparison.OrdinalIgnoreCase)));
    }

    public static double? MeasureCpuPercent(DateTime startBefore,DateTime startAfter,TimeSpan before,TimeSpan after,TimeSpan elapsed,int logicalProcessors)
    {
        if(startBefore!=startAfter||before<TimeSpan.Zero||after<before||elapsed<=TimeSpan.Zero||logicalProcessors<=0)return null;
        var percent=(after-before).TotalMilliseconds/elapsed.TotalMilliseconds/logicalProcessors*100;
        return double.IsFinite(percent)&&percent>=0&&percent<=100?percent:null;
    }

    public static Task<DiagnosticReport> ScanAsync(CancellationToken token)=>Task.Run(()=>Scan(token),token);

    private static async Task<DiagnosticReport> Scan(CancellationToken token)
    {
        var samples=new List<(int Id,OverlaySoftwareSignature Signature,string Identity,DateTime Started,TimeSpan Cpu,long Timestamp)>();var installed=new List<(OverlaySoftwareSignature Signature,string Name)>();var issues=new List<Fact>();
        foreach(var process in Process.GetProcesses())using(process)
        {
            token.ThrowIfCancellationRequested();
            try{var identity=process.ProcessName;var signature=ClassifyProcess(identity);if(signature is not null)samples.Add((process.Id,signature,identity,process.StartTime.ToUniversalTime(),process.TotalProcessorTime,Stopwatch.GetTimestamp()));}
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
        var facts=new List<Fact>();double combinedCpu=0;long combinedWorkingSet=0;int measuredCount=0;var activeProducts=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if(samples.Count>0)
        {
            await Task.Delay(500,token);
            foreach(var sample in samples)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    using var process=Process.GetProcessById(sample.Id);
                    var started=process.StartTime.ToUniversalTime();var after=process.TotalProcessorTime;var elapsed=Stopwatch.GetElapsedTime(sample.Timestamp);
                    var cpu=MeasureCpuPercent(sample.Started,started,sample.Cpu,after,elapsed,Environment.ProcessorCount);
                    if(cpu is null||!string.Equals(process.ProcessName,sample.Identity,StringComparison.OrdinalIgnoreCase))
                    {
                        issues.Add(new("Overlay process sample rejected","Process identity changed or CPU sample was inconsistent",$"PID {sample.Id}","Unavailable","Unavailable"));
                        continue;
                    }
                    var workingSet=process.WorkingSet64;
                    combinedCpu+=cpu.Value;combinedWorkingSet=checked(combinedWorkingSet+workingSet);measuredCount++;activeProducts.Add(sample.Signature.Product);
                    facts.Add(new(sample.Signature.Product,$"Running · PID {sample.Id} · interval CPU {cpu.Value:F2}% · working set {FileAnalysisFormatting.Size(workingSet)}",$"Process · {sample.Signature.Vendor}; interval {elapsed.TotalMilliseconds:F0} ms; presence does not prove overlay enabled","Measured","Available"));
                }
                catch(Exception exception) when(exception is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception){issues.Add(new("Overlay process ended during sampling",ReportExport.Redact(exception.Message),$"PID {sample.Id}","Unavailable","Unavailable"));}
            }
        }
        foreach(var item in installed.DistinctBy(item=>$"{item.Signature.Product}|{item.Name}",StringComparer.OrdinalIgnoreCase))facts.Add(new(item.Signature.Product,$"Installed · {item.Name}",$"Uninstall registry · {item.Signature.Vendor}","Direct","Available"));
        var coverage=measuredCount==samples.Count&&issues.All(i=>i.Name!="Process inspection issue")&&combinedCpu<=100?"Available":"Limited";
        facts.Add(measuredCount==0
            ?new("Combined observed process load","No valid process measurements","Process sampling; absence of measurements is not zero usage","Unavailable","Unavailable")
            :new("Combined observed process load",$"CPU {combinedCpu:F2}% · working set {FileAnalysisFormatting.Size(combinedWorkingSet)} · {measuredCount}/{samples.Count} sampled","Sum of observed per-process CPU intervals (not a synchronized total); shared working-set pages may be counted more than once; correlation does not prove performance impact","Measured",coverage));
        facts.AddRange(issues);
        if(samples.Count==0&&installed.Count==0)facts.Insert(0,new("Overlay-capable software",issues.Count==0?"No supported signature observed":"No supported signature observed in incomplete inventory","Processes and uninstall registry",issues.Count==0?"Direct":"Limited",issues.Count==0?"Available":"Limited"));
        var findings=activeProducts.Count>1?new[]{new Finding("Multiple overlay-capable processes observed",string.Join(", ",activeProducts),"Review overlay settings inside each application and compare controlled game sessions. HyperBoostX did not disable any overlay.","Direct process inventory and measured resource snapshot")}:Array.Empty<Finding>();
        return new("Overlay Conflict Detector",DateTimeOffset.UtcNow,facts,findings);
    }
}

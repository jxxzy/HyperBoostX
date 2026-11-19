using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security;
using System.Xml.Linq;
using Microsoft.Win32;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public sealed class Diagnostics
{
    public static readonly string[] Centers=["Dashboard","Smart Scan","Optimize","Gaming","Processes","Hardware","GPU","Storage","Network","Startup","Apps","Devices","Security","Windows","Power","Creator","Reliability"];
    private static readonly SemaphoreSlim WmiGate=new(2);
    public Task<DiagnosticReport> ScanAsync(string center,CancellationToken token)=>Task.Run(async()=>
    {
        var facts=new List<Fact>();var findings=new List<Finding>();
        void Add(string n,object? v,string s,string q="Direct")=>facts.Add(new(n,v?.ToString()??"Unavailable",s,q,v==null?"Unavailable":"Available"));
        async Task Query(string scope,string query,params string[] fields)
        {
            await WmiGate.WaitAsync(token);
            try
            {
                using var search=new ManagementObjectSearcher(new ManagementScope(scope),new ObjectQuery(query),new System.Management.EnumerationOptions { Timeout=TimeSpan.FromSeconds(8),ReturnImmediately=false,Rewindable=false });
                using var result=search.Get();var count=0;
                foreach(ManagementObject row in result)
                {
                    using(row)
                    {
                        token.ThrowIfCancellationRequested();if(++count>400){Add("Inventory limit","First 400 entries shown",query);break;}
                        var name=fields.Length>0?Format(row[fields[0]]):$"Entry {count}";
                        if(fields.Length>0 && row[fields[0]] is bool)Add(fields[0],row[fields[0]],query);
                        foreach(var field in fields.Skip(1))Add($"{name} · {field}",row[field] is {} value?Format(value):null,query);
                    }
                }
                if(count==0)Add("Provider result","No entries reported",query);
            }
            catch(Exception e) when(e is ManagementException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
            { facts.Add(new("Provider unavailable",e.Message,query,"Unavailable","Unavailable")); }
            finally{WmiGate.Release();}
        }
        token.ThrowIfCancellationRequested();
        switch(center)
        {
            case "Dashboard": case "Smart Scan":
                await Query(@"root\cimv2","SELECT Caption,Version,BuildNumber,LastBootUpTime,FreePhysicalMemory,TotalVisibleMemorySize FROM Win32_OperatingSystem","Caption","Version","BuildNumber","LastBootUpTime","FreePhysicalMemory","TotalVisibleMemorySize");
                await Query(@"root\cimv2","SELECT Name,NumberOfCores,NumberOfLogicalProcessors,LoadPercentage FROM Win32_Processor","Name","NumberOfCores","NumberOfLogicalProcessors","LoadPercentage");
                Memory(facts);Drives(facts,findings);Power(facts);PendingReboot(facts,findings);
                foreach(var action in RegistryAction.CreateAll().Where(a=>a.Info.Id.EndsWith("disable",StringComparison.Ordinal)))
                {
                    try
                    {
                        var state=await action.ReadAsync("",token);Add(action.Info.Name,state,"HKCU Registry");
                        if(state.Value=="1")findings.Add(new(action.Info.Name,state.ToString(),action.Info.Description,"Direct configuration evidence; optional preference",action.Info.Id));
                    }
                    catch(HbxException e){Add(action.Info.Name,null,e.Message);}
                }
                Add("Network",NetworkInterface.GetIsNetworkAvailable()?"Online interface available":"Offline Mode","NetworkInterface");
                Add("Safety Guard",Runtime.EnvironmentBlock()??"Enabled · registered actions only","Runtime policy");
                if(center=="Smart Scan")
                {
                    await Query(@"root\cimv2","SELECT Name,ConfigManagerErrorCode FROM Win32_PnPEntity WHERE ConfigManagerErrorCode <> 0","Name","ConfigManagerErrorCode");
                    await Query(@"root\Microsoft\Windows\Defender","SELECT AMServiceEnabled,AntivirusEnabled,RealTimeProtectionEnabled,AntivirusSignatureLastUpdated FROM MSFT_MpComputerStatus","AMServiceEnabled","AntivirusEnabled","RealTimeProtectionEnabled","AntivirusSignatureLastUpdated");
                }
                break;
            case "Optimize": case "Gaming":
                foreach(var action in RegistryAction.CreateAll().Where(a=>a.Info.Center==center&&a.Info.Id.EndsWith("disable",StringComparison.Ordinal)))
                    try{Add(action.Info.Name,await action.ReadAsync("",token),action.Info.Source);}catch(Exception e) when(e is not OperationCanceledException){facts.Add(new(action.Info.Name,e.Message,action.Info.Source,"Unavailable","Unavailable"));}
                Power(facts);Memory(facts);
                if(center=="Gaming")
                {
                    RunningApps(facts,["steam","epic","rockstar","ea","ubisoft","xbox","obs","discord","rtss","nvidia","radeon","vgtray"]);
                    Add("FPS / 1% low",null,"No validated frame capture provider");
                    Add("Overlay detection","A running process does not prove its overlay is enabled.","Detection method","Heuristic");
                }
                break;
            case "Hardware":
                await Query(@"root\cimv2","SELECT Manufacturer,Model,TotalPhysicalMemory,PCSystemType FROM Win32_ComputerSystem","Model","Manufacturer","TotalPhysicalMemory","PCSystemType");
                await Query(@"root\cimv2","SELECT Name,NumberOfCores,NumberOfLogicalProcessors,CurrentClockSpeed,MaxClockSpeed,LoadPercentage FROM Win32_Processor","Name","NumberOfCores","NumberOfLogicalProcessors","CurrentClockSpeed","MaxClockSpeed","LoadPercentage");
                await Query(@"root\cimv2","SELECT Manufacturer,Product FROM Win32_BaseBoard","Product","Manufacturer");
                await Query(@"root\cimv2","SELECT DeviceLocator,Capacity,Speed,ConfiguredClockSpeed,Manufacturer FROM Win32_PhysicalMemory","DeviceLocator","Capacity","Speed","ConfiguredClockSpeed","Manufacturer");
                Memory(facts);Add("CPU temperature",null,"No validated temperature provider");
                break;
            case "GPU":
                await Query(@"root\cimv2","SELECT Name,DriverVersion,DriverDate,VideoProcessor,CurrentHorizontalResolution,CurrentVerticalResolution,CurrentRefreshRate,ConfigManagerErrorCode FROM Win32_VideoController","Name","DriverVersion","DriverDate","VideoProcessor","CurrentHorizontalResolution","CurrentVerticalResolution","CurrentRefreshRate","ConfigManagerErrorCode");
                facts.AddRange(await GpuCounters.ScanAsync(token));
                Add("VRAM capacity",null,"Win32_VideoController.AdapterRAM is not reliable for modern VRAM sizes; dedicated usage is not capacity");
                Add("GPU temperature / power",null,"No validated vendor sensor provider");
                break;
            case "Storage":
                Drives(facts,findings);
                await Query(@"root\cimv2","SELECT Model,Size,InterfaceType,MediaType,Status FROM Win32_DiskDrive","Model","Size","InterfaceType","MediaType","Status");
                await Query(@"root\Microsoft\Windows\Storage","SELECT FriendlyName,MediaType,BusType,HealthStatus,OperationalStatus,Size FROM MSFT_PhysicalDisk","FriendlyName","MediaType","BusType","HealthStatus","OperationalStatus","Size");
                Add("Health interpretation","Provider health codes describe reported device status, not a full SMART/NVMe diagnostic.","Methodology");
                break;
            case "Processes":
                var processes=Process.GetProcesses();var samples=new List<(Process Process,int Id,string Name,long Memory,TimeSpan Cpu,long SampledAt)>();var unavailable=0;
                try
                {
                    foreach(var process in processes)
                    {
                        token.ThrowIfCancellationRequested();
                        try{samples.Add((process,process.Id,process.ProcessName,process.WorkingSet64,process.TotalProcessorTime,Stopwatch.GetTimestamp()));}
                        catch(System.ComponentModel.Win32Exception){unavailable++;}
                        catch(InvalidOperationException){unavailable++;}
                    }
                    if(samples.Count>0)await Task.Delay(500,token);
                    foreach(var item in samples.OrderByDescending(x=>x.Memory).Take(250))
                    {
                        token.ThrowIfCancellationRequested();
                        try
                        {
                            var currentCpu=item.Process.TotalProcessorTime;var elapsed=Stopwatch.GetElapsedTime(item.SampledAt);
                            var cpu=ProcessCpuPercent(item.Cpu,currentCpu,elapsed,Environment.ProcessorCount);
                            Add($"{item.Name} · PID {item.Id}",$"Working set {item.Process.WorkingSet64/1048576d:F1} MiB · CPU {(cpu is {} percent?$"{percent:F1}%":"Unavailable")} over {elapsed.TotalMilliseconds:F0} ms","Process API · sampled");
                        }
                        catch(System.ComponentModel.Win32Exception){unavailable++;}
                        catch(InvalidOperationException){unavailable++;}
                    }
                }
                finally{foreach(var process in processes)process.Dispose();}
                Add("Methodology","Largest 250 processes by working set; CPU is sampled across two observations and normalized to logical processors, not a continuous sensor.","Process API");
                if(unavailable>0)Add("Unavailable processes",unavailable,"Protected or exited processes","Unavailable");break;
            case "Network":
                foreach(var adapter in NetworkInterface.GetAllNetworkInterfaces())
                {
                    token.ThrowIfCancellationRequested();var ip=adapter.GetIPProperties();
                    Add(adapter.Name,$"{adapter.NetworkInterfaceType} · {adapter.OperationalStatus} · link {adapter.Speed/1000000d:F0} Mbps","NetworkInterface");
                    Add(adapter.Name+" · DNS",string.Join(", ",ip.DnsAddresses),"IPInterfaceProperties");
                    Add(adapter.Name+" · Gateway",string.Join(", ",ip.GatewayAddresses.Select(g=>g.Address)),"IPInterfaceProperties");
                    Add(adapter.Name+" · IP",string.Join(", ",ip.UnicastAddresses.Select(a=>a.Address)),"IPInterfaceProperties");
                }
                Add("Latency","Use Measure ping to sample a host. No network setting is changed.","Methodology");break;
            case "Startup":
                foreach(var entry in StartupEntries())Add(entry.Name,$"{entry.Scope} · {entry.Command} · {(entry.Protected?"Protected":"Review before disabling")}",entry.Source);
                foreach(var folder in new[]{Environment.SpecialFolder.Startup,Environment.SpecialFolder.CommonStartup})
                {
                    var path=Environment.GetFolderPath(folder);if(Directory.Exists(path))foreach(var file in Directory.EnumerateFiles(path).Take(200))Add(Path.GetFileName(file),folder.ToString(),"Startup folder");
                }
                await Query(@"root\cimv2","SELECT Name,DisplayName,State,StartMode FROM Win32_Service WHERE StartMode='Auto'","Name","DisplayName","State","StartMode");
                await Query(@"root\Microsoft\Windows\TaskScheduler","SELECT TaskName,TaskPath,State,Enabled FROM MSFT_ScheduledTask","TaskName","TaskPath","State","Enabled");
                var boot=ReadLatestBootMeasurement();
                facts.Add(new("Latest boot event",boot.EventTime?.ToLocalTime().ToString("G",CultureInfo.CurrentCulture)??"Unavailable",boot.Source,boot.EventTime is null?"Unavailable":"Direct",boot.EventTime is null?"Unavailable":"Available"));
                facts.Add(new("Windows boot time",boot.WindowsBootTime is {} total?$"{total.TotalMilliseconds:F0} ms":"Unavailable",boot.Source,boot.WindowsBootTime is null?"Unavailable":"Measured",boot.WindowsBootTime is null?"Unavailable":"Available"));
                facts.Add(new("Windows main-path boot time",boot.MainPathBootTime is {} main?$"{main.TotalMilliseconds:F0} ms":"Unavailable",boot.Source,boot.MainPathBootTime is null?"Unavailable":"Measured",boot.MainPathBootTime is null?"Unavailable":"Available"));
                facts.Add(new("Startup completion time",boot.StartupCompletionTime is {} completion?$"{completion.TotalMilliseconds:F0} ms":"Unavailable",boot.Source,boot.StartupCompletionTime is null?"Unavailable":"Measured",boot.StartupCompletionTime is null?"Unavailable":"Available"));
                if(boot.Error is not null)facts.Add(new("Boot measurement status",boot.Error,boot.Source,"Unavailable","Unavailable"));
                Add("Startup impact",null,"Requires additional startup instrumentation; entry count does not establish impact");break;
            case "Apps":
                foreach(var hive in new[]{RegistryHive.LocalMachine,RegistryHive.CurrentUser})foreach(var view in new[]{RegistryView.Registry64,RegistryView.Registry32})
                {
                    using var baseKey=RegistryKey.OpenBaseKey(hive,view);using var key=baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");if(key==null)continue;
                    foreach(var name in key.GetSubKeyNames().Take(1000))
                    {token.ThrowIfCancellationRequested();using var app=key.OpenSubKey(name);if(app?.GetValue("DisplayName") is string display)Add(display,$"{app.GetValue("DisplayVersion")??"Version unavailable"} · {app.GetValue("Publisher")??"Publisher unavailable"}",$"{hive}/{view} uninstall inventory");}
                }
                break;
            case "Devices":
                await Query(@"root\cimv2","SELECT DeviceName,DriverVersion,DriverDate,DriverProviderName,InfName,IsSigned FROM Win32_PnPSignedDriver","DeviceName","DriverVersion","DriverDate","DriverProviderName","InfName","IsSigned");
                await Query(@"root\cimv2","SELECT Name,ConfigManagerErrorCode FROM Win32_PnPEntity WHERE ConfigManagerErrorCode <> 0","Name","ConfigManagerErrorCode");break;
            case "Security":
                await Query(@"root\Microsoft\Windows\Defender","SELECT AMServiceEnabled,AntivirusEnabled,AntispywareEnabled,RealTimeProtectionEnabled,AntivirusSignatureLastUpdated,QuickScanEndTime FROM MSFT_MpComputerStatus","AMServiceEnabled","AntivirusEnabled","AntispywareEnabled","RealTimeProtectionEnabled","AntivirusSignatureLastUpdated","QuickScanEndTime");
                await Query(@"root\cimv2\Security\MicrosoftTpm","SELECT ManufacturerIdTxt,IsEnabled_InitialValue,IsActivated_InitialValue,SpecVersion FROM Win32_Tpm","ManufacturerIdTxt","IsEnabled_InitialValue","IsActivated_InitialValue","SpecVersion");
                RegistryFact(facts,Registry.LocalMachine,@"SYSTEM\CurrentControlSet\Control\SecureBoot\State","UEFISecureBootEnabled","Secure Boot (1 enabled)");
                RegistryFact(facts,Registry.LocalMachine,@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System","EnableLUA","UAC (1 enabled)");
                Firewall(facts);PendingReboot(facts,findings);break;
            case "Windows":
                await Query(@"root\cimv2","SELECT Caption,Version,BuildNumber,OSArchitecture,LastBootUpTime FROM Win32_OperatingSystem","Caption","Version","BuildNumber","OSArchitecture","LastBootUpTime");
                await Query(@"root\cimv2","SELECT Name,DisplayName,State,StartMode FROM Win32_Service","Name","DisplayName","State","StartMode");
                await Query(@"root\Microsoft\Windows\TaskScheduler","SELECT TaskName,TaskPath,State,Enabled FROM MSFT_ScheduledTask","TaskName","TaskPath","State","Enabled");
                await Query(@"root\cimv2","SELECT HotFixID,Description,InstalledOn FROM Win32_QuickFixEngineering","HotFixID","Description","InstalledOn");
                PendingReboot(facts,findings);break;
            case "Power":
                Power(facts);await Query(@"root\cimv2","SELECT Name,BatteryStatus,EstimatedChargeRemaining,EstimatedRunTime FROM Win32_Battery","Name","BatteryStatus","EstimatedChargeRemaining","EstimatedRunTime");
                Add("Battery health",null,"Charge percentage is not battery wear");break;
            case "Creator":
                RunningApps(facts,["obs","fl64","fl studio","ableton","rekordbox","premiere","afterfx","resolve","blender","reaper","voicemeeter"]);
                await Query(@"root\cimv2","SELECT Name,Status,Manufacturer FROM Win32_SoundDevice","Name","Status","Manufacturer");
                await Query(@"root\cimv2","SELECT Name,Status,ConfigManagerErrorCode FROM Win32_PnPEntity WHERE PNPClass='Camera' OR PNPClass='Image'","Name","Status","ConfigManagerErrorCode");
                Memory(facts);Drives(facts,findings);Add("Camera / microphone","No capture device is opened by this scan.","Privacy");Add("Encoder / DPC latency",null,"No validated encoder or ETW session provider");break;
            case "Reliability":
                Events(facts,token);break;
            default:throw new HbxException("HBX-SCAN-0001: Unknown diagnostic center.");
        }
        return new DiagnosticReport(center,DateTimeOffset.UtcNow,facts,findings);
    },token);
    private static string Format(object? value)=>value switch {null=>"Unavailable",Array a=>string.Join(", ",a.Cast<object>()),_=>Convert.ToString(value,CultureInfo.InvariantCulture)??"Unavailable"};
    public static double? ProcessCpuPercent(TimeSpan before,TimeSpan after,TimeSpan elapsed,int logicalProcessors)
    {
        var cpuDelta=(after-before).TotalSeconds;var capacity=elapsed.TotalSeconds*logicalProcessors;
        if(cpuDelta<0||capacity<=0)return null;
        return Math.Clamp(cpuDelta/capacity*100d,0,100);
    }
    private static void Memory(List<Fact> f)
    {
        var m=new MemoryStatus {Length=(uint)Marshal.SizeOf<MemoryStatus>()};
        if(!GlobalMemoryStatusEx(ref m)){f.Add(new("RAM","Unavailable","GlobalMemoryStatusEx","Unavailable","Unavailable"));return;}
        f.Add(new("RAM",$"{(m.TotalPhysical-m.AvailablePhysical)/1073741824d:F1} / {m.TotalPhysical/1073741824d:F1} GiB in use · {m.AvailablePhysical/1073741824d:F1} GiB available","GlobalMemoryStatusEx"));
        f.Add(new("Commit capacity",$"{m.AvailablePageFile/1073741824d:F1} GiB available of {m.TotalPageFile/1073741824d:F1} GiB limit","GlobalMemoryStatusEx"));
    }
    private static void Drives(List<Fact> facts,List<Finding> findings)
    {
        foreach(var d in DriveInfo.GetDrives())try
        {
            if(!d.IsReady)continue;
            facts.Add(new(d.Name,$"{d.DriveFormat} · {d.DriveType} · {d.AvailableFreeSpace/1073741824d:F1} GiB free / {d.TotalSize/1073741824d:F1} GiB","DriveInfo"));
            if(d.DriveType==DriveType.Fixed&&d.AvailableFreeSpace<5L*1024*1024*1024)
                findings.Add(new("Review free space on "+d.Name,$"{d.AvailableFreeSpace/1073741824d:F1} GiB available","Below the 5 GiB review threshold. Check Storage Settings; required space depends on your workload.","Direct free-space observation; heuristic review threshold"));
        }catch(Exception e) when(e is IOException or UnauthorizedAccessException){facts.Add(new(d.Name,"Unavailable","DriveInfo","Unavailable","Unavailable"));}
    }
    private static BootMeasurement ReadLatestBootMeasurement()
    {
        const string channel="Microsoft-Windows-Diagnostics-Performance/Operational";
        if(!OperatingSystem.IsWindows())return new(null,null,null,null,channel,"Unsupported operating system.");
        try
        {
            using var reader=new EventLogReader(new EventLogQuery(channel,PathType.LogName,"*[System[(EventID=100)]]"){ReverseDirection=true,TolerateQueryErrors=true});
            using var record=reader.ReadEvent();
            if(record is null)return new(null,null,null,null,channel,"No Diagnostics-Performance Event 100 is available.");
            var values=XDocument.Parse(record.ToXml(),LoadOptions.None).Descendants()
                .Where(element=>element.Name.LocalName=="Data"&&element.Attribute("Name") is not null)
                .GroupBy(element=>element.Attribute("Name")!.Value,StringComparer.Ordinal)
                .ToDictionary(group=>group.Key,group=>group.First().Value,StringComparer.Ordinal);
            return new(record.TimeCreated is null?null:new DateTimeOffset(record.TimeCreated.Value),
                ReadBootDuration(values,"BootTime"),ReadBootDuration(values,"MainPathBootTime"),ReadBootDuration(values,"BootPostBootTime"),
                $"{channel} Event 100",null);
        }
        catch(Exception exception) when(exception is EventLogException or UnauthorizedAccessException or InvalidOperationException or SecurityException or System.ComponentModel.Win32Exception or System.Xml.XmlException)
        {
            return new(null,null,null,null,channel,exception.GetType().Name);
        }
    }
    private static TimeSpan? ReadBootDuration(IReadOnlyDictionary<string,string> values,string key) =>
        values.TryGetValue(key,out var text)&&long.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out var milliseconds)&&milliseconds>=0&&milliseconds<=TimeSpan.MaxValue.TotalMilliseconds
            ?TimeSpan.FromMilliseconds(milliseconds):null;
    private sealed record BootMeasurement(DateTimeOffset? EventTime,TimeSpan? WindowsBootTime,TimeSpan? MainPathBootTime,TimeSpan? StartupCompletionTime,string Source,string? Error);
    private static void Power(List<Fact> facts)
    {
        try{var active=PowerAction.Current();foreach(var plan in PowerAction.Plans())facts.Add(new(plan.Name,(plan.Id==active?"Active · ":"")+plan.Id,"PowrProf"));}
        catch(Exception e){facts.Add(new("Power plans",e.Message,"PowrProf","Unavailable","Unavailable"));}
    }
    private static void RegistryFact(List<Fact> facts,RegistryKey root,string path,string name,string label)
    {
        using var key=root.OpenSubKey(path);var value=key?.GetValue(name);
        facts.Add(new(label,value?.ToString()??"Unavailable",root.Name+"\\"+path+"\\"+name,value==null?"Unavailable":"Direct",value==null?"Unavailable":"Available"));
    }
    private static void PendingReboot(List<Fact> f,List<Finding> findings)
    {
        using var cbs=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending");
        using var wu=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");
        var pending=cbs!=null||wu!=null;
        f.Add(new("Windows restart",pending?"Restart indicators found":"No CBS / Windows Update restart indicators found","CBS and Windows Update Registry","Derived"));
        if(pending)findings.Add(new("Restart pending","CBS or Windows Update restart indicator exists","Save your work and restart Windows when convenient.","Direct indicator; other reboot reasons may exist"));
    }
    private static void RunningApps(List<Fact> facts,string[] names)
    {
        var found=0;foreach(var p in Process.GetProcesses())using(p)try
        {if(names.Any(n=>p.ProcessName.Contains(n,StringComparison.OrdinalIgnoreCase))){facts.Add(new(p.ProcessName,$"Running · PID {p.Id}","Process-name match","Heuristic"));found++;}}
        catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}
        if(found==0)facts.Add(new("Workload detection","No known process-name matches","Process-name match","Heuristic"));
    }
    private static void Firewall(List<Fact> facts)
    {
        object? instance=null;
        try
        {
            instance=Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2")!);dynamic policy=instance!;
            foreach(var p in new[]{(1,"Domain"),(2,"Private"),(4,"Public")})facts.Add(new(p.Item2+" firewall",((bool)policy.FirewallEnabled[p.Item1])?"Enabled":"Disabled","Windows Firewall COM"));
        }
        catch(Exception e){facts.Add(new("Firewall",e.Message,"Windows Firewall COM","Unavailable","Unavailable"));}
        finally{if(instance!=null&&Marshal.IsComObject(instance))Marshal.FinalReleaseComObject(instance);}
    }
    private static void Events(List<Fact> facts,CancellationToken token)
    {
        foreach(var log in new[]{"System","Application"})try
        {
            using var reader=new EventLogReader(new EventLogQuery(log,PathType.LogName,"*[System[(Level=1 or Level=2) and TimeCreated[timediff(@SystemTime) <= 604800000]]]"){ReverseDirection=true});
            for(var i=0;i<60;i++)
            {token.ThrowIfCancellationRequested();using var record=reader.ReadEvent(TimeSpan.FromSeconds(2));if(record==null)break;facts.Add(new($"{log} · {record.ProviderName} · {record.Id}",$"{record.TimeCreated:G} · event record {record.RecordId}","Windows Event Log"));}
        }catch(EventLogException e){facts.Add(new(log,e.Message,"EventLog","Unavailable","Unavailable"));}
        facts.Add(new("Interpretation","Recent errors are observations, not proof of hardware failure or action causality.","Methodology"));
    }
    public static IReadOnlyList<StartupEntry> StartupEntries()
    {
        var entries=new List<StartupEntry>();
        foreach(var hive in new[]{RegistryHive.CurrentUser,RegistryHive.LocalMachine})foreach(var path in new[]{StartupDisableAction.RunPath,StartupDisableAction.RunPath+"Once"})
        {
            using var b=RegistryKey.OpenBaseKey(hive,RegistryView.Registry64);using var key=b.OpenSubKey(path);if(key==null)continue;
            foreach(var name in key.GetValueNames())
            {var command=key.GetValue(name,null,RegistryValueOptions.DoNotExpandEnvironmentNames)?.ToString()??"Unavailable";entries.Add(new(name,command,hive==RegistryHive.CurrentUser?"Current user":"Entire PC",$"{hive}\\{path}",SafetyGuard.ProtectedStartup(name,command),hive==RegistryHive.CurrentUser&&path==StartupDisableAction.RunPath&&key.GetValueKind(name) is RegistryValueKind.String or RegistryValueKind.ExpandString&&StartupDisableAction.ValidName(name)));}
        }
        return entries;
    }
    public static async Task<DiagnosticReport> PingAsync(string host,CancellationToken token)
    {
        host=ValidateHost(host);token.ThrowIfCancellationRequested();
        var samples=new List<long>();var differences=new List<long>();long? previous=null;var losses=0;using var ping=new Ping();
        for(var i=0;i<6;i++)
        {
            token.ThrowIfCancellationRequested();try{var reply=await ping.SendPingAsync(host,1500).WaitAsync(token);if(reply.Status==IPStatus.Success){samples.Add(reply.RoundtripTime);if(previous is {} last)differences.Add(Math.Abs(reply.RoundtripTime-last));previous=reply.RoundtripTime;}else{losses++;previous=null;}}catch(PingException){losses++;previous=null;}
            await Task.Delay(250,token);
        }
        var facts=new List<Fact>{new("Host",host,"User input","User Reported"),new("Replies",$"{samples.Count} / 6","ICMP"),new("Packet loss",$"{losses/6d*100:F1}%","6 ICMP probes","Derived")};
        if(samples.Count>0){facts.Add(new("Mean RTT",$"{samples.Average():F1} ms","ICMP","Derived"));facts.Add(new("Range",$"{samples.Min()}–{samples.Max()} ms","ICMP","Derived"));}
        else facts.Add(new("RTT","Unavailable · host may block ICMP","ICMP","Unavailable","Unavailable"));
        facts.Add(differences.Count>0?new("RTT variation (jitter)",$"{differences.Average():F2} ms","Mean absolute RTT difference between consecutive successful probes","Derived"):new("RTT variation (jitter)","Unavailable — requires consecutive replies","ICMP","Unavailable","Unavailable"));
        facts.Add(new("Interpretation","Six probes are a short sample; VPN, Wi-Fi and ICMP filtering can affect results.","Methodology"));
        return new("Network benchmark",DateTimeOffset.UtcNow,facts,[]);
    }
    public static async Task<DiagnosticReport> ResolveDnsAsync(string host,CancellationToken token)
    {
        host=ValidateHost(host);token.ThrowIfCancellationRequested();
        var facts=new List<Fact>{new("Host",host,"User input","User Reported")};
        if(IPAddress.TryParse(host,out var address))
        {
            facts.Add(new("DNS result","Not applicable: input is an IP address","System resolver","NotApplicable","NotApplicable"));
            facts.Add(new("Address",address.ToString(),"User input","Direct"));
            return new("DNS resolution",DateTimeOffset.UtcNow,facts,[]);
        }

        var started=Stopwatch.GetTimestamp();
        try
        {
            var addresses=await Dns.GetHostAddressesAsync(host,token);
            var elapsed=Stopwatch.GetElapsedTime(started);
            facts.Add(new("DNS result",addresses.Length>0?"Resolved":"Unavailable", "Windows system resolver","Derived",addresses.Length>0?"Available":"Unavailable"));
            facts.Add(new("Resolution time",addresses.Length>0?$"{elapsed.TotalMilliseconds:F2} ms":"Unavailable","Includes Windows resolver/cache behavior; not a controlled server RTT",addresses.Length>0?"Measured":"Unavailable",addresses.Length>0?"Available":"Unavailable"));
            facts.Add(new("Addresses",addresses.Length>0?string.Join(", ",addresses.Select(item=>item.ToString()).Distinct(StringComparer.OrdinalIgnoreCase)):"Unavailable","System resolver",addresses.Length>0?"Direct":"Unavailable",addresses.Length>0?"Available":"Unavailable"));
        }
        catch(SocketException exception)
        {
            facts.Add(new("DNS result","Unavailable","Windows system resolver","Unavailable","Unavailable"));
            facts.Add(new("Resolver error",exception.SocketErrorCode.ToString(),"SocketException","Unavailable","Unavailable"));
            facts.Add(new("Resolution time",$"{Stopwatch.GetElapsedTime(started).TotalMilliseconds:F2} ms before failure","Windows system resolver","Measured","Available"));
        }
        facts.Add(new("Interpretation","Resolution may be served from cache and does not measure application or endpoint connectivity.","Methodology"));
        return new("DNS resolution",DateTimeOffset.UtcNow,facts,[]);
    }
    private static string ValidateHost(string host)
    {
        host=host.Trim();
        if(host.Length>253||Uri.CheckHostName(host)==UriHostNameType.Unknown)
            throw new HbxException("HBX-NET-0001: Enter a hostname or IP address, without a URL or command.");
        return host;
    }
    [StructLayout(LayoutKind.Sequential)]private struct MemoryStatus{public uint Length,Load;public ulong TotalPhysical,AvailablePhysical,TotalPageFile,AvailablePageFile,TotalVirtual,AvailableVirtual,AvailableExtended;}
    [DllImport("kernel32.dll",SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
}
public sealed record StartupEntry(string Name,string Command,string Scope,string Source,bool Protected,bool CanDisable)
{public override string ToString()=>Name;}


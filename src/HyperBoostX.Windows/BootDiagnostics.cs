using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Management;
using System.Security;
using System.Xml.Linq;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public static class BootDiagnostics
{
    private const string Channel="Microsoft-Windows-Diagnostics-Performance/Operational";
    public static Task<DiagnosticReport> ScanAsync(CancellationToken token)=>Task.Run(()=>Scan(token),token);

    private static DiagnosticReport Scan(CancellationToken token)
    {
        var facts=new List<Fact>();var findings=new List<Finding>();
        ReadBootHistory(facts,token);ReadStartupDegradation(facts,findings,token);ReadServiceDependencies(facts,token);
        if(facts.Count==0)facts.Add(new("Boot diagnostics","Unavailable",Channel,"Unavailable","Unavailable"));
        return new("Boot Optimizer",DateTimeOffset.UtcNow,facts,findings);
    }

    private static void ReadBootHistory(List<Fact> facts,CancellationToken token)
    {
        try
        {
            using var reader=new EventLogReader(new EventLogQuery(Channel,PathType.LogName,"*[System[(EventID=100)]]"){ReverseDirection=true,TolerateQueryErrors=true});
            var count=0;while(count<10)
            {
                token.ThrowIfCancellationRequested();using var record=reader.ReadEvent();if(record is null)break;
                var data=Data(record);var total=Duration(data,"BootTime");var main=Duration(data,"MainPathBootTime");var post=Duration(data,"BootPostBootTime");
                var time=record.TimeCreated?.ToString("yyyy-MM-dd HH:mm:ss",CultureInfo.CurrentCulture)??"Unknown time";
                facts.Add(new($"Boot history {++count}",$"total {Format(total)} · main {Format(main)} · post {Format(post)}",$"{Channel} Event 100 · {time}","Measured",total is null&&main is null&&post is null?"Unavailable":"Available"));
            }
            if(count==0)facts.Add(new("Boot history","Unavailable",$"{Channel} Event 100","Unavailable","Unavailable"));
        }
        catch(Exception exception) when(exception is EventLogException or UnauthorizedAccessException or InvalidOperationException or SecurityException or System.ComponentModel.Win32Exception or System.Xml.XmlException)
        {facts.Add(new("Boot history","Unavailable",$"{Channel}: {exception.GetType().Name}","Unavailable","Unavailable"));}
    }

    private static void ReadStartupDegradation(List<Fact> facts,List<Finding> findings,CancellationToken token)
    {
        try
        {
            using var reader=new EventLogReader(new EventLogQuery(Channel,PathType.LogName,"*[System[(EventID>=101 and EventID<=110)]]"){ReverseDirection=true,TolerateQueryErrors=true});
            var count=0;while(count<50)
            {
                token.ThrowIfCancellationRequested();using var record=reader.ReadEvent();if(record is null)break;
                var data=Data(record);var name=Value(data,"FriendlyName")??Value(data,"FileName")??Value(data,"Name")??$"Event {record.Id}";
                var total=Duration(data,"TotalTime");var degradation=Duration(data,"DegradationTime");
                facts.Add(new($"Startup impact: {name}",$"total {Format(total)} · degradation {Format(degradation)}",$"{Channel} Event {record.Id}","Measured",total is null&&degradation is null?"Unavailable":"Available"));
                if(degradation is {} value&&value.TotalMilliseconds>=1000)findings.Add(new($"Review startup item: {name}",$"Windows recorded {value.TotalMilliseconds:N0} ms degradation.","Review the related application or service; do not disable security or driver components without vendor guidance.","Windows Diagnostics-Performance event; correlation is not proof of causation"));
                count++;
            }
            if(count==0)facts.Add(new("Startup degradation events","No recent Event 101–110 records",Channel,"Direct","Available"));
        }
        catch(Exception exception) when(exception is EventLogException or UnauthorizedAccessException or InvalidOperationException or SecurityException or System.ComponentModel.Win32Exception or System.Xml.XmlException)
        {facts.Add(new("Startup degradation events","Unavailable",$"{Channel}: {exception.GetType().Name}","Unavailable","Unavailable"));}
    }

    private static void ReadServiceDependencies(List<Fact> facts,CancellationToken token)
    {
        try
        {
            using var searcher=new ManagementObjectSearcher(@"root\cimv2","SELECT Name,DisplayName,Dependencies FROM Win32_Service WHERE StartMode='Auto'");
            using var results=searcher.Get();var count=0;
            foreach(ManagementObject service in results)
            {
                using(service){token.ThrowIfCancellationRequested();if(count++>=200)break;var dependencies=service["Dependencies"] as string[]??[];if(dependencies.Length>0)facts.Add(new($"Service dependencies: {service["DisplayName"]??service["Name"]}",string.Join(", ",dependencies),"Win32_Service.Dependencies","Direct","Available"));}
            }
            if(!facts.Any(f=>f.Name.StartsWith("Service dependencies:",StringComparison.Ordinal)))facts.Add(new("Automatic service dependencies","No dependency records returned","Win32_Service.Dependencies","Direct","Available"));
        }
        catch(Exception exception) when(exception is ManagementException or UnauthorizedAccessException or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {facts.Add(new("Automatic service dependencies","Unavailable",$"Win32_Service: {exception.GetType().Name}","Unavailable","Unavailable"));}
    }

    private static Dictionary<string,string> Data(EventRecord record)=>XDocument.Parse(record.ToXml(),LoadOptions.None).Descendants().Where(e=>e.Name.LocalName=="Data"&&e.Attribute("Name") is not null).GroupBy(e=>e.Attribute("Name")!.Value,StringComparer.Ordinal).ToDictionary(g=>g.Key,g=>g.First().Value,StringComparer.Ordinal);
    private static string? Value(IReadOnlyDictionary<string,string> data,string key)=>data.TryGetValue(key,out var value)&&!string.IsNullOrWhiteSpace(value)?value:null;
    private static TimeSpan? Duration(IReadOnlyDictionary<string,string> data,string key)=>data.TryGetValue(key,out var value)&&long.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out var milliseconds)&&milliseconds>=0?TimeSpan.FromMilliseconds(milliseconds):null;
    private static string Format(TimeSpan? value)=>value is null?"Unavailable":$"{value.Value.TotalMilliseconds:N0} ms";
}

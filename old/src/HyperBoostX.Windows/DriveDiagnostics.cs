using System.Diagnostics;
using System.Management;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public static class DriveDiagnostics
{
    public static Task<DiagnosticReport> ScanAsync(CancellationToken token)=>Task.Run(async()=>
    {
        var facts=new List<Fact>();
        Query(facts,@"root\Microsoft\Windows\Storage","SELECT FriendlyName,MediaType,HealthStatus,OperationalStatus,Size FROM MSFT_PhysicalDisk","Physical disk",200,token);
        Query(facts,@"root\Microsoft\Windows\Storage","SELECT DriveLetter,FileSystem,DriveType,HealthStatus,OperationalStatus,Size,SizeRemaining FROM MSFT_Volume WHERE DriveLetter IS NOT NULL","Volume",100,token);
        Query(facts,@"root\Microsoft\Windows\TaskScheduler","SELECT TaskName,TaskPath,State,Enabled FROM MSFT_ScheduledTask WHERE TaskName='ScheduledDefrag'","Optimize schedule",10,token);
        await ReadTrimAsync(facts,token).ConfigureAwait(false);
        if(facts.Count==0)facts.Add(new("Drive policy","Unavailable","Windows storage providers","Unavailable","Unavailable"));
        return new DiagnosticReport("Drive Optimization",DateTimeOffset.UtcNow,facts,[]);
    },token);

    private static void Query(List<Fact> facts,string scope,string query,string prefix,int limit,CancellationToken token)
    {
        try
        {
            using var searcher=new ManagementObjectSearcher(scope,query);using var results=searcher.Get();var count=0;
            foreach(ManagementObject item in results)
            {
                using(item){token.ThrowIfCancellationRequested();if(count++>=limit)break;var values=item.Properties.Cast<PropertyData>().Where(p=>p.Value is not null).Select(p=>$"{p.Name}={Format(p.Value)}").ToArray();facts.Add(new($"{prefix} {count}",string.Join(" · ",values),$"WMI {scope}","Direct","Available"));}
            }
            if(count==0)facts.Add(new(prefix,"No records returned",$"WMI {scope}","Direct","Available"));
        }
        catch(Exception exception) when(exception is ManagementException or UnauthorizedAccessException or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {facts.Add(new(prefix,"Unavailable",$"WMI {scope}: {exception.GetType().Name}","Unavailable","Unavailable"));}
    }

    private static async Task ReadTrimAsync(List<Fact> facts,CancellationToken token)
    {
        try
        {
            var fsutil=Path.Combine(Environment.SystemDirectory,"fsutil.exe");
            using var process=Process.Start(new ProcessStartInfo(fsutil,"behavior query DisableDeleteNotify"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true})!;
            var outputTask=process.StandardOutput.ReadToEndAsync(token);var errorTask=process.StandardError.ReadToEndAsync(token);
            await process.WaitForExitAsync(token).ConfigureAwait(false);var output=(await outputTask.ConfigureAwait(false)).Trim();var error=(await errorTask.ConfigureAwait(false)).Trim();
            facts.Add(process.ExitCode==0&&output.Length>0
                ?new("TRIM delete notification",output,"fsutil behavior query DisableDeleteNotify","Direct","Available")
                :new("TRIM delete notification","Unavailable",$"fsutil exit {process.ExitCode}: {ReportExport.Redact(error)}","Unavailable","Unavailable"));
        }
        catch(Exception exception) when(exception is System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException or InvalidOperationException)
        {facts.Add(new("TRIM delete notification","Unavailable",$"fsutil: {exception.GetType().Name}","Unavailable","Unavailable"));}
    }

    private static string Format(object value)=>value switch{Array array=>string.Join(",",array.Cast<object>()),ulong bytes when bytes>1024*1024=>$"{bytes/(1024d*1024d*1024d):N2} GiB",_=>value.ToString()??""};
}

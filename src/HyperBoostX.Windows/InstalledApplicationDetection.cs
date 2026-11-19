using System.Security;
using Microsoft.Win32;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public static class InstalledApplicationDetection
{
    public static Task<DiagnosticReport> ScanAsync(CancellationToken token)=>Task.Run(()=>
    {
        var facts=new List<Fact>();var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var truncated=false;
        foreach(var hive in new[]{RegistryHive.LocalMachine,RegistryHive.CurrentUser})foreach(var view in new[]{RegistryView.Registry64,RegistryView.Registry32})
        {
            token.ThrowIfCancellationRequested();
            try
            {
                using var root=RegistryKey.OpenBaseKey(hive,view);using var uninstall=root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");if(uninstall is null)continue;
                foreach(var keyName in uninstall.GetSubKeyNames())
                {
                    token.ThrowIfCancellationRequested();if(facts.Count>=2000){truncated=true;break;}using var entry=uninstall.OpenSubKey(keyName);var name=entry?.GetValue("DisplayName")?.ToString()?.Trim();if(string.IsNullOrWhiteSpace(name))continue;
                    var version=entry?.GetValue("DisplayVersion")?.ToString()?.Trim();var publisher=entry?.GetValue("Publisher")?.ToString()?.Trim();var location=entry?.GetValue("InstallLocation")?.ToString()?.Trim();var identity=$"{name}|{version}|{publisher}";
                    if(!seen.Add(identity))continue;var value=string.Join(" · ",new[]{version is null?null:$"version {version}",publisher,location}.Where(v=>!string.IsNullOrWhiteSpace(v)));
                    facts.Add(new(name,value.Length==0?"Installed":value,$"{hive} {view} uninstall registry","Direct","Available"));
                }
            }
            catch(Exception exception) when(exception is UnauthorizedAccessException or SecurityException or IOException)
            {facts.Add(new("Application inventory provider","Unavailable",$"{hive} {view}: {exception.GetType().Name}","Unavailable","Unavailable"));}
        }
        facts.Insert(0,new("Inventory coverage",truncated?$"Truncated at 2,000 records":$"{seen.Count:N0} unique installed application records","Windows uninstall registry","Direct",truncated?"Unavailable":"Available"));
        return new DiagnosticReport("App Load Optimizer",DateTimeOffset.UtcNow,facts.OrderBy(f=>f.Name,StringComparer.CurrentCultureIgnoreCase).ToArray(),[]);
    },token);
}

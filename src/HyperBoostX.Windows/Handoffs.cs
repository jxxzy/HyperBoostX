using System.Diagnostics;

namespace HyperBoostX.Windows;

public sealed record Handoff(string Name,string Destination)
{public override string ToString()=>Name;}
public static class Handoffs
{
    private static readonly Dictionary<string,Handoff[]> Map=new()
    {
        ["Storage"]=[new("Windows Storage Settings","ms-settings:storagesense"),new("Optimize Drives","dfrgui.exe"),new("Disk Cleanup","cleanmgr.exe")],
        ["Gaming"]=[new("Windows Game Mode","ms-settings:gaming-gamemode"),new("Game captures","ms-settings:gaming-gamedvr"),new("Graphics preference","ms-settings:display-advancedgraphics")],
        ["GPU"]=[new("Graphics preference","ms-settings:display-advancedgraphics"),new("Display settings","ms-settings:display")],
        ["Network"]=[new("Network settings","ms-settings:network-status"),new("Advanced network settings","ms-settings:network-advancedsettings")],
        ["Startup"]=[new("Windows Startup Apps","ms-settings:startupapps"),new("Task Scheduler","taskschd.msc")],
        ["Apps"]=[new("Installed apps / uninstall","ms-settings:appsfeatures")],
        ["Devices"]=[new("Device Manager","devmgmt.msc"),new("Windows Update","ms-settings:windowsupdate")],
        ["Security"]=[new("Windows Security / scan","windowsdefender:"),new("Firewall settings","ms-settings:windowsdefender")],
        ["Windows"]=[new("Windows Update","ms-settings:windowsupdate"),new("Optional features","optionalfeatures.exe"),new("Services","services.msc")],
        ["Power"]=[new("Power and battery settings","ms-settings:powersleep")],
        ["Creator"]=[new("Sound settings","ms-settings:sound"),new("Volume mixer","ms-settings:apps-volume"),new("Microphone privacy","ms-settings:privacy-microphone"),new("Camera privacy","ms-settings:privacy-webcam")],
        ["Repair"]=[new("Windows recovery options","ms-settings:recovery"),new("Troubleshooting settings","ms-settings:troubleshoot"),new("System Restore","rstrui.exe"),new("Windows Update","ms-settings:windowsupdate")],
        ["Restore"]=[new("System Protection","SystemPropertiesProtection.exe"),new("System Restore","rstrui.exe")],
        ["Reliability"]=[new("Event Viewer","eventvwr.msc")]
    };
    public static IReadOnlyList<Handoff> For(string center)=>Map.GetValueOrDefault(center)??[];
    public static void Open(Handoff item)
    {
        if(!Map.Values.SelectMany(x=>x).Contains(item))throw new InvalidOperationException("HBX-HANDOFF-0001: Unregistered external tool.");
        if(item.Destination.EndsWith(".msc",StringComparison.OrdinalIgnoreCase))
        {
            var start=new ProcessStartInfo(Path.Combine(Environment.SystemDirectory,"mmc.exe")){UseShellExecute=true};
            start.ArgumentList.Add(Path.Combine(Environment.SystemDirectory,item.Destination));Process.Start(start);
        }
        else Process.Start(new ProcessStartInfo(item.Destination.Contains(':')?item.Destination:Path.Combine(Environment.SystemDirectory,item.Destination)){UseShellExecute=true});
    }
}

using System.Management;
using System.Net.NetworkInformation;
using System.Security;
using Microsoft.Win32;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public static class NetworkProfileDiagnostics
{
    public static readonly string[] Modes=["Balanced","Gaming diagnostics"];
    public static Task<DiagnosticReport> ScanAsync(string mode,CancellationToken token)=>Task.Run(async()=>
    {
        if(!Modes.Contains(mode,StringComparer.Ordinal))throw new HbxException("Unsupported network profile preview.");
        var facts=new List<Fact>();var findings=new List<Finding>();
        facts.Add(new("Selected profile",mode,"User selection","Direct","Available"));
        facts.Add(new("Change policy","Preview and diagnostics only. No DNS server, adapter, registry, QoS, MTU, offload, firewall, or socket setting is changed.","Network safety policy","Guidance","Available"));
        facts.Add(new("Restore state","No HyperBoostX-managed network change exists; current Windows configuration remains authoritative.","Action journal","Direct","Available"));
        foreach(var adapter in NetworkInterface.GetAllNetworkInterfaces().Take(128))
        {
            token.ThrowIfCancellationRequested();var properties=adapter.GetIPProperties();facts.Add(new(adapter.Name,$"{adapter.NetworkInterfaceType} · {adapter.OperationalStatus} · link {adapter.Speed/1_000_000d:N0} Mbps · DNS {string.Join(", ",properties.DnsAddresses)} · gateway {string.Join(", ",properties.GatewayAddresses.Select(g=>g.Address))}","NetworkInterface/IPInterfaceProperties","Direct","Available"));
        }
        try
        {
            using var searcher=new ManagementObjectSearcher("SELECT Description,SettingID,DHCPEnabled,DNSServerSearchOrder,IPEnabled FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled=TRUE");using var rows=searcher.Get();
            foreach(ManagementObject row in rows)using(row)
            {
                token.ThrowIfCancellationRequested();var dns=row["DNSServerSearchOrder"] as string[]??[];facts.Add(new($"DNS policy: {row["Description"]}",$"adapter {row["SettingID"]} · DHCP {row["DHCPEnabled"]} · DNS {(dns.Length==0?"system/DHCP supplied":string.Join(", ",dns))}","Win32_NetworkAdapterConfiguration","Direct","Available"));
            }
        }
        catch(Exception exception) when(exception is ManagementException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException){facts.Add(new("DNS policy provider","Unavailable",$"Win32_NetworkAdapterConfiguration: {exception.GetType().Name}","Unavailable","Unavailable"));}
        try
        {
            using var profiles=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles");if(profiles is null)facts.Add(new("Windows network profiles","Unavailable","NetworkList registry","Unavailable","Unavailable"));else foreach(var subName in profiles.GetSubKeyNames().Take(256)){token.ThrowIfCancellationRequested();using var profile=profiles.OpenSubKey(subName);var name=profile?.GetValue("ProfileName")?.ToString()??subName;var category=profile?.GetValue("Category")?.ToString() switch{"0"=>"Public","1"=>"Private","2"=>"Domain",var value=>$"Unknown ({value??"absent"})"};facts.Add(new($"Windows profile: {name}",$"category {category}",$"NetworkList profile {subName}","Direct","Available"));}
        }
        catch(Exception exception) when(exception is UnauthorizedAccessException or SecurityException or IOException){facts.Add(new("Windows network profiles","Unavailable",$"NetworkList registry: {exception.GetType().Name}","Unavailable","Unavailable"));}
        if(mode=="Gaming diagnostics")
        {
            var gateway=NetworkInterface.GetAllNetworkInterfaces().Where(a=>a.OperationalStatus==OperationalStatus.Up).SelectMany(a=>a.GetIPProperties().GatewayAddresses).Select(g=>g.Address).FirstOrDefault();
            if(gateway is null)facts.Add(new("Gateway latency","Unavailable","No active gateway found","Unavailable","Unavailable"));
            else
            {
                var report=await Diagnostics.PingAsync(gateway.ToString(),token);facts.AddRange(report.Facts.Select(f=>f with{Name="Gateway "+f.Name,Source=$"{f.Source} · {gateway}"}));findings.AddRange(report.Findings);
            }
        }
        else facts.Add(new("Balanced profile preview","Retain Windows-managed adapter and DNS policy; use measured diagnostics before considering a change.","HyperBoostX profile policy","Guidance","Available"));
        return new DiagnosticReport("Network Profiles",DateTimeOffset.UtcNow,facts,findings);
    },token);
}

using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public sealed record DnsAdapterOption(string Id,string Name){public override string ToString()=>Name;}

public interface IDnsProfileProvider
{
    IReadOnlyList<DnsAdapterOption> Adapters();
    string[] ReadDnsServers(string adapterId);
    uint SetDnsServers(string adapterId,string[] servers);
}

public sealed class WindowsDnsProfileProvider:IDnsProfileProvider
{
    public IReadOnlyList<DnsAdapterOption> Adapters()=>NetworkInterface.GetAllNetworkInterfaces()
        .Where(adapter=>adapter.NetworkInterfaceType!=NetworkInterfaceType.Loopback&&Guid.TryParse(adapter.Id,out _))
        .Select(adapter=>new DnsAdapterOption(Guid.Parse(adapter.Id).ToString("D"),$"{adapter.Name} · {adapter.Description}"))
        .OrderBy(adapter=>adapter.Name,StringComparer.CurrentCultureIgnoreCase).ToArray();

    public string[] ReadDnsServers(string adapterId)
    {
        using var configuration=Find(adapterId);
        return configuration["DNSServerSearchOrder"] as string[]??[];
    }

    public uint SetDnsServers(string adapterId,string[] servers)
    {
        using var configuration=Find(adapterId);
        using var parameters=configuration.GetMethodParameters("SetDNSServerSearchOrder");
        parameters["DNSServerSearchOrder"]=servers.Length==0?null:servers;
        using var result=configuration.InvokeMethod("SetDNSServerSearchOrder",parameters,null);
        return Convert.ToUInt32(result?["ReturnValue"]??uint.MaxValue);
    }

    private static ManagementObject Find(string adapter)
    {
        using var searcher=new ManagementObjectSearcher("SELECT SettingID,IPEnabled,DNSServerSearchOrder FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled=TRUE");
        foreach(ManagementObject configuration in searcher.Get())
        {
            if(string.Equals(configuration["SettingID"]?.ToString()?.Trim('{','}'),adapter,StringComparison.OrdinalIgnoreCase))return configuration;
            configuration.Dispose();
        }
        throw new HbxException("HBX-DNS-0002: The selected network adapter configuration is unavailable.");
    }
}

public sealed class DnsProfileAction:ISystemAction
{
    private readonly IDnsProfileProvider _provider;
    private static readonly IReadOnlyDictionary<string,string[]> PresetValues=new Dictionary<string,string[]>(StringComparer.Ordinal)
    {
        ["automatic"]=[],["cloudflare"]=["1.1.1.1","1.0.0.1"],["google"]=["8.8.8.8","8.8.4.4"],["quad9"]=["9.9.9.9","149.112.112.112"]
    };
    public DnsProfileAction():this(new WindowsDnsProfileProvider()){}
    public DnsProfileAction(IDnsProfileProvider provider)=>_provider=provider??throw new ArgumentNullException(nameof(provider));
    public static IReadOnlyList<string> Presets=>PresetValues.Keys.ToArray();
    public ActionInfo Info {get;}=new("network.dns.set_profile","Set adapter DNS profile","Network",
        "Set an explicitly selected adapter to automatic DNS or a compiled public-resolver preset. This does not promise lower latency and may affect name resolution.",
        "Selected PC network adapter","Medium · reversible","Reconnect the adapter if applications retain cached results","Win32_NetworkAdapterConfiguration WMI",true,true);

    public static IReadOnlyList<DnsAdapterOption> Adapters()=>new WindowsDnsProfileProvider().Adapters();

    public static string Target(string adapterId,string preset)
    {
        if(!Guid.TryParseExact(adapterId,"D",out var id)||!PresetValues.ContainsKey(preset))throw new HbxException("HBX-DNS-0001: Adapter or DNS preset is invalid.");
        return $"{id:D}|{preset}";
    }

    public void Validate(string target)
    {
        var (adapter,preset)=Parse(target);_ = preset;
        if(!_provider.Adapters().Any(option=>option.Id==adapter))throw new HbxException("HBX-DNS-0002: The selected network adapter is unavailable.");
    }
    public void ValidateRestore(string target,State before)
    {
        var (adapter,_)=Parse(target);ValidateState(before);
        if(!_provider.Adapters().Any(option=>option.Id==adapter))throw new HbxException("HBX-DNS-0002: The selected network adapter is unavailable.");
    }
    public Task<State> ReadAsync(string target,CancellationToken token)
    {
        var (adapter,_)=Parse(target);token.ThrowIfCancellationRequested();
        var servers=_provider.ReadDnsServers(adapter).Select(NormalizeAddress).ToArray();
        return Task.FromResult(new State(servers.Length>0,"DnsServers",Json.Write(servers)));
    }
    public State Desired(string target,State before)
    {
        var (_,preset)=Parse(target);var servers=PresetValues[preset];return new State(servers.Length>0,"DnsServers",Json.Write(servers));
    }
    public Task WriteAsync(string target,State state,CancellationToken token)
    {
        var (adapter,_)=Parse(target);var servers=ValidateState(state);token.ThrowIfCancellationRequested();
        var code=_provider.SetDnsServers(adapter,servers);
        if(code is not (0 or 1))throw new HbxException($"HBX-DNS-0003: Windows rejected the DNS change with provider code {code}.");
        return Task.CompletedTask;
    }

    private static (string Adapter,string Preset) Parse(string target)
    {
        if(target is null||target.Length>128)return Throw();var parts=target.Split('|');
        if(parts.Length!=2||!Guid.TryParseExact(parts[0],"D",out var id)||!PresetValues.ContainsKey(parts[1]))return Throw();
        return(id.ToString("D"),parts[1]);
        static (string,string) Throw()=>throw new HbxException("HBX-DNS-0001: Adapter or DNS preset is invalid.");
    }
    private static string[] ValidateState(State state)
    {
        if(state.Kind!="DnsServers")throw new HbxException("HBX-DNS-0004: Saved DNS state is invalid.");
        string[] servers;try{servers=Json.Read<string[]>(state.Value);}catch(Exception exception) when(exception is System.Text.Json.JsonException or InvalidDataException){throw new HbxException("HBX-DNS-0004: Saved DNS state is invalid.");}
        if(servers.Length>8||servers.Any(server=>NormalizeAddress(server)!=server)||state.Exists!=(servers.Length>0))throw new HbxException("HBX-DNS-0004: Saved DNS state is invalid.");
        return servers;
    }
    private static string NormalizeAddress(string value)
    {
        if(!IPAddress.TryParse(value,out var address)||address.IsIPv6Multicast||IPAddress.IsLoopback(address)||address.Equals(IPAddress.Any)||address.Equals(IPAddress.IPv6Any))
            throw new HbxException("HBX-DNS-0004: Saved DNS state contains an invalid address.");
        return address.ToString();
    }
}

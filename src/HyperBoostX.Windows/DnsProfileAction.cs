using System.Runtime.InteropServices;
using System.Net.Sockets;
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

    private const ulong StaticNameServersFlag=0x0002;
    private const uint DnsSettingsVersion=1;

    [StructLayout(LayoutKind.Sequential)]
    private struct DnsInterfaceSettings
    {
        public uint Version;
        public ulong Flags;
        public IntPtr Domain;
        public IntPtr NameServer;
        public IntPtr SearchList;
        public uint RegistrationEnabled;
        public uint RegisterAdapterName;
        public uint EnableLLMNR;
        public uint QueryAdapterName;
        public IntPtr ProfileNameServer;
    }

    [DllImport("iphlpapi.dll",ExactSpelling=true)]
    private static extern uint GetInterfaceDnsSettings(Guid adapter,ref DnsInterfaceSettings settings);
    [DllImport("iphlpapi.dll",ExactSpelling=true)]
    private static extern uint SetInterfaceDnsSettings(Guid adapter,ref DnsInterfaceSettings settings);
    [DllImport("iphlpapi.dll",ExactSpelling=true)]
    private static extern void FreeInterfaceDnsSettings(ref DnsInterfaceSettings settings);

    // Read configured static IPv4 addresses, not effective DHCP resolvers.
    public string[] ReadDnsServers(string adapterId)
    {
        if(!Guid.TryParseExact(adapterId,"D",out var adapter))
            throw new HbxException("HBX-DNS-0001: Adapter is invalid.");
        var settings=new DnsInterfaceSettings{Version=DnsSettingsVersion};
        uint result;
        try{result=GetInterfaceDnsSettings(adapter,ref settings);}
        catch(EntryPointNotFoundException){throw new HbxException("HBX-DNS-0005: Required Windows DNS configuration API is unavailable.");}
        if(result!=0)throw new HbxException($"HBX-DNS-0005: DNS configuration could not be read (provider code {result}).");
        try
        {
            if(settings.Version!=DnsSettingsVersion)throw new HbxException("HBX-DNS-0005: Unexpected native DNS settings version.");
            return ParseStaticIpv4(Marshal.PtrToStringUni(settings.NameServer),Marshal.PtrToStringUni(settings.ProfileNameServer));
        }
        finally{FreeInterfaceDnsSettings(ref settings);}
    }

    public static string[] ParseStaticIpv4(string? configured,string? profile)
    {
        if(!string.IsNullOrWhiteSpace(profile))
            throw new HbxException("HBX-DNS-0005: Profile-specific DNS exists; exact recovery is unsupported.");
        if(string.IsNullOrWhiteSpace(configured))return [];
        var values=configured.Split([',',' ','\t','\r','\n'],StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        if(values.Length is <1 or >8)throw new HbxException("HBX-DNS-0005: Unsupported static DNS server list.");
        var normalized=new List<string>();
        foreach(var value in values)
        {
            if(!IPAddress.TryParse(value,out var address)||address.AddressFamily!=AddressFamily.InterNetwork||
               IPAddress.IsLoopback(address)||address.Equals(IPAddress.Any)||address.Equals(IPAddress.Broadcast)||
               address.GetAddressBytes()[0]>=224)
                throw new HbxException("HBX-DNS-0005: An existing static resolver cannot be safely restored.");
            normalized.Add(address.ToString());
        }
        return normalized.ToArray();
    }

    public uint SetDnsServers(string adapterId,string[] servers)
    {
        if(!Guid.TryParseExact(adapterId,"D",out var adapter))
            throw new HbxException("HBX-DNS-0001: Adapter is invalid.");
        var native=Marshal.StringToHGlobalUni(string.Join(",",servers));
        try
        {
            // Restrict writes to the static IPv4 list. An empty UTF-16 string
            // clears the static override. Other fields remain zero.
            var settings=new DnsInterfaceSettings{Version=DnsSettingsVersion,Flags=StaticNameServersFlag,NameServer=native};
            return SetInterfaceDnsSettings(adapter,ref settings);
        }
        catch(EntryPointNotFoundException){throw new HbxException("HBX-DNS-0005: Required Windows DNS configuration API is unavailable.");}
        finally{Marshal.FreeHGlobal(native);}
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
        "Set only the selected adapter static IPv4 DNS list to automatic configuration or a compiled public-resolver preset. IPv6 and other DNS settings are not changed. This may affect name resolution; no latency gain is promised.",
        "Selected PC network adapter","Medium · reversible","Reconnect the adapter if applications retain cached results","Iphlpapi GetInterfaceDnsSettings / SetInterfaceDnsSettings",true,true);

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
        return Task.FromResult(new State(servers.Length>0,"DnsIpv4StaticServers",Json.Write(servers)));
    }
    public State Desired(string target,State before)
    {
        var (_,preset)=Parse(target);var servers=PresetValues[preset];return new State(servers.Length>0,"DnsIpv4StaticServers",Json.Write(servers));
    }
    public Task WriteAsync(string target,State state,CancellationToken token)
    {
        var (adapter,_)=Parse(target);var servers=ValidateState(state);token.ThrowIfCancellationRequested();
        var code=_provider.SetDnsServers(adapter,servers);
        if(code!=0)throw new HbxException($"HBX-DNS-0003: Windows rejected the DNS change with provider code {code}.");
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
        if(state.Kind!="DnsIpv4StaticServers")throw new HbxException("HBX-DNS-0004: Saved DNS state lacks explicit static IPv4 configuration. Review old recovery records manually.");
        string[] servers;try{servers=Json.Read<string[]>(state.Value);}catch(Exception exception) when(exception is System.Text.Json.JsonException or InvalidDataException){throw new HbxException("HBX-DNS-0004: Saved DNS state is invalid.");}
        if(servers.Length>8||servers.Any(server=>NormalizeAddress(server)!=server)||state.Exists!=(servers.Length>0))throw new HbxException("HBX-DNS-0004: Saved DNS state is invalid.");
        return servers;
    }
    private static string NormalizeAddress(string value)
    {
        if(!IPAddress.TryParse(value,out var address)||address.AddressFamily!=AddressFamily.InterNetwork||IPAddress.IsLoopback(address)||address.Equals(IPAddress.Any)||address.Equals(IPAddress.Broadcast)||address.GetAddressBytes()[0]>=224)
            throw new HbxException("HBX-DNS-0004: Saved DNS state contains an invalid address.");
        return address.ToString();
    }
}

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using Microsoft.Win32;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public sealed class WindowsProtector : IStateProtector
{
    private static readonly byte[] Entropy=System.Text.Encoding.UTF8.GetBytes("HyperBoostX.v3.Recovery.1");
    public byte[] Protect(byte[] data)=>ProtectedData.Protect(data,Entropy,DataProtectionScope.CurrentUser);
    public byte[] Unprotect(byte[] data)=>ProtectedData.Unprotect(data,Entropy,DataProtectionScope.CurrentUser);
}
public sealed class Runtime
{
    public Journal Journal {get;}
    public ActionEngine Engine {get;}
    public Diagnostics Diagnostics {get;} = new();
    public DocumentStore Documents {get;}
    public ResearchExperiments Research {get;}
    public GameLibrary Games {get;}
    public GameSessions GamingSessions {get;}
    public Benchmarks Benchmarks {get;}
    public TempCleanupAction Cleanup {get;}
    private static readonly System.Net.Http.HttpClient ProviderHttp=BoundedHttp.CreateClient();
    public IAiProvider Ai {get;}=new NvidiaProvider(ProviderHttp);
    public Notifications Notifications {get;}
    public Runtime(string? dataRoot=null)
    {
        Journal=new Journal(dataRoot??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"HyperBoostX","v3"),new WindowsProtector());
        Journal.Reconcile();
        using var identity=WindowsIdentity.GetCurrent();
        using var machine=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
        var owner=Json.Hash($"{identity.User?.Value}|{machine?.GetValue("MachineGuid")}");
        Cleanup=new TempCleanupAction(Path.GetTempPath(),Journal.Root);
        Engine=new ActionEngine(RegistryAction.CreateAll().Concat<ISystemAction>([new PowerAction(),new StartupDisableAction(),Cleanup]),Journal,new SafetyGuard(EnvironmentBlock),owner);
        Documents=new DocumentStore(Journal,new WindowsProtector());Research=new ResearchExperiments(Documents);Games=new GameLibrary(Documents);GamingSessions=new GameSessions(Engine,Journal,Documents);
        Benchmarks=new Benchmarks(Documents);
        Notifications=new Notifications(Documents,ProviderHttp);
    }
    public static string? EnvironmentBlock()
    {
        if(!OperatingSystem.IsWindowsVersionAtLeast(10,0,19045))return "HBX-COMPAT-0001: Changes require Windows build 19045 or newer.";
        if(GetSystemMetrics(67)!=0)return "HBX-SAFE-0003: Safe Mode allows diagnostics and recovery only.";
        if(GetSystemMetrics(0x1000)!=0)return "HBX-SAFE-0004: Changes are blocked in Remote Desktop sessions.";
        if(!string.Equals(Environment.UserDomainName,Environment.MachineName,StringComparison.OrdinalIgnoreCase))return "HBX-SAFE-0005: Managed domain detected. Organization settings must be reviewed by your administrator.";
        using var mdm=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Provisioning\OMADM\Accounts");
        if(mdm?.SubKeyCount>0)return "HBX-SAFE-0006: Device management enrollment detected. Configuration changes are blocked.";
        return null;
    }
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
}

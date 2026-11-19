using Microsoft.Win32;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public sealed class RegistryAction(string id,string name,string center,string path,string value,int desired,string explanation):ISystemAction
{
    public ActionInfo Info {get;}=new(id,name,center,explanation,"Current user","Low · reversible","Sign out or restart affected app if needed","Windows HKCU Registry");
    public static IEnumerable<RegistryAction> CreateAll()
    {
        yield return new("windows.visual.transparency.disable","Disable transparency","Optimize",@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize","EnableTransparency",0,"Turn off Windows transparency. This changes appearance; an FPS gain is not promised.");
        yield return new("windows.visual.transparency.enable","Enable transparency","Optimize",@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize","EnableTransparency",1,"Enable Windows transparency for this user.");
        yield return new("gaming.capture.disable","Disable game capture preference","Gaming",@"Software\Microsoft\Windows\CurrentVersion\GameDVR","AppCaptureEnabled",0,"Disable the current user's game capture preference. Other capture applications remain independent. Restart the game to evaluate behavior.");
        yield return new("gaming.capture.enable","Enable game capture preference","Gaming",@"Software\Microsoft\Windows\CurrentVersion\GameDVR","AppCaptureEnabled",1,"Enable the current user's Windows game capture preference.");
    }
    public void Validate(string target)
    {
        if(target!="")throw new HbxException("HBX-REG-0001: This action does not accept a custom registry target.");
        if(!OperatingSystem.IsWindowsVersionAtLeast(10,0,19045))throw new HbxException("HBX-REG-0002: Windows build unsupported.");
        using var policy=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR");
        if(center=="Gaming" && policy?.GetValue("AllowGameDVR")!=null)throw new HbxException("HBX-REG-0003: Game capture is managed by Windows policy.");
    }
    public Task<State> ReadAsync(string target,CancellationToken token)
    {
        Validate(target);token.ThrowIfCancellationRequested();using var key=Registry.CurrentUser.OpenSubKey(path);
        if(key?.GetValue(value)==null)throw new HbxException("HBX-CAP-0001: Windows does not expose this preference. Use Windows Settings; no value will be invented.");
        if(key.GetValueKind(value)!=RegistryValueKind.DWord)throw new HbxException("HBX-REG-0004: Unexpected preference type.");
        return Task.FromResult(new State(true,"DWord",Convert.ToString(key.GetValue(value),System.Globalization.CultureInfo.InvariantCulture)!));
    }
    public State Desired(string target,State before)=>new(true,"DWord",desired.ToString(System.Globalization.CultureInfo.InvariantCulture));
    public Task WriteAsync(string target,State state,CancellationToken token)
    {
        Validate(target);token.ThrowIfCancellationRequested();
        if(!state.Exists||state.Kind!="DWord"||!int.TryParse(state.Value,out var number)||number is <0 or >1)throw new HbxException("HBX-REG-0005: Invalid preference state.");
        using var key=Registry.CurrentUser.OpenSubKey(path,true)??throw new HbxException("HBX-REG-0006: Preference key disappeared.");
        key.SetValue(value,number,RegistryValueKind.DWord);key.Flush();return Task.CompletedTask;
    }
}

public sealed class StartupDisableAction:ISystemAction
{
    public const string RunPath=@"Software\Microsoft\Windows\CurrentVersion\Run";
    public static bool ValidName(string target)=>!string.IsNullOrWhiteSpace(target)&&target.Length<=255&&!target.Any(char.IsControl);
    public ActionInfo Info {get;}=new("startup.current_user.disable","Disable selected startup entry","Startup","Remove the selected HKCU Run value after backing up its exact command and type. Restore is blocked if another program recreates it differently.","Current user","Medium · reversible","Next sign-in","HKCU Run");
    public void Validate(string target)
    {
        if(!ValidName(target))throw new HbxException("HBX-START-0001: Invalid startup value name.");
        if(SafetyGuard.ProtectedStartup(target,""))throw new HbxException("HBX-SAFE-0007: This startup entry may support security, drivers or hardware control and is protected.");
    }
    public Task<State> ReadAsync(string target,CancellationToken token)
    {
        Validate(target);token.ThrowIfCancellationRequested();using var key=Registry.CurrentUser.OpenSubKey(RunPath);
        var value=key?.GetValue(target,null,RegistryValueOptions.DoNotExpandEnvironmentNames);
        if(value==null)return Task.FromResult(new State(false,"String",""));
        if(value is not string command||key!.GetValueKind(target) is not (RegistryValueKind.String or RegistryValueKind.ExpandString))throw new HbxException("HBX-START-0002: Unsupported startup value type.");
        if(SafetyGuard.ProtectedStartup(target,command))throw new HbxException("HBX-SAFE-0007: Security, driver and hardware-control startup entries are protected.");
        return Task.FromResult(new State(true,key!.GetValueKind(target).ToString(),command));
    }
    public State Desired(string target,State before)=>new(false,"String","");
    public Task WriteAsync(string target,State state,CancellationToken token)
    {
        Validate(target);token.ThrowIfCancellationRequested();
        if(state.Exists && (state.Kind is not ("String" or "ExpandString")||state.Value.Length>32767||SafetyGuard.ProtectedStartup(target,state.Value)))throw new HbxException("HBX-START-0003: Invalid or protected backup.");
        using var key=Registry.CurrentUser.CreateSubKey(RunPath,true);
        var current=key.GetValue(target)?.ToString()??"";
        if(SafetyGuard.ProtectedStartup(target,current))throw new HbxException("HBX-SAFE-0007: Entry became protected.");
        if(state.Exists)key.SetValue(target,state.Value,Enum.Parse<RegistryValueKind>(state.Kind));else key.DeleteValue(target,false);
        key.Flush();return Task.CompletedTask;
    }
}

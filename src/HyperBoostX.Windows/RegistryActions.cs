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

public interface IVisualEffectsPreferenceProvider
{
    State Read();
    void Write(State state);
}

public sealed class WindowsVisualEffectsPreferenceProvider:IVisualEffectsPreferenceProvider
{
    private const string Path=@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
    private const string ValueName="VisualFXSetting";

    public State Read()
    {
        using var key=Registry.CurrentUser.OpenSubKey(Path);
        if(key?.GetValue(ValueName)==null)throw new HbxException("HBX-VFX-0003: Windows does not expose a stored per-user visual-effects preset; no value will be created.");
        if(key.GetValueKind(ValueName)!=RegistryValueKind.DWord)throw new HbxException("HBX-VFX-0004: The stored visual-effects preset has an unexpected registry type.");
        return new State(true,"DWord",Convert.ToString(key.GetValue(ValueName),System.Globalization.CultureInfo.InvariantCulture)!);
    }

    public void Write(State state)
    {
        var value=VisualEffectsAction.ParseSetting(state);
        using var key=Registry.CurrentUser.OpenSubKey(Path,true)??throw new HbxException("HBX-VFX-0006: The visual-effects preference key disappeared.");
        key.SetValue(ValueName,value,RegistryValueKind.DWord);key.Flush();
    }
}

public sealed class VisualEffectsAction:ISystemAction
{
    private const int BestPerformance=2;
    private readonly IVisualEffectsPreferenceProvider _provider;
    public VisualEffectsAction():this(new WindowsVisualEffectsPreferenceProvider()){}
    public VisualEffectsAction(IVisualEffectsPreferenceProvider provider)=>_provider=provider??throw new ArgumentNullException(nameof(provider));

    public ActionInfo Info {get;}=new("windows.visual.effects.best_performance","Set visual effects to best performance","Optimize",
        "Set the existing current-user Windows visual-effects preset to best performance. This changes appearance and may reduce animation or visual effects; review accessibility preferences. No FPS gain is promised.",
        "Current user","Medium · reversible","Sign out or restart affected apps","Windows HKCU VisualFXSetting");

    public void Validate(string target)
    {
        if(target!="")throw new HbxException("HBX-VFX-0001: This action does not accept a custom target.");
        if(!OperatingSystem.IsWindowsVersionAtLeast(10,0,19045))throw new HbxException("HBX-VFX-0002: This action requires Windows build 19045 or newer.");
    }

    public void ValidateRestore(string target,State before)
    {
        Validate(target);_ = ParseSetting(before);
    }

    public Task<State> ReadAsync(string target,CancellationToken token)
    {
        Validate(target);token.ThrowIfCancellationRequested();var state=_provider.Read();_ = ParseSetting(state);return Task.FromResult(state);
    }

    public State Desired(string target,State before)
    {
        Validate(target);_ = ParseSetting(before);return new State(true,"DWord",BestPerformance.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public Task WriteAsync(string target,State state,CancellationToken token)
    {
        Validate(target);token.ThrowIfCancellationRequested();_ = ParseSetting(state);_provider.Write(state);return Task.CompletedTask;
    }

    internal static int ParseSetting(State state)
    {
        if(!state.Exists||state.Kind!="DWord"||!int.TryParse(state.Value,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var value)||value is <0 or >3)
            throw new HbxException("HBX-VFX-0005: The saved visual-effects preset is invalid or unavailable.");
        return value;
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

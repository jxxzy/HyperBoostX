using System.ComponentModel;
using System.Runtime.InteropServices;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public sealed record PowerPlan(string Id,string Name)
{ public override string ToString()=>Name; }
public sealed class PowerAction:ISystemAction
{
    public ActionInfo Info {get;}=new("windows.power.set_plan","Set active power plan","Power","Select an existing Windows power plan. Higher performance plans can increase heat and battery use. OEM software may override this setting.","Current user / PC power policy","Medium · reversible","No restart","PowrProf API");
    public static IReadOnlyList<PowerPlan> Plans()
    {
        var list=new List<PowerPlan>();
        for(uint i=0;i<128;i++)
        {
            uint size=16;var buffer=new byte[size];var code=PowerEnumerate(IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,16,i,buffer,ref size);
            if(code==259)break;if(code!=0)throw new Win32Exception((int)code);
            var guid=new Guid(buffer);uint nameSize=0;PowerReadFriendlyName(IntPtr.Zero,ref guid,IntPtr.Zero,IntPtr.Zero,null,ref nameSize);
            if(nameSize>65536)throw new HbxException("HBX-PWR-0001: Unexpected plan name length.");
            var nameBuffer=new byte[nameSize];code=PowerReadFriendlyName(IntPtr.Zero,ref guid,IntPtr.Zero,IntPtr.Zero,nameBuffer,ref nameSize);
            var name=code==0?System.Text.Encoding.Unicode.GetString(nameBuffer).TrimEnd('\0'):guid.ToString();
            list.Add(new(guid.ToString(),name));
        }
        return list;
    }
    public void Validate(string target)
    {
        if(!Guid.TryParseExact(target,"D",out _)||!Plans().Any(p=>p.Id.Equals(target,StringComparison.OrdinalIgnoreCase)))throw new HbxException("HBX-PWR-0002: Power plan is not present on this PC.");
    }
    public static string Current()
    {
        var code=PowerGetActiveScheme(IntPtr.Zero,out var p);if(code!=0)throw new Win32Exception((int)code);
        try{return Marshal.PtrToStructure<Guid>(p).ToString();}finally{LocalFree(p);}
    }
    public void ValidateRestore(string target,State before)=>Validate(before.Value);
    public Task<State> ReadAsync(string target,CancellationToken token){token.ThrowIfCancellationRequested();return Task.FromResult(new State(true,"PowerPlan",Current()));}
    public State Desired(string target,State before)=>new(true,"PowerPlan",Guid.Parse(target).ToString());
    public Task WriteAsync(string target,State state,CancellationToken token)
    {
        Validate(state.Value);token.ThrowIfCancellationRequested();
        if(!state.Exists||state.Kind!="PowerPlan")throw new HbxException("HBX-PWR-0003: Invalid plan state.");
        var guid=Guid.Parse(state.Value);var code=PowerSetActiveScheme(IntPtr.Zero,ref guid);
        if(code!=0)throw new Win32Exception((int)code);return Task.CompletedTask;
    }
    [DllImport("powrprof.dll")]private static extern uint PowerGetActiveScheme(IntPtr root,out IntPtr guid);
    [DllImport("powrprof.dll")]private static extern uint PowerSetActiveScheme(IntPtr root,ref Guid guid);
    [DllImport("powrprof.dll")]private static extern uint PowerEnumerate(IntPtr root,IntPtr scheme,IntPtr subgroup,uint access,uint index,byte[] buffer,ref uint size);
    [DllImport("powrprof.dll")]private static extern uint PowerReadFriendlyName(IntPtr root,ref Guid scheme,IntPtr subgroup,IntPtr setting,byte[]? buffer,ref uint size);
    [DllImport("kernel32.dll")]private static extern IntPtr LocalFree(IntPtr memory);
}

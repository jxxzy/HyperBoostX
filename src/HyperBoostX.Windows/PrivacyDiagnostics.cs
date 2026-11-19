using System.Security;
using HyperBoostX.Core;
using Microsoft.Win32;

namespace HyperBoostX.Windows;

// Audits a bounded set of documented machine policy values. This is not
// an effective-per-app permission decision or a claim of telemetry traffic.
public static class PrivacyDiagnostics
{
    public const string AppPrivacyPath=@"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy";
    public const string DataCollectionPath=@"SOFTWARE\Policies\Microsoft\Windows\DataCollection";
    public sealed record PolicyRead(bool Readable,bool Exists,object? Value,RegistryValueKind? Kind);
    private sealed record Definition(string Label,string Path,string Name,bool DiagnosticData);
    private static readonly Definition[] Definitions=[
        new("Camera app access policy",AppPrivacyPath,"LetAppsAccessCamera",false),
        new("Microphone app access policy",AppPrivacyPath,"LetAppsAccessMicrophone",false),
        new("Location app access policy",AppPrivacyPath,"LetAppsAccessLocation",false),
        new("Diagnostic data policy",DataCollectionPath,"AllowTelemetry",true)];

    public static IReadOnlyList<Fact> Inspect(CancellationToken token,Func<string,string,PolicyRead>? provider=null)
    {
        provider??=ReadMachinePolicy;
        var facts=new List<Fact>();
        foreach(var definition in Definitions)
        {
            token.ThrowIfCancellationRequested();
            var read=provider(definition.Path,definition.Name);
            var source=$"HKLM\{definition.Path}\{definition.Name} (DWORD policy)";
            if(!read.Readable){facts.Add(new(definition.Label,"Unavailable",source,"Unavailable","Unavailable"));continue;}
            if(!read.Exists){facts.Add(new(definition.Label,"Not configured in this policy location",source,"Direct","Available"));continue;}
            if(read.Kind!=RegistryValueKind.DWord||read.Value is not int code)
            {facts.Add(new(definition.Label,"Unrecognized policy value type",source,"Limited","Limited"));continue;}
            var value=definition.DiagnosticData
                ?code switch{0=>"0 · Diagnostic data off / edition-dependent",1=>"1 · Required",2=>"2 · Enhanced / version-dependent",3=>"3 · Optional",_=>null}
                :code switch{0=>"0 · User in control",1=>"1 · Force allow",2=>"2 · Force deny",_=>null};
            facts.Add(new(definition.Label,value??"Unrecognized policy value",source,value is null?"Limited":"Direct",value is null?"Limited":"Available"));
        }
        facts.Add(new("Privacy audit scope","Configured machine policies only. Effective app permissions may also depend on Windows version, user consent, per-app exceptions and MDM. No app usage history or personal identifiers are collected.","Microsoft Windows App Privacy / Diagnostic Data policy","Guidance","Available"));
        return facts;
    }

    private static PolicyRead ReadMachinePolicy(string path,string name)
    {
        try
        {
            using var machine=RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,RegistryView.Registry64);
            using var key=machine.OpenSubKey(path,false);
            var value=key?.GetValue(name,null,RegistryValueOptions.DoNotExpandEnvironmentNames);
            return value is null?new(true,false,null,null):new(true,true,value,key!.GetValueKind(name));
        }
        catch(Exception e) when(e is UnauthorizedAccessException or SecurityException or IOException)
        {return new(false,false,null,null);}
    }
}

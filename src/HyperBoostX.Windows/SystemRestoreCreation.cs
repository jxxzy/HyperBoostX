using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public sealed record VerifiedRestorePoint(uint Sequence,string Description,DateTimeOffset VerifiedAt);

// Windows can return S_OK without creating a fresh point because of its frequency policy.
// This provider is intentionally not yet wired to mutation dispatch; worker deadline and
// disposable Windows VM qualification must be established before enabling that route.
public static class SystemRestoreCreation
{
    public static VerifiedRestorePoint RequireObservedPoint(uint returnCode,string description,IReadOnlyList<SystemRestoreInventory.Point> observed)
    {
        if(returnCode!=0)
            throw new HbxException($"HBX-RESTORE-POINT-FAILED: Windows returned 0x{returnCode:X8}; no optimization is authorized.");
        var matches=observed.Where(p=>string.Equals(p.Description,description,StringComparison.Ordinal)).ToArray();
        if(string.IsNullOrWhiteSpace(description)||matches.Length!=1||matches[0].Sequence==0)
            throw new HbxException("HBX-RESTORE-POINT-UNVERIFIED: A unique new restore point was not observed. Windows may have skipped creation; review System Protection and frequency policy.");
        return new(matches[0].Sequence,description,DateTimeOffset.UtcNow);
    }

    public static Task<VerifiedRestorePoint> CreateAsync(bool approved,CancellationToken token)=>Task.Run(()=>
    {
        token.ThrowIfCancellationRequested();
        if(!approved)throw new HbxException("HBX-RESTORE-POINT-CONSENT: Explicit approval is required before creating a Windows restore point.");
        if(Runtime.EnvironmentBlock() is {} restriction)throw new HbxException(restriction);
        using var identity=WindowsIdentity.GetCurrent();
        if(!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
            throw new HbxException("HBX-RESTORE-POINT-PRIVILEGE: Administrator approval is required.");
        var description="HyperBoostX "+Guid.NewGuid().ToString("N");
        try
        {
            var scope=new ManagementScope(@"root\default");
            using var systemRestore=new ManagementClass(scope,new ManagementPath("SystemRestore"),null);
            using var args=systemRestore.GetMethodParameters("CreateRestorePoint");
            args["Description"]=description;args["RestorePointType"]=12u;args["EventType"]=100u;
            token.ThrowIfCancellationRequested();
            using var response=systemRestore.InvokeMethod("CreateRestorePoint",args,new InvokeMethodOptions{Timeout=TimeSpan.FromMinutes(2)});
            token.ThrowIfCancellationRequested();
            if(response?["ReturnValue"] is not uint code)
                throw new HbxException("HBX-RESTORE-POINT-UNVERIFIED: Windows did not return a valid creation result.");
            if(code!=0)return RequireObservedPoint(code,description,[]);
            // description is generated exclusively from a fixed prefix and GUID, never caller input.
            using var search=new ManagementObjectSearcher(scope,new ObjectQuery("SELECT SequenceNumber, Description, CreationTime, RestorePointType FROM SystemRestore WHERE Description = '"+description+"'"),
                new System.Management.EnumerationOptions{Timeout=TimeSpan.FromSeconds(8),ReturnImmediately=false,Rewindable=false});
            using var result=search.Get();var points=new List<SystemRestoreInventory.Point>();var examined=0;
            foreach(ManagementObject row in result)
            {
                using(row)
                {
                    token.ThrowIfCancellationRequested();
                    if(++examined>1||row["SequenceNumber"] is not uint sequence||
                        row["Description"] is not string observedDescription||
                        !string.Equals(observedDescription,description,StringComparison.Ordinal))
                        throw new HbxException("HBX-RESTORE-POINT-UNVERIFIED: Unexpected or incomplete restore point inventory.");
                    points.Add(new(sequence,observedDescription,row["CreationTime"] as string??"",row["RestorePointType"] is uint type?type:null));
                }
            }
            token.ThrowIfCancellationRequested();
            return RequireObservedPoint(code,description,points);
        }
        catch(Exception e) when(e is ManagementException or UnauthorizedAccessException or COMException)
        {
            token.ThrowIfCancellationRequested();
            throw new HbxException("HBX-RESTORE-POINT-UNAVAILABLE: Windows could not confirm restore point creation ("+e.GetType().Name+"). No optimization is authorized.");
        }
    },token);
}

using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

// Read-only inventory. An empty collection is not evidence that protection is disabled,
// and an existing restore point does not prove that new point creation is permitted.
public static class SystemRestoreInventory
{
    public const int MaxPoints=400;
    private const string Source=@"root\default:SystemRestore";
    public sealed record Point(uint Sequence,string Description,string CreationTime,uint? Type);

    public static DiagnosticReport BuildReport(IReadOnlyList<Point> points,bool limited,string? failure)
    {
        var facts=new List<Fact>();
        if(failure is not null)
        {
            facts.Add(new("Windows restore point inventory",failure,Source,"Unavailable","Unavailable"));
        }
        else
        {
            facts.Add(new("Windows restore point inventory",points.Count==0?"No restore points reported":$"{points.Count} observed"+(limited?" (partial inventory)":""),Source,"Direct",limited?"Limited":"Available"));
            foreach(var point in points.OrderByDescending(p=>p.Sequence))
            {
                var timestamp="Unavailable";
                try
                {
                    if(!string.IsNullOrWhiteSpace(point.CreationTime))
                        timestamp=ManagementDateTimeConverter.ToDateTime(point.CreationTime).ToString("O",CultureInfo.InvariantCulture);
                }
                catch(ArgumentException){}
                facts.Add(new($"Restore point #{point.Sequence}",ReportExport.Redact(point.Description),Source,"Direct"));
                facts.Add(new($"Restore point #{point.Sequence} · creation time",timestamp,Source,"Direct",timestamp=="Unavailable"?"Unavailable":"Available"));
                facts.Add(new($"Restore point #{point.Sequence} · type",point.Type?.ToString(CultureInfo.InvariantCulture)??"Unavailable",Source,"Direct",point.Type.HasValue?"Available":"Unavailable"));
            }
        }
        facts.Add(new("System Protection / creation readiness","Not established by enumeration. Review Windows System Protection; no point was created.",Source,"Unavailable","Unavailable"));
        facts.Add(new("HyperBoostX recovery","Per-action backup and rollback are separate from Windows System Restore.","Action journal","Guidance"));
        return new("Restore",DateTimeOffset.UtcNow,facts,[]);
    }

    public static Task<DiagnosticReport> ScanAsync(CancellationToken token)=>Task.Run(()=>
    {
        token.ThrowIfCancellationRequested();
        var points=new List<Point>();
        try
        {
            using var search=new ManagementObjectSearcher(new ManagementScope(@"root\default"),
                new ObjectQuery("SELECT SequenceNumber, Description, CreationTime, RestorePointType FROM SystemRestore"),
                new EnumerationOptions{Timeout=TimeSpan.FromSeconds(8),ReturnImmediately=false,Rewindable=false});
            using var result=search.Get();var limited=false;
            foreach(ManagementObject row in result)
            {
                using(row)
                {
                    token.ThrowIfCancellationRequested();
                    if(points.Count>=MaxPoints){limited=true;break;}
                    if(row["SequenceNumber"] is not uint sequence){limited=true;continue;}
                    points.Add(new(sequence,row["Description"] as string??"",row["CreationTime"] as string??"",row["RestorePointType"] is uint type?type:null));
                }
            }
            token.ThrowIfCancellationRequested();
            return BuildReport(points,limited,null);
        }
        catch(Exception e) when(e is ManagementException or UnauthorizedAccessException or COMException)
        {
            token.ThrowIfCancellationRequested();
            return BuildReport([],false,"Windows restore inventory unavailable ("+e.GetType().Name+"). Access or provider support may be restricted.");
        }
    },token);
}

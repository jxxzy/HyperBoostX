using System.Text.Json;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public sealed record WorkerPreviewRequest(string ActionId,string Target);
public sealed record WorkerApplyRequest(string PreviewToken,bool Approved,string? Session);
public sealed record WorkerChangeRequest(string ChangeId,bool Approved);

public sealed class PrivilegedActionDispatcher(ActionEngine engine,Journal? auditJournal=null)
{
    public static void ValidateReviewedPreview(Preview reviewed,Preview authoritative)
    {
        ArgumentNullException.ThrowIfNull(reviewed);ArgumentNullException.ThrowIfNull(authoritative);
        if(authoritative.ActionId!=reviewed.ActionId||authoritative.Target!=reviewed.Target||authoritative.Before!=reviewed.Before||authoritative.Desired!=reviewed.Desired)
            throw new HbxException("HBX-ACTION-0004: State changed after review. Nothing was applied; review the action again.");
    }
    public static IReadOnlyDictionary<string,Func<string,string,bool>> PayloadValidators { get; }=
        new Dictionary<string,Func<string,string,bool>>(StringComparer.Ordinal)
        {
            ["action.preview"]=(target,body)=>target.Length==0&&Valid<WorkerPreviewRequest>(body,"ActionId","Target"),
            ["action.apply"]=(target,body)=>target.Length==0&&Valid<WorkerApplyRequest>(body,"PreviewToken","Approved","Session"),
            ["action.verify"]=(target,body)=>target.Length==0&&Valid<WorkerChangeRequest>(body,"ChangeId","Approved"),
            ["action.rollback"]=(target,body)=>target.Length==0&&Valid<WorkerChangeRequest>(body,"ChangeId","Approved")
        };

    public async Task<PrivilegedDispatchResult> DispatchAsync(PrivilegedRequestEnvelope request,CancellationToken token)
    {
        if(!PayloadValidators.TryGetValue(request.ActionId,out var validator)||!validator(request.Target,request.Body))
            return new(false,"HBX-IPC-PAYLOAD","Action payload validation failed.");
        PrivilegedDispatchResult result;
        try
        {
            result=request.ActionId switch
            {
                "action.preview"=>await PreviewAsync(Read<WorkerPreviewRequest>(request.Body),token),
                "action.apply"=>await ApplyAsync(Read<WorkerApplyRequest>(request.Body),token),
                "action.verify"=>await VerifyAsync(Read<WorkerChangeRequest>(request.Body),token),
                "action.rollback"=>await RollbackAsync(Read<WorkerChangeRequest>(request.Body),token),
                _=>new(false,"HBX-IPC-DENIED","Action is not allowlisted.")
            };
        }
        catch(HbxException exception){result=Failure(exception.Message);}
        catch(InvalidDataException){result=new(false,"HBX-IPC-PAYLOAD","Action payload validation failed.");}
        if(auditJournal is not null)
        {
            var subject=request.ActionId=="action.preview"?TrySubject(request.Body):request.Body;
            auditJournal.SaveWorkerAudit(request.ActionId,subject,result.Success,result.Code);
        }
        return result;
    }

    private async Task<PrivilegedDispatchResult> PreviewAsync(WorkerPreviewRequest request,CancellationToken token)=>
        Success("HBX-ACTION-PREVIEW",await engine.PreviewAsync(request.ActionId,request.Target,token));

    private async Task<PrivilegedDispatchResult> ApplyAsync(WorkerApplyRequest request,CancellationToken token)
    {
        if(!request.Approved)return new(false,"HBX-ACTION-0002","Explicit approval is required.");
        var change=await engine.ApplyAsync(request.PreviewToken,true,request.Session,token);
        return Success(change.Status=="Verified"||change.Status=="AlreadyDesiredState"?"HBX-ACTION-VERIFIED":"HBX-ACTION-FAILED",change,change.Message,change.Status=="Verified"||change.Status=="AlreadyDesiredState");
    }

    private async Task<PrivilegedDispatchResult> VerifyAsync(WorkerChangeRequest request,CancellationToken token)
    {
        if(!request.Approved)return new(false,"HBX-ACTION-APPROVAL","Explicit approval is required to inspect a privileged change record.");
        return Success("HBX-ACTION-STATE",await engine.VerifyAsync(request.ChangeId,token));
    }

    private async Task<PrivilegedDispatchResult> RollbackAsync(WorkerChangeRequest request,CancellationToken token)
    {
        if(!request.Approved)return new(false,"HBX-RESTORE-0003","Restore requires explicit approval.");
        var change=await engine.RollbackAsync(request.ChangeId,true,token);
        return Success(change.Status=="RolledBack"?"HBX-RESTORE-VERIFIED":"HBX-RESTORE-FAILED",change,change.Message,change.Status=="RolledBack");
    }

    private static PrivilegedDispatchResult Success<T>(string code,T value,string message="Authorized action completed.",bool success=true)=>
        new(success,code,string.IsNullOrWhiteSpace(message)?"Authorized action completed.":ReportExport.Redact(message),Json.Write(value));

    private static PrivilegedDispatchResult Failure(string message)
    {
        var redacted=ReportExport.Redact(message);var separator=redacted.IndexOf(':');
        var code=separator>0&&separator<=128?redacted[..separator]:"HBX-ACTION-ERROR";
        return new(false,code,redacted.Length<=1024?redacted:redacted[..1024]);
    }

    private static T Read<T>(string body)
    {
        try{return Json.Read<T>(body);}
        catch(JsonException exception){throw new InvalidDataException("Invalid action payload.",exception);}
    }

    private static bool Valid<T>(string body,params string[] fields)
    {
        try
        {
            using var document=JsonDocument.Parse(body);var root=document.RootElement;
            if(root.ValueKind!=JsonValueKind.Object)return false;
            var allowed=new HashSet<string>(fields,StringComparer.Ordinal);
            foreach(var property in root.EnumerateObject())
                if(!allowed.Remove(property.Name)||property.Value.ValueKind==JsonValueKind.Null&&property.Name!="Session")return false;
            if(allowed.Count!=0)return false;
            var value=Json.Read<T>(body);
            return value switch
            {
                WorkerPreviewRequest preview=>ValidText(preview.ActionId,128)&&preview.Target is not null&&preview.Target.Length<=1024,
                WorkerApplyRequest apply=>ValidText(apply.PreviewToken,64)&&(apply.Session is null||apply.Session.Length<=128),
                WorkerChangeRequest change=>ValidText(change.ChangeId,64),
                _=>false
            };
        }
        catch{return false;}
    }

    private static bool ValidText(string? value,int maximum)=>!string.IsNullOrWhiteSpace(value)&&value.Length<=maximum&&value.All(character=>!char.IsControl(character));
    private static string TrySubject(string body)
    {
        try{return Read<WorkerPreviewRequest>(body).ActionId;}
        catch{return "invalid";}
    }
}

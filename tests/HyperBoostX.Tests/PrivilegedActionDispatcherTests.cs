using System.IO;
using HyperBoostX.Core;
using HyperBoostX.Windows;
using Microsoft.Data.Sqlite;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class PrivilegedActionDispatcherTests:IDisposable
{
    private readonly string _root=Path.Combine(Path.GetTempPath(),"HyperBoostX-tests",Guid.NewGuid().ToString("N"));

    [Fact]public async Task TypedWorkerLifecyclePreviewsAppliesVerifiesAndRollsBack()
    {
        var action=new FixtureAction();var dispatcher=Create(action);
        var previewResult=await dispatcher.DispatchAsync(Request("action.preview",Json.Write(new WorkerPreviewRequest("fixture.action","safe-target"))),default);
        Assert.True(previewResult.Success);Assert.Equal("HBX-ACTION-PREVIEW",previewResult.Code);
        var preview=Json.Read<Preview>(previewResult.Body);Assert.Equal("0",preview.Before.Value);Assert.Equal("1",preview.Desired.Value);

        var denied=await dispatcher.DispatchAsync(Request("action.apply",Json.Write(new WorkerApplyRequest(preview.Token,false,"fixture-session"))),default);
        Assert.False(denied.Success);Assert.Equal("HBX-ACTION-0002",denied.Code);Assert.Equal("0",action.State.Value);

        var applied=await dispatcher.DispatchAsync(Request("action.apply",Json.Write(new WorkerApplyRequest(preview.Token,true,"fixture-session"))),default);
        Assert.True(applied.Success);Assert.Equal("HBX-ACTION-VERIFIED",applied.Code);var change=Json.Read<Change>(applied.Body);Assert.Equal("1",action.State.Value);
        var verifyDenied=await dispatcher.DispatchAsync(Request("action.verify",Json.Write(new WorkerChangeRequest(change.Id,false))),default);
        Assert.False(verifyDenied.Success);Assert.Equal("HBX-ACTION-APPROVAL",verifyDenied.Code);
        var verified=await dispatcher.DispatchAsync(Request("action.verify",Json.Write(new WorkerChangeRequest(change.Id,true))),default);
        Assert.True(verified.Success);Assert.Equal("1",Json.Read<State>(verified.Body).Value);
        var restored=await dispatcher.DispatchAsync(Request("action.rollback",Json.Write(new WorkerChangeRequest(change.Id,true))),default);
        Assert.True(restored.Success);Assert.Equal("HBX-RESTORE-VERIFIED",restored.Code);Assert.Equal("0",action.State.Value);
    }

    [Fact]public async Task ChangedStateAfterPreviewIsRejectedWithoutMutation()
    {
        var action=new FixtureAction();var dispatcher=Create(action);
        var preview=Json.Read<Preview>((await dispatcher.DispatchAsync(Request("action.preview",Json.Write(new WorkerPreviewRequest("fixture.action","safe-target"))),default)).Body);
        action.State=new(true,"Int","external");
        var result=await dispatcher.DispatchAsync(Request("action.apply",Json.Write(new WorkerApplyRequest(preview.Token,true,null))),default);
        Assert.False(result.Success);Assert.Equal("HBX-ACTION-0004",result.Code);Assert.Equal("external",action.State.Value);Assert.Equal(0,action.Writes);
    }

    [Fact]public void ValidatorsRejectUnknownFieldsCommandsAndUnboundedIdentifiers()
    {
        var preview=PrivilegedActionDispatcher.PayloadValidators["action.preview"];
        Assert.True(preview("",Json.Write(new WorkerPreviewRequest("fixture.action","safe-target"))));
        Assert.False(preview("powershell.exe","{}"));
        Assert.False(preview("","{\"ActionId\":\"fixture.action\",\"Target\":\"safe-target\",\"Command\":\"whoami\"}"));
        Assert.False(preview("",Json.Write(new WorkerPreviewRequest(new string('a',129),"safe-target"))));
    }

    [Fact]public void ElevatedPreviewMustExactlyMatchReviewedState()
    {
        var before=new State(true,"Int","0");var desired=new State(true,"Int","1");
        var reviewed=new Preview("fixture.action","safe-target",before,desired,"local",DateTimeOffset.UtcNow,"reviewed");
        var authoritative=reviewed with{Token="worker",CreatedAt=reviewed.CreatedAt.AddSeconds(1)};
        PrivilegedActionDispatcher.ValidateReviewedPreview(reviewed,authoritative);
        var error=Assert.Throws<HbxException>(()=>PrivilegedActionDispatcher.ValidateReviewedPreview(reviewed,authoritative with{Before=new(true,"Int","external")}));
        Assert.StartsWith("HBX-ACTION-0004",error.Message);
    }

    [Fact]public async Task DispatchPersistsHashChainedAuditWithoutRawSubject()
    {
        var action=new FixtureAction();var journal=new Journal(_root,new TestProtector());
        var engine=new ActionEngine([action],journal,new SafetyGuard(()=>null),"fixture-owner");var dispatcher=new PrivilegedActionDispatcher(engine,journal);
        var result=await dispatcher.DispatchAsync(Request("action.preview",Json.Write(new WorkerPreviewRequest("missing.secret-action","private-target"))),default);
        Assert.False(result.Success);var audit=Assert.Single(journal.WorkerAuditHistory());
        Assert.Equal("action.preview",audit.Operation);Assert.Equal(Json.Hash("missing.secret-action"),audit.SubjectHash);Assert.Equal("GENESIS",audit.PreviousHash);Assert.Equal(result.Code,audit.Code);
        Assert.DoesNotContain("missing.secret-action",Json.Write(audit));Assert.DoesNotContain("private-target",Json.Write(audit));
        journal.SaveWorkerAudit("action.verify","second-sensitive-subject",true,"HBX-ACTION-STATE");
        var history=journal.WorkerAuditHistory();Assert.Equal(2,history.Count);Assert.Equal(history[1].Hash,history[0].PreviousHash);
    }

    [Fact]public void AuditHistoryRejectsTamperedProtectedPayload()
    {
        var journal=new Journal(_root,new TestProtector());var record=journal.SaveWorkerAudit("action.preview","subject",true,"HBX-ACTION-PREVIEW");
        using(var connection=new SqliteConnection($"Data Source={Path.Combine(_root,"hyperboostx.db")};Pooling=False"))
        {
            connection.Open();using var command=connection.CreateCommand();command.CommandText="UPDATE worker_audit SET payload=$payload WHERE id=$id";
            command.Parameters.AddWithValue("$id",record.Id);command.Parameters.AddWithValue("$payload",System.Text.Encoding.UTF8.GetBytes(Json.Write(record with{Code="HBX-TAMPERED"})));command.ExecuteNonQuery();
        }
        var error=Assert.Throws<HbxException>(()=>journal.WorkerAuditHistory());Assert.StartsWith("HBX-AUDIT-0002",error.Message);
    }

    private PrivilegedActionDispatcher Create(ISystemAction action)
    {
        var journal=new Journal(_root,new TestProtector());
        return new(new ActionEngine([action],journal,new SafetyGuard(()=>null),"fixture-owner"));
    }

    private static PrivilegedRequestEnvelope Request(string action,string body)=>new(1,Guid.NewGuid().ToString("D"),action,"",body,"nonce",0,0,"fixture","");

    public void Dispose()
    {
        var root=Path.GetFullPath(_root);var parent=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"HyperBoostX-tests"))+Path.DirectorySeparatorChar;
        if(root.StartsWith(parent,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(root))Directory.Delete(root,true);
    }

    private sealed class TestProtector:IStateProtector{public byte[] Protect(byte[] value)=>value;public byte[] Unprotect(byte[] value)=>value;}
    private sealed class FixtureAction:ISystemAction
    {
        public State State=new(true,"Int","0");public int Writes;
        public ActionInfo Info=>new("fixture.action","Fixture","Tests","Fixture action","Fixture","Low","None","Fixture");
        public void Validate(string target){if(target!="safe-target")throw new HbxException("HBX-FIXTURE-TARGET: Invalid fixture target.");}
        public Task<State> ReadAsync(string target,CancellationToken token)=>Task.FromResult(State);
        public State Desired(string target,State before)=>new(true,"Int","1");
        public Task WriteAsync(string target,State state,CancellationToken token){token.ThrowIfCancellationRequested();Writes++;State=state;return Task.CompletedTask;}
    }
}

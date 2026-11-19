using System.Text;
using System.IO;
using HyperBoostX.Core;
using HyperBoostX.Windows;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class EngineTests:IDisposable
{
    [Fact]public void FrametimeCsvCalculatesLowsSpikesAndOptionalCorrelations()
    {
        var rows=new List<string>{"frametime_ms,cpu,gpu,disk_io,temperature"};
        for(var i=0;i<1000;i++){var frame=i>=990?50:10+(i%5);rows.Add($"{frame},{20+frame},{40+frame},1,{60+frame/10d}");}
        var report=FrameTimeAnalysis.AnalyzeCsv(string.Join("\n",rows),"fixture.csv");
        Assert.Equal("Stutter Analyzer",report.Center);Assert.Contains(report.Facts,f=>f.Name=="Average FPS");Assert.Contains(report.Facts,f=>f.Name=="1% low FPS");Assert.Contains(report.Facts,f=>f.Name=="0.1% low FPS");
        Assert.Contains(report.Facts,f=>f.Name=="Frame-time spikes"&&f.Value.StartsWith("10 ",StringComparison.Ordinal));Assert.Contains(report.Facts,f=>f.Name=="CPU load correlation"&&f.Availability=="Available");Assert.NotEmpty(report.Findings);
    }
    [Fact]public void FrametimeCsvRejectsMissingInvalidOrInsufficientSamples()
    {
        Assert.Throws<HbxException>(()=>FrameTimeAnalysis.AnalyzeCsv("","empty.csv"));
        Assert.Throws<HbxException>(()=>FrameTimeAnalysis.AnalyzeCsv("other\n1\n2","missing.csv"));
        Assert.Throws<HbxException>(()=>FrameTimeAnalysis.AnalyzeCsv("frametime_ms\n10\n11","short.csv"));
    }
    [Fact]public void ResearchTrialsPersistRepeatVariantsAndCalculateDeltaAgainstA()
    {
        var research=new ResearchExperiments(new DocumentStore(Store,new TestProtector()));
        research.Add("Storage upgrade","Level load","A","Load time",10,"seconds","baseline");research.Add("Storage upgrade","Level load","A","Load time",12,"seconds","repeat");research.Add("Storage upgrade","Level load","C","Load time",7,"seconds","SSD");
        Assert.Equal(3,research.History().Count);var report=research.Report("Storage upgrade");Assert.Contains(report.Facts,f=>f.Name=="Load time · variant A"&&f.Value.Contains("n=2",StringComparison.Ordinal));Assert.Contains(report.Facts,f=>f.Name=="Load time · variant C"&&f.Value.Contains("Δ vs A -4",StringComparison.Ordinal));
    }
    [Fact]public void ResearchTrialsNeverPoolDifferentWorkloadsOrBorrowTheirBaselines()
    {
        var research=new ResearchExperiments(new DocumentStore(Store,new TestProtector()));
        research.Add("Experiment","Boot","A","Time",10,"seconds","");
        research.Add("Experiment","Boot","B","Time",7,"seconds","");
        research.Add("Experiment","Game load","A","Time",100,"seconds","");
        research.Add("Experiment","Game load","B","Time",80,"seconds","");
        research.Add("Experiment","File copy","C","Time",1,"seconds","");
        var report=research.Report("Experiment");
        Assert.Contains(report.Facts,f=>f.Name=="Boot / Time · variant B"&&f.Value.Contains("n=1 · Δ vs A -3",StringComparison.Ordinal));
        Assert.Contains(report.Facts,f=>f.Name=="Game load / Time · variant B"&&f.Value.Contains("n=1 · Δ vs A -20",StringComparison.Ordinal));
        Assert.Contains(report.Facts,f=>f.Name=="File copy / Time · variant C"&&f.Value.Contains("no baseline A for this workload",StringComparison.Ordinal)&&!f.Value.Contains("Δ vs A",StringComparison.Ordinal));
    }
    [Fact]public void ResearchTrialsRejectInvalidVariantsAndNonFiniteValues()
    {
        var research=new ResearchExperiments(new DocumentStore(Store,new TestProtector()));
        Assert.Throws<HbxException>(()=>research.Add("Experiment","Workload","E","FPS",1,"FPS",""));Assert.Throws<HbxException>(()=>research.Add("Experiment","Workload","A","FPS",double.NaN,"FPS",""));
    }
    [Fact]public void AutomationRulesAreOptInAllowlistedDueAndEmergencyStoppable()
    {
        var automation=new AutomationRules(new DocumentStore(Store,new TestProtector()));var rule=automation.Add("Hourly scan","SmartScan","NotGaming",60,120);
        Assert.False(rule.Enabled);Assert.Empty(automation.Due(rule.CreatedAt.AddHours(2),new(true,false)));automation.SetEnabled(rule.Id,true);
        Assert.Empty(automation.Due(rule.CreatedAt.AddHours(2),new(true,true)));var due=Assert.Single(automation.Due(rule.CreatedAt.AddHours(2),new(true,false)));automation.Record(due,rule.CreatedAt.AddHours(2),"PASS","fixture evidence");
        Assert.Single(automation.History());Assert.Empty(automation.Due(rule.CreatedAt.AddHours(3),new(true,false)));Assert.Equal(1,automation.EmergencyStop());Assert.False(automation.List().Single().Enabled);
    }
    [Fact]public void AutomationRulesRejectCommandsAndUnsafeTiming()
    {
        var automation=new AutomationRules(new DocumentStore(Store,new TestProtector()));
        Assert.Throws<HbxException>(()=>automation.Add("Bad","powershell.exe","Always",60,60));Assert.Throws<HbxException>(()=>automation.Add("Bad","Reminder","Always",0,1));Assert.Throws<HbxException>(()=>automation.Add("Bad","Reminder","Always",60,10));
    }
    private readonly string _root=Path.Combine(Path.GetTempPath(),"HyperBoostX-tests",Guid.NewGuid().ToString("N"));
    private readonly FakeAction _action=new();
    private Journal Store=>new(_root,new TestProtector());
    private ActionEngine Engine(string? block=null)=>new([_action],Store,new SafetyGuard(()=>block),"test-owner");
    [Fact]public async Task SafeBoostVerifiesBothActionsInOneRecoverableSession()
    {
        var a=new FakeAction("windows.visual.transparency.disable");var b=new FakeAction("gaming.capture.disable");
        var journal=Store;var e=new ActionEngine([a,b],journal,new SafetyGuard(()=>null),"test-owner");
        var previews=new[]{await e.PreviewAsync(a.Info.Id,"",default),await e.PreviewAsync(b.Info.Id,"",default)};
        var result=await e.ApplySafeBoostAsync(previews,true,default);
        Assert.Equal("Verified",result.Status);Assert.Equal(2,result.Changes.Count);Assert.All(result.Changes,c=>Assert.Equal(result.Session,c.Session));
        Assert.Equal("1",a.State.Value);Assert.Equal("1",b.State.Value);
        Assert.All(journal.History(),c=>Assert.True(c.Recoverable));
    }
    [Fact]public async Task SafeBoostRestoresEarlierActionWhenLaterActionFails()
    {
        var a=new FakeAction("windows.visual.transparency.disable");var b=new FakeAction("gaming.capture.disable"){IgnoreWrite=true};
        var e=new ActionEngine([a,b],Store,new SafetyGuard(()=>null),"test-owner");
        var previews=new[]{await e.PreviewAsync(a.Info.Id,"",default),await e.PreviewAsync(b.Info.Id,"",default)};
        var result=await e.ApplySafeBoostAsync(previews,true,default);
        Assert.Equal("FailedRestored",result.Status);Assert.Equal("0",a.State.Value);Assert.Equal("0",b.State.Value);
        Assert.Contains(result.Changes,c=>c.ActionId==a.Info.Id&&c.Status=="RolledBack");
    }
    [Fact]public async Task SafeBoostRejectsTamperedAndUnapprovedPreviewsWithoutWrites()
    {
        var a=new FakeAction("windows.visual.transparency.disable");
        var e=new ActionEngine([a],Store,new SafetyGuard(()=>null),"test-owner");
        var preview=await e.PreviewAsync(a.Info.Id,"",default);
        await Assert.ThrowsAsync<HbxException>(()=>e.ApplySafeBoostAsync([preview],false,default));
        await Assert.ThrowsAsync<HbxException>(()=>e.ApplySafeBoostAsync([preview with{Desired=new(true,"Int","2")}],true,default));
        await Assert.ThrowsAsync<HbxException>(()=>e.ApplySafeBoostAsync([preview,preview],true,default));
        Assert.Equal(0,a.Writes);
    }
    [Fact]public async Task SafeBoostCancellationRecoversAppliedAction()
    {
        using var cts=new CancellationTokenSource();
        var a=new FakeAction("windows.visual.transparency.disable"){CancelSource=cts,CancelAfterWriteValue="1"};
        var b=new FakeAction("gaming.capture.disable");
        var e=new ActionEngine([a,b],Store,new SafetyGuard(()=>null),"test-owner");
        var previews=new[]{await e.PreviewAsync(a.Info.Id,"",default),await e.PreviewAsync(b.Info.Id,"",default)};
        var result=await e.ApplySafeBoostAsync(previews,true,cts.Token);
        Assert.Equal("CancelledRestored",result.Status);Assert.Equal("0",a.State.Value);Assert.Equal(0,b.Writes);
    }
    [Fact]public async Task ApplyPersistsBeforeAndVerifiesRollback()
    {
        var e=Engine();var p=await e.PreviewAsync("test","",default);var c=await e.ApplyAsync(p.Token,true,null,default);
        Assert.Equal("Verified",c.Status);Assert.Equal("1",_action.State.Value);Assert.Equal("0",Store.Find(c.Id).Before.Value);
        var undone=await e.RollbackAsync(c.Id,true,default);Assert.Equal("RolledBack",undone.Status);Assert.Equal("0",_action.State.Value);
    }
    [Fact]public async Task ConsentRequiredAndDoesNotConsumePreview()
    {
        var e=Engine();var p=await e.PreviewAsync("test","",default);
        await Assert.ThrowsAsync<HbxException>(()=>e.ApplyAsync(p.Token,false,null,default));Assert.Equal(0,_action.Writes);
        Assert.Equal("Verified",(await e.ApplyAsync(p.Token,true,null,default)).Status);
    }
    [Fact]public async Task PreviewCannotBeReplayed()
    {
        var e=Engine();var p=await e.PreviewAsync("test","",default);await e.ApplyAsync(p.Token,true,null,default);
        await Assert.ThrowsAsync<HbxException>(()=>e.ApplyAsync(p.Token,true,null,default));Assert.Equal(1,_action.Writes);
    }
    [Fact]public async Task StalePreviewNeverWrites()
    {
        var e=Engine();var p=await e.PreviewAsync("test","",default);_action.State=new(true,"Int","2");
        await Assert.ThrowsAsync<HbxException>(()=>e.ApplyAsync(p.Token,true,null,default));Assert.Equal(0,_action.Writes);
    }
    [Fact]public async Task AlreadyDesiredIsNotRewritten()
    {
        _action.State=new(true,"Int","1");var e=Engine();var p=await e.PreviewAsync("test","",default);
        var c=await e.ApplyAsync(p.Token,true,null,default);Assert.Equal("AlreadyDesiredState",c.Status);Assert.Equal(0,_action.Writes);Assert.False(c.Recoverable);
    }
    [Fact]public async Task ExternalDriftIsNeverOverwritten()
    {
        var e=Engine();var p=await e.PreviewAsync("test","",default);var c=await e.ApplyAsync(p.Token,true,null,default);
        _action.State=new(true,"Int","2");var result=await e.RollbackAsync(c.Id,true,default);
        Assert.Equal("ConflictDetected",result.Status);Assert.Equal("2",_action.State.Value);Assert.Equal(1,_action.Writes);
    }
    [Fact]public async Task WrongOwnerCannotRestore()
    {
        var e=Engine();var p=await e.PreviewAsync("test","",default);var c=await e.ApplyAsync(p.Token,true,null,default);
        var foreign=new ActionEngine([_action],Store,new SafetyGuard(()=>null),"foreign");
        await Assert.ThrowsAsync<HbxException>(()=>foreign.RollbackAsync(c.Id,true,default));Assert.Equal("1",_action.State.Value);
    }
    [Fact]public async Task UnknownActionBlocked()=>await Assert.ThrowsAsync<HbxException>(()=>Engine().PreviewAsync("powershell.exe","-command evil",default));
    [Fact]public async Task MandatoryGuardBlocksRegardlessOfUi()=>await Assert.ThrowsAsync<HbxException>(()=>Engine("Managed device").PreviewAsync("test","",default));
    [Fact]public async Task FailedReadbackIsNotSuccess()
    {
        _action.IgnoreWrite=true;var e=Engine();var p=await e.PreviewAsync("test","",default);
        var c=await e.ApplyAsync(p.Token,true,null,default);Assert.Equal("FailedRestored",c.Status);Assert.Equal("0",_action.State.Value);
    }
    [Fact]public async Task ExceptionAfterMutationCompensates()
    {
        _action.ThrowAfterApply=true;var e=Engine();var p=await e.PreviewAsync("test","",default);
        var c=await e.ApplyAsync(p.Token,true,null,default);Assert.Equal("FailedRestored",c.Status);Assert.Equal("0",_action.State.Value);
    }
    [Fact]public async Task CancellationDuringApplyRestoresDurablyAndPropagates()
    {
        using var cts=new CancellationTokenSource();_action.CancelSource=cts;_action.CancelAfterWriteValue="1";
        var e=Engine();var p=await e.PreviewAsync("test","",default);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>e.ApplyAsync(p.Token,true,null,cts.Token));
        var saved=Assert.Single(Store.History());
        Assert.Equal("CancelledRestored",saved.Status);Assert.False(saved.NeedsRecovery);Assert.Equal("0",_action.State.Value);
    }
    [Fact]public async Task CancellationDuringRollbackIsVisibleAndRetryable()
    {
        var e=Engine();var preview=await e.PreviewAsync("test","",default);var applied=await e.ApplyAsync(preview.Token,true,null,default);
        using var cts=new CancellationTokenSource();cts.Cancel();
        var cancelled=await e.RollbackAsync(applied.Id,true,cts.Token);
        Assert.Equal("RollbackCancelled",cancelled.Status);Assert.True(cancelled.NeedsRecovery);Assert.Equal("1",_action.State.Value);
        var retried=await e.RollbackAsync(applied.Id,true,default);
        Assert.Equal("RolledBack",retried.Status);Assert.False(retried.NeedsRecovery);Assert.Equal("0",_action.State.Value);
    }
    [Fact]public async Task CancellationAfterRollbackWriteStillVerifiesRestoredState()
    {
        var e=Engine();var preview=await e.PreviewAsync("test","",default);var applied=await e.ApplyAsync(preview.Token,true,null,default);
        using var cts=new CancellationTokenSource();_action.CancelSource=cts;_action.CancelAfterWriteValue="0";
        var rolledBack=await e.RollbackAsync(applied.Id,true,cts.Token);
        Assert.Equal("RolledBack",rolledBack.Status);Assert.Equal("0",rolledBack.Observed?.Value);Assert.False(rolledBack.NeedsRecovery);
    }
    [Fact]public async Task CrashIntentCanBeRestoredByNewEngine()
    {
        var c=new Change{ActionId="test",Before=new(true,"Int","0"),Desired=new(true,"Int","1"),Owner="test-owner",Status="Applying"};Store.Save(c);_action.State=c.Desired;
        Assert.Equal("RolledBack",(await Engine().RollbackAsync(c.Id,true,default)).Status);
    }
    [Fact]public async Task GlobalOperationLockPreventsConcurrentChanges()
    {
        var e=Engine();var p=await e.PreviewAsync("test","",default);using var held=Store.Acquire();
        await Assert.ThrowsAsync<IOException>(()=>e.ApplyAsync(p.Token,true,null,default));Assert.Equal(0,_action.Writes);
    }
    [Fact]public async Task BackupFailurePreventsMutation()
    {
        var e=Engine();var p=await e.PreviewAsync("test","",default);
        var recovery=Path.Combine(_root,"Recovery");Directory.Move(recovery,recovery+"-moved");File.WriteAllText(recovery,"not a directory");
        await Assert.ThrowsAnyAsync<IOException>(()=>e.ApplyAsync(p.Token,true,null,default));Assert.Equal(0,_action.Writes);
    }
    [Theory][InlineData("../outside")][InlineData("C:\\outside")][InlineData("file:stream")]
    public void UnsafePathsAreBlocked(string path)=>Assert.Throws<HbxException>(()=>SafePaths.Under(_root,path));
    [Theory][InlineData("Windows Defender")][InlineData("NVIDIA App")][InlineData("vgtray")][InlineData("TRYX KANALI")][InlineData("Realtek Audio")]
    public void CriticalStartupIsProtected(string name)=>Assert.True(SafetyGuard.ProtectedStartup(name,""));
    [Fact]public void DpapiRejectsDamagedCiphertext()
    {
        var protector=new WindowsProtector();var encrypted=protector.Protect(Encoding.UTF8.GetBytes("restore secret"));encrypted[^1]^=0xFF;
        Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(()=>protector.Unprotect(encrypted));
    }
    [Fact]public void CsvPreventsFormulaExecution()
    {
        var report=new DiagnosticReport("Test",DateTimeOffset.UtcNow,[new("=HYPERLINK(\"bad\")","+cmd","@source")],[]);
        var csv=ReportExport.Csv(report);Assert.Contains("\"'=",csv);Assert.Contains("\"'+cmd",csv);Assert.Contains("\"'@source",csv);
    }
    [Fact]public void RedactionPreservesValidJson()
    {
        var report=new DiagnosticReport("Test",DateTimeOffset.UtcNow,[new("secret","C:\\Users\\example\\file token=abcdef at 192.168.1.2","source")],[]);
        var json=Json.Write(ReportExport.Sanitize(report));Assert.DoesNotContain("abcdef",json);Assert.DoesNotContain("192.168",json);Assert.DoesNotContain("example",json);Assert.NotNull(Json.Read<DiagnosticReport>(json));
    }
    [Theory][InlineData("Authorization: Bearer sensitive-token")][InlineData("Authorization=Basic sensitive-token")]
    public void AuthorizationCredentialsAreFullyRedacted(string input)=>Assert.DoesNotContain("sensitive-token",ReportExport.Redact(input));
    [Fact]public void NullAndEmptyReportFieldsDoNotCrash()
    {
        var report=new DiagnosticReport("Test",DateTimeOffset.UtcNow,
            [new("Name",null!,null!),new("","Value",string.Empty),new("  =1+1","Value","Test")],
            [new(null!,null!,null!,null!),new("Low disk space","2 GiB","Review Storage","Measured")]);
        Assert.Equal(string.Empty,ReportExport.Redact(null!));
        Assert.Contains("Name,Value,Source,Quality,Availability",ReportExport.Csv(report));
        Assert.Contains("HyperBoostX",ReportExport.Markdown(report));
        Assert.Contains("Low disk space",ReportExport.Html(report));
    }
    [Theory][InlineData("  =1+1")][InlineData("\n+cmd")][InlineData("\t@formula")]
    public void CsvEscapesWhitespacePrefixedFormulas(string input)
    {
        var report=new DiagnosticReport("Test",DateTimeOffset.UtcNow,[new(input,"Value","Test")],[]);
        Assert.Contains("\"'"+input,ReportExport.Csv(report));
    }
    [Fact]public void MarkdownIncludesFindings()
    {
        var report=new DiagnosticReport("Test",DateTimeOffset.UtcNow,[],[new("Low disk space","2 GiB","Review Storage","Measured")]);
        var markdown=ReportExport.Markdown(report);Assert.Contains("Low disk space",markdown);Assert.Contains("Review Storage",markdown);
    }
    [Fact]public void JournalRecoversFromCorruptDatabase()
    {
        var journal=Store;var c=new Change{ActionId="test",Owner="test-owner"};journal.Save(c);
        File.WriteAllBytes(Path.Combine(_root,"hyperboostx.db"),Encoding.UTF8.GetBytes("corrupt database"));
        var recovered=Store;Assert.Contains("recovered",recovered.RecoveryNotice);Assert.Equal(c.Id,recovered.History().Single().Id);
    }
    [Fact]public void DatabaseAndEncryptedDocumentsExposeReadOnlyIntegrityChecks()
    {
        var journal=Store;var documents=new DocumentStore(journal,new TestProtector());
        Assert.Equal("ok",journal.IntegrityCheck());Assert.Null(journal.BackupIntegrityCheck());
        documents.Update("health-check",new Dictionary<string,string>(),values=>new(){["state"]="valid"});
        Assert.Equal("health-check",Assert.Single(documents.InspectIntegrity()));
        journal.Backup();Assert.Equal("ok",journal.BackupIntegrityCheck());
    }
    [Fact]public void SettingsBatchWritesAreAtomic()
    {
        var journal=Store;
        Assert.Throws<HbxException>(()=>journal.SetSettings(new Dictionary<string,string>
        {
            ["theme"]="Light",["invalid"]=new string('x',4097)
        }));
        Assert.Equal("Dark",journal.GetSetting("theme","Dark"));
        journal.SetSettings(new Dictionary<string,string>{["theme"]="Light",["language"]="en-US"});
        Assert.Equal("Light",journal.GetSetting("theme"));Assert.Equal("en-US",journal.GetSetting("language"));
    }
    [Fact]public async Task ActualWindowsRegistryRoundTripUsesEncryptedRecovery()
    {
        var path=@"Software\HyperBoostX\TestLab\"+Guid.NewGuid().ToString("N");
        try
        {
            using(var key=Microsoft.Win32.Registry.CurrentUser.CreateSubKey(path))key.SetValue("EnableTransparency",1,Microsoft.Win32.RegistryValueKind.DWord);
            var action=new RegistryAction("test.windows","Windows test","Optimize",path,"EnableTransparency",0,"Isolated registry test");
            var journal=new Journal(_root,new WindowsProtector());
            var engine=new ActionEngine([action],journal,new SafetyGuard(()=>null),"test-owner");
            var preview=await engine.PreviewAsync("test.windows","",default);var result=await engine.ApplyAsync(preview.Token,true,null,default);
            Assert.Equal("Verified",result.Status);Assert.Equal("0",(await action.ReadAsync("",default)).Value);
            var raw=File.ReadAllBytes(Path.Combine(_root,"Recovery",result.Id+".hbx"));Assert.DoesNotContain("test.windows",Encoding.UTF8.GetString(raw));
            Assert.Equal("RolledBack",(await engine.RollbackAsync(result.Id,true,default)).Status);Assert.Equal("1",(await action.ReadAsync("",default)).Value);
        }
        finally{Microsoft.Win32.Registry.CurrentUser.DeleteSubKey(path,false);}
    }
    [Fact]public void NativeSqliteIsPatched()
    {
        using var c=new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");c.Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT sqlite_version()";
        Assert.True(Version.Parse((string)cmd.ExecuteScalar()!)>=new Version(3,50,2));
    }
    [Fact]public void DamagedRecoveryDoesNotHideHealthyHistory()
    {
        var journal=Store;var c=new Change{ActionId="test",Owner="test-owner"};journal.Save(c);
        File.WriteAllText(Path.Combine(_root,"Recovery",Guid.NewGuid().ToString("N")+".hbx"),"broken JSON");
        var reopened=Store;reopened.Reconcile();
        Assert.Contains("1 recovery record",reopened.RecoveryNotice);Assert.Equal(c.Id,reopened.History().Single().Id);
        Assert.Equal(c, reopened.Find(c.Id));
    }
    [Fact]public void CorruptDatabaseAndDamagedRecordStillRecoverHealthyRecords()
    {
        var journal=Store;var c=new Change{ActionId="test",Owner="test-owner"};journal.Save(c);
        File.WriteAllText(Path.Combine(_root,"Recovery",Guid.NewGuid().ToString("N")+".hbx"),"broken JSON");
        File.WriteAllText(Path.Combine(_root,"hyperboostx.db"),"broken database");
        Assert.Equal(c.Id,Store.History().Single().Id);
    }
    [Fact]public void RecoveryFileCannotImpersonateAnotherRecord()
    {
        var journal=Store;var c=new Change{ActionId="test",Owner="test-owner"};
        File.WriteAllText(Path.Combine(_root,"Recovery",Guid.NewGuid().ToString("N")+".hbx"),Json.Write(c));
        journal.Reconcile();Assert.Empty(journal.History());Assert.Contains("could not be read",journal.RecoveryNotice);
    }
    [Fact]public void ReportsPersistAcrossRestartAndKeepLatestHundred()
    {
        var journal=Store;var start=DateTimeOffset.UtcNow;
        for(var i=0;i<105;i++)journal.SaveReport(new DiagnosticReport("Report "+i,start.AddSeconds(i),[new("Fact","Value","Test")],[]));
        var reports=Store.Reports();Assert.Equal(100,reports.Count);Assert.Equal("Report 104",reports[0].Center);Assert.Equal("Report 5",reports[^1].Center);
        Assert.Equal("Value",reports[0].Facts.Single().Value);
    }
    [Fact]public async Task RecoveryValidatesOriginalStateInsteadOfRemovedApplyTarget()
    {
        var action=new RemovedTargetAction();var journal=Store;
        var c=new Change{ActionId="removed",Target="deleted-plan",Before=new(true,"Int","0"),Desired=new(true,"Int","1"),Owner="test-owner",Status="Verified"};journal.Save(c);
        var engine=new ActionEngine([action],journal,new SafetyGuard(()=>"Managed"),"test-owner");
        Assert.Equal(c.Desired,await engine.VerifyAsync(c.Id,default));
        Assert.Equal("RolledBack",(await engine.RollbackAsync(c.Id,true,default)).Status);
    }
    private sealed class RemovedTargetAction:ISystemAction
    {
        private State _state=new(true,"Int","1");
        public ActionInfo Info=>new("removed","Test","Test","Test","Test","Low","None","Test");
        public void Validate(string target)=>throw new HbxException("Target was removed");
        public void ValidateRestore(string target,State before){if(before.Value!="0")throw new HbxException("Invalid backup");}
        public Task<State> ReadAsync(string target,CancellationToken token)=>Task.FromResult(_state);
        public State Desired(string target,State before)=>new(true,"Int","1");
        public Task WriteAsync(string target,State state,CancellationToken token){_state=state;return Task.CompletedTask;}
    }
    public void Dispose()
    {
        var root=Path.GetFullPath(_root);var parent=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"HyperBoostX-tests"))+Path.DirectorySeparatorChar;
        if(root.StartsWith(parent,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(root))Directory.Delete(root,true);
    }
    private sealed class TestProtector:IStateProtector{public byte[] Protect(byte[] b)=>b;public byte[] Unprotect(byte[] b)=>b;}
    private sealed class FakeAction(string id="test"):ISystemAction
    {
        public State State=new(true,"Int","0");public int Writes;public bool IgnoreWrite;public bool ThrowAfterApply;
        public CancellationTokenSource? CancelSource;public string? CancelAfterWriteValue;
        public ActionInfo Info=>new(id,"Test","Test","Test","Test","Low","None","Test");
        public void Validate(string target){if(target!="")throw new HbxException("invalid target");}
        public Task<State> ReadAsync(string target,CancellationToken token)=>Task.FromResult(State);
        public State Desired(string target,State before)=>new(true,"Int","1");
        public Task WriteAsync(string target,State state,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();Writes++;if(!IgnoreWrite)State=state;
            if(CancelAfterWriteValue==state.Value){CancelSource?.Cancel();token.ThrowIfCancellationRequested();}
            if(ThrowAfterApply&&state.Value=="1")throw new IOException("Injected failure after OS write");
            return Task.CompletedTask;
        }
    }
}

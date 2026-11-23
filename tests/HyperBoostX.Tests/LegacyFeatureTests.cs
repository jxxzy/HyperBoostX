using System.IO;
using System.Text;
using HyperBoostX.Core;
using HyperBoostX.Windows;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class LegacyFeatureTests:IDisposable
{
    private readonly string _root=Path.Combine(Path.GetTempPath(),"HyperBoostX-tests",Guid.NewGuid().ToString("N"));
    private readonly Journal _journal;
    private readonly DocumentStore _documents;
    public LegacyFeatureTests(){_journal=new Journal(_root,new WindowsProtector());_documents=new DocumentStore(_journal,new WindowsProtector());}
    private GameEntry Game(GameProfile? profile=null)
    {
        var executable=Path.Combine(_root,Guid.NewGuid().ToString("N")+".exe");File.WriteAllText(executable,"Never executed: test fixture");
        return new(Guid.NewGuid().ToString("N"),"Fixture",executable,profile??new(),DateTimeOffset.UtcNow);
    }
    [Fact]public void GameLibraryPersistsProfileAndRejectsDuplicate()
    {
        var fixture=Game();var library=new GameLibrary(_documents);var game=library.Add(fixture.Name,fixture.Executable);
        Assert.Throws<HbxException>(()=>library.Add("Duplicate",fixture.Executable));
        library.SaveProfile(game.Id,new(DisableCapture:true,RaisePriority:true));var reopened=new GameLibrary(new DocumentStore(_journal,new WindowsProtector()));
        Assert.True(reopened.List().Single().Profile.DisableCapture);
        Assert.True(reopened.List().Single().Profile.RaisePriority);
        Assert.DoesNotContain(fixture.Executable,Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(_root,"Documents","games.hbx"))));
        reopened.Remove(game.Id);Assert.Empty(reopened.List());Assert.True(File.Exists(fixture.Executable));
    }
    [Fact]public void ExistingGameProfileJsonDefaultsPriorityOptInToDisabled()
    {
        var profile=Json.Read<GameProfile>("{\"PowerPlan\":null,\"DisableCapture\":true,\"DisableTransparency\":false}");
        Assert.True(profile.DisableCapture);Assert.False(profile.RaisePriority);
    }
    [Fact]public void InvalidGamePathAndDocumentTraversalAreRejected()
    {
        Assert.Throws<HbxException>(()=>new GameLibrary(_documents).Add("No game",Path.Combine(_root,"missing.exe")));
        Assert.Throws<HbxException>(()=>_documents.Read("../outside",new List<GameEntry>()));
    }
    [Fact]public void GameLibraryImportValidatesAllEntriesAndMergesWithoutOverwriting()
    {
        var fixture=Game();var library=new GameLibrary(_documents);var existing=library.Add(fixture.Name,fixture.Executable);
        var additional=Game() with{Name="Imported",Profile=new(DisableCapture:true,RaisePriority:true)};
        Assert.Equal(1,library.Import([existing,additional]));
        var imported=library.List().Single(game=>game.Name=="Imported");
        Assert.True(imported.Profile.DisableCapture);Assert.True(imported.Profile.RaisePriority);Assert.NotEqual(additional.Id,imported.Id);
        Assert.Equal(0,library.Import([existing]));Assert.Equal(2,library.List().Count);
        var invalid=Game() with{Executable=Path.Combine(_root,"missing.exe")};
        Assert.Throws<HbxException>(()=>library.Import([GameEntryForImport(additional),invalid]));
        Assert.Throws<HbxException>(()=>library.Import([additional with{Executable=""}]));
        Assert.Equal(2,library.List().Count);
    }
    [Fact]public void DamagedDomainDataIsNotSilentlyReset()
    {
        var library=new GameLibrary(_documents);var game=Game();library.Add(game.Name,game.Executable);
        File.WriteAllText(Path.Combine(_root,"Documents","games.hbx"),"Corrupt");
        Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(()=>library.List());
    }
    [Fact]public async Task SessionAppliesAndRestoresThroughSharedJournalAcrossRestart()
    {
        var action=new Preference("gaming.capture.disable");var engine=Engine(action);var game=Game(new(DisableCapture:true));
        var preview=await engine.PreviewAsync(action.Info.Id,"",default);
        var sessions=new GameSessions(engine,_journal,_documents);var started=await sessions.StartAsync(game,[preview],true,default);
        Assert.Equal("Active",started.Status);Assert.Equal("0",action.Value.Value);
        var restarted=new GameSessions(engine,_journal,_documents);Assert.True(restarted.Active);
        Assert.Equal("Restored",(await restarted.RestoreAsync(true,default)).Status);Assert.Equal("1",action.Value.Value);
        Assert.All(_journal.History(),c=>Assert.Equal(started.Id,c.Session));
    }
    [Fact]public async Task GameSessionPersistsExactProcessIdentityForRestartRecovery()
    {
        var fixture=Game(new(DisableCapture:true));var library=new GameLibrary(_documents);var game=library.Add(fixture.Name,fixture.Executable);
        library.SaveProfile(game.Id,fixture.Profile);game=library.List().Single();
        var action=new Preference("gaming.capture.disable");var engine=Engine(action);var preview=await engine.PreviewAsync(action.Info.Id,"",default);
        var sessions=new GameSessions(engine,_journal,_documents);var started=await sessions.StartAsync(game,[preview],true,default);
        var identity=new GameProcessIdentity(73124,DateTimeOffset.UtcNow.UtcDateTime.Ticks);

        var attached=sessions.AttachProcess(started.Id,identity);
        var reopened=new GameSessions(engine,_journal,new DocumentStore(_journal,new WindowsProtector()));

        Assert.Equal(identity.ProcessId,reopened.Current?.ProcessId);
        Assert.Equal(identity.StartTimeUtcTicks,reopened.Current?.ProcessStartTimeUtcTicks);
        Assert.Contains("exact game process",attached.Message);
        Assert.Throws<HbxException>(()=>reopened.AttachProcess(Guid.NewGuid().ToString("N"),identity));
        Assert.Throws<HbxException>(()=>reopened.AttachProcess(started.Id,identity with{ProcessId=identity.ProcessId+1}));
        Assert.Equal("Restored",(await reopened.RestoreAsync(true,default)).Status);
        Assert.Equal("1",action.Value.Value);
    }
    [Fact]public void OlderGamingSessionRecordsRemainUntrackedInsteadOfInventingProcessIdentity()
    {
        var session=Json.Read<GamingSession>("{\"Id\":\"legacy-session\",\"GameId\":\"legacy-game\",\"GameName\":\"Legacy\",\"Status\":\"Active\",\"StartedAt\":\"2026-01-01T00:00:00+00:00\",\"Message\":\"\"}");
        Assert.Null(session.ProcessId);Assert.Null(session.ProcessStartTimeUtcTicks);
    }
    [Fact]public async Task InterruptedSessionRestoreResumesFromPersistedJournalState()
    {
        var action=new Preference("gaming.capture.disable");var engine=Engine(action);var game=Game(new(DisableCapture:true));
        var sessions=new GameSessions(engine,_journal,_documents);
        var started=await sessions.StartAsync(game,[await engine.PreviewAsync(action.Info.Id,"",default)],true,default);
        _documents.Update<GamingSession?>("gaming-session",null,current=>current! with{Status="Restoring"});

        var reopened=new GameSessions(engine,_journal,new DocumentStore(_journal,new WindowsProtector()));
        Assert.Equal("Restored",(await reopened.RestoreAsync(true,default)).Status);
        Assert.Equal("1",action.Value.Value);
        Assert.Equal(started.Id,reopened.Current?.Id);
    }
    [Fact]public async Task SessionPartialFailureRestoresEarlierActions()
    {
        var capture=new Preference("gaming.capture.disable");var visual=new Preference("windows.visual.transparency.disable"){IgnoreWrite=true};
        var engine=Engine(capture,visual);var game=Game(new(DisableCapture:true,DisableTransparency:true));
        var a=await engine.PreviewAsync(capture.Info.Id,"",default);var b=await engine.PreviewAsync(visual.Info.Id,"",default);
        var sessions=new GameSessions(engine,_journal,_documents);var result=await sessions.StartAsync(game,[a,b],true,default);
        Assert.Equal("Restored",result.Status);Assert.Contains("failed",result.Message);Assert.Equal("1",capture.Value.Value);Assert.False(sessions.Active);
    }
    [Fact]public async Task CanceledSessionStartPropagatesCancellationAfterSafeRecovery()
    {
        var action=new Preference("gaming.capture.disable");
        var engine=Engine(action);
        var game=Game(new(DisableCapture:true));
        var preview=await engine.PreviewAsync(action.Info.Id,"",default);
        var sessions=new GameSessions(engine,_journal,_documents);
        using var cts=new CancellationTokenSource();cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            ()=>sessions.StartAsync(game,[preview],true,cts.Token));

        Assert.Equal("1",action.Value.Value);
        Assert.Equal("Restored",sessions.Current?.Status);
        Assert.False(sessions.Active);
        Assert.DoesNotContain(_journal.History(),change=>change.NeedsRecovery);
    }
    [Fact]public async Task CanceledSessionRestoreRemainsRecoverableAndCanBeRetried()
    {
        var action=new Preference("gaming.capture.disable");var engine=Engine(action);var game=Game(new(DisableCapture:true));
        var sessions=new GameSessions(engine,_journal,_documents);
        await sessions.StartAsync(game,[await engine.PreviewAsync(action.Info.Id,"",default)],true,default);
        using var cts=new CancellationTokenSource();cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>sessions.RestoreAsync(true,cts.Token));
        Assert.Contains(_journal.History(),c=>c.Status=="RollbackCancelled"&&c.NeedsRecovery);
        Assert.Equal("RecoveryRequired",sessions.Current?.Status);Assert.Equal("0",action.Value.Value);
        Assert.Equal("Restored",(await sessions.RestoreAsync(true,default)).Status);Assert.Equal("1",action.Value.Value);
    }
    [Fact]public async Task CancelledEmptySessionRestoreCannotClaimCompletionAndCanRetry()
    {
        // A persisted session may have no recoverable changes (e.g. all profile
        // actions already matched their desired state). Cancellation must still
        // be observable rather than silently turning the session into Restored.
        var engine=Engine();var sessions=new GameSessions(engine,_journal,_documents);
        _documents.Update<GamingSession?>("gaming-session",null,_=>new GamingSession(
            Guid.NewGuid().ToString("N"),"fixture-game","Fixture","Active",DateTimeOffset.UtcNow));
        using var cts=new CancellationTokenSource();cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>sessions.RestoreAsync(true,cts.Token));
        Assert.Equal("RecoveryRequired",sessions.Current?.Status);
        Assert.Empty(_journal.History());
        Assert.Equal("Restored",(await sessions.RestoreAsync(true,default)).Status);
        Assert.False(sessions.Active);
    }
    [Fact]public async Task SessionCannotOverwriteDriftOrStartOverUnresolvedRecovery()
    {
        var action=new Preference("gaming.capture.disable");var engine=Engine(action);var game=Game(new(DisableCapture:true));
        var sessions=new GameSessions(engine,_journal,_documents);
        await sessions.StartAsync(game,[await engine.PreviewAsync(action.Info.Id,"",default)],true,default);
        action.Value=new(true,"DWord","2");
        Assert.Equal("RecoveryRequired",(await sessions.RestoreAsync(true,default)).Status);Assert.Equal("2",action.Value.Value);
        await Assert.ThrowsAsync<HbxException>(()=>sessions.StartAsync(game,[],true,default));
    }
    [Fact]public async Task SessionRequiresConsentAndExactProfilePreview()
    {
        var action=new Preference("gaming.capture.disable");var engine=Engine(action);var sessions=new GameSessions(engine,_journal,_documents);var game=Game(new(DisableCapture:true));
        await Assert.ThrowsAsync<HbxException>(()=>sessions.StartAsync(game,[],false,default));
        await Assert.ThrowsAsync<HbxException>(()=>sessions.StartAsync(game,[],true,default));Assert.Null(sessions.Current);Assert.Empty(_journal.History());
    }
    [Fact]public void BenchmarkImportIsAtomicAndSupportsQuotedCsv()
    {
        var benchmarks=new Benchmarks(_documents);
        const string header="game,avg_fps,one_percent_low_fps,average_frametime_ms,notes\n";
        Assert.Throws<HbxException>(()=>benchmarks.ImportCsv(header+"Game,120,90,8.3,ok\nGame,NaN,80,9,bad"));Assert.Empty(benchmarks.History());
        Assert.Equal(1,benchmarks.ImportCsv(header+"\"Game, One\",120,90,8.3,\"quoted, note\""));
        Assert.Equal("Game, One",benchmarks.History().Single().Game);Assert.Equal("quoted, note",benchmarks.History().Single().Notes);
    }
    [Theory][InlineData(double.NaN,1,1)][InlineData(double.PositiveInfinity,1,1)][InlineData(10,20,1)][InlineData(10,1,0)]
    public void BenchmarkRejectsInvalidMeasurements(double average,double low,double time)=>Assert.Throws<HbxException>(()=>Benchmarks.Validate("Game",average,low,time,""));
    [Fact]public void BenchmarkComparisonUsesActualInputsAndRejectsDifferentWorkloads()
    {
        var before=Benchmarks.Validate("Game",100,60,10,"");var after=Benchmarks.Validate("Game",120,70,8.33,"");
        var report=Benchmarks.Compare(before,after);Assert.Contains("20",report.Facts.Single(f=>f.Name=="Average FPS change").Value);
        Assert.Throws<HbxException>(()=>Benchmarks.Compare(before,after with{Game="Other"}));
    }
    private ActionEngine Engine(params ISystemAction[] actions)=>new(actions,_journal,new SafetyGuard(()=>null),"fixture-owner");
    private static GameEntry GameEntryForImport(GameEntry game)=>game with{Name="Another imported entry"};
    private sealed class Preference(string id):ISystemAction
    {
        public State Value=new(true,"DWord","1");public bool IgnoreWrite;
        public ActionInfo Info=>new(id,"Fixture","Gaming","Fixture","Test","Low","None","Fixture");
        public void Validate(string target){if(target!="")throw new HbxException("Invalid target");}
        public Task<State> ReadAsync(string target,CancellationToken token){token.ThrowIfCancellationRequested();return Task.FromResult(Value);}
        public State Desired(string target,State before)=>new(true,"DWord","0");
        public Task WriteAsync(string target,State state,CancellationToken token){token.ThrowIfCancellationRequested();if(!IgnoreWrite)Value=state;return Task.CompletedTask;}
    }
    public void Dispose()
    {
        var allowed=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"HyperBoostX-tests"))+Path.DirectorySeparatorChar;
        if(Path.GetFullPath(_root).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(_root))Directory.Delete(_root,true);
    }
}

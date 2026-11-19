using System.Diagnostics;
using System.IO;
using HyperBoostX.Core;
using HyperBoostX.Windows;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class GameProcessPriorityActionTests:IDisposable
{
    private readonly string _root=Path.Combine(Path.GetTempPath(),"HyperBoostX-tests","game-priority-"+Guid.NewGuid().ToString("N"));
    private readonly Journal _journal;
    private readonly GameLibrary _games;

    public GameProcessPriorityActionTests()
    {
        _journal=new Journal(_root,new TestProtector());
        _games=new GameLibrary(new DocumentStore(_journal,new TestProtector()));
    }

    [Fact]public async Task PriorityIsPreviewedAppliedVerifiedAndRestoredThroughTheGamingSession()
    {
        var game=AddGame();var target=new GameProcessPriorityTarget(game.Id,513247,DateTimeOffset.UtcNow.UtcDateTime.Ticks);
        var provider=new FixtureProvider(game,target);var action=new GameProcessPriorityAction(_games,provider);
        var engine=Engine(action);var sessionManager=new GameSessions(engine,_journal,new DocumentStore(_journal,new TestProtector()));
        var session=await sessionManager.StartAsync(game,[],true,default);
        var preview=await engine.PreviewAsync(action.Info.Id,Json.Write(target),default);

        Assert.Equal("Normal",preview.Before.Value);Assert.Equal("AboveNormal",preview.Desired.Value);
        var applied=await engine.ApplyAsync(preview.Token,true,session.Id,default);
        Assert.Equal("Verified",applied.Status);Assert.Equal(ProcessPriorityClass.AboveNormal,provider.Priority);

        var restored=await sessionManager.RestoreAsync(true,default);
        Assert.Equal("Restored",restored.Status);Assert.Equal(ProcessPriorityClass.Normal,provider.Priority);
        Assert.Equal("RolledBack",_journal.History().Single(change=>change.ActionId==action.Info.Id).Status);
    }

    [Fact]public async Task ExitedGameProcessIsACompletedSessionRestoreNotAStalePidWrite()
    {
        var game=AddGame();var target=new GameProcessPriorityTarget(game.Id,513248,DateTimeOffset.UtcNow.UtcDateTime.Ticks);
        var provider=new FixtureProvider(game,target);var action=new GameProcessPriorityAction(_games,provider);
        var engine=Engine(action);var sessions=new GameSessions(engine,_journal,new DocumentStore(_journal,new TestProtector()));
        var session=await sessions.StartAsync(game,[],true,default);
        var preview=await engine.PreviewAsync(action.Info.Id,Json.Write(target),default);
        var applied=await engine.ApplyAsync(preview.Token,true,session.Id,default);
        Assert.Equal("Verified",applied.Status);

        provider.Ended=true;
        var restored=await sessions.RestoreAsync(true,default);

        Assert.Equal("Restored",restored.Status);Assert.Equal(1,provider.WriteCount);
        Assert.Equal("RolledBack",_journal.Find(applied.Id).Status);
        Assert.Contains("no longer exists",_journal.Find(applied.Id).Message);
    }

    [Fact]public async Task ProcessExitDuringRollbackWriteResolvesRecoveryWithoutStalePidWrite()
    {
        var game=AddGame();var target=new GameProcessPriorityTarget(game.Id,513251,DateTimeOffset.UtcNow.UtcDateTime.Ticks);
        var provider=new FixtureProvider(game,target);var action=new GameProcessPriorityAction(_games,provider);
        var engine=Engine(action);var preview=await engine.PreviewAsync(action.Info.Id,Json.Write(target),default);
        var applied=await engine.ApplyAsync(preview.Token,true,null,default);
        Assert.Equal("Verified",applied.Status);
        provider.ExitOnRestore=true;
        var restored=await engine.RollbackAsync(applied.Id,true,default);
        Assert.Equal("RolledBack",restored.Status);
        Assert.Contains("no longer exists",restored.Message);
        Assert.Equal(1,provider.WriteCount);
        Assert.False(_journal.Find(applied.Id).NeedsRecovery);
    }

    [Fact]public async Task InvalidProcessIdentityAndUnsupportedExistingPriorityAreRejected()
    {
        var game=AddGame();var target=new GameProcessPriorityTarget(game.Id,513249,DateTimeOffset.UtcNow.UtcDateTime.Ticks);
        var provider=new FixtureProvider(game,target);var action=new GameProcessPriorityAction(_games,provider);

        Assert.Throws<HbxException>(()=>action.Validate(Json.Write(target with{GameId=Guid.NewGuid().ToString("N")})));
        Assert.Throws<HbxException>(()=>action.Validate(Json.Write(target with{ProcessId=target.ProcessId+1})));

        provider.Priority=ProcessPriorityClass.High;
        var engine=Engine(action);
        var error=await Assert.ThrowsAsync<HbxException>(()=>engine.PreviewAsync(action.Info.Id,Json.Write(target),default));
        Assert.Contains("preserved",error.Message);
        Assert.Equal(0,provider.WriteCount);
    }

    [Fact]public async Task RejectedProviderWriteDoesNotCreateVerifiedSuccess()
    {
        var game=AddGame();var target=new GameProcessPriorityTarget(game.Id,513250,DateTimeOffset.UtcNow.UtcDateTime.Ticks);
        var provider=new FixtureProvider(game,target){RejectWrites=true};var action=new GameProcessPriorityAction(_games,provider);
        var engine=Engine(action);var preview=await engine.PreviewAsync(action.Info.Id,Json.Write(target),default);

        var result=await engine.ApplyAsync(preview.Token,true,null,default);

        Assert.Equal("FailedRestored",result.Status);Assert.Equal("Normal",result.Observed?.Value);
        Assert.Equal(ProcessPriorityClass.Normal,provider.Priority);Assert.Contains("fixture rejected",result.Message,StringComparison.OrdinalIgnoreCase);
    }

    [Fact]public void ReattachTreatsReusedProcessIdAsTheOriginalGameHavingEnded()
    {
        var game=AddGame();using var current=Process.GetCurrentProcess();
        var identity=new GameProcessIdentity(current.Id,current.StartTime.ToUniversalTime().Ticks+1);
        Assert.Throws<GameProcessEndedException>(()=>GameProcesses.Reattach(game,identity));
    }

    [Fact]public void InstalledEpicDiscoveryRequiresARealManifestAndExecutable()
    {
        var epicRoot=Path.Combine(_root,"EpicManifests");
        var installRoot=Path.Combine(_root,"EpicGame");
        Directory.CreateDirectory(epicRoot);
        Directory.CreateDirectory(installRoot);
        var executable=Path.Combine(installRoot,"FixtureGame.exe");
        File.WriteAllText(executable,"Fixture executable; never launched.");
        File.WriteAllText(Path.Combine(epicRoot,"fixture.item"),
            Json.Write(new{DisplayName="Fixture Game",InstallLocation=installRoot,LaunchExecutable="FixtureGame.exe"}));

        var discovered=GameProcesses.DiscoverInstalledGames(Array.Empty<string>(),new[]{epicRoot},default);

        var candidate=Assert.Single(discovered);
        Assert.Equal("Fixture Game",candidate.Name);
        Assert.Equal(executable,candidate.Executable);
        Assert.Equal("Epic",candidate.Source);
    }

    private GameEntry AddGame()
    {
        Directory.CreateDirectory(_root);
        var executable=Path.Combine(_root,Guid.NewGuid().ToString("N")+".exe");
        File.WriteAllText(executable,"Never executed: game process priority fixture");
        return _games.Add("Fixture",executable);
    }

    private ActionEngine Engine(GameProcessPriorityAction action)=>new([action],_journal,new SafetyGuard(()=>null),"fixture-owner");

    public void Dispose()
    {
        var root=Path.GetFullPath(_root);var parent=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"HyperBoostX-tests"))+Path.DirectorySeparatorChar;
        if(root.StartsWith(parent,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(root))Directory.Delete(root,true);
    }

    private sealed class TestProtector:IStateProtector
    {
        public byte[] Protect(byte[] value)=>value;
        public byte[] Unprotect(byte[] value)=>value;
    }

    private sealed class FixtureProvider(GameEntry game,GameProcessPriorityTarget target):IGameProcessPriorityProvider
    {
        public ProcessPriorityClass Priority {get;set;}=ProcessPriorityClass.Normal;
        public bool Ended {get;set;}
        public bool RejectWrites {get;set;}
        public bool ExitOnRestore {get;set;}
        public int WriteCount {get;private set;}

        public GameProcessPrioritySnapshot Read(GameProcessPriorityTarget actual,string executable)
        {
            ValidateTarget(actual,executable);
            if(Ended)throw new GameProcessEndedException();
            return new(actual.ProcessId,actual.StartTimeUtcTicks,game.Executable,Process.GetCurrentProcess().SessionId,Priority);
        }

        public void Set(GameProcessPriorityTarget actual,string executable,ProcessPriorityClass priority)
        {
            ValidateTarget(actual,executable);
            if(Ended)throw new GameProcessEndedException();
            if(ExitOnRestore&&priority==ProcessPriorityClass.Normal){Ended=true;throw new GameProcessEndedException();}
            WriteCount++;
            if(RejectWrites)throw new HbxException("Fixture rejected the priority write.");
            Priority=priority;
        }

        private void ValidateTarget(GameProcessPriorityTarget actual,string executable)
        {
            if(actual!=target||!string.Equals(executable,game.Executable,StringComparison.OrdinalIgnoreCase))
                throw new HbxException("Fixture process identity mismatch.");
        }
    }
}

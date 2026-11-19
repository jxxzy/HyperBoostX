using System.Diagnostics;
using System.Text.Json;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public sealed record GameProcessPriorityTarget(string GameId,int ProcessId,long StartTimeUtcTicks);
public sealed record GameProcessPrioritySnapshot(int ProcessId,long StartTimeUtcTicks,string Executable,int SessionId,ProcessPriorityClass Priority);

public interface IGameProcessPriorityProvider
{
    GameProcessPrioritySnapshot Read(GameProcessPriorityTarget target,string executable);
    void Set(GameProcessPriorityTarget target,string executable,ProcessPriorityClass priority);
}

public sealed class WindowsGameProcessPriorityProvider:IGameProcessPriorityProvider
{
    public GameProcessPrioritySnapshot Read(GameProcessPriorityTarget target,string executable)
    {
        using var process=OpenValidated(target,executable);
        return Snapshot(process);
    }

    public void Set(GameProcessPriorityTarget target,string executable,ProcessPriorityClass priority)
    {
        if(priority is not (ProcessPriorityClass.BelowNormal or ProcessPriorityClass.Normal or ProcessPriorityClass.AboveNormal))
            throw new HbxException("HBX-GAME-PRIORITY-0004: Only Normal, BelowNormal and AboveNormal priority states are supported.");
        using var process=OpenValidated(target,executable);
        process.PriorityClass=priority;
    }

    private static Process OpenValidated(GameProcessPriorityTarget target,string executable)
    {
        Process process;
        try{process=Process.GetProcessById(target.ProcessId);}
        catch(ArgumentException){throw new GameProcessEndedException();}
        try
        {
            if(process.HasExited||process.StartTime.ToUniversalTime().Ticks!=target.StartTimeUtcTicks)
                throw new GameProcessEndedException();
            var actual=SafePaths.Local(process.MainModule?.FileName??"");
            if(!string.Equals(actual,GameLibrary.ValidateExecutable(executable),StringComparison.OrdinalIgnoreCase))
                throw new HbxException("HBX-GAME-PRIORITY-0003: The process executable no longer matches the selected game.");
            using var current=Process.GetCurrentProcess();
            if(process.Id==current.Id||process.SessionId!=current.SessionId)
                throw new HbxException("HBX-GAME-PRIORITY-0005: Only a different game process in the current Windows session can be changed.");
            return process;
        }
        catch
        {
            process.Dispose();
            throw;
        }
    }

    private static GameProcessPrioritySnapshot Snapshot(Process process)=>
        new(process.Id,process.StartTime.ToUniversalTime().Ticks,SafePaths.Local(process.MainModule?.FileName??""),process.SessionId,process.PriorityClass);
}

public sealed class GameProcessPriorityAction:ISystemAction
{
    private readonly GameLibrary _games;
    private readonly IGameProcessPriorityProvider _provider;

    public GameProcessPriorityAction(GameLibrary games,IGameProcessPriorityProvider? provider=null)
    {
        _games=games??throw new ArgumentNullException(nameof(games));
        _provider=provider??new WindowsGameProcessPriorityProvider();
    }

    public ActionInfo Info {get;}=new("gaming.process.priority.above_normal","Set game process priority to AboveNormal","Gaming",
        "Raise only the selected, registered game process from Normal or BelowNormal to AboveNormal for this session. This does not promise higher FPS; the original priority is journaled and restored when the session ends.",
        "Selected game process in the current Windows session","Moderate · reversible","Restore when the game session ends","Windows Process API");

    public static string Target(GameEntry game,Process process)
    {
        ArgumentNullException.ThrowIfNull(game);ArgumentNullException.ThrowIfNull(process);
        var identity=GameProcesses.CaptureIdentity(game,process);
        return Json.Write(new GameProcessPriorityTarget(game.Id,identity.ProcessId,identity.StartTimeUtcTicks));
    }

    public void Validate(string target)
    {
        var identity=Parse(target);var game=FindGame(identity.GameId);
        var snapshot=_provider.Read(identity,game.Executable);
        ValidateSnapshot(identity,game,snapshot);
    }

    public void ValidateRestore(string target,State before)
    {
        var identity=Parse(target);var game=FindGame(identity.GameId);
        if(!before.Exists||before.Kind!="ProcessPriority"||!Enum.TryParse<ProcessPriorityClass>(before.Value,false,out var priority)||
           priority is not (ProcessPriorityClass.Normal or ProcessPriorityClass.BelowNormal))
            throw new HbxException("HBX-GAME-PRIORITY-0006: Saved game priority recovery state is invalid.");
        try{ValidateSnapshot(identity,game,_provider.Read(identity,game.Executable));}
        catch(GameProcessEndedException){return;}
    }

    public Task<State> ReadAsync(string target,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var identity=Parse(target);var game=FindGame(identity.GameId);
        try
        {
            var snapshot=_provider.Read(identity,game.Executable);ValidateSnapshot(identity,game,snapshot);
            return Task.FromResult(new State(true,"ProcessPriority",snapshot.Priority.ToString()));
        }
        catch(GameProcessEndedException){return Task.FromResult(new State(false,"ProcessPriority","ProcessExited"));}
    }

    public State Desired(string target,State before)
    {
        if(!before.Exists||before.Kind!="ProcessPriority"||!Enum.TryParse<ProcessPriorityClass>(before.Value,false,out var priority))
            throw new HbxException("HBX-GAME-PRIORITY-0006: Current game priority state is invalid.");
        if(priority==ProcessPriorityClass.AboveNormal)return before;
        if(priority is not (ProcessPriorityClass.Normal or ProcessPriorityClass.BelowNormal))
            throw new HbxException("HBX-GAME-PRIORITY-0007: Only Normal or BelowNormal process priority is eligible; other priorities are preserved.");
        return new State(true,"ProcessPriority",ProcessPriorityClass.AboveNormal.ToString());
    }

    public bool IsRestoreSatisfied(State current,State before)=>before.Exists&&before.Kind=="ProcessPriority"&&
        (before.Value=="Normal"||before.Value=="BelowNormal")&&!current.Exists&&current.Kind=="ProcessPriority"&&current.Value=="ProcessExited";

    public Task WriteAsync(string target,State state,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();var identity=Parse(target);var game=FindGame(identity.GameId);
        if(!state.Exists||state.Kind!="ProcessPriority"||!Enum.TryParse<ProcessPriorityClass>(state.Value,false,out var priority)||
           priority is not (ProcessPriorityClass.Normal or ProcessPriorityClass.BelowNormal or ProcessPriorityClass.AboveNormal))
            throw new HbxException("HBX-GAME-PRIORITY-0006: Saved game priority state is invalid.");
        ValidateSnapshot(identity,game,_provider.Read(identity,game.Executable));
        _provider.Set(identity,game.Executable,priority);
        return Task.CompletedTask;
    }

    private GameEntry FindGame(string id)=>_games.List().FirstOrDefault(game=>game.Id==id)
        ??throw new HbxException("HBX-GAME-PRIORITY-0001: The selected game is no longer registered.");

    private static void ValidateSnapshot(GameProcessPriorityTarget target,GameEntry game,GameProcessPrioritySnapshot snapshot)
    {
        using var current=Process.GetCurrentProcess();
        if(snapshot.ProcessId!=target.ProcessId||snapshot.StartTimeUtcTicks!=target.StartTimeUtcTicks||
           !string.Equals(SafePaths.Local(snapshot.Executable),GameLibrary.ValidateExecutable(game.Executable),StringComparison.OrdinalIgnoreCase)||
           snapshot.SessionId!=current.SessionId||snapshot.ProcessId==current.Id)
            throw new HbxException("HBX-GAME-PRIORITY-0003: The process identity does not match the selected game in this session.");
    }

    private static GameProcessPriorityTarget Parse(string target)
    {
        if(string.IsNullOrWhiteSpace(target)||target.Length>512)throw new HbxException("HBX-GAME-PRIORITY-0001: Game process target is invalid.");
        GameProcessPriorityTarget identity;
        try{identity=Json.Read<GameProcessPriorityTarget>(target);}
        catch(Exception exception)when(exception is JsonException or InvalidDataException){throw new HbxException("HBX-GAME-PRIORITY-0001: Game process target is invalid.");}
        if(!Guid.TryParseExact(identity.GameId,"N",out _)||identity.ProcessId<=0||identity.StartTimeUtcTicks<=0)
            throw new HbxException("HBX-GAME-PRIORITY-0001: Game process target is invalid.");
        return identity;
    }
}

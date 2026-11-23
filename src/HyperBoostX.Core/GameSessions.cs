namespace HyperBoostX.Core;

public sealed record GameProcessIdentity(int ProcessId,long StartTimeUtcTicks);
public sealed record GamingSession(string Id,string GameId,string GameName,string Status,DateTimeOffset StartedAt,string Message="",int? ProcessId=null,long? ProcessStartTimeUtcTicks=null);
public sealed class GameSessions(ActionEngine engine,Journal journal,DocumentStore store)
{
    public GamingSession? Current=>store.Read<GamingSession?>("gaming-session",null);
    public bool Active=>Current is {Status:not "Restored"};
    private FileStream Lock()=>new(SafePaths.Under(journal.Root,"gaming-session.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
    private void Save(GamingSession session)=>store.Update<GamingSession?>("gaming-session",null,_=>session);
    public async Task<GamingSession> StartAsync(GameEntry game,IReadOnlyList<Preview> previews,bool consent,CancellationToken token)
    {
        if(!consent)throw new HbxException("HBX-GAME-0007: Profile approval is required.");
        using var gate=Lock();if(Active)throw new HbxException("HBX-GAME-0008: Restore the existing session first.");
        if(!GameLibrary.Actions(game.Profile).SequenceEqual(previews.Select(p=>(p.ActionId,p.Target))))throw new HbxException("HBX-GAME-0011: Preview does not match the selected profile.");
        GameLibrary.ValidateExecutable(game.Executable);
        var session=new GamingSession(Guid.NewGuid().ToString("N"),game.Id,game.Name,"Preparing",DateTimeOffset.UtcNow);
        Save(session);
        try
        {
            foreach(var preview in previews)
            {
                var change=await engine.ApplyAsync(preview.Token,true,session.Id,token);
                if(change.Status is not ("Verified" or "AlreadyDesiredState"))throw new HbxException(change.Message);
            }
            session=session with{Status="Active",Message="Profile applied. Restore this session when finished."};Save(session);return session;
        }
        catch(OperationCanceledException)
        {
            // Provider-originated cancellation must also propagate, even if the
            // caller token has not been canceled. A canceled profile application still requires best-effort recovery.
            // Use a fresh token so cancellation cannot suppress rollback, but
            // never report the canceled start as a successful session.
            await RestoreCore(session with{Message="Profile application was canceled."},CancellationToken.None);
            throw;
        }
        catch(Exception e)
        {
            return await RestoreCore(session with{Message="Profile failed: "+e.Message},CancellationToken.None);
        }
    }
    public GamingSession AttachProcess(string sessionId,GameProcessIdentity identity)
    {
        if(identity is null||identity.ProcessId<=0||identity.StartTimeUtcTicks<=0)throw new HbxException("HBX-GAME-0013: Game process identity is invalid.");
        using var gate=Lock();var session=Current??throw new HbxException("HBX-GAME-0010: No gaming session exists.");
        if(session.Id!=sessionId||session.Status!="Active")throw new HbxException("HBX-GAME-0014: Process identity does not belong to the active gaming session.");
        if(session.ProcessId is not null&&(session.ProcessId!=identity.ProcessId||session.ProcessStartTimeUtcTicks!=identity.StartTimeUtcTicks))
            throw new HbxException("HBX-GAME-0015: A different process is already attached to this gaming session.");
        if(!new GameLibrary(store).List().Any(game=>game.Id==session.GameId))throw new HbxException("HBX-GAME-0006: Game no longer exists in the library.");
        session=session with{ProcessId=identity.ProcessId,ProcessStartTimeUtcTicks=identity.StartTimeUtcTicks,Message="Profile applied. The exact game process is monitored for automatic restore."};
        Save(session);return session;
    }
    public async Task<GamingSession> RestoreAsync(bool consent,CancellationToken token)
    {
        if(!consent)throw new HbxException("HBX-GAME-0009: Session restore approval is required.");
        using var gate=Lock();var session=Current??throw new HbxException("HBX-GAME-0010: No gaming session exists.");
        return await RestoreCore(session,token);
    }
    private async Task<GamingSession> RestoreCore(GamingSession session,CancellationToken token)
    {
        Save(session with{Status="Restoring"});var problems=new List<string>();
        foreach(var change in journal.SessionHistory(session.Id).Where(c=>c.Recoverable))
        {
            try
            {
                var result=await engine.RollbackAsync(change.Id,true,token);
                if(result.Status=="RollbackCancelled" && token.IsCancellationRequested)
                {
                    // The action engine persists a retryable cancellation rather than
                    // throwing; propagate cancellation to session callers as well.
                    Save(session with{Status="RecoveryRequired",Message="Session restore was canceled. Retry restore to recover any changes that remain applied."});
                    throw new OperationCanceledException(token);
                }
                if(result.Status!="RolledBack")problems.Add(result.ActionId+": "+result.Status);
            }
            catch(OperationCanceledException)
            {
                Save(session with{Status="RecoveryRequired",Message="Session restore was canceled. Retry restore to recover any changes that remain applied."});
                throw;
            }
            catch(Exception e){problems.Add(e.Message);}
        }
        // Cancellation can arrive after the last recoverable journal entry (or when
        // there are none). Do not report a completed restore to the caller when its
        // cancellation was never acknowledged. A retry rechecks the durable journal.
        if(token.IsCancellationRequested)
        {
            Save(session with{Status="RecoveryRequired",Message="Session restore was canceled. Retry restore to verify the session is fully recovered."});
            throw new OperationCanceledException(token);
        }
        var resultSession=session with{Status=problems.Count==0?"Restored":"RecoveryRequired",Message=problems.Count==0?"Session changes restored and verified. "+session.Message:string.Join("\n",problems)};
        Save(resultSession);return resultSession;
    }
}

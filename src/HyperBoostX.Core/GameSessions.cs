namespace HyperBoostX.Core;

public sealed record GamingSession(string Id,string GameId,string GameName,string Status,DateTimeOffset StartedAt,string Message="");
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
        catch(Exception e)
        {
            return await RestoreCore(session with{Message="Profile failed: "+e.Message},CancellationToken.None);
        }
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
        foreach(var change in journal.History(10000).Where(c=>c.Session==session.Id&&c.Recoverable))
        {
            try
            {
                var result=await engine.RollbackAsync(change.Id,true,token);
                if(result.Status!="RolledBack")problems.Add(result.ActionId+": "+result.Status);
            }
            catch(OperationCanceledException) when(token.IsCancellationRequested)
            {
                Save(session with{Status="RecoveryRequired",Message="Session restore was canceled. Retry restore to recover any changes that remain applied."});
                throw;
            }
            catch(Exception e){problems.Add(e.Message);}
        }
        var resultSession=session with{Status=problems.Count==0?"Restored":"RecoveryRequired",Message=problems.Count==0?"Session changes restored and verified. "+session.Message:string.Join("\n",problems)};
        Save(resultSession);return resultSession;
    }
}

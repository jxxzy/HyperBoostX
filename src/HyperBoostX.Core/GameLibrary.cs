namespace HyperBoostX.Core;

public sealed record GameProfile(string? PowerPlan=null,bool DisableCapture=false,bool DisableTransparency=false);
public sealed record GameEntry(string Id,string Name,string Executable,GameProfile Profile,DateTimeOffset AddedAt)
{
    public override string ToString()=>Name;
}
public sealed class GameLibrary(DocumentStore store)
{
    public IReadOnlyList<GameEntry> List()=>store.Read("games",new List<GameEntry>());
    public static string ValidateExecutable(string path)
    {
        if(string.IsNullOrWhiteSpace(path))throw new HbxException("HBX-GAME-0001: Select an existing local game executable (.exe).");
        var full=SafePaths.Local(path);
        if(!full.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)||!File.Exists(full))throw new HbxException("HBX-GAME-0001: Select an existing local game executable (.exe).");
        return full;
    }
    private static string ValidateName(string name)
    {
        name=name?.Trim()??"";
        if(name.Length is <1 or >160||name.Any(char.IsControl))throw new HbxException("HBX-GAME-0002: Enter a game name of 1–160 characters.");
        return name;
    }
    private static GameProfile ValidateProfile(GameProfile? profile)
    {
        if(profile is null)throw new HbxException("HBX-GAME-0005: Game profile is missing.");
        if(profile.PowerPlan!=null&&!Guid.TryParseExact(profile.PowerPlan,"D",out _))throw new HbxException("HBX-GAME-0005: Invalid power-plan ID.");
        return profile;
    }
    public GameEntry Add(string name,string executable)
    {
        var path=ValidateExecutable(executable);name=ValidateName(name);
        var game=new GameEntry(Guid.NewGuid().ToString("N"),name,path,new(),DateTimeOffset.UtcNow);
        store.Update("games",new List<GameEntry>(),items=>
        {
            if(items.Count>=500)throw new HbxException("HBX-GAME-0003: Library limit: 500 games.");
            if(items.Any(g=>string.Equals(g.Executable,path,StringComparison.OrdinalIgnoreCase)))throw new HbxException("HBX-GAME-0004: This executable is already in the library.");
            items.Add(game);return items;
        });return game;
    }
    public int Import(IReadOnlyCollection<GameEntry> entries)
    {
        if(entries is null)throw new HbxException("HBX-GAME-0006: Imported game list is missing.");
        if(entries.Count>500)throw new HbxException("HBX-GAME-0003: Import at most 500 games.");
        var imported=entries.Select(game=>
        {
            if(game is null)throw new HbxException("HBX-GAME-0006: Imported game entry is missing.");
            return new GameEntry(Guid.NewGuid().ToString("N"),ValidateName(game.Name),ValidateExecutable(game.Executable),ValidateProfile(game.Profile),DateTimeOffset.UtcNow);
        }).ToArray();
        if(imported.Length==0)return 0;
        var added=0;
        store.Update("games",new List<GameEntry>(),items=>
        {
            foreach(var game in imported)
            {
                if(items.Any(existing=>string.Equals(existing.Executable,game.Executable,StringComparison.OrdinalIgnoreCase)))continue;
                if(items.Count>=500)throw new HbxException("HBX-GAME-0003: Library limit: 500 games.");
                items.Add(game);added++;
            }
            return items;
        });
        return added;
    }
    public void Remove(string id)=>store.Update("games",new List<GameEntry>(),items=>{items.RemoveAll(g=>g.Id==id);return items;});
    public void SaveProfile(string id,GameProfile profile)
    {
        profile=ValidateProfile(profile);
        store.Update("games",new List<GameEntry>(),items=>
        {
            var index=items.FindIndex(g=>g.Id==id);if(index<0)throw new HbxException("HBX-GAME-0006: Game no longer exists.");
            items[index]=items[index] with{Profile=profile};return items;
        });
    }
    public static IReadOnlyList<(string ActionId,string Target)> Actions(GameProfile profile)
    {
        var actions=new List<(string,string)>();
        if(profile.PowerPlan is {} plan)actions.Add(("windows.power.set_plan",plan));
        if(profile.DisableCapture)actions.Add(("gaming.capture.disable",""));
        if(profile.DisableTransparency)actions.Add(("windows.visual.transparency.disable",""));
        return actions;
    }
}

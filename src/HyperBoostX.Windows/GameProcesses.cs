using System.Diagnostics;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public sealed class GameProcessEndedException:Exception
{
    public GameProcessEndedException():base("The game process has exited or its process ID now identifies a different process."){}
}

public static class GameProcesses
{
    public sealed record InstalledGameCandidate(string Name,string? Executable,string Source,string? InstallDirectory=null)
    {
        public string DisplayName=>$"{Name} · {Source}"+(Executable is null?" · select executable":"");
    }

    public static IReadOnlyList<InstalledGameCandidate> DiscoverInstalledGames(CancellationToken token)
        =>BuildCandidates([InstalledGameDetection.Scan(token)],token);

    public static IReadOnlyList<InstalledGameCandidate> DiscoverInstalledGames(IReadOnlyList<string> steamRoots,IReadOnlyList<string> epicManifestRoots,CancellationToken token)
    {
        IEnumerable<InstalledGameDetection.Result> Results()
        {
            yield return InstalledGameDetection.ScanLocations(steamRoots,null,token);
            foreach(var root in epicManifestRoots.Take(32))
            {
                token.ThrowIfCancellationRequested();
                yield return InstalledGameDetection.ScanLocations([],root,token);
            }
        }
        return BuildCandidates(Results(),token);
    }

    public static IReadOnlyList<InstalledGameCandidate> BuildCandidates(IEnumerable<InstalledGameDetection.Result> results,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var candidates=new List<InstalledGameCandidate>();
        var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Read(InstalledGameDetection.Result result)
        {
            foreach(var game in result.Games)
            {
                token.ThrowIfCancellationRequested();
                if(candidates.Count>=500)return;
                var executable=game.Executable??(game.Store=="Steam"?FindLikelyExecutable(game.InstallDirectory,game.Name,token):null);
                var identity=executable is null?"directory|"+game.InstallDirectory:"executable|"+executable;
                if(!seen.Add(identity))continue;
                candidates.Add(new(game.Name,executable,game.Store=="Epic Games"?"Epic":game.Store,game.InstallDirectory));
            }
        }
        foreach(var result in results)
        {
            token.ThrowIfCancellationRequested();
            if(candidates.Count>=500)break;
            Read(result);
        }
        return candidates.OrderBy(c=>c.Source,StringComparer.OrdinalIgnoreCase).ThenBy(c=>c.Name,StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static string ValidateDiscoveredExecutable(InstalledGameCandidate candidate,string selectedPath)
    {
        var path=GameLibrary.ValidateExecutable(selectedPath);
        if(candidate.InstallDirectory is not {} directory)
            throw new HbxException("Discovered game installation directory is unavailable.");
        var root=SafePaths.Local(directory);
        var relative=Path.GetRelativePath(root,path);
        if(Path.IsPathRooted(relative)||relative==".."||relative.StartsWith(".."+Path.DirectorySeparatorChar,StringComparison.Ordinal))
            throw new HbxException("Select an executable inside the discovered game installation.");
        return SafePaths.Under(root,relative);
    }

    private static string? FindLikelyExecutable(string root,string gameName,CancellationToken token)
    {
        // Bounded traversal, no following reparse points or scanning outside the game root.
        var pending=new Stack<(string Path,int Depth)>();
        try{pending.Push((SafePaths.Local(root),0));}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or HbxException){return null;}
        var candidates=new List<(string Path,int Score)>();var examined=0;
        while(pending.Count>0&&examined<2000)
        {
            token.ThrowIfCancellationRequested();
            var (folder,depth)=pending.Pop();
            try
            {
                foreach(var entry in Directory.EnumerateFileSystemEntries(SafePaths.Local(folder)))
                {
                    token.ThrowIfCancellationRequested();
                    if(++examined>2000)break;
                    try
                    {
                        var attrs=File.GetAttributes(entry);
                        if((attrs&FileAttributes.ReparsePoint)!=0)continue;
                        if((attrs&FileAttributes.Directory)!=0)
                        {if(depth<4)pending.Push((entry,depth+1));continue;}
                        if(!entry.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))continue;
                        var safe=SafePaths.Local(entry);
                        var score=ExecutableScore(safe,gameName);
                        if(score>=50)candidates.Add((safe,score)); // do not guess arbitrary executable names
                    }
                    catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or HbxException){}
                }
            }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or HbxException){}
        }
        return candidates.OrderByDescending(c=>c.Score).ThenBy(c=>c.Path.Length).Select(c=>c.Path).FirstOrDefault();
    }
    private static int ExecutableScore(string path,string gameName)
    {
        var name=Path.GetFileNameWithoutExtension(path);
        var relative=Path.GetDirectoryName(path)?.Length>=0?path:"";
        var lower=relative.ToLowerInvariant();
        if(lower.Contains("\\commonredist\\")||lower.Contains("\\redistributable\\")||lower.Contains("\\support\\")||lower.Contains("\\_commonredist\\"))return -100;
        if(name.Length==0)return -100;
        var lowerName=name.ToLowerInvariant();
        if(lowerName.Contains("uninstall")||lowerName.Contains("crash")||lowerName.Contains("setup")||lowerName.Contains("launcher")||lowerName.Contains("installer")||lowerName.Contains("redist"))return -20;
        var normalizedName=gameName.ToLowerInvariant();
        var score=normalizedName.Length>0&&lowerName.Contains(normalizedName)?100:0;
        if(lowerName.Replace(" ","").Contains(normalizedName.Replace(" ","")))score+=50;
        if(relative.Split('\\').Length<=4)score+=10;
        return score>0?score:lowerName.Length>3?1:-100;
    }
    public static DiagnosticReport Scan(IReadOnlyList<GameEntry> games,CancellationToken token)
    {
        var facts=new List<Fact>();var inaccessible=0;
        foreach(var process in Process.GetProcesses())using(process)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                if(!games.Any(g=>string.Equals(Path.GetFileNameWithoutExtension(g.Executable),process.ProcessName,StringComparison.OrdinalIgnoreCase)))continue;
                var path=process.MainModule?.FileName;
                foreach(var game in games.Where(g=>string.Equals(g.Executable,path,StringComparison.OrdinalIgnoreCase)))
                    facts.Add(new(game.Name,$"Running · PID {process.Id}","Exact executable path match"));
            }
            catch(Exception e)when(e is System.ComponentModel.Win32Exception or InvalidOperationException or NotSupportedException){inaccessible++;}
        }
        if(facts.Count==0)facts.Add(new("Running games","No accessible process matched a saved game executable.","Process API"));
        if(inaccessible>0)facts.Add(new("Unavailable processes",inaccessible.ToString(),"Process API","Unavailable","Unavailable"));
        return new("Game Library",DateTimeOffset.UtcNow,facts,[]);
    }
    public static Process Launch(GameEntry game)
    {
        var path=GameLibrary.ValidateExecutable(game.Executable);
        return Process.Start(new ProcessStartInfo(path){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(path)!,WindowStyle=ProcessWindowStyle.Normal})??throw new HbxException("HBX-GAME-0012: Game did not start.");
    }
    public static GameProcessIdentity CaptureIdentity(GameEntry game,Process process)
    {
        ArgumentNullException.ThrowIfNull(game);ArgumentNullException.ThrowIfNull(process);
        var executable=GameLibrary.ValidateExecutable(game.Executable);
        if(process.HasExited)throw new GameProcessEndedException();
        var actual=SafePaths.Local(process.MainModule?.FileName??"");
        if(!string.Equals(actual,executable,StringComparison.OrdinalIgnoreCase))
            throw new HbxException("HBX-GAME-0016: Launched process does not match the registered game executable.");
        using var current=Process.GetCurrentProcess();
        if(process.Id==current.Id||process.SessionId!=current.SessionId)
            throw new HbxException("HBX-GAME-0017: Game process is not running in the current Windows session.");
        return new(process.Id,process.StartTime.ToUniversalTime().Ticks);
    }
    public static Process Reattach(GameEntry game,GameProcessIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(game);ArgumentNullException.ThrowIfNull(identity);
        if(identity.ProcessId<=0||identity.StartTimeUtcTicks<=0)throw new HbxException("HBX-GAME-0013: Game process identity is invalid.");
        var executable=GameLibrary.ValidateExecutable(game.Executable);
        Process process;
        try{process=Process.GetProcessById(identity.ProcessId);}
        catch(ArgumentException){throw new GameProcessEndedException();}
        try
        {
            if(process.HasExited||process.StartTime.ToUniversalTime().Ticks!=identity.StartTimeUtcTicks)throw new GameProcessEndedException();
            var actual=SafePaths.Local(process.MainModule?.FileName??"");
            if(!string.Equals(actual,executable,StringComparison.OrdinalIgnoreCase))
                throw new HbxException("HBX-GAME-0018: Process identity no longer matches the registered game; automatic restore is blocked.");
            using var current=Process.GetCurrentProcess();
            if(process.Id==current.Id||process.SessionId!=current.SessionId)
                throw new HbxException("HBX-GAME-0019: Game process is not accessible in the current Windows session; manual restore is available.");
            return process;
        }
        catch
        {
            process.Dispose();throw;
        }
    }
}

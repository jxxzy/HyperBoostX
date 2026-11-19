using System.Diagnostics;
using System.Text.Json;
using HyperBoostX.Core;
using Microsoft.Win32;

namespace HyperBoostX.Windows;

public sealed class GameProcessEndedException:Exception
{
    public GameProcessEndedException():base("The game process has exited or its process ID now identifies a different process."){}
}

public static class GameProcesses
{
    public sealed record InstalledGameCandidate(string Name,string Executable,string Source);

    public static IReadOnlyList<InstalledGameCandidate> DiscoverInstalledGames(CancellationToken token)
        =>DiscoverInstalledGames(SteamRoots().ToArray(),new[]{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"Epic","EpicGamesLauncher","Data","Manifests")},token);

    public static IReadOnlyList<InstalledGameCandidate> DiscoverInstalledGames(IReadOnlyList<string> steamRoots,IReadOnlyList<string> epicManifestRoots,CancellationToken token)
    {
        var candidates = new List<InstalledGameCandidate>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var steamRoot in steamRoots)
        {
            token.ThrowIfCancellationRequested();
            DiscoverSteam(candidates,seen,steamRoot,token);
        }

        foreach (var manifestRoot in epicManifestRoots.Where(Directory.Exists))
        {
            token.ThrowIfCancellationRequested();
            DiscoverEpic(candidates,seen,manifestRoot,token);
        }

        return candidates
            .Where(candidate => candidate.Executable.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)&&File.Exists(candidate.Executable))
            .OrderBy(candidate=>candidate.Source,StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate=>candidate.Name,StringComparer.OrdinalIgnoreCase)
            .Take(500)
            .ToArray();
    }

    private static IEnumerable<string> SteamRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var view in new[]{RegistryView.Registry64,RegistryView.Registry32})
        {
            foreach (var hive in new[]{RegistryHive.CurrentUser,RegistryHive.LocalMachine})
            {
                try
                {
                    using var baseKey=RegistryKey.OpenBaseKey(hive,view);
                    foreach (var subKeyName in new[]{"SOFTWARE\\Valve\\Steam","SOFTWARE\\WOW6432Node\\Valve\\Steam"})
                    {
                        using var key=baseKey.OpenSubKey(subKeyName);
                        if(key?.GetValue("InstallPath") is string installPath&&!string.IsNullOrWhiteSpace(installPath))
                            roots.Add(Path.GetFullPath(installPath));
                    }
                }
                catch(UnauthorizedAccessException){}catch(IOException){}
            }
        }
        var steamApps=Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if(!string.IsNullOrWhiteSpace(steamApps))
        {
            var commonPath=Path.Combine(steamApps,"Steam");
            if(Directory.Exists(commonPath))roots.Add(commonPath);
        }
        return roots;
    }

    private static void DiscoverSteam(List<InstalledGameCandidate> candidates,HashSet<string> seen,string steamRoot,CancellationToken token)
    {
        var steamApps=Path.Combine(steamRoot,"steamapps");
        var roots=new List<string>{steamApps};
        if(Directory.Exists(steamApps))
        {
            try
            {
                foreach(var line in ReadTextFilesByPrefix(steamApps,"libraryfolders.vdf",1))
                {
                    token.ThrowIfCancellationRequested();
                    var match=System.Text.RegularExpressions.Regex.Match(line,"\"path\"\\s+\"(?<path>[^\"]+)\"");
                    if(match.Success)
                    {
                        var path=match.Groups["path"].Value.Replace("\\\\","\\");
                        var libraryApps=Path.Combine(path,"steamapps");
                        if(Directory.Exists(libraryApps))roots.Add(libraryApps);
                    }
                }
            }
            catch(IOException){}
        }

        foreach(var libraryApps in roots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string[] manifests;
            try{manifests=Directory.GetFiles(libraryApps,"appmanifest_*.acf",SearchOption.TopDirectoryOnly);}
            catch(UnauthorizedAccessException){continue;}catch(IOException){continue;}
            foreach(var manifest in manifests)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    var values=ReadKeyValues(File.ReadAllText(manifest));
                    if(!values.TryGetValue("name",out var name)||!values.TryGetValue("installdir",out var installDir)||string.IsNullOrWhiteSpace(name)||string.IsNullOrWhiteSpace(installDir))
                        continue;
                    var installRoot=Path.Combine(libraryApps,"common",installDir);
                    if(!Directory.Exists(installRoot))continue;
                    var executable=FindLikelyExecutable(installRoot,name);
                    if(executable is null||!seen.Add(executable))continue;
                    candidates.Add(new InstalledGameCandidate(name,executable,"Steam"));
                }
                catch(IOException){}
                catch(UnauthorizedAccessException){}
            }
        }
    }

    private static void DiscoverEpic(List<InstalledGameCandidate> candidates,HashSet<string> seen,string manifestRoot,CancellationToken token)
    {
        string[] manifests;
        try{manifests=Directory.GetFiles(manifestRoot,"*.item",SearchOption.TopDirectoryOnly);}
        catch(UnauthorizedAccessException){return;}catch(IOException){return;}
        foreach(var manifest in manifests)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                using var document=JsonDocument.Parse(File.ReadAllText(manifest));
                var root=document.RootElement;
                if(!TryGetJsonString(root,"DisplayName",out var name)||!TryGetJsonString(root,"InstallLocation",out var installLocation)||!TryGetJsonString(root,"LaunchExecutable",out var launchExecutable))
                    continue;
                var executable=Path.GetFullPath(Path.Combine(installLocation,launchExecutable));
                if(!executable.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)||!File.Exists(executable)||!seen.Add(executable))continue;
                candidates.Add(new InstalledGameCandidate(name,executable,"Epic"));
            }
            catch(JsonException){}
            catch(IOException){}
            catch(UnauthorizedAccessException){}
        }
    }

    private static bool TryGetJsonString(JsonElement root,string property,out string value)
    {
        value="";
        if(!root.TryGetProperty(property,out var element)||element.ValueKind!=JsonValueKind.String)return false;
        value=element.GetString()?.Trim()??"";
        return value.Length>0;
    }

    private static IEnumerable<string> ReadTextFilesByPrefix(string root,string fileName,int _)
    {
        var path=Path.Combine(root,fileName);
        if(File.Exists(path))yield return File.ReadAllText(path);
    }

    private static Dictionary<string,string> ReadKeyValues(string content)
    {
        var values=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach(System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(content,"\"(?<key>[^\"]+)\"\\s+\"(?<value>[^\"]+)\""))
        {
            var key=match.Groups["key"].Value.Trim();
            var value=match.Groups["value"].Value.Trim();
            if(key is "name" or "installdir")values[key]=value;
        }
        return values;
    }

    private static string? FindLikelyExecutable(string root,string gameName)
    {
        string[] executables;
        try{executables=Directory.GetFiles(root,"*.exe",SearchOption.AllDirectories);}
        catch(UnauthorizedAccessException){return null;}catch(IOException){return null;}
        var scored=executables.Select(path=>new{Path=path,Score=ExecutableScore(path,gameName)})
            .Where(item=>item.Score>0)
            .OrderByDescending(item=>item.Score)
            .ThenBy(item=>item.Path.Length)
            .ToArray();
        return scored.FirstOrDefault()?.Path;
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

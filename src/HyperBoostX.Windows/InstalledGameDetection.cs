using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public static class InstalledGameDetection
{
    private const int FileLimit=1024*1024;
    private const int ManifestLimit=500;
    private const int RootLimit=32;
    private static readonly Regex VdfLine=new("^[\\t ]*\"(?<key>[^\"\\r\\n]+)\"[\\t ]+\"(?<value>(?:\\\\.|[^\"\\\\\\r\\n])*)\"[\\t ]*\\r?$",RegexOptions.Multiline|RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(250));
    public sealed record GogRecord(string? Name,string? InstallDirectory);
    public sealed record Candidate(string Name,string InstallDirectory,string Store,string? Executable);
    public sealed record Result(IReadOnlyList<Candidate> Games,IReadOnlyList<string> Issues,bool Truncated)
    {
        public DiagnosticReport Report()
        {
            var facts=new List<Fact>{
                new("Installed game candidates",Games.Count.ToString(),"Local launcher manifests and registry","Direct",Truncated||Issues.Count>0?"Limited":"Available"),
                new("Coverage",Truncated?"Limited: scan bounds reached":"Local Steam/Epic manifests and GOG installation registry when available; not an exhaustive game inventory","Local discovery","Guidance",Truncated||Issues.Count>0?"Limited":"Available")
            };
            foreach(var game in Games)facts.Add(new(game.Name,$"{game.InstallDirectory} · {(game.Executable is null?"Select direct game executable manually":game.Executable)}",game.Store=="GOG"?"GOG installation registry":game.Store+" manifest","Direct"));
            foreach(var issue in Issues.Take(100))facts.Add(new("Discovery limitation",issue,"Local launcher manifests","Unavailable","Unavailable"));
            if(Games.Count==0)facts.Add(new("Game discovery","No accessible matching installed game manifests found; this does not prove no games are installed.","Local launcher manifests","Unavailable","Unavailable"));
            return new("Game Library",DateTimeOffset.UtcNow,facts,[]);
        }
    }

    private static IEnumerable<string> SteamRoots()
    {
        foreach(var (hive,path) in new[]{(RegistryHive.CurrentUser,@"Software\Valve\Steam"),(RegistryHive.LocalMachine,@"SOFTWARE\Valve\Steam")})
        foreach(var view in new[]{RegistryView.Registry32,RegistryView.Registry64})
        {
            string? root=null;
            try
            {
                using var baseKey=RegistryKey.OpenBaseKey(hive,view);
                using var key=baseKey.OpenSubKey(path);
                root=key?.GetValue("SteamPath") as string??key?.GetValue("InstallPath") as string;
            }
            catch(Exception e) when(Recoverable(e)){}
            if(!string.IsNullOrWhiteSpace(root))yield return root;
        }
        var programFiles=Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if(!string.IsNullOrWhiteSpace(programFiles))yield return Path.Combine(programFiles,"Steam");
    }
    public static Result Scan(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var issues=new List<string>();
        var discovered=ScanLocations(SteamRoots(),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"Epic","EpicGamesLauncher","Data","Manifests"),token,ReadGogRecords(issues,token));
        return discovered with { Issues=discovered.Issues.Concat(issues).Take(100).ToArray() };
    }

    // Read installation metadata only. A registry entry is not permission to execute a game.
    private static IEnumerable<GogRecord> ReadGogRecords(List<string> issues,CancellationToken token)
    {
        foreach(var view in new[]{RegistryView.Registry32,RegistryView.Registry64})
        {
            token.ThrowIfCancellationRequested();
            var records=new List<GogRecord>();
            try
            {
                using var root=RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,view);
                using var games=root.OpenSubKey(@"SOFTWARE\GOG.com\Games");
                if(games is null)continue;
                foreach(var id in games.GetSubKeyNames().Take(ManifestLimit+1))
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        using var key=games.OpenSubKey(id);
                        records.Add(new(key?.GetValue("gameName",null,RegistryValueOptions.DoNotExpandEnvironmentNames) as string,
                            key?.GetValue("path",null,RegistryValueOptions.DoNotExpandEnvironmentNames) as string));
                    }
                    catch(Exception e) when(Recoverable(e)){if(issues.Count<100)issues.Add($"GOG registry entry: {e.GetType().Name}");}
                }
            }
            catch(Exception e) when(Recoverable(e)){if(issues.Count<100)issues.Add($"GOG registry {view}: {e.GetType().Name}");}
            foreach(var record in records)yield return record;
        }
    }

    public static Result ScanLocations(IEnumerable<string> steamRoots,string? epicManifests,CancellationToken token,IEnumerable<GogRecord>? gogRecords=null)
    {
        token.ThrowIfCancellationRequested();
        var games=new List<Candidate>();var issues=new List<string>();var truncated=false;
        var roots=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Issue(string source,Exception e){if(issues.Count<100)issues.Add($"{source}: {e.GetType().Name}");}
        void AddRoot(string path)
        {
            try
            {
                if(!Path.IsPathFullyQualified(path))throw new HbxException("Relative launcher root.");
                var root=SafePaths.Local(path);
                if(!Directory.Exists(root)||roots.Contains(root))return;
                if(roots.Count>=RootLimit){truncated=true;return;}
                roots.Add(root);
            }
            catch(Exception e) when(Recoverable(e)){Issue("Steam root",e);}
        }
        var rootCount=0;
        foreach(var root in steamRoots.Take(RootLimit+1))
        {
            token.ThrowIfCancellationRequested();
            if(rootCount++>=RootLimit){truncated=true;break;}
            AddRoot(root);
        }
        foreach(var root in roots.ToArray())
        {
            token.ThrowIfCancellationRequested();
            try
            {
                var libraries=SafePaths.Under(root,Path.Combine("steamapps","libraryfolders.vdf"));
                if(!File.Exists(libraries))continue;
                foreach(Match match in VdfLine.Matches(ReadText(libraries,token)))
                {
                    token.ThrowIfCancellationRequested();
                    var key=match.Groups["key"].Value;
                    var value=Unescape(match.Groups["value"].Value);
                    // Older Steam libraryfolders.vdf uses numeric keys directly for local library paths.
                    if(key.Equals("path",StringComparison.OrdinalIgnoreCase)||
                        (uint.TryParse(key,out _)&&Path.IsPathFullyQualified(value)))AddRoot(value);
                }
            }
            catch(Exception e) when(Recoverable(e)){Issue("Steam library folders",e);}
        }
        var scanned=0;
        void ScanManifests(string folder,string pattern,string source,Func<string,Candidate?> parse)
        {
            try
            {
                var safe=SafePaths.Local(folder);
                if(!Directory.Exists(safe))return;
                foreach(var file in Directory.EnumerateFiles(safe,pattern,SearchOption.TopDirectoryOnly))
                {
                    token.ThrowIfCancellationRequested();
                    if(scanned>=ManifestLimit){truncated=true;break;}
                    scanned++;
                    try
                    {
                        if(parse(ReadText(SafePaths.Local(file),token)) is {} game&&visited.Add(game.Store+"|"+game.InstallDirectory))games.Add(game);
                    }
                    catch(Exception e) when(Recoverable(e)){Issue(source,e);}
                }
            }
            catch(Exception e) when(Recoverable(e)){Issue(source,e);}
        }
        foreach(var root in roots)
        {
            token.ThrowIfCancellationRequested();
            var folder=Path.Combine(root,"steamapps");
            ScanManifests(folder,"appmanifest_*.acf","Steam manifest",text=>ParseSteam(text,folder));
        }
        if(!string.IsNullOrWhiteSpace(epicManifests))
            ScanManifests(epicManifests,"*.item","Epic manifest",ParseEpic);
        if(gogRecords is not null)
        {
            var count=0;
            foreach(var record in gogRecords)
            {
                token.ThrowIfCancellationRequested();
                if(count++>=ManifestLimit){truncated=true;break;}
                try
                {
                    if(record.Name is not {} name||!ValidName(name)||record.InstallDirectory is not {} directory||
                       !Path.IsPathFullyQualified(directory))continue;
                    var install=SafePaths.Local(directory);
                    if(Directory.Exists(install)&&visited.Add("GOG|"+install))
                        games.Add(new(name,install,"GOG",null));
                }
                catch(Exception e) when(Recoverable(e)){Issue("GOG installation directory",e);}
            }
        }
        return new(games.OrderBy(g=>g.Name,StringComparer.OrdinalIgnoreCase).ToArray(),issues,truncated);
    }

    private static Candidate? ParseSteam(string text,string steamApps)
    {
        var fields=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach(Match match in VdfLine.Matches(text))fields.TryAdd(match.Groups["key"].Value,Unescape(match.Groups["value"].Value));
        if(!fields.TryGetValue("appid",out var appid)||!uint.TryParse(appid,out var id)||id==0||
           !fields.TryGetValue("name",out var name)||!ValidName(name)||
           !fields.TryGetValue("installdir",out var directory)||string.IsNullOrWhiteSpace(directory))return null;
        if(Path.IsPathRooted(directory)||directory.Contains('/')||directory.Contains('\\')||directory is "." or "..")return null;
        var install=SafePaths.Under(Path.Combine(steamApps,"common"),directory);
        return Directory.Exists(install)?new(name,install,"Steam",null):null;
    }

    private static Candidate? ParseEpic(string text)
    {
        using var doc=JsonDocument.Parse(text,new JsonDocumentOptions{MaxDepth=32});
        var root=doc.RootElement;
        if(root.ValueKind!=JsonValueKind.Object)return null;
        string? Field(string key)=>root.TryGetProperty(key,out var value)&&value.ValueKind==JsonValueKind.String?value.GetString():null;
        if(root.TryGetProperty("bIsIncompleteInstall",out var incomplete)&&incomplete.ValueKind==JsonValueKind.True)return null;
        if(root.TryGetProperty("bIsApplication",out var app)&&app.ValueKind==JsonValueKind.False)return null;
        var name=Field("DisplayName");var directory=Field("InstallLocation");
        if(name is null||!ValidName(name)||directory is null||!Path.IsPathFullyQualified(directory))return null;
        var install=SafePaths.Local(directory);
        if(!Directory.Exists(install))return null;
        string? exe=null;
        var relative=Field("LaunchExecutable");
        if(!string.IsNullOrWhiteSpace(relative)&&!Path.IsPathRooted(relative))
        {
            try
            {
                var candidate=SafePaths.Under(install,relative);
                if(candidate.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)&&File.Exists(candidate))exe=candidate;
            }
            catch(Exception e) when(Recoverable(e)){}
        }
        return new(name,install,"Epic Games",exe);
    }

    private static bool ValidName(string name)=>name.Length is >0 and <=160&&!string.IsNullOrWhiteSpace(name)&&!name.Any(char.IsControl);
    private static bool Recoverable(Exception e)=>e is IOException or UnauthorizedAccessException or
        ArgumentException or NotSupportedException or HbxException or System.Security.SecurityException or JsonException or RegexMatchTimeoutException;
    private static string Unescape(string value)=>value.Replace("\\\\","\\").Replace("\\\"","\"");
    private static string ReadText(string path,CancellationToken token)
    {
        using var input=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
        if(input.Length>FileLimit)throw new HbxException("Manifest exceeds 1 MiB.");
        using var output=new MemoryStream();
        var buffer=new byte[8192];
        int count;
        while((count=input.Read(buffer,0,buffer.Length))>0)
        {
            token.ThrowIfCancellationRequested();
            if(output.Length+count>FileLimit)throw new HbxException("Manifest exceeds 1 MiB.");
            output.Write(buffer,0,count);
        }
        return Encoding.UTF8.GetString(output.ToArray()).TrimStart('\uFEFF');
    }
}

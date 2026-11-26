using System.IO;
using System.Text.Json;
using HyperBoostX.Windows;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class InstalledGameDetectionTests:IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"HyperBoostX-discovery-tests",Guid.NewGuid().ToString("N"));
    [Fact] public void SteamAndEpicManifestsDetectExistingDirectoriesWithoutLaunchingOrRegistering()
    {
        var steam=Path.Combine(root,"Steam");
        var extra=Path.Combine(root,"Extra");
        var steamApps=Path.Combine(steam,"steamapps");
        var install=Path.Combine(extra,"steamapps","common","Example");
        Directory.CreateDirectory(steamApps);Directory.CreateDirectory(install);
        File.WriteAllText(Path.Combine(steamApps,"libraryfolders.vdf"),"\"libraryfolders\"\n{\n \"1\"\n {\n \"path\" \""+extra.Replace("\\","\\\\")+"\"\n }\n}");
        File.WriteAllText(Path.Combine(extra,"steamapps","appmanifest_100.acf"),"\"AppState\"\n{\n \"appid\" \"100\"\n \"name\" \"Steam Example\"\n \"installdir\" \"Example\"\n}");
        var epic=Path.Combine(root,"EpicManifests");var epicInstall=Path.Combine(root,"EpicGame");
        Directory.CreateDirectory(epic);Directory.CreateDirectory(epicInstall);
        var exe=Path.Combine(epicInstall,"Game.exe");File.WriteAllText(exe,"not an executable; must never be launched");
        File.WriteAllText(Path.Combine(epic,"example.item"),JsonSerializer.Serialize(new {DisplayName="Epic Example",InstallLocation=epicInstall,LaunchExecutable="Game.exe"}));
        var result=InstalledGameDetection.ScanLocations([steam],epic,default);
        Assert.Equal(2,result.Games.Count);
        Assert.Contains(result.Games,g=>g.Name=="Steam Example"&&g.InstallDirectory==install&&g.Executable is null);
        Assert.Contains(result.Games,g=>g.Name=="Epic Example"&&g.Executable==exe);
        Assert.False(result.Truncated);Assert.Empty(result.Issues);
    }

    [Fact] public void InvalidOrIncompleteManifestsAndEscapedExecutablesDoNotBecomeLaunchTargets()
    {
        var epic=Path.Combine(root,"EpicManifests");var install=Path.Combine(root,"Game");
        Directory.CreateDirectory(epic);Directory.CreateDirectory(install);File.WriteAllText(Path.Combine(root,"outside.exe"),"fixture");
        File.WriteAllText(Path.Combine(epic,"bad.item"),"{bad json");
        File.WriteAllText(Path.Combine(epic,"incomplete.item"),JsonSerializer.Serialize(new {DisplayName="Incomplete",InstallLocation=install,bIsIncompleteInstall=true}));
        File.WriteAllText(Path.Combine(epic,"escaped.item"),JsonSerializer.Serialize(new {DisplayName="Escape",InstallLocation=install,LaunchExecutable="..\\outside.exe"}));
        var result=InstalledGameDetection.ScanLocations([],epic,default);
        Assert.Null(Assert.Single(result.Games).Executable);Assert.NotEmpty(result.Issues);
    }

    [Fact] public void EmptyLocationsReportUnavailableNotProofOfNoGames()
    {
        var result=InstalledGameDetection.ScanLocations([],Path.Combine(root,"missing"),default);
        Assert.Empty(result.Games);
        Assert.Contains(result.Report().Facts,f=>f.Name=="Game discovery"&&f.Availability=="Unavailable");
    }

    [Fact] public void CancellationStopsDiscovery()
    {
        Assert.ThrowsAny<OperationCanceledException>(()=>InstalledGameDetection.ScanLocations([root],null,new CancellationToken(true)));
    }

    [Fact] public void GogRegistryCandidatesRequireExistingLocalDirectoriesAndManualExecutableSelection()
    {
        var install=Path.Combine(root,"GogGame");Directory.CreateDirectory(install);
        File.WriteAllText(Path.Combine(install,"Game.exe"),"fixture only");
        var records=new InstalledGameDetection.GogRecord[]{
            new("GOG Example",install),new("Duplicate",install),new("Missing",Path.Combine(root,"missing")),
            new("Relative","relative"),new("Bad name\n",install),new(null,install)
        };
        var result=InstalledGameDetection.ScanLocations([],null,default,records);
        var game=Assert.Single(result.Games);
        Assert.Equal("GOG",game.Store);Assert.Equal("GOG Example",game.Name);
        Assert.Equal(install,game.InstallDirectory);Assert.Null(game.Executable);
        Assert.Contains(result.Report().Facts,f=>f.Source=="GOG installation registry");
    }

    [Fact] public void LegacySteamNumericLibraryPathIsDiscovered()
    {
        var steam=Path.Combine(root,"Steam");var extra=Path.Combine(root,"Extra");
        var apps=Path.Combine(steam,"steamapps");var install=Path.Combine(extra,"steamapps","common","Example");
        Directory.CreateDirectory(apps);Directory.CreateDirectory(install);
        File.WriteAllText(Path.Combine(apps,"libraryfolders.vdf"),"\"LibraryFolders\"\n{\n \"1\" \""+extra.Replace("\\","\\\\")+"\"\n}");
        File.WriteAllText(Path.Combine(extra,"steamapps","appmanifest_100.acf"),"\"AppState\"\n{\n \"appid\" \"100\"\n \"name\" \"Old format\"\n \"installdir\" \"Example\"\n}");
        Assert.Equal(install,Assert.Single(InstalledGameDetection.ScanLocations([steam],null,default).Games).InstallDirectory);
    }

    [Fact] public void BoundsAndCancellationApplyEvenToEmptyOrDuplicateSources()
    {
        var records=Enumerable.Repeat(new InstalledGameDetection.GogRecord(null,null),501);
        Assert.True(InstalledGameDetection.ScanLocations([],null,default,records).Truncated);
        Assert.True(InstalledGameDetection.ScanLocations(Enumerable.Repeat(root,33),null,default).Truncated);
        Assert.ThrowsAny<OperationCanceledException>(()=>InstalledGameDetection.ScanLocations([],null,new CancellationToken(true)));
    }

    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}

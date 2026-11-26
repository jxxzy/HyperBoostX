using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Input;
using HyperBoostX.Core;
using HyperBoostX.Windows;
using Microsoft.Win32;

namespace HyperBoostX.App;

public sealed partial class ShellViewModel
{
    public bool IsGameLibrary=>Title=="Game Library";
    public bool IsPerGameProfiles=>Title=="Per-Game Profiles";
    public bool HasRegisteredGames=>Games.Count>0;
    public bool HasGameProfileHistory=>GameProfileHistory.Count>0;
    public bool IsBenchmark=>SurfaceTitle=="Benchmark Lab";
    public bool IsStutterAnalyzer=>Title=="Stutter Analyzer";
    public bool IsResearchMode=>Title=="Research Mode";
    public bool IsCleanup=>SurfaceTitle=="Cleanup";
    public ObservableCollection<CleanupCandidate> CleanupItems {get;}=[];
    private CleanupCandidate? _selectedCleanup;
    public CleanupCandidate? SelectedCleanup{get=>_selectedCleanup;set{_selectedCleanup=value;Notify();CommandManager.InvalidateRequerySuggested();}}
    public ICommand ScanCleanupCommand{get;private set;}=null!;
    public ObservableCollection<GameEntry> Games {get;}=[];
    public ObservableCollection<GameProcesses.InstalledGameCandidate> DiscoveredGames {get;}=[];
    public ObservableCollection<Change> GameProfileHistory {get;}=[];
    public ObservableCollection<BenchmarkEntry> BenchmarkHistory {get;}=[];
    public ObservableCollection<ResearchTrial> ResearchHistory {get;}=[];
    private GameEntry? _selectedGame;
    private GameProcesses.InstalledGameCandidate? _selectedDiscoveredGame;
    public GameProcesses.InstalledGameCandidate? SelectedDiscoveredGame{get=>_selectedDiscoveredGame;set{_selectedDiscoveredGame=value;Notify();CommandManager.InvalidateRequerySuggested();}}
    public GameEntry? SelectedGame
    {
        get=>_selectedGame;
        set{_selectedGame=value;Notify();if(value!=null){ProfileUsePowerPlan=value.Profile.PowerPlan!=null;ProfileDisableCapture=value.Profile.DisableCapture;ProfileDisableTransparency=value.Profile.DisableTransparency;ProfileRaisePriority=value.Profile.RaisePriority;SelectedPlan=Plans.FirstOrDefault(p=>p.Id==value.Profile.PowerPlan)??Plans.FirstOrDefault();}else{ProfileUsePowerPlan=false;ProfileDisableCapture=false;ProfileDisableTransparency=false;ProfileRaisePriority=false;SelectedPlan=null;}CommandManager.InvalidateRequerySuggested();}
    }
    private bool _profileUsePowerPlan,_profileDisableCapture,_profileDisableTransparency,_profileRaisePriority;
    public bool ProfileUsePowerPlan{get=>_profileUsePowerPlan;set{_profileUsePowerPlan=value;Notify();}}
    public bool ProfileDisableCapture{get=>_profileDisableCapture;set{_profileDisableCapture=value;Notify();}}
    public bool ProfileDisableTransparency{get=>_profileDisableTransparency;set{_profileDisableTransparency=value;Notify();}}
    public bool ProfileRaisePriority{get=>_profileRaisePriority;set{_profileRaisePriority=value;Notify();}}
    public string GamingSessionStatus=>Runtime.GamingSessions.Current is {} session?$"{session.GameName}: {session.Status} · {session.Message}":"Tidak ada sesi gaming.";
    public bool HasGamingSession=>Runtime.GamingSessions.Active;
    public string BenchmarkGame{get;set;}="";
    public string BenchmarkAverage{get;set;}="";
    public string BenchmarkLow{get;set;}="";
    public string BenchmarkFrametime{get;set;}="";
    public string BenchmarkNotes{get;set;}="";
    public string ResearchExperiment{get;set;}="Experiment 1";
    public string ResearchWorkload{get;set;}="";
    public string ResearchVariant{get;set;}="A";
    public string ResearchMetric{get;set;}="Average FPS";
    public string ResearchValue{get;set;}="";
    public string ResearchUnit{get;set;}="FPS";
    public string ResearchNotes{get;set;}="";
    public string[] ResearchVariants{get;}=["A","B","C","D"];
    private BenchmarkEntry? _selectedBenchmark,_baselineBenchmark;
    public BenchmarkEntry? SelectedBenchmark{get=>_selectedBenchmark;set{_selectedBenchmark=value;Notify();if(value!=null)Show(Benchmarks.Report(value));CommandManager.InvalidateRequerySuggested();}}
    public BenchmarkEntry? BaselineBenchmark{get=>_baselineBenchmark;set{_baselineBenchmark=value;Notify();CommandManager.InvalidateRequerySuggested();}}
    public ICommand AddGameCommand{get;private set;}=null!;
    public ICommand RemoveGameCommand{get;private set;}=null!;
    public ICommand ImportGamesCommand{get;private set;}=null!;
    public ICommand SaveGameProfileCommand{get;private set;}=null!;
    public ICommand StartGameCommand{get;private set;}=null!;
    public ICommand RestoreGameCommand{get;private set;}=null!;
    public ICommand ScanGamesCommand{get;private set;}=null!;
    public ICommand ExportGamesCommand{get;private set;}=null!;
    public ICommand DiscoverGamesCommand{get;private set;}=null!;
    public ICommand RegisterDiscoveredGameCommand{get;private set;}=null!;
    public ICommand AddBenchmarkCommand{get;private set;}=null!;
    public ICommand ImportBenchmarkCommand{get;private set;}=null!;
    public ICommand CompareBenchmarksCommand{get;private set;}=null!;
    public ICommand CpuBenchmarkCommand{get;private set;}=null!;
    public ICommand StorageBenchmarkCommand{get;private set;}=null!;
    public ICommand ImportFrametimeCommand{get;private set;}=null!;
    public ICommand AddResearchTrialCommand{get;private set;}=null!;
    public ICommand AnalyzeResearchCommand{get;private set;}=null!;
    private void InitializeLegacyFeatures()
    {
        CpuBenchmarkCommand=new Command(()=>Run(async t=>{var report=await LocalBenchmarks.CpuAsync(t);Runtime.Journal.SaveReport(report);Show(report);Status="Pengukuran SHA-256 CPU selesai; hasil tersimpan di Reports.";}),()=>!Busy);
        StorageBenchmarkCommand=new Command(()=>Run(async t=>{if(!Confirm("Uji membutuhkan ruang kosong minimal 128 MiB, menulis file sementara 64 MiB, mengukur pembacaan, memverifikasi isi file, lalu menghapus file tes sendiri. Hasil baca mencakup cache Windows. Lanjutkan?","Storage benchmark"))return;var report=await LocalBenchmarks.StorageAsync(Runtime.Journal.Root,t);Runtime.Journal.SaveReport(report);Show(report);Status="Pengukuran storage selesai; file tes sudah dibersihkan.";}),()=>!Busy);
        ImportFrametimeCommand=new Command(()=>Run(async t=>
        {
            var dialog=new OpenFileDialog{Filter="Frametime CSV|*.csv",Title="Import frametime capture (single presentation stream)"};
            if(dialog.ShowDialog()!=true)return;
            var report=await Task.Run(()=>FrameTimeAnalysis.AnalyzeFile(dialog.FileName,t),t);
            t.ThrowIfCancellationRequested();Runtime.Journal.SaveReport(report);Show(report);Status="Frametime capture analyzed and stored in Reports.";
        }),()=>!Busy&&IsStutterAnalyzer);
        AddResearchTrialCommand=new Command(()=>Run(_=>{var trial=Runtime.Research.Add(ResearchExperiment,ResearchWorkload,ResearchVariant,ResearchMetric,ParseNumber(ResearchValue),ResearchUnit,ResearchNotes);RefreshResearch();Status=$"Research trial {trial.Variant} saved locally.";return Task.CompletedTask;}),()=>!Busy&&IsResearchMode);
        AnalyzeResearchCommand=new Command(()=>Run(_=>{var report=Runtime.Research.Report(ResearchExperiment);Runtime.Journal.SaveReport(report);Show(report);Status="A/B/C/D experiment summary calculated from recorded trials.";return Task.CompletedTask;}),()=>!Busy&&IsResearchMode&&ResearchHistory.Any(t=>string.Equals(t.Experiment,ResearchExperiment,StringComparison.OrdinalIgnoreCase)));
        ScanCleanupCommand=new Command(()=>Run(async t=>
        {
            var scan=await Task.Run(()=>Runtime.Cleanup.Scan(t),t);CleanupItems.Clear();foreach(var file in scan.Items)CleanupItems.Add(file);SelectedCleanup=CleanupItems.FirstOrDefault();
            var facts=new List<Fact>{new("Scope","User temp .tmp files older than 7 days, <=128 MiB; depth <=4; at most 200 candidates / 10,000 entries.","Cleanup policy"),new("Examined",scan.Examined.ToString(),"Filesystem scan"),new("Skipped / unavailable",scan.Skipped.ToString(),"Filesystem scan"),new("Scan limit reached",scan.Truncated.ToString(),"Filesystem scan"),new("Quarantine policy","Files are retained for Restore. Quarantine does not free disk space and is not encrypted; it remains in this user's local app data.","Recovery policy")};
            facts.AddRange(scan.Items.Select(f=>new Fact(f.Path,f.Size,"Filesystem metadata")));var report=new DiagnosticReport("Cleanup",DateTimeOffset.UtcNow,facts,[]);Runtime.Journal.SaveReport(report);Show(report);Status=$"{scan.Items.Count} kandidat ditemukan; pilih file lalu Preview & Apply.";
        }),()=>!Busy);
        AddGameCommand=new Command(()=>Run(_=>{var dialog=new OpenFileDialog{Filter="Game executable|*.exe",Title="Pilih executable game langsung (bukan launcher)"};if(dialog.ShowDialog()==true){var game=Runtime.Games.Add(Path.GetFileNameWithoutExtension(dialog.FileName),dialog.FileName);RefreshGames(game.Id);Status="Game tersimpan lokal.";}return Task.CompletedTask;}),()=>!Busy);
        RemoveGameCommand=new Command(()=>Run(_=>{if(SelectedGame is {} game&&Confirm($"Hapus {game.Name} dari library? File game tidak dihapus.","Game Library")){Runtime.Games.Remove(game.Id);RefreshGames();}return Task.CompletedTask;}),()=>!Busy&&SelectedGame!=null&&!HasGamingSession);
        ImportGamesCommand=new Command(()=>Run(_=>
        {
            var dialog=new OpenFileDialog{Filter="HyperBoostX game library|*.json",Title="Import game library"};
            if(dialog.ShowDialog()==true)
            {
                if(new FileInfo(dialog.FileName).Length>4*1024*1024)throw new HbxException("Game library JSON maksimal 4 MiB.");
                var entries=Json.Read<GameEntry[]>(File.ReadAllText(dialog.FileName));
                var imported=Runtime.Games.Import(entries);RefreshGames();
                Status=$"{imported} game diimpor; {entries.Length-imported} duplikat diabaikan.";
            }
            return Task.CompletedTask;
        }),()=>!Busy&&!HasGamingSession);
        SaveGameProfileCommand=new Command(()=>Run(_=>{SaveGameProfile();Status="Profil tersimpan. Windows belum diubah.";return Task.CompletedTask;}),()=>!Busy&&SelectedGame!=null&&!HasGamingSession);
        StartGameCommand=new Command(()=>Run(StartGame),()=>!Busy&&SelectedGame!=null&&!HasGamingSession);
        RestoreGameCommand=new Command(()=>Run(async t=>{if(Confirm("Pulihkan perubahan sesi gaming? Game yang berjalan tidak akan ditutup.","Restore session"))await RestoreGaming(t);}),()=>!Busy&&HasGamingSession);
        ScanGamesCommand=new Command(()=>Run(async t=>{var games=Runtime.Games.List();var report=await Task.Run(()=>GameProcesses.Scan(games,t),t);Runtime.Journal.SaveReport(report);Show(report);Status="Pencocokan executable game selesai.";}),()=>!Busy);
        ExportGamesCommand=new Command(()=>Run(_=>{ExportGameLibrary();return Task.CompletedTask;}),()=>!Busy&&Games.Count>0);
        DiscoverGamesCommand=new Command(()=>Run(async t=>
        {
            var candidates=await Task.Run(()=>GameProcesses.DiscoverInstalledGames(t),t);
            DiscoveredGames.Clear();
            foreach(var candidate in candidates)DiscoveredGames.Add(candidate);
            SelectedDiscoveredGame=DiscoveredGames.FirstOrDefault();
            Status=candidates.Count>0
                ?$"{candidates.Count} game installation candidate(s) found. Review or select an executable before registration."
                :"No accessible Steam, Epic or GOG game installation was found. This does not prove no games are installed.";
        }),()=>!Busy);
        RegisterDiscoveredGameCommand=new Command(()=>Run(_=>
        {
            if(SelectedDiscoveredGame is not {} candidate)return Task.CompletedTask;
            var executable=candidate.Executable;
            if(executable is null)
            {
                var dialog=new OpenFileDialog{Filter="Game executable|*.exe",Title="Select the game executable",InitialDirectory=candidate.InstallDirectory??"",CheckFileExists=true};
                if(dialog.ShowDialog()!=true)return Task.CompletedTask;
                executable=dialog.FileName;
            }
            executable=GameProcesses.ValidateDiscoveredExecutable(candidate,executable);
            if(!Confirm($"Register {candidate.Name}?\n{executable}\n\nSource: {candidate.Source}\n\nThis adds the executable only; no Windows setting is changed.","Register discovered game"))return Task.CompletedTask;
            var game=Runtime.Games.Add(candidate.Name,executable);
            DiscoveredGames.Remove(candidate);
            SelectedDiscoveredGame=DiscoveredGames.FirstOrDefault();
            RefreshGames(game.Id);
            Status="Discovered game registered locally. Configure and save its profile when ready.";
            return Task.CompletedTask;
        }),()=>!Busy&&SelectedDiscoveredGame!=null&&!HasGamingSession);
        AddBenchmarkCommand=new Command(()=>Run(_=>{var item=Runtime.Benchmarks.Add(BenchmarkGame,ParseNumber(BenchmarkAverage),ParseNumber(BenchmarkLow),ParseNumber(BenchmarkFrametime),BenchmarkNotes);RefreshBenchmarks(item.Id);Runtime.Journal.SaveReport(Benchmarks.Report(item));Status="Hasil benchmark tersimpan sebagai data yang dimasukkan pengguna.";return Task.CompletedTask;}),()=>!Busy);
        ImportBenchmarkCommand=new Command(()=>Run(_=>{var dialog=new OpenFileDialog{Filter="Benchmark CSV|*.csv"};if(dialog.ShowDialog()==true){if(new FileInfo(dialog.FileName).Length>4*1024*1024)throw new HbxException("CSV maksimal 4 MiB.");var count=Runtime.Benchmarks.ImportCsv(File.ReadAllText(dialog.FileName));RefreshBenchmarks();Status=$"{count} benchmark diimpor.";}return Task.CompletedTask;}),()=>!Busy);
        CompareBenchmarksCommand=new Command(()=>Run(_=>{if(BaselineBenchmark!=null&&SelectedBenchmark!=null){var report=Benchmarks.Compare(BaselineBenchmark,SelectedBenchmark);Runtime.Journal.SaveReport(report);Show(report);}return Task.CompletedTask;}),()=>!Busy&&BaselineBenchmark!=null&&SelectedBenchmark!=null&&BaselineBenchmark.Id!=SelectedBenchmark.Id);
    }
    private static double ParseNumber(string text)=>double.TryParse(text,NumberStyles.Float,CultureInfo.CurrentCulture,out var value)||double.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out value)?value:throw new HbxException("Masukkan angka FPS/frametime yang valid.");
    private void LoadLegacyPage()
    {
        if(IsGameLibrary||IsPerGameProfiles)
        {
            Plans.Clear();try{foreach(var plan in PowerAction.Plans())Plans.Add(plan);}catch(Exception e){Status=e.Message;}
            RefreshGames();Notify(nameof(GamingSessionStatus));Notify(nameof(HasGamingSession));Notify(nameof(HasRegisteredGames));
        }
        if(IsPerGameProfiles)RefreshGameProfileHistory();
        if(IsBenchmark)RefreshBenchmarks();
        if(IsResearchMode)RefreshResearch();
        if(IsNotifications)RefreshNotifications();
        Notify(nameof(IsAiCenter));Notify(nameof(IsNotifications));Notify(nameof(IsIntegration));
        if(SurfaceTitle=="License Center")Show(new DiagnosticReport("License Center",DateTimeOffset.UtcNow,
            [new("Application version",typeof(ShellViewModel).Assembly.GetName().Version?.ToString()??"Unavailable","Assembly metadata"),
             new("Local entitlement","All implemented features are available. This build has no key activation, Free/Pro tiers, device limits, or subscription server.","Application policy"),
             new("Cloud sync","Not implemented; local data is not uploaded automatically.","Application policy"),
             new("Protected storage","Game profiles, benchmarks, reports and recovery metadata use Windows DPAPI for this user. Quarantine preserves file bytes locally.","Storage implementation"),
             new("Release status","Development — full master release gates are not yet met.","Release policy")],[]));
        Notify(nameof(IsGameLibrary));Notify(nameof(IsPerGameProfiles));Notify(nameof(HasRegisteredGames));Notify(nameof(HasGameProfileHistory));Notify(nameof(IsBenchmark));Notify(nameof(IsStutterAnalyzer));Notify(nameof(IsResearchMode));Notify(nameof(IsCleanup));
    }
    private void RefreshGames(string? selectedId=null)
    {
        selectedId??=SelectedGame?.Id;Games.Clear();foreach(var game in Runtime.Games.List())Games.Add(game);
        SelectedGame=Games.FirstOrDefault(g=>g.Id==selectedId)??Games.FirstOrDefault();
        Notify(nameof(HasRegisteredGames));
    }
    private void RefreshGameProfileHistory()
    {
        GameProfileHistory.Clear();
        foreach(var change in Runtime.Journal.History(10000).Where(change=>change.ActionId.StartsWith("gaming.",StringComparison.Ordinal)))
            GameProfileHistory.Add(change);
        Notify(nameof(HasGameProfileHistory));
    }
    private void SaveGameProfile()
    {
        if(SelectedGame is not {} game)return;
        if(ProfileUsePowerPlan&&SelectedPlan==null)throw new HbxException("Pilih power plan yang tersedia.");
        Runtime.Games.SaveProfile(game.Id,new(ProfileUsePowerPlan?SelectedPlan?.Id:null,ProfileDisableCapture,ProfileDisableTransparency,ProfileRaisePriority));RefreshGames(game.Id);
    }
    private async Task StartGame(CancellationToken token)
    {
        SaveGameProfile();if(SelectedGame is not {} game)return;
        var previews=new List<Preview>();
        foreach(var action in GameLibrary.Actions(game.Profile))previews.Add(await Task.Run(()=>Runtime.Engine.PreviewAsync(action.ActionId,action.Target,token),token));
        var changes=previews.Count==0?"Tidak ada perubahan Windows.":string.Join("\n",previews.Select(p=>$"{p.ActionId}: {p.Before} → {p.Desired}"));
        if(game.Profile.RaisePriority)changes+="\nGame process priority: separate post-launch review required; only the selected executable can be changed to AboveNormal.";
        if(!Confirm($"Jalankan {game.Name}?\n{game.Executable}\n\n{changes}\n\nPerubahan dipulihkan saat executable ini keluar. Identitas proses persisten memungkinkan pemantauan dilanjutkan setelah HyperBoostX restart; jika identitas tidak dapat diverifikasi, pemulihan manual diperlukan. Gunakan executable game langsung; launcher dapat keluar sebelum game selesai.\n\nSetujui profil dan pemulihan sesi ini?","Preview gaming session"))return;
        var session=await Task.Run(()=>Runtime.GamingSessions.StartAsync(game,previews,true,token),token);
        NotifyGamingSession();
        if(session.Status!="Active"){Status=session.Status+": "+session.Message;return;}
        System.Diagnostics.Process process;
        try{process=GameProcesses.Launch(game);}
        catch{await RestoreGaming(CancellationToken.None);throw;}
        WatchGame(process,session.Id);
        try
        {
            var identity=GameProcesses.CaptureIdentity(game,process);
            Runtime.GamingSessions.AttachProcess(session.Id,identity);
        }
        catch(Exception exception)
        {
            Status="Game launched, but durable process identity could not be saved. Automatic restore requires HyperBoostX to remain open. "+ReportExport.Redact(exception.Message);
            NotifyGamingSession();return;
        }
        Status="Game launched. The exact process identity is saved for automatic restore after exit or app restart.";
        NotifyGamingSession();
        if(game.Profile.RaisePriority)await ApplyGamePriority(process,game,session.Id,token);
    }
    private async Task ApplyGamePriority(System.Diagnostics.Process process,GameEntry game,string sessionId,CancellationToken token)
    {
        var actionId="gaming.process.priority.above_normal";
        var target=GameProcessPriorityAction.Target(game,process);
        var preview=await Task.Run(()=>Runtime.Engine.PreviewAsync(actionId,target,token),token);
        if(preview.Before==preview.Desired){Status="Game priority is already AboveNormal; no change was needed.";return;}
        var approved=Confirm($"Set {game.Name} process (PID {process.Id}) from {preview.Before.Value} to AboveNormal?\n\nOnly this registered executable is targeted. The original priority is journaled and restored with the gaming session. This does not guarantee higher FPS.","Review game process priority");
        if(!approved){Status="Game launched; process priority was left unchanged. Session auto-restore remains active.";return;}
        var change=await Task.Run(()=>Runtime.Engine.ApplyAsync(preview.Token,true,sessionId,token),token);
        Status=change.Status switch
        {
            "Verified"=>"Game priority set to AboveNormal and verified; it will be restored with the session.",
            "AlreadyDesiredState"=>"Game priority was already AboveNormal; no change was needed.",
            _=>$"Game launched, but priority was not changed: {change.Status}. {change.Message}"
        };
        RefreshHistory();
    }
    private async void WatchGame(System.Diagnostics.Process process,string sessionId)
    {
        try
        {
            using(process)await process.WaitForExitAsync();
            while(Busy)await Task.Delay(250);
            if(Runtime.GamingSessions.Current is {Status:"Active"} session&&session.Id==sessionId)await Execute(RestoreGaming);
        }
        catch(Exception e){Status="Auto-restore membutuhkan pemeriksaan: "+ReportExport.Redact(e.Message);NotifyGamingSession();}
    }
    private async Task RestoreGaming(CancellationToken token)
    {
        var session=await Task.Run(()=>Runtime.GamingSessions.RestoreAsync(true,token),token);Status=session.Status+": "+session.Message;NotifyGamingSession();RefreshHistory();
    }
    private void NotifyGamingSession(){Notify(nameof(GamingSessionStatus));Notify(nameof(HasGamingSession));if(IsPerGameProfiles)RefreshGameProfileHistory();CommandManager.InvalidateRequerySuggested();}
    private void RefreshBenchmarks(string? selectedId=null)
    {
        var baselineId=BaselineBenchmark?.Id;BenchmarkHistory.Clear();foreach(var entry in Runtime.Benchmarks.History())BenchmarkHistory.Add(entry);
        SelectedBenchmark=BenchmarkHistory.FirstOrDefault(b=>b.Id==selectedId)??BenchmarkHistory.FirstOrDefault();BaselineBenchmark=BenchmarkHistory.FirstOrDefault(b=>b.Id==baselineId)??BenchmarkHistory.Skip(1).FirstOrDefault();
    }
    private void RefreshResearch(){ResearchHistory.Clear();foreach(var trial in Runtime.Research.History())ResearchHistory.Add(trial);CommandManager.InvalidateRequerySuggested();}
    private void ExportGameLibrary()
    {
        if(!Confirm("Export berisi nama game, profil, dan path executable lengkap yang dapat mengungkap lokasi file pengguna. Simpan hanya di lokasi tepercaya.","Export game library"))return;
        var dialog=new SaveFileDialog{Filter="JSON|*.json",FileName="HyperBoostX-game-library.json"};if(dialog.ShowDialog()!=true)return;
        File.WriteAllText(dialog.FileName,Json.Write(Runtime.Games.List()));Status="Library game diekspor; file berisi path executable lokal.";
    }
}

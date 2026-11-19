using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using HyperBoostX.Core;
using HyperBoostX.Windows;
using Microsoft.Win32;

namespace HyperBoostX.App;

public sealed class Command(Action execute,Func<bool>? can=null):ICommand
{
    public bool CanExecute(object? parameter)=>can?.Invoke()??true;
    public void Execute(object? parameter)=>execute();
    public event EventHandler? CanExecuteChanged{add{CommandManager.RequerySuggested+=value;}remove{CommandManager.RequerySuggested-=value;}}
}
public sealed partial class ShellViewModel:INotifyPropertyChanged
{
    public Runtime Runtime {get;}
    private CancellationTokenSource? _cts;
    private DiagnosticReport? _report;
    private DiagnosticReport? _pageReport;
    private bool _busy;
    private string _status="";
    private string _search="";
    private string _factFilter="";
    private Navigation? _selected;
    private bool _sidebarCollapsed;
    private ActionInfo? _selectedAction;
    private Fact? _selectedFact;
    private PowerPlan? _selectedPlan;
    private StartupEntry? _selectedStartup;
    private Change? _selectedChange;
    private Handoff? _selectedHandoff;
    private SafeTestRun? _selectedTestRun;
    private SafeTestResult? _selectedTestResult;
    private string _theme;
    private string _goal;
    private string _mode;
    private string _language;
    private string _host="1.1.1.1";
    private string _detail="";
    private string _fileAnalysisRoot="";
    private long _minimumLargeFileMiB=100;
    private string _fileAnalysisProgress="Pilih folder lokal untuk dianalisis. Tidak ada file yang diubah.";
    private string _appLoadExecutable="";
    private double _lcdCpuThreshold=2;
    private long _lcdMemoryThresholdMiB=300;
    private bool _lcdGamingOnly=true;
    public static IReadOnlyList<Navigation> AllNavigation {get;}=FeatureRouteCatalog.All;
    public ObservableCollection<Navigation> Navigation {get;}=new(AllNavigation);
    public ICollectionView NavigationView {get;}
    public ObservableCollection<Fact> Facts {get;}=[];
    public ICollectionView FactsView {get;}
    public ObservableCollection<Finding> Findings {get;}=[];
    public ObservableCollection<ActionInfo> Actions {get;}=[];
    public ObservableCollection<PowerPlan> Plans {get;}=[];
    public ObservableCollection<StartupEntry> StartupItems {get;}=[];
    public ObservableCollection<Change> History {get;}=[];
    public ObservableCollection<DiagnosticReport> Reports {get;}=[];
    public ObservableCollection<SafeTestRun> TestRuns {get;}=[];
    public ObservableCollection<SafeTestResult> TestResults {get;}=[];
    public ObservableCollection<DuplicateFileGroup> DuplicateGroups {get;}=[];
    public ObservableCollection<FileAnalysisEntry> LargeFiles {get;}=[];
    public ObservableCollection<FileAnalysisIssue> FileAnalysisIssues {get;}=[];
    public DiagnosticReport? SelectedReport{get=>_report;set{if(value==null)return;Show(value);Notify();}}
    public ObservableCollection<Handoff> Tools {get;}=[];
    public LocalizationService Localization {get;}
    public IReadOnlyList<LanguageOption> LanguageOptions=>LocalizationService.Languages;
    public string[] Themes {get;}=["Dark","Light"];
    public string[] Goals {get;}=["Everyday","Gaming","Streaming","Music Production","Video Editing","Battery Life","Troubleshooting"];
    public string[] Modes {get;}=["Beginner","Advanced","Expert"];
    public string Theme{get=>_theme;set{_theme=value;Runtime.Journal.SetSetting("theme",value);App.SetTheme(value);Notify();}}
    public string Goal{get=>_goal;set{_goal=value;Runtime.Journal.SetSetting("goal",value);Notify();}}
    public string Mode{get=>_mode;set{_mode=value;Runtime.Journal.SetSetting("mode",value);Notify();Notify(nameof(Expert));}}
    public string Language
    {
        get=>_language;
        set
        {
            var wasReady=string.Equals(_status,Localization["ReadyStatus"],StringComparison.Ordinal);
            Localization.SetLanguage(value);
            _language=Localization.Language;
            Runtime.Journal.SetSetting("language",_language);
            Notify();
            if(wasReady)Status=Localization["ReadyStatus"];
            foreach(var name in new[]{nameof(RouteStatus),nameof(SafetyStatus),nameof(SafetyDetail),nameof(ActionDescription),nameof(ObservationSummary),nameof(FactDetails),nameof(LiveCpu),nameof(LiveMemory),nameof(LiveNetwork),nameof(LiveUptime),nameof(UptimeText),nameof(TelemetryStatus)})Notify(name);
        }
    }
    public bool Expert=>Mode=="Expert";
    public string Host{get=>_host;set{_host=value;Notify();}}
    public bool Busy{get=>_busy;private set{_busy=value;Notify();CommandManager.InvalidateRequerySuggested();}}
    public string Status{get=>_status;set{_status=value;Notify();}}
    public string Detail{get=>_detail;set{_detail=value;Notify();Notify(nameof(HasDetail));}}
    public bool SidebarCollapsed{get=>_sidebarCollapsed;private set{if(_sidebarCollapsed==value)return;_sidebarCollapsed=value;Runtime.Journal.SetSetting("sidebar-collapsed",value.ToString());Notify();Notify(nameof(SidebarWidth));}}
    public GridLength SidebarWidth=>new(SidebarCollapsed?64:252);
    public string SurfaceTitle=>FeatureRouteCatalog.BindingFor(Title).Surface;
    public NativeRouteBinding RouteBinding=>FeatureRouteCatalog.BindingFor(Title);
    public string RouteStatus=>Localization[RouteBinding.Support switch{RouteSupport.Implemented=>"ImplementedStatus",RouteSupport.Limited=>"LimitedStatus",_=>"UnavailableStatus"}];
    public bool IsRouteLimited=>RouteBinding.Support!=RouteSupport.Implemented;
    public string RouteDetail=>RouteBinding.Detail;
    public string FeatureId=>Selected?.FeatureId??"";
    public IReadOnlyList<string> RouteFeatures=>Selected?.Features??[];
    public string Breadcrumb=>Selected is {} page?$"HYPERBOOSTX / {page.Group}":"HYPERBOOSTX / COMMAND CENTER";
    public string SafetyStatus=>Runtime.EnvironmentBlock() is null?Localization["SafetyGuardEnforced"]:Localization["ChangesBlocked"];
    public string SafetyDetail=>Runtime.EnvironmentBlock()??Localization["RegisteredActionsRequireReview"];
    public bool HasDetail=>!string.IsNullOrWhiteSpace(Detail);
    public bool HasFacts=>Facts.Count>0;
    public bool HasFactDetails=>SelectedFact is not null;
    public string FactDetails=>SelectedFact is {} fact
        ?$"{Localization["Observation"]}: {fact.Name}\n{Localization["Value"]}: {fact.Value}\n{Localization["Source"]}: {fact.Source}\n{Localization["Evidence"]}: {fact.Quality}\n{Localization["Availability"]}: {fact.Availability}"
        :"";
    public Fact? SelectedFact
    {
        get=>_selectedFact;
        set
        {
            if(_selectedFact==value)return;
            _selectedFact=value;
            Notify();
            Notify(nameof(HasFactDetails));
            Notify(nameof(FactDetails));
        }
    }
    public string FactFilter
    {
        get=>_factFilter;
        set
        {
            if(_factFilter==value)return;
            _factFilter=value??"";
            Notify();
            FactsView.Refresh();
            if(SelectedFact is {} selected&&!MatchesFact(selected))SelectedFact=null;
        }
    }
    public string Search{get=>_search;set{_search=value;Notify();Navigation.Clear();foreach(var n in AllNavigation.Where(n=>(n.Name+" "+n.Aliases+" "+n.Group).Contains(value,StringComparison.OrdinalIgnoreCase)))Navigation.Add(n);}}
    public Navigation? Selected{get=>_selected;set{if(value==null||value==_selected||Busy)return;_selected=value;Runtime.Journal.SetSetting("last-page",value.Name);Runtime.Journal.SetSetting("last-feature",value.FeatureId);Notify();LoadPage();}}
    public ActionInfo? SelectedAction{get=>_selectedAction;set{_selectedAction=value;Notify();Notify(nameof(ActionDescription));CommandManager.InvalidateRequerySuggested();}}
    public PowerPlan? SelectedPlan{get=>_selectedPlan;set{_selectedPlan=value;Notify();}}
    public StartupEntry? SelectedStartup{get=>_selectedStartup;set{_selectedStartup=value;Notify();}}
    public Change? SelectedChange{get=>_selectedChange;set{_selectedChange=value;Notify();if(value!=null)Detail=$"{value.ActionId}\nTarget: {value.Target}\nBefore: {value.Before}\nRequested: {value.Desired}\nObserved: {value.Observed}\n{value.Message}";CommandManager.InvalidateRequerySuggested();}}
    public Handoff? SelectedHandoff{get=>_selectedHandoff;set{_selectedHandoff=value;Notify();CommandManager.InvalidateRequerySuggested();}}
    public string Title=>Selected?.Name??"Dashboard";
    public string Subtitle=>Selected?.Description??"";
    public string ActionDescription=>SelectedAction==null?Localization["SelectChangeToReview"]:$"{SelectedAction.Description}\n{Localization["Scope"]}: {SelectedAction.Scope} · {Localization["Risk"]}: {SelectedAction.Risk} · {Localization["Restart"]}: {SelectedAction.Restart}";
    public bool IsOneClickBoost=>Title=="One Click Boost";
    private bool _boostTransparency,_boostCapture;
    public bool BoostTransparency{get=>_boostTransparency;set{_boostTransparency=value;Notify();CommandManager.InvalidateRequerySuggested();}}
    public bool BoostCapture{get=>_boostCapture;set{_boostCapture=value;Notify();CommandManager.InvalidateRequerySuggested();}}
    public bool IsDashboard=>SurfaceTitle=="Dashboard";
    public bool HasActions=>Actions.Count>0;
    public bool HasTools=>Tools.Count>0;
    public bool IsPower=>SurfaceTitle=="Power";
    public bool IsStartup=>SurfaceTitle=="Startup"&&Title!="Boot Optimizer";
    public bool IsNetwork=>SurfaceTitle=="Network"&&RouteBinding.Support!=RouteSupport.Unsupported;
    public bool IsRestore=>SurfaceTitle=="Restore";
    public bool IsSettings=>SurfaceTitle=="Settings";
    public bool IsReports=>SurfaceTitle=="Reports";
    public bool IsTestCenter=>SurfaceTitle is "Test Center" or "Application Health";
    public bool IsFileAnalysis=>Title=="Duplicate & Large Files";
    public bool IsAppLoadOptimizer=>Title=="App Load Optimizer";
    public bool IsLcdGuard=>Title=="LCD Performance Guard";
    public double LcdCpuThreshold{get=>_lcdCpuThreshold;set{_lcdCpuThreshold=Math.Clamp(value,0,100);Notify();}}
    public long LcdMemoryThresholdMiB{get=>_lcdMemoryThresholdMiB;set{_lcdMemoryThresholdMiB=Math.Clamp(value,1,1_048_576);Notify();}}
    public bool LcdGamingOnly{get=>_lcdGamingOnly;set{_lcdGamingOnly=value;Notify();}}
    public string FileAnalysisRoot{get=>_fileAnalysisRoot;set{_fileAnalysisRoot=value??"";Notify();CommandManager.InvalidateRequerySuggested();}}
    public long MinimumLargeFileMiB{get=>_minimumLargeFileMiB;set{_minimumLargeFileMiB=Math.Clamp(value,1,1_048_576);Notify();}}
    public string FileAnalysisStatus{get=>_fileAnalysisProgress;private set{_fileAnalysisProgress=value;Notify();}}
    public string AppLoadExecutable{get=>_appLoadExecutable;set{_appLoadExecutable=value??"";Notify();CommandManager.InvalidateRequerySuggested();}}
    public SafeTestRun? SelectedTestRun
    {
        get=>_selectedTestRun;
        set
        {
            if(_selectedTestRun==value)return;
            _selectedTestRun=value;TestResults.Clear();
            if(value is not null)foreach(var result in value.Results)TestResults.Add(result);
            SelectedTestResult=TestResults.FirstOrDefault();
            Notify();Notify(nameof(TestRunSummary));
        }
    }
    public SafeTestResult? SelectedTestResult
    {
        get=>_selectedTestResult;
        set{_selectedTestResult=value;Notify();Notify(nameof(TestResultDetails));}
    }
    public string TestRunSummary=>SelectedTestRun?.Summary??Localization["NoTestRuns"];
    public string TestResultDetails=>SelectedTestResult is {} result
        ?$"{result.TestId}\n{result.Name}\n{result.Result} · {result.StartedAt.ToLocalTime():G} · {result.DurationMilliseconds} ms\n{result.ErrorCode}\n{result.Evidence}"
        :"";
    public bool IsDiagnostic=>Diagnostics.Centers.Contains(SurfaceTitle);
    public bool HasFindings=>Findings.Count>0;
    public string ObservationSummary=>(IsReports?_report:_pageReport) is {} report?$"{report.Center} · {report.Summary}":Localization["NoScanYet"];
    public string DataLocation=>Runtime.Journal.Root;
    public ICommand ScanCommand{get;}
    public ICommand CancelCommand{get;}
    public ICommand ApplyCommand{get;}
    public ICommand SafeBoostCommand{get;}
    public ICommand RollbackCommand{get;}
    public ICommand VerifyCommand{get;}
    public ICommand ExportCommand{get;}
    public ICommand SupportCommand{get;}
    public ICommand PingCommand{get;}
    public ICommand ResolveDnsCommand{get;}
    public ICommand OpenToolCommand{get;}
    public ICommand BackupCommand{get;}
    public ICommand RunSafeTestsCommand{get;}
    public ICommand ExportSettingsCommand{get;}
    public ICommand ImportSettingsCommand{get;}
    public ICommand ResetSettingsCommand{get;}
    public ICommand SmartScanCommand{get;}
    public ICommand OptimizeCommand{get;}
    public ICommand RestoreCommand{get;}
    public ICommand ToggleSidebarCommand{get;}
    public ICommand NotificationsCommand{get;}
    public ICommand HelpCommand{get;}
    public ICommand AboutCommand{get;}
    public ICommand SelectFileAnalysisFolderCommand{get;}
    public ICommand RunFileAnalysisCommand{get;}
    public ICommand SelectAppLoadExecutableCommand{get;}
    public ICommand MeasureAppLoadCommand{get;}
    public ShellViewModel(Runtime runtime)
    {
        Runtime=runtime;_theme=runtime.Journal.GetSetting("theme","Dark");_goal=runtime.Journal.GetSetting("goal","Everyday");_mode=runtime.Journal.GetSetting("mode","Beginner");
        var savedLanguage=runtime.Journal.GetSetting("language","id-ID");
        var startupLanguage=LocalizationService.Languages.Any(language=>string.Equals(language.Code,savedLanguage,StringComparison.OrdinalIgnoreCase))?savedLanguage:"id-ID";
        Localization=new LocalizationService(startupLanguage);
        _language=Localization.Language;
        _status=Localization["ReadyStatus"];
        _sidebarCollapsed=bool.TryParse(runtime.Journal.GetSetting("sidebar-collapsed"),out var collapsed)&&collapsed;
        NavigationView=CollectionViewSource.GetDefaultView(Navigation);NavigationView.GroupDescriptions?.Add(new PropertyGroupDescription("Group"));
        FactsView=CollectionViewSource.GetDefaultView(Facts);FactsView.Filter=MatchesFact;
        Facts.CollectionChanged+=(_,_)=>Notify(nameof(HasFacts));
        ScanCommand=new Command(()=>Run(Scan),()=>!Busy&&IsDiagnostic&&RouteBinding.Support!=RouteSupport.Unsupported);
        CancelCommand=new Command(()=>_cts?.Cancel(),()=>Busy);
        ApplyCommand=new Command(()=>Run(Apply),()=>!Busy&&RouteBinding.Support!=RouteSupport.Unsupported&&SelectedAction!=null&&(!IsStartup||SelectedStartup!=null)&&(!IsPower||SelectedPlan!=null)&&(!IsCleanup||SelectedCleanup!=null));
        SafeBoostCommand=new Command(()=>Run(ApplySafeBoost),()=>!Busy&&IsOneClickBoost&&!HasGamingSession&&(BoostTransparency||BoostCapture));
        RollbackCommand=new Command(()=>Run(Rollback),()=>!Busy&&SelectedChange?.Recoverable==true);
        VerifyCommand=new Command(()=>Run(Verify),()=>!Busy&&SelectedChange!=null);
        ExportCommand=new Command(Export,()=>!Busy&&_report!=null);
        SupportCommand=new Command(Support,()=>!Busy);
        PingCommand=new Command(()=>Run(async t=>{var report=await Diagnostics.PingAsync(Host.Trim(),t);t.ThrowIfCancellationRequested();Runtime.Journal.SaveReport(report);Show(report);Status="Pengukuran selesai. Tidak ada pengaturan jaringan yang diubah.";}),()=>!Busy&&IsNetwork);
        ResolveDnsCommand=new Command(()=>Run(async t=>{var report=await Diagnostics.ResolveDnsAsync(Host.Trim(),t);t.ThrowIfCancellationRequested();Runtime.Journal.SaveReport(report);Show(report);Status=Localization["DnsResultRecorded"];}),()=>!Busy&&IsNetwork);
        OpenToolCommand=new Command(()=>{try{if(SelectedHandoff!=null){Handoffs.Open(SelectedHandoff);Status="Alat Windows dibuka. Hasil perubahan di alat eksternal belum diverifikasi HyperBoostX.";}}catch(Exception e){Status=e.Message;}},()=>!Busy&&RouteBinding.Support!=RouteSupport.Unsupported&&SelectedHandoff!=null);
        BackupCommand=new Command(()=>Run(async _=>{await Task.Run(Runtime.Journal.Backup);Status="Backup database lokal selesai.";}),()=>!Busy);
        ExportSettingsCommand=new Command(()=>Run(_=>ExportUserSettings()),()=>!Busy);
        ImportSettingsCommand=new Command(()=>Run(ImportUserSettings),()=>!Busy);
        ResetSettingsCommand=new Command(()=>Run(ResetUserSettings),()=>!Busy);
        RunSafeTestsCommand=new Command(()=>Run(async token=>
        {
            var run=await TestCenterRunner.RunAsync(Runtime,token,()=>Busy);
            RefreshTestHistory(run.Id);
            Status=run.Summary;
        }),()=>!Busy&&IsTestCenter);
        SmartScanCommand=new Command(()=>{Navigate("Smart Scan");Run(Scan);},()=>!Busy);
        OptimizeCommand=new Command(()=>Navigate("Optimize"),()=>!Busy);
        RestoreCommand=new Command(()=>Navigate("Restore"),()=>!Busy);
        ToggleSidebarCommand=new Command(()=>SidebarCollapsed=!SidebarCollapsed,()=>!Busy);
        NotificationsCommand=new Command(()=>Navigate("Notifications"),()=>!Busy);
        HelpCommand=new Command(()=>Navigate("Knowledge Base"),()=>!Busy);
        AboutCommand=new Command(()=>Navigate("License Center"),()=>!Busy);
        SelectFileAnalysisFolderCommand=new Command(SelectFileAnalysisFolder,()=>!Busy&&IsFileAnalysis);
        RunFileAnalysisCommand=new Command(()=>Run(RunFileAnalysis),()=>!Busy&&IsFileAnalysis&&Directory.Exists(FileAnalysisRoot));
        SelectAppLoadExecutableCommand=new Command(SelectAppLoadExecutable,()=>!Busy&&IsAppLoadOptimizer);
        MeasureAppLoadCommand=new Command(ConfirmAndMeasureAppLoad,()=>!Busy&&IsAppLoadOptimizer&&File.Exists(AppLoadExecutable));
        InitializeLegacyFeatures();
        InitializeIntegrations();
        _selected=AllNavigation.FirstOrDefault(page=>page.FeatureId==runtime.Journal.GetSetting("last-feature"))??
                  AllNavigation.FirstOrDefault(page=>page.Name==runtime.Journal.GetSetting("last-page"))??AllNavigation[0];LoadPage();
        if(runtime.Journal.RecoveryNotice!="")Status=runtime.Journal.RecoveryNotice;
    }
    public async void Initialize()
    {
        if(Runtime.Journal.GetSetting("onboarding")!="complete")
        {
            MessageBox.Show("Selamat datang di HyperBoostX v3.\n\n• Scan membaca kondisi PC tanpa optimasi otomatis.\n• Setiap perubahan memerlukan preview dan persetujuan.\n• Backup dan rollback disimpan untuk pengguna Windows ini.\n• Data tersimpan lokal; cloud, AI, analytics, dan upload tidak aktif.\n\nBuild ini belum memenuhi seluruh gate Stable dalam spesifikasi. Fitur yang belum selesai tidak ditampilkan sebagai fitur siap pakai.","Mulai menggunakan HyperBoostX",MessageBoxButton.OK,MessageBoxImage.Information);
            Runtime.Journal.SetSetting("onboarding","complete");
        }
        await Execute(Scan);
        var incomplete=Runtime.Journal.History().Count(c=>c.NeedsRecovery);
        if(incomplete>0)Status=$"{incomplete} operasi belum selesai ditemukan. Buka Restore untuk memeriksa atau memulihkan.";
        if(Runtime.GamingSessions.Active)Status="Sesi gaming sebelumnya belum dipulihkan. Buka Game Library > Restore session.";
    }
    public void Navigate(string name)
    {
        var target=name switch
        {
            "Optimize"=>"One Click Boost","Gaming"=>"Game Boost Engine","Processes"=>"Process & Task Manager",
            "Hardware"=>"Hardware Inventory","GPU"=>"GPU Center","Storage"=>"Storage Center","Cleanup"=>"Cleanup Center",
            "Network"=>"Network Center","Startup"=>"Startup Manager","Apps"=>"Debloat Center","Devices"=>"Device Manager",
            "Security"=>"Security Health","Windows"=>"Windows Optimization","Power"=>"Power Optimization","Creator"=>"Streaming Center",
            "Reliability"=>"Application Health","Repair"=>"Repair & Troubleshooting","Restore"=>"Restore & Rollback",
            "Reports"=>"Reports Center","Notifications"=>"Activity & Notifications","AI Center"=>"AI Assistant",
            "Benchmark Lab"=>"Benchmark Center",_=>name
        };
        Selected=AllNavigation.First(n=>n.Name==target);
    }
    private void LoadPage()
    {
        FactFilter="";SelectedFact=null;_pageReport=null;Facts.Clear();Findings.Clear();Actions.Clear();Tools.Clear();Detail="";
        var surface=SurfaceTitle;
        if(Title!="Boot Optimizer")foreach(var action in Runtime.Engine.Actions.Where(a=>a.Center==surface))Actions.Add(action);SelectedAction=Actions.FirstOrDefault();
        if(RouteBinding.Support!=RouteSupport.Unsupported)
            foreach(var tool in Handoffs.For(surface))Tools.Add(tool);
        SelectedHandoff=Tools.FirstOrDefault();
        if(IsPower){Plans.Clear();try{foreach(var p in PowerAction.Plans())Plans.Add(p);SelectedPlan=Plans.FirstOrDefault(p=>p.Id==PowerAction.Current());}catch(Exception e){Status=e.Message;}}
        if(IsStartup){StartupItems.Clear();try{foreach(var s in Diagnostics.StartupEntries().Where(s=>s.CanDisable&&!s.Protected))StartupItems.Add(s);SelectedStartup=StartupItems.FirstOrDefault();}catch(Exception e){Status=e.Message;}}
        if(IsRestore)RefreshHistory();
        if(IsReports){Reports.Clear();foreach(var report in Runtime.Journal.Reports())Reports.Add(report);if(Reports.FirstOrDefault() is {} latest)SelectedReport=latest;else{_report=null;Notify(nameof(SelectedReport));}}
        if(IsTestCenter)RefreshTestHistory();
        if(surface=="Knowledge Base")LoadKnowledge();
        if(surface=="Repair")
        {
            Facts.Add(new("SFC / DISM","Gunakan pemulihan Windows untuk pemeriksaan dan perbaikan sistem. Build ini belum mengeksekusi repair elevated otomatis.","Batas implementasi","Guidance"));
            Facts.Add(new("Sebelum repair","Simpan pekerjaan, periksa backup dan kunci pemulihan BitLocker, gunakan daya AC pada laptop. Jangan hentikan servis keamanan.","Panduan pemulihan","Guidance"));
            Facts.Add(new("Pemulihan HyperBoostX","Perubahan yang dibuat HyperBoostX tersedia pada Restore, terpisah dari Windows System Restore.","Action journal","Guidance"));
        }
        if(surface is "Knowledge Base" or "Repair")Show(new DiagnosticReport(surface,DateTimeOffset.UtcNow,Facts.ToArray(),[]));
        LoadLegacyPage();
        Notify(nameof(ObservationSummary));
        foreach(var name in new[]{nameof(Title),nameof(Subtitle),nameof(Breadcrumb),nameof(SurfaceTitle),nameof(RouteBinding),nameof(RouteStatus),nameof(RouteDetail),nameof(FeatureId),nameof(RouteFeatures),nameof(IsRouteLimited),nameof(IsOneClickBoost),nameof(IsDashboard),nameof(HasActions),nameof(HasTools),nameof(IsPower),nameof(IsStartup),nameof(IsNetwork),nameof(IsRestore),nameof(IsSettings),nameof(IsReports),nameof(IsTestCenter),nameof(IsFileAnalysis),nameof(IsAppLoadOptimizer),nameof(IsLcdGuard),nameof(IsDiagnostic),nameof(HasFindings)})Notify(name);
        CommandManager.InvalidateRequerySuggested();
    }
    private void SelectFileAnalysisFolder()
    {
        var dialog=new OpenFolderDialog{Title="Pilih folder lokal untuk pencarian duplikat dan file besar",Multiselect=false};
        if(dialog.ShowDialog()==true)FileAnalysisRoot=dialog.FolderName;
    }
    public async Task RunFileAnalysis(CancellationToken token)
    {
        DuplicateGroups.Clear();LargeFiles.Clear();FileAnalysisIssues.Clear();
        var dispatcher=System.Windows.Threading.Dispatcher.CurrentDispatcher;
        // Progress<T> may deliver queued callbacks after ScanAsync completes. Serialize
        // both progress and the terminal state on the UI dispatcher; close the reporter
        // before publishing the final result, including on cancellation or failure.
        var acceptingProgress=true;
        var progress=new Progress<FileAnalysisProgress>(value=>
        {
            _=dispatcher.InvokeAsync(()=>
            {
                if(!acceptingProgress)return;
                FileAnalysisStatus=$"{value.FilesExamined:N0} file diperiksa · {FileAnalysisFormatting.Size(value.BytesHashed)} di-hash"+(string.IsNullOrWhiteSpace(value.CurrentPath)?"":$" · {value.CurrentPath}");
            });
        });
        try
        {
            var threshold=checked(MinimumLargeFileMiB*1024L*1024L);
            var result=await FileAnalysis.ScanAsync(FileAnalysisRoot,threshold,progress,token);
            await dispatcher.InvokeAsync(()=>
            {
                acceptingProgress=false;
                foreach(var group in result.Duplicates)DuplicateGroups.Add(group);
                foreach(var file in result.LargeFiles)LargeFiles.Add(file);
                foreach(var issue in result.Issues)FileAnalysisIssues.Add(issue);
                FileAnalysisStatus=$"Selesai: {result.FilesExamined:N0} file, {result.Duplicates.Count:N0} grup duplikat, {result.LargeFiles.Count:N0} file besar, potensi duplikat {FileAnalysisFormatting.Size(result.Duplicates.Sum(group=>group.PotentialDuplicateBytes))}. "+
                    (result.CoverageComplete?"Cakupan lengkap.":$"Cakupan tidak lengkap: skipped={result.Skipped}, truncated={result.Truncated}, cancelled={result.Cancelled}.");
                Status="Analisis file selesai. Tidak ada file yang diubah atau dihapus.";
            });
        }
        finally
        {
            await dispatcher.InvokeAsync(()=>{acceptingProgress=false;});
        }
    }
    private void SelectAppLoadExecutable()
    {
        var dialog=new OpenFileDialog{Title="Pilih aplikasi yang akan diluncurkan dan diukur",Filter="Windows executable (*.exe)|*.exe",CheckFileExists=true,Multiselect=false};
        if(dialog.ShowDialog()==true)AppLoadExecutable=dialog.FileName;
    }
    private void ConfirmAndMeasureAppLoad()
    {
        string executable;
        try{executable=ApplicationLaunchMeasurement.ValidateExecutable(AppLoadExecutable);}
        catch(Exception exception) when(exception is HbxException or IOException or UnauthorizedAccessException or ArgumentException){Status=exception.Message;return;}
        if(!Confirm($"HyperBoostX akan meluncurkan aplikasi berikut tanpa argumen:\n\n{executable}\n\nAplikasi tidak akan ditutup atau diubah oleh HyperBoostX. Simpan pekerjaan bila aplikasi tersebut mungkin membuka instance baru.\n\nLanjutkan pengukuran?","Konfirmasi peluncuran aplikasi"))return;
        Run(token=>RunAppLoadMeasurement(executable,token));
    }
    public async Task RunAppLoadMeasurement(string executable,CancellationToken token)
    {
        Status="Meluncurkan aplikasi dan mengukur kesiapan awal…";
        var result=await ApplicationLaunchMeasurement.MeasureAsync(executable,token);
        var report=ApplicationLaunchMeasurement.ToReport(result);
        Runtime.Journal.SaveReport(report);Show(report);
        Status=result.ExitedDuringMeasurement
            ?$"Pengukuran selesai: {Path.GetFileName(executable)} keluar selama pengambilan sampel."
            :$"Pengukuran selesai. {Path.GetFileName(executable)} tetap berjalan dan tidak diubah HyperBoostX.";
    }
    private bool MatchesFact(object item)
    {
        if(item is not Fact fact)return false;
        var filter=_factFilter.Trim();
        return filter.Length==0||
               fact.Name.Contains(filter,StringComparison.OrdinalIgnoreCase)||
               fact.Value.Contains(filter,StringComparison.OrdinalIgnoreCase)||
               fact.Source.Contains(filter,StringComparison.OrdinalIgnoreCase)||
               fact.Quality.Contains(filter,StringComparison.OrdinalIgnoreCase)||
               fact.Availability.Contains(filter,StringComparison.OrdinalIgnoreCase);
    }
    private void LoadKnowledge()
    {
        string[][] articles=[
            ["Game Mode","Windows mengatur prioritas sumber daya game. Evaluasi dengan beban game yang sama; bukan jaminan FPS."],
            ["Game capture","Mematikan preferensi capture mengurangi fitur rekam Windows. Aplikasi OBS/Discord mempunyai pengaturan terpisah."],
            ["HAGS","Ketersediaan bergantung pada GPU dan driver. Gunakan Graphics Settings; bandingkan beberapa sesi sebelum menyimpulkan manfaat."],
            ["DLSS / FSR / XeSS","Upscaling tersedia melalui implementasi game dan dukungan GPU. HyperBoostX tidak dapat mengaktifkannya secara universal."],
            ["Frame Generation / AFMF","Frame tambahan tidak sama dengan peningkatan respons input. Dukungan bergantung pada game, GPU, dan driver."],
            ["Reflex / HYPR-RX","Gunakan kontrol yang tersedia di game atau aplikasi vendor. HyperBoostX tidak melakukan patch driver."],
            ["VRR / FreeSync / G-SYNC / V-Sync","Sinkronisasi bergantung pada monitor, koneksi dan driver. Tinjau pengaturan display dan game; bukan optimasi universal."],
            ["Resizable BAR","Dukungan memerlukan kombinasi perangkat, firmware, dan driver. HyperBoostX tidak mengubah BIOS."],
            ["Power plans","Plan performance dapat meningkatkan panas dan konsumsi baterai. Pilih plan yang benar-benar tersedia dan simpan kondisi sebelumnya."],
            ["RAM usage vs pressure","RAM terpakai tinggi tidak selalu bermasalah. Cache membantu kinerja. Jangan mengosongkan standby atau menutup proses sembarangan."],
            ["Startup","Disable hanya entri yang dipahami. Keamanan, driver dan utilitas kontrol perangkat dilindungi. Gunakan Restore untuk mengembalikan entri yang dikelola aplikasi."],
            ["Shader cache","Pembersihan cache dapat menambah stutter saat kompilasi ulang. Gunakan sebagai troubleshooting yang beralasan."],
            ["Rollback / drift","Rollback membandingkan kondisi saat ini dengan kondisi yang pernah diterapkan. Konflik berarti state eksternal dipertahankan."],
            ["Unknown is valid","Unavailable berarti provider tidak menyediakan nilai. Itu tidak berarti suhu 0°C atau kesehatan 100%."],
            ["Measured improvement","Bandingkan beban, driver, resolusi, dan versi metode yang sama. Perbedaan satu tes tidak membuktikan sebab akibat."]];
        foreach(var a in articles)Facts.Add(new(a[0],a[1],"Local KB v1 · reviewed 2026-09-29 · Windows 10/11 · verify availability in Windows/vendor UI","Guidance"));
    }
    private void RefreshTestHistory(string? selectedId=null)
    {
        try
        {
            var runs=Runtime.Documents.Read("test-history",new List<SafeTestRun>());
            TestRuns.Clear();foreach(var run in runs.OrderByDescending(run=>run.StartedAt))TestRuns.Add(run);
            SelectedTestRun=TestRuns.FirstOrDefault(run=>run.Id==selectedId)??TestRuns.FirstOrDefault();
        }
        catch(Exception exception) when(exception is IOException or UnauthorizedAccessException or System.Security.Cryptography.CryptographicException or System.Text.Json.JsonException or InvalidDataException or HbxException)
        {
            TestRuns.Clear();TestResults.Clear();SelectedTestRun=null;
            Status=$"Test history could not be read: {ReportExport.Redact(exception.Message)}";
        }
    }
    private async Task Scan(CancellationToken token)
    {
        var center=SurfaceTitle;Status=$"Membaca {Title}…";
        var report=Title switch{"RGB Software Detector"=>await VendorSoftwareDetection.ScanRgbAsync(token),"Overlay Conflict Detector"=>await OverlaySoftwareDetection.ScanAsync(token),"Boot Optimizer"=>await BootDiagnostics.ScanAsync(token),"Drive Optimization"=>await DriveDiagnostics.ScanAsync(token),"LCD Performance Guard"=>await LcdPerformanceDetection.ScanAsync(LcdCpuThreshold,LcdMemoryThresholdMiB,LcdGamingOnly,Runtime.GamingSessions.Active,token),"App Load Optimizer"=>await InstalledApplicationDetection.ScanAsync(token),_=>await Runtime.Diagnostics.ScanAsync(center,token)};token.ThrowIfCancellationRequested();Runtime.Journal.SaveReport(report);
        if(SurfaceTitle==center){Show(report);RefreshTargets();}Status=$"{Title}: {report.Facts.Count} observasi terbaca. Tidak ada konfigurasi Windows yang diubah.";
    }
    private void Show(DiagnosticReport report)
    {
        _report=report;_pageReport=report;SelectedFact=null;Facts.Clear();foreach(var f in report.Facts)Facts.Add(f);Findings.Clear();foreach(var f in report.Findings.OrderBy(f=>Goal=="Gaming"&&f.ActionId?.StartsWith("gaming.",StringComparison.Ordinal)==true?0:1))Findings.Add(f);
        Notify(nameof(HasFindings));Notify(nameof(ObservationSummary));CommandManager.InvalidateRequerySuggested();
    }
    private async Task ApplySafeBoost(CancellationToken token)
    {
        if(HasGamingSession)throw new HbxException("Restore the active gaming session first.");
        var ids=ActionEngine.SafeBoostActionIds(BoostTransparency,BoostCapture);
        var previews=new List<Preview>();
        foreach(var id in ids)previews.Add(await Task.Run(()=>Runtime.Engine.PreviewAsync(id,"",token),token));
        if(previews.Count==0)return;
        var review=string.Join("\n\n",previews.Select(p=>$"{p.Description}\nBefore: {p.Before}\nAfter: {p.Desired}"));
        if(!Confirm("Perubahan preferensi Windows yang dipilih:\n\n"+review+"\n\nEach action is backed up and read back. On failure the batch attempts conflict-aware recovery. No FPS gain is promised. Apply?","Safe Boost preview"))return;
        var result=await Task.Run(()=>Runtime.Engine.ApplySafeBoostAsync(previews,true,token),token);
        Status=$"{result.Status}: {result.Message}";Detail=Json.Write(result);RefreshHistory();
        Facts.Clear();Findings.Clear();_pageReport=null;Notify(nameof(HasFindings));Notify(nameof(ObservationSummary));
    }
    private async Task Apply(CancellationToken token)
    {
        if(SelectedAction==null)return;var a=SelectedAction;var target=a.Id switch{"windows.power.set_plan"=>SelectedPlan?.Id??"","startup.current_user.disable"=>SelectedStartup?.Name??"","storage.temp.quarantine"=>SelectedCleanup?.Path??"",_=>""};
        var preview=await Task.Run(()=>Runtime.Engine.PreviewAsync(a.Id,target,token),token);
        if(!Confirm($"{a.Name}\n\n{a.Description}\n\nScope: {a.Scope}\nRisk: {a.Risk}\nTarget: {(target==""?"Current user's Windows preference":target)}\nBefore: {preview.Before}\nAfter: {preview.Desired}\nRestart: {a.Restart}\n\nBackup disimpan sebelum perubahan. Tidak ada klaim kenaikan FPS.\n\nTerapkan perubahan ini?","Preview perubahan"))return;
        var result=await Task.Run(()=>Runtime.Engine.ApplyAsync(preview.Token,true,null,token),token);Status=$"{result.Status}: {result.Message}";Detail=Json.Write(result);RefreshHistory();RefreshTargets();
        Facts.Clear();Findings.Clear();_pageReport=null;Notify(nameof(HasFindings));Notify(nameof(ObservationSummary));
    }
    private async Task Rollback(CancellationToken token)
    {
        if(SelectedChange is not {} c)return;
        if(!Confirm($"Pulihkan {c.ActionId}?\n\nTarget: {c.Target}\nDiterapkan: {c.Desired}\nKembali ke: {c.Before}\n\nJika ada perubahan di luar aplikasi, pemulihan diblokir.","Konfirmasi restore"))return;
        var result=await Task.Run(()=>Runtime.Engine.RollbackAsync(c.Id,true,token),token);Status=$"{result.Status}: {result.Message}";RefreshHistory();
    }
    private async Task Verify(CancellationToken token)
    {
        if(SelectedChange is not {} c)return;var state=await Task.Run(()=>Runtime.Engine.VerifyAsync(c.Id,token),token);
        Detail=$"Action: {c.ActionId}\nOriginal: {c.Before}\nApplied: {c.Desired}\nCurrent: {state}\n\n"+(state==c.Desired?"Current state matches the applied value.":state==c.Before?"Current state matches the original value.":"External change detected. State preserved.");Status="Kondisi terkini dibaca. Tidak ada perubahan.";
    }
    private void RefreshHistory(){var id=SelectedChange?.Id;History.Clear();foreach(var c in Runtime.Journal.History(10000))History.Add(c);SelectedChange=History.FirstOrDefault(c=>c.Id==id)??History.FirstOrDefault();}
    private void RefreshTargets()
    {
        if(IsCleanup){foreach(var item in CleanupItems.Where(i=>!File.Exists(i.Path)).ToArray())CleanupItems.Remove(item);SelectedCleanup=CleanupItems.FirstOrDefault();}
        if(IsStartup){var name=SelectedStartup?.Name;StartupItems.Clear();foreach(var s in Diagnostics.StartupEntries().Where(s=>s.CanDisable&&!s.Protected))StartupItems.Add(s);SelectedStartup=StartupItems.FirstOrDefault(s=>s.Name==name)??StartupItems.FirstOrDefault();}
        if(IsPower){Plans.Clear();foreach(var p in PowerAction.Plans())Plans.Add(p);SelectedPlan=Plans.FirstOrDefault(p=>p.Id==PowerAction.Current());}
        CommandManager.InvalidateRequerySuggested();
    }
    private void Export()
    {
        if(_report==null)return;var dialog=new SaveFileDialog{Title="Ekspor laporan lokal",FileName=$"HyperBoostX-{_report.Center.Replace(' ','-')}-{DateTime.Now:yyyyMMdd-HHmmss}",Filter="JSON|*.json|Markdown|*.md|CSV|*.csv|HTML|*.html",AddExtension=true};
        if(dialog.ShowDialog()!=true)return;
        try{var text=dialog.FilterIndex switch{2=>ReportExport.Markdown(_report),3=>ReportExport.Csv(_report),4=>ReportExport.Html(_report),_=>Json.Write(ReportExport.Sanitize(_report))};File.WriteAllText(dialog.FileName,text,new UTF8Encoding(false));Status="Laporan diekspor: "+dialog.FileName;}
        catch(Exception e){Status="Ekspor gagal: "+e.Message;}
    }
    public static UserSettingsDocument ValidateSettingsDocument(UserSettingsDocument settings)
    {
        if(settings.SchemaVersion!=1)throw new HbxException("HBX-CONFIG-0001: Unsupported settings schema version.");
        if(settings.Theme is not ("Dark" or "Light")||
           settings.Goal is not ("Everyday" or "Gaming" or "Streaming" or "Music Production" or "Video Editing" or "Battery Life" or "Troubleshooting")||
           settings.Mode is not ("Beginner" or "Advanced" or "Expert")||
           !LocalizationService.Languages.Any(language=>string.Equals(language.Code,settings.Language,StringComparison.OrdinalIgnoreCase)))
            throw new HbxException("HBX-CONFIG-0002: Settings document contains an unsupported value.");
        return settings;
    }
    public static string SerializeSettingsDocument(UserSettingsDocument settings)=>Json.Write(ValidateSettingsDocument(settings));
    public static UserSettingsDocument DeserializeSettingsDocument(string json)=>ValidateSettingsDocument(Json.Read<UserSettingsDocument>(json));
    private UserSettingsDocument CurrentSettings()=>new(1,Theme,Goal,Mode,Language,SidebarCollapsed);
    private Task ExportUserSettings()
    {
        var dialog=new SaveFileDialog{Title=Localization["ExportSettings"],FileName="HyperBoostX-settings-v1.json",Filter="HyperBoostX settings|*.json",AddExtension=true};
        if(dialog.ShowDialog()!=true)return Task.CompletedTask;
        File.WriteAllText(dialog.FileName,SerializeSettingsDocument(CurrentSettings()),new UTF8Encoding(false));
        Status=Localization["SettingsExportedStatus"];
        return Task.CompletedTask;
    }
    private Task ImportUserSettings(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var dialog=new OpenFileDialog{Title=Localization["ImportSettings"],Filter="HyperBoostX settings|*.json",CheckFileExists=true};
        if(dialog.ShowDialog()!=true)return Task.CompletedTask;
        if(new FileInfo(dialog.FileName).Length>1024*1024)throw new HbxException("HBX-CONFIG-0003: Settings file exceeds 1 MiB.");
        var imported=DeserializeSettingsDocument(File.ReadAllText(dialog.FileName));
        var current=CurrentSettings();
        var diff=$"{Localization["Appearance"]}: {current.Theme} → {imported.Theme}\n{Localization["YourGoal"]}: {current.Goal} → {imported.Goal}\n{Localization["DetailLevelSafety"]}: {current.Mode} → {imported.Mode}\n{Localization["Language"]}: {current.Language} → {imported.Language}\n{Localization["SidebarPreference"]}: {current.SidebarCollapsed} → {imported.SidebarCollapsed}";
        if(!Confirm($"{Localization["SettingsImportConfirmation"]}\n\n{diff}\n\n{Localization["SettingsDataScope"]}",Localization["ImportSettings"]))return Task.CompletedTask;
        ApplySettingsDocument(imported);
        Status=Localization["SettingsImportedStatus"];
        return Task.CompletedTask;
    }
    private Task ResetUserSettings(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if(!Confirm(Localization["SettingsResetConfirmation"],Localization["ResetPreferences"]))return Task.CompletedTask;
        ApplySettingsDocument(new UserSettingsDocument(1,"Dark","Everyday","Beginner","id-ID",false),true);
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(()=>Navigate("Dashboard")));
        Status=Localization["SettingsResetStatus"];
        return Task.CompletedTask;
    }
    private void ApplySettingsDocument(UserSettingsDocument settings,bool resetNavigation=false)
    {
        var values=new Dictionary<string,string>
        {
            ["theme"]=settings.Theme,["goal"]=settings.Goal,["mode"]=settings.Mode,
            ["language"]=settings.Language,["sidebar-collapsed"]=settings.SidebarCollapsed.ToString()
        };
        if(resetNavigation)
        {
            values["last-page"]="Dashboard";
            values["last-feature"]=AllNavigation[0].FeatureId;
        }
        Runtime.Journal.SetSettings(values);
        var wasReady=string.Equals(_status,Localization["ReadyStatus"],StringComparison.Ordinal);
        _theme=settings.Theme;App.SetTheme(_theme);Notify(nameof(Theme));
        _goal=settings.Goal;Notify(nameof(Goal));
        _mode=settings.Mode;Notify(nameof(Mode));Notify(nameof(Expert));
        Localization.SetLanguage(settings.Language);_language=Localization.Language;Notify(nameof(Language));
        if(wasReady)Status=Localization["ReadyStatus"];
        _sidebarCollapsed=settings.SidebarCollapsed;Notify(nameof(SidebarCollapsed));Notify(nameof(SidebarWidth));
        CommandManager.InvalidateRequerySuggested();
    }
    private void Support()
    {
        if(!Confirm("Bundle berisi versi aplikasi/Windows dan ringkasan status 50 perubahan terakhir (ID, status, waktu). Tidak berisi command startup, backup mentah, token, report scan, atau file pribadi.\n\nSimpan ZIP lokal?","Preview support bundle"))return;
        var dialog=new SaveFileDialog{Filter="ZIP|*.zip",FileName=$"HyperBoostX-support-{DateTime.Now:yyyyMMdd-HHmmss}.zip"};if(dialog.ShowDialog()!=true)return;
        try
        {
            using var stream=new FileStream(dialog.FileName,FileMode.Create);using var zip=new ZipArchive(stream,ZipArchiveMode.Create);
            using var writer=new StreamWriter(zip.CreateEntry("support.json").Open());
            writer.Write(Json.Write(new{App="HyperBoostX 3.0.0",Release="Development — Stable gates pending",OS=Environment.OSVersion.VersionString,Changes=Runtime.Journal.History(50).Select(c=>new{c.Id,c.ActionId,c.Status,c.StartedAt,c.CompletedAt})}));
            Status="Support bundle disimpan lokal.";
        }catch(Exception e){Status="Support bundle gagal: "+e.Message;}
    }
    private static bool Confirm(string text,string title)=>MessageBox.Show(text,title,MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)==MessageBoxResult.Yes;
    private async void Run(Func<CancellationToken,Task> task)=>await Execute(task);
    private async Task Execute(Func<CancellationToken,Task> task)
    {
        if(Busy)return;Busy=true;using var cts=new CancellationTokenSource(TimeSpan.FromMinutes(3));_cts=cts;
        try{await task(cts.Token);}catch(OperationCanceledException){Status="Operasi dibatalkan pada batas aman. Periksa Restore jika perubahan sempat dimulai.";}catch(Exception e){Status=ReportExport.Redact(e.Message);Detail=Expert?ReportExport.Redact(e.ToString()):"Periksa target dan izin Windows, lalu ulangi scan. Tidak ada keberhasilan yang diasumsikan.";await RecordOperationFailure(e);}
        finally{_cts=null;Busy=false;}
    }
    public void Stop()=>_cts?.Cancel();
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName]string? name=null)=>PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(name));
}

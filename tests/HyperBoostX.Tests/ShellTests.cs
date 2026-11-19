using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows.Threading;
using HyperBoostX.App;
using HyperBoostX.Core;
using HyperBoostX.Windows;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class ShellTests
{
    [Fact]public void BootOptimizerExposesTheReadOnlyStartupDiagnosticInsteadOfAnUnavailableRoute()=>Run(vm=>
    {
        vm.Navigate("Boot Optimizer");
        Assert.Equal(RouteSupport.Implemented,vm.RouteBinding.Support);Assert.Equal("Startup",vm.SurfaceTitle);
        Assert.False(vm.IsStartup);Assert.True(vm.ScanCommand.CanExecute(null));Assert.False(vm.ApplyCommand.CanExecute(null));Assert.Empty(vm.Actions);
    });
    [Fact]public void DriveOptimizationExposesReadOnlyDiagnosticsAndWindowsOptimizeDrivesHandoff()=>Run(vm=>
    {
        vm.Navigate("Drive Optimization");Assert.Equal(RouteSupport.Implemented,vm.RouteBinding.Support);
        Assert.Equal("Storage",vm.SurfaceTitle);Assert.True(vm.ScanCommand.CanExecute(null));
        Assert.Contains(vm.Tools,tool=>tool.Name=="Optimize Drives");
    });
    [Fact]public void LcdPerformanceGuardIsImplementedWithConfigurableSafeThresholds()=>Run(vm=>
    {
        vm.Navigate("LCD Performance Guard");Assert.Equal(RouteSupport.Implemented,vm.RouteBinding.Support);Assert.True(vm.IsLcdGuard);Assert.True(vm.ScanCommand.CanExecute(null));
        vm.LcdCpuThreshold=150;vm.LcdMemoryThresholdMiB=0;Assert.Equal(100,vm.LcdCpuThreshold);Assert.Equal(1,vm.LcdMemoryThresholdMiB);
    });
    [Fact]public void StutterAnalyzerIsImplementedWithARealFrametimeImportCommand()=>Run(vm=>
    {
        vm.Navigate("Stutter Analyzer");Assert.Equal(RouteSupport.Implemented,vm.RouteBinding.Support);Assert.True(vm.IsBenchmark);Assert.True(vm.IsStutterAnalyzer);Assert.True(vm.ImportFrametimeCommand.CanExecute(null));
    });
    [Fact]public void ResearchModeIsImplementedWithPersistedTrialCommands()=>Run(vm=>
    {
        vm.Navigate("Research Mode");Assert.Equal(RouteSupport.Implemented,vm.RouteBinding.Support);Assert.True(vm.IsResearchMode);Assert.True(vm.AddResearchTrialCommand.CanExecute(null));Assert.False(vm.AnalyzeResearchCommand.CanExecute(null));
        vm.ResearchWorkload="Fixture";vm.ResearchValue="100";vm.AddResearchTrialCommand.Execute(null);
    });
    [Fact]public void AppLoadOptimizerRouteIsImplementedAndRequiresASelectedExecutable()=>Run(vm=>
    {
        vm.Navigate("App Load Optimizer");
        Assert.Equal(RouteSupport.Implemented,vm.RouteBinding.Support);Assert.True(vm.IsAppLoadOptimizer);
        Assert.False(vm.MeasureAppLoadCommand.CanExecute(null));
        vm.AppLoadExecutable=Path.Combine(Environment.SystemDirectory,"where.exe");
        Assert.True(vm.MeasureAppLoadCommand.CanExecute(null));
    });
    [Fact]public void RgbDetectorRouteIsImplementedAndScannable()=>Run(vm=>
    {
        vm.Navigate("RGB Software Detector");
        Assert.Equal(RouteSupport.Implemented,vm.RouteBinding.Support);Assert.True(vm.IsDiagnostic);Assert.True(vm.ScanCommand.CanExecute(null));
    });
    [Fact]public void OverlayDetectorRouteIsImplementedAndScannable()=>Run(vm=>
    {
        vm.Navigate("Overlay Conflict Detector");
        Assert.Equal(RouteSupport.Implemented,vm.RouteBinding.Support);Assert.True(vm.IsDiagnostic);Assert.True(vm.ScanCommand.CanExecute(null));
    });
    private static void AssertWmiProviderObserved(DiagnosticReport report,string provider)
    {
        var facts=report.Facts.Where(f=>f.Source.Contains(provider,StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(facts);
        Assert.True(facts.Any(f=>f.Availability=="Available")||
                    facts.Any(f=>f.Name=="Provider unavailable"&&f.Availability=="Unavailable"),
            $"Expected {provider} data or an explicit unavailable result.");
    }

    private static void Run(Action<ShellViewModel> test)
    {
        Exception? error=null;
        var root=Path.Combine(Path.GetTempPath(),"HyperBoostX-tests",Guid.NewGuid().ToString("N"));
        var thread=new Thread(()=>
        {
            try{test(new ShellViewModel(new Runtime(root)));}
            catch(Exception e){error=e;}
        });
        thread.SetApartmentState(ApartmentState.STA);thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)),"WPF component test timed out.");
        var parent=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"HyperBoostX-tests"))+Path.DirectorySeparatorChar;
        if(Path.GetFullPath(root).StartsWith(parent,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(root))Directory.Delete(root,true);
        if(error!=null)ExceptionDispatchInfo.Capture(error).Throw();
    }
    [Fact]public void EveryNavigationPageHasItsExpectedCommands()=>Run(vm=>
    {
        Assert.Equal(73,ShellViewModel.AllNavigation.Count);
        Assert.Equal(73,ShellViewModel.AllNavigation.Select(page=>page.FeatureId).Distinct().Count());
        Assert.Equal(73,ShellViewModel.AllNavigation.Select(page=>page.Name).Distinct().Count());
        var groupCounts=ShellViewModel.AllNavigation.GroupBy(page=>page.Group)
            .ToDictionary(group=>group.Key,group=>group.Count(),StringComparer.Ordinal);
        Assert.Equal(14,groupCounts.Count);
        Assert.Equal(5,groupCounts["COMMAND CENTER"]);
        Assert.Equal(8,groupCounts["OPTIMIZATION"]);
        Assert.Equal(7,groupCounts["GAMING"]);
        Assert.Equal(10,groupCounts["WINDOWS"]);
        var executableActionIds=vm.Runtime.Engine.Actions.Select(action=>action.Id).ToHashSet(StringComparer.Ordinal);
        var navigableActionIds=ShellViewModel.AllNavigation
            .SelectMany(page=>vm.Runtime.Engine.Actions.Where(action=>action.Center==FeatureRouteCatalog.BindingFor(page.Name).Surface))
            .Select(action=>action.Id).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(executableActionIds,navigableActionIds);
        foreach(var page in ShellViewModel.AllNavigation)
        {
            vm.Navigate(page.Name);Assert.Equal(page.Name,vm.Title);
            Assert.Equal(Diagnostics.Centers.Contains(vm.SurfaceTitle)&&vm.RouteBinding.Support!=RouteSupport.Unsupported,vm.ScanCommand.CanExecute(null));
            Assert.Equal(vm.SurfaceTitle=="Network"&&vm.RouteBinding.Support!=RouteSupport.Unsupported,vm.PingCommand.CanExecute(null));
            Assert.Equal(vm.SurfaceTitle=="Network"&&vm.RouteBinding.Support!=RouteSupport.Unsupported,vm.ResolveDnsCommand.CanExecute(null));
            if(vm.RouteBinding.Support==RouteSupport.Unsupported)
            {
                Assert.False(vm.ApplyCommand.CanExecute(null));
                Assert.False(vm.OpenToolCommand.CanExecute(null));
            }
            Assert.All(vm.Actions,a=>Assert.Equal(vm.SurfaceTitle,a.Center));
            Assert.Equal(vm.RouteBinding.Support==RouteSupport.Unsupported?0:Handoffs.For(vm.SurfaceTitle).Count,vm.Tools.Count);
            Assert.NotEmpty(vm.FeatureId);
            Assert.NotEmpty(vm.RouteDetail);
            Assert.NotEmpty(vm.Subtitle);
        }
    });
    [Fact]public void GroupedNavigationAndCompactSidebarPreferencesPersist()=>Run(vm=>
    {
        Assert.True(vm.NavigationView.Groups?.Count>1);
        vm.ToggleSidebarCommand.Execute(null);Assert.True(vm.SidebarCollapsed);Assert.Equal(new System.Windows.GridLength(64),vm.SidebarWidth);
        vm.Navigate("Game Library");var reopened=new ShellViewModel(vm.Runtime);
        Assert.True(reopened.SidebarCollapsed);Assert.Equal("Game Library",reopened.Selected?.Name);
        reopened.NotificationsCommand.Execute(null);Assert.Equal("Activity & Notifications",reopened.Title);
        Assert.Equal("Activity & Notifications",vm.Runtime.Journal.GetSetting("last-page"));
        Assert.Equal(reopened.Selected?.FeatureId,vm.Runtime.Journal.GetSetting("last-feature"));
    });
    [Fact]public void SavedReportsLoadOnNewShellAndSwitchContent()=>Run(vm=>
    {
        vm.Runtime.Journal.SaveReport(new("Hardware",DateTimeOffset.UtcNow.AddSeconds(-1),[new("CPU","Test CPU","Fixture")],[]));
        vm.Runtime.Journal.SaveReport(new("Network benchmark",DateTimeOffset.UtcNow,[new("Replies","6 / 6","Fixture")],[]));
        var reopened=new ShellViewModel(vm.Runtime);reopened.Navigate("Reports");
        Assert.Equal(2,reopened.Reports.Count);Assert.Equal("Replies",reopened.Facts.Single().Name);
        reopened.SelectedReport=reopened.Reports.Last();Assert.Equal("CPU",reopened.Facts.Single().Name);
        Assert.True(reopened.ExportCommand.CanExecute(null));Assert.Contains("Hardware",reopened.ObservationSummary);
        reopened.Navigate("GPU");Assert.Empty(reopened.Facts);Assert.Contains("Belum ada scan",reopened.ObservationSummary);
    });
    [Fact]public void RestoreSelectsCurrentRecordAndActionsReflectItsStatus()=>Run(vm=>
    {
        var change=new Change{ActionId="test",Status="Verified"};vm.Runtime.Journal.Save(change);vm.Navigate("Restore");
        Assert.Equal(change.Id,vm.SelectedChange?.Id);Assert.True(vm.RollbackCommand.CanExecute(null));
        vm.Runtime.Journal.Save(change with{Status="RolledBack"});vm.Navigate("Dashboard");vm.Navigate("Restore");
        Assert.False(vm.RollbackCommand.CanExecute(null));Assert.True(vm.VerifyCommand.CanExecute(null));
    });
    [Fact]public void SearchFindsAliasesAndRecoversAllPages()=>Run(vm=>
    {
        vm.Search="webcam";Assert.Equal("Webcam Diagnostics",vm.Navigation.Single().Name);
        vm.Search="no-such-feature";Assert.Empty(vm.Navigation);
        vm.Search="";Assert.Equal(ShellViewModel.AllNavigation.Count,vm.Navigation.Count);
    });
    [Fact]public void ObservationFilterSearchesEvidenceFieldsAndResetsForANewPage()=>Run(vm=>
    {
        var service=new Fact("Windows Update","Stopped","Win32_Service","Direct","Available");
        vm.Facts.Add(service);
        vm.Facts.Add(new("Scheduled task","Ready","MSFT_ScheduledTask","Unavailable","Unavailable"));
        Assert.Equal(2,vm.FactsView.Cast<Fact>().Count());
        vm.SelectedFact=service;
        Assert.True(vm.HasFactDetails);Assert.Contains("Sumber",vm.FactDetails);
        vm.Language="en-US";Assert.Contains("Source",vm.FactDetails);
        vm.FactFilter="windows update";Assert.Single(vm.FactsView.Cast<Fact>());
        vm.FactFilter="stopped";Assert.Single(vm.FactsView.Cast<Fact>());
        vm.FactFilter="win32_service";Assert.Single(vm.FactsView.Cast<Fact>());
        vm.FactFilter="direct";Assert.Single(vm.FactsView.Cast<Fact>());
        vm.FactFilter="unavailable";Assert.Single(vm.FactsView.Cast<Fact>());
        Assert.Null(vm.SelectedFact);Assert.False(vm.HasFactDetails);
        vm.Navigate("Windows Services");
        Assert.Equal("",vm.FactFilter);
    });
    [Fact]public void LanguagePacksAreCompleteSelectableAndPersisted()=>Run(vm=>
    {
        Assert.Equal(6,vm.LanguageOptions.Count);
        Assert.Equal(6,vm.LanguageOptions.Select(language=>language.Code).Distinct(StringComparer.Ordinal).Count());
        foreach(var language in vm.LanguageOptions)
        {
            vm.Language=language.Code;
            Assert.Equal(language.Code,vm.Localization.Language);
            Assert.NotEqual("[Language]",vm.Localization["Language"]);
            Assert.NotEqual("[Search]",vm.Localization["Search"]);
            Assert.Equal(vm.Localization["ImplementedStatus"],vm.RouteStatus);
            Assert.Equal(vm.Localization["ReadyStatus"],vm.Status);
            Assert.Equal(vm.Localization["Sampling"],vm.TelemetryStatus);
        }
        Assert.Equal("zh-CN",vm.Runtime.Journal.GetSetting("language"));
        Assert.Equal("语言",vm.Localization["Language"]);
        Assert.Throws<ArgumentException>(()=>vm.Language="ru-RU");
        Assert.Equal("zh-CN",vm.Language);
        var reopened=new ShellViewModel(vm.Runtime);
        Assert.Equal("zh-CN",reopened.Language);
        Assert.Equal("语言",reopened.Localization["Language"]);
    });
    [Fact]public void VersionedSettingsDocumentsValidateBeforeImport()=>Run(vm=>
    {
        var valid=new UserSettingsDocument(1,"Light","Gaming","Advanced","en-US",true);
        Assert.Equal(valid,ShellViewModel.ValidateSettingsDocument(valid));
        var exported=ShellViewModel.SerializeSettingsDocument(valid);
        Assert.Equal(valid,ShellViewModel.DeserializeSettingsDocument(exported));
        Assert.DoesNotContain("credential",exported,StringComparison.OrdinalIgnoreCase);
        Assert.Throws<HbxException>(()=>ShellViewModel.ValidateSettingsDocument(valid with{SchemaVersion=2}));
        Assert.Throws<HbxException>(()=>ShellViewModel.ValidateSettingsDocument(valid with{Theme="Unknown"}));
        Assert.Throws<HbxException>(()=>ShellViewModel.ValidateSettingsDocument(valid with{Language="ru-RU"}));
        Assert.ThrowsAny<Exception>(()=>ShellViewModel.DeserializeSettingsDocument("{"));
    });
    [Fact]public void SafeTestCenterRunsAndPersistsHonestTimestampedResults()=>Run(vm=>
    {
        vm.Navigate("Test Center");
        Assert.True(vm.IsTestCenter);Assert.False(vm.ScanCommand.CanExecute(null));Assert.True(vm.RunSafeTestsCommand.CanExecute(null));
        vm.RunSafeTestsCommand.Execute(null);
        var frame=new DispatcherFrame();var deadline=DateTime.UtcNow.AddSeconds(25);
        var timer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(20)};
        timer.Tick+=(_,_)=>{if(!vm.Busy||DateTime.UtcNow>=deadline){frame.Continue=false;timer.Stop();}};
        timer.Start();Dispatcher.PushFrame(frame);timer.Stop();
        Assert.False(vm.Busy,"Safe Test Center did not finish before the deadline.");
        var run=Assert.Single(vm.TestRuns);Assert.NotEmpty(run.Id);Assert.NotEqual(default,run.StartedAt);
        Assert.Equal(run.Results.Count,vm.TestResults.Count);Assert.Equal(run.Results.Count,run.Results.Select(result=>result.TestId).Distinct(StringComparer.Ordinal).Count());
        Assert.All(run.Results,result=>
        {
            Assert.Contains(result.Result,new[]{"PASS","FAIL","BLOCKED","SKIPPED","NOT RUN","UNSUPPORTED"});
            Assert.NotEqual(default,result.StartedAt);Assert.True(result.DurationMilliseconds>=0);Assert.NotEmpty(result.Evidence);
        });
        Assert.Equal("PASS",run.Results.Single(result=>result.TestId=="ui.route-bindings").Result);
        Assert.Equal("PASS",run.Results.Single(result=>result.TestId=="backend.diagnostics").Result);
        Assert.Equal("PASS",run.Results.Single(result=>result.TestId=="permissions.current-user").Result);
        Assert.Equal("PASS",run.Results.Single(result=>result.TestId=="database.integrity").Result);
        Assert.Equal("PASS",run.Results.Single(result=>result.TestId=="configuration.integrity").Result);
        Assert.Equal("PASS",run.Results.Single(result=>result.TestId=="recovery.journal").Result);
        Assert.Equal("PASS",run.Results.Single(result=>result.TestId=="operations.background").Result);
        Assert.Equal("PASS",run.Results.Single(result=>result.TestId=="errors.history").Result);
        Assert.Equal("PASS",run.Results.Single(result=>result.TestId=="resources.process").Result);
        Assert.Contains(run.Results.Single(result=>result.TestId=="backup.integrity").Result,new[]{"PASS","SKIPPED"});
        var workerResult=run.Results.Single(result=>result.TestId=="privileged-worker.ipc");
        Assert.True(workerResult.Result=="PASS",workerResult.Evidence);
        Assert.Equal("UNSUPPORTED",run.Results.Single(result=>result.TestId=="application.file-integrity").Result);
        Assert.Equal("UNSUPPORTED",run.Results.Single(result=>result.TestId=="repair.safe-workflows").Result);
        Assert.Equal("SKIPPED",run.Results.Single(result=>result.TestId=="ai.connection").Result);
        Assert.Equal("SKIPPED",run.Results.Single(result=>result.TestId=="webhook.connection").Result);
        Assert.Single(vm.Runtime.Documents.Read("test-history",new List<SafeTestRun>()));
        Assert.Contains(vm.Runtime.Journal.Reports(),report=>report.Center=="Test Center");
        vm.Navigate("Application Health");Assert.True(vm.IsTestCenter);
    });
    [Fact]public void CancelledSafeTestRunRecordsRemainingChecksAsNotRun()=>Run(vm=>
    {
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        var run=TestCenterRunner.RunAsync(vm.Runtime,cancellation.Token,()=>false).GetAwaiter().GetResult();
        Assert.True(run.Cancelled);Assert.NotEmpty(run.Results);
        Assert.All(run.Results,result=>Assert.Equal("NOT RUN",result.Result));
        Assert.All(run.Results,result=>Assert.Equal("HBX-TEST-CANCELLED",result.ErrorCode));
        Assert.Contains(vm.Runtime.Documents.Read("test-history",new List<SafeTestRun>()),saved=>saved.Id==run.Id);
    });

    [Fact]public void DuplicateAndLargeFileRouteRunsRealReadOnlyAnalysis()=>Run(vm=>
    {
        var root=Path.Combine(Path.GetTempPath(),"HyperBoostX-shell-files",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            var bytes=new byte[1024*1024];Random.Shared.NextBytes(bytes);
            File.WriteAllBytes(Path.Combine(root,"first.bin"),bytes);File.WriteAllBytes(Path.Combine(root,"second.bin"),bytes);
            vm.Navigate("Duplicate & Large Files");Assert.Equal(RouteSupport.Implemented,vm.RouteBinding.Support);Assert.True(vm.IsFileAnalysis);
            vm.FileAnalysisRoot=root;vm.MinimumLargeFileMiB=1;
            var task=vm.RunFileAnalysis(default);var frame=new DispatcherFrame();var timer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(20)};
            timer.Tick+=(_,_)=>{if(task.IsCompleted){frame.Continue=false;timer.Stop();}};timer.Start();Dispatcher.PushFrame(frame);task.GetAwaiter().GetResult();
            Assert.Single(vm.DuplicateGroups);Assert.Equal(2,vm.LargeFiles.Count);Assert.Empty(vm.FileAnalysisIssues);Assert.Contains("Cakupan lengkap",vm.FileAnalysisStatus);
            Assert.Equal(bytes,File.ReadAllBytes(Path.Combine(root,"first.bin")));
        }
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    });
    [Fact]public void WebhookCannotBeEnabledBeforeAValidDestinationIsSaved()=>Run(vm=>
    {
        Assert.False(vm.WebhookEnabled);Assert.False(vm.CanToggleWebhook);
        vm.WebhookEnabled=true;
        Assert.False(vm.WebhookEnabled);Assert.False(vm.Runtime.Notifications.Enabled);
        Assert.Contains("URL webhook",vm.Status);
    });
    [Fact]public void MissingTargetDisablesApplyAndGuidanceCanBeExported()=>Run(vm=>
    {
        vm.Navigate("Startup");vm.SelectedStartup=null;Assert.False(vm.ApplyCommand.CanExecute(null));
        vm.Navigate("Power");vm.SelectedPlan=null;Assert.False(vm.ApplyCommand.CanExecute(null));
        vm.Navigate("Knowledge Base");Assert.True(vm.HasFacts);Assert.True(vm.ExportCommand.CanExecute(null));
        Assert.Contains("Knowledge Base",vm.ObservationSummary);
    });
    [Fact]public async Task DnsDiagnosticsHandleIpInputsAndRejectUrls()
    {
        var report=await Diagnostics.ResolveDnsAsync("127.0.0.1",default);
        Assert.Equal("DNS resolution",report.Center);
        Assert.Equal("Not applicable: input is an IP address",report.Facts.Single(f=>f.Name=="DNS result").Value);
        var local=await Diagnostics.ResolveDnsAsync("localhost",default);
        Assert.Equal("Resolved",local.Facts.Single(f=>f.Name=="DNS result").Value);
        Assert.NotEmpty(local.Facts.Single(f=>f.Name=="Addresses").Value);
        await Assert.ThrowsAsync<HbxException>(()=>Diagnostics.ResolveDnsAsync("https://example.com",default));
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>Diagnostics.ResolveDnsAsync("127.0.0.1",cancelled.Token));
    }
    [Fact]public async Task StartupDiagnosticsReportBootTimesOrExplicitUnavailableValues()
    {
        var report=await new Diagnostics().ScanAsync("Startup",default);
        AssertWmiProviderObserved(report,"Win32_Service");
        AssertWmiProviderObserved(report,"MSFT_ScheduledTask");
        foreach(var name in new[]{"Windows boot time","Windows main-path boot time","Startup completion time"})
        {
            var measurement=report.Facts.Single(f=>f.Name==name);
            Assert.Contains("Diagnostics-Performance",measurement.Source,StringComparison.Ordinal);
            Assert.Contains(measurement.Availability,new[]{"Available","Unavailable"});
            if(measurement.Availability=="Unavailable")Assert.Equal("Unavailable",measurement.Value);
            else Assert.Equal("Measured",measurement.Quality);
        }
    }
    [Fact]public async Task WindowsDiagnosticsExposeReadOnlyServiceAndTaskInventoryResults()
    {
        var report=await new Diagnostics().ScanAsync("Windows",default);
        AssertWmiProviderObserved(report,"Win32_Service");
        AssertWmiProviderObserved(report,"MSFT_ScheduledTask");
    }
}

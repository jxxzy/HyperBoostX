using System.IO;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using HyperBoostX.Core;
using HyperBoostX.Windows;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class IntegrationProviderTests:IDisposable
{
    [Theory]
    [InlineData("NVIDIA GeForce RTX 4090",null,"NVIDIA","GeForce RTX / RTX")]
    [InlineData("AMD Radeon RX 7900 XT",null,"AMD","Radeon RX")]
    [InlineData("Intel Arc A770",null,"Intel","Intel Arc")]
    [InlineData("Intel Iris Xe Graphics",null,"Intel","Intel Iris Xe")]
    [InlineData("Microsoft Basic Display Adapter",null,"Microsoft","Basic Display Adapter")]
    [InlineData("Mystery Display Adapter",null,"Unknown","Unknown")]
    [InlineData("Unknown adapter",@"PCI\VEN_10DE&DEV_1234","NVIDIA","NVIDIA GPU")]
    [InlineData("NVIDIA GeForce",@"PCI\VEN_8086&DEV_1234","Intel","Intel GPU")]
    [InlineData("NVIDIA GeForce",@"PCI\VEN_1234&DEV_1234","Unknown","Unknown")]
    [InlineData(null,null,"Unknown","Unknown")]
    public void GpuIdentityAdaptsLegacyClassificationWithoutInventingHardware(string? model,string? deviceId,string vendor,string family)
    {
        var identity=GpuIdentity.Classify(model,deviceId);
        Assert.Equal(vendor,identity.Vendor);Assert.Equal(family,identity.Family);
        Assert.False(string.IsNullOrWhiteSpace(identity.Profile));
    }

    [Fact]public async Task TelemetryReadsPhysicalMemoryAndSecondCpuIntervalFromWindows()
    {
        var telemetry=new Telemetry();
        var first=telemetry.Sample();
        Assert.True(first.TotalMemory>0);Assert.InRange(first.AvailableMemory,0UL,first.TotalMemory);
        Assert.InRange(first.MemoryPercent,0,100);
        Assert.InRange(first.UptimeMilliseconds,0,long.MaxValue);
        await Task.Delay(250);
        var second=telemetry.Sample();
        if(second.CpuProcessorGroups==1){Assert.Equal(MeasurementAvailability.Available,second.CpuAvailability);Assert.NotNull(second.CpuPercent);Assert.InRange(second.CpuPercent.Value,0,100);}
        else{Assert.Equal(MeasurementAvailability.Limited,second.CpuAvailability);Assert.Null(second.CpuPercent);}
        Assert.InRange(second.MemoryPercent,0,100);Assert.True(second.ObservedAt>=first.ObservedAt);
        telemetry.ResetBaseline();Assert.Null(telemetry.Sample().CpuPercent);
    }
    [Fact]public void TelemetryReadsWindowsLowMemorySignalWithoutInducingPressure()
    {
        var sample=new Telemetry().Sample();
        Assert.NotNull(sample.LowMemorySignaled);
        Assert.Null(sample.MemoryNotificationWin32Error);
    }
    [Fact]public void HealthScoreRejectsNonFiniteAndInvalidMeasuredComponents()
    {
        var sample=new TelemetrySample(DateTimeOffset.UnixEpoch,double.NaN,1000,1200,null,null,0,"fixture"){CpuAvailability=MeasurementAvailability.Available,NetworkAvailability=MeasurementAvailability.Unavailable};
        var score=SystemHealthScore.Calculate(sample,new HealthScoreEvidence(double.PositiveInfinity,"gpu",-1,"storage",50,"reliability"));
        Assert.Equal(1,score.AvailableComponents);Assert.Equal(50,score.Score);
    }
    [Fact]public void CommitPageCountsConvertUsingProviderPageSize()
    {
        var result=Telemetry.ConvertCommitPages(4,10,4096);
        Assert.Equal((16384UL,40960UL),result);
        Assert.Throws<ArgumentOutOfRangeException>(()=>Telemetry.ConvertCommitPages(1,2,0));
        Assert.Throws<OverflowException>(()=>Telemetry.ConvertCommitPages(ulong.MaxValue,1,4096));
    }
    [Fact]public void TelemetryReadsActualWindowsCommitCharge()
    {
        var sample=new Telemetry().Sample();
        Assert.Equal(MeasurementAvailability.Available,sample.CommitAvailability);
        Assert.True(sample.CommitUsedBytes>0);
        Assert.True(sample.CommitLimitBytes>=sample.CommitUsedBytes);
        Assert.Null(sample.CommitWin32Error);
    }
    [Fact]public void HealthScoreUsesOnlyAvailableMeasuredComponents()
    {
        var sample=new TelemetrySample(DateTimeOffset.UnixEpoch,20,1000,250,null,null,1000,"fixture") {CpuAvailability=MeasurementAvailability.Available,NetworkAvailability=MeasurementAvailability.Unavailable};var result=SystemHealthScore.Calculate(sample);
        Assert.Equal(52.5,result.Score,1);Assert.Equal(2,result.AvailableComponents);Assert.Contains(result.Facts,f=>f.Name=="HyperBoost Score"&&f.Availability=="Limited");Assert.Contains(result.Facts,f=>f.Name=="Network component"&&f.Value=="Not scored");
    }
    [Fact]public void HealthScoreReportsFullCoverageWithoutInventingWeights()
    {
        var sample=new TelemetrySample(DateTimeOffset.UnixEpoch,10,1000,500,1,1,1000,"fixture") {CpuAvailability=MeasurementAvailability.Available,NetworkAvailability=MeasurementAvailability.Available};var result=SystemHealthScore.Calculate(sample);
        Assert.Equal(80,result.Score,1);Assert.Equal(3,result.AvailableComponents);Assert.Empty(result.Findings);Assert.Contains(result.Facts,f=>f.Name=="Method"&&f.Source=="HBX-HEALTH-SCORE-2");
    }
    [Fact]public void HealthScoreIncludesAllSixDocumentedComponentsWhenEvidenceExists()
    {
        var sample=new TelemetrySample(DateTimeOffset.UnixEpoch,10,1000,250,1,1,1000,"fixture") {CpuAvailability=MeasurementAvailability.Available,NetworkAvailability=MeasurementAvailability.Available};
        var result=SystemHealthScore.Calculate(sample,new HealthScoreEvidence(100,"gpu",80,"storage",90,"reliability"));
        Assert.Equal(6,result.TotalComponents);Assert.Equal(6,result.AvailableComponents);Assert.Contains(result.Facts,f=>f.Name=="HyperBoost Score"&&f.Availability=="Available");Assert.Contains(result.Facts,f=>f.Name=="Method"&&f.Source=="HBX-HEALTH-SCORE-2");
    }
    [Fact]public async Task HealthScoreMeasurementPublishesSixComponentCoverageWithoutInventedValues()
    {
        var report=await SystemHealthScore.MeasureAsync(CancellationToken.None);
        Assert.Contains(report.Facts,f=>f.Name=="HyperBoost Score"&&f.Source.Contains("/6 available components",StringComparison.Ordinal));
        Assert.Contains(report.Facts,f=>f.Name=="GPU component");Assert.Contains(report.Facts,f=>f.Name=="Storage component");Assert.Contains(report.Facts,f=>f.Name=="Reliability component");
        Assert.All(report.Facts.Where(f=>f.Name.EndsWith("component",StringComparison.Ordinal)),fact=>Assert.Contains(fact.Availability,new[]{"Available","Limited","Unavailable"}));
    }

    private readonly string _root=Path.Combine(Path.GetTempPath(),"HyperBoostX-tests",Guid.NewGuid().ToString("N"));
    public IntegrationProviderTests()=>Directory.CreateDirectory(_root);
    private DocumentStore Store=>new(new Journal(_root,new WindowsProtector()),new WindowsProtector());
    private sealed class Handler(Func<HttpRequestMessage,Task<HttpResponseMessage>> respond):HttpMessageHandler
    {public int Calls;protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){Calls++;return respond(request);}}
    private static HttpResponseMessage Response(string text,HttpStatusCode status=HttpStatusCode.OK)=>new(status){Content=new StringContent(text,Encoding.UTF8,"application/json")};
    [Fact]public async Task BootDiagnosticsReturnsHistoryImpactAndDependencyEvidenceOrExplicitAvailability()
    {
        var report=await BootDiagnostics.ScanAsync(default);
        Assert.Equal("Boot Optimizer",report.Center);Assert.NotEmpty(report.Facts);
        Assert.Contains(report.Facts,fact=>fact.Name.StartsWith("Boot history",StringComparison.Ordinal));
        Assert.Contains(report.Facts,fact=>fact.Name.StartsWith("Startup ",StringComparison.Ordinal));
        Assert.Contains(report.Facts,fact=>fact.Name.Contains("service dependencies",StringComparison.OrdinalIgnoreCase));
        Assert.All(report.Facts,fact=>Assert.Contains(fact.Availability,new[]{"Available","Unavailable"}));
    }
    [Fact]public async Task DriveDiagnosticsReturnsMediaVolumeScheduleAndTrimEvidenceWithoutMutation()
    {
        var report=await DriveDiagnostics.ScanAsync(default);
        Assert.Equal("Drive Optimization",report.Center);Assert.NotEmpty(report.Facts);
        Assert.Contains(report.Facts,fact=>fact.Name.StartsWith("Physical disk",StringComparison.Ordinal));
        Assert.Contains(report.Facts,fact=>fact.Name.StartsWith("Volume",StringComparison.Ordinal));
        Assert.Contains(report.Facts,fact=>fact.Name.StartsWith("Optimize schedule",StringComparison.Ordinal));
        Assert.Contains(report.Facts,fact=>fact.Name=="TRIM delete notification");
        Assert.All(report.Facts,fact=>Assert.Contains(fact.Availability,new[]{"Available","Unavailable"}));
    }
    [Fact]public async Task SmartScanReportsVisualEffectsStateOrExplicitUnavailability()
    {
        var report=await new Diagnostics().ScanAsync("Smart Scan",default);
        var preset=Assert.Single(report.Facts,fact=>fact.Name=="Visual effects preset");
        Assert.Contains(preset.Availability,new[]{"Available","Unavailable"});
        if(preset.Availability=="Available")Assert.Contains("HKCU VisualFXSetting",preset.Source,StringComparison.Ordinal);
    }
    [Theory]
    [InlineData("iCUE.exe",true)]
    [InlineData("NZXT CAM",true)]
    [InlineData("L-Connect 3",true)]
    [InlineData("AIDA64",false)]
    [InlineData("HWiNFO64",false)]
    public void LcdUtilitySignaturesIdentifyCoolingCriticalExclusions(string name,bool coolingCritical)=>Assert.Equal(coolingCritical,LcdPerformanceDetection.Classify(name)?.CoolingCritical);

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-1d)]
    [InlineData(101d)]
    public async Task LcdGuardRejectsInvalidCpuThresholdBeforeSampling(double threshold)
        =>await Assert.ThrowsAsync<HbxException>(()=>LcdPerformanceDetection.ScanAsync(threshold,1,false,false,default));

    [Fact]public async Task LcdGuardHonorsPreCancelledToken()
    {
        using var source=new CancellationTokenSource();source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>LcdPerformanceDetection.ScanAsync(10,100,false,false,source.Token));
    }

    [Fact]public void MsiDependenciesDistinguishUnknownEmptyAndConfiguredLists()
    {
        Assert.Equal("Unavailable",MsiSoftwareDetection.DescribeDependencies(null,[]));
        Assert.Equal("Unavailable",MsiSoftwareDetection.DescribeDependencies([],null));
        Assert.Equal("none configured",MsiSoftwareDetection.DescribeDependencies([],[]));
        Assert.Equal("service: FanControl, group: Network",MsiSoftwareDetection.DescribeDependencies(["FanControl"],["Network"]));
    }

    [Fact]public async Task LcdGuardMeasuresARealUtilityNamedProcessAndHonorsGamingAlertGate()
    {
        var executable=Path.Combine(_root,"AIDA64.exe");File.Copy(Path.Combine(Environment.SystemDirectory,"ping.exe"),executable);
        using var process=Process.Start(new ProcessStartInfo(executable,"127.0.0.1 -n 4"){UseShellExecute=false,CreateNoWindow=true})!;
        try
        {
            var inactive=await LcdPerformanceDetection.ScanAsync(0,1,true,false,default);
            Assert.Contains(inactive.Facts,fact=>fact.Name=="SensorPanel"&&fact.Value.Contains($"PID {process.Id}",StringComparison.Ordinal));Assert.Empty(inactive.Findings);
            var active=await LcdPerformanceDetection.ScanAsync(0,1,true,true,default);
            Assert.Contains(active.Findings,finding=>finding.Title.Contains("SensorPanel",StringComparison.Ordinal));
        }
        finally{if(!process.HasExited)process.Kill(true);await process.WaitForExitAsync();}
    }
    [Theory]
    [InlineData("")]
    [InlineData("relative.exe")]
    [InlineData(@"C:\missing-hyperboostx-fixture.exe")]
    [InlineData(@"\\server\share\application.exe")]
    [InlineData(@"C:\Windows\not-an-executable.txt")]
    public void AppLaunchMeasurementRejectsInvalidOrNonLocalExecutables(string path)=>Assert.ThrowsAny<Exception>(()=>ApplicationLaunchMeasurement.ValidateExecutable(path));

    [Fact]public async Task AppLaunchMeasurementStartsARealExecutableAndReportsOnlyObservedReadiness()
    {
        var executable=Path.Combine(_root,"launch-fixture.exe");File.Copy(Path.Combine(Environment.SystemDirectory,"where.exe"),executable);
        var result=await ApplicationLaunchMeasurement.MeasureAsync(executable,default);
        Assert.Equal(executable,result.ExecutablePath,StringComparer.OrdinalIgnoreCase);Assert.True(result.ProcessId>0);Assert.True(result.ProcessCreatedMilliseconds>=0);
        var report=ApplicationLaunchMeasurement.ToReport(result);
        Assert.Equal("App Load Optimizer",report.Center);Assert.Contains(report.Facts,fact=>fact.Name=="Process created"&&fact.Quality=="Measured");
        var readiness=Assert.Single(report.Facts,fact=>fact.Name=="Window input ready");
        if(result.InputIdleMilliseconds is null){Assert.Equal("Unavailable",readiness.Value);Assert.Equal("Unavailable",readiness.Availability);}
        else{Assert.EndsWith(" ms",readiness.Value);Assert.Equal("Available",readiness.Availability);}
    }
    [Fact]public async Task InstalledApplicationInventoryIsBoundedAndReportsCoverage()
    {
        var report=await InstalledApplicationDetection.ScanAsync(default);
        Assert.Equal("App Load Optimizer",report.Center);Assert.NotEmpty(report.Facts);
        var coverage=Assert.Single(report.Facts,fact=>fact.Name=="Inventory coverage");Assert.Contains("MSIX",coverage.Value,StringComparison.OrdinalIgnoreCase);
        Assert.Contains(report.Facts,fact=>fact.Name.StartsWith("MSIX package:",StringComparison.Ordinal)||fact.Name=="MSIX application inventory");
        Assert.True(report.Facts.Count<=2001);Assert.All(report.Facts,fact=>Assert.Contains(fact.Availability,new[]{"Available","Unavailable"}));
    }
    [Fact]public async Task ReleasePackageInspectionHashesAndTrustChecksWithoutExecutingTheCandidate()
    {
        var candidate=Path.Combine(_root,"candidate.exe");File.Copy(Path.Combine(Environment.SystemDirectory,"where.exe"),candidate);var before=File.GetLastWriteTimeUtc(candidate);
        var report=await ReleasePackageInspection.InspectAsync(candidate,new Version(3,0),default);
        Assert.Equal("Update Center",report.Center);Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(candidate))),report.Facts.Single(f=>f.Name=="SHA-256").Value);Assert.Contains(report.Facts,f=>f.Name=="Authenticode trust");Assert.Contains(report.Facts,f=>f.Name=="Execution policy"&&f.Value.Contains("did not execute",StringComparison.Ordinal));Assert.Equal(before,File.GetLastWriteTimeUtc(candidate));
    }
    [Theory][InlineData("")][InlineData("relative.exe")][InlineData(@"\\server\share\candidate.exe")][InlineData(@"C:\missing-candidate.exe")]
    public void ReleasePackageInspectionRejectsUnsafePaths(string path)=>Assert.ThrowsAny<Exception>(()=>ReleasePackageInspection.ValidatePath(path));

    [Fact]public async Task PluginManifestInspectionValidatesHashPermissionsCompatibilityAndNeverLoadsCode()
    {
        var root=Path.Combine(_root,"plugins");var plugin=Path.Combine(root,"fixture");Directory.CreateDirectory(plugin);var assembly=Path.Combine(plugin,"fixture.dll");File.Copy(Path.Combine(Environment.SystemDirectory,"where.exe"),assembly);var hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(assembly)));
        File.WriteAllText(Path.Combine(plugin,"plugin.json"),Json.Write(new PluginManifest(1,"fixture.plugin","1.0.0","1.0.0","Fixture Publisher","fixture.dll",hash,["Diagnostics.Read"])));
        var report=await PluginManifestInspection.ScanAsync(root,new Version(3,0),default);Assert.Equal("Plugin Manager",report.Center);Assert.Contains(report.Facts,f=>f.Name=="fixture.plugin"&&f.Value.Contains("hash True",StringComparison.Ordinal));Assert.Contains(report.Facts,f=>f.Name=="fixture.plugin activity"&&f.Value.Contains("Never executed",StringComparison.Ordinal));
    }
    [Fact]public async Task PluginManifestInspectionRejectsUnknownPermissionsAndPathTraversal()
    {
        var root=Path.Combine(_root,"bad-plugins");var plugin=Path.Combine(root,"bad");Directory.CreateDirectory(plugin);File.WriteAllText(Path.Combine(plugin,"plugin.json"),Json.Write(new PluginManifest(1,"bad.plugin","1.0","1.0","Bad","..\\outside.dll",new string('0',64),["Process.Execute"])));
        var report=await PluginManifestInspection.ScanAsync(root,new Version(3,0),default);Assert.Contains(report.Facts,f=>f.Name=="bad"&&f.Value.StartsWith("Rejected:",StringComparison.Ordinal));
    }
    [Theory]
    [InlineData("missing")]
    [InlineData("null")]
    [InlineData("duplicate")]
    [InlineData("traversal")]
    [InlineData("duplicate-permission")]
    public async Task PluginManifestRejectsMalformedFieldsAndTraversalWithoutStoppingInventory(string kind)
    {
        var root=Path.Combine(_root,"plugin-validation");var plugin=Path.Combine(root,"invalid");Directory.CreateDirectory(plugin);
        var manifest=Json.Write(new PluginManifest(1,"test.plugin","1.0","1.0","Fixture",kind=="traversal"?"../outside.dll":"fixture.dll",new string('0',64),
            kind=="duplicate-permission"?["Diagnostics.Read","Diagnostics.Read"]:[]));
        if(kind=="missing"||kind=="null"||kind=="duplicate")
        {
            var obj=System.Text.Json.Nodes.JsonNode.Parse(manifest)!.AsObject();
            if(kind=="missing")obj.Remove("Permissions");
            if(kind=="null")obj["Permissions"]=null;
            manifest=obj.ToJsonString();
            if(kind=="duplicate")manifest=manifest.Insert(1,"\"SchemaVersion\":1,");
        }
        File.WriteAllText(Path.Combine(plugin,"plugin.json"),manifest);
        var second=Path.Combine(root,"second");Directory.CreateDirectory(second);
        var report=await PluginManifestInspection.ScanAsync(root,new Version(3,0),default);
        Assert.Contains(report.Facts,f=>f.Name=="invalid"&&f.Value.StartsWith("Rejected:",StringComparison.Ordinal));
        Assert.Contains(report.Facts,f=>f.Name=="second"&&f.Value.Contains("plugin.json missing",StringComparison.Ordinal));
    }
    [Fact]public void PackageInspectionRejectsAlternateDataStreamPaths()
    {
        var path=Path.Combine(_root,"candidate.exe");File.WriteAllText(path,"fixture");
        Assert.Throws<HbxException>(()=>ReleasePackageInspection.ValidatePath(path+":hidden.exe"));
    }

    [Fact]public async Task NetworkProfilePreviewReturnsAdaptersDnsPolicyProfilesAndExplicitNoChangeState()
    {
        var report=await NetworkProfileDiagnostics.ScanAsync("Balanced",default);Assert.Equal("Network Profiles",report.Center);Assert.Contains(report.Facts,f=>f.Name=="Selected profile"&&f.Value=="Balanced");Assert.Contains(report.Facts,f=>f.Name=="Change policy"&&f.Value.Contains("No DNS server",StringComparison.Ordinal));Assert.Contains(report.Facts,f=>f.Name=="Restore state"&&f.Value.Contains("No HyperBoostX-managed",StringComparison.Ordinal));Assert.Contains(report.Facts,f=>f.Name.StartsWith("DNS policy",StringComparison.Ordinal)||f.Name=="DNS policy provider");
    }
    [Fact]public async Task NetworkProfilePreviewRejectsUnknownProfiles()=>await Assert.ThrowsAsync<HbxException>(()=>NetworkProfileDiagnostics.ScanAsync("Ultra magic",default));
    [Theory]
    [InlineData("iCUE.exe","Corsair")]
    [InlineData("LightingService","ASUS")]
    [InlineData("MSI Center SDK","MSI")]
    [InlineData("Razer Synapse Service","Razer")]
    [InlineData("OpenRGB","OpenRGB")]
    public void RgbVendorSignaturesClassifyKnownSoftware(string value,string vendor)=>Assert.Equal(vendor,VendorSoftwareDetection.ClassifyRgb(value)?.Vendor);

    [Theory][InlineData("MSI Center.exe")][InlineData("Mystic Light")][InlineData("LEDKeeper2")][InlineData("Dragon Center")]
    public void MsiUtilitySignaturesClassifySupportedSoftware(string value)=>Assert.True(MsiSoftwareDetection.IsMsiUtility(value));
    [Theory][InlineData("MSI Center Service")][InlineData("Frozr Cooling Service")][InlineData("Pump Controller")]
    public void MsiCoolingExclusionsAreConservative(string value)=>Assert.True(MsiSoftwareDetection.IsCoolingCritical(value));
    [Fact]public async Task MsiSafeOptimizerMeasuresARealNamedProcessWithoutStoppingIt()
    {
        var executable=Path.Combine(_root,"MSICenter.exe");File.Copy(Path.Combine(Environment.SystemDirectory,"ping.exe"),executable);
        using var process=Process.Start(new ProcessStartInfo(executable,"127.0.0.1 -n 4"){UseShellExecute=false,CreateNoWindow=true})!;
        try{var report=await MsiSoftwareDetection.ScanAsync(default);Assert.Equal("MSI Safe Optimizer",report.Center);Assert.Contains(report.Facts,f=>f.Value.Contains($"PID {process.Id}",StringComparison.Ordinal)&&f.Value.Contains("protected from termination",StringComparison.Ordinal));Assert.False(process.HasExited);}
        finally{if(!process.HasExited)process.Kill(true);await process.WaitForExitAsync();}
    }

    [Fact]public void RgbVendorSignaturesDoNotGuessUnknownSoftware()=>Assert.Null(VendorSoftwareDetection.ClassifyRgb("ordinary-unrelated-application"));

    [Fact]public async Task RgbInventoryReturnsDirectObservationsOrExplicitProviderIssues()
    {
        var report=await VendorSoftwareDetection.ScanRgbAsync(default);
        Assert.Equal("RGB Software Detector",report.Center);Assert.NotEmpty(report.Facts);
        Assert.All(report.Facts,fact=>Assert.Contains(fact.Availability,new[]{"Available","Unavailable"}));
    }

    [Theory]
    [InlineData("L-Connect.exe","Lian Li")]
    [InlineData("LConnect3.exe","Lian Li")]
    [InlineData("OpenRGB.exe","OpenRGB")]
    [InlineData("SignalRGB.exe","Whirlwind FX")]
    public void RgbProcessAliasesRecognizeLegacyControllers(string name,string vendor)
        =>Assert.Equal(vendor,VendorSoftwareDetection.ClassifyRgbProcess(name)?.Vendor);

    [Theory]
    [InlineData("icuebackup.exe")]
    [InlineData("openrgbinstaller.exe")]
    [InlineData("lconnectuninstall.exe")]
    public void RgbProcessAliasesRejectUnrelatedNames(string name)
        =>Assert.Null(VendorSoftwareDetection.ClassifyRgbProcess(name));

    [Fact]public async Task RgbInventoryMeasuresRunningControllerCpuAndMemory()
    {
        var executable=Path.Combine(_root,"OpenRGB.exe");File.Copy(Path.Combine(Environment.SystemDirectory,"ping.exe"),executable);
        using var process=Process.Start(new ProcessStartInfo(executable,"127.0.0.1 -n 4"){UseShellExecute=false,CreateNoWindow=true})!;
        try
        {
            var report=await VendorSoftwareDetection.ScanRgbAsync(default);
            var fact=Assert.Single(report.Facts,item=>item.Name=="OpenRGB"&&item.Source.StartsWith("Running process",StringComparison.Ordinal));
            Assert.Contains("interval CPU",fact.Value);Assert.Contains("working set",fact.Value);Assert.Equal("Available",fact.Availability);
        }
        finally{if(!process.HasExited)process.Kill(true);await process.WaitForExitAsync();}
    }

    [Theory]
    [InlineData("Discord.exe","Discord")]
    [InlineData("gameoverlayui64","Valve")]
    [InlineData("NVIDIA Share","NVIDIA")]
    [InlineData("RadeonSoftware","AMD")]
    [InlineData("RTSS","RivaTuner")]
    [InlineData("GameBarFTServer","Microsoft")]
    public void OverlaySignaturesClassifyPrototypeProviders(string value,string vendor)=>Assert.Equal(vendor,OverlaySoftwareDetection.Classify(value)?.Vendor);

    [Theory]
    [InlineData("IntelArcControl.exe","Intel")]
    [InlineData("DiscordHookHelper.exe","Discord")]
    [InlineData("OBS32.exe","OBS Project")]
    [InlineData("obs.exe","OBS Project")]
    [InlineData("GameBarPresenceWriter.exe","Microsoft")]
    [InlineData("NVIDIA Broadcast.exe","NVIDIA")]
    [InlineData("TikTokLiveStudio.exe","TikTok")]
    [InlineData("RTSSHooksLoader.exe","RivaTuner")]
    public void OverlayProcessAliasesRecoverLegacyCatalog(string name,string vendor)
        =>Assert.Equal(vendor,OverlaySoftwareDetection.ClassifyProcess(name)?.Vendor);

    [Theory]
    [InlineData("steamship.exe")]
    [InlineData("medallion.exe")]
    [InlineData("discordbackup.exe")]
    [InlineData("nvcontainer.exe")]
    public void OverlayProcessAliasesRejectUnrelatedAndDriverServiceNames(string name)
        =>Assert.Null(OverlaySoftwareDetection.ClassifyProcess(name));

    [Fact]public void OverlayCpuMeasurementRejectsPidReuseAndInvalidSamples()
    {
        var start=DateTime.UnixEpoch;
        var before=TimeSpan.FromMilliseconds(100);var after=TimeSpan.FromMilliseconds(300);
        var elapsed=TimeSpan.FromMilliseconds(500);
        Assert.Equal(10d,OverlaySoftwareDetection.MeasureCpuPercent(start,start,before,after,elapsed,4));
        Assert.Equal(0d,OverlaySoftwareDetection.MeasureCpuPercent(start,start,before,before,elapsed,4));
        Assert.Null(OverlaySoftwareDetection.MeasureCpuPercent(start,start.AddSeconds(1),before,after,elapsed,4));
        Assert.Null(OverlaySoftwareDetection.MeasureCpuPercent(start,start,after,before,elapsed,4));
        Assert.Null(OverlaySoftwareDetection.MeasureCpuPercent(start,start,before,after,TimeSpan.Zero,4));
        Assert.Null(OverlaySoftwareDetection.MeasureCpuPercent(start,start,before,after,elapsed,0));
        Assert.Null(OverlaySoftwareDetection.MeasureCpuPercent(start,start,before,TimeSpan.FromSeconds(5),elapsed,4));
    }

    [Fact]public async Task OverlayInventoryMeasuresRunningProcessAndCombinedLoad()
    {
        var executable=Path.Combine(_root,"Discord.exe");File.Copy(Path.Combine(Environment.SystemDirectory,"ping.exe"),executable);
        using var process=Process.Start(new ProcessStartInfo(executable,"127.0.0.1 -n 4"){UseShellExecute=false,CreateNoWindow=true})!;
        try
        {
            var report=await OverlaySoftwareDetection.ScanAsync(default);
            var fact=Assert.Single(report.Facts,item=>item.Name=="Discord Overlay"&&item.Source.StartsWith("Process",StringComparison.Ordinal)&&item.Value.Contains($"PID {process.Id}",StringComparison.Ordinal));
            Assert.Contains("interval CPU",fact.Value);Assert.Contains("working set",fact.Value);
            Assert.Contains(report.Facts,item=>item.Name=="Combined observed process load"&&item.Quality=="Measured");
        }
        finally{if(!process.HasExited)process.Kill(true);await process.WaitForExitAsync();}
    }
    [Fact]public async Task ModelsComeFromProviderAndCredentialIsOnlyInHeader()
    {
        var handler=new Handler(request=>
        {
            Assert.Equal("https://integrate.api.nvidia.com/v1/models",request.RequestUri!.AbsoluteUri);
            Assert.Equal("Bearer",request.Headers.Authorization?.Scheme);Assert.Equal("fixture-key",request.Headers.Authorization?.Parameter);
            Assert.Null(request.Content);return Task.FromResult(Response("{\"data\":[{\"id\":\"provider/actual-model\"}]}"));
        });using var client=new HttpClient(handler);var provider=new NvidiaProvider(client);
        Assert.Equal("provider/actual-model",(await provider.ModelsAsync(NvidiaProvider.DefaultEndpoint,"fixture-key",default)).Single());
    }
    [Fact]public async Task AiCannotInjectAnUnregisteredActionAndEchoedSecretIsRedacted()
    {
        var handler=new Handler(async request=>
        {
            var body=await request.Content!.ReadAsStringAsync();Assert.DoesNotContain("fixture-key",body);
            var response=new{choices=new[]{new{message=new{content=Json.Write(new{explanation="fixture-key should never leak",action_ids=new[]{"safe.action","shell.arbitrary"}})}}}};
            return Response(Json.Write(response));
        });using var client=new HttpClient(handler);var provider=new NvidiaProvider(client);
        var advice=await provider.AdviseAsync(NvidiaProvider.DefaultEndpoint,"fixture-key","actual/model","report",[new("safe.action","Safe","Optimize","","User","Low","None","test")],default);
        Assert.Equal("safe.action",advice.ActionIds.Single());Assert.DoesNotContain("fixture-key",advice.Explanation);
    }
    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{")]
    [InlineData("{\"data\":null}")]
    public async Task MalformedCatalogReturnsSanitizedProviderError(string body)
    {
        using var client=new HttpClient(new Handler(_=>Task.FromResult(Response(body))));
        var error=await Assert.ThrowsAsync<HbxException>(()=>new NvidiaProvider(client).ModelsAsync(NvidiaProvider.DefaultEndpoint,"fixture-key",default));
        Assert.Contains("HBX-AI-",error.Message);
    }
    [Fact]public async Task CatalogSkipsNonObjectsAndEchoedKey()
    {
        using var client=new HttpClient(new Handler(_=>Task.FromResult(Response("{\"data\":[null,42,{\"id\":\"fixture-key\"},{\"id\":\"valid/model\"}]}"))));
        var models=await new NvidiaProvider(client).ModelsAsync(NvidiaProvider.DefaultEndpoint,"fixture-key",default);
        Assert.Equal("valid/model",Assert.Single(models));
    }
    [Theory]
    [InlineData("null")]
    [InlineData("{")]
    [InlineData("{\"choices\":[null]}")]
    [InlineData("{\"choices\":[{\"message\":42}]}")]
    public async Task MalformedChatEnvelopeReturnsSanitizedProviderError(string body)
    {
        using var client=new HttpClient(new Handler(_=>Task.FromResult(Response(body))));
        await Assert.ThrowsAsync<HbxException>(()=>new NvidiaProvider(client).AdviseAsync(NvidiaProvider.DefaultEndpoint,"fixture-key","model","report",[],default));
    }
    [Fact]public async Task ChatRedactsKeyAfterJsonDecodingAndRejectsNonObjectPlanActions()
    {
        const string key="fixture+key";
        var content=Json.Write(new{explanation=key,action_ids=Array.Empty<string>()});
        var response=Json.Write(new{choices=new[]{new{message=new{content}}}});
        using var client=new HttpClient(new Handler(_=>Task.FromResult(Response(response))));
        var advice=await new NvidiaProvider(client).AdviseAsync(NvidiaProvider.DefaultEndpoint,key,"model","report",[],default);
        Assert.DoesNotContain(key,advice.Explanation);Assert.Empty(advice.ActionIds);
        using var arrayClient=new HttpClient(new Handler(_=>Task.FromResult(Response(Json.Write(new{choices=new[]{new{message=new{content="[]"}}}})))));
        Assert.Empty((await new NvidiaProvider(arrayClient).AdviseAsync(NvidiaProvider.DefaultEndpoint,key,"model","report",[],default)).ActionIds);
    }
    private sealed class StalledBody:MemoryStream
    {
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken cancellationToken=default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan,cancellationToken);
            return 0;
        }
    }
    [Fact]public async Task ProviderDeadlineCoversResponseBodyAfterHeaders()
    {
        using var client=new HttpClient(new Handler(_=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StreamContent(new StalledBody())}))){Timeout=TimeSpan.FromMilliseconds(100)};
        using var guard=new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var error=await Assert.ThrowsAsync<HbxException>(()=>new NvidiaProvider(client).ModelsAsync(NvidiaProvider.DefaultEndpoint,"fixture-key",guard.Token));
        Assert.Contains("HBX-AI-0005",error.Message);
        Assert.False(guard.IsCancellationRequested);
    }
    [Theory][InlineData(401)][InlineData(429)][InlineData(500)][InlineData(302)]
    public async Task ProviderErrorsNeverBecomeSuccessOrExposeResponseBody(int code)
    {
        var handler=new Handler(_=>Task.FromResult(Response("secret provider body",(HttpStatusCode)code)));using var client=new HttpClient(handler);
        var error=await Assert.ThrowsAsync<HbxException>(()=>new NvidiaProvider(client).ModelsAsync(NvidiaProvider.DefaultEndpoint,"fixture-key",default));
        Assert.DoesNotContain("secret provider body",error.Message);Assert.Contains(code.ToString(),error.Message);Assert.Equal(1,handler.Calls);
    }
    [Fact]public async Task EmptyChatResponseDoesNotPassConnectionTest()
    {
        using var client=new HttpClient(new Handler(_=>Task.FromResult(Response("{\"choices\":[{\"message\":{\"content\":\"\"}}]}"))));
        await Assert.ThrowsAsync<HbxException>(()=>new NvidiaProvider(client).AdviseAsync(NvidiaProvider.DefaultEndpoint,"fixture-key","model","test",[],default));
    }
    [Theory][InlineData("http://example.com/v1")][InlineData("https://user:password@example.com/v1")][InlineData("https://example.com/v1?key=secret")]
    public void UnsafeProviderEndpointsAreRejected(string endpoint)=>Assert.Throws<HbxException>(()=>NvidiaProvider.ValidateEndpoint(endpoint));
    [Theory][InlineData("https://evil.example/api/webhooks/123/token")][InlineData("http://discord.com/api/webhooks/123/token")][InlineData("https://discord.com.evil.example/api/webhooks/123/token")]
    public void WebhookCannotTargetArbitraryDestinations(string webhook)=>Assert.Throws<HbxException>(()=>Notifications.ValidateWebhook(webhook));
    [Fact]public async Task DisabledNotificationsNeverMakeNetworkRequests()
    {
        var handler=new Handler(_=>throw new Exception("Network must not be called"));using var client=new HttpClient(handler);
        var notifier=new Notifications(Store,client);Assert.Contains("disabled",await notifier.ReportAsync("Test","HBX-TEST",true,default));Assert.Equal(0,handler.Calls);Assert.Single(notifier.History);
    }
    [Fact]public async Task NotificationsSuppressMentionsAndEnforceLocalRateLimit()
    {
        var store=Store;store.Update("integration-secrets",new IntegrationSecrets(),s=>s with{DiscordWebhook="https://discord.com/api/webhooks/123456/fixture_token"});
        var handler=new Handler(async request=>
        {
            var body=await request.Content!.ReadAsStringAsync();Assert.Contains("allowed_mentions",body);Assert.DoesNotContain("fixture_token",body);Assert.DoesNotContain("@everyone",body);
            return Response("",HttpStatusCode.NoContent);
        });using var client=new HttpClient(handler);var notifier=new Notifications(store,client);notifier.SetEnabled(true);
        Assert.Equal("Delivered",await notifier.ReportAsync("@everyone","HBX-TEST",true,default));
        var restartedNotifier=new Notifications(Store,client);
        Assert.Contains("rate limited",await restartedNotifier.ReportAsync("Test","HBX-TEST",true,default));Assert.Equal(1,handler.Calls);
    }
    [Fact]public async Task WebhookConnectionErrorsAreActionableAndNeverEchoSecrets()
    {
        var store=Store;store.Update("integration-secrets",new IntegrationSecrets(),s=>s with{DiscordWebhook="https://discord.com/api/webhooks/123456/fixture_token"});
        var handler=new Handler(_=>throw new HttpRequestException("fixture_token leaked"));using var client=new HttpClient(handler);
        var notifier=new Notifications(store,client);notifier.SetEnabled(true);
        var result=await notifier.ReportAsync("Test","HBX-TEST",true,default);
        Assert.Contains("connection error",result);Assert.Contains(nameof(HttpRequestException),result);
        Assert.DoesNotContain("fixture_token",result);Assert.Equal(result,notifier.History.Single().Delivery);
    }
    [Fact]public async Task ProviderLongRateLimitFallsBackLocallyWithoutRetryStorm()
    {
        var store=Store;store.Update("integration-secrets",new IntegrationSecrets(),s=>s with{DiscordWebhook="https://discord.com/api/webhooks/123456/fixture_token"});
        var handler=new Handler(_=>{var response=Response("",HttpStatusCode.TooManyRequests);response.Headers.RetryAfter=new RetryConditionHeaderValue(TimeSpan.FromSeconds(120));return Task.FromResult(response);});
        using var client=new HttpClient(handler);var notifier=new Notifications(store,client);notifier.SetEnabled(true);
        Assert.Contains("provider rate limited",await notifier.ReportAsync("Test","HBX-TEST",true,default));Assert.Equal(1,handler.Calls);Assert.Single(notifier.History);
    }
    public void Dispose()
    {
        var allowed=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"HyperBoostX-tests"))+Path.DirectorySeparatorChar;
        if(Path.GetFullPath(_root).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(_root))Directory.Delete(_root,true);
    }
}

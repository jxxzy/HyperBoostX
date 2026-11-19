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
    [Theory]
    [InlineData("iCUE.exe",true)]
    [InlineData("NZXT CAM",true)]
    [InlineData("L-Connect 3",true)]
    [InlineData("AIDA64",false)]
    [InlineData("HWiNFO64",false)]
    public void LcdUtilitySignaturesIdentifyCoolingCriticalExclusions(string name,bool coolingCritical)=>Assert.Equal(coolingCritical,LcdPerformanceDetection.Classify(name)?.CoolingCritical);

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
        var coverage=Assert.Single(report.Facts,fact=>fact.Name=="Inventory coverage");Assert.Contains("records",coverage.Value,StringComparison.OrdinalIgnoreCase);
        Assert.True(report.Facts.Count<=2001);Assert.All(report.Facts,fact=>Assert.Contains(fact.Availability,new[]{"Available","Unavailable"}));
    }
    [Theory]
    [InlineData("iCUE.exe","Corsair")]
    [InlineData("LightingService","ASUS")]
    [InlineData("MSI Center SDK","MSI")]
    [InlineData("Razer Synapse Service","Razer")]
    [InlineData("OpenRGB","OpenRGB")]
    public void RgbVendorSignaturesClassifyKnownSoftware(string value,string vendor)=>Assert.Equal(vendor,VendorSoftwareDetection.ClassifyRgb(value)?.Vendor);

    [Fact]public void RgbVendorSignaturesDoNotGuessUnknownSoftware()=>Assert.Null(VendorSoftwareDetection.ClassifyRgb("ordinary-unrelated-application"));

    [Fact]public async Task RgbInventoryReturnsDirectObservationsOrExplicitProviderIssues()
    {
        var report=await VendorSoftwareDetection.ScanRgbAsync(default);
        Assert.Equal("RGB Software Detector",report.Center);Assert.NotEmpty(report.Facts);
        Assert.All(report.Facts,fact=>Assert.Contains(fact.Availability,new[]{"Available","Unavailable"}));
    }

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

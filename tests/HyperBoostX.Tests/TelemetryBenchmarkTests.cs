using System.IO;
using HyperBoostX.Core;
using HyperBoostX.Windows;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class TelemetryBenchmarkTests
{
    [Fact]public void ProcessCpuDeltaIsNormalizedToLogicalProcessorsAndClamped()
    {
        Assert.Equal(50d,Diagnostics.ProcessCpuPercent(TimeSpan.Zero,TimeSpan.FromMilliseconds(500),TimeSpan.FromSeconds(1),1));
        Assert.Equal(25d,Diagnostics.ProcessCpuPercent(TimeSpan.Zero,TimeSpan.FromMilliseconds(500),TimeSpan.FromSeconds(1),2));
        Assert.Equal(100d,Diagnostics.ProcessCpuPercent(TimeSpan.Zero,TimeSpan.FromSeconds(2),TimeSpan.FromSeconds(1),1));
        Assert.Null(Diagnostics.ProcessCpuPercent(TimeSpan.FromSeconds(1),TimeSpan.Zero,TimeSpan.FromSeconds(1),1));
        Assert.Null(Diagnostics.ProcessCpuPercent(TimeSpan.Zero,TimeSpan.FromSeconds(1),TimeSpan.Zero,1));
    }
    [Fact]public void CpuDeltaIncludesIdleInsideKernelTime()
    {
        Assert.Equal(50d,Telemetry.CpuUsage((10,30,20),(60,110,40)));
        Assert.Null(Telemetry.CpuUsage((10,30,20),(10,30,20)));
        Assert.Null(Telemetry.CpuUsage((10,30,20),(1,2,3)));
    }
    [Fact]public async Task WindowsTelemetryReportsMeasuredMemoryAndBoundedCpu()
    {
        var provider=new Telemetry();var first=provider.Sample();await Task.Delay(150);var second=provider.Sample();
        Assert.True(first.TotalMemory>0);Assert.True(first.AvailableMemory<=first.TotalMemory);Assert.InRange(second.MemoryPercent,0,100);
        if(second.CpuPercent is {} cpu){Assert.InRange(cpu,0,100);Assert.Equal(MeasurementAvailability.Available,second.CpuAvailability);}
        else Assert.NotEqual(MeasurementAvailability.Available,second.CpuAvailability);
        Assert.Equal(second.CpuAvailability==MeasurementAvailability.Unavailable,second.CpuWin32Error.HasValue);Assert.True(second.UptimeMilliseconds>0);
        if(second.ReceivedBytesPerSecond.HasValue||second.SentBytesPerSecond.HasValue)
        {
            var supportedNetworkStates=new[]{MeasurementAvailability.Available,MeasurementAvailability.Limited};
            Assert.Contains(second.NetworkAvailability,supportedNetworkStates);
        }
        else Assert.NotEqual(MeasurementAvailability.Available,second.NetworkAvailability);
    }
    [Fact]public void ResetBaselinePreventsPausedIntervalsFromBecomingStaleSamples()
    {
        var provider=new Telemetry();provider.Sample();provider.ResetBaseline();
        var resumed=provider.Sample();
        Assert.Null(resumed.CpuPercent);Assert.Equal(MeasurementAvailability.Sampling,resumed.CpuAvailability);
        Assert.Null(resumed.ReceivedBytesPerSecond);Assert.Null(resumed.SentBytesPerSecond);
        Assert.NotEqual(MeasurementAvailability.Available,resumed.NetworkAvailability);
    }
    [Fact]public async Task StorageBenchmarkUsesAndRemovesOnlyItsScratchFile()
    {
        var parent=Path.Combine(Path.GetTempPath(),"HyperBoostX-tests");var root=Path.Combine(parent,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            var marker=Path.Combine(root,"keep.txt");File.WriteAllText(marker,"Preserve");
            var report=await LocalBenchmarks.StorageAsync(root,default);Assert.Contains(report.Facts,f=>f.Name=="Bytes verified"&&f.Value=="67108864");
            Assert.Empty(Directory.GetFiles(Path.Combine(root,"BenchmarkScratch")));Assert.Equal("Preserve",File.ReadAllText(marker));
            using var cts=new CancellationTokenSource();cts.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>LocalBenchmarks.StorageAsync(root,cts.Token));
            Assert.Empty(Directory.GetFiles(Path.Combine(root,"BenchmarkScratch")));
        }
        finally{if(Path.GetFullPath(root).StartsWith(Path.GetFullPath(parent)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))Directory.Delete(root,true);}
    }
    [Fact]public void HtmlExportEscapesUntrustedContentAndRedactsTokens()
    {
        var report=new DiagnosticReport("<script>alert(1)</script>",DateTimeOffset.UtcNow,[new("<img onerror=evil>","token=secretvalue","<iframe>")],[]);
        var html=ReportExport.Html(report);Assert.DoesNotContain("<script>",html);Assert.DoesNotContain("<iframe>",html);Assert.DoesNotContain("secretvalue",html);Assert.Contains("&lt;script&gt;",html);
    }
}

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
    [Theory]
    [InlineData(90,50,false,true,false)]
    [InlineData(50,90,false,false,true)]
    [InlineData(50,50,true,false,false)]
    [InlineData(89,89,false,false,false)]
    public void MemoryPressureUsesPhysicalCommitAndWindowsSignalsIndependently(int physical,int commit,bool low,bool physicalHigh,bool commitHigh)
    {
        var sample=new TelemetrySample(DateTimeOffset.UnixEpoch,null,100,(ulong)(100-physical),null,null,0,"fixture")
        {CommitUsedBytes=(ulong)commit,CommitLimitBytes=100,CommitAvailability=MeasurementAvailability.Available,LowMemorySignaled=low};
        var pressure=MemoryPressureAssessment.FromSample(sample,90);
        Assert.Equal((double)physical,pressure.PhysicalPercent);
        Assert.Equal((double)commit,pressure.CommitPercent);
        Assert.Equal(physicalHigh,pressure.PhysicalHigh);
        Assert.Equal(commitHigh,pressure.CommitHigh);
        Assert.Equal(physicalHigh||commitHigh||low,pressure.ShouldAlert);
    }
    [Fact]public void MemoryPressureNeverTreatsInvalidOrUnavailableCountersAsValid()
    {
        var sample=new TelemetrySample(DateTimeOffset.UnixEpoch,null,100,101,null,null,0,"fixture")
        {CommitUsedBytes=100,CommitLimitBytes=100,CommitAvailability=MeasurementAvailability.Unavailable};
        var pressure=MemoryPressureAssessment.FromSample(sample,20);
        Assert.Null(pressure.PhysicalPercent);Assert.Null(pressure.CommitPercent);
        Assert.Null(pressure.WindowsLowMemory);Assert.False(pressure.ShouldAlert);Assert.Equal(70,pressure.ThresholdPercent);
        var invalid=MemoryPressureAssessment.FromSample(sample with{CommitAvailability=MeasurementAvailability.Available,CommitLimitBytes=99},100);
        Assert.Null(invalid.CommitPercent);Assert.Equal(95,invalid.ThresholdPercent);
    }
    [Fact]public void MemoryGuardRecordsSustainedPressureAndResetsOnGapOrNormalSample()
    {
        var tracker=new MemoryGuardEvidence();
        var sample=new TelemetrySample(DateTimeOffset.UnixEpoch,null,100,5,null,null,0,"fixture"){LowMemorySignaled=false};
        Assert.Null(tracker.Observe(sample,90));
        Assert.Null(tracker.Observe(sample with{ObservedAt=sample.ObservedAt.AddSeconds(2)},90));
        var report=tracker.Observe(sample with{ObservedAt=sample.ObservedAt.AddSeconds(4)},90);
        Assert.NotNull(report);
        Assert.Contains(report.Facts,f=>f.Name=="Consecutive pressure observations"&&f.Value=="3");
        Assert.Contains(report.Facts,f=>f.Name=="Commit charge usage"&&f.Availability=="Unavailable");
        Assert.Null(tracker.Observe(sample with{ObservedAt=sample.ObservedAt.AddSeconds(20)},90));
        Assert.Null(tracker.Observe(sample with{ObservedAt=sample.ObservedAt.AddSeconds(22),AvailableMemory=50},90));
        Assert.Null(tracker.Observe(sample with{ObservedAt=sample.ObservedAt.AddSeconds(24)},90));
    }
    [Fact]public void MemoryGuardRecordsWindowsLowMemorySignalImmediately()
    {
        var tracker=new MemoryGuardEvidence();
        var sample=new TelemetrySample(DateTimeOffset.UnixEpoch,null,100,60,null,null,0,"fixture"){LowMemorySignaled=true};
        var report=tracker.Observe(sample,90);
        Assert.NotNull(report);
        Assert.Contains(report.Facts,f=>f.Name=="Windows low-memory signal"&&f.Value=="True");
        Assert.Contains(report.Facts,f=>f.Name=="Consecutive pressure observations"&&f.Value=="1");
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
    [Fact]public void MemoryBenchmarkPreflightRejectsPressureAndUnknownSignals()
    {
        var sample=new TelemetrySample(DateTimeOffset.UnixEpoch,null,1024UL*1024*1024,LocalBenchmarks.MemoryMinimumAvailable,null,null,0,"fixture"){LowMemorySignaled=false};
        LocalBenchmarks.ValidateMemoryBenchmarkPreflight(sample);
        Assert.Throws<HbxException>(()=>LocalBenchmarks.ValidateMemoryBenchmarkPreflight(sample with{AvailableMemory=LocalBenchmarks.MemoryMinimumAvailable-1}));
        Assert.Throws<HbxException>(()=>LocalBenchmarks.ValidateMemoryBenchmarkPreflight(sample with{LowMemorySignaled=true}));
        Assert.Throws<HbxException>(()=>LocalBenchmarks.ValidateMemoryBenchmarkPreflight(sample with{LowMemorySignaled=null}));
        Assert.Throws<HbxException>(()=>LocalBenchmarks.ValidateMemoryBenchmarkPreflight(sample with{AvailableMemory=sample.TotalMemory+1}));
    }

    [Fact]public async Task MemoryBenchmarkPreCancelledDoesNotStart()
    {
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>LocalBenchmarks.MemoryAsync(new CancellationToken(true)));
    }

    [Fact]public async Task StorageVerificationRejectsSameLengthCorruptionAndTruncation()
    {
        byte[] block=[1,2,3,4];
        using var valid=new MemoryStream(new byte[]{1,2,3,4,1,2,3,4});
        await LocalBenchmarks.VerifyRepeatedBlocksAsync(valid,block,2,default);
        using var corrupt=new MemoryStream(new byte[]{1,2,3,4,1,9,3,4});
        await Assert.ThrowsAsync<HbxException>(()=>LocalBenchmarks.VerifyRepeatedBlocksAsync(corrupt,block,2,default));
        using var shortStream=new MemoryStream(new byte[]{1,2,3,4,1});
        await Assert.ThrowsAsync<HbxException>(()=>LocalBenchmarks.VerifyRepeatedBlocksAsync(shortStream,block,2,default));
        using var extra=new MemoryStream(new byte[]{1,2,3,4,1,2,3,4,9});
        await Assert.ThrowsAsync<HbxException>(()=>LocalBenchmarks.VerifyRepeatedBlocksAsync(extra,block,2,default));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>LocalBenchmarks.VerifyRepeatedBlocksAsync(valid,block,2,new CancellationToken(true)));
    }

    [Fact]public async Task StorageBenchmarkUsesAndRemovesOnlyItsScratchFile()
    {
        var parent=Path.Combine(Path.GetTempPath(),"HyperBoostX-tests");var root=Path.Combine(parent,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            var marker=Path.Combine(root,"keep.txt");File.WriteAllText(marker,"Preserve");
            var report=await LocalBenchmarks.StorageAsync(root,default);Assert.Contains(report.Facts,f=>f.Name=="Bytes verified"&&f.Value=="67108864");
            Assert.Contains(report.Facts,f=>f.Name=="Content verification"&&f.Availability=="Available");
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

using HyperBoostX.Windows;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class GpuCounterTests
{
    [Fact] public void LoadUsesBusiestPhysicalEngineNotSumAcrossEnginesOrAdapters()
    {
        var rows=new GpuCounters.EngineSample[]{
            new("pid_1_luid_0x0_0x1_phys_0_eng_0_engtype_3D",30),
            new("pid_2_luid_0x0_0x1_phys_0_eng_0_engtype_3D",25),
            new("pid_1_luid_0x0_0x1_phys_0_eng_1_engtype_Copy",40),
            new("pid_1_luid_0x0_0x2_phys_0_eng_0_engtype_3D",48)};
        var sample=GpuCounters.SummarizeLoad(rows,false,DateTimeOffset.UnixEpoch);
        Assert.Equal(55d,sample.Percent);Assert.Equal(3,sample.EngineCount);
        Assert.Equal(MeasurementAvailability.Available,sample.Availability);
    }
    [Fact] public void LoadReportsIncompleteDataAndDoesNotClampInvalidEngine()
    {
        var sample=GpuCounters.SummarizeLoad([
            new("pid_1_luid_0x0_0x1_phys_0_eng_0_engtype_3D",65),
            new("pid_2_luid_0x0_0x1_phys_0_eng_0_engtype_3D",65),
            new("pid_3_luid_0x0_0x1_phys_0_eng_1_engtype_Copy",0),
            new("unexpected_layout",99)],true,DateTimeOffset.UnixEpoch);
        Assert.Equal(0d,sample.Percent);Assert.Equal(1,sample.EngineCount);
        Assert.Equal(MeasurementAvailability.Limited,sample.Availability);Assert.True(sample.OmittedRows>0);
        var duplicate=GpuCounters.SummarizeLoad([
            new("pid_1_luid_0x0_0x1_phys_0_eng_0_engtype_3D",15),
            new("pid_1_luid_0x00_0x01_phys_0_eng_0_engtype_3D",20)],false,DateTimeOffset.UnixEpoch);
        Assert.Null(duplicate.Percent);Assert.Equal(MeasurementAvailability.Unavailable,duplicate.Availability);
    }
    [Fact] public async Task LiveLoadReturnsBoundedRealProviderDataOrExplicitUnavailable()
    {
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var sample=await GpuCounters.SampleLoadAsync(timeout.Token);
        Assert.True(sample.ObservedAt>DateTimeOffset.UtcNow.AddMinutes(-1));
        if(sample.Percent.HasValue){Assert.InRange(sample.Percent.Value,0,100);Assert.True(sample.EngineCount>0);}
        else Assert.Equal(MeasurementAvailability.Unavailable,sample.Availability);
    }
    [Fact] public void EngineSnapshotsPreserveZeroAndRejectInvalidOrAggregateSamples()
    {
        var facts=GpuCounters.DescribeEngines([
            new("pid_1_engine_3D",0),new("pid_2_engine_Copy",81),
            new("pid_3_engine_3D",101),new("pid_4_engine_Compute",null),
            new("_Total",90),new("pid_2_engine_Copy",42)]);
        Assert.Equal(4,facts.Count);
        Assert.Contains(facts,f=>f.Name.EndsWith("pid_1_engine_3D")&&f.Value=="0%"&&f.Availability=="Available");
        Assert.Contains(facts,f=>f.Name.EndsWith("pid_2_engine_Copy")&&f.Value=="81%");
        Assert.Equal(2,facts.Count(f=>f.Availability=="Unavailable"));
        Assert.DoesNotContain(facts,f=>f.Name.Contains("_Total"));
    }
    [Fact] public void MemoryCountersPreserveValuesAboveFourGiBAndMissingValues()
    {
        var facts=GpuCounters.DescribeMemory([new("adapter_0",24UL*1073741824,0),new("adapter_1",null,1073741824)]);
        Assert.Equal(4,facts.Count);
        Assert.Contains(facts,f=>f.Name.Contains("dedicated")&&f.Value.Contains("25769803776 bytes")&&f.Availability=="Available");
        Assert.Contains(facts,f=>f.Name.Contains("shared")&&f.Value.StartsWith("0 bytes")&&f.Availability=="Available");
        Assert.Contains(facts,f=>f.Name.Contains("dedicated")&&f.Name.EndsWith("adapter_1")&&f.Availability=="Unavailable");
    }
    [Fact] public void EmptyProviderDoesNotInventMetrics()
    {
        Assert.Equal("Unavailable",Assert.Single(GpuCounters.DescribeEngines([])).Availability);
        Assert.Equal("Unavailable",Assert.Single(GpuCounters.DescribeMemory([])).Availability);
    }
    [Fact] public void FormatterBoundsInputAndSkipsInvalidNames()
    {
        var rows=Enumerable.Range(0,600).Select(i=>new GpuCounters.EngineSample(i==0?"\n":$"instance_{i}",10));
        Assert.Equal(399,GpuCounters.DescribeEngines(rows).Count);
    }
    [Fact] public async Task GpuCenterReturnsCounterEvidenceOrExplicitUnavailable()
    {
        var report=await new Diagnostics().ScanAsync("GPU",default);
        Assert.Contains(report.Facts,f=>f.Source==GpuCounters.EngineClass);
        Assert.Contains(report.Facts,f=>f.Source==GpuCounters.MemoryClass);
        Assert.Contains(report.Facts,f=>f.Name=="GPU counter methodology");
        Assert.Contains(report.Facts,f=>f.Name=="VRAM capacity"&&f.Availability=="Unavailable");
    }
}

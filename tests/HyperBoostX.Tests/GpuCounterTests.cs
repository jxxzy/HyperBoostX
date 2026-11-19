using HyperBoostX.Windows;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class GpuCounterTests
{
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

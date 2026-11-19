using System.IO;
using HyperBoostX.Core;
using HyperBoostX.Windows;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class VisualEffectsActionTests:IDisposable
{
    private readonly string _root=Path.Combine(Path.GetTempPath(),"HyperBoostX-tests","visual-effects-"+Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task VisualEffectsActionUsesJournaledApplyReadBackAndRollback()
    {
        var provider=new FixtureVisualEffectsProvider(new State(true,"DWord","1"));
        var action=new VisualEffectsAction(provider);
        var engine=new ActionEngine([action],new Journal(_root,new TestProtector()),new SafetyGuard(()=>null),"fixture-owner");

        var preview=await engine.PreviewAsync(action.Info.Id,"",default);
        Assert.Equal(new State(true,"DWord","1"),preview.Before);
        Assert.Equal(new State(true,"DWord","2"),preview.Desired);

        var applied=await engine.ApplyAsync(preview.Token,true,"fixture-session",default);
        Assert.Equal("Verified",applied.Status);
        Assert.Equal(preview.Desired,provider.Current);

        var restored=await engine.RollbackAsync(applied.Id,true,default);
        Assert.Equal("RolledBack",restored.Status);
        Assert.Equal(preview.Before,provider.Current);
    }

    [Fact]
    public async Task VisualEffectsWriteWithoutMatchingReadBackIsNotReportedAsSuccess()
    {
        var provider=new FixtureVisualEffectsProvider(new State(true,"DWord","1")){IgnoreWrites=true};
        var action=new VisualEffectsAction(provider);
        var engine=new ActionEngine([action],new Journal(_root,new TestProtector()),new SafetyGuard(()=>null),"fixture-owner");
        var preview=await engine.PreviewAsync(action.Info.Id,"",default);

        var result=await engine.ApplyAsync(preview.Token,true,null,default);

        Assert.Equal("FailedRestored",result.Status);
        Assert.Contains("read-back",result.Message,StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new State(true,"DWord","1"),provider.Current);
    }

    [Theory]
    [InlineData(false,"DWord","2")]
    [InlineData(true,"String","2")]
    [InlineData(true,"DWord","4")]
    [InlineData(true,"DWord","invalid")]
    public async Task VisualEffectsActionRejectsMissingOrInvalidStoredPresets(bool exists,string kind,string value)
    {
        var action=new VisualEffectsAction(new FixtureVisualEffectsProvider(new State(exists,kind,value)));

        await Assert.ThrowsAsync<HbxException>(()=>action.ReadAsync("",default));
    }

    [Fact]
    public void VisualEffectsActionRejectsCustomTargets()
    {
        var action=new VisualEffectsAction(new FixtureVisualEffectsProvider(new State(true,"DWord","1")));

        Assert.Throws<HbxException>(()=>action.Validate(@"Software\arbitrary"));
    }

    [Fact]
    public async Task WindowsVisualEffectsProviderReadIsNonMutatingAndExplicitWhenUnavailable()
    {
        if(!OperatingSystem.IsWindows())return;

        try
        {
            var state=await new VisualEffectsAction().ReadAsync("",default);
            Assert.Equal(new State(true,"DWord",state.Value),state);
            Assert.InRange(int.Parse(state.Value,System.Globalization.CultureInfo.InvariantCulture),0,3);
        }
        catch(HbxException exception)
        {
            Assert.StartsWith("HBX-VFX-0003:",exception.Message,StringComparison.Ordinal);
        }
    }

    public void Dispose()
    {
        var root=Path.GetFullPath(_root);
        var parent=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"HyperBoostX-tests"))+Path.DirectorySeparatorChar;
        if(root.StartsWith(parent,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(root))Directory.Delete(root,true);
    }

    private sealed class TestProtector:IStateProtector
    {
        public byte[] Protect(byte[] value)=>value;
        public byte[] Unprotect(byte[] value)=>value;
    }

    private sealed class FixtureVisualEffectsProvider(State state):IVisualEffectsPreferenceProvider
    {
        public State Current=state;
        public bool IgnoreWrites{get;init;}
        public State Read()=>Current;
        public void Write(State value)
        {
            if(!IgnoreWrites)Current=value;
        }
    }
}

using System.IO;
using HyperBoostX.Core;
using HyperBoostX.Windows;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class DnsProfileActionTests:IDisposable
{
    private readonly string _root=Path.Combine(Path.GetTempPath(),"HyperBoostX-tests","dns-"+Guid.NewGuid().ToString("N"));

    [Fact]public void TargetContractAcceptsOnlyGuidAndCompiledPreset()
    {
        var id=Guid.NewGuid().ToString("D");Assert.Equal($"{id}|cloudflare",DnsProfileAction.Target(id,"cloudflare"));
        Assert.Throws<HbxException>(()=>DnsProfileAction.Target("Ethernet","cloudflare"));
        Assert.Throws<HbxException>(()=>DnsProfileAction.Target(id,"custom-command"));
        var action=new DnsProfileAction();Assert.True(action.Info.RequiresElevation);Assert.True(action.Info.Reversible);
        Assert.DoesNotContain("custom",DnsProfileAction.Presets);
    }

    [Fact]public async Task ExistingAdapterDnsStateIsReadOnlyAndRoundTripSerializable()
    {
        if(!OperatingSystem.IsWindows())return;var adapter=DnsProfileAction.Adapters().FirstOrDefault();if(adapter is null)return;
        var action=new DnsProfileAction();var target=DnsProfileAction.Target(adapter.Id,"automatic");var state=await action.ReadAsync(target,default);
        Assert.Equal("DnsIpv4StaticServers",state.Kind);var servers=Json.Read<string[]>(state.Value);Assert.Equal(state.Exists,servers.Length>0);
        Assert.All(servers,server=>Assert.True(System.Net.IPAddress.TryParse(server,out _)));
    }

    [Fact]public void StaticConfigurationParsingRejectsAmbiguousProfiles()
    {
        Assert.Empty(WindowsDnsProfileProvider.ParseStaticIpv4(null,null));
        Assert.Empty(WindowsDnsProfileProvider.ParseStaticIpv4("",null));
        Assert.Equal(new[]{"1.1.1.1","8.8.8.8"},WindowsDnsProfileProvider.ParseStaticIpv4("1.1.1.1, 8.8.8.8",null));
        Assert.Throws<HbxException>(()=>WindowsDnsProfileProvider.ParseStaticIpv4("8.8.8.8","10.0.0.5"));
        Assert.Throws<HbxException>(()=>WindowsDnsProfileProvider.ParseStaticIpv4("2606:4700:4700::1111",null));
        Assert.Throws<HbxException>(()=>WindowsDnsProfileProvider.ParseStaticIpv4("127.0.0.1",null));
        Assert.Throws<HbxException>(()=>WindowsDnsProfileProvider.ParseStaticIpv4("224.0.0.1",null));
    }

    [Fact]public async Task AutomaticDnsRestoresAbsenceOfStaticOverride()
    {
        var adapter=Guid.NewGuid().ToString("D");
        var provider=new FixtureDnsProfileProvider(adapter,[]);
        var action=new DnsProfileAction(provider);
        var journal=new Journal(_root,new TestProtector());
        var engine=new ActionEngine([action],journal,new SafetyGuard(()=>null),"fixture-owner");
        var preview=await engine.PreviewAsync(action.Info.Id,DnsProfileAction.Target(adapter,"cloudflare"),default);
        Assert.False(preview.Before.Exists);
        var change=await engine.ApplyAsync(preview.Token,true,null,default);
        Assert.Equal("Verified",change.Status);
        Assert.Equal("RolledBack",(await engine.RollbackAsync(change.Id,true,default)).Status);
        Assert.Empty(provider.Servers);
    }

    [Fact]public void OldEffectiveDnsSnapshotsCannotBeAutomaticallyRestored()
    {
        var adapter=Guid.NewGuid().ToString("D");
        var action=new DnsProfileAction(new FixtureDnsProfileProvider(adapter,["1.1.1.1"]));
        var state=new State(true,"DnsServers",Json.Write(new[]{"1.1.1.1"}));
        Assert.Throws<HbxException>(()=>action.ValidateRestore(DnsProfileAction.Target(adapter,"automatic"),state));
    }

    [Fact]public async Task DnsProfileApplyAndRollbackUseVerifiedEngineJournal()
    {
        var adapter=Guid.NewGuid().ToString("D");var provider=new FixtureDnsProfileProvider(adapter,["9.9.9.9"]);
        var action=new DnsProfileAction(provider);var journal=new Journal(_root,new TestProtector());
        var engine=new ActionEngine([action],journal,new SafetyGuard(()=>null),"fixture-owner");
        var target=DnsProfileAction.Target(adapter,"cloudflare");
        var preview=await engine.PreviewAsync(action.Info.Id,target,default);
        Assert.Equal(Json.Write(new[]{"9.9.9.9"}),preview.Before.Value);
        Assert.Equal(Json.Write(new[]{"1.1.1.1","1.0.0.1"}),preview.Desired.Value);

        var applied=await engine.ApplyAsync(preview.Token,true,"fixture-session",default);
        Assert.Equal("Verified",applied.Status);Assert.Equal(new[]{"1.1.1.1","1.0.0.1"},provider.Servers);
        var restored=await engine.RollbackAsync(applied.Id,true,default);
        Assert.Equal("RolledBack",restored.Status);Assert.Equal(new[]{"9.9.9.9"},provider.Servers);
    }

    [Fact]public async Task RejectedDnsProviderWriteDoesNotReturnVerifiedSuccess()
    {
        var adapter=Guid.NewGuid().ToString("D");var provider=new FixtureDnsProfileProvider(adapter,["9.9.9.9"]){ResultCode=64};
        var action=new DnsProfileAction(provider);var journal=new Journal(_root,new TestProtector());
        var engine=new ActionEngine([action],journal,new SafetyGuard(()=>null),"fixture-owner");
        var preview=await engine.PreviewAsync(action.Info.Id,DnsProfileAction.Target(adapter,"google"),default);
        var result=await engine.ApplyAsync(preview.Token,true,null,default);
        Assert.Equal("FailedRestored",result.Status);Assert.Contains("provider code 64",result.Message);
        Assert.Equal(new[]{"9.9.9.9"},provider.Servers);
    }

    [Fact]public void InvalidRestoreStateIsRejectedBeforeAnyProviderWrite()
    {
        var action=new DnsProfileAction();var method=typeof(DnsProfileAction).GetMethod("WriteAsync");Assert.NotNull(method);
        var invalid=new State(true,"DnsIpv4StaticServers",Json.Write(new[]{"127.0.0.1"}));
        var target=$"{Guid.NewGuid():D}|automatic";
        var error=Assert.Throws<HbxException>(()=>action.ValidateRestore(target,invalid));Assert.StartsWith("HBX-DNS-0004",error.Message);
    }

    public void Dispose()
    {
        var root=Path.GetFullPath(_root);var parent=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"HyperBoostX-tests"))+Path.DirectorySeparatorChar;
        if(root.StartsWith(parent,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(root))Directory.Delete(root,true);
    }

    private sealed class TestProtector:IStateProtector
    {
        public byte[] Protect(byte[] value)=>value;
        public byte[] Unprotect(byte[] value)=>value;
    }

    private sealed class FixtureDnsProfileProvider(string adapterId,string[] servers):IDnsProfileProvider
    {
        public string[] Servers=servers;
        public uint ResultCode {get;set;}
        public IReadOnlyList<DnsAdapterOption> Adapters()=>[new(adapterId,"Isolated fixture adapter")];
        public string[] ReadDnsServers(string adapter)=>string.Equals(adapter,adapterId,StringComparison.OrdinalIgnoreCase)?Servers.ToArray():throw new HbxException("Fixture adapter unavailable.");
        public uint SetDnsServers(string adapter,string[] value)
        {
            if(!string.Equals(adapter,adapterId,StringComparison.OrdinalIgnoreCase))throw new HbxException("Fixture adapter unavailable.");
            if(ResultCode==0)Servers=value.ToArray();
            return ResultCode;
        }
    }
}

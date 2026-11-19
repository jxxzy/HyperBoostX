using HyperBoostX.Windows;
using Microsoft.Win32;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class PrivacyDiagnosticsTests
{
    [Fact]public void PolicyAuditDistinguishesUnsetDeniedInvalidAndUnreadable()
    {
        var facts=PrivacyDiagnostics.Inspect(default,(_,name)=>name switch
        {
            "LetAppsAccessCamera"=>new(true,true,2,RegistryValueKind.DWord),
            "LetAppsAccessMicrophone"=>new(true,false,null,null),
            "LetAppsAccessLocation"=>new(false,false,null,null),
            _=>new(true,true,"3",RegistryValueKind.String)
        });
        Assert.Equal("2 · Force deny",facts.Single(f=>f.Name=="Camera app access policy").Value);
        Assert.Equal("Not configured in this policy location",facts.Single(f=>f.Name=="Microphone app access policy").Value);
        Assert.Equal("Unavailable",facts.Single(f=>f.Name=="Location app access policy").Availability);
        Assert.Equal("Limited",facts.Single(f=>f.Name=="Diagnostic data policy").Availability);
        Assert.Contains(facts,f=>f.Name=="Privacy audit scope"&&f.Value.Contains("Effective app permissions",StringComparison.Ordinal));
    }
    [Fact]public void PolicyAuditHonorsCancellationBeforeProviderReads()
    {
        using var cts=new CancellationTokenSource();cts.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(()=>PrivacyDiagnostics.Inspect(cts.Token,(_,_)=>throw new InvalidOperationException("Provider must not be read")));
    }
}

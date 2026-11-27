using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Principal;
using System.Security.Cryptography;
using HyperBoostX.Core;
using HyperBoostX.Windows;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class PrivilegedPipeIdentityTests
{
    [Fact]public async Task ConnectedClientIsBoundToProcessSessionAndWindowsSid()
    {
        if(!OperatingSystem.IsWindows())return;
        var pipeName=$"HyperBoostX.Tests.{Guid.NewGuid():N}";
        await using var server=new NamedPipeServerStream(pipeName,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
        await using var client=new NamedPipeClientStream(".",pipeName,PipeDirection.InOut,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly,TokenImpersonationLevel.Impersonation);
        var waiting=server.WaitForConnectionAsync();await client.ConnectAsync(5000);await waiting;

        var actual=PrivilegedPipeIdentity.ReadConnectedClient(server);
        using var current=WindowsIdentity.GetCurrent();using var process=Process.GetCurrentProcess();
        Assert.Equal((uint)Environment.ProcessId,actual.ProcessId);
        Assert.Equal((uint)process.SessionId,actual.SessionId);
        Assert.Equal(current.User?.Value,actual.UserSid);
        Assert.Equal(current.Name,actual.Name);
    }

    [Fact]public async Task ExplicitUserAclPipeAcceptsAuthorizedLocalClient()
    {
        if(!OperatingSystem.IsWindows())return;
        using var current=WindowsIdentity.GetCurrent();var sid=Assert.IsType<SecurityIdentifier>(current.User);
        var pipeName=$"HyperBoostX.Tests.Acl.{Guid.NewGuid():N}";
        await using var server=PrivilegedPipeFactory.CreateServer(pipeName,sid);
        await using var client=new NamedPipeClientStream(".",pipeName,PipeDirection.InOut,PipeOptions.Asynchronous,TokenImpersonationLevel.Impersonation);
        var waiting=server.WaitForConnectionAsync();await client.ConnectAsync(5000);await waiting;
        Assert.Equal(sid.Value,PrivilegedPipeIdentity.ReadConnectedClient(server).UserSid);
    }

    [Fact]public async Task EndpointAuthenticatesPeerAndRequestBeforeDispatchAndSignsResponse()
    {
        if(!OperatingSystem.IsWindows())return;
        using var current=WindowsIdentity.GetCurrent();using var process=Process.GetCurrentProcess();var sid=Assert.IsType<SecurityIdentifier>(current.User);
        var secret=RandomNumberGenerator.GetBytes(32);var pipeName=$"HyperBoostX.Tests.Endpoint.{Guid.NewGuid():N}";var dispatched=0;
        await using var server=PrivilegedPipeFactory.CreateServer(pipeName,sid);
        await using var client=new NamedPipeClientStream(".",pipeName,PipeDirection.InOut,PipeOptions.Asynchronous,TokenImpersonationLevel.Impersonation);
        var waiting=server.WaitForConnectionAsync();await client.ConnectAsync(5000);await waiting;
        using var endpoint=new PrivilegedPipeEndpoint(secret,new Dictionary<string,Func<string,string,bool>>{{"dns.set",(target,body)=>target=="Ethernet"&&body=="{}"}});
        var request=new PrivilegedRequestEnvelope(1,Guid.NewGuid().ToString("D"),"dns.set","Ethernet","{}",Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),DateTimeOffset.UtcNow.ToUnixTimeSeconds(),process.SessionId,sid.Value,"");
        request=request with{AuthenticationTag=PrivilegedRequestValidator.Sign(request,secret)};
        var serving=endpoint.ServeOnceAsync(server,(uint)Environment.ProcessId,sid.Value,(uint)process.SessionId,(received,_)=>
        {dispatched++;return Task.FromResult(new PrivilegedDispatchResult(true,"HBX-IPC-APPLIED",received.ActionId));},TimeSpan.FromSeconds(5),default);
        await PrivilegedFrameCodec.WriteAsync(client,request,default);var response=await PrivilegedFrameCodec.ReadResponseAsync(client,default);await serving;
        Assert.Equal(1,dispatched);Assert.True(response.Success);Assert.True(PrivilegedResponseAuthenticator.Validate(response,request.RequestId,request.Nonce,secret).Accepted);
    }

    [Fact]public async Task EndpointRejectsTamperedRequestWithoutDispatch()
    {
        if(!OperatingSystem.IsWindows())return;
        using var current=WindowsIdentity.GetCurrent();using var process=Process.GetCurrentProcess();var sid=Assert.IsType<SecurityIdentifier>(current.User);
        var secret=RandomNumberGenerator.GetBytes(32);var pipeName=$"HyperBoostX.Tests.Reject.{Guid.NewGuid():N}";var dispatched=false;
        await using var server=PrivilegedPipeFactory.CreateServer(pipeName,sid);
        await using var client=new NamedPipeClientStream(".",pipeName,PipeDirection.InOut,PipeOptions.Asynchronous,TokenImpersonationLevel.Impersonation);
        var waiting=server.WaitForConnectionAsync();await client.ConnectAsync(5000);await waiting;
        using var endpoint=new PrivilegedPipeEndpoint(secret,new Dictionary<string,Func<string,string,bool>>{{"dns.set",(_,_)=>true}});
        var request=new PrivilegedRequestEnvelope(1,Guid.NewGuid().ToString("D"),"dns.set","Ethernet","{}",Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),DateTimeOffset.UtcNow.ToUnixTimeSeconds(),process.SessionId,sid.Value,"invalid");
        var serving=endpoint.ServeOnceAsync(server,(uint)Environment.ProcessId,sid.Value,(uint)process.SessionId,(_,_)=>{dispatched=true;return Task.FromResult(new PrivilegedDispatchResult(true,"bad","bad"));},TimeSpan.FromSeconds(5),default);
        await PrivilegedFrameCodec.WriteAsync(client,request,default);var response=await PrivilegedFrameCodec.ReadResponseAsync(client,default);await serving;
        Assert.False(dispatched);Assert.False(response.Success);Assert.Equal("HBX-IPC-AUTH",response.Code);Assert.True(PrivilegedResponseAuthenticator.Validate(response,request.RequestId,request.Nonce,secret).Accepted);
    }

    [Fact]public async Task EndpointDoesNotExposeDispatcherExceptions()
    {
        if(!OperatingSystem.IsWindows())return;
        using var current=WindowsIdentity.GetCurrent();using var process=Process.GetCurrentProcess();var sid=Assert.IsType<SecurityIdentifier>(current.User);
        var secret=RandomNumberGenerator.GetBytes(32);var pipeName=$"HyperBoostX.Tests.Error.{Guid.NewGuid():N}";
        await using var server=PrivilegedPipeFactory.CreateServer(pipeName,sid);await using var client=new NamedPipeClientStream(".",pipeName,PipeDirection.InOut,PipeOptions.Asynchronous,TokenImpersonationLevel.Impersonation);
        var waiting=server.WaitForConnectionAsync();await client.ConnectAsync(5000);await waiting;
        using var endpoint=new PrivilegedPipeEndpoint(secret,new Dictionary<string,Func<string,string,bool>>{{"dns.set",(_,_)=>true}});
        var request=new PrivilegedRequestEnvelope(1,Guid.NewGuid().ToString("D"),"dns.set","Ethernet","{}",Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),DateTimeOffset.UtcNow.ToUnixTimeSeconds(),process.SessionId,sid.Value,"");
        request=request with{AuthenticationTag=PrivilegedRequestValidator.Sign(request,secret)};
        var serving=endpoint.ServeOnceAsync(server,(uint)Environment.ProcessId,sid.Value,(uint)process.SessionId,(_,_)=>throw new InvalidOperationException("secret internal detail"),TimeSpan.FromSeconds(5),default);
        await PrivilegedFrameCodec.WriteAsync(client,request,default);var response=await PrivilegedFrameCodec.ReadResponseAsync(client,default);await serving;
        Assert.Equal("HBX-IPC-DISPATCH",response.Code);Assert.DoesNotContain("secret internal detail",response.Message,StringComparison.Ordinal);
    }

    [Fact]public async Task EndpointRejectsLateSuccessAndSignsUncertainTimeout()
    {
        if(!OperatingSystem.IsWindows())return;
        using var current=WindowsIdentity.GetCurrent();using var process=Process.GetCurrentProcess();var sid=Assert.IsType<SecurityIdentifier>(current.User);
        var secret=RandomNumberGenerator.GetBytes(32);var pipeName=$"HyperBoostX.Tests.Timeout.{Guid.NewGuid():N}";
        await using var server=PrivilegedPipeFactory.CreateServer(pipeName,sid);
        await using var client=new NamedPipeClientStream(".",pipeName,PipeDirection.InOut,PipeOptions.Asynchronous,TokenImpersonationLevel.Impersonation);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var waiting=server.WaitForConnectionAsync(deadline.Token);await client.ConnectAsync(deadline.Token);await waiting;
        using var endpoint=new PrivilegedPipeEndpoint(secret,new Dictionary<string,Func<string,string,bool>>{{"test.read",(_,_)=>true}});
        var request=new PrivilegedRequestEnvelope(1,Guid.NewGuid().ToString("D"),"test.read","","{}",Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),DateTimeOffset.UtcNow.ToUnixTimeSeconds(),process.SessionId,sid.Value,"");
        request=request with{AuthenticationTag=PrivilegedRequestValidator.Sign(request,secret)};
        var serving=endpoint.ServeOnceAsync(server,(uint)Environment.ProcessId,sid.Value,(uint)process.SessionId,async (_,operationToken)=>
        {
            try{await Task.Delay(Timeout.InfiniteTimeSpan,operationToken);}catch(OperationCanceledException) when(operationToken.IsCancellationRequested){}
            return new PrivilegedDispatchResult(true,"late","late success");
        },TimeSpan.FromMilliseconds(100),deadline.Token,frameReadTimeout:TimeSpan.FromSeconds(3));
        await PrivilegedFrameCodec.WriteAsync(client,request,deadline.Token);var response=await PrivilegedFrameCodec.ReadResponseAsync(client,deadline.Token);await serving;
        Assert.False(response.Success);Assert.Equal("HBX-IPC-TIMEOUT",response.Code);Assert.Contains("recovery history",response.Message);
        Assert.True(PrivilegedResponseAuthenticator.Validate(response,request.RequestId,request.Nonce,secret).Accepted);
    }

    [Fact]public async Task VerifiedPeersDeriveTheSameEphemeralSessionKey()
    {
        if(!OperatingSystem.IsWindows())return;
        using var current=WindowsIdentity.GetCurrent();using var process=Process.GetCurrentProcess();var sid=Assert.IsType<SecurityIdentifier>(current.User);
        var pipeName=$"HyperBoostX.Tests.Bootstrap.{Guid.NewGuid():N}";await using var server=PrivilegedPipeFactory.CreateServer(pipeName,sid);
        await using var client=new NamedPipeClientStream(".",pipeName,PipeDirection.InOut,PipeOptions.Asynchronous,TokenImpersonationLevel.Impersonation);
        var waiting=server.WaitForConnectionAsync();await client.ConnectAsync(5000);await waiting;
        var serverKeyTask=PrivilegedBootstrap.EstablishServerAsync(server,(uint)Environment.ProcessId,sid.Value,(uint)process.SessionId,pipeName,default);
        var clientKeyTask=PrivilegedBootstrap.EstablishClientAsync(client,(uint)Environment.ProcessId,(uint)process.SessionId,pipeName,default);
        var keys=await Task.WhenAll(serverKeyTask,clientKeyTask);
        Assert.Equal(32,keys[0].Length);Assert.Equal(keys[0],keys[1]);Assert.Contains(keys[0],value=>value!=0);
        CryptographicOperations.ZeroMemory(keys[0]);CryptographicOperations.ZeroMemory(keys[1]);
    }

    [Fact]public async Task BootstrapRejectsUnexpectedProcessBeforeKeyExchange()
    {
        if(!OperatingSystem.IsWindows())return;
        using var current=WindowsIdentity.GetCurrent();using var process=Process.GetCurrentProcess();var sid=Assert.IsType<SecurityIdentifier>(current.User);
        var pipeName=$"HyperBoostX.Tests.PeerReject.{Guid.NewGuid():N}";await using var server=PrivilegedPipeFactory.CreateServer(pipeName,sid);
        await using var client=new NamedPipeClientStream(".",pipeName,PipeDirection.InOut,PipeOptions.Asynchronous,TokenImpersonationLevel.Impersonation);
        var waiting=server.WaitForConnectionAsync();await client.ConnectAsync(5000);await waiting;
        var wrongPid=unchecked((uint)Environment.ProcessId+1);
        var clientError=await Assert.ThrowsAsync<HbxException>(()=>PrivilegedBootstrap.EstablishClientAsync(client,wrongPid,(uint)process.SessionId,pipeName,default));
        Assert.StartsWith("HBX-IPC-BOOTSTRAP-PEER",clientError.Message);
        var serverError=await Assert.ThrowsAsync<HbxException>(()=>PrivilegedBootstrap.EstablishServerAsync(server,wrongPid,sid.Value,(uint)process.SessionId,pipeName,default));
        Assert.StartsWith("HBX-IPC-BOOTSTRAP-PEER",serverError.Message);
    }
}

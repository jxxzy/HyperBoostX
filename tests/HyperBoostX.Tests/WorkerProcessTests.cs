using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json;
using HyperBoostX.Core;
using HyperBoostX.Windows;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class WorkerProcessTests
{
    [Fact]public async Task PrivilegedWorkerEngineContainsOnlyElevationRequiredActions()
    {
        var root=Path.Combine(Path.GetTempPath(),"HyperBoostX-tests","worker-engine-"+Guid.NewGuid().ToString("N"));
        try
        {
            var runtime=new Runtime(root);
            var actions=runtime.CreatePrivilegedWorkerEngine().Actions;
            Assert.NotEmpty(actions);
            Assert.All(actions,action=>Assert.True(action.RequiresElevation));
            Assert.Equal(new[]{"network.dns.set_profile"},actions.Select(action=>action.Id));
            Assert.DoesNotContain(actions,action=>action.Id.StartsWith("windows.visual.",StringComparison.Ordinal));
            Assert.DoesNotContain(actions,action=>action.Id.StartsWith("gaming.",StringComparison.Ordinal));
            var dispatcher=new PrivilegedActionDispatcher(runtime.CreatePrivilegedWorkerEngine());
            using var identity=WindowsIdentity.GetCurrent();
            var secret=RandomNumberGenerator.GetBytes(32);
            try
            {
                var request=CreateRequest("action.preview",secret,Process.GetCurrentProcess().SessionId,identity.User?.Value??throw new InvalidOperationException("Current SID unavailable."))
                    with{Body=JsonSerializer.Serialize(new WorkerPreviewRequest("windows.visual.transparency.disable","")),AuthenticationTag=""};
                request=request with{AuthenticationTag=PrivilegedRequestValidator.Sign(request,secret)};
                var result=await dispatcher.DispatchAsync(request,default);
                Assert.False(result.Success);
                Assert.Contains("not registered",result.Message,StringComparison.OrdinalIgnoreCase);
            }
            finally{CryptographicOperations.ZeroMemory(secret);}
        }
        finally
        {
            if(Directory.Exists(root))Directory.Delete(root,true);
        }
    }

    [Fact]public async Task WorkerPowerInspectUsesLiveWindowsStateAndRejectsUnexpectedTarget()
    {
        if(!OperatingSystem.IsWindows())return;
        await using var client=await PrivilegedWorkerClient.StartAsync(FindWorker(),false,default);
        var response=await client.SendAsync("worker.power.inspect","","{}",default);
        Assert.True(response.Success,response.Message);
        Assert.Equal("HBX-WORKER-POWER",response.Code);
        using var document=JsonDocument.Parse(response.Body);
        var activeId=document.RootElement.GetProperty("ActivePlanId").GetString();
        Assert.True(Guid.TryParse(activeId,out _));
        Assert.True(document.RootElement.GetProperty("PlanCount").GetInt32()>0);
        Assert.True(document.RootElement.GetProperty("ActivePlanEnumerated").GetBoolean());
        Assert.Equal(PowerAction.Current(),activeId);
        var rejected=await client.SendAsync("worker.power.inspect","unexpected","{}",default);
        Assert.False(rejected.Success);
        Assert.Equal("HBX-IPC-PAYLOAD",rejected.Code);
    }

    [Fact]public async Task ProductionClientOwnsWorkerHealthAndShutdownLifecycle()
    {
        if(!OperatingSystem.IsWindows())return;
        await using var client=await PrivilegedWorkerClient.StartAsync(FindWorker(),false,default);
        var response=await client.SendAsync("worker.health","","{}",default);
        Assert.True(response.Success);Assert.Equal("HBX-WORKER-HEALTH",response.Code);
        using var body=JsonDocument.Parse(response.Body);Assert.Equal(client.ProcessId,body.RootElement.GetProperty("ProcessId").GetInt32());
        var denied=await client.SendAsync("action.preview","","{\"ActionId\":\"fixture\",\"Target\":\"\",\"Command\":\"whoami\"}",default);
        Assert.False(denied.Success);Assert.Equal("HBX-IPC-PAYLOAD",denied.Code);
    }

    [Fact]public async Task ConcurrentDisposeDrainsInFlightExchangeAndRejectsNewRequests()
    {
        if(!OperatingSystem.IsWindows())return;
        await using var client=await PrivilegedWorkerClient.StartAsync(FindWorker(),false,default);
        var inFlight=client.SendAsync("worker.health","","{}",default);
        var first=client.DisposeAsync().AsTask();
        var second=client.DisposeAsync().AsTask();
        await Assert.ThrowsAsync<ObjectDisposedException>(()=>client.SendAsync("worker.health","","{}",default));
        Assert.True((await inFlight.WaitAsync(TimeSpan.FromSeconds(20))).Success);
        await Task.WhenAll(first,second).WaitAsync(TimeSpan.FromSeconds(25));
        await client.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(()=>client.SendAsync("worker.health","","{}",default));
    }

    [Fact]public async Task ConcurrentHealthBurstStaysBoundedAndDoesNotPoisonConnection()
    {
        if(!OperatingSystem.IsWindows())return;
        await using var client=await PrivilegedWorkerClient.StartAsync(FindWorker(),false,default);
        async Task<string> Health()
        {
            try
            {
                var response=await client.SendAsync("worker.health","","{}",default);
                Assert.True(response.Success,response.Message);return "success";
            }
            catch(HbxException error) when(error.Message.StartsWith("HBX-WORKER-BUSY:",StringComparison.Ordinal))
            {return "busy";}
        }
        var requests=Enumerable.Range(0,PrivilegedWorkerClient.MaximumPendingRequests*4).Select(_=>Health()).ToArray();
        var results=await Task.WhenAll(requests).WaitAsync(TimeSpan.FromSeconds(35));
        Assert.Contains("success",results);
        Assert.All(results,result=>Assert.Contains(result,new[]{"success","busy"}));
        Assert.True((await client.SendAsync("worker.health","","{}",default)).Success);
    }

    [Fact]public async Task SaturatedWorkerQueueRejectsOverloadAndRecoversAfterCancellation()
    {
        if(!OperatingSystem.IsWindows())return;
        await using var client=await PrivilegedWorkerClient.StartAsync(FindWorker(),false,default);
        var field=typeof(PrivilegedWorkerClient).GetField("_requestLock",BindingFlags.Instance|BindingFlags.NonPublic);
        var gate=Assert.IsType<SemaphoreSlim>(field?.GetValue(client));
        await gate.WaitAsync();
        var cancellation=new CancellationTokenSource();
        Task<PrivilegedResponseEnvelope>[] queued=Array.Empty<Task<PrivilegedResponseEnvelope>>();
        try
        {
            queued=Enumerable.Range(0,PrivilegedWorkerClient.MaximumPendingRequests)
                .Select(_=>client.SendAsync("worker.health","","{}",cancellation.Token)).ToArray();
            // Each queued call must have reserved admission without writing to the pipe.
            Assert.All(queued,request=>Assert.False(request.IsCompleted));
            var rejected=await Assert.ThrowsAsync<HbxException>(
                ()=>client.SendAsync("worker.health","","{}",default));
            Assert.StartsWith("HBX-WORKER-BUSY:",rejected.Message,StringComparison.Ordinal);
            cancellation.Cancel();
            foreach(var request in queued)
                await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>request);
        }
        finally
        {
            cancellation.Cancel();
            gate.Release();
            cancellation.Dispose();
        }
        Assert.True((await client.SendAsync("worker.health","","{}",default)).Success);
    }

    [Fact]public async Task CancelledBeforeSendDoesNotPoisonWorkerConnection()
    {
        if(!OperatingSystem.IsWindows())return;
        await using var client=await PrivilegedWorkerClient.StartAsync(FindWorker(),false,default);
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>client.SendAsync("worker.health","","{}",cancelled.Token));
        Assert.True((await client.SendAsync("worker.health","","{}",default)).Success);
        var denied=await client.SendAsync("unregistered.action","","{}",default);
        Assert.False(denied.Success);Assert.Equal("HBX-IPC-DENIED",denied.Code);
        Assert.True((await client.SendAsync("worker.health","","{}",default)).Success);
    }

    [Fact]public async Task SeparateWorkerAuthenticatesHealthAndShutsDownCleanly()
    {
        if(!OperatingSystem.IsWindows())return;
        var workerPath=FindWorker();using var identity=WindowsIdentity.GetCurrent();using var currentProcess=Process.GetCurrentProcess();var sid=Assert.IsType<SecurityIdentifier>(identity.User);
        var pipeName=$"HyperBoostX.Tests.Worker.{Guid.NewGuid():N}";
        using var worker=new Process{StartInfo=new ProcessStartInfo(workerPath){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true}};
        worker.StartInfo.ArgumentList.Add("--pipe");worker.StartInfo.ArgumentList.Add(pipeName);
        worker.StartInfo.ArgumentList.Add("--client-pid");worker.StartInfo.ArgumentList.Add(Environment.ProcessId.ToString());
        worker.StartInfo.ArgumentList.Add("--client-session");worker.StartInfo.ArgumentList.Add(currentProcess.SessionId.ToString());
        worker.StartInfo.ArgumentList.Add("--client-sid");worker.StartInfo.ArgumentList.Add(sid.Value);
        Assert.True(worker.Start());byte[]? sessionKey=null;
        try
        {
            await using var client=new NamedPipeClientStream(".",pipeName,PipeDirection.InOut,PipeOptions.Asynchronous,TokenImpersonationLevel.Impersonation);
            await client.ConnectAsync(5000);sessionKey=await PrivilegedBootstrap.EstablishClientAsync(client,(uint)worker.Id,(uint)worker.SessionId,pipeName,default);
            var health=CreateRequest("worker.health",sessionKey,currentProcess.SessionId,sid.Value);
            await PrivilegedFrameCodec.WriteAsync(client,health,default);var healthResponse=await PrivilegedFrameCodec.ReadResponseAsync(client,default);
            Assert.True(PrivilegedResponseAuthenticator.Validate(healthResponse,health.RequestId,health.Nonce,sessionKey).Accepted);Assert.True(healthResponse.Success);Assert.Equal("HBX-WORKER-HEALTH",healthResponse.Code);
            using(var body=JsonDocument.Parse(healthResponse.Body)){Assert.Equal(worker.Id,body.RootElement.GetProperty("ProcessId").GetInt32());Assert.Equal(sid.Value,body.RootElement.GetProperty("UserSid").GetString());}
            var shutdown=CreateRequest("worker.shutdown",sessionKey,currentProcess.SessionId,sid.Value);
            await PrivilegedFrameCodec.WriteAsync(client,shutdown,default);var shutdownResponse=await PrivilegedFrameCodec.ReadResponseAsync(client,default);
            Assert.True(PrivilegedResponseAuthenticator.Validate(shutdownResponse,shutdown.RequestId,shutdown.Nonce,sessionKey).Accepted);Assert.Equal("HBX-WORKER-SHUTDOWN",shutdownResponse.Code);
            await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));Assert.Equal(0,worker.ExitCode);
        }
        finally
        {
            if(sessionKey is not null)CryptographicOperations.ZeroMemory(sessionKey);
            if(!worker.HasExited)worker.Kill(true);
        }
    }

    private static PrivilegedRequestEnvelope CreateRequest(string actionId,byte[] secret,int sessionId,string sid)
    {
        var request=new PrivilegedRequestEnvelope(1,Guid.NewGuid().ToString("D"),actionId,"","{}",Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),DateTimeOffset.UtcNow.ToUnixTimeSeconds(),sessionId,sid,"");
        return request with{AuthenticationTag=PrivilegedRequestValidator.Sign(request,secret)};
    }

    private static string FindWorker()
    {
        for(var directory=new DirectoryInfo(AppContext.BaseDirectory);directory is not null;directory=directory.Parent)
        {
            var candidate=Path.Combine(directory.FullName,"src","HyperBoostX.Worker","bin","Release","net8.0-windows","HyperBoostX.Worker.exe");
            if(File.Exists(candidate))return candidate;
        }
        throw new FileNotFoundException("Release worker executable was not built.");
    }
}

using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json;
using HyperBoostX.Core;
using HyperBoostX.Windows;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class WorkerProcessTests
{
    [Fact]public async Task ProductionClientOwnsWorkerHealthAndShutdownLifecycle()
    {
        if(!OperatingSystem.IsWindows())return;
        await using var client=await PrivilegedWorkerClient.StartAsync(FindWorker(),false,default);
        var response=await client.SendAsync("worker.health","","{}",default);
        Assert.True(response.Success);Assert.Equal("HBX-WORKER-HEALTH",response.Code);
        using var body=JsonDocument.Parse(response.Body);Assert.Equal(client.ProcessId,body.RootElement.GetProperty("ProcessId").GetInt32());
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

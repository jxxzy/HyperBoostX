using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public sealed class PrivilegedWorkerClient:IAsyncDisposable
{
    private readonly Process _process;
    private readonly NamedPipeClientStream _pipe;
    private readonly byte[] _sessionKey;
    private readonly int _sessionId;
    private readonly string _userSid;
    private readonly SemaphoreSlim _requestLock=new(1,1);
    private int _closing;
    private readonly TaskCompletionSource<bool> _closed=new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _faulted;
    private static readonly TimeSpan RequestTimeout=TimeSpan.FromSeconds(15);

    private PrivilegedWorkerClient(Process process,NamedPipeClientStream pipe,byte[] sessionKey,int sessionId,string userSid)
    {_process=process;_pipe=pipe;_sessionKey=sessionKey;_sessionId=sessionId;_userSid=userSid;}

    public int ProcessId=>_process.Id;
    public bool HasExited=>_process.HasExited;

    public static async Task<PrivilegedWorkerClient> StartAsync(string workerPath,bool requestElevation,CancellationToken token)
    {
        if(!OperatingSystem.IsWindows())throw new PlatformNotSupportedException("HyperBoostX Worker requires Windows.");
        var fullPath=Path.GetFullPath(workerPath??"");
        if(!File.Exists(fullPath)||!string.Equals(Path.GetFileName(fullPath),"HyperBoostX.Worker.exe",StringComparison.OrdinalIgnoreCase))
            throw new HbxException("HBX-WORKER-PATH: The packaged worker executable is missing or invalid.");
        using var identity=WindowsIdentity.GetCurrent();var sid=identity.User?.Value??throw new HbxException("HBX-WORKER-IDENTITY: Current Windows SID is unavailable.");
        using var current=Process.GetCurrentProcess();var sessionId=current.SessionId;var pipeName=$"HyperBoostX.Worker.{Guid.NewGuid():N}";
        var start=new ProcessStartInfo(fullPath){UseShellExecute=requestElevation,WindowStyle=ProcessWindowStyle.Hidden};
        if(requestElevation)start.Verb="runas";else start.CreateNoWindow=true;
        start.ArgumentList.Add("--pipe");start.ArgumentList.Add(pipeName);start.ArgumentList.Add("--client-pid");start.ArgumentList.Add(Environment.ProcessId.ToString());
        start.ArgumentList.Add("--client-session");start.ArgumentList.Add(sessionId.ToString());start.ArgumentList.Add("--client-sid");start.ArgumentList.Add(sid);
        Process process;
        try{process=Process.Start(start)??throw new HbxException("HBX-WORKER-START: Worker process did not start.");}
        catch(Win32Exception exception) when(exception.NativeErrorCode==1223){throw new HbxException("HBX-WORKER-UAC-CANCELLED: Administrator consent was cancelled.");}
        var pipe=new NamedPipeClientStream(".",pipeName,PipeDirection.InOut,PipeOptions.Asynchronous,TokenImpersonationLevel.Impersonation);
        byte[]? sessionKey=null;
        try
        {
            using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(TimeSpan.FromSeconds(30));
            var connect=pipe.ConnectAsync(timeout.Token);var exited=process.WaitForExitAsync(timeout.Token);
            var first=await Task.WhenAny(connect,exited);
            if(first==exited){await exited;throw new HbxException($"HBX-WORKER-EXIT: Worker exited before IPC connection with code {process.ExitCode}.");}
            await connect;sessionKey=await PrivilegedBootstrap.EstablishClientAsync(pipe,(uint)process.Id,(uint)process.SessionId,pipeName,timeout.Token);
            return new(process,pipe,sessionKey,sessionId,sid);
        }
        catch
        {
            if(sessionKey is not null)CryptographicOperations.ZeroMemory(sessionKey);pipe.Dispose();
            if(!process.HasExited)try{process.Kill(true);}catch{}
            process.Dispose();throw;
        }
    }

    public async Task<PrivilegedResponseEnvelope> SendAsync(string actionId,string target,string body,CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _closing)!=0,this);
        if(string.IsNullOrWhiteSpace(actionId))throw new ArgumentException("Action ID is required.",nameof(actionId));
        await _requestLock.WaitAsync(token);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _closing)!=0,this);
            return await ExchangeAsync(actionId,target,body,token);
        }
        finally{_requestLock.Release();}
    }

    // Caller owns the request lock for the entire authenticated exchange.
    private async Task<PrivilegedResponseEnvelope> ExchangeAsync(string actionId,string target,string body,CancellationToken token)
    {
            if(_faulted)throw new HbxException("HBX-WORKER-FAULTED: IPC exchange failed; start a new worker.");
            if(_process.HasExited)throw new HbxException($"HBX-WORKER-EXIT: Worker exited with code {_process.ExitCode}.");
            var request=new PrivilegedRequestEnvelope(PrivilegedRequestValidator.ProtocolVersion,Guid.NewGuid().ToString("D"),actionId,target,body,Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),DateTimeOffset.UtcNow.ToUnixTimeSeconds(),_sessionId,_userSid,"");
            request=request with{AuthenticationTag=PrivilegedRequestValidator.Sign(request,_sessionKey)};
            token.ThrowIfCancellationRequested();
            using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(RequestTimeout);
            try
            {
                await PrivilegedFrameCodec.WriteAsync(_pipe,request,timeout.Token);
                var response=await PrivilegedFrameCodec.ReadResponseAsync(_pipe,timeout.Token);
                var validation=PrivilegedResponseAuthenticator.Validate(response,request.RequestId,request.Nonce,_sessionKey);
                if(!validation.Accepted)throw new HbxException($"{validation.Code}: {validation.Message}");
                return response;
            }
            catch(Exception)
            {
                // Partial frames or late responses cannot safely be reused or retried.
                _faulted=true;
                _pipe.Dispose();
                throw;
            }
    }

    public Task<PrivilegedResponseEnvelope> PreviewActionAsync(string actionId,string target,CancellationToken token)=>
        SendAsync("action.preview","",JsonSerializer.Serialize(new WorkerPreviewRequest(actionId,target)),token);
    public Task<PrivilegedResponseEnvelope> ApplyActionAsync(string previewToken,bool approved,string? session,CancellationToken token)=>
        SendAsync("action.apply","",JsonSerializer.Serialize(new WorkerApplyRequest(previewToken,approved,session)),token);
    public Task<PrivilegedResponseEnvelope> VerifyActionAsync(string changeId,bool approved,CancellationToken token)=>
        SendAsync("action.verify","",JsonSerializer.Serialize(new WorkerChangeRequest(changeId,approved)),token);
    public Task<PrivilegedResponseEnvelope> RollbackActionAsync(string changeId,bool approved,CancellationToken token)=>
        SendAsync("action.rollback","",JsonSerializer.Serialize(new WorkerChangeRequest(changeId,approved)),token);

    public async ValueTask DisposeAsync()
    {
        if(Interlocked.Exchange(ref _closing,1)!=0)
        {
            await _closed.Task;
            return;
        }
        try
        {
            // Let already-started exchanges finish under their existing timeout.
            // Queued callers recheck closing after acquiring the lock.
            await _requestLock.WaitAsync();
            try
            {
                if(!_faulted&&_pipe.IsConnected&&!_process.HasExited)
                {
                    using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    try{await ExchangeAsync("worker.shutdown","","{}",timeout.Token);await _process.WaitForExitAsync(timeout.Token);}catch{}
                }
                if(!_process.HasExited)try{_process.Kill(true);}catch{}
            }
            finally
            {
                CryptographicOperations.ZeroMemory(_sessionKey);
                try{_pipe.Dispose();}finally{_process.Dispose();}
                // Do not dispose the semaphore: pre-close callers may still be
                // queued and must be allowed to observe closing safely.
                _requestLock.Release();
            }
            _closed.TrySetResult(true);
        }
        catch(Exception exception)
        {
            _closed.TrySetException(exception);
            throw;
        }
    }
}

using System.IO.Pipes;
using System.Security.Cryptography;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public sealed record PrivilegedDispatchResult(bool Success,string Code,string Message,string Body="");

public sealed class PrivilegedPipeEndpoint:IDisposable
{
    private readonly byte[] _secret;
    private readonly PrivilegedRequestValidator _validator;
    private bool _disposed;

    public PrivilegedPipeEndpoint(ReadOnlySpan<byte> secret,IReadOnlyDictionary<string,Func<string,string,bool>> allowlist)
    {
        if(secret.Length<32)throw new ArgumentException("IPC authentication secret must contain at least 256 bits.",nameof(secret));
        _secret=secret.ToArray();_validator=new PrivilegedRequestValidator(secret,allowlist);
    }

    public async Task<PrivilegedResponseEnvelope> ServeOnceAsync(
        NamedPipeServerStream server,
        uint expectedClientProcessId,
        string expectedUserSid,
        uint expectedSessionId,
        Func<PrivilegedRequestEnvelope,CancellationToken,Task<PrivilegedDispatchResult>> dispatch,
        TimeSpan operationTimeout,
        CancellationToken token,
        TimeSpan? frameReadTimeout=null)
    {
        ObjectDisposedException.ThrowIf(_disposed,this);ArgumentNullException.ThrowIfNull(server);ArgumentNullException.ThrowIfNull(dispatch);
        if(operationTimeout<=TimeSpan.Zero||operationTimeout>TimeSpan.FromMinutes(5))throw new ArgumentOutOfRangeException(nameof(operationTimeout));
        if(frameReadTimeout is { } readLimit&&(readLimit<=TimeSpan.Zero||readLimit>TimeSpan.FromMinutes(5)))
            throw new ArgumentOutOfRangeException(nameof(frameReadTimeout));
        token.ThrowIfCancellationRequested();
        var peer=PrivilegedPipeIdentity.ReadConnectedClient(server);
        if(peer.ProcessId!=expectedClientProcessId||peer.SessionId!=expectedSessionId||!string.Equals(peer.UserSid,expectedUserSid,StringComparison.Ordinal))
            throw new HbxException("HBX-IPC-PEER: Connected client identity does not match the authorized launch context.");
        PrivilegedRequestEnvelope request;
        // Waiting for the next frame must not consume the authorized operation budget.
        using(var readCancellation=CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            if(frameReadTimeout is { } limit)readCancellation.CancelAfter(limit);
            request=await PrivilegedFrameCodec.ReadAsync(server,readCancellation.Token);
        }
        token.ThrowIfCancellationRequested();
        var validation=_validator.Validate(request,peer.UserSid,checked((int)peer.SessionId),DateTimeOffset.UtcNow);
        PrivilegedDispatchResult result;
        if(!validation.Accepted)result=new(false,validation.Code,validation.Message);
        else
        {
            using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(operationTimeout);
            try
            {
                result=await dispatch(request,timeout.Token);
                token.ThrowIfCancellationRequested();
                // Some providers finish after cancellation. Never acknowledge late success;
                // the caller must reconcile durable history before considering a retry.
                if(timeout.IsCancellationRequested)result=TimedOut();
            }
            catch(OperationCanceledException) when(token.IsCancellationRequested){throw;}
            catch(OperationCanceledException) when(timeout.IsCancellationRequested){result=TimedOut();}
            catch(Exception){result=new(false,"HBX-IPC-DISPATCH","Authorized operation failed. Internal details were withheld from the IPC response.");}
        }
        result=Normalize(result);
        var response=new PrivilegedResponseEnvelope(PrivilegedRequestValidator.ProtocolVersion,request.RequestId,request.Nonce,result.Success,result.Code,result.Message,result.Body,DateTimeOffset.UtcNow.ToUnixTimeSeconds(),"");
        response=response with{AuthenticationTag=PrivilegedResponseAuthenticator.Sign(response,_secret)};
        // Bound response delivery independently of idle and dispatch cancellation.
        using var responseCancellation=CancellationTokenSource.CreateLinkedTokenSource(token);
        responseCancellation.CancelAfter(TimeSpan.FromSeconds(5));
        await PrivilegedFrameCodec.WriteResponseAsync(server,response,responseCancellation.Token);
        return response;
    }

    private static PrivilegedDispatchResult TimedOut()=>new(false,"HBX-IPC-TIMEOUT",
        "Authorized operation exceeded its time limit. Its final state may be uncertain; inspect recovery history before retrying.");

    private static PrivilegedDispatchResult Normalize(PrivilegedDispatchResult result)
    {
        if(result is null||string.IsNullOrWhiteSpace(result.Code)||result.Code.Length>128||result.Message is null||result.Message.Length>1024||result.Body is null||System.Text.Encoding.UTF8.GetByteCount(result.Body)>8192)
            return new(false,"HBX-IPC-RESULT","Authorized operation returned an invalid bounded result.");
        return result;
    }

    public void Dispose()
    {
        if(_disposed)return;_validator.Dispose();CryptographicOperations.ZeroMemory(_secret);_disposed=true;
    }
}

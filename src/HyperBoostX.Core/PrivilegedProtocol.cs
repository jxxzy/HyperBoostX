using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace HyperBoostX.Core;

public sealed record PrivilegedRequestEnvelope(
    int Version,
    string RequestId,
    string ActionId,
    string Target,
    string Body,
    string Nonce,
    long IssuedAtUnixSeconds,
    int SessionId,
    string UserSid,
    string AuthenticationTag);

public sealed record PrivilegedRequestValidation(bool Accepted,string Code,string Message)
{
    public static PrivilegedRequestValidation Allow()=>new(true,"HBX-IPC-OK","Request authenticated and validated.");
    public static PrivilegedRequestValidation Deny(string code,string message)=>new(false,code,message);
}

public sealed record PrivilegedResponseEnvelope(
    int Version,
    string RequestId,
    string RequestNonce,
    bool Success,
    string Code,
    string Message,
    string Body,
    long CompletedAtUnixSeconds,
    string AuthenticationTag);

public static class PrivilegedResponseAuthenticator
{
    public static string Sign(PrivilegedResponseEnvelope response,ReadOnlySpan<byte> secret)
    {
        if(secret.Length<32)throw new ArgumentException("IPC authentication secret must contain at least 256 bits.",nameof(secret));
        return Convert.ToBase64String(ComputeTag(response with{AuthenticationTag=""},secret));
    }

    public static PrivilegedRequestValidation Validate(PrivilegedResponseEnvelope response,string expectedRequestId,string expectedRequestNonce,ReadOnlySpan<byte> secret)
    {
        if(secret.Length<32)return PrivilegedRequestValidation.Deny("HBX-IPC-AUTH","IPC authentication secret is invalid.");
        if(response is null||response.Version!=PrivilegedRequestValidator.ProtocolVersion||response.RequestId is null||response.RequestNonce is null||response.Code is null||response.Message is null||response.Body is null||response.AuthenticationTag is null)
            return PrivilegedRequestValidation.Deny("HBX-IPC-RESPONSE","Response is malformed.");
        if(!string.Equals(response.RequestId,expectedRequestId,StringComparison.Ordinal)||!string.Equals(response.RequestNonce,expectedRequestNonce,StringComparison.Ordinal))
            return PrivilegedRequestValidation.Deny("HBX-IPC-RESPONSE-BINDING","Response is not bound to the expected request.");
        byte[] supplied;
        try{supplied=Convert.FromBase64String(response.AuthenticationTag);}
        catch(FormatException){return PrivilegedRequestValidation.Deny("HBX-IPC-AUTH","Response authentication tag is invalid.");}
        var expected=ComputeTag(response with{AuthenticationTag=""},secret);
        var valid=supplied.Length==expected.Length&&CryptographicOperations.FixedTimeEquals(supplied,expected);
        CryptographicOperations.ZeroMemory(expected);
        return valid?PrivilegedRequestValidation.Allow():PrivilegedRequestValidation.Deny("HBX-IPC-AUTH","Response authentication failed.");
    }

    private static byte[] ComputeTag(PrivilegedResponseEnvelope response,ReadOnlySpan<byte> secret)
    {
        using var hmac=new HMACSHA256(secret.ToArray());using var stream=new MemoryStream();using var writer=new BinaryWriter(stream,Encoding.UTF8,true);
        writer.Write("HBX-IPC-RESPONSE-V1");writer.Write(response.Version);writer.Write(response.RequestId);writer.Write(response.RequestNonce);writer.Write(response.Success);
        writer.Write(response.Code);writer.Write(response.Message);writer.Write(response.Body);writer.Write(response.CompletedAtUnixSeconds);writer.Flush();
        return hmac.ComputeHash(stream.GetBuffer(),0,checked((int)stream.Length));
    }
}

public sealed class PrivilegedRequestValidator:IDisposable
{
    public const int ProtocolVersion=1;
    public static readonly TimeSpan MaximumClockSkew=TimeSpan.FromMinutes(2);
    private const int MaximumTargetLength=1024;
    private const int MaximumBodyBytes=4096;
    private readonly byte[] _secret;
    private readonly IReadOnlyDictionary<string,Func<string,string,bool>> _allowlist;
    private readonly ConcurrentDictionary<string,long> _seenNonces=new(StringComparer.Ordinal);
    private bool _disposed;

    public PrivilegedRequestValidator(ReadOnlySpan<byte> secret,IReadOnlyDictionary<string,Func<string,string,bool>> allowlist)
    {
        if(secret.Length<32)throw new ArgumentException("IPC authentication secret must contain at least 256 bits.",nameof(secret));
        _secret=secret.ToArray();
        _allowlist=allowlist??throw new ArgumentNullException(nameof(allowlist));
    }

    public PrivilegedRequestValidation Validate(PrivilegedRequestEnvelope request,string expectedUserSid,int expectedSessionId,DateTimeOffset now)
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        if(request is null||string.IsNullOrEmpty(expectedUserSid))return PrivilegedRequestValidation.Deny("HBX-IPC-MALFORMED","Request or connection identity is missing.");
        if(request.Version!=ProtocolVersion)return PrivilegedRequestValidation.Deny("HBX-IPC-VERSION","Unsupported protocol version.");
        if(!Guid.TryParseExact(request.RequestId,"D",out _))return PrivilegedRequestValidation.Deny("HBX-IPC-ID","Request ID is invalid.");
        if(string.IsNullOrWhiteSpace(request.ActionId)||request.ActionId.Length>128)return PrivilegedRequestValidation.Deny("HBX-IPC-ACTION","Action ID is invalid.");
        if(!_allowlist.TryGetValue(request.ActionId,out var validatePayload))return PrivilegedRequestValidation.Deny("HBX-IPC-DENIED","Action is not allowlisted.");
        if(request.Target is null||request.Body is null||request.UserSid is null||request.Nonce is null||request.AuthenticationTag is null)
            return PrivilegedRequestValidation.Deny("HBX-IPC-MALFORMED","Request contains missing fields.");
        if(request.Target.Length>MaximumTargetLength||Encoding.UTF8.GetByteCount(request.Body)>MaximumBodyBytes)return PrivilegedRequestValidation.Deny("HBX-IPC-SIZE","Request payload exceeds the protocol limit.");
        var suppliedSid=Encoding.UTF8.GetBytes(request.UserSid);var connectionSid=Encoding.UTF8.GetBytes(expectedUserSid);
        if(request.SessionId!=expectedSessionId||suppliedSid.Length!=connectionSid.Length||!CryptographicOperations.FixedTimeEquals(suppliedSid,connectionSid))
            return PrivilegedRequestValidation.Deny("HBX-IPC-IDENTITY","Request identity does not match the authenticated connection.");
        DateTimeOffset issued;
        try{issued=DateTimeOffset.FromUnixTimeSeconds(request.IssuedAtUnixSeconds);}
        catch(ArgumentOutOfRangeException){return PrivilegedRequestValidation.Deny("HBX-IPC-TIME","Request timestamp is invalid.");}
        if((now-issued).Duration()>MaximumClockSkew)return PrivilegedRequestValidation.Deny("HBX-IPC-EXPIRED","Request is outside the accepted time window.");
        byte[] nonce;
        try{nonce=Convert.FromBase64String(request.Nonce);}
        catch(FormatException){return PrivilegedRequestValidation.Deny("HBX-IPC-NONCE","Request nonce is invalid.");}
        if(nonce.Length!=32||!string.Equals(request.Nonce,Convert.ToBase64String(nonce),StringComparison.Ordinal))return PrivilegedRequestValidation.Deny("HBX-IPC-NONCE","Request nonce must be canonical base64 containing 256 bits.");
        byte[] suppliedTag;
        try{suppliedTag=Convert.FromBase64String(request.AuthenticationTag);}
        catch(FormatException){return PrivilegedRequestValidation.Deny("HBX-IPC-AUTH","Authentication tag is invalid.");}
        var expectedTag=ComputeAuthenticationTag(request with{AuthenticationTag=""},_secret);
        var authenticated=suppliedTag.Length==expectedTag.Length&&CryptographicOperations.FixedTimeEquals(suppliedTag,expectedTag);
        CryptographicOperations.ZeroMemory(expectedTag);
        if(!authenticated)return PrivilegedRequestValidation.Deny("HBX-IPC-AUTH","Request authentication failed.");
        try{if(!validatePayload(request.Target,request.Body))return PrivilegedRequestValidation.Deny("HBX-IPC-PAYLOAD","Action payload validation failed.");}
        catch{return PrivilegedRequestValidation.Deny("HBX-IPC-PAYLOAD","Action payload validation failed.");}
        RemoveExpiredNonces(now.ToUnixTimeSeconds());
        if(!_seenNonces.TryAdd(Convert.ToBase64String(nonce),request.IssuedAtUnixSeconds))return PrivilegedRequestValidation.Deny("HBX-IPC-REPLAY","Request nonce has already been used.");
        return PrivilegedRequestValidation.Allow();
    }

    public static string Sign(PrivilegedRequestEnvelope request,ReadOnlySpan<byte> secret)
    {
        if(secret.Length<32)throw new ArgumentException("IPC authentication secret must contain at least 256 bits.",nameof(secret));
        return Convert.ToBase64String(ComputeAuthenticationTag(request with{AuthenticationTag=""},secret));
    }

    private static byte[] ComputeAuthenticationTag(PrivilegedRequestEnvelope request,ReadOnlySpan<byte> secret)
    {
        using var hmac=new HMACSHA256(secret.ToArray());
        using var stream=new MemoryStream();
        using var writer=new BinaryWriter(stream,Encoding.UTF8,true);
        writer.Write("HBX-IPC-REQUEST-V1");writer.Write(request.Version);writer.Write(request.RequestId);writer.Write(request.ActionId);writer.Write(request.Target);writer.Write(request.Body);
        writer.Write(request.Nonce);writer.Write(request.IssuedAtUnixSeconds);writer.Write(request.SessionId);writer.Write(request.UserSid);writer.Flush();
        return hmac.ComputeHash(stream.GetBuffer(),0,checked((int)stream.Length));
    }

    private void RemoveExpiredNonces(long nowUnixSeconds)
    {
        var oldest=nowUnixSeconds-(long)MaximumClockSkew.TotalSeconds;
        foreach(var entry in _seenNonces)if(entry.Value<oldest)_seenNonces.TryRemove(entry.Key,out _);
    }

    public void Dispose()
    {
        if(_disposed)return;
        CryptographicOperations.ZeroMemory(_secret);_disposed=true;
    }
}

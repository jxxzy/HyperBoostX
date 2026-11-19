using System.Security.Cryptography;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using HyperBoostX.Core;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class PrivilegedProtocolTests
{
    private static readonly DateTimeOffset Now=DateTimeOffset.FromUnixTimeSeconds(2_000_000_000);

    [Fact]public void AuthenticatedAllowlistedRequestIsAcceptedOnce()
    {
        var secret=RandomNumberGenerator.GetBytes(32);
        using var validator=Validator(secret);
        var request=Signed(Request(),secret);
        Assert.True(validator.Validate(request,"S-1-5-21-test",7,Now).Accepted);
        var replay=validator.Validate(request,"S-1-5-21-test",7,Now);
        Assert.False(replay.Accepted);Assert.Equal("HBX-IPC-REPLAY",replay.Code);

        var canonicalEquivalent=request with{Nonce=Convert.ToBase64String(Convert.FromBase64String(request.Nonce))};
        var replayEquivalent=validator.Validate(Signed(canonicalEquivalent,secret),"S-1-5-21-test",7,Now);
        Assert.False(replayEquivalent.Accepted);Assert.Equal("HBX-IPC-REPLAY",replayEquivalent.Code);
    }

    [Fact]public void NonCanonicalNonceCannotBypassReplayProtection()
    {
        var secret=RandomNumberGenerator.GetBytes(32);using var validator=Validator(secret);
        var canonical=Signed(Request(),secret);
        Assert.True(validator.Validate(canonical,"S-1-5-21-test",7,Now).Accepted);
        var alternate=Signed(canonical with{Nonce=canonical.Nonce.Insert(4," ")},secret);
        Assert.Equal("HBX-IPC-NONCE",validator.Validate(alternate,"S-1-5-21-test",7,Now).Code);
        Assert.Equal("HBX-IPC-REPLAY",validator.Validate(canonical,"S-1-5-21-test",7,Now).Code);
    }

    [Fact]public async Task FrameRejectsDuplicateAndMissingFieldsInRequestsAndResponses()
    {
        var request=System.Text.Json.JsonSerializer.Serialize(Request());
        await using var duplicate=RawFrame(request.Insert(1,"\"Version\":1,"));
        await Assert.ThrowsAsync<HbxException>(()=>PrivilegedFrameCodec.ReadAsync(duplicate,default));
        await using var missing=RawFrame(request.Replace("\"SessionId\":7,",""));
        await Assert.ThrowsAsync<HbxException>(()=>PrivilegedFrameCodec.ReadAsync(missing,default));
        var response=new PrivilegedResponseEnvelope(1,Request().RequestId,Request().Nonce,true,"OK","verified","{}",Now.ToUnixTimeSeconds(),"tag");
        var json=System.Text.Json.JsonSerializer.Serialize(response);
        await using var duplicateResponse=RawFrame(json.Insert(1,"\"Success\":false,"));
        await Assert.ThrowsAsync<HbxException>(()=>PrivilegedFrameCodec.ReadResponseAsync(duplicateResponse,default));
        await using var missingResponse=RawFrame(json.Replace("\"Success\":true,",""));
        await Assert.ThrowsAsync<HbxException>(()=>PrivilegedFrameCodec.ReadResponseAsync(missingResponse,default));
    }

    private static MemoryStream RawFrame(string json)
    {
        var bytes=Encoding.UTF8.GetBytes(json);var header=new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header,bytes.Length);
        var stream=new MemoryStream();stream.Write(header);stream.Write(bytes);stream.Position=0;return stream;
    }

    [Fact]public void TamperingIdentityAndUnknownActionsAreDenied()
    {
        var secret=RandomNumberGenerator.GetBytes(32);using var validator=Validator(secret);
        var signed=Signed(Request(),secret);
        Assert.Equal("HBX-IPC-AUTH",validator.Validate(signed with{Body="tampered"},"S-1-5-21-test",7,Now).Code);
        Assert.Equal("HBX-IPC-IDENTITY",validator.Validate(signed,"S-1-5-21-other",7,Now).Code);
        var unknown=Signed(Request() with{ActionId="process.start"},secret);
        Assert.Equal("HBX-IPC-DENIED",validator.Validate(unknown,"S-1-5-21-test",7,Now).Code);
    }

    [Fact]public void ExpiredMalformedAndActionInvalidPayloadsAreDenied()
    {
        var secret=RandomNumberGenerator.GetBytes(32);using var validator=Validator(secret);
        var expired=Signed(Request() with{IssuedAtUnixSeconds=Now.AddMinutes(-3).ToUnixTimeSeconds()},secret);
        Assert.Equal("HBX-IPC-EXPIRED",validator.Validate(expired,"S-1-5-21-test",7,Now).Code);
        var malformed=Signed(Request() with{Nonce="invalid"},secret);
        Assert.Equal("HBX-IPC-NONCE",validator.Validate(malformed,"S-1-5-21-test",7,Now).Code);
        var invalid=Signed(Request() with{Target="forbidden"},secret);
        Assert.Equal("HBX-IPC-PAYLOAD",validator.Validate(invalid,"S-1-5-21-test",7,Now).Code);
    }

    [Fact]public void NullFieldsAndThrowingPayloadValidatorsAreDeniedWithoutCrashing()
    {
        var secret=RandomNumberGenerator.GetBytes(32);
        using var validator=Validator(secret);
        Assert.Equal("HBX-IPC-MALFORMED",validator.Validate(Request() with{Body=null!},"S-1-5-21-test",7,Now).Code);
        using var throwing=new PrivilegedRequestValidator(secret,new Dictionary<string,Func<string,string,bool>>{{"dns.set",(_,_)=>throw new InvalidOperationException("untrusted validator failure")}});
        var signed=Signed(Request(),secret);
        Assert.Equal("HBX-IPC-PAYLOAD",throwing.Validate(signed,"S-1-5-21-test",7,Now).Code);
    }

    [Fact]public void SigningRejectsWeakSecrets()=>
        Assert.Throws<ArgumentException>(()=>PrivilegedRequestValidator.Sign(Request(),new byte[16]));

    [Fact]public async Task BoundedFrameRoundTripsWithExactFields()
    {
        await using var stream=new MemoryStream();var request=Request();
        await PrivilegedFrameCodec.WriteAsync(stream,request,default);stream.Position=0;
        Assert.Equal(request,await PrivilegedFrameCodec.ReadAsync(stream,default));
    }

    [Fact]public async Task OversizedTruncatedAndUnknownFieldFramesAreRejected()
    {
        await using var oversized=new MemoryStream();var header=new byte[4];BinaryPrimitives.WriteInt32LittleEndian(header,PrivilegedFrameCodec.MaximumFrameBytes+1);
        await oversized.WriteAsync(header);oversized.Position=0;
        await Assert.ThrowsAsync<HbxException>(()=>PrivilegedFrameCodec.ReadAsync(oversized,default));

        await using var truncated=new MemoryStream();BinaryPrimitives.WriteInt32LittleEndian(header,20);await truncated.WriteAsync(header);await truncated.WriteAsync(new byte[2]);truncated.Position=0;
        await Assert.ThrowsAsync<EndOfStreamException>(()=>PrivilegedFrameCodec.ReadAsync(truncated,default));

        var json="{\"Version\":1,\"Unexpected\":true}";var bytes=Encoding.UTF8.GetBytes(json);
        await using var unknown=new MemoryStream();BinaryPrimitives.WriteInt32LittleEndian(header,bytes.Length);await unknown.WriteAsync(header);await unknown.WriteAsync(bytes);unknown.Position=0;
        var malformed=await Assert.ThrowsAsync<HbxException>(()=>PrivilegedFrameCodec.ReadAsync(unknown,default));
        Assert.StartsWith("HBX-IPC-FRAME-JSON",malformed.Message);
    }

    [Fact]public async Task ResponseIsAuthenticatedBoundAndFramed()
    {
        var secret=RandomNumberGenerator.GetBytes(32);var request=Request();
        var response=new PrivilegedResponseEnvelope(1,request.RequestId,request.Nonce,true,"HBX-IPC-OK","verified","{}",Now.ToUnixTimeSeconds(),"");
        response=response with{AuthenticationTag=PrivilegedResponseAuthenticator.Sign(response,secret)};
        Assert.True(PrivilegedResponseAuthenticator.Validate(response,request.RequestId,request.Nonce,secret).Accepted);
        Assert.Equal("HBX-IPC-RESPONSE-BINDING",PrivilegedResponseAuthenticator.Validate(response,Guid.NewGuid().ToString("D"),request.Nonce,secret).Code);
        Assert.Equal("HBX-IPC-AUTH",PrivilegedResponseAuthenticator.Validate(response with{Body="tampered"},request.RequestId,request.Nonce,secret).Code);
        await using var stream=new MemoryStream();await PrivilegedFrameCodec.WriteResponseAsync(stream,response,default);stream.Position=0;
        Assert.Equal(response,await PrivilegedFrameCodec.ReadResponseAsync(stream,default));
    }

    private static PrivilegedRequestValidator Validator(byte[] secret)=>new(secret,new Dictionary<string,Func<string,string,bool>>(StringComparer.Ordinal)
    {
        ["dns.set"]=(target,body)=>target=="Ethernet"&&body=="{\"servers\":[\"1.1.1.1\"]}"
    });

    private static PrivilegedRequestEnvelope Request()=>new(1,Guid.NewGuid().ToString("D"),"dns.set","Ethernet","{\"servers\":[\"1.1.1.1\"]}",Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),Now.ToUnixTimeSeconds(),7,"S-1-5-21-test","");
    private static PrivilegedRequestEnvelope Signed(PrivilegedRequestEnvelope request,byte[] secret)=>request with{AuthenticationTag=PrivilegedRequestValidator.Sign(request,secret)};
}

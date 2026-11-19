using System.Buffers.Binary;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public static class PrivilegedBootstrap
{
    private const int MaximumPublicKeyBytes=512;

    public static async Task<byte[]> EstablishServerAsync(NamedPipeServerStream server,uint expectedClientProcessId,string expectedUserSid,uint expectedSessionId,string pipeName,CancellationToken token)
    {
        var peer=PrivilegedPipeIdentity.ReadConnectedClient(server);
        if(peer.ProcessId!=expectedClientProcessId||peer.SessionId!=expectedSessionId||!string.Equals(peer.UserSid,expectedUserSid,StringComparison.Ordinal))
            throw new HbxException("HBX-IPC-BOOTSTRAP-PEER: Bootstrap client does not match the authorized launch context.");
        using var local=ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var clientPublic=await ReadBlobAsync(server,token);var serverPublic=local.ExportSubjectPublicKeyInfo();
        await WriteBlobAsync(server,serverPublic,token);
        return Derive(local,clientPublic,clientPublic,serverPublic,pipeName);
    }

    public static async Task<byte[]> EstablishClientAsync(NamedPipeClientStream client,uint expectedServerProcessId,uint expectedSessionId,string pipeName,CancellationToken token)
    {
        var peer=PrivilegedPipeIdentity.ReadConnectedServer(client);
        if(peer.ProcessId!=expectedServerProcessId||peer.SessionId!=expectedSessionId)
            throw new HbxException("HBX-IPC-BOOTSTRAP-PEER: Bootstrap server does not match the authorized elevated process.");
        using var local=ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);var clientPublic=local.ExportSubjectPublicKeyInfo();
        await WriteBlobAsync(client,clientPublic,token);var serverPublic=await ReadBlobAsync(client,token);
        return Derive(local,serverPublic,clientPublic,serverPublic,pipeName);
    }

    private static byte[] Derive(ECDiffieHellman local,byte[] remotePublic,byte[] clientPublic,byte[] serverPublic,string pipeName)
    {
        if(string.IsNullOrWhiteSpace(pipeName)||pipeName.Length>200)throw new HbxException("HBX-IPC-BOOTSTRAP-NAME: Pipe name is invalid.");
        using var remote=ECDiffieHellman.Create();
        try{remote.ImportSubjectPublicKeyInfo(remotePublic,out var read);if(read!=remotePublic.Length)throw new CryptographicException("Trailing key data.");}
        catch(CryptographicException exception){throw new HbxException("HBX-IPC-BOOTSTRAP-KEY: Peer public key is invalid: "+ReportExport.Redact(exception.Message));}
        var shared=local.DeriveKeyMaterial(remote.PublicKey);
        try
        {
            using var transcript=new MemoryStream();using var writer=new BinaryWriter(transcript,Encoding.UTF8,true);
            writer.Write("HBX-IPC-BOOTSTRAP-V1");writer.Write(pipeName);writer.Write(clientPublic.Length);writer.Write(clientPublic);writer.Write(serverPublic.Length);writer.Write(serverPublic);writer.Flush();
            using var hmac=new HMACSHA256(shared);return hmac.ComputeHash(transcript.GetBuffer(),0,checked((int)transcript.Length));
        }
        finally{CryptographicOperations.ZeroMemory(shared);}
    }

    private static async Task WriteBlobAsync(Stream stream,byte[] blob,CancellationToken token)
    {
        if(blob.Length==0||blob.Length>MaximumPublicKeyBytes)throw new HbxException("HBX-IPC-BOOTSTRAP-SIZE: Public key length is invalid.");
        var header=new byte[4];BinaryPrimitives.WriteInt32LittleEndian(header,blob.Length);await stream.WriteAsync(header,token);await stream.WriteAsync(blob,token);await stream.FlushAsync(token);
    }

    private static async Task<byte[]> ReadBlobAsync(Stream stream,CancellationToken token)
    {
        var header=new byte[4];await ReadExactlyAsync(stream,header,token);var length=BinaryPrimitives.ReadInt32LittleEndian(header);
        if(length<=0||length>MaximumPublicKeyBytes)throw new HbxException("HBX-IPC-BOOTSTRAP-SIZE: Public key length is invalid.");
        var blob=new byte[length];await ReadExactlyAsync(stream,blob,token);return blob;
    }

    private static async Task ReadExactlyAsync(Stream stream,Memory<byte> buffer,CancellationToken token)
    {
        var read=0;while(read<buffer.Length){var count=await stream.ReadAsync(buffer[read..],token);if(count==0)throw new EndOfStreamException("HBX-IPC-BOOTSTRAP-EOF: Bootstrap stream ended early.");read+=count;}
    }
}

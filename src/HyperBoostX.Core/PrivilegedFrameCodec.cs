using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HyperBoostX.Core;

public static class PrivilegedFrameCodec
{
    public const int MaximumFrameBytes=16*1024;
    private static readonly JsonSerializerOptions JsonOptions=new()
    {
        PropertyNameCaseInsensitive=false,
        UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow,
        MaxDepth=8
    };

    public static async Task WriteAsync(Stream stream,PrivilegedRequestEnvelope request,CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(stream);ArgumentNullException.ThrowIfNull(request);
        var payload=JsonSerializer.SerializeToUtf8Bytes(request,JsonOptions);
        if(payload.Length>MaximumFrameBytes)throw new HbxException("HBX-IPC-FRAME-SIZE: Request frame exceeds the protocol limit.");
        var header=new byte[sizeof(int)];BinaryPrimitives.WriteInt32LittleEndian(header,payload.Length);
        await stream.WriteAsync(header,token);await stream.WriteAsync(payload,token);await stream.FlushAsync(token);
    }

    public static async Task<PrivilegedRequestEnvelope> ReadAsync(Stream stream,CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var header=new byte[sizeof(int)];await ReadExactlyAsync(stream,header,token);
        var length=BinaryPrimitives.ReadInt32LittleEndian(header);
        if(length<=0||length>MaximumFrameBytes)throw new HbxException("HBX-IPC-FRAME-SIZE: Request frame length is invalid.");
        var payload=new byte[length];await ReadExactlyAsync(stream,payload,token);
        try
        {
            ValidateEnvelope<PrivilegedRequestEnvelope>(payload);
            return JsonSerializer.Deserialize<PrivilegedRequestEnvelope>(payload,JsonOptions)
                ??throw new HbxException("HBX-IPC-FRAME-JSON: Request frame is empty.");
        }
        catch(JsonException exception){throw new HbxException($"HBX-IPC-FRAME-JSON: Request frame is malformed: {ReportExport.Redact(exception.Message)}");}
    }

    public static async Task WriteResponseAsync(Stream stream,PrivilegedResponseEnvelope response,CancellationToken token)=>
        await WriteFrameAsync(stream,response,token);

    public static async Task<PrivilegedResponseEnvelope> ReadResponseAsync(Stream stream,CancellationToken token)=>
        await ReadFrameAsync<PrivilegedResponseEnvelope>(stream,token);

    private static async Task WriteFrameAsync<T>(Stream stream,T value,CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(stream);ArgumentNullException.ThrowIfNull(value);
        var payload=JsonSerializer.SerializeToUtf8Bytes(value,JsonOptions);
        if(payload.Length>MaximumFrameBytes)throw new HbxException("HBX-IPC-FRAME-SIZE: IPC frame exceeds the protocol limit.");
        var header=new byte[sizeof(int)];BinaryPrimitives.WriteInt32LittleEndian(header,payload.Length);
        await stream.WriteAsync(header,token);await stream.WriteAsync(payload,token);await stream.FlushAsync(token);
    }

    private static async Task<T> ReadFrameAsync<T>(Stream stream,CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var header=new byte[sizeof(int)];await ReadExactlyAsync(stream,header,token);
        var length=BinaryPrimitives.ReadInt32LittleEndian(header);
        if(length<=0||length>MaximumFrameBytes)throw new HbxException("HBX-IPC-FRAME-SIZE: IPC frame length is invalid.");
        var payload=new byte[length];await ReadExactlyAsync(stream,payload,token);
        try{ValidateEnvelope<T>(payload);return JsonSerializer.Deserialize<T>(payload,JsonOptions)??throw new HbxException("HBX-IPC-FRAME-JSON: IPC frame is empty.");}
        catch(JsonException exception){throw new HbxException($"HBX-IPC-FRAME-JSON: IPC frame is malformed: {ReportExport.Redact(exception.Message)}");}
    }

    private static void ValidateEnvelope<T>(byte[] payload)
    {
        // JsonSerializer otherwise allows duplicate property names (last value wins),
        // and omitted primitive fields silently receive CLR defaults.
        using var doc=JsonDocument.Parse(payload,new JsonDocumentOptions{MaxDepth=8});
        if(doc.RootElement.ValueKind!=JsonValueKind.Object)
            throw new JsonException("IPC envelope must be a JSON object.");
        var expected=typeof(T).GetProperties().Select(p=>p.Name).ToHashSet(StringComparer.Ordinal);
        foreach(var property in doc.RootElement.EnumerateObject())
        {
            if(!expected.Remove(property.Name)||property.Value.ValueKind==JsonValueKind.Null)
                throw new JsonException("IPC envelope contains unknown, duplicate or null fields.");
        }
        if(expected.Count!=0)throw new JsonException("IPC envelope is missing required fields.");
    }

    private static async Task ReadExactlyAsync(Stream stream,Memory<byte> buffer,CancellationToken token)
    {
        var read=0;
        while(read<buffer.Length)
        {
            var count=await stream.ReadAsync(buffer[read..],token);
            if(count==0)throw new EndOfStreamException("HBX-IPC-FRAME-EOF: IPC stream ended before the complete frame was received.");
            read+=count;
        }
    }
}

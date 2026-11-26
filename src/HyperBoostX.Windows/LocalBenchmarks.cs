using System.Diagnostics;
using System.Security.Cryptography;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public static class LocalBenchmarks
{
    public static Task<DiagnosticReport> CpuAsync(CancellationToken token)=>Task.Run(()=>
    {
        var data=new byte[1024*1024];RandomNumberGenerator.Fill(data);var digest=new byte[32];
        SHA256.HashData(data,digest);var watch=Stopwatch.StartNew();long bytes=0;var checksum=0;
        while(watch.Elapsed<TimeSpan.FromSeconds(3)){token.ThrowIfCancellationRequested();SHA256.HashData(data,digest);bytes+=data.Length;checksum^=digest[0];}
        watch.Stop();
        return new DiagnosticReport("CPU benchmark",DateTimeOffset.UtcNow,
            [new("Method","HBX-SHA256-1: single worker SHA-256 over 1 MiB buffer for >=3 seconds; hardware acceleration may apply.","Benchmark method"),
             new("Throughput",$"{bytes/1048576d/watch.Elapsed.TotalSeconds:F2} MiB/s","Stopwatch + processed bytes","Measured"),
             new("Elapsed",$"{watch.Elapsed.TotalSeconds:F3} seconds","Stopwatch","Measured"),new("Data processed",bytes.ToString(),"Byte counter","Measured"),
             new("Checksum accumulator",checksum.ToString(),"Work result"),new("Comparison limits","Compare identical app/runtime versions, AC/battery state, power plan, background load and thermal conditions. This is not an FPS or general CPU score.","Methodology")],[]);
    },token);
    public const int StorageBlockSize=1024*1024;
    public const int StorageBlockCount=64;

    public static async Task VerifyRepeatedBlocksAsync(Stream stream,byte[] expectedBlock,int blockCount,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if(expectedBlock.Length==0||blockCount<=0)throw new HbxException("Invalid storage verification parameters.");
        var actual=new byte[expectedBlock.Length];
        for(int block=0;block<blockCount;block++)
        {
            token.ThrowIfCancellationRequested();
            int offset=0;
            while(offset<actual.Length)
            {
                int read=await stream.ReadAsync(actual.AsMemory(offset),token);
                if(read==0)throw new HbxException("HBX-BENCH-0010: Scratch file ended before the expected data was verified.");
                offset+=read;
            }
            if(!CryptographicOperations.FixedTimeEquals(actual,expectedBlock))
                throw new HbxException("HBX-BENCH-0010: Scratch file content verification failed.");
        }
        if(await stream.ReadAsync(actual.AsMemory(0,1),token)!=0)
            throw new HbxException("HBX-BENCH-0010: Unexpected extra scratch file data.");
        token.ThrowIfCancellationRequested();
    }

    public static async Task<DiagnosticReport> StorageAsync(string dataRoot,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var folder=SafePaths.Under(dataRoot,"BenchmarkScratch");Directory.CreateDirectory(folder);
        var path=SafePaths.Under(folder,Guid.NewGuid().ToString("N")+".tmp");
        var driveRoot=Path.GetPathRoot(folder)??throw new HbxException("Storage test drive is unavailable.");
        var drive=new DriveInfo(driveRoot);
        if(drive.AvailableFreeSpace<StorageBlockSize*(long)StorageBlockCount*2)
            throw new HbxException("Storage benchmark requires at least 128 MiB available space.");
        var expected=new byte[StorageBlockSize];RandomNumberGenerator.Fill(expected);
        var readBuffer=new byte[StorageBlockSize];
        token.ThrowIfCancellationRequested();
        // Keep one exclusive handle for the whole test. CreateNew never overwrites
        // another file; DeleteOnClose removes only the file opened by this handle.
        await using var file=new FileStream(path,FileMode.CreateNew,FileAccess.ReadWrite,FileShare.None,
            StorageBlockSize,FileOptions.Asynchronous|FileOptions.WriteThrough|FileOptions.DeleteOnClose);
        var write=Stopwatch.StartNew();
        for(var i=0;i<StorageBlockCount;i++)
        {
            token.ThrowIfCancellationRequested();await file.WriteAsync(expected,token);
        }
        file.Flush(true);write.Stop();
        token.ThrowIfCancellationRequested();
        file.Position=0;var read=Stopwatch.StartNew();long total=0;
        while(true)
        {
            var count=await file.ReadAsync(readBuffer,token);if(count==0)break;total+=count;
        }
        read.Stop();
        if(total!=StorageBlockCount*(long)StorageBlockSize)throw new HbxException("HBX-BENCH-0010: Scratch file length verification failed.");
        // Content verification is a separate untimed pass, not part of the read throughput.
        file.Position=0;await VerifyRepeatedBlocksAsync(file,expected,StorageBlockCount,token);
        if(write.Elapsed.TotalSeconds<=0||read.Elapsed.TotalSeconds<=0)
            throw new HbxException("Storage measurement interval is unavailable.");
        return new("Storage benchmark",DateTimeOffset.UtcNow,
            [new("Method","HBX-STORAGE-2: 64 MiB sequential write-through + flush; same-handle buffered read; separate untimed byte verification. Read results include Windows cache, not raw-device speed.","Benchmark method"),
             new("Test drive",driveRoot,"Scratch location"),
             new("Write throughput",$"{StorageBlockCount/write.Elapsed.TotalSeconds:F2} MiB/s","Stopwatch + written bytes","Measured"),
             new("Buffered read throughput",$"{StorageBlockCount/read.Elapsed.TotalSeconds:F2} MiB/s","Stopwatch + read bytes","Measured"),
             new("Write elapsed",$"{write.Elapsed.TotalSeconds:F3} seconds","Stopwatch","Measured"),
             new("Read elapsed",$"{read.Elapsed.TotalSeconds:F3} seconds","Stopwatch","Measured"),
             new("Bytes verified",total.ToString(),"Full content and length read-back","Measured"),
             new("Content verification","Every byte matched the generated test data","Untimed read-back","Direct","Available")],[]);
    }
}

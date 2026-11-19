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
    public static async Task<DiagnosticReport> StorageAsync(string dataRoot,CancellationToken token)
    {
        var folder=SafePaths.Under(dataRoot,"BenchmarkScratch");Directory.CreateDirectory(folder);
        var path=SafePaths.Under(folder,Guid.NewGuid().ToString("N")+".tmp");
        var buffer=new byte[1024*1024];RandomNumberGenerator.Fill(buffer);const int blocks=64;
        try
        {
            var write=Stopwatch.StartNew();
            await using(var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,buffer.Length,FileOptions.Asynchronous|FileOptions.WriteThrough))
            {
                for(var i=0;i<blocks;i++){token.ThrowIfCancellationRequested();await file.WriteAsync(buffer,token);}file.Flush(true);
            }
            write.Stop();var read=Stopwatch.StartNew();long total=0;
            await using(var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,buffer.Length,FileOptions.Asynchronous|FileOptions.SequentialScan))
            {
                while(true){var count=await file.ReadAsync(buffer,token);if(count==0)break;total+=count;}
            }
            read.Stop();if(total!=blocks*1024L*1024)throw new HbxException("HBX-BENCH-0010: Scratch file length verification failed.");
            return new("Storage benchmark",DateTimeOffset.UtcNow,
                [new("Method","HBX-STORAGE-1: 64 MiB sequential write-through + flush, followed by buffered read. Read results include Windows cache and do not measure raw-device speed.","Benchmark method"),
                 new("Test drive",Path.GetPathRoot(folder)??"Unavailable","Scratch location"),new("Write throughput",$"{blocks/write.Elapsed.TotalSeconds:F2} MiB/s","Stopwatch + written bytes","Measured"),
                 new("Buffered read throughput",$"{blocks/read.Elapsed.TotalSeconds:F2} MiB/s","Stopwatch + read bytes","Measured"),new("Bytes verified",total.ToString(),"File length read-back","Measured")],[]);
        }
        finally{SafePaths.Local(path);if(File.Exists(path))File.Delete(path);}
    }
}

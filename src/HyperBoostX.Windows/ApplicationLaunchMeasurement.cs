using System.Diagnostics;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public sealed record ApplicationLaunchResult(
    string ExecutablePath,
    int ProcessId,
    long ProcessCreatedMilliseconds,
    long? InputIdleMilliseconds,
    bool ExitedDuringMeasurement,
    long? WorkingSetBytes,
    long? PrivateMemoryBytes,
    long? ProcessorTimeMilliseconds,
    DateTimeOffset MeasuredAt);

public static class ApplicationLaunchMeasurement
{
    public const int InputIdleTimeoutMilliseconds=10_000;
    public const int ResourceSampleDelayMilliseconds=500;

    public static string ValidateExecutable(string path)
    {
        if(string.IsNullOrWhiteSpace(path))throw new HbxException("Pilih file executable terlebih dahulu.");
        if(!Path.IsPathFullyQualified(path))throw new HbxException("Lokasi aplikasi harus berupa path absolut lokal.");
        var fullPath=Path.GetFullPath(path);
        if(fullPath.StartsWith(@"\\",StringComparison.Ordinal)||fullPath.StartsWith(@"\\?\UNC\",StringComparison.OrdinalIgnoreCase))
            throw new HbxException("Executable jaringan tidak didukung. Pilih aplikasi pada drive lokal.");
        if(!string.Equals(Path.GetExtension(fullPath),".exe",StringComparison.OrdinalIgnoreCase))
            throw new HbxException("Pengukuran hanya mendukung file .exe.");
        if(!File.Exists(fullPath))throw new HbxException("File executable tidak ditemukan.");
        return fullPath;
    }

    public static async Task<ApplicationLaunchResult> MeasureAsync(string path,CancellationToken token)
    {
        var executable=ValidateExecutable(path);
        token.ThrowIfCancellationRequested();
        var stopwatch=Stopwatch.StartNew();
        using var process=Process.Start(new ProcessStartInfo
        {
            FileName=executable,
            WorkingDirectory=Path.GetDirectoryName(executable)!,
            UseShellExecute=false
        })??throw new HbxException("Windows tidak mengembalikan proses aplikasi yang diluncurkan.");
        var created=stopwatch.ElapsedMilliseconds;
        long? inputIdle=null;
        try
        {
            if(await Task.Run(()=>process.WaitForInputIdle(InputIdleTimeoutMilliseconds),CancellationToken.None).ConfigureAwait(false))
                inputIdle=stopwatch.ElapsedMilliseconds;
        }
        catch(InvalidOperationException) { }
        catch(NotSupportedException) { }

        token.ThrowIfCancellationRequested();
        await Task.Delay(ResourceSampleDelayMilliseconds,token).ConfigureAwait(false);
        bool exited;
        try{exited=process.HasExited;}catch(InvalidOperationException){exited=true;}
        long? workingSet=null,privateMemory=null,processorTime=null;
        if(!exited)
        {
            try
            {
                process.Refresh();
                workingSet=process.WorkingSet64;
                privateMemory=process.PrivateMemorySize64;
                processorTime=(long)process.TotalProcessorTime.TotalMilliseconds;
            }
            catch(InvalidOperationException){exited=true;}
        }
        return new(executable,process.Id,created,inputIdle,exited,workingSet,privateMemory,processorTime,DateTimeOffset.UtcNow);
    }

    public static DiagnosticReport ToReport(ApplicationLaunchResult result)
    {
        var name=Path.GetFileName(result.ExecutablePath);
        var facts=new List<Fact>
        {
            new("Application",name,result.ExecutablePath,"Direct","Available"),
            new("Process created",$"{result.ProcessCreatedMilliseconds:N0} ms",$"Process.Start to PID {result.ProcessId}","Measured","Available"),
            result.InputIdleMilliseconds is long idle
                ?new("Window input ready",$"{idle:N0} ms","Process.WaitForInputIdle","Measured","Available")
                :new("Window input ready","Unavailable",result.ExitedDuringMeasurement?"Process exited before a GUI input-idle signal":"Application did not expose a GUI input-idle signal within 10 seconds","Measured","Unavailable"),
            new("Measurement state",result.ExitedDuringMeasurement?"Process exited during measurement":$"Process {result.ProcessId} remains running","HyperBoostX launch measurement","Observed","Available")
        };
        if(result.WorkingSetBytes is long workingSet)
            facts.Add(new("Initial working set",FormatBytes(workingSet),$"Process {result.ProcessId} after {ResourceSampleDelayMilliseconds} ms sampling delay","Measured","Available"));
        if(result.PrivateMemoryBytes is long privateMemory)
            facts.Add(new("Initial private memory",FormatBytes(privateMemory),$"Process {result.ProcessId} after {ResourceSampleDelayMilliseconds} ms sampling delay","Measured","Available"));
        if(result.ProcessorTimeMilliseconds is long cpu)
            facts.Add(new("Initial processor time",$"{cpu:N0} ms",$"Process {result.ProcessId} cumulative CPU time","Measured","Available"));
        return new("App Load Optimizer",result.MeasuredAt,facts,[]);
    }

    private static string FormatBytes(long value)=>value>=1024L*1024L*1024L?$"{value/(1024d*1024d*1024d):N2} GiB":$"{value/(1024d*1024d):N1} MiB";
}

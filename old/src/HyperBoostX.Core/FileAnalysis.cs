using System.Diagnostics;
using System.Security;
using System.Security.Cryptography;

namespace HyperBoostX.Core;

public sealed record FileAnalysisEntry(string Path,long Length,DateTimeOffset LastWriteTimeUtc)
{
    public string SizeDisplay=>FileAnalysisFormatting.Size(Length);
    public string LastWriteTimeDisplay=>LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
}
public sealed record DuplicateFileGroup(string Sha256,long Length,IReadOnlyList<FileAnalysisEntry> Files)
{
    public decimal PotentialDuplicateBytes=>(decimal)Length*(Files.Count-1);
    public string SizeDisplay=>FileAnalysisFormatting.Size(Length);
    public string PotentialDuplicateSizeDisplay=>FileAnalysisFormatting.Size(PotentialDuplicateBytes);
    public string PathsDisplay=>string.Join(Environment.NewLine,Files.Select(file=>file.Path));
}
public static class FileAnalysisFormatting
{
    public static string Size(decimal bytes)
    {
        var size=bytes;string[] units=["B","KiB","MiB","GiB","TiB","PiB"];
        var index=0;while(size>=1024&&index<units.Length-1){size/=1024;index++;}
        return $"{size:0.##} {units[index]}";
    }
}
public sealed record FileAnalysisIssue(string Path,string ErrorCode,string Message);
public sealed record FileAnalysisProgress(int FilesExamined,long BytesHashed,string CurrentPath);
public sealed record FileAnalysisResult(
    string Root,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    long DurationMilliseconds,
    int FilesExamined,
    int FilesHashed,
    long BytesHashed,
    int Skipped,
    bool Truncated,
    bool Cancelled,
    IReadOnlyList<DuplicateFileGroup> Duplicates,
    IReadOnlyList<FileAnalysisEntry> LargeFiles,
    IReadOnlyList<FileAnalysisIssue> Issues)
{
    public bool CoverageComplete=>!Truncated&&!Cancelled&&Skipped==0;
}

public static class FileAnalysis
{
    public const int MaximumFiles=100_000;
    // Bound worst-case disk IO even when a folder contains many large same-size files.
    public const long MaximumHashBytes=2L*1024*1024*1024;
    private const int MaximumIssues=100;

    public static async Task<FileAnalysisResult> ScanAsync(
        string root,
        long minimumLargeFileBytes,
        IProgress<FileAnalysisProgress>? progress,
        CancellationToken token)=>await ScanAsync(root,minimumLargeFileBytes,MaximumHashBytes,progress,token);

    internal static async Task<FileAnalysisResult> ScanAsync(
        string root,
        long minimumLargeFileBytes,
        long maximumHashBytes,
        IProgress<FileAnalysisProgress>? progress,
        CancellationToken token)
    {
        if(minimumLargeFileBytes<=0)throw new HbxException("HBX-FILES-0001: Large-file threshold must be positive.");
        if(maximumHashBytes<=0)throw new HbxException("HBX-FILES-0007: Hash-read budget must be positive.");
        var fullRoot=ValidateRoot(root);
        var started=DateTimeOffset.UtcNow;var timer=Stopwatch.StartNew();
        var directories=new Stack<DirectoryInfo>();directories.Push(new DirectoryInfo(fullRoot));
        var candidates=new Dictionary<long,List<FileAnalysisEntry>>();
        var duplicateBuckets=new Dictionary<string,List<FileAnalysisEntry>>(StringComparer.Ordinal);
        var largeFiles=new List<FileAnalysisEntry>();var issues=new List<FileAnalysisIssue>();
        var filesExamined=0;var filesHashed=0;var skipped=0;long bytesHashed=0;var truncated=false;var cancelled=false;

        void AddIssue(string path,string code,Exception exception)
        {
            skipped++;
            if(issues.Count<MaximumIssues)issues.Add(new(path,code,ReportExport.Redact(exception.Message)));
        }

        try
        {
            while(directories.Count>0)
            {
                token.ThrowIfCancellationRequested();
                var directory=directories.Pop();
                try
                {
                    foreach(var entry in directory.EnumerateFileSystemInfos())
                    {
                        token.ThrowIfCancellationRequested();
                        FileAttributes attributes;
                        try{attributes=entry.Attributes;}
                        catch(UnauthorizedAccessException exception){AddIssue(entry.FullName,"HBX-FILES-ACCESS",exception);continue;}
                        catch(IOException exception){AddIssue(entry.FullName,"HBX-FILES-IO",exception);continue;}
                        catch(SecurityException exception){AddIssue(entry.FullName,"HBX-FILES-SECURITY",exception);continue;}

                        if((attributes&FileAttributes.ReparsePoint)!=0)
                        {
                            skipped++;
                            continue;
                        }
                        if((attributes&FileAttributes.Directory)!=0)
                        {
                            directories.Push(new DirectoryInfo(entry.FullName));
                            continue;
                        }
                        if((attributes&FileAttributes.Device)!=0)continue;
                        if(filesExamined>=MaximumFiles)
                        {
                            truncated=true;directories.Clear();break;
                        }

                        try
                        {
                            entry.Refresh();
                            if(!entry.Exists)continue;
                            var item=new FileAnalysisEntry(entry.FullName,new FileInfo(entry.FullName).Length,new DateTimeOffset(entry.LastWriteTimeUtc,TimeSpan.Zero));
                            filesExamined++;
                            if(item.Length>=minimumLargeFileBytes)largeFiles.Add(item);
                            if(!candidates.TryGetValue(item.Length,out var sameSize))candidates[item.Length]=sameSize=[];
                            sameSize.Add(item);
                        }
                        catch(UnauthorizedAccessException exception){AddIssue(entry.FullName,"HBX-FILES-ACCESS",exception);}
                        catch(IOException exception){AddIssue(entry.FullName,"HBX-FILES-IO",exception);}
                        catch(SecurityException exception){AddIssue(entry.FullName,"HBX-FILES-SECURITY",exception);}
                    }
                }
                catch(UnauthorizedAccessException exception){AddIssue(directory.FullName,"HBX-FILES-ACCESS",exception);}
                catch(IOException exception){AddIssue(directory.FullName,"HBX-FILES-IO",exception);}
                catch(SecurityException exception){AddIssue(directory.FullName,"HBX-FILES-SECURITY",exception);}
            }

            long hashBytesScheduled=0;
            foreach(var sameSize in candidates.Values.Where(group=>group.Count>1))
            {
                foreach(var candidate in sameSize)
                {
                    token.ThrowIfCancellationRequested();
                    if(candidate.Length>maximumHashBytes-hashBytesScheduled)
                    {
                        truncated=true;
                        goto HashingComplete;
                    }
                    hashBytesScheduled=checked(hashBytesScheduled+candidate.Length);
                    try
                    {
                        var info=new FileInfo(candidate.Path);info.Refresh();
                        if(!info.Exists||info.Length!=candidate.Length||info.LastWriteTimeUtc!=candidate.LastWriteTimeUtc.UtcDateTime)
                        {
                            AddIssue(candidate.Path,"HBX-FILES-CHANGED",new IOException("File metadata changed during the scan."));
                            continue;
                        }
                        await using var stream=new FileStream(candidate.Path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete,131072,FileOptions.Asynchronous|FileOptions.SequentialScan);
                        if(stream.Length!=candidate.Length)
                        {
                            AddIssue(candidate.Path,"HBX-FILES-CHANGED",new IOException("File length changed before hashing."));
                            continue;
                        }
                        var hash=Convert.ToHexString(await SHA256.HashDataAsync(stream,token));
                        info.Refresh();
                        if(!info.Exists||info.Length!=candidate.Length||info.LastWriteTimeUtc!=candidate.LastWriteTimeUtc.UtcDateTime)
                        {
                            AddIssue(candidate.Path,"HBX-FILES-CHANGED",new IOException("File metadata changed while hashing."));
                            continue;
                        }
                        if(!duplicateBuckets.TryGetValue(hash,out var matches))duplicateBuckets[hash]=matches=[];
                        matches.Add(candidate);filesHashed++;bytesHashed=checked(bytesHashed+candidate.Length);
                        progress?.Report(new(filesExamined,bytesHashed,candidate.Path));
                    }
                    catch(OperationCanceledException){throw;}
                    catch(UnauthorizedAccessException exception){AddIssue(candidate.Path,"HBX-FILES-ACCESS",exception);}
                    catch(IOException exception){AddIssue(candidate.Path,"HBX-FILES-IO",exception);}
                    catch(SecurityException exception){AddIssue(candidate.Path,"HBX-FILES-SECURITY",exception);}
                }
            }
            HashingComplete:
            timer.Stop();
            var duplicates=ToDuplicateGroups(duplicateBuckets);
            progress?.Report(new(filesExamined,bytesHashed,""));
            return new(fullRoot,started,DateTimeOffset.UtcNow,timer.ElapsedMilliseconds,filesExamined,filesHashed,bytesHashed,skipped,truncated,false,duplicates,
                largeFiles.OrderByDescending(item=>item.Length).ThenBy(item=>item.Path,StringComparer.OrdinalIgnoreCase).ToArray(),issues.AsReadOnly());
        }
        catch(OperationCanceledException) when(token.IsCancellationRequested)
        {
            cancelled=true;timer.Stop();
            return new(fullRoot,started,DateTimeOffset.UtcNow,timer.ElapsedMilliseconds,filesExamined,filesHashed,bytesHashed,skipped,truncated,cancelled,
                ToDuplicateGroups(duplicateBuckets),largeFiles.OrderByDescending(item=>item.Length).ThenBy(item=>item.Path,StringComparer.OrdinalIgnoreCase).ToArray(),issues.AsReadOnly());
        }
    }

    private static DuplicateFileGroup[] ToDuplicateGroups(Dictionary<string,List<FileAnalysisEntry>> buckets)=>
        buckets.Where(pair=>pair.Value.Count>1)
            .Select(pair=>new DuplicateFileGroup(pair.Key,pair.Value[0].Length,pair.Value.OrderBy(item=>item.Path,StringComparer.OrdinalIgnoreCase).ToArray()))
            .OrderByDescending(group=>group.Length).ToArray();

    private static string ValidateRoot(string root)
    {
        if(string.IsNullOrWhiteSpace(root))throw new HbxException("HBX-FILES-0002: Choose a local folder to scan.");
        if(!Path.IsPathRooted(root.Trim()))throw new HbxException("HBX-FILES-0003: Choose an absolute local folder path.");
        string full;
        try{full=Path.GetFullPath(root.Trim());}
        catch(Exception exception) when(exception is ArgumentException or IOException or NotSupportedException)
        {throw new HbxException("HBX-FILES-0003: The selected folder path is invalid.");}
        if(!Path.IsPathRooted(full)||full.StartsWith(@"\\",StringComparison.Ordinal)||full.StartsWith(@"\\?\",StringComparison.Ordinal))
            throw new HbxException("HBX-FILES-0004: Network, device, and extended paths are not scanned.");
        var volume=Path.GetPathRoot(full);
        if(volume is null||volume.Length<3||volume[1]!=':')
            throw new HbxException("HBX-FILES-0004: Only local drive folders are supported.");
        var directory=new DirectoryInfo(full);
        if(!directory.Exists)throw new HbxException("HBX-FILES-0005: The selected folder does not exist.");
        if((directory.Attributes&FileAttributes.ReparsePoint)!=0)
            throw new HbxException("HBX-FILES-0006: Reparse-point folders are not scanned.");
        return full;
    }
}

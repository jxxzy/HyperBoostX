using System.Security.Cryptography;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public sealed record CleanupCandidate(string Path,long Bytes,DateTime LastModified)
{
    public string Size=>$"{Bytes/1048576d:F2} MiB";
    public override string ToString()=>Path;
}
public sealed record FileStamp(long Length,long LastWriteTicks,int Attributes,string Sha256);
public sealed record CleanupScan(IReadOnlyList<CleanupCandidate> Items,int Examined,int Skipped,bool Truncated);
public sealed class TempCleanupAction:ISystemAction
{
    private readonly string _tempRoot,_quarantine;
    public TempCleanupAction(string tempRoot,string dataRoot)
    {
        _tempRoot=SafePaths.Local(tempRoot).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        _quarantine=SafePaths.Under(dataRoot,"Quarantine");Directory.CreateDirectory(_quarantine);
    }
    public ActionInfo Info {get;}=new("storage.temp.quarantine","Quarantine selected old temporary file","Cleanup",
        "Move one selected .tmp file older than 7 days into local recovery quarantine. Contents are preserved, not erased. Restore verifies SHA-256 and never overwrites a new file. Quarantine does not reclaim disk space until separately managed.",
        "Current user's temporary folder","Moderate · reversible","None","Native filesystem + SHA-256");
    public void Validate(string target)
    {
        var path=SafePaths.Local(target);
        if(!path.StartsWith(_tempRoot,StringComparison.OrdinalIgnoreCase)||!path.EndsWith(".tmp",StringComparison.OrdinalIgnoreCase)||Directory.Exists(path))
            throw new HbxException("HBX-CLEAN-0001: Only files ending in .tmp inside the configured user temporary folder are supported.");
        if(!string.Equals(Path.GetPathRoot(path),Path.GetPathRoot(_quarantine),StringComparison.OrdinalIgnoreCase))throw new HbxException("HBX-CLEAN-0002: Reversible cleanup requires temp and recovery storage on the same drive.");
    }
    private string Backup(string target)=>SafePaths.Under(_quarantine,Json.Hash(Path.GetFullPath(target).ToUpperInvariant())+".bin");
    private static FileStamp Stamp(string path,FileStream stream)=>new(stream.Length,File.GetLastWriteTimeUtc(path).Ticks,(int)File.GetAttributes(path),Convert.ToHexString(SHA256.HashData(stream)));
    private static FileStream OpenExclusive(string path)=>new(path,FileMode.Open,FileAccess.Read,FileShare.Delete);
    private static void Eligible(string path,FileStamp stamp)
    {
        if(stamp.Length>128L*1024*1024||new DateTime(stamp.LastWriteTicks,DateTimeKind.Utc)>DateTime.UtcNow.AddDays(-7)||((FileAttributes)stamp.Attributes&(FileAttributes.System|FileAttributes.ReadOnly|FileAttributes.Encrypted))!=0)
            throw new HbxException("HBX-CLEAN-0003: File is recent, protected, read-only, encrypted, or larger than 128 MiB.");
    }
    public Task<State> ReadAsync(string target,CancellationToken token)
    {
        Validate(target);token.ThrowIfCancellationRequested();
        if(File.Exists(target)){using var stream=OpenExclusive(target);return Task.FromResult(new State(true,"TempFile",Json.Write(Stamp(target,stream))));}
        var backup=Backup(target);
        if(File.Exists(backup)){using var stream=OpenExclusive(backup);return Task.FromResult(new State(false,"QuarantinedFile",Json.Write(Stamp(backup,stream))));}
        return Task.FromResult(new State(false,"MissingFile",""));
    }
    public State Desired(string target,State before)
    {
        if(!before.Exists)throw new HbxException("HBX-CLEAN-0004: Temporary file no longer exists.");
        if(File.Exists(Backup(target)))throw new HbxException("HBX-CLEAN-0005: An older quarantine backup exists. Resolve its Restore record first.");
        Eligible(target,Json.Read<FileStamp>(before.Value));return new(false,"QuarantinedFile",before.Value);
    }
    public Task WriteAsync(string target,State state,CancellationToken token)
    {
        Validate(target);token.ThrowIfCancellationRequested();var backup=Backup(target);
        var source=state.Exists?backup:target;var destination=state.Exists?target:backup;
        if((state.Exists&&state.Kind!="TempFile")||(!state.Exists&&state.Kind!="QuarantinedFile"))throw new HbxException("HBX-CLEAN-0006: Invalid cleanup state.");
        using var stream=OpenExclusive(source);var actual=Stamp(source,stream);
        if(actual!=Json.Read<FileStamp>(state.Value))throw new HbxException("HBX-CLEAN-0007: File contents or metadata changed; operation blocked.");
        if(!state.Exists)Eligible(target,actual);
        SafePaths.Local(source);SafePaths.Local(destination);
        // The open handle denies writes while allowing our same-volume rename. Never overwrite.
        File.Move(source,destination,false);return Task.CompletedTask;
    }
    public CleanupScan Scan(CancellationToken token)
    {
        var result=new List<CleanupCandidate>();var queue=new Queue<(string Path,int Depth)>();queue.Enqueue((_tempRoot,0));var examined=0;var skipped=0;
        while(queue.TryDequeue(out var folder)&&examined<10000&&result.Count<200)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                SafePaths.Local(folder.Path);
                foreach(var path in Directory.EnumerateFileSystemEntries(folder.Path))
                {
                    token.ThrowIfCancellationRequested();if(++examined>10000||result.Count>=200)break;
                    try
                    {
                        var attributes=File.GetAttributes(path);if((attributes&FileAttributes.ReparsePoint)!=0)continue;
                        if((attributes&FileAttributes.Directory)!=0){if(folder.Depth<4)queue.Enqueue((path,folder.Depth+1));continue;}
                        if(!path.EndsWith(".tmp",StringComparison.OrdinalIgnoreCase))continue;
                        Validate(path);var file=new FileInfo(path);Eligible(path,new(file.Length,file.LastWriteTimeUtc.Ticks,(int)attributes,""));
                        if(!File.Exists(Backup(path)))result.Add(new(path,file.Length,file.LastWriteTime));
                    }
                    catch(Exception e)when(e is IOException or UnauthorizedAccessException or HbxException){skipped++;}
                }
            }
            catch(Exception e)when(e is IOException or UnauthorizedAccessException or HbxException){skipped++;}
        }
        return new(result.OrderByDescending(f=>f.Bytes).ToArray(),examined,skipped,examined>=10000||result.Count>=200);
    }
}

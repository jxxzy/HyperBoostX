using System.IO;
using HyperBoostX.Core;
using HyperBoostX.Windows;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class CleanupTests:IDisposable
{
    private readonly string _root=Path.Combine(Path.GetTempPath(),"HyperBoostX-tests",Guid.NewGuid().ToString("N"));
    private readonly TempCleanupAction _action;
    private readonly Journal _journal;
    private readonly ActionEngine _engine;
    private string Temp=>Path.Combine(_root,"temp");
    public CleanupTests()
    {
        Directory.CreateDirectory(Temp);_journal=new Journal(Path.Combine(_root,"data"),new WindowsProtector());
        _action=new TempCleanupAction(Temp,_journal.Root);_engine=new ActionEngine([_action],_journal,new SafetyGuard(()=>null),"test-owner");
    }
    private string FileFixture(string name="fixture.tmp",int ageDays=10)
    {
        var path=Path.Combine(Temp,name);File.WriteAllText(path,"Fixture content\n");File.SetLastWriteTimeUtc(path,DateTime.UtcNow.AddDays(-ageDays));return path;
    }
    [Fact]public async Task RealFileQuarantineAndRestorePreserveBytesAndTimestamp()
    {
        var path=FileFixture();var bytes=File.ReadAllBytes(path);var time=File.GetLastWriteTimeUtc(path);
        var preview=await _engine.PreviewAsync(_action.Info.Id,path,default);var applied=await _engine.ApplyAsync(preview.Token,true,null,default);
        Assert.Equal("Verified",applied.Status);Assert.False(File.Exists(path));Assert.Single(Directory.GetFiles(Path.Combine(_journal.Root,"Quarantine")));
        var restored=await _engine.RollbackAsync(applied.Id,true,default);Assert.Equal("RolledBack",restored.Status);
        Assert.Equal(bytes,File.ReadAllBytes(path));Assert.Equal(time,File.GetLastWriteTimeUtc(path));
    }
    [Fact]public async Task ChangedFileAfterPreviewIsPreserved()
    {
        var path=FileFixture();var preview=await _engine.PreviewAsync(_action.Info.Id,path,default);File.WriteAllText(path,"New content");
        await Assert.ThrowsAsync<HbxException>(()=>_engine.ApplyAsync(preview.Token,true,null,default));Assert.Equal("New content",File.ReadAllText(path));
    }
    [Fact]public async Task RestoreNeverOverwritesNewFileAtOriginalPath()
    {
        var path=FileFixture();var preview=await _engine.PreviewAsync(_action.Info.Id,path,default);var result=await _engine.ApplyAsync(preview.Token,true,null,default);
        File.WriteAllText(path,"External file");Assert.Equal("ConflictDetected",(await _engine.RollbackAsync(result.Id,true,default)).Status);Assert.Equal("External file",File.ReadAllText(path));
    }
    [Fact]public async Task DamagedQuarantineCannotBeRestoredAsOriginal()
    {
        var path=FileFixture();var preview=await _engine.PreviewAsync(_action.Info.Id,path,default);var result=await _engine.ApplyAsync(preview.Token,true,null,default);
        File.WriteAllText(Directory.GetFiles(Path.Combine(_journal.Root,"Quarantine")).Single(),"Damaged");
        Assert.Equal("ConflictDetected",(await _engine.RollbackAsync(result.Id,true,default)).Status);Assert.False(File.Exists(path));
    }
    [Fact]public async Task RecentAndNonTemporaryAndOutsideFilesAreRejected()
    {
        await Assert.ThrowsAsync<HbxException>(()=>_engine.PreviewAsync(_action.Info.Id,FileFixture(ageDays:1),default));
        await Assert.ThrowsAsync<HbxException>(()=>_engine.PreviewAsync(_action.Info.Id,FileFixture("document.txt"),default));
        await Assert.ThrowsAsync<HbxException>(()=>_engine.PreviewAsync(_action.Info.Id,Path.Combine(_root,"outside.tmp"),default));
    }
    [Fact]public void ScanReportsOnlyEligibleFilesAndCancellationIsHonored()
    {
        var old=FileFixture("old.tmp");FileFixture("recent.tmp",1);FileFixture("personal.txt");
        var scan=_action.Scan(default);Assert.Equal(old,scan.Items.Single().Path);Assert.True(scan.Skipped>=1);
        using var cts=new CancellationTokenSource();cts.Cancel();Assert.Throws<OperationCanceledException>(()=>_action.Scan(cts.Token));
    }
    [Fact]public async Task LockedFileIsNotMoved()
    {
        var path=FileFixture();using var held=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None);
        await Assert.ThrowsAsync<IOException>(()=>_engine.PreviewAsync(_action.Info.Id,path,default));Assert.True(File.Exists(path));
    }
    public void Dispose()
    {
        var allowed=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"HyperBoostX-tests"))+Path.DirectorySeparatorChar;
        if(Path.GetFullPath(_root).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(_root))Directory.Delete(_root,true);
    }
}

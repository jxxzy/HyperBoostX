using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HyperBoostX.Core;
using Xunit;

namespace HyperBoostX.Tests;

public sealed class FileAnalysisTests:IDisposable
{
    private readonly string _root=Path.Combine(Path.GetTempPath(),"HyperBoostX-tests",Guid.NewGuid().ToString("N"));
    public FileAnalysisTests()=>Directory.CreateDirectory(_root);

    [Fact]public async Task FindsContentDuplicatesAndLargeFilesWithoutChangingFiles()
    {
        var first=Path.Combine(_root,"first.bin");var second=Path.Combine(_root,"nested","second.bin");var different=Path.Combine(_root,"different.bin");
        Directory.CreateDirectory(Path.GetDirectoryName(second)!);
        var duplicateBytes=Encoding.UTF8.GetBytes("same content");
        await File.WriteAllBytesAsync(first,duplicateBytes);await File.WriteAllBytesAsync(second,duplicateBytes);
        await File.WriteAllBytesAsync(different,Encoding.UTF8.GetBytes("else content"));
        var firstTime=File.GetLastWriteTimeUtc(first);var secondTime=File.GetLastWriteTimeUtc(second);

        var result=await FileAnalysis.ScanAsync(_root,duplicateBytes.Length,null,default);

        var group=Assert.Single(result.Duplicates);
        Assert.Equal(duplicateBytes.Length,group.Length);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(duplicateBytes)),group.Sha256);
        Assert.Equal(2,group.Files.Count);
        Assert.Equal(duplicateBytes.Length,group.PotentialDuplicateBytes);
        Assert.Equal(3,result.LargeFiles.Count);
        Assert.True(result.CoverageComplete);
        Assert.Equal(3,result.FilesHashed);
        Assert.Equal(duplicateBytes.Length*3,result.BytesHashed);
        Assert.Equal(duplicateBytes,await File.ReadAllBytesAsync(first));
        Assert.Equal(duplicateBytes,await File.ReadAllBytesAsync(second));
        Assert.Equal(firstTime,File.GetLastWriteTimeUtc(first));
        Assert.Equal(secondTime,File.GetLastWriteTimeUtc(second));
    }

    [Fact]public async Task LargeFileThresholdIsAppliedWithoutHashingUniqueFiles()
    {
        var path=Path.Combine(_root,"large.bin");await File.WriteAllBytesAsync(path,new byte[16]);
        var result=await FileAnalysis.ScanAsync(_root,16,null,default);
        Assert.Equal(path,Assert.Single(result.LargeFiles).Path);
        Assert.Empty(result.Duplicates);
        Assert.Equal(0,result.FilesHashed);
        Assert.Equal(0,result.BytesHashed);
    }

    [Fact]public async Task CancellationReturnsAnExplicitIncompletePartialResult()
    {
        using var cts=new CancellationTokenSource();
        var bytes=Encoding.UTF8.GetBytes("same content");var paths=Enumerable.Range(0,3).Select(index=>Path.Combine(_root,$"{index}.bin")).ToArray();
        foreach(var path in paths)await File.WriteAllBytesAsync(path,bytes);
        var progress=new CancelAfterFirstHash(cts);

        var result=await FileAnalysis.ScanAsync(_root,1024,progress,cts.Token);

        Assert.True(result.Cancelled);
        Assert.False(result.CoverageComplete);
        Assert.Equal(1,result.FilesHashed);
        Assert.Empty(result.Duplicates);
        Assert.All(paths,path=>Assert.True(File.Exists(path)));
    }

    [Fact]public async Task HashBudgetStopsBeforeOverrunAndMarksCoverageIncomplete()
    {
        var bytes=Encoding.UTF8.GetBytes("same content");
        await File.WriteAllBytesAsync(Path.Combine(_root,"first.bin"),bytes);
        await File.WriteAllBytesAsync(Path.Combine(_root,"second.bin"),bytes);

        var result=await FileAnalysis.ScanAsync(_root,1024,bytes.Length,null,default);

        Assert.True(result.Truncated);
        Assert.False(result.CoverageComplete);
        Assert.Equal(1,result.FilesHashed);
        Assert.Equal(bytes.Length,result.BytesHashed);
        Assert.Empty(result.Duplicates);
    }

    [Fact]public async Task RejectsNonPositiveHashBudget()=>
        await Assert.ThrowsAsync<HbxException>(()=>FileAnalysis.ScanAsync(_root,1,0,null,default));

    [Theory]
    [InlineData("")]
    [InlineData("relative-folder")]
    [InlineData(@"\\server\share")]
    public async Task RejectsMissingRelativeAndNetworkRoots(string root)=>
        await Assert.ThrowsAsync<HbxException>(()=>FileAnalysis.ScanAsync(root,1,null,default));

    public void Dispose()
    {
        var allowed=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"HyperBoostX-tests"))+Path.DirectorySeparatorChar;
        if(Path.GetFullPath(_root).StartsWith(allowed,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(_root))Directory.Delete(_root,true);
    }

    private sealed class CancelAfterFirstHash(CancellationTokenSource source):IProgress<FileAnalysisProgress>
    {
        private bool _cancelled;
        public void Report(FileAnalysisProgress value)
        {
            if(!_cancelled&&value.BytesHashed>0){_cancelled=true;source.Cancel();}
        }
    }
}

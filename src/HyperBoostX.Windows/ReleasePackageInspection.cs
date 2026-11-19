using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public static class ReleasePackageInspection
{
    private static readonly string[] Extensions=[".exe",".msi",".msix",".msixbundle",".appx",".appxbundle"];
    public static string ValidatePath(string path)
    {
        if(string.IsNullOrWhiteSpace(path)||!Path.IsPathFullyQualified(path))throw new HbxException("Select an absolute local release package path.");
        var full=SafePaths.Local(path);if(full.StartsWith(@"\\",StringComparison.Ordinal))throw new HbxException("Network release packages are not inspected. Copy the file locally first.");
        if(!File.Exists(full))throw new HbxException("Release package was not found.");
        if(!Extensions.Contains(Path.GetExtension(full),StringComparer.OrdinalIgnoreCase))throw new HbxException("Supported release packages: EXE, MSI, MSIX, APPX and bundle variants.");
        if(new FileInfo(full).Length>4L*1024*1024*1024)throw new HbxException("Release package exceeds the 4 GiB inspection limit.");
        return full;
    }
    public static async Task<DiagnosticReport> InspectAsync(string path,Version currentVersion,CancellationToken token)
    {
        var full=ValidatePath(path);var info=new FileInfo(full);await using var stream=new FileStream(full,FileMode.Open,FileAccess.Read,FileShare.Read,1024*1024,FileOptions.Asynchronous|FileOptions.SequentialScan);
        var hash=Convert.ToHexString(await SHA256.HashDataAsync(stream,token));token.ThrowIfCancellationRequested();
        var trust=OperatingSystem.IsWindows()?VerifyAuthenticode(full):new(false,"Unsupported operating system");
        string signer="Unavailable";try{using var certificate=new X509Certificate2(X509Certificate.CreateFromSignedFile(full));signer=certificate.Subject;}catch(CryptographicException){}
        var fileVersion=FileVersionInfo.GetVersionInfo(full);var candidate=Version.TryParse(fileVersion.FileVersion?.Split(' ').FirstOrDefault(),out var parsed)?parsed:null;
        var comparison=candidate is null?"Unavailable":candidate>currentVersion?"Newer than current":candidate==currentVersion?"Same as current":"Older than current";
        var facts=new List<Fact>
        {
            new("Current version",currentVersion.ToString(),typeof(ReleasePackageInspection).Assembly.Location,"Direct","Available"),
            new("Candidate package",Path.GetFileName(full),full,"Direct","Available"),
            new("Package size",$"{info.Length:N0} bytes","File metadata","Direct","Available"),
            new("SHA-256",hash,"SHA256.HashDataAsync","Measured","Available"),
            new("Authenticode trust",trust.Trusted?"Trusted":$"Not trusted: {trust.Detail}","WinVerifyTrust (local cache; online revocation not verified)",trust.Trusted?"Verified":"Unavailable",trust.Trusted?"Available":"Unavailable"),
            new("Signer",signer,"Authenticode certificate",signer=="Unavailable"?"Unavailable":"Direct",signer=="Unavailable"?"Unavailable":"Available"),
            new("Candidate version",candidate?.ToString()??"Unavailable","File version metadata",candidate is null?"Unavailable":"Direct",candidate is null?"Unavailable":"Available"),
            new("Version comparison",comparison,"Candidate vs current assembly version",candidate is null?"Unavailable":"Calculated",candidate is null?"Unavailable":"Available"),
            new("Execution policy","Inspection only. HyperBoostX did not execute, install, replace, download, or delete this package.","Update safety policy","Guidance","Available"),
        };
        return new("Update Center",DateTimeOffset.UtcNow,facts,trust.Trusted?[]:[new Finding("Package is not trusted by Windows",trust.Detail,"Do not install it. Obtain a signed package from the project owner's authenticated release channel.","Windows WinVerifyTrust result")]);
    }
    public static Task<DiagnosticReport> InspectCurrentAsync(CancellationToken token)=>InspectAsync(Environment.ProcessPath??throw new HbxException("Current executable path is unavailable."),typeof(ReleasePackageInspection).Assembly.GetName().Version??new Version(0,0),token);

    public static (bool Trusted,string Detail) VerifyAuthenticode(string path)
    {
        var fileInfo=new WinTrustFileInfo(path);var data=new WinTrustData(fileInfo);var action=WinTrustActionGenericVerifyV2;
        try{var result=WinVerifyTrust(IntPtr.Zero,action,data);return result==0?(true,"Windows trust verification succeeded (local cache; online revocation not verified)"):(false,$"WINTRUST 0x{result:X8}");}
        finally{data.StateAction=WinTrustDataStateAction.Close;WinVerifyTrust(IntPtr.Zero,action,data);data.Dispose();fileInfo.Dispose();}
    }
    private static readonly Guid WinTrustActionGenericVerifyV2=new("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");
    [DllImport("wintrust.dll",ExactSpelling=true,SetLastError=true)]private static extern uint WinVerifyTrust(IntPtr hwnd,[MarshalAs(UnmanagedType.LPStruct)]Guid actionId,WinTrustData data);
    private enum WinTrustDataUIChoice:uint{None=2} private enum WinTrustDataRevocationChecks:uint{None=0} private enum WinTrustDataChoice:uint{File=1} private enum WinTrustDataStateAction:uint{Ignore=0,Verify=1,Close=2} private enum WinTrustDataProvFlags:uint{Safer=0x100,CacheOnlyUrlRetrieval=0x1000}
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]private sealed class WinTrustFileInfo:IDisposable
    {
        public uint StructSize=(uint)Marshal.SizeOf<WinTrustFileInfo>();public IntPtr FilePath;public IntPtr FileHandle=IntPtr.Zero;public IntPtr KnownSubject=IntPtr.Zero;
        public WinTrustFileInfo(string path)=>FilePath=Marshal.StringToCoTaskMemUni(path);public void Dispose(){if(FilePath!=IntPtr.Zero){Marshal.FreeCoTaskMem(FilePath);FilePath=IntPtr.Zero;}}
    }
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]private sealed class WinTrustData:IDisposable
    {
        public uint StructSize=(uint)Marshal.SizeOf<WinTrustData>();public IntPtr PolicyCallbackData=IntPtr.Zero;public IntPtr SIPClientData=IntPtr.Zero;public WinTrustDataUIChoice UIChoice=WinTrustDataUIChoice.None;public WinTrustDataRevocationChecks RevocationChecks=WinTrustDataRevocationChecks.None;public WinTrustDataChoice UnionChoice=WinTrustDataChoice.File;public IntPtr FileInfoPtr;public WinTrustDataStateAction StateAction=WinTrustDataStateAction.Verify;public IntPtr StateData=IntPtr.Zero;public IntPtr URLReference=IntPtr.Zero;public WinTrustDataProvFlags ProvFlags=WinTrustDataProvFlags.Safer|WinTrustDataProvFlags.CacheOnlyUrlRetrieval;public uint UIContext=0;
        public WinTrustData(WinTrustFileInfo file){FileInfoPtr=Marshal.AllocCoTaskMem(Marshal.SizeOf<WinTrustFileInfo>());Marshal.StructureToPtr(file,FileInfoPtr,false);}public void Dispose(){if(FileInfoPtr!=IntPtr.Zero){Marshal.FreeCoTaskMem(FileInfoPtr);FileInfoPtr=IntPtr.Zero;}}
    }
}

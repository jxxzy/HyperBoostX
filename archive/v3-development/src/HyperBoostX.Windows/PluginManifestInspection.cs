using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using HyperBoostX.Core;

namespace HyperBoostX.Windows;

public sealed record PluginManifest(int SchemaVersion,string Id,string Version,string MinimumAppVersion,string Publisher,string EntryAssembly,string Sha256,string[] Permissions);

public static class PluginManifestInspection
{
    private static readonly HashSet<string> AllowedPermissions=new(["Diagnostics.Read","Reports.Write","Notifications.Local"],StringComparer.Ordinal);
    private static readonly JsonSerializerOptions StrictJson=new(){PropertyNameCaseInsensitive=false,UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow};
    public static Task<DiagnosticReport> ScanAsync(string root,Version appVersion,CancellationToken token)=>Task.Run(()=>
    {
        Directory.CreateDirectory(root);var facts=new List<Fact>();var findings=new List<Finding>();var directories=Directory.EnumerateDirectories(root).Take(101).ToArray();
        if(directories.Length>100)facts.Add(new("Plugin inventory coverage","Truncated at 100 plugin directories",root,"Unavailable","Unavailable"));
        foreach(var directory in directories.Take(100))
        {
            token.ThrowIfCancellationRequested();var manifestPath=Path.Combine(directory,"plugin.json");if(!File.Exists(manifestPath)){facts.Add(new(Path.GetFileName(directory),"Rejected: plugin.json missing",directory,"Unavailable","Unavailable"));continue;}
            try
            {
                var manifestInfo=new FileInfo(manifestPath);if(manifestInfo.Length>64*1024)throw new HbxException("Manifest exceeds 64 KiB.");
                var manifest=JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(manifestPath),StrictJson)??throw new HbxException("Manifest is empty.");Validate(manifest);
                var assemblyPath=SafeAssemblyPath(directory,manifest.EntryAssembly);var actualHash=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assemblyPath)));var hashMatches=string.Equals(actualHash,manifest.Sha256,StringComparison.OrdinalIgnoreCase);
                var compatible=Version.TryParse(manifest.MinimumAppVersion,out var minimum)&&appVersion>=minimum;var trust=ReleasePackageInspection.VerifyAuthenticode(assemblyPath);var permissionsValid=manifest.Permissions.All(AllowedPermissions.Contains);
                var status=hashMatches&&compatible&&trust.Trusted&&permissionsValid?"Trusted manifest (review only)":"Rejected";
                facts.Add(new(manifest.Id,$"{status} · version {manifest.Version} · publisher {manifest.Publisher} · compatible {compatible} · hash {hashMatches} · Authenticode {trust.Trusted} · permissions {(manifest.Permissions.Length==0?"none":string.Join(", ",manifest.Permissions))}",manifestPath,hashMatches&&compatible&&trust.Trusted&&permissionsValid?"Verified":"Unavailable",hashMatches&&compatible&&trust.Trusted&&permissionsValid?"Available":"Unavailable"));
                facts.Add(new(manifest.Id+" activity","Never executed by HyperBoostX; manifest review only",assemblyPath,"Direct","Available"));
                if(status=="Rejected")findings.Add(new($"Plugin rejected: {manifest.Id}",$"compatible={compatible}, hash={hashMatches}, Authenticode={trust.Detail}, permissions={permissionsValid}","Do not load this plugin. Obtain a compatible signed package whose manifest hash and allowlisted permissions validate.","Fail-closed manifest inspection"));
            }
            catch(Exception exception) when(exception is IOException or UnauthorizedAccessException or JsonException or HbxException or CryptographicException)
            {facts.Add(new(Path.GetFileName(directory),$"Rejected: {ReportExport.Redact(exception.Message)}",manifestPath,"Unavailable","Unavailable"));}
        }
        if(!directories.Any())facts.Add(new("Plugin inventory","No plugin packages found",root,"Direct","Available"));
        facts.Insert(0,new("Plugin execution policy","Disabled. This build reviews manifests, compatibility, permissions, hashes and Authenticode trust but never loads plugin code.","Fail-closed plugin policy","Guidance","Available"));
        return new DiagnosticReport("Plugin Manager",DateTimeOffset.UtcNow,facts,findings);
    },token);
    private static void Validate(PluginManifest manifest)
    {
        if(manifest.SchemaVersion!=1)throw new HbxException("Unsupported plugin manifest schema.");
        foreach(var (value,name,max) in new[]{(manifest.Id,"id",100),(manifest.Version,"version",40),(manifest.MinimumAppVersion,"minimum version",40),(manifest.Publisher,"publisher",160),(manifest.EntryAssembly,"entry assembly",160),(manifest.Sha256,"SHA-256",64)})
            if(string.IsNullOrWhiteSpace(value)||value.Length>max||value.Any(char.IsControl))throw new HbxException($"Invalid plugin {name}.");
        if(!Version.TryParse(manifest.Version,out _)||!Version.TryParse(manifest.MinimumAppVersion,out _))throw new HbxException("Plugin versions are invalid.");
        if(manifest.Sha256.Length!=64||!manifest.Sha256.All(Uri.IsHexDigit))throw new HbxException("Plugin SHA-256 is invalid.");
        if(manifest.Permissions.Length>16||manifest.Permissions.Any(permission=>!AllowedPermissions.Contains(permission)))throw new HbxException("Plugin requests a permission outside the allowlist.");
    }
    private static string SafeAssemblyPath(string directory,string relative)
    {
        if(Path.IsPathFullyQualified(relative)||Path.GetExtension(relative).Equals(".dll",StringComparison.OrdinalIgnoreCase)==false)throw new HbxException("Plugin entry assembly must be a relative DLL path.");
        var root=Path.GetFullPath(directory)+Path.DirectorySeparatorChar;var full=Path.GetFullPath(Path.Combine(directory,relative));if(!full.StartsWith(root,StringComparison.OrdinalIgnoreCase)||!File.Exists(full))throw new HbxException("Plugin entry assembly is missing or outside its package.");
        if((File.GetAttributes(full)&FileAttributes.ReparsePoint)!=0)throw new HbxException("Plugin entry assembly cannot be a reparse point.");if(new FileInfo(full).Length>256L*1024*1024)throw new HbxException("Plugin assembly exceeds 256 MiB.");return full;
    }
}

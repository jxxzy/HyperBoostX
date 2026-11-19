using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HyperBoostX.Core;

public static class Json
{
    public static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public static string Write<T>(T value) => JsonSerializer.Serialize(value, Options);
    public static T Read<T>(string value) => JsonSerializer.Deserialize<T>(value, Options) ?? throw new InvalidDataException("HBX-DATA-0001: Empty document.");
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
public sealed record State(bool Exists, string Kind, string Value)
{
    public string Fingerprint => Json.Hash($"{Exists}\n{Kind}\n{Value}");
    public override string ToString() => Exists ? $"{Value} ({Kind})" : "Value absent";
}
public sealed record ActionInfo(string Id, string Name, string Center, string Description, string Scope,
    string Risk, string Restart, string Source, bool Reversible = true)
{
    public override string ToString()=>Name;
}
public interface ISystemAction
{
    ActionInfo Info { get; }
    void Validate(string target);
    // Recovery must validate the saved state, not require the old apply target to still exist.
    void ValidateRestore(string target, State before) => Validate(target);
    Task<State> ReadAsync(string target, CancellationToken token);
    State Desired(string target, State before);
    Task WriteAsync(string target, State state, CancellationToken token);
}
public sealed record Preview(string ActionId, string Target, State Before, State Desired, string Token,
    DateTimeOffset CreatedAt, string Description);
public sealed record Change
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Session { get; init; } = Guid.NewGuid().ToString("N");
    public string ActionId { get; init; } = "";
    public string Target { get; init; } = "";
    public State Before { get; init; } = new(false, "", "");
    public State Desired { get; init; } = new(false, "", "");
    public State? Observed { get; init; }
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; init; }
    public string Status { get; init; } = "IntentPersisted";
    public string Message { get; init; } = "";
    public string Owner { get; init; } = "";
    public string WindowsVersion { get; init; } = Environment.OSVersion.VersionString;
    public bool Recoverable => Status is "Verified" or "Applying" or "IntentPersisted" or "RecoveryRequired" or "ConflictDetected" or "RollbackFailed" or "RollingBack" or "RollbackCancelled";
    public bool NeedsRecovery => Status is "Applying" or "IntentPersisted" or "RecoveryRequired" or "ConflictDetected" or "RollbackFailed" or "RollingBack" or "RollbackCancelled";
}
public interface IStateProtector
{
    byte[] Protect(byte[] data);
    byte[] Unprotect(byte[] data);
}
public sealed record Fact(string Name, string Value, string Source, string Quality = "Direct", string Availability = "Available");
public sealed record Finding(string Title, string Evidence, string Recommendation, string Confidence, string? ActionId = null);
public sealed record SafeTestResult(string TestId,string Name,string Result,DateTimeOffset StartedAt,long DurationMilliseconds,string Evidence,string? ErrorCode=null);
public sealed record SafeTestRun(string Id,DateTimeOffset StartedAt,long DurationMilliseconds,IReadOnlyList<SafeTestResult> Results)
{
    public bool Cancelled{get;init;}
    public string DisplayName=>$"{StartedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss} · {Results.Count} checks";
    public string Summary=>Results.Count(result=>result.Result=="PASS")+" PASS · "+Results.Count(result=>result.Result=="FAIL")+" FAIL · "+Results.Count(result=>result.Result=="BLOCKED")+" BLOCKED"+(Cancelled?" · CANCELLED":"");
}
public sealed record UserSettingsDocument(int SchemaVersion,string Theme,string Goal,string Mode,string Language,bool SidebarCollapsed);
public sealed record DiagnosticReport(string Center, DateTimeOffset ObservedAt, IReadOnlyList<Fact> Facts, IReadOnlyList<Finding> Findings)
{
    public string DisplayName=>$"{Center} · {ObservedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
    public override string ToString()=>DisplayName;
    public string Summary => $"{Facts.Count} observations · {Findings.Count} findings · {ObservedAt.ToLocalTime():HH:mm:ss}";
}
public sealed class HbxException(string message) : Exception(message);

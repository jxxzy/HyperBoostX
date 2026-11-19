param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

function Read-Text([string]$RelativePath) {
    $path = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $script:failures.Add("Missing file: $RelativePath")
        return ""
    }
    return Get-Content -LiteralPath $path -Raw
}

$aiService = Read-Text "wpf\Services\NvidiaCopilotService.cs"
$redactor = Read-Text "wpf\Services\SensitiveTextRedactor.cs"
$releaseRedactor = Read-Text "scripts\redact_release_evidence.ps1"

foreach ($token in @("AiSecretRedactor", "RedactSecret", "nvapi-", "sk-", "Bearer", "[REDACTED]")) {
    if ($aiService -notmatch [regex]::Escape($token)) {
        $failures.Add("AI service redaction missing token: $token")
    }
}

foreach ($token in @("SensitiveTextRedactor", "api", "token", "secret")) {
    if ($redactor -notmatch [regex]::Escape($token)) {
        $failures.Add("WPF sensitive text redactor missing token: $token")
    }
}

foreach ($token in @("REDACTED_TOKEN", "REDACTED_SECRET", "api[_-]?key", "Bearer")) {
    if ($releaseRedactor -notmatch [regex]::Escape($token)) {
        $failures.Add("Release evidence redactor missing token: $token")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: AI redaction verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: AI redaction verification" -ForegroundColor Green

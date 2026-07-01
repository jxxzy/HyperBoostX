[CmdletBinding()]
param(
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $scriptDir "..")).Path
}

$scanFiles = @()
foreach ($pattern in @(
    "docs\runtime-audit\*.json",
    "docs\runtime-audit\*.md",
    "docs\OWNER_ADMIN_STABLE_GATE_RESULT_v2.10.0.md",
    "docs\QA_FULL_TEST_REPORT.md"
)) {
    $scanFiles += @(Get-ChildItem -Path (Join-Path $RepoRoot $pattern) -File -ErrorAction SilentlyContinue)
}

$patterns = [ordered]@{
    "raw repo path" = 'F:\\\\?BOOSTER|F:\\BOOSTER'
    "raw user profile" = 'C:\\\\?Users\\\\?jxxzy|C:\\Users\\jxxzy'
    "raw OneDrive desktop" = 'OneDrive\\\\?Desktop|OneDrive\\Desktop'
    "raw local user principal" = '(?i)desktop-[a-z0-9-]+\\jxxzy'
    "GitHub token" = 'ghp_[A-Za-z0-9_]{20,}|github_pat_[A-Za-z0-9_]{20,}'
    "OpenAI/API secret" = 'sk-[A-Za-z0-9_-]{20,}|(?i)\b(api[_-]?key|secret|license[_-]?key)"?\s*[:=]\s*"[^"\r\n]{8,}"'
    "Discord webhook" = 'https://discord(?:app)?\.com/api/webhooks/[0-9]+/[A-Za-z0-9_-]+'
    "Bearer token" = '(?i)\bBearer\s+[A-Za-z0-9._~+/=-]{16,}\b'
}

$hits = @()
foreach ($file in $scanFiles) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($entry in $patterns.GetEnumerator()) {
        if ($text -match $entry.Value) {
            $hits += [pscustomobject]@{
                file = $file.FullName
                rule = $entry.Key
            }
        }
    }
}

if ($hits.Count -gt 0) {
    $hits | Format-Table -AutoSize | Out-String | Write-Host
    throw "Public evidence redaction check failed."
}

Write-Host "Public evidence redaction PASS ($($scanFiles.Count) files scanned)."

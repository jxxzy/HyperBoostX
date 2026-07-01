[CmdletBinding()]
param(
    [string]$RepoRoot = "",
    [string[]]$Paths = @()
)

$ErrorActionPreference = "Stop"

$scriptDir = if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $scriptDir "..")).Path
}

function Get-DefaultEvidencePaths {
    $patterns = @(
        "docs\runtime-audit\*.json",
        "docs\runtime-audit\*.md",
        "docs\OWNER_ADMIN_STABLE_GATE_RESULT_v2.10.0.md",
        "docs\QA_FULL_TEST_REPORT.md"
    )
    foreach ($pattern in $patterns) {
        Get-ChildItem -LiteralPath $RepoRoot -ErrorAction SilentlyContinue | Out-Null
        Get-ChildItem -Path (Join-Path $RepoRoot $pattern) -File -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty FullName
    }
}

function ConvertTo-RedactedEvidenceText {
    param([string]$Text)

    $redacted = $Text
    $literalReplacements = [ordered]@{
        "F:\\BOOSTER BY MR.4NONY" = "<REPO_ROOT>"
        "F:\BOOSTER BY MR.4NONY" = "<REPO_ROOT>"
        "C:\\Users\\jxxzy\\OneDrive\\Desktop" = "<USER_DESKTOP>"
        "C:\Users\jxxzy\OneDrive\Desktop" = "<USER_DESKTOP>"
        "C:\\Users\\Public\\Desktop" = "<PUBLIC_DESKTOP>"
        "C:\Users\Public\Desktop" = "<PUBLIC_DESKTOP>"
        "C:\\Program Files\\HyperBoostX" = "<INSTALL_DIR>"
        "C:\Program Files\HyperBoostX" = "<INSTALL_DIR>"
        "C:\\ProgramData\\Microsoft\\Windows\\Start Menu" = "<START_MENU>"
        "C:\ProgramData\Microsoft\Windows\Start Menu" = "<START_MENU>"
        "OneDrive\\Desktop" = "<USER_DESKTOP>"
        "OneDrive\Desktop" = "<USER_DESKTOP>"
    }
    foreach ($key in $literalReplacements.Keys) {
        $redacted = $redacted.Replace($key, $literalReplacements[$key])
    }

    $redacted = [regex]::Replace($redacted, 'F:(?:\\{1,4})BOOSTER BY MR\.4NONY', '<REPO_ROOT>')
    $redacted = [regex]::Replace($redacted, 'C:(?:\\{1,4})Users(?:\\{1,4})jxxzy(?:\\{1,4})OneDrive(?:\\{1,4})Desktop', '<USER_DESKTOP>')
    $redacted = [regex]::Replace($redacted, 'C:(?:\\{1,4})Users(?:\\{1,4})Public(?:\\{1,4})Desktop', '<PUBLIC_DESKTOP>')
    $redacted = [regex]::Replace($redacted, 'C:(?:\\{1,4})Program Files(?:\\{1,4})HyperBoostX', '<INSTALL_DIR>')
    $redacted = [regex]::Replace($redacted, 'C:(?:\\{1,4})ProgramData(?:\\{1,4})Microsoft(?:\\{1,4})Windows(?:\\{1,4})Start Menu', '<START_MENU>')
    $redacted = [regex]::Replace($redacted, 'OneDrive(?:\\{1,4})Desktop', '<USER_DESKTOP>')

    $redacted = [regex]::Replace($redacted, 'C:\\Users\\[^\\\r\n"''\]\}]+', 'C:\Users\<USER>')
    $redacted = [regex]::Replace($redacted, 'C:\\\\Users\\\\[^\\\r\n"''\]\}]+', 'C:\\Users\\<USER>')
    $redacted = [regex]::Replace($redacted, '(?i)https://discord(?:app)?\.com/api/webhooks/[0-9]+/[A-Za-z0-9_-]+', '<REDACTED_WEBHOOK>')
    $redacted = [regex]::Replace($redacted, '(?i)\bghp_[A-Za-z0-9_]{20,}\b', '<REDACTED_TOKEN>')
    $redacted = [regex]::Replace($redacted, '(?i)\bgithub_pat_[A-Za-z0-9_]{20,}\b', '<REDACTED_TOKEN>')
    $redacted = [regex]::Replace($redacted, '(?i)\bsk-[A-Za-z0-9_-]{20,}\b', '<REDACTED_SECRET>')
    $redacted = [regex]::Replace($redacted, '(?i)\bxox[baprs]-[A-Za-z0-9-]{20,}\b', '<REDACTED_TOKEN>')
    $redacted = [regex]::Replace($redacted, '(?i)\bBearer\s+[A-Za-z0-9._~+/=-]{16,}\b', 'Bearer <REDACTED_TOKEN>')
    $redacted = [regex]::Replace($redacted, '(?i)\b(api[_-]?key|token|secret|license[_-]?key)"?\s*[:=]\s*"[^"\r\n]{8,}"', '$1: "<REDACTED_SECRET>"')
    $redacted = [regex]::Replace($redacted, '(?i)\bdesktop-[a-z0-9-]+\\[a-z0-9._-]+\b', '<LOCAL_USER>')

    return $redacted
}

if ($Paths.Count -eq 0) {
    $Paths = @(Get-DefaultEvidencePaths)
}

$changed = @()
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
foreach ($path in $Paths | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique) {
    $fullPath = if ([System.IO.Path]::IsPathRooted($path)) { $path } else { Join-Path $RepoRoot $path }
    if (-not (Test-Path -LiteralPath $fullPath)) { continue }
    $before = Get-Content -LiteralPath $fullPath -Raw
    $after = ConvertTo-RedactedEvidenceText -Text $before
    if ($after -ne $before) {
        [System.IO.File]::WriteAllText($fullPath, $after.TrimEnd("`r", "`n") + "`r`n", $utf8NoBom)
        $changed += $fullPath
    }
}

[pscustomobject]@{
    changed_count = $changed.Count
    changed_files = $changed
} | ConvertTo-Json -Depth 4

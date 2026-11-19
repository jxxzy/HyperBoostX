[CmdletBinding()]
param(
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $scriptDir "..")).Path
}

$files = @()
$files += Get-Item -LiteralPath (Join-Path $RepoRoot "README.md")
$files += Get-Item -LiteralPath (Join-Path $RepoRoot "CONTRIBUTING.md")
$files += Get-Item -LiteralPath (Join-Path $RepoRoot "SECURITY.md")
$files += Get-ChildItem -LiteralPath (Join-Path $RepoRoot "docs") -Recurse -File -Include *.md |
    Where-Object {
        $_.FullName -notmatch '\\docs\\archive\\' -and
        $_.FullName -notmatch '\\docs\\dev\\' -and
        $_.FullName -notmatch '\\docs\\release-notes\\release-notes-v1\.' -and
        $_.FullName -notmatch '\\docs\\release-notes\\RELEASE_NOTES_v1\.' -and
        $_.FullName -notmatch '\\docs\\release-notes\\RELEASE_NOTES_v2\.0\.0\.md'
    }

$forbidden = [ordered]@{
    "admin lab still required for passed gates" = "Requires admin installer lab"
    "release publication stale blocker" = "GitHub tag/release publication depends on repository credentials"
    "stable candidate requires lab" = "stable_candidate_requires_lab"
    "v210 public stable claim" = "Current release: **HyperBoostX v2.10.0 Stable Unsigned**"
    "v210 stable unsigned public policy" = "v2.10.0 is the Stable Unsigned public release"
    "final stable pass claim" = "FINAL_STABLE_PASS"
    "pasti naik fps" = "pasti naik FPS"
    "ping pasti turun" = "ping pasti turun"
    "anti drop absolute" = "anti drop 100%"
}

$requiredReadme = Get-Content -LiteralPath (Join-Path $RepoRoot "README.md") -Raw
foreach ($required in @(
    "Recommended public stable baseline remains **HyperBoostX v1.3.0 Stable**",
    "Owner Lab / Release Candidate"
)) {
    if ($requiredReadme.IndexOf($required, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Docs consistency check failed: README is missing required release policy text: $required"
    }
}

$hits = @()
foreach ($file in $files) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($entry in $forbidden.GetEnumerator()) {
        if ($text.IndexOf($entry.Value, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $hits += [pscustomobject]@{
                file = $file.FullName
                rule = $entry.Key
                text = $entry.Value
            }
        }
    }
}

if ($hits.Count -gt 0) {
    $hits | Format-Table -AutoSize | Out-String | Write-Host
    throw "Docs consistency check failed."
}

Write-Host "Docs consistency PASS ($($files.Count) files scanned)."

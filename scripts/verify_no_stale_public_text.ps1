param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

$files = @()
foreach ($relativeRoot in @("README.md", "CHANGELOG.md", "SECURITY.md", "PRIVACY.md")) {
    $path = Join-Path $RepoRoot $relativeRoot
    if (Test-Path -LiteralPath $path) { $files += Get-Item -LiteralPath $path }
}

$docsRoot = Join-Path $RepoRoot "docs"
if (Test-Path -LiteralPath $docsRoot) {
    $files += Get-ChildItem -LiteralPath $docsRoot -Recurse -File -Include *.md |
        Where-Object {
            $_.FullName -notmatch '\\docs\\archive\\' -and
            $_.FullName -notmatch '\\docs\\dev\\' -and
            $_.FullName -notmatch '\\docs\\release-notes\\RELEASE_NOTES_v1\.' -and
            $_.FullName -notmatch '\\docs\\release-notes\\release-notes-v1\.' -and
            $_.FullName -notmatch '\\docs\\release-notes\\RELEASE_NOTES_v2\.0\.0\.md'
        }
}

$forbiddenPhrases = @(
    "Public stable remains v1.3.0",
    "README public status now points normal users to v1.3.0",
    "Stable status: NO-GO",
    "PUBLIC_STABLE_BLOCKED",
    "SOURCE_BETA_READY",
    "stable_candidate_requires_lab",
    "v2.10.0 is not stable",
    "must not be promoted as stable",
    "pasti naik FPS",
    "ping pasti turun",
    "anti drop 100%",
    "official NVIDIA/AMD/Intel",
    "driver terbaru otomatis",
    "auto fix semua"
)

function Test-IsNegativeOrPolicyLine([string]$Line) {
    return $Line -match '(?i)\b(no|not|must not|do not|does not|without|avoid|blocked|forbid|forbidden|never|cannot|mustn''t)\b'
}

foreach ($file in $files) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    $lines = $text -split "\r?\n"
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        foreach ($phrase in $forbiddenPhrases) {
            if ($line.IndexOf($phrase, [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -and -not (Test-IsNegativeOrPolicyLine $line)) {
                $failures.Add("$($file.FullName):$($i + 1) contains stale/forbidden public text: $phrase")
            }
        }

        if ($line -match '(?i)\bguaranteed\s+(FPS|ping|performance|latency|smoothness|improvement|increase|gain|gains)' -and -not (Test-IsNegativeOrPolicyLine $line)) {
            $failures.Add("$($file.FullName):$($i + 1) contains unsafe guaranteed performance wording.")
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: no stale public text verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: no stale public text verification ($($files.Count) files scanned)" -ForegroundColor Green

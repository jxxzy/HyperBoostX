param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

$requiredDocs = @(
    "README.md",
    "CHANGELOG.md",
    "SECURITY.md",
    "docs\UI_ACTION_MAP_v2.10.0.md",
    "docs\UI_V13_TO_V210_PLACEMENT_MATRIX.md",
    "docs\UI_RELEASE_GATE_v2.10.0.md",
    "docs\FINAL_AUDIT_REPORT_v2.10.0.md",
    "docs\FEATURE_TRUTH_MATRIX_v2.10.0.md",
    "docs\QA_RESULTS_v2.10.0.md",
    "docs\OWNER_ADMIN_STABLE_GATE_RESULT_v2.10.0.md",
    "docs\release-notes\RELEASE_NOTES_v2.10.0.md"
)

foreach ($relativePath in $requiredDocs) {
    $path = Join-Path $RepoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing release doc: $relativePath")
        continue
    }

    $text = Get-Content -LiteralPath $path -Raw
    if ($relativePath -ne "CHANGELOG.md" -and $text -notmatch "2\.10\.0") {
        $failures.Add("$relativePath does not mention 2.10.0.")
    }
}

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "verify_docs_consistency.ps1") -RepoRoot $RepoRoot
if ($LASTEXITCODE -ne 0) {
    $failures.Add("verify_docs_consistency.ps1 failed.")
}

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "verify_no_stale_public_text.ps1") -RepoRoot $RepoRoot
if ($LASTEXITCODE -ne 0) {
    $failures.Add("verify_no_stale_public_text.ps1 failed.")
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: release docs consistency verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: release docs consistency verification" -ForegroundColor Green


param(
    [string]$CandidateVersion = "2.10.0",
    [switch]$OwnerApprovedStable,
    [switch]$WhatIfOnly
)

$ErrorActionPreference = "Stop"

$scriptDir = if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$repoRoot = (Resolve-Path (Join-Path $scriptDir "..\..")).Path
Set-Location $repoRoot

if (-not $OwnerApprovedStable) {
    Write-Host "HyperBoostX stable release guard is active." -ForegroundColor Yellow
    Write-Host "This script will not promote v$CandidateVersion to Stable without -OwnerApprovedStable." -ForegroundColor Yellow
    Write-Host "Run release gates and manual lab evidence first. Current allowed state is Stable Candidate / Owner Approval Required."
    exit 2
}

if ($WhatIfOnly) {
    Write-Host "[DRY RUN] Owner approval flag supplied. No files will be modified."
}

$requiredFiles = @(
    "VERSION",
    "wpf\HyperBoostX.csproj",
    "launcher\HyperBoostLauncher.csproj",
    "HyperBoostXInstaller.nsi",
    "README.md",
    "docs\RELEASE.md",
    "docs\FINAL_AUDIT_REPORT_v2.10.0.md",
    "docs\STABLE_MODE_AUDIT_v2.10.0.md",
    "docs\INSTALLER_LAB_GATE_v2.10.0.md",
    "docs\HARDWARE_MATRIX_v2.10.0.md",
    "docs\CODE_SIGNING_READINESS.md"
)

$missing = @($requiredFiles | Where-Object { -not (Test-Path -LiteralPath $_) })
if ($missing.Count -gt 0) {
    throw "Cannot prepare stable release. Missing required files: $($missing -join ', ')"
}

$versionText = (Get-Content -LiteralPath "VERSION" -Raw).Trim()
if ($versionText -notin @("2.10.0-beta.1", "2.10.0-rc.1", $CandidateVersion)) {
    throw "Unexpected VERSION value '$versionText'. Refusing stable promotion."
}

$gateSummary = if (Test-Path -LiteralPath "docs\runtime-audit\full_qa_summary.json") {
    Get-Content -LiteralPath "docs\runtime-audit\full_qa_summary.json" -Raw | ConvertFrom-Json
} else {
    $null
}

if ($gateSummary -and $gateSummary.status -notin @("STABLE_CANDIDATE_READY", "BETA_READY")) {
    throw "Full QA status '$($gateSummary.status)' is not acceptable for stable preparation."
}

Write-Host "Stable owner approval guard passed for v$CandidateVersion." -ForegroundColor Green
Write-Host "No automatic tag/release is created by this script."
Write-Host "Next manual steps: verify installer lab, hardware matrix, code signing decision, then create tag/release explicitly."

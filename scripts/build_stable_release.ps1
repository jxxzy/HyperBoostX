[CmdletBinding()]
param(
    [string]$RepoRoot = "",
    [string]$ExpectedVersion = "2.10.0",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

$ScriptDir = if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..")).Path
}

$versionPath = Join-Path $RepoRoot "VERSION"
$currentVersion = (Get-Content -LiteralPath $versionPath -Raw).Trim()
if ($currentVersion -ne $ExpectedVersion) {
    throw "Stable build refused: VERSION is '$currentVersion', expected '$ExpectedVersion'. Run this only after owner admin gate passes and stable promotion is intentionally applied."
}

$ownerGateReport = Join-Path $RepoRoot "runtime_audit\owner_admin_stable_gate_report.json"
if (-not (Test-Path -LiteralPath $ownerGateReport)) {
    throw "Stable build refused: owner admin stable gate report is missing: $ownerGateReport"
}

$gate = Get-Content -LiteralPath $ownerGateReport -Raw | ConvertFrom-Json
if (-not $gate.ok) {
    throw "Stable build refused: owner admin stable gate did not pass."
}
if ([string]$gate.expected_version -ne $ExpectedVersion) {
    throw "Stable build refused: owner admin stable gate expected '$($gate.expected_version)', not '$ExpectedVersion'."
}

Push-Location $RepoRoot
try {
    if (-not $SkipTests) {
        powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $ScriptDir "full_qa_gate.ps1") -SkipInstall | Out-Host
        if ($LASTEXITCODE -ne 0) {
            throw "Full QA gate failed with exit code $LASTEXITCODE"
        }
    }

    powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $ScriptDir "build_release_v2.10.0.ps1") -SkipTests | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Release build failed with exit code $LASTEXITCODE"
    }
    powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $ScriptDir "package_installer_v2.10.0.ps1") -SkipBuild | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Installer package failed with exit code $LASTEXITCODE"
    }
    powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $ScriptDir "verify_installer_payload.ps1") | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Installer payload verification failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

Write-Host "Stable unsigned package build complete. Code signing: SKIPPED_BY_OWNER_NO_CERT."

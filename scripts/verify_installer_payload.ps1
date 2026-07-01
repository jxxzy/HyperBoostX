[CmdletBinding()]
param(
    [string]$RepoRoot = "",
    [switch]$AllowMissingInstaller
)

$ErrorActionPreference = "Stop"

$ScriptDir = if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..")).Path
}

$expectedVersion = (Get-Content -LiteralPath (Join-Path $RepoRoot "VERSION") -Raw).Trim()
$installerPath = Join-Path $RepoRoot "HyperBoostXInstaller.exe"
$nsiPath = Join-Path $RepoRoot "HyperBoostXInstaller.nsi"
$outDir = Join-Path $RepoRoot "runtime_audit"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$jsonPath = Join-Path $outDir "installer_payload_report.json"
$mdPath = Join-Path $outDir "installer_payload_report.md"

$checks = New-Object System.Collections.Generic.List[object]
function Add-Check {
    param([string]$Name, [bool]$Ok, [string]$Evidence)
    $checks.Add([pscustomobject]@{ name = $Name; ok = $Ok; evidence = $Evidence })
}

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepoRoot "scripts\verify_version_sync.ps1") | Out-Host
Add-Check "version sync script passed" ($LASTEXITCODE -eq 0) "scripts\\verify_version_sync.ps1"

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepoRoot "scripts\verify_release_artifact_contents.ps1") -RepoRoot $RepoRoot | Out-Host
Add-Check "release artifact content script passed" ($LASTEXITCODE -eq 0) "scripts\\verify_release_artifact_contents.ps1"

$nsiText = if (Test-Path -LiteralPath $nsiPath) { Get-Content -LiteralPath $nsiPath -Raw } else { "" }
Add-Check "NSIS file exists" (Test-Path -LiteralPath $nsiPath) $nsiPath
Add-Check "NSIS DisplayVersion uses current version" ($nsiText -match [regex]::Escape("DisplayVersion`" `"$expectedVersion")) "expected DisplayVersion $expectedVersion"
Add-Check "NSIS writes HKLM uninstall metadata" ($nsiText -match "CurrentVersion\\Uninstall\\HyperBoostX") "uninstall registry entry"
Add-Check "NSIS writes QuietUninstallString" ($nsiText -match "QuietUninstallString") "silent uninstall metadata"
Add-Check "NSIS writes owner publisher" ($nsiText -match 'Publisher"\s+"HyperBoostX / jxxzy"') "publisher metadata"
Add-Check "NSIS creates Start Menu shortcut" ($nsiText -match "SMPROGRAMS") "Start Menu shortcut"
Add-Check "NSIS creates desktop shortcut" ($nsiText -match "DESKTOP") "desktop shortcut"

if (Test-Path -LiteralPath $installerPath) {
    $hash = Get-FileHash -LiteralPath $installerPath -Algorithm SHA256
    Add-Check "installer exists" $true $installerPath
    Add-Check "installer hash available" (-not [string]::IsNullOrWhiteSpace($hash.Hash)) $hash.Hash.ToLowerInvariant()
}
elseif ($AllowMissingInstaller) {
    Add-Check "installer exists" $true "missing allowed for source-only verification"
}
else {
    Add-Check "installer exists" $false $installerPath
}

$hashFile = Join-Path $RepoRoot ("SHA256SUMS_{0}.txt" -f $expectedVersion)
Add-Check "v2.10 checksum file exists" (Test-Path -LiteralPath $hashFile) $hashFile
if ((Test-Path -LiteralPath $hashFile) -and (Test-Path -LiteralPath $installerPath)) {
    $hashText = Get-Content -LiteralPath $hashFile -Raw
    $currentHash = (Get-FileHash -LiteralPath $installerPath -Algorithm SHA256).Hash.ToLowerInvariant()
    Add-Check "checksum file includes current installer hash" ($hashText -match [regex]::Escape($currentHash)) $currentHash
}

$report = [pscustomobject]@{
    generated_at = (Get-Date).ToUniversalTime().ToString("o")
    repo_root = $RepoRoot
    expected_version = $expectedVersion
    installer_path = $installerPath
    checks = $checks
    ok = -not ($checks | Where-Object { -not $_.ok })
}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

$lines = @(
    "# HyperBoostX Installer Payload Verification",
    "",
    "Expected version: $expectedVersion",
    "Installer: $installerPath",
    "",
    "| Check | Status | Evidence |",
    "| --- | --- | --- |"
)
foreach ($check in $checks) {
    $status = if ($check.ok) { "PASS" } else { "FAIL" }
    $evidence = ([string]$check.evidence).Replace("|", "/")
    $lines += "| $($check.name) | $status | $evidence |"
}
$lines | Set-Content -LiteralPath $mdPath -Encoding UTF8

Write-Host "Installer payload report: $jsonPath"
if (-not $report.ok) { exit 1 }

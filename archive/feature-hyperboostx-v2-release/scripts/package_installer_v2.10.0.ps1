param(
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$packageVersion = (Get-Content -LiteralPath (Join-Path $repoRoot "VERSION") -Raw).Trim()
$versionedHashFile = "SHA256SUMS_$packageVersion.txt"

Write-Host "== Package HyperBoostX $packageVersion installer =="

if (-not $SkipBuild) {
    powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build_release_v2.10.0.ps1 -SkipTests | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Release build failed with exit code $LASTEXITCODE"
    }
}

$makensis = "${env:ProgramFiles(x86)}\NSIS\makensis.exe"
if (-not (Test-Path -LiteralPath $makensis)) {
    throw "NSIS makensis.exe not found at $makensis"
}

& $makensis .\HyperBoostXInstaller.nsi
if ($LASTEXITCODE -ne 0) {
    throw "NSIS installer build failed with exit code $LASTEXITCODE"
}

$hashCandidates = @(
    "HyperBoostXInstaller.exe",
    "release\app\HyperBoostX.exe",
    "release\app\runtime\wpf\HyperBoostX.exe",
    "release\app\runtime\backend\hyperboost_backend.exe",
    "release\package\backend\hyperboost_backend.exe",
    "release\package\wpf\HyperBoostX.exe",
    "release\package\launcher\HyperBoostLauncher.exe"
)

$hashLines = @()
foreach ($candidate in $hashCandidates) {
    if (Test-Path -LiteralPath $candidate) {
        $hash = Get-FileHash -LiteralPath $candidate -Algorithm SHA256
        $hashLines += "{0}  {1}" -f $hash.Hash.ToLowerInvariant(), $candidate
    }
}

if ($hashLines.Count -gt 0) {
    $hashLines | Set-Content -LiteralPath $versionedHashFile -Encoding ASCII
    $hashLines | Set-Content -LiteralPath "SHA256SUMS.txt" -Encoding ASCII
}

Write-Host "Installer package step complete. Signing remains blocked unless owner certificate/PFX is provided."

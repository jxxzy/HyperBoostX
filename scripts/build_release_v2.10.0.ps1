param(
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

Write-Host "== Build HyperBoostX v2.10.0 beta package =="

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$localDeployRoot = Join-Path $repoRoot "artifacts\local-deploy"
$releaseRoot = Join-Path $repoRoot "release"

if (-not $SkipTests) {
    powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\release_gate_v2.10.0.ps1 -SkipFullQa -SkipDotnet:$false
    if ($LASTEXITCODE -ne 0) {
        throw "Release gate failed with exit code $LASTEXITCODE"
    }
}

powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build_release_local.ps1 | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "Local release build failed with exit code $LASTEXITCODE"
}

$resolvedReleaseRoot = [System.IO.Path]::GetFullPath($releaseRoot)
$repoPrefix = [System.IO.Path]::GetFullPath($repoRoot).TrimEnd('\') + '\'
if (-not $resolvedReleaseRoot.StartsWith($repoPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to sync release output outside repository: $resolvedReleaseRoot"
}

New-Item -ItemType Directory -Force -Path $resolvedReleaseRoot | Out-Null
foreach ($name in @("package", "app")) {
    $source = Join-Path $localDeployRoot $name
    $target = Join-Path $resolvedReleaseRoot $name
    if (-not (Test-Path -LiteralPath $source)) {
        throw "Expected local deploy output missing: $source"
    }
    if (Test-Path -LiteralPath $target) {
        $resolvedTarget = [System.IO.Path]::GetFullPath($target)
        if (-not $resolvedTarget.StartsWith($resolvedReleaseRoot.TrimEnd('\') + '\', [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove release target outside release root: $resolvedTarget"
        }
        Remove-Item -LiteralPath $target -Recurse -Force
    }
    Copy-Item -LiteralPath $source -Destination $target -Recurse -Force
}

Write-Host "Build complete. Verify artifacts before sharing beta builds."

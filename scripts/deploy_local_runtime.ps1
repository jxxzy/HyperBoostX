[CmdletBinding()]
param(
    [string]$SourceRuntimeRoot = (Join-Path (Join-Path (Split-Path -Parent $PSScriptRoot) "artifacts\local-deploy") "app"),
    [string]$InstallRoot = "C:\Program Files\HyperBoost X",
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

function Ensure-Dir {
    param([string]$PathValue)
    New-Item -ItemType Directory -Force -Path $PathValue | Out-Null
}

function Stop-IfRunning {
    param([string]$ProcessName)

    Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}

if (-not $SkipBuild) {
    Write-Host "Building latest release package first..."
    & powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "build_release_local.ps1")
}

if (-not (Test-Path $SourceRuntimeRoot)) {
    throw "Portable runtime root was not found: $SourceRuntimeRoot"
}

Write-Host "Stopping running HyperBoost X processes if present..."
Stop-IfRunning -ProcessName "HyperBoostX"
Stop-IfRunning -ProcessName "HyperBoostUI"
Stop-IfRunning -ProcessName "HyperBoostLauncher"
Stop-IfRunning -ProcessName "hyperboost_backend"

$runtimeWpfSource = Join-Path $SourceRuntimeRoot "runtime\wpf"
$runtimeBackendSource = Join-Path $SourceRuntimeRoot "runtime\backend"
$launcherSource = Join-Path $SourceRuntimeRoot "HyperBoostX.exe"

if (-not (Test-Path $launcherSource)) {
    throw "Launcher entrypoint is missing from portable runtime: $launcherSource"
}

Ensure-Dir $InstallRoot
Ensure-Dir (Join-Path $InstallRoot "runtime")
Ensure-Dir (Join-Path $InstallRoot "runtime\wpf")
Ensure-Dir (Join-Path $InstallRoot "runtime\backend")

Write-Host "Copying launcher and runtime files to $InstallRoot ..."
Copy-Item $launcherSource (Join-Path $InstallRoot "HyperBoostX.exe") -Force
Copy-Item (Join-Path $runtimeWpfSource "*") (Join-Path $InstallRoot "runtime\wpf") -Recurse -Force
Copy-Item (Join-Path $runtimeBackendSource "*") (Join-Path $InstallRoot "runtime\backend") -Recurse -Force

Write-Host "Local runtime deploy completed."
Write-Host "Entrypoint: $(Join-Path $InstallRoot 'HyperBoostX.exe')"

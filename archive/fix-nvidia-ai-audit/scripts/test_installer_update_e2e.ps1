[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$InstallerPath,

    [string]$UpgradeInstallerPath = "",

    [int]$LaunchSeconds = 12
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$reportDir = Join-Path $projectRoot "build_tmp"
New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$reportPath = Join-Path $reportDir "installer-update-e2e-$timestamp.log"

function Write-Report {
    param([string]$Message)
    $line = "[{0}] {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Message
    $line | Tee-Object -FilePath $reportPath -Append
}

function Require-Condition {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Stop-HyperBoostProcesses {
    $names = @("HyperBoostX", "HyperBoostUI", "HyperBoostLauncher", "hyperboost_backend")
    foreach ($name in $names) {
        Get-Process -Name $name -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    }
}

function Get-UninstallInfo {
    $paths = @(
        "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX",
        "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX"
    )

    foreach ($path in $paths) {
        if (Test-Path $path) {
            return Get-ItemProperty -Path $path
        }
    }

    return $null
}

function Invoke-ExistingUninstall {
    $info = Get-UninstallInfo
    if ($null -eq $info) {
        Write-Report "No previous HyperBoost X installation found."
        return
    }

    $uninstallString = [string]$info.UninstallString
    Require-Condition (-not [string]::IsNullOrWhiteSpace($uninstallString)) "Uninstall string is missing."

    $quoted = $uninstallString.Trim()
    if ($quoted.StartsWith('"')) {
        $exe = $quoted.Split('"')[1]
    } else {
        $exe = $quoted.Split(' ')[0]
    }

    Require-Condition (Test-Path $exe) "Uninstaller path not found: $exe"
    Write-Report "Running existing uninstaller: $exe"
    Stop-HyperBoostProcesses
    $process = Start-Process -FilePath $exe -ArgumentList "/S" -Wait -PassThru
    Require-Condition ($process.ExitCode -eq 0) "Uninstaller failed with exit code $($process.ExitCode)."
}

function Invoke-Installer {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    Require-Condition (Test-Path $Path) "Installer not found: $Path"
    Write-Report "Launching installer: $Path"
    Stop-HyperBoostProcesses
    $process = Start-Process -FilePath $Path -ArgumentList "/S" -Wait -PassThru
    Require-Condition ($process.ExitCode -eq 0) "Installer failed with exit code $($process.ExitCode)."
}

function Assert-InstallLayout {
    $installDir = Join-Path $env:ProgramFiles "HyperBoost X"
    $launcherPath = Join-Path $installDir "HyperBoostX.exe"
    $uiPath = Join-Path $installDir "runtime\wpf\HyperBoostX.exe"
    $backendPath = Join-Path $installDir "runtime\backend\hyperboost_backend.exe"

    Require-Condition (Test-Path $launcherPath) "Installed launcher not found: $launcherPath"
    Require-Condition (Test-Path $uiPath) "Installed UI runtime not found: $uiPath"
    Require-Condition (Test-Path $backendPath) "Installed backend runtime not found: $backendPath"

    Write-Report "Install layout verified at $installDir"
    return $launcherPath
}

function Invoke-LaunchSmoke {
    param(
        [Parameter(Mandatory = $true)]
        [string]$LauncherPath
    )

    Write-Report "Starting launcher smoke test."
    $process = Start-Process -FilePath $LauncherPath -PassThru
    Start-Sleep -Seconds $LaunchSeconds
    Require-Condition (-not $process.HasExited) "Launcher exited unexpectedly during smoke window."
    Stop-HyperBoostProcesses
    Write-Report "Launcher smoke test passed."
}

Write-Report "HyperBoost X installer/update E2E harness started."
Write-Report "Installer path: $InstallerPath"
if (-not [string]::IsNullOrWhiteSpace($UpgradeInstallerPath)) {
    Write-Report "Upgrade installer path: $UpgradeInstallerPath"
}

$markerDir = Join-Path $env:LOCALAPPDATA "HyperBoost X\config"
$markerPath = Join-Path $markerDir "e2e-preserve.marker"
New-Item -ItemType Directory -Force -Path $markerDir | Out-Null
"preserve-check $(Get-Date -Format s)" | Set-Content -Path $markerPath -Encoding UTF8

Invoke-ExistingUninstall
Invoke-Installer -Path $InstallerPath
$launcher = Assert-InstallLayout
Invoke-LaunchSmoke -LauncherPath $launcher

if (-not [string]::IsNullOrWhiteSpace($UpgradeInstallerPath)) {
    Write-Report "Running upgrade validation."
    Invoke-Installer -Path $UpgradeInstallerPath
    $launcher = Assert-InstallLayout
    Require-Condition (Test-Path $markerPath) "Preserved config marker missing after upgrade."
    Invoke-LaunchSmoke -LauncherPath $launcher
}

Write-Report "E2E harness completed successfully."
Write-Host "Report saved to $reportPath"

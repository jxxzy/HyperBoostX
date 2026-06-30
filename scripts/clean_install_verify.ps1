[CmdletBinding()]
param(
    [switch]$Execute,
    [string]$RepoRoot = "",
    [string]$InstallerPath = ""
)

$ErrorActionPreference = "Stop"

$ScriptDir = if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..")).Path
}

if ([string]::IsNullOrWhiteSpace($InstallerPath)) {
    $InstallerPath = Join-Path $RepoRoot "HyperBoostXInstaller.exe"
}

$outDir = Join-Path $RepoRoot "runtime_audit"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$jsonPath = Join-Path $outDir "clean_install_verify_report.json"
$mdPath = Join-Path $outDir "clean_install_verify_report.md"

$report = [ordered]@{
    generated_at = (Get-Date).ToUniversalTime().ToString("o")
    repo_root = $RepoRoot
    installer_path = $InstallerPath
    execute = [bool]$Execute
    dry_run = -not [bool]$Execute
    local_appdata_backup = $null
    steps = @()
    ok = $false
}

function Add-Step {
    param([string]$Name, [bool]$Ok, [string]$Detail)
    $script:report.steps += [pscustomobject]@{ name = $Name; ok = $Ok; detail = $Detail }
}

function Test-IsAdmin {
    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Write-ReportAndExit {
    param([int]$ExitCode)

    $script:report.ok = -not ($script:report.steps | Where-Object { -not $_.ok })
    $script:report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding UTF8
    $statusText = if ($script:report.ok) { "PASS" } else { "FAIL" }
    $lines = @(
        "# HyperBoostX Clean Install Verification",
        "",
        "Status: $statusText",
        "Installer: $InstallerPath",
        "",
        "| Step | Status | Detail |",
        "| --- | --- | --- |"
    )
    foreach ($step in $script:report.steps) {
        $status = if ($step.ok) { "PASS" } else { "FAIL" }
        $detail = ([string]$step.detail).Replace("|", "/")
        $lines += "| $($step.name) | $status | $detail |"
    }
    $lines | Set-Content -LiteralPath $mdPath -Encoding UTF8
    Write-Host "Clean install report: $jsonPath"
    exit $ExitCode
}

function Get-HyperBoostRegistryEntry {
    $keys = @(
        "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX",
        "HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX",
        "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX",
        "HKCU:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX"
    )

    foreach ($key in $keys) {
        if (Test-Path -LiteralPath $key) {
            $props = Get-ItemProperty -LiteralPath $key
            return [pscustomobject]@{
                key = $key
                DisplayVersion = $props.DisplayVersion
                InstallLocation = $props.InstallLocation
                UninstallString = $props.UninstallString
                QuietUninstallString = $props.QuietUninstallString
            }
        }
    }
    return $null
}

function Split-CommandLine {
    param([string]$CommandLine)

    $trimmed = $CommandLine.Trim()
    if ($trimmed.StartsWith('"')) {
        $match = [regex]::Match($trimmed, '^"([^"]+)"\s*(.*)$')
        if (-not $match.Success) { throw "Unable to parse quoted uninstall command: $CommandLine" }
        return [pscustomobject]@{ file = $match.Groups[1].Value; arguments = $match.Groups[2].Value }
    }

    $parts = $trimmed -split '\s+', 2
    return [pscustomobject]@{
        file = $parts[0]
        arguments = if ($parts.Count -gt 1) { $parts[1] } else { "" }
    }
}

function Test-PathUnderAllowedInstallRoot {
    param([string]$PathToCheck, [string]$InstallLocation)

    if (-not (Test-Path -LiteralPath $PathToCheck)) { return $false }
    $resolvedPath = [System.IO.Path]::GetFullPath($PathToCheck)
    $roots = @()
    if ($InstallLocation) { $roots += $InstallLocation }
    $roots += Join-Path $env:ProgramFiles "HyperBoostX"
    if (${env:ProgramFiles(x86)}) { $roots += Join-Path ${env:ProgramFiles(x86)} "HyperBoostX" }

    foreach ($root in $roots | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) {
        try {
            $resolvedRoot = [System.IO.Path]::GetFullPath($root).TrimEnd('\') + '\'
            if ($resolvedPath.StartsWith($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
                return $true
            }
        }
        catch {
            continue
        }
    }
    return $false
}

Add-Step "installer exists" (Test-Path -LiteralPath $InstallerPath) $InstallerPath

if (-not $Execute) {
    Add-Step "destructive clean install skipped" $true "Run from an elevated shell with -Execute to stop processes, uninstall, back up LocalAppData, install, launch, and run runtime verifiers."
    Write-ReportAndExit 0
}

if (-not (Test-IsAdmin)) {
    Add-Step "administrator shell required" $false "BLOCKED_BY_ENVIRONMENT: clean install/uninstall touches Program Files and HKLM."
    Write-ReportAndExit 1
}

if (-not (Test-Path -LiteralPath $InstallerPath)) {
    Add-Step "installer missing" $false $InstallerPath
    Write-ReportAndExit 1
}

Get-Process -ErrorAction SilentlyContinue |
    Where-Object { $_.ProcessName -in @("HyperBoostX", "HyperBoostLauncher", "hyperboost_backend") } |
    Stop-Process -Force -ErrorAction SilentlyContinue
Add-Step "stopped HyperBoostX processes" $true "Process stop requested for HyperBoostX-owned runtime names."

$localData = Join-Path $env:LOCALAPPDATA "HyperBoost X"
if (Test-Path -LiteralPath $localData) {
    $backup = Join-Path $env:LOCALAPPDATA ("HyperBoost X.backup." + (Get-Date -Format "yyyyMMddHHmmss"))
    Copy-Item -LiteralPath $localData -Destination $backup -Recurse -Force
    $report.local_appdata_backup = $backup
    Add-Step "backed up LocalAppData" $true $backup
}
else {
    Add-Step "LocalAppData backup skipped" $true "No existing LocalAppData folder found."
}

$registry = Get-HyperBoostRegistryEntry
if ($registry -and ($registry.UninstallString -or $registry.QuietUninstallString)) {
    $commandText = if ($registry.QuietUninstallString) { $registry.QuietUninstallString } else { $registry.UninstallString }
    $command = Split-CommandLine $commandText
    if (-not (Test-PathUnderAllowedInstallRoot -PathToCheck $command.file -InstallLocation $registry.InstallLocation)) {
        Add-Step "uninstall path safety guard" $false "Uninstaller path is outside the expected HyperBoostX install root: $($command.file)"
        Write-ReportAndExit 1
    }

    $arguments = $command.arguments
    if ($arguments -notmatch '(^|\s)/S(\s|$)') {
        $arguments = ($arguments + " /S").Trim()
    }
    Start-Process -FilePath $command.file -ArgumentList $arguments -Wait -WindowStyle Hidden
    Add-Step "uninstalled previous HyperBoostX" $true $command.file
}
else {
    Add-Step "previous install not found" $true "Registry uninstall key missing or uninstall command unavailable."
}

Start-Process -FilePath $InstallerPath -ArgumentList "/S" -Wait -WindowStyle Hidden
Add-Step "installed new HyperBoostX" $true $InstallerPath

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepoRoot "scripts\runtime_verifier.ps1") -RepoRoot $RepoRoot -LaunchInstalledApp -StopAfterProbe
$verifierExit = $LASTEXITCODE
Add-Step "verified installed runtime" ($verifierExit -eq 0) "runtime_verifier.ps1 exit=$verifierExit"

Write-ReportAndExit $verifierExit

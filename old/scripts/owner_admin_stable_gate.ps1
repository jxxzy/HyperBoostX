[CmdletBinding()]
param(
    [string]$RepoRoot = "",
    [string]$InstallerPath = "",
    [string]$ExpectedVersion = "",
    [int]$BackendPort = 5000,
    [int]$TimeoutSeconds = 60
)

$ErrorActionPreference = "Stop"

$ScriptDir = if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..")).Path
}
if ([string]::IsNullOrWhiteSpace($InstallerPath)) {
    $InstallerPath = Join-Path $RepoRoot "HyperBoostXInstaller.exe"
}
if ([string]::IsNullOrWhiteSpace($ExpectedVersion)) {
    $ExpectedVersion = (Get-Content -LiteralPath (Join-Path $RepoRoot "VERSION") -Raw).Trim()
}

. (Join-Path $RepoRoot "scripts\lib\HyperBoostXReleaseContract.ps1")

$outDir = Join-Path $RepoRoot "docs\runtime-audit"
$docsDir = Join-Path $RepoRoot "docs"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
New-Item -ItemType Directory -Force -Path $docsDir | Out-Null
$jsonPath = Join-Path $outDir "owner_admin_stable_gate_report.json"
$mdPath = Join-Path $docsDir "OWNER_ADMIN_STABLE_GATE_RESULT_v2.10.0.md"

$script:steps = @()
$script:oldRegistryEntries = @()
$script:newRegistryEntries = @()
$script:runtimeVerifierExit = $null

function Add-Step {
    param([string]$Name, [bool]$Ok, [string]$Detail)
    $script:steps += [pscustomobject]@{ name = $Name; ok = $Ok; detail = $Detail }
}

function Test-IsAdmin {
    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-RegistryValue {
    param([object]$Item, [string[]]$PropertyNames, [string]$Name)
    if ($PropertyNames -contains $Name) { return $Item.$Name }
    return $null
}

function Get-HyperBoostRegistryEntries {
    $roots = @(
        "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall",
        "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall",
        "HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
        "HKCU:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    )
    $items = @()
    foreach ($root in $roots) {
        if (-not (Test-Path -LiteralPath $root)) { continue }
        foreach ($key in Get-ChildItem -LiteralPath $root -ErrorAction SilentlyContinue) {
            $p = Get-ItemProperty -LiteralPath $key.PSPath -ErrorAction SilentlyContinue
            if (-not $p) { continue }
            $propertyNames = @($p | Get-Member -MemberType NoteProperty | Select-Object -ExpandProperty Name)
            $displayName = Get-RegistryValue -Item $p -PropertyNames $propertyNames -Name "DisplayName"
            $publisher = Get-RegistryValue -Item $p -PropertyNames $propertyNames -Name "Publisher"
            if (($displayName -like "*HyperBoost*") -or ($publisher -like "*HyperBoost*") -or ($publisher -like "*HYPERINDO*")) {
                $items += [pscustomobject]@{
                    root = $root
                    key = $key.PSChildName
                    DisplayName = $displayName
                    DisplayVersion = Get-RegistryValue -Item $p -PropertyNames $propertyNames -Name "DisplayVersion"
                    Publisher = $publisher
                    InstallLocation = Get-RegistryValue -Item $p -PropertyNames $propertyNames -Name "InstallLocation"
                    UninstallString = Get-RegistryValue -Item $p -PropertyNames $propertyNames -Name "UninstallString"
                    QuietUninstallString = Get-RegistryValue -Item $p -PropertyNames $propertyNames -Name "QuietUninstallString"
                }
            }
        }
    }
    return @($items)
}

function Split-CommandLine {
    param([string]$CommandLine)
    $trimmed = $CommandLine.Trim()
    if ($trimmed.StartsWith('"')) {
        $match = [regex]::Match($trimmed, '^"([^"]+)"\s*(.*)$')
        if (-not $match.Success) { throw "Unable to parse command line: $CommandLine" }
        return [pscustomobject]@{ file = $match.Groups[1].Value; arguments = $match.Groups[2].Value }
    }
    $parts = $trimmed -split '\s+', 2
    return [pscustomobject]@{ file = $parts[0]; arguments = if ($parts.Count -gt 1) { $parts[1] } else { "" } }
}

function Test-PathUnderHyperBoostRoot {
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
            if ($resolvedPath.StartsWith($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) { return $true }
        }
        catch {
            continue
        }
    }
    return $false
}

function Get-HyperBoostProcesses {
    param([string]$InstallLocation)
    $names = @("HyperBoostX", "HyperBoostUI", "HyperBoostLauncher", "hyperboost_backend")
    $resolvedInstall = if ($InstallLocation -and (Test-Path -LiteralPath $InstallLocation)) { [System.IO.Path]::GetFullPath($InstallLocation).TrimEnd('\') } else { "" }
    $rows = @()
    foreach ($name in $names) {
        foreach ($proc in Get-Process -Name $name -ErrorAction SilentlyContinue) {
            $path = $null
            try { $path = $proc.Path } catch { $path = $null }
            $fromInstall = $false
            if ($path -and $resolvedInstall) {
                try { $fromInstall = [System.IO.Path]::GetFullPath($path).StartsWith($resolvedInstall, [System.StringComparison]::OrdinalIgnoreCase) } catch { $fromInstall = $false }
            }
            $rows += [pscustomobject]@{ name = $proc.ProcessName; id = $proc.Id; path = $path; from_install = $fromInstall }
        }
    }
    return @($rows)
}

function Stop-HyperBoostProcesses {
    param([string]$InstallLocation, [switch]$OnlyInstalled)
    $stopped = @()
    foreach ($proc in Get-HyperBoostProcesses -InstallLocation $InstallLocation) {
        if ($OnlyInstalled -and -not $proc.from_install) { continue }
        try {
            Stop-Process -Id $proc.id -Force -ErrorAction Stop
            $stopped += $proc
        }
        catch {
            $stopped += [pscustomobject]@{ name = $proc.name; id = $proc.id; path = $proc.path; error = $_.Exception.Message }
        }
    }
    return @($stopped)
}

function Invoke-JsonEndpoint {
    param([string]$Uri, [int]$Timeout = 4)
    $curl = Get-Command curl.exe -ErrorAction SilentlyContinue
    if (-not $curl) { return [pscustomobject]@{ ok = $false; uri = $Uri; error = "curl.exe not found"; data = $null } }
    $previous = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & $curl.Source --silent --show-error --max-time $Timeout --noproxy "*" $Uri 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previous
    }
    $text = (($output | ForEach-Object { $_.ToString() }) -join "`n").Trim()
    if ($exitCode -ne 0) { return [pscustomobject]@{ ok = $false; uri = $Uri; error = $text; data = $null } }
    try {
        $data = if ([string]::IsNullOrWhiteSpace($text)) { $null } else { $text | ConvertFrom-Json }
        return [pscustomobject]@{ ok = $true; uri = $Uri; error = $null; data = $data }
    }
    catch {
        return [pscustomobject]@{ ok = $false; uri = $Uri; error = "Invalid JSON: $($_.Exception.Message)"; data = $text }
    }
}

function Wait-Backend {
    param([int]$Port, [int]$Timeout)
    $deadline = (Get-Date).AddSeconds($Timeout)
    $last = $null
    do {
        $health = Invoke-JsonEndpoint -Uri "http://127.0.0.1:$Port/api/health" -Timeout 3
        if ($health.ok) {
            $version = Invoke-JsonEndpoint -Uri "http://127.0.0.1:$Port/api/version" -Timeout 3
            return [pscustomobject]@{ ok = $true; health = $health; version = $version }
        }
        $last = $health
        Start-Sleep -Seconds 1
    } while ((Get-Date) -lt $deadline)
    return [pscustomobject]@{ ok = $false; health = $last; version = $null }
}

function Get-ShortcutInfo {
    param([string]$Path)
    $info = [ordered]@{
        path = $Path
        exists = Test-Path -LiteralPath $Path
        target = $null
        working_directory = $null
        icon_location = $null
        error = $null
    }
    if (-not $info.exists) { return [pscustomobject]$info }
    try {
        $shell = New-Object -ComObject WScript.Shell
        $shortcut = $shell.CreateShortcut($Path)
        $info.target = $shortcut.TargetPath
        $info.working_directory = $shortcut.WorkingDirectory
        $info.icon_location = $shortcut.IconLocation
    }
    catch {
        $info.error = $_.Exception.Message
    }
    return [pscustomobject]$info
}

function Invoke-Uninstall {
    param([object]$RegistryEntry)
    $commandText = if ($RegistryEntry.QuietUninstallString) { $RegistryEntry.QuietUninstallString } else { $RegistryEntry.UninstallString }
    if ([string]::IsNullOrWhiteSpace($commandText)) { throw "Registry uninstall command is empty." }
    $command = Split-CommandLine $commandText
    if (-not (Test-PathUnderHyperBoostRoot -PathToCheck $command.file -InstallLocation $RegistryEntry.InstallLocation)) {
        throw "Refusing to run uninstaller outside confirmed HyperBoostX install root: $($command.file)"
    }
    $arguments = $command.arguments
    if ($arguments -notmatch '(^|\s)/S(\s|$)') { $arguments = ($arguments + " /S").Trim() }
    Start-Process -FilePath $command.file -ArgumentList $arguments -Wait -WindowStyle Hidden
}

function Write-ReportsAndExit {
    param([int]$ExitCode)
    $report = [ordered]@{
        generated_at = (Get-Date).ToUniversalTime().ToString("o")
        expected_version = $ExpectedVersion
        backend_port = $BackendPort
        installer_path = $InstallerPath
        is_admin = Test-IsAdmin
        old_registry_entries = $script:oldRegistryEntries
        new_registry_entries = $script:newRegistryEntries
        runtime_verifier_exit = $script:runtimeVerifierExit
        steps = $script:steps
        ok = -not ($script:steps | Where-Object { -not $_.ok })
    }
    $report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $jsonPath -Encoding UTF8
    $statusText = if ($report.ok) { "PASS" } else { "FAIL" }
    $lines = @(
        "# Owner Admin Stable Gate Result v2.10.0",
        "",
        "Expected version: $ExpectedVersion",
        "Backend port: $BackendPort",
        "Installer: $InstallerPath",
        "Status: $statusText",
        "",
        "| Step | Status | Detail |",
        "| --- | --- | --- |"
    )
    foreach ($step in $script:steps) {
        $status = if ($step.ok) { "PASS" } else { "FAIL" }
        $detail = ([string]$step.detail).Replace("|", "/")
        $lines += "| $($step.name) | $status | $detail |"
    }
    $lines | Set-Content -LiteralPath $mdPath -Encoding UTF8
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepoRoot "scripts\redact_release_evidence.ps1") -RepoRoot $RepoRoot -Paths $jsonPath,$mdPath | Out-Null
    Write-Host "Owner admin stable gate report: $jsonPath"
    Write-Host "Owner admin stable gate docs: $mdPath"
    exit $ExitCode
}

if (-not (Test-IsAdmin)) {
    Add-Step "administrator shell required" $false "STABLE_BLOCKED_ELEVATION: run this script from Windows PowerShell as Administrator."
    Write-ReportsAndExit 1
}

Add-Step "administrator shell" $true "Running elevated."
Add-Step "installer exists" (Test-Path -LiteralPath $InstallerPath) $InstallerPath
if (-not (Test-Path -LiteralPath $InstallerPath)) { Write-ReportsAndExit 1 }

$script:oldRegistryEntries = Get-HyperBoostRegistryEntries
$oldPrimary = $script:oldRegistryEntries | Select-Object -First 1
$oldInstallLocation = if ($oldPrimary -and $oldPrimary.InstallLocation) { $oldPrimary.InstallLocation } else { Join-Path $env:ProgramFiles "HyperBoostX" }
Add-Step "record old installed version" $true ($(if ($oldPrimary) { "$($oldPrimary.DisplayVersion) at $oldInstallLocation" } else { "No previous registry entry found." }))

$stoppedBefore = Stop-HyperBoostProcesses -InstallLocation $oldInstallLocation
Add-Step "stop existing HyperBoostX processes" $true (($stoppedBefore | ConvertTo-Json -Compress -Depth 4))

if ($oldPrimary) {
    try {
        Invoke-Uninstall -RegistryEntry $oldPrimary
        Add-Step "uninstall previous HyperBoostX" $true "$($oldPrimary.DisplayVersion) via registry uninstall command"
    }
    catch {
        Add-Step "uninstall previous HyperBoostX" $false $_.Exception.Message
        Write-ReportsAndExit 1
    }
}
else {
    Add-Step "uninstall previous HyperBoostX" $true "No previous install found."
}

Start-Sleep -Seconds 2
$afterUninstall = Get-HyperBoostRegistryEntries
Add-Step "old registry entry removed" (@($afterUninstall).Count -eq 0) (($afterUninstall | ConvertTo-Json -Compress -Depth 5))
if (@($afterUninstall).Count -gt 0) { Write-ReportsAndExit 1 }

Start-Process -FilePath $InstallerPath -ArgumentList "/S" -Wait -WindowStyle Hidden
Add-Step "silent install current installer" $true $InstallerPath

$script:newRegistryEntries = Get-HyperBoostRegistryEntries
$primary = $script:newRegistryEntries | Select-Object -First 1
$installLocation = if ($primary -and $primary.InstallLocation) { $primary.InstallLocation } else { Join-Path $env:ProgramFiles "HyperBoostX" }
$launcherPath = Join-Path $installLocation "HyperBoostX.exe"
$wpfPath = Join-Path $installLocation "runtime\wpf\HyperBoostX.exe"
$backendPath = Join-Path $installLocation "runtime\backend\hyperboost_backend.exe"
$installedActionMapPath = Join-Path $installLocation "runtime\wpf\Data\ui_action_map_v2_10.json"

Add-Step "registry DisplayVersion matches expected" ($primary -and $primary.DisplayVersion -eq $ExpectedVersion) ($(if ($primary) { $primary.DisplayVersion } else { "missing" }))
Add-Step "registry Publisher recorded" ($primary -and -not [string]::IsNullOrWhiteSpace($primary.Publisher)) ($(if ($primary) { $primary.Publisher } else { "missing" }))
Add-Step "launcher installed" (Test-Path -LiteralPath $launcherPath) $launcherPath
Add-Step "WPF runtime installed" (Test-Path -LiteralPath $wpfPath) $wpfPath
Add-Step "backend runtime installed" (Test-Path -LiteralPath $backendPath) $backendPath
Add-Step "installed action map present" (Test-Path -LiteralPath $installedActionMapPath) $installedActionMapPath
foreach ($check in (Test-HyperBoostXActionMapContract -ActionMapPath $installedActionMapPath -ExpectedVersion $ExpectedVersion -NamePrefix "installed action map").checks) {
    Add-Step $check.name $check.ok $check.evidence
}

$desktopShortcuts = @(
    (Join-Path ([Environment]::GetFolderPath("Desktop")) "HyperBoostX.lnk"),
    (Join-Path ([Environment]::GetFolderPath("CommonDesktopDirectory")) "HyperBoostX.lnk")
) | Select-Object -Unique
$desktopInfos = @($desktopShortcuts | ForEach-Object { Get-ShortcutInfo $_ })
$startMenuPath = Join-Path ([Environment]::GetFolderPath("CommonPrograms")) "HyperBoostX\HyperBoostX.lnk"
$startInfo = Get-ShortcutInfo $startMenuPath
$desktopOk = @($desktopInfos | Where-Object { $_.exists -and $_.target -eq $launcherPath }).Count -gt 0
$startOk = $startInfo.exists -and $startInfo.target -eq $launcherPath
Add-Step "desktop shortcut targets launcher" $desktopOk ($desktopInfos | ConvertTo-Json -Compress -Depth 5)
Add-Step "start menu shortcut targets launcher" $startOk ($startInfo | ConvertTo-Json -Compress -Depth 5)

if (-not (Test-Path -LiteralPath $launcherPath)) { Write-ReportsAndExit 1 }

$oldPort = $env:HYPERBOOSTX_BACKEND_PORT
try {
    $env:HYPERBOOSTX_BACKEND_PORT = [string]$BackendPort
    $launchProcess = Start-Process -FilePath $launcherPath -WorkingDirectory $installLocation -PassThru
    Add-Step "launch installed HyperBoostX" $true "pid=$($launchProcess.Id)"
}
catch {
    Add-Step "launch installed HyperBoostX" $false $_.Exception.Message
    Write-ReportsAndExit 1
}
finally {
    $env:HYPERBOOSTX_BACKEND_PORT = $oldPort
}

$backend = Wait-Backend -Port $BackendPort -Timeout $TimeoutSeconds
$versionValue = $null
$sessionTokenRequired = $false
if ($backend.health -and $backend.health.data) {
    try { $sessionTokenRequired = [bool]$backend.health.data.session_token_required } catch { $sessionTokenRequired = $false }
}
if ($backend.version -and $backend.version.data) {
    try { $versionValue = [string]$backend.version.data.version } catch { $versionValue = $null }
}
$runningAfterLaunch = Get-HyperBoostProcesses -InstallLocation $installLocation
$wpfRunning = @($runningAfterLaunch | Where-Object { $_.name -eq "HyperBoostX" -and $_.from_install }).Count -gt 0
Add-Step "backend health on port $BackendPort" ($backend.ok -and $backend.health.ok) ($(if ($backend.health) { $backend.health | ConvertTo-Json -Compress -Depth 5 } else { "missing" }))
Add-Step "backend version matches expected" ($versionValue -eq $ExpectedVersion) ($(if ($versionValue) { $versionValue } else { "missing" }))
$featureAudit = Invoke-JsonEndpoint -Uri "http://127.0.0.1:$BackendPort/api/features/audit" -Timeout 4
$featureStableVisible = Invoke-JsonEndpoint -Uri "http://127.0.0.1:$BackendPort/api/features/stable-visible" -Timeout 4
$featureNonReal = Invoke-JsonEndpoint -Uri "http://127.0.0.1:$BackendPort/api/features/non-real" -Timeout 4
$contract = Get-HyperBoostXReleaseContract
Add-Step "feature audit endpoint works" ($featureAudit.ok) ($(if ($featureAudit.ok) { $featureAudit.uri } else { $featureAudit.error }))
Add-Step "feature stable-visible endpoint works" ($featureStableVisible.ok) ($(if ($featureStableVisible.ok) { $featureStableVisible.uri } else { $featureStableVisible.error }))
Add-Step "feature non-real endpoint works" ($featureNonReal.ok) ($(if ($featureNonReal.ok) { $featureNonReal.uri } else { $featureNonReal.error }))
if ($featureAudit.ok -and $featureAudit.data) {
    Add-Step "feature audit stable_ui_ok true" ([bool]$featureAudit.data.ok -eq $true) ($(if ($featureAudit.data.errors) { ($featureAudit.data.errors -join "; ") } else { "ok=$($featureAudit.data.ok)" }))
    Add-Step "feature audit stable_visible_features matches contract" ([int]$featureAudit.data.counts.stable_visible_features -eq $contract.ExpectedStableMenus) "actual=$($featureAudit.data.counts.stable_visible_features); expected=$($contract.ExpectedStableMenus)"
    Add-Step "feature audit stable_visible_buttons matches contract" ([int]$featureAudit.data.counts.stable_visible_buttons -eq $contract.ExpectedStableButtons) "actual=$($featureAudit.data.counts.stable_visible_buttons); expected=$($contract.ExpectedStableButtons)"
    Add-Step "feature audit non_real_visible_in_stable is 0" ([int]$featureAudit.data.counts.non_real_visible_in_stable -eq 0) "actual=$($featureAudit.data.counts.non_real_visible_in_stable); expected=0"
} else {
    Add-Step "feature audit stable_ui_ok true" $false "feature audit endpoint unavailable"
    Add-Step "feature audit stable_visible_features matches contract" $false "feature audit endpoint unavailable"
    Add-Step "feature audit stable_visible_buttons matches contract" $false "feature audit endpoint unavailable"
    Add-Step "feature audit non_real_visible_in_stable is 0" $false "feature audit endpoint unavailable"
}
Add-Step "stable-visible count matches contract" ($featureStableVisible.ok -and [int]$featureStableVisible.data.count -eq $contract.ExpectedStableMenus) ($(if ($featureStableVisible.ok) { "actual=$($featureStableVisible.data.count); expected=$($contract.ExpectedStableMenus)" } else { "stable-visible endpoint unavailable" }))
Add-Step "non-real count is 0" ($featureNonReal.ok -and [int]$featureNonReal.data.count -eq 0) ($(if ($featureNonReal.ok) { "actual=$($featureNonReal.data.count); expected=0" } else { "non-real endpoint unavailable" }))
Add-Step "WPF installed smoke" $wpfRunning ($runningAfterLaunch | ConvertTo-Json -Compress -Depth 5)
Add-Step "token sync inferred" ($sessionTokenRequired -and $wpfRunning) "session_token_required=$sessionTokenRequired; wpf_running=$wpfRunning"

$stoppedInstalled = Stop-HyperBoostProcesses -InstallLocation $installLocation -OnlyInstalled
Start-Sleep -Seconds 2
$orphans = @(Get-HyperBoostProcesses -InstallLocation $installLocation | Where-Object { $_.from_install })
Add-Step "close installed app" $true ($stoppedInstalled | ConvertTo-Json -Compress -Depth 5)
Add-Step "no orphan installed processes" ($orphans.Count -eq 0) ($orphans | ConvertTo-Json -Compress -Depth 5)

$postInstallRegistry = Get-HyperBoostRegistryEntries | Select-Object -First 1
if (-not $postInstallRegistry) {
    Add-Step "silent uninstall before reinstall" $false "Registry entry missing before silent uninstall."
    Write-ReportsAndExit 1
}
try {
    Invoke-Uninstall -RegistryEntry $postInstallRegistry
    Add-Step "silent uninstall" $true "Quiet uninstall completed."
}
catch {
    Add-Step "silent uninstall" $false $_.Exception.Message
    Write-ReportsAndExit 1
}

Start-Sleep -Seconds 2
$afterSilentUninstall = Get-HyperBoostRegistryEntries
Add-Step "silent uninstall removed registry" (@($afterSilentUninstall).Count -eq 0) ($afterSilentUninstall | ConvertTo-Json -Compress -Depth 5)
if (@($afterSilentUninstall).Count -gt 0) { Write-ReportsAndExit 1 }

Start-Process -FilePath $InstallerPath -ArgumentList "/S" -Wait -WindowStyle Hidden
Add-Step "silent reinstall" $true $InstallerPath

$runtimeVerifier = Join-Path $ScriptDir "runtime_verifier.ps1"
& powershell -NoProfile -ExecutionPolicy Bypass -File $runtimeVerifier -RepoRoot $RepoRoot -ExpectedVersion $ExpectedVersion -LaunchInstalledApp -StopAfterProbe -BackendPort $BackendPort -ProbeTimeoutSeconds $TimeoutSeconds
$script:runtimeVerifierExit = $LASTEXITCODE
Add-Step "runtime verifier after reinstall" ($script:runtimeVerifierExit -eq 0) "exit=$script:runtimeVerifierExit"

Write-ReportsAndExit $(if (-not ($script:steps | Where-Object { -not $_.ok })) { 0 } else { 1 })

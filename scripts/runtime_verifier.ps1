[CmdletBinding()]
param(
    [string]$RepoRoot = "",
    [string]$ExpectedVersion = "",
    [switch]$LaunchInstalledApp,
    [switch]$StopAfterProbe,
    [int]$ProbeTimeoutSeconds = 35,
    [int]$BackendPort = 5000
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptDir = if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..")).Path
}

. (Join-Path $RepoRoot "scripts\lib\HyperBoostXReleaseContract.ps1")

function Write-Stage {
    param([string]$Name)
    Write-Host "[runtime_verifier] $Name"
    if ($script:tracePath) {
        Add-Content -LiteralPath $script:tracePath -Value ("{0} {1}" -f (Get-Date -Format o), $Name) -Encoding UTF8
    }
}

if ([string]::IsNullOrWhiteSpace($ExpectedVersion)) {
    $ExpectedVersion = (Get-Content -LiteralPath (Join-Path $RepoRoot "VERSION") -Raw).Trim()
}

$outDir = Join-Path $RepoRoot "docs\runtime-audit"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$jsonPath = Join-Path $outDir "runtime_audit_report.json"
$mdPath = Join-Path $outDir "runtime_audit_report.md"
$script:tracePath = Join-Path $outDir "runtime_verifier_trace.log"
Set-Content -LiteralPath $script:tracePath -Value "runtime verifier trace $(Get-Date -Format o)" -Encoding UTF8
$launcherLog = Join-Path $env:LOCALAPPDATA "HyperBoost X\logs\hyperboost-launcher.log"

$script:checks = @()

function Add-Check {
    param([string]$Name, [bool]$Ok, [string]$Evidence)
    $script:checks += [pscustomobject]@{ name = $Name; ok = $Ok; evidence = $Evidence }
}

function Test-IsAdmin {
    $principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-HyperBoostRegistryEntries {
    $roots = @(
        "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall",
        "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall",
        "HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
        "HKCU:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    )

    $items = @()
    function Get-RegistryValue {
        param([object]$Item, [string[]]$PropertyNames, [string]$Name)
        if ($PropertyNames -contains $Name) {
            return $Item.$Name
        }
        return $null
    }

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
                    InstallDate = Get-RegistryValue -Item $p -PropertyNames $propertyNames -Name "InstallDate"
                    EstimatedSize = Get-RegistryValue -Item $p -PropertyNames $propertyNames -Name "EstimatedSize"
                }
            }
        }
    }
    return @($items)
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

    $info.target = "exists; target resolution skipped to avoid COM shell hangs"

    return [pscustomobject]$info
}

function Invoke-JsonEndpoint {
    param([string]$Uri, [int]$TimeoutSeconds = 2)

    $curl = Get-Command curl.exe -ErrorAction SilentlyContinue
    if (-not $curl) { return [pscustomobject]@{ ok = $false; error = "curl.exe not found"; data = $null; uri = $Uri } }

    $previous = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & $curl.Source --silent --show-error --max-time $TimeoutSeconds --noproxy "*" $Uri 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previous
    }

    $text = (($output | ForEach-Object { $_.ToString() }) -join "`n").Trim()
    if ($exitCode -ne 0) {
        return [pscustomobject]@{ ok = $false; error = $text; data = $null; uri = $Uri }
    }

    try {
        $data = if ([string]::IsNullOrWhiteSpace($text)) { $null } else { $text | ConvertFrom-Json }
        return [pscustomobject]@{ ok = $true; error = $null; data = $data; uri = $Uri }
    }
    catch {
        return [pscustomobject]@{ ok = $false; error = "Invalid JSON: $($_.Exception.Message)"; data = $text; uri = $Uri }
    }
}

function Test-TcpPortOpen {
    param([int]$Port, [int]$TimeoutMilliseconds = 80)

    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $async = $client.BeginConnect("127.0.0.1", $Port, $null, $null)
        $connected = $async.AsyncWaitHandle.WaitOne($TimeoutMilliseconds, $false)
        if (-not $connected) {
            $client.Close()
            return $false
        }
        $client.EndConnect($async)
        return $true
    }
    catch {
        return $false
    }
    finally {
        try { $client.Close() } catch { }
    }
}

function Get-BackendCandidatePorts {
    $ports = New-Object System.Collections.Generic.List[int]
    foreach ($port in @($BackendPort) + (5055..5095) + (51200..51300) + @(5000)) {
        if ($port -ge 1024 -and $port -le 65535 -and -not $ports.Contains([int]$port)) {
            $ports.Add([int]$port)
        }
    }
    return $ports
}

function Get-ListeningTcpPorts {
    try {
        return @(
            Get-NetTCPConnection -State Listen -ErrorAction Stop |
                Where-Object { $_.LocalAddress -in @("127.0.0.1", "0.0.0.0", "::", "::1") } |
                Select-Object -ExpandProperty LocalPort -Unique
        )
    }
    catch {
        return @()
    }
}

function Find-Backend {
    param([int]$TimeoutSeconds)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $last = $null
    do {
        $candidatePorts = @(Get-BackendCandidatePorts)
        $listeningPorts = @(Get-ListeningTcpPorts)
        $portsToProbe = if ($listeningPorts.Count -gt 0) {
            @($candidatePorts | Where-Object { $listeningPorts -contains $_ })
        } else {
            @()
        }

        foreach ($port in $portsToProbe) {
            if ((Get-Date) -ge $deadline) { break }
            $base = "http://127.0.0.1:$port"
            $health = Invoke-JsonEndpoint "$base/api/health" 2
            if ($health.ok) {
                $version = Invoke-JsonEndpoint "$base/api/version" 2
                return [pscustomobject]@{ found = $true; port = $port; base_url = $base; health = $health; version = $version }
            }
            $last = $health
        }
        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $deadline)

    return [pscustomobject]@{ found = $false; port = $null; base_url = $null; health = $last; version = $null }
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

function Stop-InstalledHyperBoostProcesses {
    param([string]$InstallLocation)

    $stopped = @()
    foreach ($proc in Get-HyperBoostProcesses $InstallLocation) {
        if (-not $proc.from_install) { continue }
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

Write-Stage "reading uninstall registry"
$registryEntries = Get-HyperBoostRegistryEntries
$primaryRegistry = $registryEntries | Where-Object { $_.DisplayName -eq "HyperBoostX" } | Select-Object -First 1
if (-not $primaryRegistry -and @($registryEntries).Count -gt 0) {
    $primaryRegistry = @($registryEntries)[0]
}
$installLocation = if ($primaryRegistry -and $primaryRegistry.InstallLocation) { $primaryRegistry.InstallLocation } else { Join-Path $env:ProgramFiles "HyperBoostX" }
$launcherPath = Join-Path $installLocation "HyperBoostX.exe"
$wpfPath = Join-Path $installLocation "runtime\wpf\HyperBoostX.exe"
$backendPath = Join-Path $installLocation "runtime\backend\hyperboost_backend.exe"
$installedActionMapPath = Join-Path $installLocation "runtime\wpf\Data\ui_action_map_v2_10.json"

$desktopPaths = @(
    (Join-Path ([Environment]::GetFolderPath("Desktop")) "HyperBoostX.lnk"),
    (Join-Path ([Environment]::GetFolderPath("CommonDesktopDirectory")) "HyperBoostX.lnk")
) | Select-Object -Unique
$startMenuPath = Join-Path ([Environment]::GetFolderPath("CommonPrograms")) "HyperBoostX\HyperBoostX.lnk"
Write-Stage "checking shortcuts"
$desktopShortcuts = @($desktopPaths | ForEach-Object { Get-ShortcutInfo $_ })
$startShortcut = Get-ShortcutInfo $startMenuPath

$launchProcess = $null
$launchError = $null
if ($LaunchInstalledApp -and (Test-Path -LiteralPath $launcherPath)) {
    Write-Stage "launching installed app"
    $oldPort = $env:HYPERBOOSTX_BACKEND_PORT
    try {
        $env:HYPERBOOSTX_BACKEND_PORT = [string]$BackendPort
        $launchProcess = Start-Process -FilePath $launcherPath -WorkingDirectory $installLocation -PassThru
        Start-Sleep -Seconds 2
    }
    catch {
        $launchError = $_.Exception.Message
    }
    finally {
        $env:HYPERBOOSTX_BACKEND_PORT = $oldPort
    }
}

Write-Stage "probing backend"
$backend = Find-Backend -TimeoutSeconds $ProbeTimeoutSeconds
Write-Stage "checking processes"
$processesBeforeStop = Get-HyperBoostProcesses $installLocation
$launcherLogTail = @()
if (Test-Path -LiteralPath $launcherLog) {
    $launcherLogTail = @("log exists; tail read skipped to avoid blocking on a locked runtime log")
}

$healthVersion = $null
$sessionTokenRequired = $false
$featureAudit = $null
$featureStableVisible = $null
$featureNonReal = $null
if ($backend.found -and $backend.health -and $backend.health.data) {
    try { $sessionTokenRequired = [bool]$backend.health.data.session_token_required } catch { $sessionTokenRequired = $false }
}
if ($backend.found -and $backend.version -and $backend.version.data) {
    try { $healthVersion = [string]$backend.version.data.version } catch { $healthVersion = $null }
}
if ($backend.found) {
    Write-Stage "probing feature registry endpoints"
    $featureAudit = Invoke-JsonEndpoint "$($backend.base_url)/api/features/audit" 3
    $featureStableVisible = Invoke-JsonEndpoint "$($backend.base_url)/api/features/stable-visible" 3
    $featureNonReal = Invoke-JsonEndpoint "$($backend.base_url)/api/features/non-real" 3
}

$wpfRunningFromInstall = @($processesBeforeStop | Where-Object { $_.name -eq "HyperBoostX" -and $_.from_install }).Count -gt 0
$tokenSyncEvidence = if ($LaunchInstalledApp -and $sessionTokenRequired -and $wpfRunningFromInstall) {
    "INFERRED_FROM_LAUNCHER_ENV_AND_TOKEN_REQUIRED_HEALTH"
} elseif ($LaunchInstalledApp) {
    "NOT_VERIFIED"
} else {
    "NOT_TESTED_NO_LAUNCH"
}

$stopped = @()
if ($StopAfterProbe) {
    Write-Stage "stopping installed processes"
    $stopped = Stop-InstalledHyperBoostProcesses $installLocation
    Start-Sleep -Seconds 2
}
$processesAfterStop = Get-HyperBoostProcesses $installLocation
$orphanOk = if ($StopAfterProbe) { @($processesAfterStop | Where-Object { $_.from_install }).Count -eq 0 } else { $false }

Add-Check "registry installed" ($null -ne $primaryRegistry) $installLocation
Add-Check "DisplayVersion matches source" ($primaryRegistry -and $primaryRegistry.DisplayVersion -eq $ExpectedVersion) ($(if ($primaryRegistry) { $primaryRegistry.DisplayVersion } else { "missing" }))
Add-Check "launcher exists" (Test-Path -LiteralPath $launcherPath) $launcherPath
Add-Check "WPF runtime exists" (Test-Path -LiteralPath $wpfPath) $wpfPath
Add-Check "backend runtime exists" (Test-Path -LiteralPath $backendPath) $backendPath
Add-Check "desktop shortcut exists" (@($desktopShortcuts | Where-Object { $_.exists }).Count -gt 0) (($desktopShortcuts | ConvertTo-Json -Compress -Depth 4))
Add-Check "start menu shortcut exists" $startShortcut.exists ($startShortcut | ConvertTo-Json -Compress -Depth 4)
Add-Check "backend health works" ($backend.found -and $backend.health.ok) ($(if ($backend.found) { $backend.base_url } else { "not found" }))
Add-Check "backend version matches source" ($healthVersion -eq $ExpectedVersion) ($(if ($healthVersion) { $healthVersion } else { "missing" }))
Add-Check "installed action map exists" (Test-Path -LiteralPath $installedActionMapPath) $installedActionMapPath
foreach ($check in (Test-HyperBoostXActionMapContract -ActionMapPath $installedActionMapPath -ExpectedVersion $ExpectedVersion -NamePrefix "installed action map").checks) {
    Add-Check $check.name $check.ok $check.evidence
}
Add-Check "feature audit endpoint works" ($featureAudit -and $featureAudit.ok) ($(if ($featureAudit) { $featureAudit.uri } else { "not called" }))
Add-Check "feature stable-visible endpoint works" ($featureStableVisible -and $featureStableVisible.ok) ($(if ($featureStableVisible) { $featureStableVisible.uri } else { "not called" }))
Add-Check "feature non-real endpoint works" ($featureNonReal -and $featureNonReal.ok) ($(if ($featureNonReal) { $featureNonReal.uri } else { "not called" }))
if ($featureAudit -and $featureAudit.ok -and $featureAudit.data) {
    $auditCounts = $featureAudit.data.counts
    $contract = Get-HyperBoostXReleaseContract
    Add-Check "feature audit stable_ui_ok true" ([bool]$featureAudit.data.ok -eq $true) ($(if ($featureAudit.data.errors) { ($featureAudit.data.errors -join "; ") } else { "ok=$($featureAudit.data.ok)" }))
    Add-Check "feature audit stable_visible_features is 72" ([int]$auditCounts.stable_visible_features -eq $contract.ExpectedStableMenus) "actual=$($auditCounts.stable_visible_features); expected=$($contract.ExpectedStableMenus)"
    Add-Check "feature audit stable_visible_buttons is 596" ([int]$auditCounts.stable_visible_buttons -eq $contract.ExpectedStableButtons) "actual=$($auditCounts.stable_visible_buttons); expected=$($contract.ExpectedStableButtons)"
    Add-Check "feature audit non_real_visible_in_stable is 0" ([int]$auditCounts.non_real_visible_in_stable -eq $contract.ExpectedNonRealVisibleInStable) "actual=$($auditCounts.non_real_visible_in_stable); expected=$($contract.ExpectedNonRealVisibleInStable)"
}
else {
    Add-Check "feature audit stable_ui_ok true" $false "feature audit endpoint unavailable"
    Add-Check "feature audit stable_visible_features is 72" $false "feature audit endpoint unavailable"
    Add-Check "feature audit stable_visible_buttons is 596" $false "feature audit endpoint unavailable"
    Add-Check "feature audit non_real_visible_in_stable is 0" $false "feature audit endpoint unavailable"
}
if ($featureStableVisible -and $featureStableVisible.ok -and $featureStableVisible.data) {
    $contract = Get-HyperBoostXReleaseContract
    Add-Check "stable-visible count is 72" ([int]$featureStableVisible.data.count -eq $contract.ExpectedStableMenus) "actual=$($featureStableVisible.data.count); expected=$($contract.ExpectedStableMenus)"
}
else {
    Add-Check "stable-visible count is 72" $false "stable-visible endpoint unavailable"
}
if ($featureNonReal -and $featureNonReal.ok -and $featureNonReal.data) {
    Add-Check "non-real count is 0" ([int]$featureNonReal.data.count -eq 0) "actual=$($featureNonReal.data.count); expected=0"
}
else {
    Add-Check "non-real count is 0" $false "non-real endpoint unavailable"
}
Add-Check "WPF installed smoke" ($LaunchInstalledApp -and $wpfRunningFromInstall) ($(if ($LaunchInstalledApp) { "wpf_running_from_install=$wpfRunningFromInstall launch_error=$launchError" } else { "not launched" }))
Add-Check "token sync" ($tokenSyncEvidence -eq "INFERRED_FROM_LAUNCHER_ENV_AND_TOKEN_REQUIRED_HEALTH") $tokenSyncEvidence
Add-Check "no orphan process" ($StopAfterProbe -and $orphanOk) ($(if ($StopAfterProbe) { ($processesAfterStop | ConvertTo-Json -Compress -Depth 4) } else { "not tested without -StopAfterProbe" }))
Add-Check "legacy active runtime not detected" (-not ($processesBeforeStop | Where-Object { $_.path -match "legacy_ui|HyperBoostUI" })) ($processesBeforeStop | ConvertTo-Json -Compress -Depth 4)

$report = [ordered]@{
    generated_at = (Get-Date).ToUniversalTime().ToString("o")
    expected_version = $ExpectedVersion
    is_admin = Test-IsAdmin
    launch_installed_app = [bool]$LaunchInstalledApp
    stop_after_probe = [bool]$StopAfterProbe
    backend_probe_timeout_seconds = $ProbeTimeoutSeconds
    requested_backend_port = $BackendPort
    registry_entries = $registryEntries
    primary_registry = $primaryRegistry
    install_location = $installLocation
    launcher_path = $launcherPath
    wpf_path = $wpfPath
    backend_path = $backendPath
    installed_action_map_path = $installedActionMapPath
    desktop_shortcuts = $desktopShortcuts
    start_menu_shortcut = $startShortcut
    launch_process = if ($launchProcess) { [pscustomobject]@{ id = $launchProcess.Id; has_exited = $launchProcess.HasExited } } else { $null }
    launch_error = $launchError
    backend = $backend
    backend_version = $healthVersion
    feature_audit = $featureAudit
    feature_stable_visible = $featureStableVisible
    feature_non_real = $featureNonReal
    session_token_required = $sessionTokenRequired
    token_sync_status = $tokenSyncEvidence
    processes_before_stop = $processesBeforeStop
    stopped_processes = $stopped
    processes_after_stop = $processesAfterStop
    launcher_log_tail = $launcherLogTail
    checks = $checks
    ok = -not ($checks | Where-Object { -not $_.ok })
}

Write-Stage "writing reports"
$report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

$lines = @(
    "# HyperBoostX Installed Runtime Audit",
    "",
    "Expected version: $ExpectedVersion",
    "Install location: $installLocation",
    "Launch installed app: $([bool]$LaunchInstalledApp)",
    "Stop after probe: $([bool]$StopAfterProbe)",
    "Backend: $(if ($backend.found) { $backend.base_url } else { 'not found' })",
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
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepoRoot "scripts\redact_release_evidence.ps1") -RepoRoot $RepoRoot -Paths $jsonPath,$mdPath | Out-Null

Write-Host "Installed runtime report: $jsonPath"
if (-not $report.ok) { exit 1 }

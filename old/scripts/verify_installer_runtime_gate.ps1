param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")),
    [string]$ExpectedVersion = ""
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($ExpectedVersion)) {
    $ExpectedVersion = (Get-Content -LiteralPath (Join-Path $RepoRoot "VERSION") -Raw).Trim()
}

$docsDir = Join-Path $RepoRoot "docs"
$runtimeAuditDir = Join-Path $RepoRoot "docs\runtime-audit"
New-Item -ItemType Directory -Force -Path $docsDir | Out-Null
New-Item -ItemType Directory -Force -Path $runtimeAuditDir | Out-Null

$installerPath = Join-Path $RepoRoot "HyperBoostXInstaller.exe"
$reportCandidates = @(
    (Join-Path $runtimeAuditDir "owner_admin_stable_gate_report.json"),
    (Join-Path $RepoRoot "runtime_audit\owner_admin_stable_gate_report.json")
) | Where-Object { Test-Path -LiteralPath $_ }

$checks = New-Object System.Collections.Generic.List[object]
function Add-Check([string]$Name, [bool]$Ok, [string]$Detail, [string]$Severity = "blocker") {
    $script:checks.Add([pscustomobject]@{ name = $Name; ok = $Ok; detail = $Detail; severity = $Severity })
}

function Get-LatestReleaseInput {
    $candidates = New-Object System.Collections.Generic.List[object]

    foreach ($rootName in @("wpf", "launcher")) {
        $rootPath = Join-Path $RepoRoot $rootName
        if (-not (Test-Path -LiteralPath $rootPath)) { continue }
        Get-ChildItem -LiteralPath $rootPath -Recurse -File -Force |
            Where-Object {
                $_.FullName -notmatch '\\(bin|obj)\\' -and
                $_.Extension -in @(".cs", ".xaml", ".csproj", ".resx", ".json", ".ico")
            } |
            ForEach-Object { $candidates.Add($_) }
    }

    foreach ($pattern in @("app\*.py", "app\data\*")) {
        Get-ChildItem -Path (Join-Path $RepoRoot $pattern) -Recurse -File -Force -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -notmatch '\\(__pycache__|\.pytest_cache|venv|build|dist)\\' } |
            ForEach-Object { $candidates.Add($_) }
    }

    foreach ($fileName in @(
        "VERSION",
        "HyperBoostXInstaller.nsi",
        "scripts\build_release_local.ps1",
        "scripts\build_release_v2.10.0.ps1",
        "scripts\package_installer_v2.10.0.ps1",
        "scripts\lib\HyperBoostXReleaseContract.ps1"
    )) {
        $path = Join-Path $RepoRoot $fileName
        if (Test-Path -LiteralPath $path) {
            $candidates.Add((Get-Item -LiteralPath $path))
        }
    }

    $latest = @($candidates | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1)
    if (-not $latest) { return $null }
    return [pscustomobject]@{
        path = $latest.FullName
        last_write_utc = $latest.LastWriteTimeUtc
    }
}

function ConvertTo-UtcDateTime {
    param([string]$Value)
    if ([string]::IsNullOrWhiteSpace($Value)) { return $null }
    try {
        return ([datetimeoffset]::Parse($Value)).UtcDateTime
    }
    catch {
        return $null
    }
}

$installerItem = $null
Add-Check "installer artifact exists" (Test-Path -LiteralPath $installerPath) $installerPath
if (Test-Path -LiteralPath $installerPath) {
    $installerItem = Get-Item -LiteralPath $installerPath
    $hash = (Get-FileHash -LiteralPath $installerPath -Algorithm SHA256).Hash
    Add-Check "installer SHA256 available" (-not [string]::IsNullOrWhiteSpace($hash)) $hash
    $latestInput = Get-LatestReleaseInput
    if ($latestInput) {
        $fresh = $installerItem.LastWriteTimeUtc.AddSeconds(2) -ge $latestInput.last_write_utc
        Add-Check "installer newer than release inputs" $fresh ("installer_utc={0:o}; latest_input_utc={1:o}; latest_input={2}" -f $installerItem.LastWriteTimeUtc, $latestInput.last_write_utc, $latestInput.path)
    } else {
        Add-Check "installer newer than release inputs" $false "No release input files were found."
    }
}

if ($reportCandidates.Count -eq 0) {
    Add-Check "owner admin installed-runtime evidence present" $false "owner_admin_stable_gate_report.json not found" "partial"
} else {
    $reportPath = @($reportCandidates)[0]
    $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
    $reportGeneratedUtc = ConvertTo-UtcDateTime ([string]$report.generated_at)
    Add-Check "owner admin evidence present" $true $reportPath
    if ($installerItem -and $reportGeneratedUtc) {
        $reportFresh = $reportGeneratedUtc.AddSeconds(2) -ge $installerItem.LastWriteTimeUtc
        Add-Check "owner evidence newer than installer" $reportFresh ("report_utc={0:o}; installer_utc={1:o}" -f $reportGeneratedUtc, $installerItem.LastWriteTimeUtc)
    } else {
        Add-Check "owner evidence newer than installer" $false "Missing installer timestamp or report generated_at."
    }
    Add-Check "owner admin evidence expected version" ([string]$report.expected_version -eq $ExpectedVersion) "actual=$($report.expected_version); expected=$ExpectedVersion"
    Add-Check "owner admin evidence ok" ([bool]$report.ok -eq $true) "ok=$($report.ok)"
    Add-Check "runtime verifier exit 0" ([int]$report.runtime_verifier_exit -eq 0) "exit=$($report.runtime_verifier_exit)"

    $requiredSteps = @(
        "registry DisplayVersion matches expected",
        "desktop shortcut targets launcher",
        "start menu shortcut targets launcher",
        "backend health on port 5000",
        "backend version matches expected",
        "WPF installed smoke",
        "token sync inferred",
        "no orphan installed processes",
        "silent uninstall",
        "silent reinstall",
        "runtime verifier after reinstall"
    )
    foreach ($stepName in $requiredSteps) {
        $step = @($report.steps | Where-Object { $_.name -eq $stepName } | Select-Object -First 1)
        Add-Check $stepName ($step -and [bool]$step.ok) ($(if ($step) { $step.detail } else { "missing" }))
    }

    $screenshotDir = Join-Path $docsDir "screenshots\v2.10.0-final"
    $requiredScreenshots = @(
        "dashboard.png",
        "dashboard-after-scroll.png",
        "performance.png",
        "startup.png",
        "background-apps.png",
        "cleanup.png",
        "storage.png",
        "one-click-boost.png",
        "gaming-mode.png",
        "smart-recommendation.png",
        "gpu-center.png",
        "hardware-vendor-center.png",
        "gaming-booster.png",
        "streaming-center.png",
        "creator-mode.png",
        "network-booster.png",
        "dns-latency-tools.png",
        "privacy-center.png",
        "security-health.png",
        "apps-manager.png",
        "tweaks-center.png",
        "windows-features.png",
        "update-control.png",
        "repair-tools.png",
        "driver-update-center.png",
        "app-uninstaller.png",
        "restore-backup.png",
        "settings.png",
        "about.png"
    )
    foreach ($screenshotName in $requiredScreenshots) {
        $screenshotPath = Join-Path $screenshotDir $screenshotName
        $exists = Test-Path -LiteralPath $screenshotPath
        Add-Check "installed screenshot present: $screenshotName" $exists $screenshotPath
        if ($exists -and $reportGeneratedUtc) {
            $shotItem = Get-Item -LiteralPath $screenshotPath
            $shotFresh = $shotItem.LastWriteTimeUtc.AddSeconds(2) -ge $reportGeneratedUtc
            Add-Check "installed screenshot newer than owner gate: $screenshotName" $shotFresh ("screenshot_utc={0:o}; report_utc={1:o}" -f $shotItem.LastWriteTimeUtc, $reportGeneratedUtc)
        }
    }
}

$blockers = @($checks | Where-Object { -not $_.ok -and $_.severity -eq "blocker" })
$partials = @($checks | Where-Object { -not $_.ok -and $_.severity -eq "partial" })
$status = if ($blockers.Count -gt 0) { "BLOCKED" } elseif ($partials.Count -gt 0) { "PARTIAL" } else { "PASS" }

$jsonPath = Join-Path $runtimeAuditDir "installer_runtime_gate_report.json"
$mdPath = Join-Path $docsDir "INSTALLER_STABLE_GATE_v2.10.0.md"
$payload = [pscustomobject]@{
    generated_at = (Get-Date).ToUniversalTime().ToString("o")
    expected_version = $ExpectedVersion
    installer_path = $installerPath
    status = $status
    checks = $checks
}
$payload | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

$lines = @(
    "# Installer Stable Gate v2.10.0",
    "",
    "Generated: $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'))",
    "Expected version: $ExpectedVersion",
    "Status: $status",
    "",
    "| Check | Status | Detail |",
    "| --- | --- | --- |"
)
foreach ($check in $checks) {
    $checkStatus = if ($check.ok) { "PASS" } else { if ($check.severity -eq "partial") { "PARTIAL" } else { "FAIL" } }
    $detail = ([string]$check.detail).Replace("|", "/")
    $lines += "| $($check.name) | $checkStatus | $detail |"
}
$lines | Set-Content -LiteralPath $mdPath -Encoding UTF8

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepoRoot "scripts\redact_release_evidence.ps1") -RepoRoot $RepoRoot -Paths $jsonPath,$mdPath | Out-Null

Write-Host "Installer runtime gate docs: $mdPath"
if ($status -eq "PASS") {
    Write-Host "PASS: installer runtime gate" -ForegroundColor Green
    exit 0
}
if ($status -eq "PARTIAL") {
    Write-Host "PARTIAL: installer runtime gate" -ForegroundColor Yellow
    exit 2
}
Write-Host "BLOCKED: installer runtime gate" -ForegroundColor Red
exit 1

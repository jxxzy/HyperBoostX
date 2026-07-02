param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")),
    [int]$BackendPort = 5000,
    [int]$WaitSeconds = 45
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class HbxNativeWindow {
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
"@

$docsDir = Join-Path $RepoRoot "docs"
$runtimeAuditDir = Join-Path $docsDir "runtime-audit"
$screenshotDir = Join-Path $docsDir "screenshots\v2.10.0-final"
New-Item -ItemType Directory -Force -Path $runtimeAuditDir | Out-Null
New-Item -ItemType Directory -Force -Path $screenshotDir | Out-Null

$installCandidates = @(
    (Join-Path $env:ProgramFiles "HyperBoostX\HyperBoostX.exe")
)
if (${env:ProgramFiles(x86)}) {
    $installCandidates += (Join-Path ${env:ProgramFiles(x86)} "HyperBoostX\HyperBoostX.exe")
}
$launcherPath = $installCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $launcherPath) {
    throw "Installed HyperBoostX launcher was not found."
}
$installDir = Split-Path -Parent $launcherPath
$uiSettingsPath = Join-Path $env:LOCALAPPDATA "HyperBoost X\config\ui_settings.json"
$uiSettingsExisted = Test-Path -LiteralPath $uiSettingsPath
$uiSettingsOriginal = $null
if ($uiSettingsExisted) {
    try { $uiSettingsOriginal = Get-Content -LiteralPath $uiSettingsPath -Raw } catch { $uiSettingsOriginal = $null }
}

function Get-InstalledHyperBoostProcesses {
    $rows = @()
    foreach ($name in @("HyperBoostX", "HyperBoostLauncher", "HyperBoostUI", "hyperboost_backend")) {
        foreach ($proc in Get-Process -Name $name -ErrorAction SilentlyContinue) {
            $path = $null
            try { $path = $proc.Path } catch { $path = $null }
            $fromInstall = $false
            if ($path) {
                try { $fromInstall = [System.IO.Path]::GetFullPath($path).StartsWith($installDir, [System.StringComparison]::OrdinalIgnoreCase) } catch { $fromInstall = $false }
            }
            if ($fromInstall) { $rows += $proc }
        }
    }
    return @($rows)
}

function Wait-HyperBoostWindow {
    param([int]$TimeoutSeconds)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        foreach ($proc in Get-InstalledHyperBoostProcesses | Where-Object { $_.ProcessName -eq "HyperBoostX" }) {
            $proc.Refresh()
            if ($proc.MainWindowHandle -ne [IntPtr]::Zero) {
                return $proc
            }
        }
        Start-Sleep -Milliseconds 300
    } while ((Get-Date) -lt $deadline)
    throw "Timed out waiting for installed HyperBoostX window."
}

function Invoke-NavButton {
    param(
        [IntPtr]$WindowHandle,
        [string]$Label
    )

    $deadline = (Get-Date).AddSeconds(10)
    do {
        $root = [System.Windows.Automation.AutomationElement]::FromHandle($WindowHandle)
        $nameCondition = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::NameProperty,
            $Label
        )
        $buttonCondition = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Button
        )
        $condition = New-Object System.Windows.Automation.AndCondition($nameCondition, $buttonCondition)
        $button = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
        if ($button) {
            $pattern = $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
            $pattern.Invoke()
            Start-Sleep -Milliseconds 900
            return $true
        }
        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)
    return $false
}

function Capture-Window {
    param(
        [IntPtr]$WindowHandle,
        [string]$Path
    )

    [HbxNativeWindow]::ShowWindow($WindowHandle, 9) | Out-Null
    [HbxNativeWindow]::SetForegroundWindow($WindowHandle) | Out-Null
    Start-Sleep -Milliseconds 500

    $rect = New-Object HbxNativeWindow+RECT
    if (-not [HbxNativeWindow]::GetWindowRect($WindowHandle, [ref]$rect)) {
        throw "Unable to read HyperBoostX window rectangle."
    }
    $width = [Math]::Max(1, $rect.Right - $rect.Left)
    $height = [Math]::Max(1, $rect.Bottom - $rect.Top)
    $bitmap = New-Object System.Drawing.Bitmap($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size($width, $height)))
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$pages = @(
    @{ file = "dashboard.png"; label = "Dashboard"; requireClick = $false },
    @{ file = "dashboard-after-scroll.png"; label = "Dashboard"; requireClick = $true; scrollAfterClick = $true },
    @{ file = "performance.png"; label = "Performance"; requireClick = $true },
    @{ file = "startup.png"; label = "Startup"; requireClick = $true },
    @{ file = "background-apps.png"; label = "Background Apps"; requireClick = $true },
    @{ file = "cleanup.png"; label = "Cleanup"; requireClick = $true },
    @{ file = "storage.png"; label = "Storage"; requireClick = $true },
    @{ file = "one-click-boost.png"; label = "One Click Boost"; requireClick = $true },
    @{ file = "gaming-mode.png"; label = "Gaming Mode"; requireClick = $true },
    @{ file = "smart-recommendation.png"; label = "Smart Recommendation"; requireClick = $true },
    @{ file = "gpu-center.png"; label = "GPU Center"; requireClick = $true },
    @{ file = "gaming-booster.png"; label = "Gaming Booster"; requireClick = $true },
    @{ file = "streaming-center.png"; label = "Streaming Center"; requireClick = $true },
    @{ file = "creator-mode.png"; label = "Creator Mode"; requireClick = $true },
    @{ file = "network-booster.png"; label = "Network Booster"; requireClick = $true },
    @{ file = "dns-latency-tools.png"; label = "DNS & Latency Tools"; requireClick = $true },
    @{ file = "privacy-center.png"; label = "Privacy Center"; requireClick = $true },
    @{ file = "security-health.png"; label = "Security & Health"; requireClick = $true },
    @{ file = "apps-manager.png"; label = "Apps Manager"; requireClick = $true },
    @{ file = "tweaks-center.png"; label = "Tweaks Center"; requireClick = $true },
    @{ file = "windows-features.png"; label = "Windows Features"; requireClick = $true },
    @{ file = "update-control.png"; label = "Update Control"; requireClick = $true },
    @{ file = "repair-tools.png"; label = "Repair Tools"; requireClick = $true },
    @{ file = "driver-update-center.png"; label = "Driver & Update Center"; requireClick = $true },
    @{ file = "app-uninstaller.png"; label = "App Uninstaller"; requireClick = $true },
    @{ file = "restore-backup.png"; label = "Restore & Backup"; requireClick = $true },
    @{ file = "settings.png"; label = "App Settings"; requireClick = $true },
    @{ file = "about.png"; label = "About App"; requireClick = $true }
)

$oldPort = $env:HYPERBOOSTX_BACKEND_PORT
$results = New-Object System.Collections.Generic.List[object]
try {
    $env:HYPERBOOSTX_BACKEND_PORT = [string]$BackendPort
    $configDir = Split-Path -Parent $uiSettingsPath
    New-Item -ItemType Directory -Force -Path $configDir | Out-Null
    [pscustomobject]@{
        configSchemaVersion = 2
        migrationHistory = @("screenshot_evidence_profile", "schema_v2")
        lastMigrationStatus = "screenshot_evidence_profile"
        enableAnimations = $true
        reduceMotion = $false
        accentColor = "Cyan"
        mode = "Beginner"
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $uiSettingsPath -Encoding UTF8

    Start-Process -FilePath $launcherPath -WorkingDirectory $installDir | Out-Null
    $windowProcess = Wait-HyperBoostWindow -TimeoutSeconds $WaitSeconds
    $handle = $windowProcess.MainWindowHandle
    Start-Sleep -Seconds 2

    foreach ($page in $pages) {
        $clicked = $true
        if ($page.requireClick) {
            $clicked = Invoke-NavButton -WindowHandle $handle -Label $page.label
        }
        if ($clicked -and $page.scrollAfterClick) {
            [HbxNativeWindow]::SetForegroundWindow($handle) | Out-Null
            Start-Sleep -Milliseconds 300
            [System.Windows.Forms.SendKeys]::SendWait("{PGDN}")
            Start-Sleep -Milliseconds 800
        }
        $targetPath = Join-Path $screenshotDir $page.file
        if ($clicked) {
            Capture-Window -WindowHandle $handle -Path $targetPath
        }
        $item = if (Test-Path -LiteralPath $targetPath) { Get-Item -LiteralPath $targetPath } else { $null }
        $results.Add([pscustomobject]@{
            file = $page.file
            label = $page.label
            clicked = $clicked
            path = $targetPath
            exists = [bool]$item
            bytes = if ($item) { $item.Length } else { 0 }
            last_write_utc = if ($item) { $item.LastWriteTimeUtc.ToString("o") } else { $null }
        })
    }
}
finally {
    $env:HYPERBOOSTX_BACKEND_PORT = $oldPort
    foreach ($proc in Get-InstalledHyperBoostProcesses) {
        try { Stop-Process -Id $proc.Id -Force -ErrorAction Stop } catch { }
    }
    if ($uiSettingsExisted -and $null -ne $uiSettingsOriginal) {
        Set-Content -LiteralPath $uiSettingsPath -Value $uiSettingsOriginal -Encoding UTF8
    } elseif (-not $uiSettingsExisted -and (Test-Path -LiteralPath $uiSettingsPath)) {
        Remove-Item -LiteralPath $uiSettingsPath -Force -ErrorAction SilentlyContinue
    }
}

$ok = -not ($results | Where-Object { -not $_.clicked -or -not $_.exists -or $_.bytes -le 0 })
$jsonPath = Join-Path $runtimeAuditDir "installed_screenshot_report.json"
$mdPath = Join-Path $docsDir "SCREENSHOT_REVIEW_v2.10.0.md"
$payload = [pscustomobject]@{
    generated_at = (Get-Date).ToUniversalTime().ToString("o")
    installed_launcher = $launcherPath
    backend_port = $BackendPort
    ok = $ok
    screenshots = $results
}
$payload | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

$lines = @(
    "# Screenshot Review v2.10.0",
    "",
    "Generated: $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'))",
    "Installed launcher: $launcherPath",
    "Status: $(if ($ok) { 'PASS' } else { 'BLOCKED' })",
    "",
    "| Page | Clicked | Captured | Bytes | File |",
    "| --- | --- | --- | ---: | --- |"
)
foreach ($result in $results) {
    $lines += "| $($result.label) | $($result.clicked) | $($result.exists) | $($result.bytes) | $($result.path) |"
}
$lines += ""
$lines += "Review notes:"
$lines += "- Screenshots are captured from the installed app, not the source tree."
$lines += "- Capture uses a temporary Beginner evidence profile and restores the previous local UI settings afterward."
$lines += "- Dashboard evidence must show Live Hardware Snapshot and Smart Scan Results, with no fake score rings or template placement panels."
$lines += "- Settings and About are purpose-built pages, not generic placement templates."
$lines | Set-Content -LiteralPath $mdPath -Encoding UTF8

Write-Host "Installed screenshot report: $jsonPath"
Write-Host "Screenshot review docs: $mdPath"
if ($ok) { exit 0 }
exit 1

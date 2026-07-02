param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

$corePages = @(
    @{ Key = "Dashboard"; View = "DashboardView"; Generated = $false },
    @{ Key = "PerformanceBoost"; View = "PerformanceBoostView"; Generated = $true },
    @{ Key = "StartupManager"; View = "StartupManagerView"; Generated = $true },
    @{ Key = "BackgroundApps"; View = "BackgroundAppsView"; Generated = $true },
    @{ Key = "Cleanup"; View = "CleanupView"; Generated = $true },
    @{ Key = "Storage"; View = "StorageView"; Generated = $true },
    @{ Key = "OneClickBoost"; View = "OneClickBoostView"; Generated = $true },
    @{ Key = "AutoGamingMode"; View = "AutoGamingModeView"; Generated = $true },
    @{ Key = "AIPerformanceAdvisor"; View = "AIPerformanceAdvisorView"; Generated = $true },
    @{ Key = "GpuCenter"; View = "GpuCenterView"; Generated = $true },
    @{ Key = "GamingBooster"; View = "GamingBoosterView"; Generated = $true },
    @{ Key = "StreamingCenter"; View = "StreamingCenterView"; Generated = $false },
    @{ Key = "CreatorMode"; View = "CreatorModeView"; Generated = $true },
    @{ Key = "NetworkBooster"; View = "NetworkBoosterView"; Generated = $true },
    @{ Key = "DnsLatencyTools"; View = "DnsLatencyToolsView"; Generated = $true },
    @{ Key = "PrivacyCenter"; View = "PrivacyCenterView"; Generated = $true },
    @{ Key = "SecurityHealth"; View = "SecurityHealthView"; Generated = $true },
    @{ Key = "AppsManager"; View = "AppsManagerView"; Generated = $true },
    @{ Key = "TweaksCenter"; View = "TweaksCenterView"; Generated = $true },
    @{ Key = "WindowsFeatures"; View = "WindowsFeaturesView"; Generated = $true },
    @{ Key = "UpdateControl"; View = "UpdateControlView"; Generated = $true },
    @{ Key = "RepairTools"; View = "RepairToolsView"; Generated = $true },
    @{ Key = "DriverUpdateCenter"; View = "DriverUpdateCenterView"; Generated = $true },
    @{ Key = "AppUninstaller"; View = "AppUninstallerView"; Generated = $true },
    @{ Key = "RestoreBackup"; View = "RestoreBackupView"; Generated = $true },
    @{ Key = "Settings"; View = "SettingsView"; Generated = $false },
    @{ Key = "About"; View = "AboutView"; Generated = $false }
)

$forbiddenTemplateTokens = @(
    "<views:PlacementPageChrome",
    "Placement Notes",
    "Feature Placement",
    "Run About App",
    "Preview About App",
    "Apply reviewed selection",
    "Result & History",
    "Restore & Safety",
    "Beta until installed",
    "beta build must pass",
    "Accent:"
)

foreach ($page in $corePages) {
    $relativePath = "wpf\Views\$($page.View).xaml"
    $path = Join-Path $RepoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing core page XAML: $relativePath")
        continue
    }

    $text = Get-Content -LiteralPath $path -Raw
    foreach ($token in $forbiddenTemplateTokens) {
        if ($text.IndexOf($token, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $failures.Add("$relativePath contains forbidden template/stale text: $token")
        }
    }

    if ($text -match "__[A-Z0-9_]+__") {
        $failures.Add("$relativePath still contains generator placeholder text.")
    }
    if ($text -notmatch [regex]::Escape("CORE_UI:$($page.Key)")) {
        $failures.Add("$relativePath missing CORE_UI marker for $($page.Key).")
    }
    if ($text -notmatch [regex]::Escape("CoreFeaturePage_$($page.Key)")) {
        $failures.Add("$relativePath missing AutomationId marker for $($page.Key).")
    }
    if ($page.Generated -and $text -notmatch '<views:PlacementActionPageBase') {
        $failures.Add("$relativePath must use PlacementActionPageBase for wired backend buttons.")
    }
    if ($page.Generated) {
        foreach ($required in @("Guided Workflow", "Feature Modules", "Safety and Restore", "Technical Details", "Page Recommendations")) {
            if ($text -notmatch [regex]::Escape($required)) {
                $failures.Add("$relativePath missing page-specific body marker: $required")
            }
        }
    }
}

$mainWindowPath = Join-Path $RepoRoot "wpf\MainWindow.xaml.cs"
if (-not (Test-Path -LiteralPath $mainWindowPath)) {
    $failures.Add("Missing MainWindow route source.")
} else {
    $mainWindow = Get-Content -LiteralPath $mainWindowPath -Raw
    foreach ($page in $corePages | Where-Object { $_.Key -notin @("Dashboard") }) {
        $routeToken = "_navigationService.Register(`"$($page.Key)`", () => new $($page.View)())"
        if ($mainWindow -notmatch [regex]::Escape($routeToken)) {
            $failures.Add("MainWindow missing dedicated route: $routeToken")
        }
    }
    if ($mainWindow -notmatch "_navigationService\.RegisterIfMissing\(key") {
        $failures.Add("RegisterLegacyRoute must use RegisterIfMissing so legacy pages cannot overwrite dedicated core routes.")
    }
}

$settings = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Views\SettingsView.xaml") -Raw
if ($settings -notmatch "CyberComboBoxStyle") {
    $failures.Add("Settings ComboBox controls must use dark CyberComboBoxStyle.")
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: no template UI regression verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: no template UI regression verification" -ForegroundColor Green

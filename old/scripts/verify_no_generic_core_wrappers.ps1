param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

$coreViews = @(
    "DashboardView.xaml",
    "PerformanceBoostView.xaml",
    "StartupManagerView.xaml",
    "BackgroundAppsView.xaml",
    "CleanupView.xaml",
    "StorageView.xaml",
    "OneClickBoostView.xaml",
    "AutoGamingModeView.xaml",
    "AIPerformanceAdvisorView.xaml",
    "GpuCenterView.xaml",
    "GamingBoosterView.xaml",
    "StreamingCenterView.xaml",
    "CreatorModeView.xaml",
    "NetworkBoosterView.xaml",
    "DnsLatencyToolsView.xaml",
    "PrivacyCenterView.xaml",
    "SecurityHealthView.xaml",
    "AppsManagerView.xaml",
    "TweaksCenterView.xaml",
    "WindowsFeaturesView.xaml",
    "UpdateControlView.xaml",
    "RepairToolsView.xaml",
    "DriverUpdateCenterView.xaml",
    "AppUninstallerView.xaml",
    "RestoreBackupView.xaml",
    "SettingsView.xaml",
    "AboutView.xaml"
)

foreach ($view in $coreViews) {
    $path = Join-Path $RepoRoot "wpf\Views\$view"
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing core view: $view")
        continue
    }

    $raw = Get-Content -LiteralPath $path -Raw
    if ($raw -match '<views:CyberPageChrome') {
        $failures.Add("Core view still uses generic CyberPageChrome: $view")
    }
    if ($raw -match '<views:PlacementPageChrome') {
        $failures.Add("Core view still uses generic PlacementPageChrome: $view")
    }
    if ($raw -notmatch 'CORE_UI:') {
        $failures.Add("Core view missing CORE_UI marker: $view")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: generic core wrapper verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: generic core wrapper verification" -ForegroundColor Green

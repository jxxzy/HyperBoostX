param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

$coreViews = @(
    "OneClickBoostView.xaml",
    "AIPerformanceAdvisorView.xaml",
    "AutoGamingModeView.xaml",
    "StartupManagerView.xaml",
    "ProcessAnalyzerView.xaml",
    "CleanupView.xaml",
    "GpuCenterView.xaml",
    "NetworkToolsView.xaml",
    "RestoreBackupView.xaml",
    "FeatureAuditView.xaml",
    "LegacyFeatureView.xaml"
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
    if ($raw -notmatch '<views:PlacementPageChrome' -and $view -ne "SettingsView.xaml") {
        $failures.Add("Core view does not use placement shell: $view")
    }
}

$allViewFiles = Get-ChildItem -LiteralPath (Join-Path $RepoRoot "wpf\Views") -Filter "*View.xaml" |
    Where-Object { $_.Name -ne "CyberPageChrome.xaml" }
foreach ($file in $allViewFiles) {
    $raw = Get-Content -LiteralPath $file.FullName -Raw
    if ($raw -match '<views:CyberPageChrome') {
        $failures.Add("View still uses generic CyberPageChrome: $($file.Name)")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: generic core wrapper verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: generic core wrapper verification" -ForegroundColor Green

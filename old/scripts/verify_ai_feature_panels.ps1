param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

$pageViews = @(
    "PerformanceBoostView","StartupManagerView","BackgroundAppsView","CleanupView","StorageView",
    "OneClickBoostView","AutoGamingModeView","AIPerformanceAdvisorView","GpuCenterView",
    "HardwareVendorCenterView","GamingBoosterView","CreatorModeView","NetworkBoosterView",
    "DnsLatencyToolsView","PrivacyCenterView","SecurityHealthView","AppsManagerView",
    "TweaksCenterView","WindowsFeaturesView","UpdateControlView","RepairToolsView",
    "DriverUpdateCenterView","AppUninstallerView","RestoreBackupView"
)

foreach ($view in $pageViews) {
    $path = Join-Path $RepoRoot "wpf\Views\$view.xaml"
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing core AI panel page: $view")
        continue
    }
    $xaml = Get-Content -LiteralPath $path -Raw
    foreach ($token in @("AI Insight", "10-Model Audit", "AI Risk Reviewed", "No Auto Apply", "AI Action Boundary", "Preview and approval required")) {
        if ($xaml -notmatch [regex]::Escape($token)) {
            $failures.Add("$view missing AI panel token: $token")
        }
    }
}

$dashboard = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Views\DashboardView.xaml") -Raw
if ($dashboard -notmatch [regex]::Escape("AI Recommendation")) {
    $failures.Add("Dashboard missing AI Recommendation panel.")
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: AI feature panel verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: AI feature panel verification" -ForegroundColor Green

param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]
$views = @(
    "PerformanceBoostView","StartupManagerView","BackgroundAppsView","CleanupView","StorageView",
    "OneClickBoostView","AutoGamingModeView","AIPerformanceAdvisorView","GpuCenterView",
    "HardwareVendorCenterView","GamingBoosterView","CreatorModeView","NetworkBoosterView",
    "DnsLatencyToolsView","PrivacyCenterView","SecurityHealthView","AppsManagerView",
    "TweaksCenterView","WindowsFeaturesView","UpdateControlView","RepairToolsView",
    "DriverUpdateCenterView","AppUninstallerView","RestoreBackupView"
)

foreach ($view in $views) {
    $path = Join-Path $RepoRoot "wpf\Views\$view.xaml"
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing dense page: $view")
        continue
    }
    $xaml = Get-Content -LiteralPath $path -Raw
    foreach ($token in @("Operational Snapshot", "Workspace Map", "Runbook", "Audit Console", "Safety Boundary", "Usage Notes", "AI Insight", "LegacyTools", "PlacementSections")) {
        if ($xaml -notmatch [regex]::Escape($token)) {
            $failures.Add("$view missing dense content token: $token")
        }
    }
    if (([regex]::Matches($xaml, '<Border\b')).Count -lt 18) {
        $failures.Add("$view does not have enough dense row/panel structure.")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: dense page content verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: dense page content verification" -ForegroundColor Green

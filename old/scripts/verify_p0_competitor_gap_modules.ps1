param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]
$source = @(
    "wpf\ViewModels\PlacementPageViewModel.cs",
    "wpf\ViewModels\LegacyFeatureCatalog.cs",
    "wpf\Data\ui_action_map_v2_10.json",
    "app\services\product_features.py",
    "app\api\product_v14.py",
    "app\api\real_features_v210.py",
    "docs\COMPETITOR_GAP_MODULES_v2.10.0.md"
) | ForEach-Object {
    $path = Join-Path $RepoRoot $_
    if (Test-Path -LiteralPath $path) { Get-Content -LiteralPath $path -Raw } else { "" }
}
$text = $source -join "`n"

$modules = [ordered]@{
    "Safe Tweak Library" = @("Safe Tweaks", "/api/system-config/tweaks")
    "Game Boost Engine" = @("Gaming Booster", "/api/boost/plan")
    "Smart Memory Guard" = @("RAM pressure", "ram_pressure")
    "Performance Profile Center" = @("power_profiles", "Performance Profile Center")
    "Game Detection Engine" = @("Game detection", "/api/games/running")
    "Per-Game Profile Advanced" = @("Game Profiles", "/api/games/profile")
    "Driver Health Center" = @("Driver & Update Center", "/api/drivers")
    "GPU Vendor Optimizer" = @("GPU Center", "GpuVendor")
    "Boot Optimizer" = @("Startup Manager", "/api/startup")
    "App Load Optimizer" = @("Apps Manager", "/api/apps")
    "Stutter Analyzer" = @("stutter", "PerformanceAdvisorService")
    "Network Latency Profile Center" = @("DNS & Latency Tools", "/api/network")
    "Before-After Benefit Report" = @("before/after", "ReportService")
}

foreach ($module in $modules.Keys) {
    $tokens = $modules[$module]
    $found = $false
    foreach ($token in $tokens) {
        if ($text -match [regex]::Escape($token)) {
            $found = $true
            break
        }
    }
    if (-not $found) {
        $failures.Add("P0 competitor-gap module lacks implementation/evidence token: $module")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: P0 competitor-gap module verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: P0 competitor-gap module verification" -ForegroundColor Green

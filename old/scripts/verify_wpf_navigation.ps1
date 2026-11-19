param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"

$mainVmPath = Join-Path $RepoRoot "wpf\ViewModels\MainWindowViewModel.cs"
$mainWindowPath = Join-Path $RepoRoot "wpf\MainWindow.xaml.cs"
$dashboardPath = Join-Path $RepoRoot "wpf\Views\DashboardView.xaml"

$mainVm = Get-Content -LiteralPath $mainVmPath -Raw
$mainWindow = Get-Content -LiteralPath $mainWindowPath -Raw
$dashboard = Get-Content -LiteralPath $dashboardPath -Raw

$navMatches = [regex]::Matches($mainVm, 'Key\s*=\s*"([^"]+)"\s*,\s*Label\s*=\s*"([^"]+)"')
$routeMatches = [regex]::Matches($mainWindow, '_navigationService\.Register\("([^"]+)",\s*\(\)\s*=>\s*new\s+([A-Za-z0-9_]+)\(')
$legacyRouteMatches = [regex]::Matches($mainWindow, 'RegisterLegacyRoute\("([^"]+)"')

$routes = @{}
foreach ($match in $routeMatches) { $routes[$match.Groups[1].Value] = $match.Groups[2].Value }
foreach ($match in $legacyRouteMatches) { $routes[$match.Groups[1].Value] = "LegacyFeatureView" }

$checks = New-Object System.Collections.Generic.List[object]

foreach ($match in $navMatches) {
    $key = $match.Groups[1].Value
    $label = $match.Groups[2].Value
    $routeExists = $routes.ContainsKey($key)
    $viewPath = if ($routeExists) { Join-Path $RepoRoot ("wpf\Views\{0}.xaml" -f $routes[$key]) } else { "" }

    $checks.Add([pscustomobject]@{ name = "sidebar label: $label"; ok = $true; evidence = $mainVmPath })
    $checks.Add([pscustomobject]@{ name = "route registered: $key"; ok = $routeExists; evidence = $mainWindowPath })
    $checks.Add([pscustomobject]@{ name = "view exists: $key"; ok = ($routeExists -and (Test-Path -LiteralPath $viewPath)); evidence = $viewPath })
}

foreach ($button in @("Start Smart Scan", "One Click Safe Boost", "Restore / Undo", "Gaming Mode", "View Last Report", "Refresh Snapshot", "Open Boost Plan", "Export Report", "Settings", "History")) {
    $checks.Add([pscustomobject]@{ name = "dashboard button: $button"; ok = $dashboard.Contains($button); evidence = $dashboardPath })
}

$blankViews = @()
foreach ($view in Get-ChildItem -LiteralPath (Join-Path $RepoRoot "wpf\Views") -Filter "*View.xaml") {
    $text = Get-Content -LiteralPath $view.FullName -Raw
    if ($text.Length -lt 120 -or ($text -match "Coming soon|TODO")) {
        $blankViews += $view.FullName
    }
}
$checks.Add([pscustomobject]@{ name = "no blank or placeholder-only views"; ok = ($blankViews.Count -eq 0); evidence = ($blankViews -join "; ") })
$checks.Add([pscustomobject]@{ name = "source sidebar inventory count >= 50"; ok = ($navMatches.Count -ge 50); evidence = "found $($navMatches.Count)" })

$report = [pscustomobject]@{
    generated_at = (Get-Date).ToUniversalTime().ToString("o")
    repo_root = $RepoRoot
    expected_sidebar_count = $navMatches.Count
    checks = $checks
    ok = -not ($checks | Where-Object { -not $_.ok })
}

$outDir = Join-Path $RepoRoot "docs\runtime-audit"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$jsonPath = Join-Path $outDir "wpf_navigation_report.json"
$mdPath = Join-Path $outDir "wpf_navigation_report.md"
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

$md = @("# HyperBoostX WPF Navigation Report", "", "Sidebar count: $($navMatches.Count)", "")
foreach ($check in $checks) {
    $status = if ($check.ok) { "PASS" } else { "FAIL" }
    $md += "- $status - $($check.name)"
}
$md | Set-Content -LiteralPath $mdPath -Encoding UTF8

Write-Host "WPF navigation report: $jsonPath"
if (-not $report.ok) { exit 1 }

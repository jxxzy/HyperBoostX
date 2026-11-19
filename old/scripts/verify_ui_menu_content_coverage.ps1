param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

function Read-RepoFile([string]$RelativePath) {
    Get-Content -LiteralPath (Join-Path $RepoRoot $RelativePath) -Raw
}

$mainVm = Read-RepoFile "wpf\ViewModels\MainWindowViewModel.cs"
$mainWindow = Read-RepoFile "wpf\MainWindow.xaml.cs"
$legacyCatalog = Read-RepoFile "wpf\ViewModels\LegacyFeatureCatalog.cs"
$placementVm = Read-RepoFile "wpf\ViewModels\PlacementPageViewModel.cs"
$placementXaml = Read-RepoFile "wpf\Views\PlacementPageChrome.xaml"

$navItems = [regex]::Matches($mainVm, 'Key\s*=\s*"([^"]+)",\s*Label\s*=\s*"([^"]+)",\s*Glyph\s*=\s*"([^"]+)",\s*Group\s*=\s*"([^"]+)"') |
    ForEach-Object {
        [pscustomobject]@{
            Key = $_.Groups[1].Value
            Label = $_.Groups[2].Value
            Group = $_.Groups[4].Value
        }
    }

$dedicatedRoutes = @{}
[regex]::Matches($mainWindow, '_navigationService\.Register\("([^"]+)",\s*\(\)\s*=>\s*new\s+([A-Za-z0-9_]+)\(') |
    ForEach-Object { $dedicatedRoutes[$_.Groups[1].Value] = $_.Groups[2].Value }

$legacyRoutes = @{}
[regex]::Matches($mainWindow, 'RegisterLegacyRoute\("([^"]+)"') |
    ForEach-Object { $legacyRoutes[$_.Groups[1].Value] = $true }

$legacyToolKeys = [regex]::Matches($legacyCatalog, '\["([^"]+)"\]\s*=\s*new\[\]') |
    ForEach-Object { $_.Groups[1].Value } |
    Sort-Object -Unique

$placementSeedKeys = [regex]::Matches($placementVm, '\["([^"]+)"\]\s*=\s*Page\(') |
    ForEach-Object { $_.Groups[1].Value } |
    Sort-Object -Unique

if ($navItems.Count -lt 70) {
    $failures.Add("Expected at least 70 sidebar menu entries; found $($navItems.Count).")
}

$rows = foreach ($item in $navItems) {
    $route = "Missing"
    $viewKind = "Missing"
    $viewName = ""

    if ($dedicatedRoutes.ContainsKey($item.Key)) {
        $route = "Dedicated"
        $viewName = $dedicatedRoutes[$item.Key]
        $viewPath = Join-Path $RepoRoot "wpf\Views\$viewName.xaml"
        if (Test-Path -LiteralPath $viewPath) {
            $raw = Get-Content -LiteralPath $viewPath -Raw
            if ($raw -match '<views:PlacementPageChrome') {
                $viewKind = "PlacementChrome"
            } elseif ($raw -match '<views:CyberPageChrome') {
                $viewKind = "CyberChrome"
            } else {
                $viewKind = "CustomXaml"
            }
        } else {
            $viewKind = "MissingViewFile"
        }
    } elseif ($legacyRoutes.ContainsKey($item.Key)) {
        $route = "LegacyOnly"
        $viewName = "LegacyFeatureView"
        $viewKind = "PlacementChrome"
    }

    [pscustomobject]@{
        Key = $item.Key
        Label = $item.Label
        Group = $item.Group
        Route = $route
        View = $viewName
        ViewKind = $viewKind
        HasLegacyToolkit = $legacyToolKeys -contains $item.Key
        HasPlacementSeed = $placementSeedKeys -contains $item.Key
    }
}

foreach ($row in $rows) {
    if ($row.Route -eq "Missing") {
        $failures.Add("Sidebar menu has no route: $($row.Key)")
    }
    if ($row.ViewKind -eq "CyberChrome") {
        $failures.Add("Sidebar menu still uses old CyberPageChrome wrapper: $($row.Key)")
    }
    if (-not $row.HasLegacyToolkit -and -not $row.HasPlacementSeed) {
        $failures.Add("Sidebar menu lacks domain content seed/toolkit: $($row.Key)")
    }
}

foreach ($token in @(
    "HeroPrimaryActionItems",
    "Feature Toolkit",
    "LegacyTools",
    "WorkflowSteps",
    "Evidence Actions",
    "PlacementSections",
    "Advanced Details"
)) {
    if ($placementXaml -notmatch [regex]::Escape($token)) {
        $failures.Add("PlacementPageChrome missing mature menu body token: $token")
    }
}

$docPath = Join-Path $RepoRoot "docs\UI_MENU_CONTENT_AUDIT_v2.10.0.md"
$routeSummary = $rows | Group-Object Route, ViewKind | Sort-Object Name
$lines = @(
    "# HyperBoostX v2.10.0 UI Menu Content Audit",
    "",
    "Generated: $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'))",
    "",
    "Overall: $(if ($failures.Count -eq 0) { 'PASS' } else { 'FAIL' })",
    "",
    "## Summary",
    "",
    "| Metric | Count |",
    "| --- | ---: |",
    "| Sidebar menus | $($rows.Count) |",
    "| Dedicated custom XAML menus | $(($rows | Where-Object ViewKind -eq 'CustomXaml').Count) |",
    "| Dedicated PlacementChrome menus | $(($rows | Where-Object { $_.Route -eq 'Dedicated' -and $_.ViewKind -eq 'PlacementChrome' }).Count) |",
    "| Legacy PlacementChrome menus | $(($rows | Where-Object Route -eq 'LegacyOnly').Count) |",
    "| Menus with legacy toolkit content | $(($rows | Where-Object HasLegacyToolkit).Count) |",
    "| Menus with placement seed | $(($rows | Where-Object HasPlacementSeed).Count) |",
    "| Menus without route | $(($rows | Where-Object Route -eq 'Missing').Count) |",
    "| Menus without domain content | $(($rows | Where-Object { -not $_.HasLegacyToolkit -and -not $_.HasPlacementSeed }).Count) |",
    "",
    "## Route Shape",
    "",
    "| Route / View Kind | Count |",
    "| --- | ---: |"
)

foreach ($group in $routeSummary) {
    $lines += "| $($group.Name) | $($group.Count) |"
}

$lines += @(
    "",
    "## Menu Matrix",
    "",
    "| Group | Key | Label | Route | View Kind | Legacy Toolkit | Placement Seed |",
    "| --- | --- | --- | --- | --- | --- | --- |"
)

foreach ($row in $rows | Sort-Object Group, Key) {
    $lines += "| $($row.Group) | $($row.Key) | $($row.Label) | $($row.Route) | $($row.ViewKind) | $($row.HasLegacyToolkit) | $($row.HasPlacementSeed) |"
}

if ($failures.Count -gt 0) {
    $lines += @("", "## Failures", "")
    foreach ($failure in $failures) {
        $lines += "- $failure"
    }
}

$lines | Set-Content -LiteralPath $docPath -Encoding UTF8

if ($failures.Count -gt 0) {
    Write-Host "FAIL: UI menu content coverage verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    Write-Host "Audit doc: $docPath"
    exit 1
}

Write-Host "PASS: UI menu content coverage verification" -ForegroundColor Green
Write-Host "Audit doc: $docPath"

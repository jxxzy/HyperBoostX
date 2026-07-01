param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

$scanFiles = @(
    "wpf\Views\DashboardView.xaml",
    "wpf\Views\PlacementPageChrome.xaml",
    "wpf\Views\AboutView.xaml",
    "wpf\Views\SettingsView.xaml",
    "wpf\ViewModels\DashboardViewModel.cs",
    "wpf\ViewModels\PlacementPageViewModel.cs",
    "docs\UI_ACTION_MAP_v2.10.0.md",
    "docs\UI_V13_TO_V210_PLACEMENT_MATRIX.md"
)

$forbidden = @(
    "Placement Notes",
    "Feature Placement",
    "Run About App",
    "Preview About App",
    "Apply reviewed selection",
    "No live measurement yet",
    "Beta until installed",
    "beta build must pass",
    "Accent:"
)

foreach ($relativePath in $scanFiles) {
    $path = Join-Path $RepoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing final UI artifact: $relativePath")
        continue
    }

    $text = Get-Content -LiteralPath $path -Raw
    foreach ($token in $forbidden) {
        if ($text.IndexOf($token, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $failures.Add("$relativePath contains template/stale UI text: $token")
        }
    }
}

$about = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Views\AboutView.xaml") -Raw
if ($about -match '<views:PlacementPageChrome') {
    $failures.Add("About page must stay purpose-built and must not use PlacementPageChrome.")
}

$settings = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Views\SettingsView.xaml") -Raw
if ($settings -notmatch 'CyberComboBoxStyle') {
    $failures.Add("Settings ComboBox controls must use dark CyberComboBoxStyle.")
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: no template UI regression verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: no template UI regression verification" -ForegroundColor Green


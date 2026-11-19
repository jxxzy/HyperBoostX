param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

$theme = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Themes\CyberTheme.xaml") -Raw
$cards = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Styles\Cards.xaml") -Raw
$sidebar = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Styles\Sidebar.xaml") -Raw
$mainWindow = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\MainWindow.xaml") -Raw

foreach ($forbidden in @("#C026D3", "#7C3AED", "#F472B6", "#201139", "#261946")) {
    if ($theme -match [regex]::Escape($forbidden)) {
        $failures.Add("Theme still contains old neon-heavy color: $forbidden")
    }
}

if ($theme -notmatch '<CornerRadius x:Key="CornerRadius.Button">8</CornerRadius>') {
    $failures.Add("Button corner radius should be compact for clean premium UI.")
}

if ($cards -match 'TranslateTransform Y' -or $cards -match 'To="-4"') {
    $failures.Add("Card hover should not move layout vertically.")
}

if ($sidebar -match 'Brush\.Gradient\.Cyber' -or $sidebar -match 'Shadow\.Glow') {
    $failures.Add("Sidebar active state should be calm, not gradient/glow heavy.")
}

if ($mainWindow -match 'CyberPulseStoryboard') {
    $failures.Add("MainWindow still starts pulse animation unconditionally.")
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: UI theme resource verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: UI theme resource verification" -ForegroundColor Green

param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]
$views = Get-ChildItem -LiteralPath (Join-Path $RepoRoot "wpf\Views") -Filter "*View.xaml" |
    Where-Object { (Get-Content -LiteralPath $_.FullName -Raw) -match "PlacementActionPageBase" }

foreach ($view in $views) {
    $xaml = Get-Content -LiteralPath $view.FullName -Raw
    foreach ($token in @(
        'ItemsSource="{Binding PrimaryPlacementActions}"',
        'ItemsSource="{Binding SecondaryPlacementActions}"',
        'ItemsSource="{Binding RestorePlacementActions}"',
        'Tag="{Binding}"',
        'Click="RunPlacementAction_Click"',
        'ToolTip="{Binding Tooltip}"',
        'ItemsSource="{Binding LegacyTools}"'
    )) {
        if ($xaml -notmatch [regex]::Escape($token)) {
            $failures.Add("$($view.Name) missing action row token: $token")
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: page action row verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: page action row verification" -ForegroundColor Green

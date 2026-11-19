param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]
$views = Get-ChildItem -LiteralPath (Join-Path $RepoRoot "wpf\Views") -Filter "*View.xaml" |
    Where-Object { (Get-Content -LiteralPath $_.FullName -Raw) -match "PlacementActionPageBase" }

foreach ($view in $views) {
    $xaml = Get-Content -LiteralPath $view.FullName -Raw
    foreach ($token in @("Audit Console", "Advanced Details", "LiveResult", "ResultSummary", "AdvancedRouteLines", "TextBox", "Grid.Column=`"1`"")) {
        if ($xaml -notmatch [regex]::Escape($token)) {
            $failures.Add("$($view.Name) missing log/evidence panel token: $token")
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: right log/evidence panel verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: right log/evidence panel verification" -ForegroundColor Green

param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

$paths = @(
    "wpf\Views\DashboardView.xaml",
    "wpf\Views\PlacementPageChrome.xaml",
    "wpf\Views\SettingsView.xaml",
    "wpf\Views\AboutView.xaml",
    "docs\UI_V13_TO_V210_PLACEMENT_MATRIX.md",
    "docs\UI_CONTENT_PLACEMENT_AUDIT_v2.10.0.md"
)

foreach ($relativePath in $paths) {
    $path = Join-Path $RepoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing file: $relativePath")
        continue
    }

    $text = Get-Content -LiteralPath $path -Raw
    foreach ($token in @("Placement Notes", "Feature Placement", "Result & History", "Restore & Safety")) {
        if ($text -match [regex]::Escape($token)) {
            $failures.Add("$relativePath still contains template placement text: $token")
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: no placement notes verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: no placement notes verification" -ForegroundColor Green


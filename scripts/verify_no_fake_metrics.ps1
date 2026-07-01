param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

$dashboardXamlPath = Join-Path $RepoRoot "wpf\Views\DashboardView.xaml"
$dashboardVmPath = Join-Path $RepoRoot "wpf\ViewModels\DashboardViewModel.cs"
$dashboardXaml = Get-Content -LiteralPath $dashboardXamlPath -Raw
$dashboardVm = Get-Content -LiteralPath $dashboardVmPath -Raw

$forbiddenDashboardPatterns = @(
    '<Canvas\b',
    '<Polyline\b',
    'Live-style',
    'Points="0,110',
    'fake live charts',
    'CyberRingContainerStyle',
    'No live measurement',
    'Feature Audit'
)

foreach ($pattern in $forbiddenDashboardPatterns) {
    if ($dashboardXaml -match $pattern) {
        $failures.Add("Dashboard contains fake/hardcoded metric pattern: $pattern")
    }
}

if ($dashboardVm -match 'Value\s*=\s*"Beta"' -or $dashboardVm -match 'Glyph\s*=\s*"BETA"') {
    $failures.Add("Dashboard release metric still reports Beta.")
}

foreach ($fakeValue in @('Value\s*=\s*"Scan"', 'Value\s*=\s*"Guard"')) {
    if ($dashboardVm -match $fakeValue) {
        $failures.Add("Dashboard score cards still use synthetic value pattern: $fakeValue")
    }
}

$heroBlock = [regex]::Match($dashboardXaml, '<Border Style="\{StaticResource CyberHeroCardStyle\}".*?</Border>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
if (-not $heroBlock.Success) {
    $failures.Add("Dashboard hero card block not found.")
}
else {
    $heroButtonCount = [regex]::Matches($heroBlock.Value, '<Button\b').Count
    if ($heroButtonCount -gt 3) {
        $failures.Add("Dashboard hero has $heroButtonCount CTA buttons; expected max 3.")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: fake metrics verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: fake metrics verification" -ForegroundColor Green

param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

$auditPath = Join-Path $RepoRoot "docs\UI_CONTENT_PLACEMENT_AUDIT_v2.10.0.md"
$matrixPath = Join-Path $RepoRoot "docs\UI_V13_TO_V210_PLACEMENT_MATRIX.md"
$placementVmPath = Join-Path $RepoRoot "wpf\ViewModels\PlacementPageViewModel.cs"
$placementXamlPath = Join-Path $RepoRoot "wpf\Views\PlacementPageChrome.xaml"

foreach ($path in @($auditPath, $matrixPath, $placementVmPath, $placementXamlPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing required placement artifact: $path")
    }
}

if (Test-Path -LiteralPath $auditPath) {
    $audit = Get-Content -LiteralPath $auditPath -Raw
    foreach ($token in @("Dashboard", "One Click Boost", "GPU Center", "Cleanup", "Network Booster", "Raw JSON", "Safety Guard")) {
        if ($audit -notmatch [regex]::Escape($token)) {
            $failures.Add("Placement audit missing token: $token")
        }
    }
}

if (Test-Path -LiteralPath $matrixPath) {
    $matrix = Get-Content -LiteralPath $matrixPath -Raw
    foreach ($token in @("Historical Panel", "Beginner Baseline", "Expert Boundary", "Plugin Marketplace", "RGB Software Detector", "Final Placement")) {
        if ($matrix -notmatch [regex]::Escape($token)) {
            $failures.Add("Placement matrix missing token: $token")
        }
    }
}

if (Test-Path -LiteralPath $placementXamlPath) {
    $placementXaml = Get-Content -LiteralPath $placementXamlPath -Raw
    foreach ($token in @("StateTitle", "WorkspaceTitle", "ActionTitle", "SecondaryActionTitle", "ResultTitle", "SafetyTitle", "RecommendationsTitle", "PlacementSections", "Technical Details")) {
        if ($placementXaml -notmatch [regex]::Escape($token)) {
            $failures.Add("Placement shell missing section: $token")
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: UI placement matrix verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: UI placement matrix verification" -ForegroundColor Green

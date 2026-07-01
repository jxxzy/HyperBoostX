param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

& (Join-Path $PSScriptRoot "verify_wpf_button_handlers.ps1") -RepoRoot $RepoRoot
if (-not $?) {
    $failures.Add("Base WPF button handler check failed.")
}

$placementXaml = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Views\PlacementPageChrome.xaml") -Raw
$placementCode = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Views\PlacementPageChrome.xaml.cs") -Raw
$actionCatalog = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\ViewModels\FeatureActionCatalog.cs") -Raw

foreach ($token in @("RunPlacementAction_Click", "PrimaryPlacementActions", "SecondaryPlacementActions", "RestorePlacementActions", "Advanced Details")) {
    if ($placementXaml -notmatch [regex]::Escape($token) -and $placementCode -notmatch [regex]::Escape($token)) {
        $failures.Add("Placement button/action surface missing token: $token")
    }
}

foreach ($token in @("ConfirmationRequired", "BuildFriendlyError", "PostJsonRouteAsync", "GetJsonAsync", "ResultSummary")) {
    if ($placementCode -notmatch [regex]::Escape($token)) {
        $failures.Add("Placement action handler missing behavior: $token")
    }
}

if ($actionCatalog -notmatch 'ui_action_map_v2_10\.json') {
    $failures.Add("FeatureActionCatalog is not using the v2.10 action map.")
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: UI button handler verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: UI button handler verification" -ForegroundColor Green

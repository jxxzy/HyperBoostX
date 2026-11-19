param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

$models = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\ViewModels\CyberModels.cs") -Raw
$mainVm = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\ViewModels\MainWindowViewModel.cs") -Raw
$mainXaml = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\MainWindow.xaml") -Raw
$visibility = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\ViewModels\FeatureVisibilityService.cs") -Raw
$actionMap = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Data\ui_action_map_v2_10.json") -Raw | ConvertFrom-Json

foreach ($token in @("public string Status", "Maturity: {0}", "FeatureVisibilityService.GetStatus", "StatusByFeature")) {
    if (($models + $mainVm + $mainXaml + $visibility) -notmatch [regex]::Escape($token)) {
        $failures.Add("Maturity label surface missing token: $token")
    }
}

$missingStatus = @($actionMap.menus | Where-Object { [string]::IsNullOrWhiteSpace($_.status) })
if ($missingStatus.Count -gt 0) {
    $failures.Add("Action map menus without status: $($missingStatus.Count)")
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: feature maturity label verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: feature maturity label verification" -ForegroundColor Green

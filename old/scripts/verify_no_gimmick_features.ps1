param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]
$actionMapText = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Data\ui_action_map_v2_10.json") -Raw
$actionMap = $actionMapText | ConvertFrom-Json
$mainVm = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\ViewModels\MainWindowViewModel.cs") -Raw
$publicLabels = @($actionMap.menus | ForEach-Object { $_.label }) + @($actionMap.menus.actions | ForEach-Object { $_.label }) + $mainVm

foreach ($forbidden in @(
    "NVIDIA Copilot",
    "Plugin Marketplace",
    "Apply All",
    "AI Optimize",
    "Disable Defender",
    "Disable Firewall",
    "Guaranteed FPS",
    "Guaranteed ping"
)) {
    if (($publicLabels -join "`n") -match [regex]::Escape($forbidden)) {
        $failures.Add("Forbidden gimmick/public claim found: $forbidden")
    }
}

foreach ($menu in $actionMap.menus) {
    $actions = @($menu.actions)
    if ($actions.Count -lt 6) {
        $failures.Add("$($menu.key) has too few actions to be a mature Expert surface: $($actions.Count)")
    }
    foreach ($action in $actions) {
        if ([string]::IsNullOrWhiteSpace($action.path)) {
            $failures.Add("$($action.id) has no backend path.")
        }
        if ([string]::IsNullOrWhiteSpace($action.loading_state) -or [string]::IsNullOrWhiteSpace($action.success_state) -or [string]::IsNullOrWhiteSpace($action.error_state)) {
            $failures.Add("$($action.id) missing button state text.")
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: no gimmick feature verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: no gimmick feature verification" -ForegroundColor Green

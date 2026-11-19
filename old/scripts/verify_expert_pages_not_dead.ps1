param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]
$actionMap = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Data\ui_action_map_v2_10.json") -Raw | ConvertFrom-Json
$mainWindow = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\MainWindow.xaml.cs") -Raw

foreach ($menu in $actionMap.menus | Where-Object { $_.key -ne "Default" }) {
    $actions = @($menu.actions)
    if ($actions.Count -eq 0) {
        $failures.Add("$($menu.key) has no actions.")
    }
    foreach ($action in $actions) {
        if ([string]::IsNullOrWhiteSpace($action.command) -or [string]::IsNullOrWhiteSpace($action.method) -or [string]::IsNullOrWhiteSpace($action.path)) {
            $failures.Add("$($action.id) missing command/method/path.")
        }
    }

    $routePattern = 'RegisterLegacyRoute\("' + [regex]::Escape($menu.key) + '"' 
    $directPattern = '_navigationService\.Register\("' + [regex]::Escape($menu.key) + '"'
    if ($mainWindow -notmatch $routePattern -and $mainWindow -notmatch $directPattern) {
        $failures.Add("$($menu.key) has action-map entries but no WPF route registration.")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: Expert pages not-dead verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: Expert pages not-dead verification" -ForegroundColor Green

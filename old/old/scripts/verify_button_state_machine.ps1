param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]
$actionMap = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Data\ui_action_map_v2_10.json") -Raw | ConvertFrom-Json
$placementBase = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Views\PlacementActionPageBase.cs") -Raw

foreach ($token in @("SetActionButtonsEnabled(false)", "SetActionButtonsEnabled(true)", "ConfirmMutatingAction", "PreviewRequired", "ConfirmationRequired", "SafetyGuard", "BuildStatusText", "BuildFriendlyError")) {
    if ($placementBase -notmatch [regex]::Escape($token)) {
        $failures.Add("Placement action button state machine missing token: $token")
    }
}

foreach ($action in $actionMap.menus.actions) {
    foreach ($stateField in @("loading_state", "success_state", "error_state")) {
        if ([string]::IsNullOrWhiteSpace($action.$stateField)) {
            $failures.Add("$($action.id) missing $stateField")
        }
    }
    if ($action.method -in @("POST", "PUT", "PATCH", "DELETE") -and $action.id -match '\.apply\.|\.restore\.') {
        if (-not $action.preview_required -and $action.id -match '\.apply\.') {
            $failures.Add("$($action.id) apply action must require preview.")
        }
        if (-not $action.confirmation_required) {
            $failures.Add("$($action.id) mutating action must require confirmation.")
        }
        if (-not $action.safety_guard) {
            $failures.Add("$($action.id) mutating action must keep Safety Guard enabled.")
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: button state machine verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: button state machine verification" -ForegroundColor Green

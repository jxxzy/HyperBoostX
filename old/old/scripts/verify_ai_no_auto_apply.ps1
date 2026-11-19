param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]
$service = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Services\NvidiaCopilotService.cs") -Raw
$productApi = Get-Content -LiteralPath (Join-Path $RepoRoot "app\api\product_v14.py") -Raw
$actionMap = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Data\ui_action_map_v2_10.json") -Raw | ConvertFrom-Json

foreach ($token in @("Never execute actions directly", "RequireActionApproval", "RequiresApproval", "RequireApproval = true")) {
    if ($service -notmatch [regex]::Escape($token)) {
        $failures.Add("AI no-auto-apply contract missing token: $token")
    }
}

if ($productApi -notmatch [regex]::Escape('"auto_apply_pending_action": False')) {
    $failures.Add("Backend AI status must explicitly keep auto_apply_pending_action false.")
}

$aiMenus = @($actionMap.menus | Where-Object { $_.key -in @("AIPerformanceAdvisor", "AICenter", "NvidiaCopilot") })
foreach ($menu in $aiMenus) {
    $applyActions = @($menu.actions | Where-Object { $_.id -match '\.apply\.' })
    foreach ($action in $applyActions) {
        if (-not $action.preview_required) {
            $failures.Add("$($action.id) must require preview.")
        }
        if (-not $action.confirmation_required) {
            $failures.Add("$($action.id) must require confirmation.")
        }
        if (-not $action.safety_guard) {
            $failures.Add("$($action.id) must keep Safety Guard enabled.")
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: AI no-auto-apply verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: AI no-auto-apply verification" -ForegroundColor Green

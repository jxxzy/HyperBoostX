param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]
$reportService = Get-Content -LiteralPath (Join-Path $RepoRoot "app\services\monitoring\report_service.py") -Raw
$productFeatures = Get-Content -LiteralPath (Join-Path $RepoRoot "app\services\product_features.py") -Raw
$realFeatures = Get-Content -LiteralPath (Join-Path $RepoRoot "app\api\real_features_v210.py") -Raw
$actionMap = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Data\ui_action_map_v2_10.json") -Raw | ConvertFrom-Json

foreach ($token in @("report_id", "build_report", "export_report", "before", "after")) {
    if ($reportService -notmatch [regex]::Escape($token)) {
        $failures.Add("Report service missing schema token: $token")
    }
}

foreach ($token in @("class RestoreService", "restore_sessions", "create_session", "preview", "apply", "verify", "export")) {
    if ($productFeatures -notmatch [regex]::Escape($token)) {
        $failures.Add("Restore service missing schema token: $token")
    }
}

foreach ($token in @("restore_metadata_id", "report_id", "blocked_reasons", "requires_admin", "safe_alternative")) {
    if ($realFeatures -notmatch [regex]::Escape($token)) {
        $failures.Add("Normalized backend response missing token: $token")
    }
}

$menusWithoutRestoreOrExport = @()
foreach ($menu in $actionMap.menus | Where-Object { $_.key -notin @("Default", "About") }) {
    $labels = @($menu.actions.label) -join " "
    if ($labels -notmatch "Restore" -and $labels -notmatch "Export") {
        $menusWithoutRestoreOrExport += $menu.key
    }
}
if ($menusWithoutRestoreOrExport.Count -gt 0) {
    $failures.Add("Menus without restore/export action evidence: $($menusWithoutRestoreOrExport -join ', ')")
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: report/restore schema verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: report/restore schema verification" -ForegroundColor Green

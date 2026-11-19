param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

$servicePath = Join-Path $RepoRoot "wpf\Services\NvidiaCopilotService.cs"
$actionMapPath = Join-Path $RepoRoot "wpf\Data\ui_action_map_v2_10.json"
$service = if (Test-Path -LiteralPath $servicePath) { Get-Content -LiteralPath $servicePath -Raw } else { "" }
$actionMap = if (Test-Path -LiteralPath $actionMapPath) { Get-Content -LiteralPath $actionMapPath -Raw | ConvertFrom-Json } else { $null }

foreach ($token in @(
    "safe_actions must only contain",
    "cleanup",
    "ram_optimize",
    "gaming_prep",
    "network_fix",
    "background_trim",
    "power_balanced",
    "scan_only",
    "BlockedTerms",
    "run_arbitrary_command",
    "powershell_freeform"
)) {
    if ($service -notmatch [regex]::Escape($token)) {
        $failures.Add("AI action mapper missing token: $token")
    }
}

if ($null -eq $actionMap) {
    $failures.Add("Missing action map JSON.")
} else {
    $paths = @($actionMap.menus.actions | ForEach-Object { $_.path }) | Sort-Object -Unique
    foreach ($path in @("/api/ai/approve", "/api/advisor/safe-actions", "/api/boost/apply", "/api/protection/evaluate-action")) {
        if ($paths -notcontains $path) {
            $failures.Add("Action map missing AI-safe route: $path")
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: AI action mapper verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: AI action mapper verification" -ForegroundColor Green

param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

$actionMap = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Data\ui_action_map_v2_10.json") -Raw | ConvertFrom-Json
$service = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Services\NvidiaCopilotService.cs") -Raw
$placementVm = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\ViewModels\PlacementPageViewModel.cs") -Raw

foreach ($menuKey in @("AIPerformanceAdvisor", "AICenter", "NvidiaCopilot")) {
    if (-not (@($actionMap.menus).key -contains $menuKey)) {
        $failures.Add("Universal AI layer missing action-map menu: $menuKey")
    }
}

foreach ($token in @("AiModelRegistry", "NvidiaAiProvider", "AiSafetyGuard", "AiActionApprovalService", "AI Performance Advisor", "NVIDIA API Provider", "AI output cannot execute arbitrary shell commands")) {
    if (($service + $placementVm) -notmatch [regex]::Escape($token)) {
        $failures.Add("Universal AI layer missing token: $token")
    }
}

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "verify_ai_10_model_registry.ps1") -RepoRoot $RepoRoot
if ($LASTEXITCODE -ne 0) { $failures.Add("10-model registry subgate failed.") }
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "verify_ai_no_auto_apply.ps1") -RepoRoot $RepoRoot
if ($LASTEXITCODE -ne 0) { $failures.Add("AI no-auto-apply subgate failed.") }

if ($failures.Count -gt 0) {
    Write-Host "FAIL: AI universal layer verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: AI universal layer verification" -ForegroundColor Green

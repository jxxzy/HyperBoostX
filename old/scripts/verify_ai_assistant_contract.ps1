param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]
$subgates = @(
    "verify_ai_10_model_registry.ps1",
    "verify_ai_model_router.ps1",
    "verify_ai_redaction.ps1",
    "verify_ai_action_mapper.ps1",
    "verify_ai_no_auto_apply.ps1",
    "verify_ai_feature_panels.ps1",
    "verify_ai_universal_layer.ps1"
)

foreach ($gate in $subgates) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot $gate) -RepoRoot $RepoRoot
    if ($LASTEXITCODE -ne 0) {
        $failures.Add("AI assistant subgate failed: $gate")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: AI Assistant contract verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: AI Assistant contract verification" -ForegroundColor Green

param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")),
    [string]$ExpectedVersion = ""
)

$ErrorActionPreference = "Continue"
if ([string]::IsNullOrWhiteSpace($ExpectedVersion)) {
    $ExpectedVersion = (Get-Content -LiteralPath (Join-Path $RepoRoot "VERSION") -Raw).Trim()
}

$checks = @(
    @{ name = "version consistency"; kind = "script"; path = "verify_version_consistency.ps1" },
    @{ name = "UI release gate"; kind = "script"; path = "verify_ui_release_gate.ps1" },
    @{ name = "no template UI regression"; kind = "script"; path = "verify_no_template_ui_regression.ps1" },
    @{ name = "UI page body markers"; kind = "script"; path = "verify_ui_page_body_markers.ps1" },
    @{ name = "UI button labels"; kind = "script"; path = "verify_ui_button_labels.ps1" },
    @{ name = "no placement notes"; kind = "script"; path = "verify_ui_no_placement_notes.ps1" },
    @{ name = "dense page content"; kind = "script"; path = "verify_dense_page_content.ps1" },
    @{ name = "page action rows"; kind = "script"; path = "verify_page_action_rows.ps1" },
    @{ name = "right log/evidence panel"; kind = "script"; path = "verify_right_log_panel.ps1" },
    @{ name = "button state machine"; kind = "script"; path = "verify_button_state_machine.ps1" },
    @{ name = "report/restore schema"; kind = "script"; path = "verify_report_restore_schema.ps1" },
    @{ name = "AI Assistant contract"; kind = "script"; path = "verify_ai_assistant_contract.ps1" },
    @{ name = "AI universal layer"; kind = "script"; path = "verify_ai_universal_layer.ps1" },
    @{ name = "AI 10-model registry"; kind = "script"; path = "verify_ai_10_model_registry.ps1" },
    @{ name = "AI model router"; kind = "script"; path = "verify_ai_model_router.ps1" },
    @{ name = "AI redaction"; kind = "script"; path = "verify_ai_redaction.ps1" },
    @{ name = "AI action mapper"; kind = "script"; path = "verify_ai_action_mapper.ps1" },
    @{ name = "AI no auto apply"; kind = "script"; path = "verify_ai_no_auto_apply.ps1" },
    @{ name = "AI feature panels"; kind = "script"; path = "verify_ai_feature_panels.ps1" },
    @{ name = "Expert mode surface"; kind = "script"; path = "verify_expert_mode_surface.ps1" },
    @{ name = "feature maturity labels"; kind = "script"; path = "verify_feature_maturity_labels.ps1" },
    @{ name = "no gimmick features"; kind = "script"; path = "verify_no_gimmick_features.ps1" },
    @{ name = "Expert pages not dead"; kind = "script"; path = "verify_expert_pages_not_dead.ps1" },
    @{ name = "P0 competitor-gap modules"; kind = "script"; path = "verify_p0_competitor_gap_modules.ps1" },
    @{ name = "release docs consistency"; kind = "script"; path = "verify_release_docs_consistency.ps1" },
    @{ name = "installer runtime evidence"; kind = "script"; path = "verify_installer_runtime_gate.ps1" }
)

$results = New-Object System.Collections.Generic.List[object]

foreach ($check in $checks) {
    $started = Get-Date
    $scriptPath = Join-Path $PSScriptRoot $check.path
    if ($check.path -eq "verify_installer_runtime_gate.ps1") {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $scriptPath -RepoRoot $RepoRoot -ExpectedVersion $ExpectedVersion
    } else {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $scriptPath -RepoRoot $RepoRoot
    }
    $exitCode = $LASTEXITCODE
    if ($null -eq $exitCode) { $exitCode = 0 }
    $results.Add([pscustomobject]@{
        name = $check.name
        status = if ($exitCode -eq 0) { "PASS" } elseif ($exitCode -eq 2) { "PARTIAL" } else { "FAIL" }
        exit_code = $exitCode
        duration_ms = [int]((Get-Date) - $started).TotalMilliseconds
    })
}

$hasFail = @($results | Where-Object { $_.status -eq "FAIL" }).Count -gt 0
$hasPartial = @($results | Where-Object { $_.status -eq "PARTIAL" }).Count -gt 0
$finalStatus = if ($hasFail) { "OWNER_LAB_BLOCKED" } elseif ($hasPartial) { "OWNER_LAB_PARTIAL" } else { "OWNER_LAB_PASS" }

$docsDir = Join-Path $RepoRoot "docs"
$runtimeAuditDir = Join-Path $RepoRoot "docs\runtime-audit"
New-Item -ItemType Directory -Force -Path $docsDir | Out-Null
New-Item -ItemType Directory -Force -Path $runtimeAuditDir | Out-Null

$jsonPath = Join-Path $runtimeAuditDir "final_stable_release_gate_report.json"
$mdPath = Join-Path $docsDir "STABLE_RELEASE_AUDIT_v2.10.0.md"
$payload = [pscustomobject]@{
    generated_at = (Get-Date).ToUniversalTime().ToString("o")
    expected_version = $ExpectedVersion
    final_status = $finalStatus
    checks = $results
}
$payload | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

$lines = @(
    "# Owner Lab Release Gate Audit v2.10.0",
    "",
    "Generated: $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'))",
    "Expected version: $ExpectedVersion",
    "Final status: $finalStatus",
    "",
    "| Gate | Status | Duration |",
    "| --- | --- | ---: |"
)
foreach ($result in $results) {
    $lines += "| $($result.name) | $($result.status) | $($result.duration_ms) ms |"
}
$lines += ""
$lines += "Meaning:"
$lines += "- OWNER_LAB_PASS: all automated source, docs, UI, and installed-runtime evidence gates passed; public stable promotion still needs explicit owner approval."
$lines += "- OWNER_LAB_PARTIAL: source/docs/UI passed but one or more owner/install evidence gates were not current or not run."
$lines += "- OWNER_LAB_BLOCKED: at least one release blocker gate failed."
$lines | Set-Content -LiteralPath $mdPath -Encoding UTF8

Write-Host $finalStatus
Write-Host "Owner lab gate report: $jsonPath"
Write-Host "Owner lab audit docs: $mdPath"

if ($finalStatus -eq "OWNER_LAB_PASS") { exit 0 }
if ($finalStatus -eq "OWNER_LAB_PARTIAL") { exit 2 }
exit 1

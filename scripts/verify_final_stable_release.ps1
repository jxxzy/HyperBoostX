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
$finalStatus = if ($hasFail) { "FINAL_STABLE_BLOCKED" } elseif ($hasPartial) { "FINAL_STABLE_PARTIAL" } else { "FINAL_STABLE_PASS" }

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
    "# Stable Release Audit v2.10.0",
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
$lines += "- FINAL_STABLE_PASS: all source, docs, UI, and installed-runtime evidence gates passed."
$lines += "- FINAL_STABLE_PARTIAL: source/docs/UI passed but one or more owner/install evidence gates were not current or not run."
$lines += "- FINAL_STABLE_BLOCKED: at least one release blocker gate failed."
$lines | Set-Content -LiteralPath $mdPath -Encoding UTF8

Write-Host $finalStatus
Write-Host "Final stable gate report: $jsonPath"
Write-Host "Final stable audit docs: $mdPath"

if ($finalStatus -eq "FINAL_STABLE_PASS") { exit 0 }
if ($finalStatus -eq "FINAL_STABLE_PARTIAL") { exit 2 }
exit 1


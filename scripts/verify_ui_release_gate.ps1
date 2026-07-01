param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Continue"
$checks = @(
    "verify_ui_navigation.ps1",
    "verify_ui_button_handlers.ps1",
    "verify_ui_theme_resources.ps1",
    "verify_ui_placement_matrix.ps1",
    "verify_no_fake_metrics.ps1",
    "verify_no_generic_core_wrappers.ps1",
    "verify_ui_ux_quality.ps1"
)

$results = New-Object System.Collections.Generic.List[object]
foreach ($check in $checks) {
    $path = Join-Path $PSScriptRoot $check
    $started = Get-Date
    & powershell -NoProfile -ExecutionPolicy Bypass -File $path -RepoRoot $RepoRoot
    $exitCode = $LASTEXITCODE
    if ($null -eq $exitCode) { $exitCode = 0 }
    $results.Add([pscustomobject]@{
        name = $check
        status = if ($exitCode -eq 0) { "PASS" } else { "FAIL" }
        duration_ms = [int]((Get-Date) - $started).TotalMilliseconds
    })
}

$ok = -not ($results | Where-Object { $_.status -ne "PASS" })
$docsPath = Join-Path $RepoRoot "docs\UI_RELEASE_GATE_v2.10.0.md"
$lines = @(
    "# HyperBoostX v2.10.0 UI Release Gate",
    "",
    "Generated: $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz'))",
    "",
    "Overall: $(if ($ok) { 'PASS' } else { 'FAIL' })",
    "",
    "| Gate | Status | Duration |",
    "| --- | --- | ---: |"
)
foreach ($result in $results) {
    $lines += "| $($result.name) | $($result.status) | $($result.duration_ms) ms |"
}
$lines += ""
$lines += "Release is blocked if any gate fails."
$lines | Set-Content -LiteralPath $docsPath -Encoding UTF8

Write-Host "UI release gate docs: $docsPath"
if (-not $ok) { exit 1 }
exit 0

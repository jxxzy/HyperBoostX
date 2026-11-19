param(
    [string]$ActionMapPath = "wpf\Data\ui_action_map_v2_10.json",
    [string]$DataPath = "data\full_feature_integrity_v2_10.json",
    [string]$ReportPath = "docs\FULL_FEATURE_INTEGRITY_MATRIX_v2_10.md"
)

$ErrorActionPreference = "Stop"
$map = Get-Content -LiteralPath $ActionMapPath -Raw | ConvertFrom-Json
$contract = Get-Content -LiteralPath $DataPath -Raw | ConvertFrom-Json
$summary = $map.summary
$actions = @($map.menus | ForEach-Object { @($_.actions) })
$endpointCount = @($actions | ForEach-Object { "$([string]$_.method) $(([string]$_.path).Split('?')[0])" } | Sort-Object -Unique).Count
$failures = New-Object System.Collections.Generic.List[string]
if ([int]$summary.total_menus -lt [int]$contract.minimum_contract.menus) { $failures.Add("menu count below contract") }
if ([int]$summary.total_buttons -lt [int]$contract.minimum_contract.buttons) { $failures.Add("button count below contract") }
if ($endpointCount -lt [int]$contract.minimum_contract.unique_endpoints) { $failures.Add("endpoint count below contract") }
if ([int]$summary.total_partial_or_roadmap_buttons -ne 0) { $failures.Add("partial/roadmap buttons present") }
if (@($map.menus | Where-Object { $_.label -match 'v1\.3|v1\.4' }).Count -gt 0) { $failures.Add("retired version labels present") }

$status = if ($failures.Count -eq 0) { "PASS" } else { "FAIL" }
$lines = @(
    "# Full Feature Integrity Matrix v2.10",
    "",
    "Status: ``$status``",
    "",
    "| Metric | Actual | Contract |",
    "|---|---:|---:|",
    "| Menus | $($summary.total_menus) | $($contract.minimum_contract.menus) |",
    "| Buttons | $($summary.total_buttons) | $($contract.minimum_contract.buttons) |",
    "| Unique endpoints | $endpointCount | $($contract.minimum_contract.unique_endpoints) |",
    "| Partial/roadmap buttons | $($summary.total_partial_or_roadmap_buttons) | 0 |",
    "",
    "Failures: $($failures -join ', ')",
    "",
    "Feature Coverage and Action Coverage are based on ``$ActionMapPath``."
)
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $ReportPath) | Out-Null
$lines | Set-Content -LiteralPath $ReportPath -Encoding UTF8
if ($status -ne "PASS") { exit 1 }

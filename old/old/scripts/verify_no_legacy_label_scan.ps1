param([string]$ReportPath = "docs\NO_LEGACY_LABEL_SCAN_REPORT.md")
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\lib\InvokeTestCenterSuite.ps1"
$result = Invoke-HyperBoostXTestCenterSuite -SuiteId "no_legacy_label_scan" -ReportPath $ReportPath
if (-not $result.ok) { exit $result.exit_code }

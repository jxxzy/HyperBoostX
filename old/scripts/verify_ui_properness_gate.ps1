param([string]$ReportPath = "docs\UI_PROPERNESS_GATE_REPORT.md")
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\lib\InvokeTestCenterSuite.ps1"
$result = Invoke-HyperBoostXTestCenterSuite -SuiteId "ui_properness_gate" -ReportPath $ReportPath
if (-not $result.ok) { exit $result.exit_code }

param([string]$ReportPath = "docs\SAFETY_GUARD_RUNTIME_REPORT.md")
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\lib\InvokeTestCenterSuite.ps1"
$result = Invoke-HyperBoostXTestCenterSuite -SuiteId "safety_guard_runtime" -ReportPath $ReportPath
if (-not $result.ok) { exit $result.exit_code }

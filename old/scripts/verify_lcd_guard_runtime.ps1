param([string]$ReportPath = "docs\LCD_PERFORMANCE_GUARD_RUNTIME_REPORT.md")
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\lib\InvokeTestCenterSuite.ps1"
$result = Invoke-HyperBoostXTestCenterSuite -SuiteId "lcd_guard_runtime" -ReportPath $ReportPath
if (-not $result.ok) { exit $result.exit_code }

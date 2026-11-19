param([string]$ReportPath = "docs\EMBEDDED_TEST_RUNNER.md")
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\lib\InvokeTestCenterSuite.ps1"
$result = Invoke-HyperBoostXTestCenterSuite -SuiteId "full_app_smoke_test" -ReportPath $ReportPath
if (-not $result.ok) { exit $result.exit_code }

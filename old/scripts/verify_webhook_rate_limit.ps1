param([string]$ReportPath = "docs\WEBHOOK_RATE_LIMIT_POLICY.md")
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\lib\InvokeTestCenterSuite.ps1"
$result = Invoke-HyperBoostXTestCenterSuite -SuiteId "webhook_rate_limit" -ReportPath $ReportPath
if (-not $result.ok) { exit $result.exit_code }

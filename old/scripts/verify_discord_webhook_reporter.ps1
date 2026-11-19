param([string]$ReportPath = "docs\DISCORD_WEBHOOK_ERROR_REPORTING.md")
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\lib\InvokeTestCenterSuite.ps1"
$result = Invoke-HyperBoostXTestCenterSuite -SuiteId "discord_webhook" -ReportPath $ReportPath
if (-not $result.ok) { exit $result.exit_code }

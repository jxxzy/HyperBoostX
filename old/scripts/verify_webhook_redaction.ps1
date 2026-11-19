param([string]$ReportPath = "docs\WEBHOOK_REDACTION_POLICY.md")
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\lib\InvokeTestCenterSuite.ps1"
$result = Invoke-HyperBoostXTestCenterSuite -SuiteId "webhook_redaction" -ReportPath $ReportPath
if (-not $result.ok) { exit $result.exit_code }

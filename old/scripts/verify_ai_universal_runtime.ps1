param([string]$ReportPath = "docs\AI_RUNTIME_EVIDENCE.md")
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\lib\InvokeTestCenterSuite.ps1"
$result = Invoke-HyperBoostXTestCenterSuite -SuiteId "ai_universal_runtime" -ReportPath $ReportPath
if (-not $result.ok) { exit $result.exit_code }

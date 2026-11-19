param([string]$ReportPath = "docs\RESTORE_BACKUP_RUNTIME_REPORT.md")
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\lib\InvokeTestCenterSuite.ps1"
$result = Invoke-HyperBoostXTestCenterSuite -SuiteId "restore_backup_runtime" -ReportPath $ReportPath
if (-not $result.ok) { exit $result.exit_code }

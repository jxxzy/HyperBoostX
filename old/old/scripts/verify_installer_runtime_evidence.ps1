param([string]$ReportPath = "docs\INSTALLER_RUNTIME_EVIDENCE.md")
$ErrorActionPreference = "Stop"
. "$PSScriptRoot\lib\InvokeTestCenterSuite.ps1"
$result = Invoke-HyperBoostXTestCenterSuite -SuiteId "installer_runtime" -ReportPath $ReportPath
Copy-Item -LiteralPath $ReportPath -Destination "docs\OWNER_ADMIN_RUNTIME_GATE_REPORT.md" -Force
if (-not $result.ok) { exit $result.exit_code }

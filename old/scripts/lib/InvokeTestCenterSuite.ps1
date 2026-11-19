function Invoke-HyperBoostXTestCenterSuite {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SuiteId,

        [Parameter(Mandatory = $true)]
        [string]$ReportPath
    )

    $repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $env:PYTHONPATH = "$repoRoot\app;$repoRoot"
    $python = @"
import json
import sys
from services.test_center_service import TestCenterService

result = TestCenterService.run_suite("$SuiteId")
print(json.dumps(result, indent=2))
sys.exit(0 if result.get("status") == "pass" else 1)
"@

    $output = $python | python -
    $exit = $LASTEXITCODE
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $ReportPath) | Out-Null
    $payload = $output | ConvertFrom-Json
    $lines = @(
        "# $($payload.suite_name)",
        "",
        "Suite: ``$SuiteId``",
        "Status: ``$($payload.status)``",
        "Finished: ``$($payload.finished_at)``",
        "",
        "Message: $($payload.message)",
        "",
        '```json',
        ($payload | ConvertTo-Json -Depth 20),
        '```'
    )
    $lines | Set-Content -LiteralPath $ReportPath -Encoding UTF8
    return [pscustomobject]@{
        ok = ($exit -eq 0)
        exit_code = $exit
        payload = $payload
        report = $ReportPath
    }
}

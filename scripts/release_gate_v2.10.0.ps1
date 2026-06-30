param(
    [switch]$SkipDotnet,
    [switch]$SkipFullQa,
    [switch]$SkipInstall
)

$ErrorActionPreference = "Stop"

Write-Host "== HyperBoostX v2.10.0 beta release gate =="

powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\generate_ui_action_map_v2_10.ps1 | Out-Host
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\generate_v210_audit_docs.ps1 | Out-Host
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify_version_sync.ps1 | Out-Host

app\venv\Scripts\python.exe -m pytest -q tests\test_ui_action_map_v210.py tests\test_runtime_route_contract.py tests\test_v13_api_contract.py

if (-not $SkipDotnet) {
    dotnet build HyperBoostX.sln -v minimal
    dotnet test dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj -c Debug -v minimal
}

if (-not $SkipFullQa) {
    $args = @()
    if ($SkipInstall) {
        $args += "-SkipInstall"
    }
    powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\full_qa_gate.ps1 @args | Out-Host
}

Write-Host "Release gate complete. Stable label is still blocked until installed runtime, admin rollback, hardware matrix, and signing gates pass."

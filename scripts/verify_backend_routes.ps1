param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"

$python = Join-Path $RepoRoot "app\venv\Scripts\python.exe"
if (-not (Test-Path -LiteralPath $python)) {
    $python = "python"
}

$outDir = Join-Path $RepoRoot "docs\runtime-audit"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$jsonPath = Join-Path $outDir "backend_routes_report.json"
$mdPath = Join-Path $outDir "backend_routes_report.md"

$output = & $python -m pytest -q tests\test_runtime_route_contract.py 2>&1
$exitCode = $LASTEXITCODE

$report = [pscustomobject]@{
    generated_at = (Get-Date).ToUniversalTime().ToString("o")
    repo_root = $RepoRoot
    command = "$python -m pytest -q tests\test_runtime_route_contract.py"
    ok = ($exitCode -eq 0)
    exit_code = $exitCode
    output = ($output -join "`n")
}

$report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $jsonPath -Encoding UTF8
$statusText = if ($exitCode -eq 0) { "PASS" } else { "FAIL" }
@(
    "# HyperBoostX Backend Route Verification",
    "",
    "Status: $statusText",
    "",
    '```text',
    ($output -join "`n"),
    '```'
) | Set-Content -LiteralPath $mdPath -Encoding UTF8

Write-Host "Backend route report: $jsonPath"
if ($exitCode -ne 0) { exit $exitCode }

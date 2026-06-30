param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

function Invoke-Checked([string]$Name, [scriptblock]$Command) {
    Write-Host "Running: $Name"
    & $Command
    if ($LASTEXITCODE -ne 0) {
        $script:failures.Add("$Name failed with exit code $LASTEXITCODE")
    }
}

$python = Join-Path $RepoRoot "app/venv/Scripts/python.exe"
if (-not (Test-Path -LiteralPath $python)) {
    $python = "python"
}

Push-Location $RepoRoot
try {
    Invoke-Checked "backend route contract" { & $python -m pytest -q tests/test_runtime_route_contract.py }
    Invoke-Checked "WPF button handlers" { powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepoRoot "scripts/verify_wpf_button_handlers.ps1") -RepoRoot $RepoRoot }
    Invoke-Checked "placeholder guard" { powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepoRoot "scripts/verify_placeholder_guard.ps1") -RepoRoot $RepoRoot }
    Invoke-Checked ".NET backend/client contract tests" { dotnet test dotnet-tests/HyperBoostX.Tests/HyperBoostX.Tests.csproj -c Debug --no-restore }
}
finally {
    Pop-Location
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: real usability verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: real usability verification" -ForegroundColor Green

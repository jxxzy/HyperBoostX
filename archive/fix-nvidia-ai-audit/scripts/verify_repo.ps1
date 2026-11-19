[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$SkipPython,
    [switch]$SkipDotnet,
    [switch]$NoRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$pythonExe = Join-Path $repoRoot "app\venv\Scripts\python.exe"
$dotnetTestProject = Join-Path $repoRoot "dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj"

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    Write-Host ""
    Write-Host ("=" * 72) -ForegroundColor DarkGray
    Write-Host $Name -ForegroundColor Cyan
    Write-Host ("=" * 72) -ForegroundColor DarkGray

    $startedAt = Get-Date
    & $Action
    $elapsed = (Get-Date) - $startedAt
    Write-Host ("Completed in {0:n1}s" -f $elapsed.TotalSeconds) -ForegroundColor Green
}

Push-Location $repoRoot
try {
    Write-Host "HyperBoost X repo verification" -ForegroundColor Yellow
    Write-Host "Repo root: $repoRoot"

    Invoke-Step "Version sync" {
        & (Join-Path $repoRoot "scripts\verify_version_sync.ps1")
        if (-not $?) {
            throw "Version sync check failed."
        }
    }

    if (-not $SkipPython) {
        if (-not (Test-Path $pythonExe)) {
            throw "Python virtual environment not found at '$pythonExe'."
        }

        Invoke-Step "Python backend tests" {
            $env:PYTHONPATH = "$repoRoot;$repoRoot\app"
            & $pythonExe -m pytest -q tests
            if ($LASTEXITCODE -ne 0) {
                throw "Python tests failed with exit code $LASTEXITCODE."
            }
        }
    }

    if (-not $SkipDotnet) {
        if (-not (Test-Path $dotnetTestProject)) {
            throw ".NET test project not found at '$dotnetTestProject'."
        }

        Invoke-Step ".NET desktop tests" {
            $arguments = @(
                "test",
                $dotnetTestProject,
                "-c", $Configuration,
                "-v", "minimal",
                "--logger", "console;verbosity=normal"
            )

            if ($NoRestore) {
                $arguments += "--no-restore"
            }

            & dotnet @arguments
            if ($LASTEXITCODE -ne 0) {
                throw ".NET tests failed with exit code $LASTEXITCODE."
            }
        }
    }

    Write-Host ""
    Write-Host "Repository verification passed." -ForegroundColor Green
}
finally {
    Pop-Location
}

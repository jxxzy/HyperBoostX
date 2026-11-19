[CmdletBinding()]
param(
    [string]$OutputRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $projectRoot "artifacts\local-deploy"
}

$OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)

function Remove-IfExists {
    param([string]$PathValue)
    if (Test-Path $PathValue) {
        Remove-Item -LiteralPath $PathValue -Recurse -Force
    }
}

function Ensure-Dir {
    param([string]$PathValue)
    New-Item -ItemType Directory -Force -Path $PathValue | Out-Null
}

function Invoke-Checked {
    param(
        [string]$Description,
        [scriptblock]$Command
    )

    Write-Host $Description
    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE"
    }
}

$backendOut = Join-Path $OutputRoot "backend"
$launcherOut = Join-Path $OutputRoot "launcher"
$wpfOut = Join-Path $OutputRoot "wpf"
$portableOut = Join-Path $OutputRoot "app"
$packageOut = Join-Path $OutputRoot "package"
$pyInstallerWork = Join-Path $OutputRoot "pyinstaller\build"
$pyInstallerDist = Join-Path $OutputRoot "pyinstaller\dist"
$pyInstallerSpec = Join-Path $OutputRoot "pyinstaller\spec"

Write-Host "Cleaning local deploy artifacts..."
Remove-IfExists $OutputRoot
Ensure-Dir $backendOut
Ensure-Dir $launcherOut
Ensure-Dir $wpfOut
Ensure-Dir $portableOut
Ensure-Dir $packageOut
Ensure-Dir $pyInstallerWork
Ensure-Dir $pyInstallerDist
Ensure-Dir $pyInstallerSpec

Push-Location (Join-Path $projectRoot "app")
try {
    $pythonExe = Join-Path (Get-Location) "venv\Scripts\python.exe"
    $dataDir = Join-Path (Get-Location) "data"
    if (-not (Test-Path $pythonExe)) {
        $pythonExe = "python"
    }

    Invoke-Checked "Building backend into isolated artifacts..." {
        & $pythonExe -m PyInstaller --clean --noconfirm --onefile --name hyperboost_backend --distpath $pyInstallerDist --workpath $pyInstallerWork --specpath $pyInstallerSpec --add-data "$dataDir;data" --hidden-import flask_sock --hidden-import wmi --hidden-import psutil backend_server.py
    }
    $builtBackend = Join-Path $pyInstallerDist "hyperboost_backend.exe"
    if (-not (Test-Path $builtBackend)) {
        throw "Backend executable was not produced."
    }

    Copy-Item $builtBackend $backendOut -Force
}
finally {
    Pop-Location
}

Invoke-Checked "Restoring launcher for win-x64..." {
    dotnet restore launcher\HyperBoostLauncher.csproj -r win-x64
}

Invoke-Checked "Publishing launcher into isolated artifacts..." {
    dotnet publish launcher\HyperBoostLauncher.csproj -c Release -r win-x64 --self-contained true --no-restore /p:PublishSingleFile=true -o $launcherOut
}

Invoke-Checked "Restoring WPF for win-x64..." {
    dotnet restore wpf\HyperBoostX.csproj -r win-x64
}

Invoke-Checked "Publishing WPF into isolated artifacts..." {
    dotnet publish wpf\HyperBoostX.csproj -c Release -r win-x64 --self-contained true --no-restore /p:PublishSingleFile=false -o $wpfOut
}

$requiredOutputs = @(
    (Join-Path $launcherOut "HyperBoostLauncher.exe"),
    (Join-Path $backendOut "hyperboost_backend.exe"),
    (Join-Path $wpfOut "HyperBoostX.exe")
)
foreach ($requiredOutput in $requiredOutputs) {
    if (-not (Test-Path -LiteralPath $requiredOutput)) {
        throw "Required build output missing: $requiredOutput"
    }
}

Write-Host "Assembling portable/runtime layout..."
Ensure-Dir (Join-Path $portableOut "runtime\backend")
Ensure-Dir (Join-Path $portableOut "runtime\wpf")
Ensure-Dir (Join-Path $packageOut "backend")
Ensure-Dir (Join-Path $packageOut "launcher")
Ensure-Dir (Join-Path $packageOut "wpf")

Copy-Item (Join-Path $launcherOut "HyperBoostLauncher.exe") (Join-Path $portableOut "HyperBoostX.exe") -Force
Copy-Item (Join-Path $backendOut "hyperboost_backend.exe") (Join-Path $portableOut "runtime\backend\hyperboost_backend.exe") -Force
Copy-Item (Join-Path $wpfOut "*") (Join-Path $portableOut "runtime\wpf") -Recurse -Force

Copy-Item (Join-Path $backendOut "hyperboost_backend.exe") (Join-Path $packageOut "backend\hyperboost_backend.exe") -Force
Copy-Item (Join-Path $launcherOut "HyperBoostLauncher.exe") (Join-Path $packageOut "launcher\HyperBoostLauncher.exe") -Force
Copy-Item (Join-Path $wpfOut "*") (Join-Path $packageOut "wpf") -Recurse -Force

Write-Host "Local deploy artifacts ready."
Write-Host "Portable runtime: $(Join-Path $portableOut 'HyperBoostX.exe')"
Write-Host "Package root: $packageOut"

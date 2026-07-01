[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

$releaseDir = Join-Path $projectRoot "release"
$packageDir = Join-Path $releaseDir "package"
$portableDir = Join-Path $releaseDir "app"
$backendReleaseDir = Join-Path $releaseDir "backend"
$launcherReleaseDir = Join-Path $releaseDir "launcher"
$wpfReleaseDir = Join-Path $releaseDir "wpf"
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

function Get-NsisExe {
    if (Get-Command makensis -ErrorAction SilentlyContinue) {
        return "makensis"
    }

    $candidates = @(
        "C:\Program Files (x86)\NSIS\makensis.exe",
        "C:\Program Files\NSIS\makensis.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw "NSIS makensis was not found."
}

function Assert-SignedArtifacts {
    param([string[]]$Paths)

    foreach ($path in $Paths) {
        if (-not (Test-Path $path)) {
            throw "Signed artifact check failed because file is missing: $path"
        }

        $signature = Get-AuthenticodeSignature -FilePath $path
        if ($signature.Status -ne "Valid") {
            throw "Code signing is required, but signature status for '$path' is '$($signature.Status)'."
        }
    }
}

Write-Host "Cleaning previous release outputs..."
Remove-IfExists $backendReleaseDir
Remove-IfExists $launcherReleaseDir
Remove-IfExists $wpfReleaseDir
Remove-IfExists $packageDir
Remove-IfExists $portableDir
Ensure-Dir $backendReleaseDir
Ensure-Dir $launcherReleaseDir
Ensure-Dir $wpfReleaseDir
Ensure-Dir $packageDir
Ensure-Dir $portableDir
Write-Host "Building Python backend with PyInstaller..."
Push-Location (Join-Path $projectRoot "app")
try {
    $pythonExe = Join-Path (Get-Location) "venv\Scripts\python.exe"
    if (-not (Test-Path $pythonExe)) {
        $pythonExe = "python"
    }

    & $pythonExe -m PyInstaller --clean --noconfirm --onefile --name hyperboost_backend --add-data "data;data" --hidden-import flask_sock --hidden-import wmi --hidden-import psutil backend_server.py
    if (-not (Test-Path "dist\hyperboost_backend.exe")) {
        throw "Backend executable was not produced."
    }

    Copy-Item "dist\hyperboost_backend.exe" $backendReleaseDir -Force
}
finally {
    Pop-Location
}

Write-Host "Publishing launcher..."
dotnet publish launcher\HyperBoostLauncher.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
Copy-Item "launcher\bin\Release\net8.0-windows\win-x64\publish\*" $launcherReleaseDir -Recurse -Force

Write-Host "Publishing WPF..."
dotnet publish wpf\HyperBoostX.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=false
Copy-Item "wpf\bin\Release\net8.0-windows\win-x64\publish\*" $wpfReleaseDir -Recurse -Force

Write-Host "Assembling packaged release..."
Ensure-Dir (Join-Path $packageDir "backend")
Ensure-Dir (Join-Path $packageDir "launcher")
Ensure-Dir (Join-Path $packageDir "wpf")
Ensure-Dir (Join-Path $portableDir "runtime\backend")
Ensure-Dir (Join-Path $portableDir "runtime\wpf")

Copy-Item (Join-Path $backendReleaseDir "hyperboost_backend.exe") (Join-Path $packageDir "backend\hyperboost_backend.exe") -Force
Copy-Item (Join-Path $launcherReleaseDir "HyperBoostLauncher.exe") (Join-Path $packageDir "launcher\HyperBoostLauncher.exe") -Force
Copy-Item (Join-Path $wpfReleaseDir "*") (Join-Path $packageDir "wpf") -Recurse -Force

Copy-Item (Join-Path $launcherReleaseDir "HyperBoostLauncher.exe") (Join-Path $portableDir "HyperBoostX.exe") -Force
Copy-Item (Join-Path $backendReleaseDir "hyperboost_backend.exe") (Join-Path $portableDir "runtime\backend\hyperboost_backend.exe") -Force
Copy-Item (Join-Path $wpfReleaseDir "*") (Join-Path $portableDir "runtime\wpf") -Recurse -Force

Write-Host "Building installer..."
$nsisExe = Get-NsisExe
& $nsisExe "HyperBoostXInstaller.nsi"
if (-not (Test-Path (Join-Path $projectRoot "HyperBoostXInstaller.exe"))) {
    throw "Installer was not produced."
}

Write-Host "Generating docs\release\checksums\SHA256SUMS.txt..."
$checksumTargets = @(
    (Join-Path $projectRoot "HyperBoostXInstaller.exe"),
    (Join-Path $backendReleaseDir "hyperboost_backend.exe"),
    (Join-Path $launcherReleaseDir "HyperBoostLauncher.exe"),
    (Join-Path $wpfReleaseDir "HyperBoostX.exe")
) | Where-Object { Test-Path $_ }

$checksumLines = foreach ($target in $checksumTargets) {
    $hash = (Get-FileHash -Algorithm SHA256 -Path $target).Hash.ToLowerInvariant()
    "{0} *{1}" -f $hash, (Split-Path $target -Leaf)
}

$checksumDir = Join-Path $projectRoot "docs\release\checksums"
Ensure-Dir $checksumDir
$checksumPath = Join-Path $checksumDir "SHA256SUMS.txt"
Set-Content -Path $checksumPath -Value $checksumLines -Encoding ASCII

if ($env:HYPERBOOSTX_REQUIRE_SIGNING -eq "1") {
    Write-Host "Enforcing signed artifacts..."
    Assert-SignedArtifacts @(
        (Join-Path $projectRoot "HyperBoostXInstaller.exe"),
        (Join-Path $backendReleaseDir "hyperboost_backend.exe"),
        (Join-Path $launcherReleaseDir "HyperBoostLauncher.exe"),
        (Join-Path $wpfReleaseDir "HyperBoostX.exe")
    )
}

Write-Host "Release build completed."

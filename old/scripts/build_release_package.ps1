[CmdletBinding()]
param(
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$scriptPath = Join-Path $PSScriptRoot "package_installer_v2.10.0.ps1"
if (-not (Test-Path -LiteralPath $scriptPath)) {
    throw "Packaging script not found: $scriptPath"
}

$argsForPackage = @(
    "-NoProfile",
    "-ExecutionPolicy", "Bypass",
    "-File", $scriptPath
)
if ($SkipBuild) {
    $argsForPackage += "-SkipBuild"
}

& powershell @argsForPackage
exit $LASTEXITCODE

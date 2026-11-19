[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$version = (Get-Content (Join-Path $repoRoot "VERSION") -Raw).Trim()
if ([string]::IsNullOrWhiteSpace($version)) {
    throw "VERSION is empty."
}

function Assert-Contains {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Pattern,
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    $content = Get-Content $Path -Raw
    if ($content -notmatch $Pattern) {
        throw $Message
    }
}

$escapedVersion = [regex]::Escape($version)
$assemblyVersion = "$version.0"
$escapedAssemblyVersion = [regex]::Escape($assemblyVersion)

Assert-Contains `
    -Path (Join-Path $repoRoot "app\core\config.py") `
    -Pattern "VERSION\s*=\s*`"$escapedVersion`"" `
    -Message "app/core/config.py VERSION does not match $version."

foreach ($project in @("wpf\HyperBoostX.csproj", "launcher\HyperBoostLauncher.csproj")) {
    $path = Join-Path $repoRoot $project
    Assert-Contains -Path $path -Pattern "<Version>$escapedVersion</Version>" -Message "$project <Version> does not match $version."
    Assert-Contains -Path $path -Pattern "<AssemblyVersion>$escapedAssemblyVersion</AssemblyVersion>" -Message "$project <AssemblyVersion> does not match $assemblyVersion."
    Assert-Contains -Path $path -Pattern "<FileVersion>$escapedAssemblyVersion</FileVersion>" -Message "$project <FileVersion> does not match $assemblyVersion."
    Assert-Contains -Path $path -Pattern "<InformationalVersion>$escapedVersion</InformationalVersion>" -Message "$project <InformationalVersion> does not match $version."
}

Assert-Contains `
    -Path (Join-Path $repoRoot "README.md") `
    -Pattern "\b$escapedVersion\b" `
    -Message "README.md does not mention current VERSION $version."

Write-Host "Version sync verified: $version"

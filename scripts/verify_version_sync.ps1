param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"

function Add-Check {
    param([string]$Name, [string]$Expected, [string]$Actual)
    [pscustomobject]@{
        name = $Name
        expected = $Expected
        actual = $Actual
        ok = ($Expected -eq $Actual)
    }
}

function Get-WindowsVersion {
    param([string]$SemVer)
    $match = [regex]::Match($SemVer, '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)')
    if (-not $match.Success) {
        throw "VERSION must start with numeric SemVer core major.minor.patch. Actual: $SemVer"
    }
    return "{0}.{1}.{2}.0" -f $match.Groups["major"].Value, $match.Groups["minor"].Value, $match.Groups["patch"].Value
}

$versionPath = Join-Path $RepoRoot "VERSION"
$expectedVersion = (Get-Content -LiteralPath $versionPath -Raw).Trim()
$expectedWindowsVersion = Get-WindowsVersion $expectedVersion

$constantsText = Get-Content -LiteralPath (Join-Path $RepoRoot "app\core\constants.py") -Raw
$backendVersion = [regex]::Match($constantsText, 'APP_VERSION\s*=\s*"([^"\r\n]+)"').Groups[1].Value

[xml]$csproj = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\HyperBoostX.csproj") -Raw
$wpfVersion = $csproj.Project.PropertyGroup.Version
$wpfFileVersion = $csproj.Project.PropertyGroup.FileVersion
$wpfInformationalVersion = $csproj.Project.PropertyGroup.InformationalVersion

[xml]$launcherCsproj = Get-Content -LiteralPath (Join-Path $RepoRoot "launcher\HyperBoostLauncher.csproj") -Raw
$launcherVersion = $launcherCsproj.Project.PropertyGroup.Version
$launcherFileVersion = $launcherCsproj.Project.PropertyGroup.FileVersion
$launcherInformationalVersion = $launcherCsproj.Project.PropertyGroup.InformationalVersion

$installerText = Get-Content -LiteralPath (Join-Path $RepoRoot "HyperBoostXInstaller.nsi") -Raw
$installerDisplayVersion = [regex]::Match($installerText, 'DisplayVersion"\s+"([^"\r\n]+)"').Groups[1].Value
$installerProductVersion = [regex]::Match($installerText, 'VIProductVersion\s+"([^"\r\n]+)"').Groups[1].Value
$installerResourceProductVersion = [regex]::Match($installerText, 'VIAddVersionKey\s+"ProductVersion"\s+"([^"\r\n]+)"').Groups[1].Value

$checks = @(
    Add-Check "backend APP_VERSION" $expectedVersion $backendVersion
    Add-Check "WPF Version" $expectedVersion $wpfVersion
    Add-Check "WPF FileVersion" $expectedWindowsVersion $wpfFileVersion
    Add-Check "WPF InformationalVersion" $expectedVersion $wpfInformationalVersion
    Add-Check "Launcher Version" $expectedVersion $launcherVersion
    Add-Check "Launcher FileVersion" $expectedWindowsVersion $launcherFileVersion
    Add-Check "Launcher InformationalVersion" $expectedVersion $launcherInformationalVersion
    Add-Check "Installer DisplayVersion" $expectedVersion $installerDisplayVersion
    Add-Check "Installer VIProductVersion" $expectedWindowsVersion $installerProductVersion
    Add-Check "Installer resource ProductVersion" $expectedVersion $installerResourceProductVersion
)

$report = [pscustomobject]@{
    generated_at = (Get-Date).ToUniversalTime().ToString("o")
    repo_root = $RepoRoot
    expected_version = $expectedVersion
    expected_windows_version = $expectedWindowsVersion
    checks = $checks
    ok = -not ($checks | Where-Object { -not $_.ok })
}

$outDir = Join-Path $RepoRoot "docs\runtime-audit"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$jsonPath = Join-Path $outDir "version_sync_report.json"
$mdPath = Join-Path $outDir "version_sync_report.md"
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

$md = @()
$md += "# HyperBoostX Version Sync Report"
$md += ""
$md += "Expected version: $expectedVersion"
$md += "Expected Windows numeric version: $expectedWindowsVersion"
$md += ""
foreach ($check in $checks) {
    $status = if ($check.ok) { "PASS" } else { "FAIL" }
    $md += "- $status - $($check.name): $($check.actual)"
}
$md | Set-Content -LiteralPath $mdPath -Encoding UTF8
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepoRoot "scripts\redact_release_evidence.ps1") -RepoRoot $RepoRoot -Paths $jsonPath,$mdPath | Out-Null

Write-Host "Version sync report: $jsonPath"
if (-not $report.ok) { exit 1 }

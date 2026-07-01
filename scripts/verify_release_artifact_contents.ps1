param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")),
    [string]$PackageRoot = ""
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "lib\HyperBoostXReleaseContract.ps1")

if ([string]::IsNullOrWhiteSpace($PackageRoot)) {
    $PackageRoot = Join-Path $RepoRoot "release\package"
}

function Get-WindowsVersion {
    param([string]$SemVer)
    $match = [regex]::Match($SemVer, '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)')
    if (-not $match.Success) {
        throw "VERSION must start with numeric SemVer core major.minor.patch. Actual: $SemVer"
    }
    return "{0}.{1}.{2}.0" -f $match.Groups["major"].Value, $match.Groups["minor"].Value, $match.Groups["patch"].Value
}

$expectedVersion = (Get-Content -LiteralPath (Join-Path $RepoRoot "VERSION") -Raw).Trim()
$expectedWindowsVersion = Get-WindowsVersion $expectedVersion
$installerPath = Join-Path $RepoRoot "HyperBoostXInstaller.exe"

$requiredSourcePaths = @(
    "wpf\Views\DashboardView.xaml",
    "wpf\Views\FeatureAuditView.xaml",
    "wpf\Themes\CyberTheme.xaml",
    "wpf\Themes\AccentColors.xaml",
    "wpf\Styles\Buttons.xaml",
    "wpf\Styles\Sidebar.xaml",
    "app\backend_server.py",
    "launcher\Program.cs"
)

$requiredPackagePaths = @(
    "launcher\HyperBoostLauncher.exe",
    "backend\hyperboost_backend.exe",
    "wpf\HyperBoostX.exe",
    "wpf\HyperBoostX.dll",
    "wpf\Data",
    "wpf\Data\ui_action_map_v2_10.json"
)

$checks = New-Object System.Collections.Generic.List[object]
foreach ($path in $requiredSourcePaths) {
    $full = Join-Path $RepoRoot $path
    $checks.Add([pscustomobject]@{ name = "source contains $path"; ok = (Test-Path -LiteralPath $full); evidence = $full })
}

foreach ($path in $requiredPackagePaths) {
    $full = Join-Path $PackageRoot $path
    $checks.Add([pscustomobject]@{ name = "package contains $path"; ok = (Test-Path -LiteralPath $full); evidence = $full })
}

$checks.Add([pscustomobject]@{ name = "installer exists"; ok = (Test-Path -LiteralPath $installerPath); evidence = $installerPath })

$launcherExe = Join-Path $PackageRoot "launcher\HyperBoostLauncher.exe"
$wpfExe = Join-Path $PackageRoot "wpf\HyperBoostX.exe"
foreach ($artifact in @(
    @{ name = "launcher file version"; path = $launcherExe },
    @{ name = "WPF file version"; path = $wpfExe }
)) {
    $version = $null
    if (Test-Path -LiteralPath $artifact.path) {
        $version = (Get-Item -LiteralPath $artifact.path).VersionInfo.FileVersion
    }
    $checks.Add([pscustomobject]@{ name = "$($artifact.name) matches $expectedWindowsVersion"; ok = ($version -eq $expectedWindowsVersion); evidence = if ($version) { $version } else { $artifact.path } })
}

$legacyEntry = Join-Path $PackageRoot "app\main.py"
$checks.Add([pscustomobject]@{ name = "package does not expose legacy Python UI entrypoint as app launcher"; ok = -not (Test-Path -LiteralPath $legacyEntry); evidence = $legacyEntry })

$sourceActionMap = Join-Path $RepoRoot "wpf\Data\ui_action_map_v2_10.json"
$packageActionMap = Join-Path $PackageRoot "wpf\Data\ui_action_map_v2_10.json"
foreach ($result in @(
    (Test-HyperBoostXActionMapContract -ActionMapPath $sourceActionMap -ExpectedVersion $expectedVersion -NamePrefix "source action map"),
    (Test-HyperBoostXActionMapContract -ActionMapPath $packageActionMap -ExpectedVersion $expectedVersion -NamePrefix "package action map")
)) {
    foreach ($check in $result.checks) {
        $checks.Add($check)
    }
}

$report = [pscustomobject]@{
    generated_at = (Get-Date).ToUniversalTime().ToString("o")
    repo_root = $RepoRoot
    package_root = $PackageRoot
    expected_version = $expectedVersion
    expected_windows_version = $expectedWindowsVersion
    action_map_contract = Get-HyperBoostXReleaseContract
    checks = $checks
    ok = -not ($checks | Where-Object { -not $_.ok })
}

$outDir = Join-Path $RepoRoot "docs\runtime-audit"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$jsonPath = Join-Path $outDir "release_artifact_contents_report.json"
$mdPath = Join-Path $outDir "release_artifact_contents_report.md"
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $jsonPath -Encoding UTF8

$md = @("# HyperBoostX Release Artifact Contents", "", "Version: $expectedVersion", "Windows file version: $expectedWindowsVersion", "Package root: $PackageRoot", "")
foreach ($check in $checks) {
    $status = if ($check.ok) { "PASS" } else { "FAIL" }
    $md += "- $status - $($check.name)"
}
$md | Set-Content -LiteralPath $mdPath -Encoding UTF8
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $RepoRoot "scripts\redact_release_evidence.ps1") -RepoRoot $RepoRoot -Paths $jsonPath,$mdPath | Out-Null

Write-Host "Release artifact report: $jsonPath"
if (-not $report.ok) { exit 1 }

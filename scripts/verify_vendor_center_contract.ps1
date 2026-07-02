param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

function Read-RequiredFile([string]$RelativePath) {
    $path = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $script:failures.Add("Missing file: $RelativePath")
        return ""
    }

    return Get-Content -LiteralPath $path -Raw
}

$mainVm = Read-RequiredFile "wpf\ViewModels\MainWindowViewModel.cs"
$mainWindow = Read-RequiredFile "wpf\MainWindow.xaml.cs"
$legacyCatalog = Read-RequiredFile "wpf\ViewModels\LegacyFeatureCatalog.cs"
$placementVm = Read-RequiredFile "wpf\ViewModels\PlacementPageViewModel.cs"
$actionMapGenerator = Read-RequiredFile "scripts\generate_ui_action_map_v2_10.ps1"
$view = Read-RequiredFile "wpf\Views\HardwareVendorCenterView.xaml"

foreach ($pair in @(
    @{ Name = "MainWindowViewModel"; Text = $mainVm },
    @{ Name = "MainWindow route"; Text = $mainWindow },
    @{ Name = "LegacyFeatureCatalog"; Text = $legacyCatalog },
    @{ Name = "PlacementPageViewModel"; Text = $placementVm },
    @{ Name = "Action map generator"; Text = $actionMapGenerator }
)) {
    if ($pair.Text -notmatch [regex]::Escape("HardwareVendorCenter")) {
        $failures.Add("$($pair.Name) missing vendor center key.")
    }
}

foreach ($pair in @(
    @{ Name = "MainWindowViewModel"; Text = $mainVm },
    @{ Name = "Action map generator"; Text = $actionMapGenerator },
    @{ Name = "HardwareVendorCenterView"; Text = $view }
)) {
    if ($pair.Text -notmatch [regex]::Escape("Hardware Vendor Center")) {
        $failures.Add("$($pair.Name) missing Hardware Vendor Center label.")
    }
}

foreach ($pair in @(
    @{ Name = "LegacyFeatureCatalog"; Text = $legacyCatalog },
    @{ Name = "PlacementPageViewModel"; Text = $placementVm },
    @{ Name = "HardwareVendorCenterView"; Text = $view }
)) {
    if ($pair.Text -notmatch [regex]::Escape("Vendor App Analyzer")) {
        $failures.Add("$($pair.Name) missing visible Vendor App Analyzer content.")
    }
}

foreach ($token in @("/api/hardware/vendors", "/api/hardware/profile", "/api/protection/evaluate-action")) {
    if ($actionMapGenerator -notmatch [regex]::Escape($token)) {
        $failures.Add("Vendor Center action map missing endpoint: $token")
    }
}

foreach ($token in @("MSI Center", "ASUS Armoury Crate", "Gigabyte Control Center", "TRCC", "KANALI", "HiMOS", "fan", "RGB", "LCD", "driver")) {
    if (($legacyCatalog + $placementVm) -notmatch [regex]::Escape($token)) {
        $failures.Add("Vendor Center content missing required coverage: $token")
    }
}

$beginnerBlock = [regex]::Match($mainVm, 'BeginnerNavigationKeys\s*=\s*new\[\]\s*\{(?<body>.*?)\};', [System.Text.RegularExpressions.RegexOptions]::Singleline).Groups["body"].Value
if ($beginnerBlock -notmatch [regex]::Escape("HardwareVendorCenter")) {
    $failures.Add("HardwareVendorCenter must be visible in Beginner mode per final addendum.")
}
if ($beginnerBlock -match [regex]::Escape("MsiSafeOptimizer")) {
    $failures.Add("MsiSafeOptimizer must remain outside Beginner top-level navigation.")
}
if ($mainVm -notmatch 'Key\s*=\s*"MsiSafeOptimizer".*Group\s*=\s*"Advanced System"') {
    $failures.Add("MsiSafeOptimizer must remain under Advanced System.")
}
if ($view -match '<views:PlacementPageChrome') {
    $failures.Add("HardwareVendorCenterView must not be a full-page PlacementPageChrome wrapper.")
}
if ($view -notmatch '<views:PlacementActionPageBase') {
    $failures.Add("HardwareVendorCenterView must use the wired PlacementActionPageBase shell.")
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: Vendor Center contract verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: Vendor Center contract verification" -ForegroundColor Green

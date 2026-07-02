param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

function Assert-Contains {
    param([string]$RelativePath, [string[]]$Tokens)
    $path = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $script:failures.Add("Missing file: $RelativePath")
        return
    }
    $text = Get-Content -LiteralPath $path -Raw
    foreach ($token in $Tokens) {
        if ($text -notmatch [regex]::Escape($token)) {
            $script:failures.Add("$RelativePath missing body marker: $token")
        }
    }
}

Assert-Contains "wpf\Views\DashboardView.xaml" @(
    "Live Hardware Snapshot",
    "Smart Scan Results",
    "Recommendations Preview",
    "Activity &amp; Shortcuts"
)

Assert-Contains "wpf\Views\SettingsView.xaml" @(
    "General",
    "Appearance",
    "Motion &amp; Accessibility",
    "Experience Mode",
    "Safety Guard",
    "Backend &amp; Local Engine",
    "Privacy &amp; Local Data",
    "Reports &amp; History",
    "Updates",
    "Technical Details"
)

Assert-Contains "wpf\Views\AboutView.xaml" @(
    "Product Summary",
    "Release Status",
    "Safety &amp; Transparency",
    "Key Features",
    "Architecture",
    "Developer / Author",
    "Release &amp; Support Actions",
    "Technical Details"
)

$corePageFiles = @{
    Dashboard = "DashboardView"
    PerformanceBoost = "PerformanceBoostView"
    StartupManager = "StartupManagerView"
    BackgroundApps = "BackgroundAppsView"
    Cleanup = "CleanupView"
    Storage = "StorageView"
    OneClickBoost = "OneClickBoostView"
    AutoGamingMode = "AutoGamingModeView"
    AIPerformanceAdvisor = "AIPerformanceAdvisorView"
    GpuCenter = "GpuCenterView"
    GamingBooster = "GamingBoosterView"
    StreamingCenter = "StreamingCenterView"
    CreatorMode = "CreatorModeView"
    NetworkBooster = "NetworkBoosterView"
    DnsLatencyTools = "DnsLatencyToolsView"
    PrivacyCenter = "PrivacyCenterView"
    SecurityHealth = "SecurityHealthView"
    AppsManager = "AppsManagerView"
    TweaksCenter = "TweaksCenterView"
    WindowsFeatures = "WindowsFeaturesView"
    UpdateControl = "UpdateControlView"
    RepairTools = "RepairToolsView"
    DriverUpdateCenter = "DriverUpdateCenterView"
    AppUninstaller = "AppUninstallerView"
    RestoreBackup = "RestoreBackupView"
    Settings = "SettingsView"
    About = "AboutView"
}

foreach ($entry in $corePageFiles.GetEnumerator()) {
    $path = "wpf\Views\$($entry.Value).xaml"
    Assert-Contains $path @(
        "CORE_UI:$($entry.Key)",
        "CoreFeaturePage_$($entry.Key)"
    )
}

Assert-Contains "wpf\Views\PlacementPageChrome.xaml" @(
    "PlacementSections",
    "PrimaryPlacementActions",
    "SecondaryPlacementActions",
    "RestorePlacementActions",
    "Technical Details"
)

$placementVm = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\ViewModels\PlacementPageViewModel.cs") -Raw
foreach ($key in @(
    "PerformanceBoost","StartupManager","BackgroundApps","Cleanup","Storage","OneClickBoost",
    "AutoGamingMode","AIPerformanceAdvisor","GpuCenter","GamingBooster","CreatorMode",
    "NetworkBooster","DnsLatencyTools","PrivacyCenter","SecurityHealth","AppsManager",
    "TweaksCenter","WindowsFeatures","UpdateControl","RepairTools","DriverUpdateCenter",
    "AppUninstaller","RestoreBackup"
)) {
    if ($placementVm -notmatch ('\["' + [regex]::Escape($key) + '"\]')) {
        $failures.Add("PlacementPageViewModel missing purpose-built page seed: $key")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: UI page body marker verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: UI page body marker verification" -ForegroundColor Green

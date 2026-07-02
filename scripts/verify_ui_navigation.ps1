param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

$mainVmPath = Join-Path $RepoRoot "wpf\ViewModels\MainWindowViewModel.cs"
$mainWindowPath = Join-Path $RepoRoot "wpf\MainWindow.xaml.cs"
$mainVm = Get-Content -LiteralPath $mainVmPath -Raw
$mainWindow = Get-Content -LiteralPath $mainWindowPath -Raw

foreach ($token in @("BeginnerNavigationKeys", "AdvancedNavigationKeys", "ApplyExperienceModeFilter", "Expert")) {
    if ($mainVm -notmatch [regex]::Escape($token)) {
        $failures.Add("Navigation model missing token: $token")
    }
}

$beginnerRequired = @(
    "Dashboard","OneClickBoost","AutoGamingMode","AIPerformanceAdvisor","PerformanceBoost",
    "StartupManager","BackgroundApps","Cleanup","Storage","GpuCenter","GamingBooster",
    "HardwareVendorCenter","StreamingCenter","CreatorMode","NetworkBooster","DnsLatencyTools","PrivacyCenter",
    "SecurityHealth","AppsManager","TweaksCenter","WindowsFeatures","UpdateControl",
    "RepairTools","DriverUpdateCenter","AppUninstaller","RestoreBackup","Settings","About"
)

foreach ($key in $beginnerRequired) {
    $pattern = '"' + [regex]::Escape($key) + '"'
    if ($mainVm -notmatch $pattern) {
        $failures.Add("Beginner sidebar missing expected key: $key")
    }
}

if ($mainWindow -notmatch '_viewModel\.ApplyFeatureVisibility\(\);\s*NavigateToPage') {
    $failures.Add("MainWindow should apply saved settings before final feature visibility/navigation.")
}

$navKeyCount = ([regex]::Matches($mainVm, 'Key\s*=\s*"([^"]+)"')).Count
if ($navKeyCount -lt 60) {
    $failures.Add("Expert/source sidebar inventory should keep v1.x parity. Found $navKeyCount keys.")
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: UI navigation verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: UI navigation verification" -ForegroundColor Green

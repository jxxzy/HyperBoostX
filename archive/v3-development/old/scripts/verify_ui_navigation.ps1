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
    "StreamingCenter","CreatorMode","NetworkBooster","DnsLatencyTools","PrivacyCenter",
    "SecurityHealth","AppsManager","TweaksCenter","WindowsFeatures","UpdateControl",
    "RepairTools","DriverUpdateCenter","AppUninstaller","RestoreBackup","Settings","About"
)

foreach ($key in $beginnerRequired) {
    $pattern = '"' + [regex]::Escape($key) + '"'
    if ($mainVm -notmatch $pattern) {
        $failures.Add("Beginner sidebar missing expected key: $key")
    }
}

$beginnerBlock = [regex]::Match($mainVm, 'BeginnerNavigationKeys\s*=\s*new\[\]\s*\{(?<body>.*?)\};', [System.Text.RegularExpressions.RegexOptions]::Singleline).Groups["body"].Value
$beginnerKeys = [regex]::Matches($beginnerBlock, '"([^"]+)"') | ForEach-Object { $_.Groups[1].Value }
if (@($beginnerKeys).Count -ne 27) {
    $failures.Add("Beginner sidebar must expose exactly 27 pages. Found $(@($beginnerKeys).Count).")
}

foreach ($forbidden in @("HardwareVendorCenter", "MsiSafeOptimizer", "NvidiaCopilot", "AICenter", "ReleaseReadiness", "FeatureAudit", "MasterTestEngine")) {
    if ($beginnerKeys -contains $forbidden) {
        $failures.Add("$forbidden must not be a Beginner top-level menu.")
    }
}

$advancedBlock = [regex]::Match($mainVm, 'AdvancedNavigationKeys\s*=\s*BeginnerNavigationKeys\s*\.Concat\(new\[\]\s*\{(?<body>.*?)\}\)', [System.Text.RegularExpressions.RegexOptions]::Singleline).Groups["body"].Value
foreach ($advancedKey in @("HardwareVendorCenter", "MsiSafeOptimizer", "ProcessAnalyzer", "KnowledgeBase")) {
    if ($advancedBlock -notmatch [regex]::Escape('"' + $advancedKey + '"')) {
        $failures.Add("Advanced sidebar missing expected key: $advancedKey")
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

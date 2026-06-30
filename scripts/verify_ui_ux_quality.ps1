param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

function Add-Failure([string]$Message) {
    $script:failures.Add($Message)
}

function Read-RepoFile([string]$RelativePath) {
    Get-Content -LiteralPath (Join-Path $RepoRoot $RelativePath) -Raw
}

$mainVm = Read-RepoFile "wpf/ViewModels/MainWindowViewModel.cs"
$mainWindow = Read-RepoFile "wpf/MainWindow.xaml.cs"
$mainXaml = Read-RepoFile "wpf/MainWindow.xaml"
$settingsXaml = Read-RepoFile "wpf/Views/SettingsView.xaml"
$settingsCode = Read-RepoFile "wpf/Views/SettingsView.xaml.cs"

$navKeys = [regex]::Matches($mainVm, 'Key\s*=\s*"([^"]+)"') | ForEach-Object { $_.Groups[1].Value }
$routeMatches = [regex]::Matches($mainWindow, '_navigationService\.Register\("([^"]+)",\s*\(\)\s*=>\s*new\s+([A-Za-z0-9_]+)\(')
$routes = @{}
foreach ($match in $routeMatches) {
    $routes[$match.Groups[1].Value] = $match.Groups[2].Value
}

$legacyRouteMatches = [regex]::Matches($mainWindow, 'RegisterLegacyRoute\("([^"]+)"')
foreach ($match in $legacyRouteMatches) {
    $routes[$match.Groups[1].Value] = "LegacyFeatureView"
}

if ($navKeys.Count -lt 50) {
    Add-Failure "Sidebar has fewer than 50 expected v1.3 parity entries. Found $($navKeys.Count)."
}

foreach ($key in $navKeys) {
    if (-not $routes.ContainsKey($key)) {
        Add-Failure "Sidebar item '$key' has no registered route."
        continue
    }

    $viewName = $routes[$key]
    $xamlPath = Join-Path $RepoRoot "wpf/Views/$viewName.xaml"
    $codePath = Join-Path $RepoRoot "wpf/Views/$viewName.xaml.cs"
    if (-not (Test-Path -LiteralPath $xamlPath)) {
        Add-Failure "Route '$key' targets missing View XAML: $viewName.xaml"
    }
    if (-not (Test-Path -LiteralPath $codePath)) {
        Add-Failure "Route '$key' targets missing View code-behind: $viewName.xaml.cs"
    }
    if (Test-Path -LiteralPath $xamlPath) {
        $viewRaw = Get-Content -LiteralPath $xamlPath -Raw
        if ($viewRaw.Length -lt 120 -or ($viewRaw -notmatch '<views:CyberPageChrome' -and $viewRaw -notmatch '<Button\b|<ItemsControl\b|<TextBox\b|<CheckBox\b')) {
            Add-Failure "Route '$key' appears empty or non-interactive: $viewName.xaml"
        }
    }
}

if (($navKeys | Group-Object | Where-Object Count -gt 1).Count -gt 0) {
    Add-Failure "Sidebar contains duplicate navigation keys."
}

foreach ($required in @('Dashboard','OneClickBoost','AutoGamingMode','AIPerformanceAdvisor','PerformanceBoost','StartupManager','BackgroundApps','Cleanup','Storage','GpuCenter','GamingBooster','StreamingCenter','CreatorMode','AdvancedMicMixer','WebcamStudio','CameraTracking','NetworkBooster','DnsLatencyTools','PrivacyCenter','SecurityHealth','AppsManager','TweaksCenter','WindowsFeatures','UpdateControl','RepairTools','DriverUpdateCenter','AppUninstaller','AdvancedTweaks','WindowsServices','PowerOptimization','VisualEffects','RestoreBackup','RestorePointManager','ScheduledAutomation','UtilitiesTools','FeatureAudit','MasterTestEngine','Settings','About')) {
    if ($navKeys -notcontains $required) {
        Add-Failure "Required page missing from sidebar: $required"
    }
}

foreach ($requiredGroup in @('Quick Access','Performance','Gaming & Creator','Network','Privacy & Security','App Management','System Config','System Tools','Backup & Restore','Automation','Extra Tools','Settings','About')) {
    $groupPattern = 'Group\s*=\s*"' + [regex]::Escape($requiredGroup) + '"'
    if ($mainVm -notmatch $groupPattern) {
        Add-Failure "Sidebar group missing: $requiredGroup"
    }
}

foreach ($quick in @('QuickSmartScan_Click','QuickSafeBoost_Click','QuickRestore_Click')) {
    if ($mainXaml -notmatch $quick -or $mainWindow -notmatch "void\s+$quick") {
        Add-Failure "Sidebar quick action is not wired: $quick"
    }
}

foreach ($setting in @('Enable Animations','Reduce Motion','Accent Color','Beginner','Advanced','Expert Preview')) {
    if ($settingsXaml -notmatch [regex]::Escape($setting)) {
        Add-Failure "Settings UI missing expected control/text: $setting"
    }
}

foreach ($persist in @('SaveUiSettings','LoadUiSettings','ReduceMotion','AccentColor','Mode')) {
    if ($settingsCode -notmatch $persist -and $mainWindow -notmatch $persist) {
        Add-Failure "Settings persistence missing expected member: $persist"
    }
}

try {
    & (Join-Path $PSScriptRoot "verify_wpf_button_handlers.ps1") -RepoRoot $RepoRoot
    if (-not $?) { Add-Failure "Button handler verification failed." }
}
catch {
    Add-Failure "Button handler verification failed: $($_.Exception.Message)"
}

try {
    & (Join-Path $PSScriptRoot "verify_placeholder_guard.ps1") -RepoRoot $RepoRoot
    if (-not $?) { Add-Failure "Placeholder/fake UI guard failed." }
}
catch {
    Add-Failure "Placeholder/fake UI guard failed: $($_.Exception.Message)"
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: UI/UX quality verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: UI/UX quality verification" -ForegroundColor Green

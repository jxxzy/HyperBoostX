param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

function Add-Failure([string]$Message) {
    $script:failures.Add($Message)
}

$productApi = Get-Content -LiteralPath (Join-Path $RepoRoot "app/api/product_v14.py") -Raw
$boostApi = Get-Content -LiteralPath (Join-Path $RepoRoot "app/api/boost.py") -Raw
$mainWindow = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf/MainWindow.xaml.cs") -Raw

$requiredRoutes = @(
    '/scan/smart',
    '/advisor/plan',
    '/advisor/safe-actions',
    '/games/library',
    '/games/profile/preview',
    '/gpu/status',
    '/processes/background-pressure',
    '/startup/list',
    '/startup/preview',
    '/cleanup/scan',
    '/cleanup/preview',
    '/network/diagnostics',
    '/benchmark/latest',
    '/history/timeline',
    '/streaming/status',
    '/creator/status',
    '/restore/sessions',
    '/protection/processes',
    '/feature-audit/status',
    '/update/check'
)

foreach ($route in $requiredRoutes) {
    if ($productApi -notmatch [regex]::Escape($route)) {
        Add-Failure "Missing pre-v2 compatibility route: $route"
    }
}

foreach ($route in @('/boost/plan','/boost/apply','/boost/undo')) {
    if ($boostApi -notmatch [regex]::Escape($route.Replace('/boost',''))) {
        Add-Failure "Missing safe boost route in app/api/boost.py: $route"
    }
}

$requiredViews = @(
    'DashboardView','AIPerformanceAdvisorView','OneClickBoostView','AutoGamingModeView','GameLibraryView','GameProfilesView',
    'GpuCenterView','HyperBalanceView','ProcessAnalyzerView','StartupManagerView','CleanupView','NetworkToolsView',
    'BenchmarkLabView','PerformanceHistoryView','PerformanceReportView','StreamingCenterView','CreatorModeView',
    'GamingEssentialsView','RestoreBackupView','ProtectedAppsView','KnowledgeBaseView','SettingsView','FeatureAuditView','AboutView'
)

foreach ($view in $requiredViews) {
    if ($mainWindow -notmatch "new\s+$view\(") {
        Add-Failure "Missing registered WPF view: $view"
    }
    if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot "wpf/Views/$view.xaml"))) {
        Add-Failure "Missing WPF View XAML: $view.xaml"
    }
}

$requiredDocs = @(
    'docs/LEGACY_FEATURE_MATRIX.md',
    'docs/REGRESSION_AUDIT_FROM_V1.md',
    'FEATURE_MATRIX.md',
    'QA_RESULTS.md',
    'RELEASE_NOTES_NEXT.md'
)

foreach ($doc in $requiredDocs) {
    if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot $doc))) {
        Add-Failure "Missing audit/status document: $doc"
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: pre-v2 feature preservation verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: pre-v2 feature preservation verification" -ForegroundColor Green

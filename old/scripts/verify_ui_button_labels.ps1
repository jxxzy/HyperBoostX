param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

$actionMapPath = Join-Path $RepoRoot "wpf\Data\ui_action_map_v2_10.json"
$actionMapDocsPath = Join-Path $RepoRoot "docs\UI_ACTION_MAP_v2.10.0.md"
$aboutViewPath = Join-Path $RepoRoot "wpf\Views\AboutView.xaml"
$dashboardPath = Join-Path $RepoRoot "wpf\Views\DashboardView.xaml"
$settingsPath = Join-Path $RepoRoot "wpf\Views\SettingsView.xaml"

foreach ($path in @($actionMapPath, $actionMapDocsPath, $aboutViewPath, $dashboardPath, $settingsPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing expected UI/button artifact: $path")
    }
}

if (Test-Path -LiteralPath $actionMapPath) {
    $jsonText = Get-Content -LiteralPath $actionMapPath -Raw
    foreach ($token in @("Run About App", "Preview About App", "Apply Approved About App", "Restore About App", "Export About App")) {
        if ($jsonText -match [regex]::Escape($token)) {
            $failures.Add("Action map still contains About template label: $token")
        }
    }
    $payload = $jsonText | ConvertFrom-Json
    $about = @($payload.menus | Where-Object { $_.key -eq "About" } | Select-Object -First 1)
    if (-not $about) {
        $failures.Add("Action map missing About menu.")
    } else {
        foreach ($label in @("Open Version Info", "Check Backend Health", "Check for Updates", "Open Latest Release", "Open Release Readiness", "Refresh Backend")) {
            if (-not (@($about.actions).label -contains $label)) {
                $failures.Add("About action map missing final label: $label")
            }
        }
    }
}

if (Test-Path -LiteralPath $actionMapDocsPath) {
    $docs = Get-Content -LiteralPath $actionMapDocsPath -Raw
    foreach ($token in @("Run About App", "Preview About App", "Apply Approved About App", "Restore About App", "Export About App")) {
        if ($docs -match [regex]::Escape($token)) {
            $failures.Add("Action map docs still contain About template label: $token")
        }
    }
}

if (Test-Path -LiteralPath $aboutViewPath) {
    $aboutXaml = Get-Content -LiteralPath $aboutViewPath -Raw
    foreach ($token in @("<views:PlacementPageChrome", "Primary Actions", "Run About", "Preview About", "Apply reviewed selection")) {
        if ($aboutXaml -match [regex]::Escape($token)) {
            $failures.Add("About page still looks like an action template: $token")
        }
    }
}

if (Test-Path -LiteralPath $dashboardPath) {
    $dashboard = Get-Content -LiteralPath $dashboardPath -Raw
    foreach ($label in @("Start Smart Scan", "One Click Safe Boost", "Restore / Undo", "Refresh Snapshot")) {
        if ($dashboard -notmatch [regex]::Escape($label)) {
            $failures.Add("Dashboard missing final button label: $label")
        }
    }
}

if (Test-Path -LiteralPath $settingsPath) {
    $settings = Get-Content -LiteralPath $settingsPath -Raw
    foreach ($label in @("Save Settings", "Reload", "Reset Settings", "Export Settings", "Import Settings", "Open GitHub Release")) {
        if ($settings -notmatch [regex]::Escape($label)) {
            $failures.Add("Settings missing final button label: $label")
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: UI button label verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: UI button label verification" -ForegroundColor Green


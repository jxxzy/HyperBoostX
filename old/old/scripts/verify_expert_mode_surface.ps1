param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]
$mainVm = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\ViewModels\MainWindowViewModel.cs") -Raw
$actionMap = Get-Content -LiteralPath (Join-Path $RepoRoot "wpf\Data\ui_action_map_v2_10.json") -Raw | ConvertFrom-Json

$beginnerBlock = [regex]::Match($mainVm, 'BeginnerNavigationKeys\s*=\s*new\[\]\s*\{(?<body>.*?)\};', [System.Text.RegularExpressions.RegexOptions]::Singleline).Groups["body"].Value
$beginnerCount = ([regex]::Matches($beginnerBlock, '"([^"]+)"')).Count
if ($beginnerCount -ne 27) {
    $failures.Add("Beginner mode must expose exactly 27 pages. Found $beginnerCount.")
}

$navKeyCount = ([regex]::Matches($mainVm, 'Key\s*=\s*"([^"]+)"')).Count
if ($navKeyCount -lt 72) {
    $failures.Add("Expert source surface must expose at least 72 navigation keys. Found $navKeyCount.")
}

if (@($actionMap.menus).Count -lt 72) {
    $failures.Add("Action map must expose at least 72 Expert menus. Found $(@($actionMap.menus).Count).")
}

$groupCount = @([regex]::Matches($mainVm, 'Group\s*=\s*"([^"]+)"') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique).Count
if ($groupCount -lt 15) {
    $failures.Add("Expert sidebar should be grouped into at least 15 groups. Found $groupCount.")
}

foreach ($token in @("SearchText", "ApplySearchFilter", "modeLabel.Contains(`"Expert`"", "return visible.ToList()", "Status = FeatureVisibilityService.GetStatus")) {
    if ($mainVm -notmatch [regex]::Escape($token)) {
        $failures.Add("Expert mode surface missing token: $token")
    }
}

foreach ($forbidden in @("NVIDIA Copilot", "Plugin Marketplace")) {
    if ($mainVm -match [regex]::Escape($forbidden)) {
        $failures.Add("Public Expert surface still exposes forbidden label: $forbidden")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: Expert mode surface verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: Expert mode surface verification" -ForegroundColor Green

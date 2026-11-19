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

$gpuService = Read-RequiredFile "app\services\monitoring\gpu_detection_service.py"
$placementVm = Read-RequiredFile "wpf\ViewModels\PlacementPageViewModel.cs"
$mainVm = Read-RequiredFile "wpf\ViewModels\MainWindowViewModel.cs"

foreach ($token in @("GpuVendor.NVIDIA", "GpuVendor.AMD", "GpuVendor.INTEL", "GpuVendor.UNKNOWN", "Microsoft Basic")) {
    if ($gpuService -notmatch [regex]::Escape($token)) {
        $failures.Add("GPU detection service missing vendor-neutral token: $token")
    }
}

foreach ($token in @("NVIDIA / AMD / Intel / Microsoft Basic", "No silent install", "overclock", "undervolt", "BIOS")) {
    if ($placementVm -notmatch [regex]::Escape($token)) {
        $failures.Add("GPU Center placement seed missing safety/vendor token: $token")
    }
}

$beginnerBlock = [regex]::Match($mainVm, 'BeginnerNavigationKeys\s*=\s*new\[\]\s*\{(?<body>.*?)\};', [System.Text.RegularExpressions.RegexOptions]::Singleline).Groups["body"].Value
foreach ($forbidden in @("NvidiaCopilot", "MsiSafeOptimizer")) {
    if ($beginnerBlock -match [regex]::Escape($forbidden)) {
        $failures.Add("$forbidden must not be a Beginner top-level menu.")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: GPU vendor-neutrality verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: GPU vendor-neutrality verification" -ForegroundColor Green

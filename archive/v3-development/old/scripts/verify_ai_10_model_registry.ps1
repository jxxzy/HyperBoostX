param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]
$servicePath = Join-Path $RepoRoot "wpf\Services\NvidiaCopilotService.cs"

if (-not (Test-Path -LiteralPath $servicePath)) {
    $failures.Add("Missing NVIDIA AI service: $servicePath")
} else {
    $service = Get-Content -LiteralPath $servicePath -Raw
    $modelCount = ([regex]::Matches($service, 'new\s+AiModelInfo\s*\{')).Count
    if ($modelCount -lt 10) {
        $failures.Add("AI model registry must contain at least 10 models. Found $modelCount.")
    }

    foreach ($token in @(
        "nvidia/nemotron-3-nano-30b-a3b",
        "nvidia/nvidia-nemotron-nano-9b-v2",
        "nvidia/nemotron-content-safety-reasoning-4b",
        "nvidia/llama-3.1-nemoguard-8b-content-safety",
        "GetAvailableModels",
        "GetDefaultModel",
        "GetFallbackModel",
        "ValidateModel"
    )) {
        if ($service -notmatch [regex]::Escape($token)) {
            $failures.Add("AI model registry missing token: $token")
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: AI 10-model registry verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: AI 10-model registry verification" -ForegroundColor Green

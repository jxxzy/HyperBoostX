param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]
$servicePath = Join-Path $RepoRoot "wpf\Services\NvidiaCopilotService.cs"
$service = if (Test-Path -LiteralPath $servicePath) { Get-Content -LiteralPath $servicePath -Raw } else { "" }

foreach ($token in @(
    "ValidateModel(request.Model)",
    "request.AutoFallback",
    "request.FallbackModel",
    "GetFallbackModel()",
    "AiProviderFactory.Create",
    "Only NVIDIA AI provider is supported in this build.",
    "ChatEndpoint",
    "chat/completions"
)) {
    if ($service -notmatch [regex]::Escape($token)) {
        $failures.Add("AI model router missing token: $token")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL: AI model router verification" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "- $_" }
    exit 1
}

Write-Host "PASS: AI model router verification" -ForegroundColor Green

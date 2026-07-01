param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "verify_version_sync.ps1") -RepoRoot $RepoRoot
exit $LASTEXITCODE


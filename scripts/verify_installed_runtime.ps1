[CmdletBinding()]
param(
    [string]$RepoRoot = "",
    [string]$ExpectedVersion = "",
    [switch]$LaunchInstalledApp,
    [switch]$StopAfterProbe,
    [int]$ProbeTimeoutSeconds = 35,
    [int]$BackendPort = 5055
)

$ErrorActionPreference = "Stop"

$ScriptDir = if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $ScriptDir "..")).Path
}

$runtimeVerifier = Join-Path $ScriptDir "runtime_verifier.ps1"
if (-not (Test-Path -LiteralPath $runtimeVerifier)) {
    throw "Runtime verifier not found: $runtimeVerifier"
}

$argsForVerifier = @(
    "-NoProfile",
    "-ExecutionPolicy", "Bypass",
    "-File", $runtimeVerifier,
    "-RepoRoot", $RepoRoot,
    "-ProbeTimeoutSeconds", $ProbeTimeoutSeconds,
    "-BackendPort", $BackendPort
)

if (-not [string]::IsNullOrWhiteSpace($ExpectedVersion)) {
    $argsForVerifier += @("-ExpectedVersion", $ExpectedVersion)
}
if ($LaunchInstalledApp) {
    $argsForVerifier += "-LaunchInstalledApp"
}
if ($StopAfterProbe) {
    $argsForVerifier += "-StopAfterProbe"
}

& powershell @argsForVerifier
exit $LASTEXITCODE

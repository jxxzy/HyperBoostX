[CmdletBinding()]
param(
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $scriptDir "..")).Path
}

. (Join-Path $RepoRoot "scripts\lib\HyperBoostXReleaseContract.ps1")

$expectedVersion = (Get-Content -LiteralPath (Join-Path $RepoRoot "VERSION") -Raw).Trim()
$actionMapPath = Join-Path $RepoRoot "wpf\Data\ui_action_map_v2_10.json"
$result = Test-HyperBoostXActionMapContract -ActionMapPath $actionMapPath -ExpectedVersion $expectedVersion -NamePrefix "source action map"

$failed = @($result.checks | Where-Object { -not $_.ok })
if ($failed.Count -gt 0) {
    $failed | Format-Table -AutoSize | Out-String | Write-Host
    throw "Action map contract verification failed."
}

Write-Host "Action map contract PASS."

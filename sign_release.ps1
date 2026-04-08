$ErrorActionPreference = "Stop"

param(
    [string]$Thumbprint = "",
    [string]$PfxPath = "",
    [string]$PfxPassword = "",
    [string]$TimestampUrl = "http://timestamp.digicert.com",
    [string]$Description = "HyperBoost X by MR.4NONY",
    [string]$DescriptionUrl = "https://github.com/jxxzy/HyperBoostX"
)

Set-Location -LiteralPath $PSScriptRoot

$signtoolCandidates = @(
    "C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe",
    "C:\Program Files (x86)\Windows Kits\10\App Certification Kit\signtool.exe",
    "signtool.exe"
)

$signtool = $signtoolCandidates | Where-Object { $_ -eq "signtool.exe" -or (Test-Path $_) } | Select-Object -First 1
if (-not $signtool) {
    throw "signtool.exe not found. Install Windows SDK signing tools first."
}

$targets = @(
    "HyperBoostXInstaller.exe",
    "release\package\launcher\HyperBoostLauncher.exe",
    "release\package\wpf\HyperBoostX.exe",
    "release\package\backend\hyperboost_backend.exe",
    "release\app\HyperBoostX.exe",
    "release\app\runtime\wpf\HyperBoostX.exe",
    "release\app\runtime\backend\hyperboost_backend.exe"
) | Where-Object { Test-Path $_ }

if ($targets.Count -eq 0) {
    throw "No release artifacts found to sign. Build the release first."
}

function Invoke-SignTarget {
    param([string]$FilePath)

    $baseArgs = @("sign", "/fd", "SHA256", "/td", "SHA256", "/tr", $TimestampUrl, "/d", $Description, "/du", $DescriptionUrl)

    if ($Thumbprint) {
        & $signtool @baseArgs /sha1 $Thumbprint $FilePath
        return
    }

    if ($PfxPath) {
        $args = @($baseArgs + @("/f", $PfxPath))
        if ($PfxPassword) {
            $args += @("/p", $PfxPassword)
        }

        & $signtool @args $FilePath
        return
    }

    throw "Provide either -Thumbprint or -PfxPath to sign release artifacts."
}

foreach ($target in $targets) {
    Write-Host "Signing $target"
    Invoke-SignTarget -FilePath $target
}

Write-Host ""
Write-Host "Signing complete for $($targets.Count) file(s)."

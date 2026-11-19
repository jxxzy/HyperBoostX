param(
    [switch]$AllowMissingInstaller
)

$ErrorActionPreference = "Stop"

Write-Host "== Verify HyperBoostX v2.10.0 beta release assets =="

powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify_version_sync.ps1 | Out-Host
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify_release_artifact_contents.ps1 | Out-Host

$installer = "HyperBoostXInstaller.exe"
if (Test-Path -LiteralPath $installer) {
    Get-FileHash -LiteralPath $installer -Algorithm SHA256 | Format-List | Out-Host
} elseif (-not $AllowMissingInstaller) {
    throw "HyperBoostXInstaller.exe missing. Run scripts\package_installer_v2.10.0.ps1 or pass -AllowMissingInstaller for source-only beta verification."
}

Write-Host "Asset verification complete. Stable release still requires signing and installed-runtime lab evidence."

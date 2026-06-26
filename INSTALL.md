# Install HyperBoostX

Target release: `HyperBoostX v1.4.0 Feature Expansion Stable`

## Recommended Install

1. Download `HyperBoostXInstaller.exe` and `SHA256SUMS.txt` from the official GitHub Release.
2. Verify SHA256:

```powershell
Get-FileHash .\HyperBoostXInstaller.exe -Algorithm SHA256
Get-Content .\SHA256SUMS.txt
```

3. Run the installer.
4. Launch HyperBoostX through the installed shortcut or launcher.

## SmartScreen

If the installer is unsigned, Windows may show Unknown Publisher or SmartScreen. This is expected until a signed release is available. Do not install from unofficial mirrors.

## Portable Mode For Development

Set `HYPERBOOSTX_PORTABLE_HOME` to a writable folder before starting the backend to keep config, reports, profiles, sessions, and logs in that folder.

## Uninstall

Use Windows Installed Apps/Programs or the NSIS uninstaller entry. Verify no backend process remains after closing the app.

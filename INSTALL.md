# Install HyperBoostX

Target release: `HyperBoostX v1.3.0 Stable`

## Normal User Install

1. Download `HyperBoostXInstaller.exe` from the GitHub Release.
2. Run the installer.
3. Launch HyperBoostX from the desktop or Start Menu shortcut.

Optional checksum verification:

```powershell
Get-FileHash .\HyperBoostXInstaller.exe -Algorithm SHA256
```

Compare the result with `SHA256SUMS.txt` if it is published.

## Installer Behavior

- Installs the app under `C:\Program Files\HyperBoostX` by default.
- Removes the previous installed application files before installing the new runtime.
- Preserves legacy user config, logs, backups, and automation state under `%LocalAppData%\HyperBoost X`.
- Writes uninstall metadata under `HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\HyperBoostX`.

## Public Asset Policy

Publish for normal users:

- `HyperBoostXInstaller.exe`
- `SHA256SUMS.txt` when checksum verification is included

Do not publish confusing internal artifacts such as raw backend executables, raw launcher executables, debug packages, temp packages, logs, cache folders, or local state.

## Release Validation

Installer install, installed launch, close, silent uninstall, reinstall, and reinstalled launch must be recorded in `QA_RESULTS.md` before a stable GitHub Release is published.

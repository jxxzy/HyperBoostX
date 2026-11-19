# Owner Lab Gate Checklist v2.10.0-beta.1

This checklist is required before promoting beyond `SOURCE_BETA_READY`.

## Elevated Runtime Gates

Run from elevated PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\clean_install_verify.ps1 -Execute
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\runtime_verifier.ps1 -LaunchInstalledApp -StopAfterProbe
```

Required pass conditions:

- Registry `DisplayVersion` equals `2.10.0-beta.1`.
- Installed backend `/api/health` responds.
- Installed backend `/api/version` returns `2.10.0-beta.1`.
- WPF launches from installed path.
- Token sync is verified or strongly inferred from launcher env plus token-required backend health.
- No orphan installed runtime processes remain after stop.
- Desktop and Start Menu shortcuts exist.

## Installer Gates

- Fresh install.
- Reinstall.
- Silent install.
- Silent uninstall.
- Launch after install.
- Uninstall leaves no orphan process.

## Hardware Gates

- Low-end PC profile.
- NVIDIA GPU.
- AMD GPU.
- Intel GPU.
- No GPU detected.
- No admin.
- Admin mode.
- Backend offline.
- Token mismatch.
- Corrupt config.
- Missing reports folder.
- Windows scaling 100%, 125%, 150%.
- Empty game library.
- Many startup items.
- Protected process action blocked.

## Promotion Rule

Only after all gates pass may the owner mark a local lab build as `OWNER_LAB_STABLE_CANDIDATE`. Public stable still requires owner decision on unsigned distribution and release process.


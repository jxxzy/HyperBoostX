# Stable Installed Runtime Fix v2.10.0

Generated: 2026-07-01

## Current Problem

The installed HyperBoostX runtime on this machine still reports registry `DisplayVersion=1.3.0` under Program Files. Source/package validation is beta-ready, but public stable cannot be claimed while the installed runtime is stale.

## Root Cause

The repo build/package artifacts were updated, but the installed Windows runtime was not replaced through an elevated fresh install. The current shell is not Administrator, so it cannot safely uninstall from Program Files/HKLM and reinstall the rebuilt package.

## Fixes Applied In Source

- `HyperBoostXInstaller.nsi` now writes `QuietUninstallString` for silent uninstall/reinstall gates.
- `HyperBoostXInstaller.nsi` now writes publisher metadata as `jxxzy / HyperBoostX`.
- `launcher/Program.cs` now defaults the managed backend to port `5000`, matching the stable runtime gate requirement for:
  - `http://127.0.0.1:5000/api/health`
  - `http://127.0.0.1:5000/api/version`
- `scripts/owner_admin_stable_gate.ps1` was added for the owner to run from an elevated PowerShell.

## Registry Expected After Owner Admin Install

| Registry value | Expected |
| --- | --- |
| `DisplayName` | `HyperBoostX` |
| `DisplayVersion` | Current expected version from `VERSION`; after stable promotion this must be `2.10.0` |
| `Publisher` | `jxxzy / HyperBoostX` |
| `InstallLocation` | `C:\Program Files\HyperBoostX` or the selected install root |
| `UninstallString` | Quoted path to `Uninstall.exe` |
| `QuietUninstallString` | Quoted path to `Uninstall.exe /S` |

## Current Result

Not fixed in the installed system yet. The owner must run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\owner_admin_stable_gate.ps1
```

If the source is still `2.10.0-beta.1`, that run validates the beta runtime replacement. After that pass, stable promotion to `2.10.0` can be applied and the same gate must pass again for stable.


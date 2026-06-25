# HyperBoostX Build Guide

Target version: `1.2.14`

## Prerequisites

- Windows 10 or Windows 11
- .NET SDK 8
- Python runtime used by `app\venv`
- NSIS for installer builds
- Git

## Verify Repository

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify_repo.ps1
```

This runs version sync, Python backend tests, and .NET desktop tests.

## Build Backend

```bat
build_backend.bat
```

Expected output:

- `release\backend\hyperboost_backend.exe`

## Build WPF Client

```bat
build_release.bat
```

Expected output:

- `release\wpf\HyperBoostX.exe`

## Build Launcher

```bat
build_launcher.bat
```

Expected output:

- `release\launcher\HyperBoostX.exe`

## Package Portable Runtime

```bat
package_release.bat
```

Expected output:

- `release\app\HyperBoostX.exe`
- `release\package`

## Build Installer

```bat
build_installer.bat
```

Expected output:

- `HyperBoostXInstaller.exe`

If NSIS is missing, install NSIS and rerun only the installer step after backend, WPF, launcher, and package builds are already green.

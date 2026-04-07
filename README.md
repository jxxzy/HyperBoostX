# HyperBoost X

HyperBoost X is a Windows optimization suite with a native WPF desktop client, a Python backend, and a .NET launcher. The current beta is focused on turning the app into a single control center for performance, cleanup, automation, repair, AI-assisted actions, and recovery.

Current beta version:
- `1.1.0-beta`

Author:
- `MR.4NONY`

## Architecture

HyperBoost X is built from three main parts:

- `wpf` - WPF desktop UI and core client logic
- `app` - Python Flask backend and optimization services
- `launcher` - .NET launcher that boots the backend, waits for health readiness, opens the UI, and shuts the backend down when the UI exits

## Core runtime layout

- Installed app entrypoint: `HyperBoostX.exe`
- Internal UI runtime: `runtime\wpf\HyperBoostUI.exe`
- Internal backend runtime: `runtime\backend\hyperboost_backend.exe`
- User logs: `%LocalAppData%\HyperBoost X\logs`
- App config and state: `%LocalAppData%\HyperBoost X\config`
- Local backups and automation state: `%LocalAppData%\HyperBoost X\backups`

## Major feature areas

- Core dashboard with real-time system monitoring and quick actions
- One Click Boost, Performance Boost, Startup Manager, Cleanup, Storage, and Network modules
- Gaming Booster, Streaming Mode, and Creator Mode
- Privacy Center, Security & Health, Repair Tools, Driver & Update Center
- Tweaks Center, Advanced Tweaks, Windows Features, Windows Services, Power Optimization, and Visual Effects
- Restore & Backup plus Restore Point Manager
- Scheduled Automation with persistent runtime rules and task queue
- AI Assistant (HyperBoostX Copilot) with OpenAI integration, approval flow, safe action routing, and automation creation
- Discord webhook reporting for important errors and crash events
- Multi-language foundation with modular localization packs
- In-app release checker that can detect the latest author build from GitHub
- Installer upgrade flow that removes the old app version while preserving user config/state
- Secure secret persistence for OpenAI and Discord via Windows Credential Manager
- About App donation shortcut via Sociabuzz

## What changed in `1.1.0-beta`

- Reworked most major menu modules into native in-app panels instead of external shortcuts
- Added persistent settings and shared app state across modules
- Separated automation mode from policy profile
- Upgraded Scheduled Automation from summary UI into real task and rule storage
- Added OpenAI-powered Copilot foundation with context-aware suggestions and safe action approval
- Added Discord webhook error reporting with filtering and cooldown
- Added modular localization foundation with `en-US` and `id-ID` packs
- Added in-app app-update checking against the latest GitHub release
- Added automatic secret loading for OpenAI and Discord credentials with reinstall-safe Windows Credential Manager storage
- Updated installer behavior so upgrades remove the previous app version first while preserving `%LocalAppData%\HyperBoost X\...`
- Added About App donation shortcut for Sociabuzz support
- Improved runtime safety around PowerShell execution, API failures, and activity logging
- Synced About App, binary version metadata, and installer metadata to the latest beta build

## Main folders

- `app` - backend API, services, and Python runtime code
- `wpf` - WPF UI, services, localization, and app orchestration
- `launcher` - launcher/entrypoint application
- `release` - packaged runtime outputs
- `tests` - tests and support assets

## Build scripts

- `build_backend.bat` - builds `release\backend\hyperboost_backend.exe`
- `build_release.bat` - publishes the WPF UI into `release\wpf`
- `build_launcher.bat` - publishes the launcher into `release\launcher`
- `package_release.bat` - assembles `release\package` and `release\app`
- `build_installer.bat` - builds `HyperBoostXInstaller.exe`

## Automated testing

- In-app `Feature Audit / Testing` includes:
  - `Mock Mode`
  - `Safe Read-Only`
  - `Live Read-Only`
  - `Unit`, `Integration`, `UI Flow`, `End-to-End`, `Regression`, `Performance`, `Stress`, `Stability`, `Security`, `Compatibility`
  - `Run Full QA Matrix`
- GitHub Actions CI:
  - `.github/workflows/windows-ci.yml`
  - validates backend tests plus WPF and launcher builds on Windows
- Installer/update lab harness:
  - `.github/workflows/windows-e2e-lab.yml`
  - `scripts/test_installer_update_e2e.ps1`
  - intended for a self-hosted Windows lab runner, not a normal user machine

## Development scripts

- `start_backend.bat` - run backend only
- `start_wpf_client.bat` - run WPF client only against a running backend
- `scripts\verify_repo.ps1` - one-command verification for Python backend tests plus .NET desktop tests

Run the unified verification from PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify_repo.ps1
```

Useful flags:

- `-Configuration Release`
- `-SkipPython`
- `-SkipDotnet`
- `-NoRestore`

## Release outputs

- Portable app: `release\app\HyperBoostX.exe`
- Installer: `HyperBoostXInstaller.exe`
- GitHub prerelease: `v1.1.0-beta`

## Documentation

- `API_REFERENCE.md` - API overview
- `DIRECTORY_MAP.md` - current repo map

## Beta status

This build is suitable for internal beta testing and feature validation. It is not yet declared fully public-stable. The highest-value remaining work is end-to-end QA on admin-required flows, restore/update/service operations, installer upgrade paths, and cross-machine runtime validation.

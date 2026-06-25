<div align="center">

<img src="https://capsule-render.vercel.app/api?type=waving&height=230&color=0:020617,35:0891b2,70:16a34a,100:0f172a&text=HyperBoostX&fontSize=58&fontColor=ffffff&animation=fadeIn&fontAlignY=38&desc=Universal%20Windows%20Gaming%20Optimizer%20%7C%20Scan.%20Plan.%20Approve.%20Boost.%20Undo.&descSize=17&descAlignY=58" />

[![Stable Release](https://img.shields.io/badge/Stable-v1.3.0-16a34a?style=for-the-badge&logo=github&logoColor=white)](https://github.com/jxxzy/HyperBoostX/releases/tag/v1.3.0)
[![Windows](https://img.shields.io/badge/Windows-Desktop-0078D4?style=for-the-badge&logo=windows&logoColor=white)](#)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](#)
[![Python](https://img.shields.io/badge/Python-Flask-3776AB?style=for-the-badge&logo=python&logoColor=white)](#)
[![Safety](https://img.shields.io/badge/Safety-Plan%20First%20%2B%20Undo-22c55e?style=for-the-badge&logo=shield&logoColor=white)](#safety-guard)

[![Download Installer](https://img.shields.io/badge/Download-HyperBoostXInstaller.exe-22c55e?style=for-the-badge&logo=github&logoColor=white)](https://github.com/jxxzy/HyperBoostX/releases/download/v1.3.0/HyperBoostXInstaller.exe)
[![Beginner Guide](https://img.shields.io/badge/Beginner%20Guide-USER_GUIDE.md-38bdf8?style=for-the-badge&logo=readme&logoColor=white)](USER_GUIDE.md)
[![Release Notes](https://img.shields.io/badge/Release%20Notes-v1.3.0-111827?style=for-the-badge&logo=readme&logoColor=white)](RELEASE_NOTES_v1.3.0.md)

**Premium Universal Windows Gaming Optimizer for NVIDIA GeForce GTX/RTX, AMD Radeon/RX/Vega, Intel Arc/iGPU, Microsoft Basic Display Adapter, and unknown GPU fallback systems.**

`Scan PC -> Hardware Profile -> Safe Plan -> User Approval -> Guarded Boost -> Before/After Report -> Undo`

</div>

---

# HyperBoostX

HyperBoostX is a Windows performance and optimization app built around safety, explainability, and reversible changes. It combines a WPF desktop client, Python local backend, .NET launcher, NSIS installer, hardware-aware recommendations, AI-assisted planning, restore/undo metadata, GPU Center, before/after reports, and release-gated validation.

HyperBoostX is not an overclocking tool, does not claim guaranteed FPS gains, and does not claim official NVIDIA, AMD, Intel, Microsoft, or hardware-vendor partnership.

## Quick Links

| Need | Open This |
|---|---|
| Install the app | [Download `HyperBoostXInstaller.exe`](https://github.com/jxxzy/HyperBoostX/releases/download/v1.3.0/HyperBoostXInstaller.exe) |
| Learn how to use it | [Beginner User Guide](USER_GUIDE.md) |
| See what changed | [Release Notes v1.3.0](RELEASE_NOTES_v1.3.0.md) |
| Fix common issues | [Troubleshooting](TROUBLESHOOTING.md) |
| Read safety policy | [Security](SECURITY.md) |
| Report a bug | [Bug Report Template](BUG_REPORT_TEMPLATE.md) |
| Check release validation | [QA Results](QA_RESULTS.md) |

## Contents

- [Recommended Download](#recommended-download)
- [Beginner Quick Start](#beginner-quick-start)
- [What HyperBoostX Does](#what-hyperboostx-does)
- [GPU Support](#gpu-support)
- [Safety Guard](#safety-guard)
- [Common Workflows](#common-workflows)
- [Validation Snapshot](#validation-snapshot)
- [Architecture](#architecture)
- [Backend API](#backend-api)
- [Build And Test](#build-and-test)
- [Documentation](#documentation)
- [Known Limitations](#known-limitations)

## Recommended Download

For normal users, download only the installer from the GitHub Release page:

- Recommended: [`HyperBoostXInstaller.exe`](https://github.com/jxxzy/HyperBoostX/releases/download/v1.3.0/HyperBoostXInstaller.exe)
- Optional checksum: `SHA256SUMS.txt`

Do not download raw backend executables, debug folders, cache files, logs, or internal release artifacts unless you are developing or testing the project.

If Windows shows `Unknown Publisher` or SmartScreen, it means the installer may be unsigned. Only continue if the file came from the official HyperBoostX GitHub Release.

Optional checksum verification:

```powershell
Get-FileHash .\HyperBoostXInstaller.exe -Algorithm SHA256
```

Expected SHA256 for v1.3.0:

```text
16024ADF082ACEBA47387A6A32B9C574BBF2FBB722EC3610286494AC95D764A8  HyperBoostXInstaller.exe
```

## Beginner Quick Start

This is the safest path for people who are not familiar with Windows tweaking:

```text
Install -> Open HyperBoostX -> Dashboard -> Restore & Backup -> GPU Center -> Smart Recommendation -> One Click Boost -> Review Plan -> Approve -> Read Report -> Undo if needed
```

Step-by-step:

1. Install with `HyperBoostXInstaller.exe`.
2. Open HyperBoostX from Desktop or Start Menu.
3. Wait until backend/system status is connected.
4. Open `Dashboard` and read CPU, RAM, disk, network, and health scores.
5. Open `Restore & Backup` so you know where undo/recovery lives.
6. Open `GPU Center`, click refresh, and check GPU/overlay/vendor app detection.
7. Open `Smart Recommendation` and read the safe suggestions.
8. Use `One Click Boost` in safe or balanced mode.
9. Review the plan before approving anything.
10. Read the before/after report.
11. If something feels wrong, use `Restore & Backup` before applying more tweaks.

Full beginner manual: [USER_GUIDE.md](USER_GUIDE.md)

Normal users do not need to start the backend manually. Launch HyperBoostX from the installed shortcut so the launcher can start the local backend, generate the local session token, open the WPF app, and clean up the runtime when the app closes.

## What HyperBoostX Does

| Area | What It Helps With |
|---|---|
| Dashboard | CPU, RAM, disk, network, health score, readiness score, activity, recommendation preview |
| One Click Boost | Safe plan-first optimization, user approval, report, undo path |
| GPU Center | NVIDIA/AMD/Intel/Microsoft Basic/unknown GPU detection, vendor badge, overlays, VRAM, driver, profile |
| Smart Recommendation | Context-aware suggestions for cleanup, startup, overlays, network, and gaming readiness |
| Gaming Mode | Safer pre-game preparation without driver hacks or forced service disablement |
| Streaming Mode | OBS/Discord/network-aware preparation without breaking live tools |
| Creator Mode | Editing/rendering-focused recommendations for creator workflows |
| Cleanup | Safe temp/cache cleanup and cleanup report support |
| Startup Manager | Startup cleanliness review and safer startup decisions |
| Network Tools | DNS test, DNS refresh, latency diagnostics, and network recommendations |
| Restore & Backup | Restore metadata, undo path, restore point guidance, and recovery workflow |
| AI Doctor / Copilot | Plan-first AI analysis with Safety Guard and required approval |
| Reports | Before/after performance report and local crash report export with redaction |

HyperBoostX is built for guided use: scan first, explain the plan, ask for approval, apply safe actions, show what happened, and keep recovery visible.

## v1.3.0 Highlights

- Universal GPU detection for NVIDIA, AMD Radeon, Intel Arc/iGPU, Microsoft Basic Display Adapter, and unknown fallback.
- GPU Center backend contract with vendor badge, model/family, VRAM, usage, temperature when available, driver version, overlays, vendor software, safe actions, skipped actions, and blocked risky actions.
- Hardware profile engine with PC Health, Gaming Readiness, Streaming Readiness, and Startup Cleanliness scoring.
- Safe boost flow that creates a plan first, explains risk, requires approval, blocks risky actions, and generates a before/after report.
- Local job queue for longer tasks with progress, stage, log tail, cancel, completion, and error states.
- Local API security with launcher-generated `X-HyperBoostX-Session` token when the packaged launcher starts the backend and WPF client.
- Local crash report export with redaction for API keys, AI keys, tokens, GitHub tokens, usernames, sensitive paths, and future license keys.
- Support docs, FAQ, troubleshooting, roadmap, bug report template, feature request template, and beginner guide.

## GPU Support

HyperBoostX v1.3.0 includes safe detection paths for:

- NVIDIA GeForce GTX
- NVIDIA GeForce RTX
- AMD Radeon RX
- AMD Radeon Vega
- AMD Radeon integrated graphics
- Intel Arc
- Intel Iris Xe
- Intel UHD / iGPU
- Microsoft Basic Display Adapter
- Unknown GPU fallback

GPU telemetry depends on Windows, drivers, WMI, and hardware counters. If temperature, VRAM usage, driver details, or active display data are unavailable, HyperBoostX should fall back safely instead of crashing.

## Safety Guard

HyperBoostX blocks or refuses unsafe behavior including:

- forced Defender disable
- permanent Windows Update disable
- GPU driver service disablement without a clear safe approval path
- BIOS/UEFI edits
- overclock, undervolt, or voltage changes
- system-file deletion
- user-data deletion
- irreversible registry edits without restore metadata
- AI-generated system actions without user approval

AI and automation must generate a plan first, explain risk, require user approval, respect Safety Guard, and preserve undo/restore metadata where applicable.

## Safe Beginner Rules

For regular users:

- Start with `Dashboard`, `GPU Center`, `Smart Recommendation`, and `One Click Boost`.
- Use safe or balanced mode before advanced modes.
- Read the action plan before clicking approve.
- Keep Safety Guard and Require Approval enabled.
- Use `Restore & Backup` before applying bigger changes.
- Do not use `Advanced Tweaks` or `Windows Services` unless you understand the risk.
- Do not disable Defender, Windows Update, GPU drivers, audio drivers, network services, or antivirus services.
- Do not expect guaranteed FPS gains; results depend on hardware, Windows state, drivers, background apps, games, and network conditions.

Recommended first-week flow for beginners:

```text
Day 1: Dashboard -> GPU Center -> Smart Recommendation -> One Click Boost safe -> Read report
Day 2+: Dashboard -> Startup Manager if boot is slow -> Cleanup safe if disk is full -> Gaming Mode before playing -> Restore & Backup if needed
```

## Feature Map

| Menu | Beginner Use | Risk Note |
|---|---|---|
| `Dashboard` | First screen to inspect PC condition | Safe read-only overview |
| `GPU Center` | Refresh GPU/vendor/overlay status | Do not disable GPU driver services |
| `Smart Recommendation` | Let HyperBoostX suggest safe next steps | Review before applying |
| `One Click Boost` | Run safe/balanced boost with approval | Read plan and report |
| `Cleanup` | Remove safe temp/cache files | Avoid deleting personal files |
| `Startup Manager` | Reduce startup clutter | Do not disable driver/security tools |
| `Background Apps` | Find heavy apps | Avoid force-killing system apps |
| `Gaming Mode` | Prepare before gaming | Do not pause tools you need while playing |
| `Streaming Mode` | Prepare OBS/Discord/live workflows | Do not pause streaming tools mid-live |
| `Creator Mode` | Prepare editing/rendering workflows | Save projects before cleanup |
| `Network Booster` | DNS and latency diagnostics | Does not guarantee lower ping |
| `Repair Tools` | SFC/DISM/network repair | Can take time and may need admin |
| `Restore & Backup` | Undo/recovery path | Use this first if anything feels wrong |
| `Settings` | Theme, safety, AI, app config | Keep Safety Guard enabled |
| `Advanced Tweaks` | Power-user controls | Not recommended for beginners |
| `Windows Services` | Service review | Do not disable services blindly |

## Common Workflows

### Before Gaming

```text
Dashboard -> GPU Center -> Smart Recommendation -> One Click Boost safe/balanced -> Gaming Mode -> Launch game
```

### After Gaming

```text
Dashboard -> Read report -> Keep changes if normal -> Restore & Backup if something feels wrong
```

### Streaming

```text
Open OBS/Discord -> Streaming Mode -> Network diagnostics -> Keep streaming tools active -> Start stream
```

### Slow Startup

```text
Dashboard -> Startup Manager -> Disable clearly unnecessary startup apps -> Cleanup safe -> Restart -> Dashboard
```

### High Ping Or Network Issues

```text
Dashboard -> Network Booster -> DNS & Latency Tools -> Flush DNS if recommended -> Retest
```

### Disk Almost Full

```text
Storage -> Cleanup safe -> Review Recycle Bin -> Delete only personal files you recognize
```

## Current Stable Target

| Item | Value |
|---|---|
| Version | `1.3.0` |
| Tag | `v1.3.0` |
| Release name | `HyperBoostX v1.3.0 Stable` |
| Public asset | `HyperBoostXInstaller.exe` |
| Optional asset | `SHA256SUMS.txt` |
| Branch | `main` |

Full multi-machine Windows lab compatibility is not claimed unless recorded in `QA_RESULTS.md`.

## Validation Snapshot

The v1.3.0 stable release was validated with:

- `scripts\verify_repo.ps1`: PASS
- Python tests: PASS, 43 tests
- .NET tests: PASS, 28 tests
- WPF Debug/Release builds: PASS
- Launcher Release build: PASS
- Backend PyInstaller package: PASS
- NSIS installer build: PASS
- Packaged backend health: PASS, version `1.3.0`
- Portable launch smoke: PASS
- Installer install/uninstall/reinstall smoke: PASS
- Secret scan: PASS
- SHA256 verification: PASS

See [QA_RESULTS.md](QA_RESULTS.md) for details and limitations.

## Architecture

```mermaid
flowchart LR
    User[User] --> Launcher[.NET Launcher]
    Launcher --> Backend[Python Backend on 127.0.0.1]
    Launcher --> WPF[WPF Desktop App]
    WPF <--> Backend
    WPF --> Config[Local Config]
    WPF --> Creds[Windows Credential Manager]
    WPF --> Restore[Backups / Restore Metadata]
```

| Component | Path | Purpose |
|---|---|---|
| Desktop UI | `wpf` | Native WPF interface, settings, update checks, audit flows, and feature pages |
| Backend | `app` | Flask API, monitoring, GPU detection, reports, optimization services |
| Launcher | `launcher` | Starts backend, injects local session token, launches WPF, and cleans up lifecycle |
| Tests | `tests`, `dotnet-tests` | Python and .NET validation suites |
| Installer | `HyperBoostXInstaller.nsi` | NSIS installer and uninstall metadata |

Legacy user data is preserved under `%LocalAppData%\HyperBoost X` for compatibility with previous stable installs.

## Backend API

Base URL: `http://127.0.0.1:5000`

The backend is local-only for the desktop app. It is not a public cloud API and should not be exposed to the network.

Important v1.3.0 endpoints:

- `GET /api/health`
- `GET /api/version`
- `GET /api/system/stats`
- `GET /api/system/info`
- `GET /api/system/startup`
- `GET /api/system/processes`
- `GET /api/hardware/profile`
- `GET /api/hardware/gpu`
- `GET /api/hardware/vendors`
- `GET /api/hardware/overlays`
- `POST /api/boost/plan`
- `POST /api/boost/apply`
- `POST /api/boost/undo`
- `GET /api/reports/latest`
- `POST /api/reports/export`
- `POST /api/reports/crash-export`
- `POST /api/jobs/start`
- `GET /api/jobs/{id}`
- `POST /api/jobs/{id}/cancel`

Mutating endpoints require `X-HyperBoostX-Session` when the packaged launcher supplies a session token.

## Build And Test

For developers:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify_repo.ps1
app\venv\Scripts\python.exe -m pytest -q tests
dotnet test dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj -c Debug
dotnet build wpf\HyperBoostX.csproj -c Release
dotnet build launcher\HyperBoostLauncher.csproj -c Release
```

Release scripts:

- `build_backend.bat`
- `build_release.bat`
- `build_launcher.bat`
- `package_release.bat`
- `build_installer.bat`

Normal users should not run these commands. They are for maintainers, contributors, and release validation.

## Documentation

For users:

- [USER_GUIDE.md](USER_GUIDE.md) - beginner-friendly complete user guide
- [FAQ.md](FAQ.md) - common user questions
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - backend, install, GPU, restore, crash report help
- [SUPPORT.md](SUPPORT.md) - support workflow and bug report format

Release and engineering docs:

- [RELEASE.md](RELEASE.md)
- [RELEASE_NOTES_v1.3.0.md](RELEASE_NOTES_v1.3.0.md)
- [QA_RESULTS.md](QA_RESULTS.md)
- [AUDIT_REPORT.md](AUDIT_REPORT.md)
- [BUGS_FOUND.md](BUGS_FOUND.md)
- [BUGS_FIXED.md](BUGS_FIXED.md)
- [SECURITY.md](SECURITY.md)
- [ROADMAP.md](ROADMAP.md)
- [docs/API_REFERENCE.md](docs/API_REFERENCE.md)

## Known Limitations

- Automated validation in this workspace does not equal full multi-machine Windows lab certification.
- GPU temperature, VRAM usage, and driver metadata depend on Windows/WMI/driver support and may fall back to `Unknown`.
- AI cloud features require user-supplied credentials stored through Windows Credential Manager.
- If the installer is unsigned, Windows may show Unknown Publisher or SmartScreen until code signing is available.
- Auto updater, website, opt-in telemetry, and license activation are roadmap-only items.
- No v1.3.0 feature is locked behind a license.

## Credits

Created by `MR.4NONY - HYPERINDO CYBER TEAM`.

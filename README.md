# HyperBoostX

**HyperBoostX** is a Windows optimization suite built for gaming PCs, creators, streamers, and power users who want safer performance tuning with backup, restore, real-time monitoring, and AI-assisted recommendations.

> **AI PC Performance Doctor**  
> `Scan. Analyze. Boost. Revert.`

---

## Current Stable Version

**Version:** `1.2.13`  
**Release target:** `v1.2.13`  
**Author:** `MR.4NONY - HYPERINDO CYBER TEAM`

`v1.2.13` is the current validated stable release line after release-gate checks, backend validation, installer QA, secret handling checks, and real NVIDIA API gate validation passed on the current test machine.

Public release output uses one installer download:

```text
HyperBoostXInstaller.exe
```

---

## Product Direction

HyperBoostX is designed as an **AI PC Performance Doctor**, not an unsafe extreme tweak tool.

The main optimization flow is:

```text
Scan PC
   ↓
AI Analyzer
   ↓
AI Safety Guard
   ↓
AI Assistant
   ↓
User Approval
   ↓
Safe Tweak Engine
   ↓
Backup / Revert
   ↓
Performance Report
```

### Branding Guardrail

HyperBoostX may use NVIDIA-aware tuning language only where supported, such as RTX, DLSS, Reflex, Frame Generation, or NVIDIA settings guidance.

Do **not** claim:

- `Powered by NVIDIA`
- `Official NVIDIA Partner`
- `NVIDIA Certified`

unless a formal partnership exists.

---

## Architecture

HyperBoostX is built from three main components:

| Component | Description |
|---|---|
| `wpf` | Native WPF desktop UI and core client logic |
| `app` | Python Flask backend and optimization services |
| `launcher` | .NET launcher that starts the backend, waits for health readiness, opens the UI, and shuts the backend down when the UI exits |

---

## Runtime Layout

### Installed App

```text
HyperBoostX.exe
```

### Internal Runtime

```text
runtime\wpf\HyperBoostX.exe
runtime\backend\hyperboost_backend.exe
```

### User Data

```text
%LocalAppData%\HyperBoost X\logs
%LocalAppData%\HyperBoost X\config
%LocalAppData%\HyperBoost X\backups
```

---

## Main Features

### Core Dashboard

- Real-time system monitoring
- Quick performance actions
- Device-aware status cards
- Smart recommendations
- Activity and health overview

### Optimization Modules

- One Click Boost
- Performance Boost
- Gaming Booster
- Streaming Mode
- Creator Mode
- Startup Manager
- Cleanup
- Storage Optimization
- Network Optimization
- Power Optimization
- Visual Effects
- Windows Services
- Windows Features
- Tweaks Center
- Advanced Tweaks

### System Repair & Safety

- Repair Tools
- Security & Health
- Privacy Center
- Restore & Backup
- Restore Point Manager
- Backup and revert flow before risky changes

### Automation

- Scheduled Automation
- Persistent runtime rules
- Task queue
- Safe action routing
- Automation creation through AI approval flow

### AI Features

- HyperBoostX Copilot
- AI Assistant
- AI Analyzer
- AI Safety Guard
- Local RAG-style knowledge base
- NVIDIA Copilot integration where configured
- Safe recommendation flow with user approval

### Integration

- Discord webhook reporting for important errors and crash events
- In-app release checker from GitHub
- Multi-language foundation with modular localization packs
- About App donation shortcut via Sociabuzz

---

## HyperBoostX Triple AI Engine

The **Triple AI Engine** is the intelligence layer behind HyperBoostX.

It consists of three main roles:

### 1. AI Assistant

Explains scan results, bottlenecks, FPS-drop causes, NVIDIA settings, game settings, and safe optimization actions in user-friendly language.

### 2. AI Analyzer

Ranks structured findings from:

- PC scan data
- Windows state
- Game-related settings
- NVIDIA-related settings
- Tweak database
- Performance rules

### 3. AI Safety Guard

Blocks unsafe or irreversible actions.

The Safety Guard prevents:

- Forced overclocking
- Undervolting
- BIOS or UEFI modification
- Voltage tuning
- Disabling Windows Security
- Irreversible registry edits
- Unsafe service removal
- Guaranteed FPS claims

### Local Knowledge Base

HyperBoostX also includes a local grounding layer for:

- Tweak policy
- Game settings
- NVIDIA settings
- Windows errors
- Benchmark notes
- Safe optimization guidance

This knowledge base supports the AI flow, but it is not presented as a fourth AI role.

### Offline Safety

Cloud AI is optional.

The following features continue to work without an AI API key:

- Basic scan
- Local rules
- Safety Guard validation
- Reports
- Backup and revert flow

---

## Main Folders

```text
app         Backend API, services, and Python runtime code
wpf         WPF UI, services, localization, and app orchestration
launcher    Launcher / entrypoint application
release     Packaged runtime outputs
tests       Tests and support assets
docs        Documentation, release notes, and release gates
scripts     Build, deploy, validation, and QA scripts
```

---

## Build Scripts

| Script | Purpose |
|---|---|
| `build_backend.bat` | Builds `release\backend\hyperboost_backend.exe` |
| `build_release.bat` | Publishes the WPF UI into `release\wpf` |
| `build_launcher.bat` | Publishes the launcher into `release\launcher` |
| `package_release.bat` | Assembles `release\package` and `release\app` |
| `build_installer.bat` | Builds `HyperBoostXInstaller.exe` |
| `scripts\build_release_local.ps1` | Builds isolated local-deploy artifacts without touching locked release folders |
| `scripts\deploy_local_runtime.ps1` | Copies the latest runtime into an installed HyperBoostX directory for local QA |

---

## Development Scripts

Run backend only:

```bat
start_backend.bat
```

Run WPF client only against a running backend:

```bat
start_wpf_client.bat
```

Run unified repository verification:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify_repo.ps1
```

Useful flags:

```powershell
-Configuration Release
-SkipPython
-SkipDotnet
-NoRestore
```

---

## Automated Testing

### In-App Testing

HyperBoostX includes an in-app **Feature Audit / Testing** area with:

- Mock Mode
- Safe Read-Only
- Live Read-Only
- Unit Tests
- Integration Tests
- UI Flow Tests
- End-to-End Tests
- Regression Tests
- Performance Tests
- Stress Tests
- Stability Tests
- Security Tests
- Compatibility Tests
- Full QA Matrix

### GitHub Actions CI

```text
.github/workflows/windows-ci.yml
```

Validates:

- Backend tests
- WPF build
- Launcher build

### Installer / Update Lab Harness

```text
.github/workflows/windows-e2e-lab.yml
scripts/test_installer_update_e2e.ps1
```

This is intended for a self-hosted Windows lab runner, not a normal user machine.

---

## Release Outputs

### Public Release

```text
HyperBoostXInstaller.exe
```

### Internal QA Runtime

```text
release\app\HyperBoostX.exe
```

### Internal Build Artifacts

Backend and QA artifacts stay local or in CI artifacts. They are not published as separate public release downloads.

---

## Documentation

| File | Description |
|---|---|
| `docs/API_REFERENCE.md` | API overview |
| `DIRECTORY_MAP.md` | Current repository map |
| `BUILD.md` | Build commands and expected outputs |
| `INSTALL.md` | Portable, installer, uninstall, and config-preservation notes |
| `SECURITY.md` | Local API, credential, redaction, AI safety, and restore policy |
| `USER_GUIDE.md` | Dashboard, boost, restore, and NVIDIA Copilot usage |
| `RELEASE.md` | Release gates, checksum, installer, and GitHub release process |
| `AUDIT_REPORT.md` | Current audit summary |
| `BUGS_FOUND.md` | Known bugs and findings |
| `BUGS_FIXED.md` | Fixed bugs and completed repairs |
| `QA_RESULTS.md` | QA validation results |
| `RELEASE_NOTES_NEXT.md` | Next release notes |
| `docs/release-notes/` | Archived historical release notes |
| `docs/release-gates/` | Archived historical release gates |

---

## Latest Stable Changes

### Version `1.2.13`

- Stable release gate line validated.
- Current-machine automated validation passed.
- Restore metadata validation passed.
- Backend health validation passed.
- Secure NVIDIA and Discord credential handling verified.
- Installer install, uninstall, and reinstall flow passed.
- Real NVIDIA API gate validation passed.
- Public release simplified to one installer download.

### Version `1.2.0`

- Added adaptive optimization foundation.
- Added system-drive classification.
- Added device profile detection.
- Added bottleneck hints.
- Improved dashboard and Smart Recommendation messaging.
- Clarified updater readiness states.
- Cleared validated `v1.2.0` release candidate gates.

### Version `1.1.x`

- Improved NVIDIA Copilot diagnostics.
- Improved app update detection.
- Added stable tags and version badges.
- Reduced dashboard refresh latency.
- Reduced UI freeze risk.
- Improved audit reliability.
- Added Discord release notifications.
- Added localization foundation.
- Added installer upgrade flow.
- Added secure secret loading through Windows Credential Manager.

Older changelogs are archived in:

```text
docs/release-notes/
docs/release-gates/
```

---

## Release Status

```text
Version: v1.2.13
Status: Stable
Installer: HyperBoostXInstaller.exe
Validation: Current-machine release gate passed
Public download: Single installer release
```

### Validation Summary

Passed:

- Backend health check
- Restore metadata validation
- Secret handling validation
- Installer silent install
- App launch after install
- Backend health after install
- App close and orphan-process check
- Silent uninstall
- Reinstall validation
- Real NVIDIA API automated gate
- SHA256 checksum verification
- Repository verification
- Python tests
- .NET tests

Not claimed:

- Full multi-machine Windows lab matrix
- Official NVIDIA partnership
- Guaranteed FPS improvement

---

## Safety Policy

HyperBoostX focuses on safe, reversible optimization.

The app should always prefer:

- Scan before action
- Explanation before execution
- User approval before tweak
- Backup before risky change
- Revert path after optimization
- Clear report after action

HyperBoostX must not promise guaranteed FPS increases or perform unsafe system changes without protection.

---

## License

License information should be added here if the project is public.

```text
TODO: Add license information.
```

---

## Credits

Created by:

```text
MR.4NONY - HYPERINDO CYBER TEAM
```

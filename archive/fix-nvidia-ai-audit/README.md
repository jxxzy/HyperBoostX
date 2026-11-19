# HyperBoost X

HyperBoost X is a Windows optimization suite with a native WPF desktop client, a Python backend, and a .NET launcher. The current line is treated as an internal beta / release candidate until the safety, restore, API-token, and release-gate checks are complete.

Product direction:
- HyperBoostX Triple AI Engine: `Scan. Analyze. Boost. Revert.`
- Positioning: AI PC Performance Doctor for Gaming PCs, with NVIDIA RTX-aware tuning language only where supported.
- Branding guardrail: do not claim `Powered by NVIDIA`, `Official NVIDIA Partner`, or `NVIDIA Certified` unless a formal partnership exists.

Current version:
- `1.2.12`

Planning:
- See [RELEASE_BLUEPRINT.md](RELEASE_BLUEPRINT.md) for the consolidated release plan.

Author:
- `MR.4NONY - HYPERINDO CYBER TEAM`

## Architecture

HyperBoost X is built from three main parts:

- `wpf` - WPF desktop UI and core client logic
- `app` - Python Flask backend and optimization services
- `launcher` - .NET launcher that boots the backend, waits for health readiness, opens the UI, and shuts the backend down when the UI exits

## Core runtime layout

- Installed app entrypoint: `HyperBoostX.exe`
- Internal UI runtime: `runtime\wpf\HyperBoostX.exe`
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
- AI Assistant (HyperBoostX Copilot) with NVIDIA Copilot integration, approval flow, safe action routing, and automation creation
- HyperBoostX Triple AI Engine: AI Assistant, AI Analyzer, AI Safety Guard, and a local RAG-style knowledge base for safe PC performance recommendations
- Discord webhook reporting for important errors and crash events
- Multi-language foundation with modular localization packs
- In-app release checker that can detect the latest author build from GitHub
- Installer upgrade flow that removes the old app version while preserving user config/state
- Secure secret persistence for NVIDIA and Discord via Windows Credential Manager
- About App donation shortcut via Sociabuzz

## HyperBoostX Triple AI Engine

The Triple AI Engine treats HyperBoost X as an AI PC Performance Doctor, not an extreme tweak tool. The required flow is:

`Scan PC -> AI Analyzer -> AI Safety Guard -> AI Assistant -> User Approval -> Safe Tweak Engine -> Backup/Revert -> Performance Report`

Core roles:

- AI Assistant: explains scan results, bottlenecks, FPS-drop causes, DLSS/Reflex/V-Sync/Frame Generation guidance, and safe actions in user-friendly language.
- AI Analyzer: ranks structured findings from scan data, game/NVIDIA knowledge, Windows state, and the tweak database.
- AI Safety Guard: blocks unsafe tweaks, requires backup/restore paths, and prevents overclock, undervolt, Windows Security disable, BIOS/UEFI, voltage, irreversible registry edits, and guaranteed FPS claims.
- RAG/Knowledge Base: local grounding layer for tweak policy, game settings, NVIDIA settings, Windows errors, and benchmark notes. It is not presented as a fourth AI role.

Cloud AI is optional. Basic scan, local rules, Safety Guard validation, and reports continue to work without an AI API key.

## What changed in `1.2.0`
- Added the first adaptive optimization foundation: system-drive classification, device profile detection, bottleneck hints, and more device-aware dashboard and Smart Recommendation messaging.
- Clarified updater readiness states so the app can distinguish release-page-only, blocked, manual-ready, and auto-installable update paths.
- Cleared the in-app `Feature Audit Full` and `Full QA Matrix` gates on the validated `v1.2.0` candidate before release packaging.

## What changed in `1.1.9`
- Fixed the remaining full feature audit failure where `Utilities Workflow Actions` could be marked failed because workflow output was overwritten by history text.

## What changed in `1.1.8`
- Split `Gaming Mode` and `Gaming Booster` into distinct menu destinations so one focuses on profiles/session behavior and the other on instant optimization actions.
- Included the warning-classification hotfix so admin-required gaming tweaks no longer surface as false error alerts in newer builds.

## What changed in `1.1.6`
- Fixed structured log severity detection so backend `WARNING` lines do not get escalated into false error alerts.
- Clarified Gaming Mode partial-success messaging when a tweak needs Administrator privileges.

## What changed in `1.1.5`

- Reduced backend `/api/health` spam by caching health checks inside the WPF client
- Normalized backend JSON handling in the WPF client for automation, startup, process, cleanup, and network paths
- Switched the WPF release layout from single-file publish to a full runtime folder to reduce exit-time native teardown and missing-dependency failures
- Updated installer and package assembly so the full WPF runtime folder is shipped correctly
- Synced installer, launcher, backend, and update metadata to `1.1.5`

## What changed in `1.1.4`

- Hardened `Feature Audit` for `Network Booster` and `Storage` so transient adapter/drive access issues no longer fail the whole audit run
- Improved fallback text inside audit snapshots so partial runtime issues are reported as degraded data instead of generic false failures
- Synced installer, launcher, backend, and update metadata to `1.1.4`

## What changed in `1.1.3`

- Fixed Feature Audit so only incidents from the current audit run affect current results, reducing stale false failures
- Improved NVIDIA Copilot diagnostics with clearer quota/auth messages, endpoint labeling, and request-id support when available
- Fixed app update notifications so already-updated builds no longer report a false newer version because of version label formatting
- Synced installer, launcher, backend, and update metadata to `1.1.3`

## What changed in `1.1.2`

- Fixed HyperBoostX Copilot connectivity by making the NVIDIA chat-completions request path more resilient and improving response parsing/fallback behavior
- Added visible `Last Test` status for `Test AI Connection`, including persisted result across restart
- Extended `Feature Audit` so real runtime feature errors are tracked more accurately without stale false failures sticking after recovery
- Synced installer, launcher, backend, and update metadata to `1.1.2`

## What changed in `1.1.1`

- Refreshed About App copy so the desktop UI clearly marks this line as a stable release
- Added visible stable tags and version badges in the About App page
- Synced installer, launcher, backend, and update metadata to `1.1.1`

## What changed in `1.1.0`

- Promoted the latest validated `1.1.0` line from prerelease to stable
- Reached `56/56` passing checks in the in-app Full QA Matrix
- Hardened startup/stats audit paths and reduced dashboard refresh latency
- Added Discord release notifications from both the app and GitHub releases
- Removed the compatibility-summary dependency on `System.Security.Principal.Windows`
- Reduced UI freeze risk when leaving Feature Audit or switching between heavier pages

## What changed in `1.1.0-beta.5`

- Added Discord notifications when the app detects a newer release during update checks
- Added a GitHub Actions workflow that posts to Discord when a GitHub release is published
- Intended as a small validation prerelease for the GitHub-to-Discord update notification flow

## What changed in `1.1.0-beta`

- Reworked most major menu modules into native in-app panels instead of external shortcuts
- Added persistent settings and shared app state across modules
- Separated automation mode from policy profile
- Upgraded Scheduled Automation from summary UI into real task and rule storage
- Added NVIDIA Copilot foundation with context-aware suggestions and safe action approval
- Added Discord webhook error reporting with filtering and cooldown
- Added modular localization foundation with `en-US` and `id-ID` packs
- Added in-app app-update checking against the latest GitHub release
- Added automatic secret loading for NVIDIA and Discord credentials with reinstall-safe Windows Credential Manager storage
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
- `scripts\build_release_local.ps1` - builds isolated local-deploy artifacts without touching locked `release\...` folders
- `scripts\deploy_local_runtime.ps1` - copies the latest built runtime into an installed `HyperBoost X` directory for local QA

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
- GitHub release target: `v1.2.12`

## Documentation

- `API_REFERENCE.md` - API overview
- `DIRECTORY_MAP.md` - current repo map
- `BUILD.md` - build commands and expected outputs
- `INSTALL.md` - portable, installer, uninstall, and config-preservation notes
- `SECURITY.md` - local API, credential, redaction, AI safety, and restore policy
- `USER_GUIDE.md` - dashboard, boost, restore, and NVIDIA Copilot usage
- `RELEASE.md` - release gates, checksum, installer, and GitHub release process
- `AUDIT_REPORT.md`, `BUGS_FOUND.md`, `BUGS_FIXED.md`, `QA_RESULTS.md`, `RELEASE_NOTES_NEXT.md` - current audit and release evidence

## Release status

`v1.2.12` is an internal beta / RC line. Do not claim final public stable until the release checklist records passing restore, secret, backend-token, installer, and Windows lab evidence.



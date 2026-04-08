# Changelog

All notable changes to HyperBoostX are documented here.

## v1.1.0-beta.3 - 2026-04-08

### Added
- `tests\test_startup_api.py` to lock the backend startup payload contract for both `items` and `startup_items`.
- `dotnet-tests\HyperBoostX.Tests\FeatureAuditRegressionTests.cs` to cover the critical audit-path suites from the desktop side.
- `release-notes-v1.1.0-beta.3.txt` for the new prerelease asset notes.

### Changed
- Updated runtime/app metadata from `1.1.0-beta.2` to `1.1.0-beta.3`.
- Returned both legacy and current startup payload keys from `/api/startup/list` to keep WPF and audit flows aligned.
- Hardened WPF stats/token parsing to avoid `dynamic` JSON crashes in Smart Recommendation, automation snapshots, and security health views.
- Reduced dashboard and smart-scan latency by caching junk estimation and parallelizing lightweight backend reads.
- Updated monitoring stats collection to use the active Windows system drive and short-lived GPU/temperature caching during refresh bursts.
- Added safer storage refresh fallback behavior so storage-page failures degrade gracefully instead of crashing the app.

### Validation
- `powershell -ExecutionPolicy Bypass -File .\scripts\verify_repo.ps1 -Configuration Debug`
- `dotnet build wpf\HyperBoostX.csproj`
- `powershell -ExecutionPolicy Bypass -File .\scripts\build_release_local.ps1 -OutputRoot '.\artifacts\local-deploy-20260408-3'`

### Notes
- This prerelease is intended to replace `v1.1.0-beta.2` for runtime validation and cleaner audit passes.

## v1.1.0-beta - 2026-04-07

### Added
- HyperBoostX Copilot foundation with OpenAI integration, safe action approval, session memory, reasoning summary, and automation creation flow.
- Discord webhook reporting for important errors and crash events.
- Modular localization foundation with `en-US` and `id-ID` language packs.
- Persistent app configuration shared across settings, automation, AI, and recovery-related modules.
- In-app release checker for detecting newer author builds from GitHub.
- Secure OpenAI API key and Discord webhook persistence via Windows Credential Manager.
- Sociabuzz donation shortcut in About App.
- `release-notes-v1.1.0-beta.txt` for beta release documentation.

### Changed
- Reworked major menu modules into native in-app panels instead of mostly external shortcuts.
- Separated automation runtime mode from policy profile.
- Upgraded Scheduled Automation from summary UI to persistent rules, queue, deferred tasks, retry state, and audit tracking.
- Updated About App, binary metadata, and installer metadata to `1.1.0-beta`.
- Synced installer publisher/author metadata to `MR.4NONY`.
- Updated the installer upgrade flow to remove the previous app version first while preserving user config, logs, backups, and runtime state in `%LocalAppData%\HyperBoost X`.
- Improved runtime safety for PowerShell execution, backend API failure handling, and activity logging.
- Refreshed README to document the beta architecture and current feature scope.

### Notes
- This build is intended for internal beta testing and feature validation.
- Public stable release should still be preceded by broader end-to-end QA on admin-required flows, restore/update paths, and installer upgrade behavior.

## v1.1.0-beta.2 - 2026-04-08

### Added
- `scripts\deploy_local_runtime.ps1` to copy the latest packaged runtime into an installed `HyperBoost X` directory for local validation.
- `tests\test_registry_util.py` to cover registry key creation behavior in the backend utility layer.

### Changed
- Updated runtime/app metadata from `1.1.0-beta` to `1.1.0-beta.2`.
- Improved gaming booster feedback so partial success responses include per-setting reasons and clearer admin-required guidance.
- Hardened registry writes by creating missing keys when possible and distinguishing access-denied vs unavailable-path failures.
- Moved gaming visual-effects and Xbox overlay tweaks to the correct current-user registry hive.
- Fixed Smart Recommendation and AI system-context parsing in the WPF client by switching critical audit paths away from fragile `dynamic` token access.
- Tightened automation snapshot parsing to reduce JObject/JToken mismatch errors in diagnostics.

### Validation
- `dotnet build wpf\HyperBoostX.csproj -c Release`
- `app\venv\Scripts\python.exe -m pytest tests\test_booster_service.py tests\test_registry_util.py tests\test_tweak_contract.py`

### Notes
- This prerelease is intended to replace older installed beta runtimes during local QA before the next broader public beta pass.

## v1.0.2 - 2026-04-07

### Changed
- Expanded Game Mode into a fuller gaming control panel with presets, manual mode, whitelist handling, launch-with-boost, and restore flow.
- Expanded Startup Manager with richer inventory, startup profiles, safer recommendations, delay support, and restore flow.
- Improved startup analysis with impact score, estimated memory pressure, estimated load time, and clearer recommendations.

### Assets
- `HyperBoostXInstaller.exe`
- `HyperBoostX-Portable-v1.0.2.zip`
- `HyperBoostX-Package-v1.0.2.zip`

## v1.0.1 - 2026-04-07

### Changed
- Improved WPF UI/UX with clearer page headers and inline action status cards.
- Connected more frontend actions directly to backend optimizations, tweaks, cleanup, repair, and network flows.
- Refined About App content and presentation.
- Rebuilt installer and release assets from the latest source.

## v1.0.0 - 2026-04-07

### Added
- Initial public release of HyperBoostX.
- WPF frontend, Python backend, and .NET launcher runtime architecture.
- Unified installer entrypoint as `HyperBoostX.exe`.
- Backend lifecycle handling from the launcher.
- Logging under `%LocalAppData%\\HyperBoost X\\logs`.
- Build, packaging, and installer pipeline for public distribution.

### Assets
- `HyperBoostXInstaller.exe`

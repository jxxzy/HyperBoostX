# Changelog

All notable changes to HyperBoostX are documented here.

## v1.2.12 - 2026-05-09
- Adds a dedicated `Send Test Update` action for the release-update Discord webhook so update alerts can be verified separately from error/audit alerts.
- Confirms release-update webhook credentials remain in Windows Credential Manager and are not written back into app-state.
- Fixes the local runtime deploy script default path so local deployment works more reliably from PowerShell.

## v1.2.11 - 2026-05-09
- Fixes Discord webhook storage so saved webhook URLs are kept in Windows Credential Manager without being shown in Settings.
- Adds an explicit Save Webhooks action, keeps Feature Audit delivery as one combined report, and removes `feature-audit.log` from runtime auto-log spam.
- Hardens Streaming Mode wording and behavior for safer mic endpoint volume/mute control, webcam profile diagnostics, and OpenCV motion tracking.
- Rebuilds the portable and installer artifacts for the stable release line.

## v1.2.10 - 2026-05-08
- Adds Apply Mic Settings so the Streaming Voice Mixer can push mute and default microphone volume to the active Windows communications microphone.
- Upgrades Streaming Mode webcam tools into Advanced Webcam Studio with local profile controls, diagnostics, streaming, low-light, and sharp-detail presets.
- Adds camera studio profile output to webcam diagnostics for OBS, TikTok LIVE Studio, Discord, and camera driver setup.

## v1.2.9 - 2026-05-08
- Upgrades Streaming Mode mic tools into a mixer-style control strip with Windows endpoint volume/mute, live meter, profile preview, and streaming preset controls.
- Adds Windows Volume Mixer, sound settings, diagnostics, and privacy shortcuts for safer audio setup without bundling external audio apps.
- Keeps webcam and mic controls inside Streaming Mode so users on the latest installer see the same feature set.

## v1.2.8 - 2026-05-08
- Reissued the Streaming Mode mic/webcam build under a new version so every PC can update past older same-label installs.
- Keeps Advanced Mic / Voice Meter and Advanced Webcam Settings directly inside Streaming Mode.

## v1.2.7 - 2026-05-08
- Moved Voice Meter and Webcam Settings from utility/driver-adjacent placement into Streaming Mode.
- Added advanced streaming mic controls for diagnostics, audio service reset, sound settings, device refresh, and privacy.
- Added advanced webcam controls for camera diagnostics, Windows Camera app launch, camera settings, privacy, and Device Manager.

## v1.2.6 - 2026-05-08
- Added Voice Meter in Utilities Tools with live microphone input level, peak reading, device refresh, and microphone privacy shortcut.
- Added Webcam Settings in Utilities Tools with camera device scan, Windows Camera settings, camera privacy, and Device Manager shortcuts.
- Added NAudio-powered microphone capture handling with safe cleanup when the app closes.

## v1.2.5 - 2026-05-08
- Rebalanced dashboard performance scoring so normal Windows process counts do not unfairly cap the score.
- Rebalanced Smart Recommendation optimization scoring with pressure-based CPU, RAM, disk, and startup components.
- Kept high-load systems clearly lower while making healthy idle systems read as healthy.

## v1.2.4 - 2026-05-08
- Fixed release-update Discord notifications after update webhook credentials are changed while the app is already open.
- Both Feature Audit and App Update notifications now reload secure webhook settings before sending.

## v1.2.3 - 2026-05-08
- Fixed Feature Audit Discord delivery after webhook credentials are updated while the app is already open.
- Manual Feature Audit report sending now reloads secure webhook settings from Windows Credential Manager before posting.

## v1.2.2 - 2026-05-08
- Fixed startup item collection and process memory handling so missing `memory_info` fields no longer break startup views after boost actions.
- Hardened backend shell execution and local CORS handling for safer desktop-only API usage.
- Improved Discord webhook alert delivery with validated webhook URLs and detailed send results.
- Added latest-release Discord notification support for users who enable update alerts.
- Removed numeric prefixes from the WPF sidebar navigation labels for a cleaner menu.
- Aligned WPF, launcher, backend, and installer metadata for the stable `v1.2.2` release.

## v1.2.1 - 2026-04-10
- Fixed startup item collection so missing `memory_info` on some Windows processes no longer crashes the startup list API.
- Expanded cleanup scopes across backend and WPF so temp, cache, browser, logs, and advanced cleanup actions return richer structured results.
- Hardened Streaming, Gaming, Booster, and Smart Recommendation flows with shared session detection for game, streamer, and Discord companion processes.
- Clarified in-app action status messaging so launcher-only, requested, applied, and warning states are more honest across repair, update, tweak, restore, utilities, and audit flows.

## v1.2.0 - 2026-04-08
- Added adaptive optimization groundwork with system-drive classification, device profiling, and bottleneck-aware recommendations for SSD/HDD, desktop/laptop, and different RAM classes.
- Extended Dashboard, Smart Recommendation, and System Info so the app surfaces device context and more realistic expected-gain messaging directly in the UI.
- Clarified updater readiness states so release-page-only, blocked, manual-ready, and auto-install-ready paths are easier to understand before installation.
- Carried forward the audit stabilization fixes so the `Feature Audit Full` and `Full QA Matrix` candidate both pass cleanly before official release.

## v1.1.7 - 2026-04-08
- Separated `Gaming Mode` and `Gaming Booster` into distinct UI flows so Gaming Mode focuses on profiles/session behavior while Gaming Booster focuses on instant optimization actions and reports.
- Kept the structured-log severity fix so backend warnings such as admin-required timer-resolution tweaks are not escalated into false error alerts in current builds.

## v1.1.9 - 2026-04-08
- Fixed `RUN FULL FEATURE AUDIT` so `Utilities Workflow Actions` no longer fails from the workflow output being overwritten by utilities history text.
- Updated runtime/app metadata from `1.1.8` to `1.1.9`.

## v1.1.8 - 2026-04-08
- Raised long-running Utilities `SFC` flows to a 20-minute timeout so `AUTO FIX SYSTEM`, `Repair Utilities`, and `FULL SYSTEM MAINTENANCE` no longer fail after the old 45-second cutoff.
- Reduced perceived notification lag by forcing the action status card to render immediately and by showing `Processing request...` before long-running actions finish.
- Expanded `RUN FULL FEATURE AUDIT` coverage with additional runtime, settings, update, compatibility, notification, and utilities workflow/safety probes.

## v1.1.6 - 2026-04-08
- Fixed structured log severity detection in the WPF log watcher so backend warnings are not escalated into false error alerts.
- Clarified booster profile partial-success messaging for admin-only tweaks such as timer resolution changes.

## v1.1.5 - 2026-04-08

### Changed
- Reduced backend `/api/health` churn by caching WPF health checks briefly instead of letting every panel and QA path hit the backend immediately.
- Normalized backend JSON payload handling in the WPF client so automation, startup, process, cleanup, and network flows consume consistent `JToken` objects.
- Switched WPF release packaging away from single-file publish so the installed runtime keeps its full dependency set, reducing exit-time native teardown risk and runtime assembly loss.
- Updated installer and release packaging to ship the full WPF runtime folder instead of only the UI executable.
- Updated runtime/app metadata from `1.1.4` to `1.1.5`.

### Validation
- `dotnet build wpf\\HyperBoostX.csproj`
- `dotnet build wpf\\HyperBoostX.csproj -c Release`

### Notes
- This hotfix focuses on deeper runtime hardening after the full audit passed, especially shutdown stability, JSON contract resilience, and release packaging correctness.

## v1.1.3 - 2026-04-08

### Changed
- Fixed Feature Audit incident handling so current audit runs no longer fail because of stale incidents from previous sessions.
- Improved NVIDIA Copilot error diagnostics with clearer 429/401/403 guidance, endpoint labels, and request-id support when available.
- Fixed app update version normalization so builds that already match the latest release no longer show a false "new version available" notification.
- Updated runtime/app metadata from `1.1.2` to `1.1.3`.

### Validation
- `dotnet build wpf\\HyperBoostX.csproj`
- `dotnet build wpf\\HyperBoostX.csproj -c Release`

### Notes
- This hotfix focuses on more trustworthy audit results, clearer NVIDIA failure diagnostics, and accurate in-app update detection.

## v1.1.4 - 2026-04-08

### Changed
- Hardened `Feature Audit` for `Network Booster` so transient adapter/process inspection failures degrade gracefully instead of failing the audit.
- Hardened `Feature Audit` for `Storage` so transient drive-label, drive-metric, and analyzer failures no longer crash the target audit pass.
- Updated runtime/app metadata from `1.1.3` to `1.1.4`.

### Validation
- `dotnet build wpf\\HyperBoostX.csproj -c Release`

### Notes
- This hotfix focuses on stabilizing the remaining `Feature Audit` runtime refresh paths in the desktop app.

## v1.1.2 - 2026-04-08

### Changed
- Hardened HyperBoostX Copilot NVIDIA Copilot connectivity with a safer request fallback path and improved response parsing.
- Added a visible `Last Test` result to the AI settings panel and persisted the latest connection-test status across restart.
- Improved Feature Audit runtime incident tracking so real feature errors are detected while stale or warning-only states do not keep modules failing incorrectly.
- Updated runtime/app metadata from `1.1.1` to `1.1.2`.

### Validation
- `dotnet build wpf\\HyperBoostX.csproj`
- `dotnet build wpf\\HyperBoostX.csproj -c Release`

### Notes
- This is a stable hotfix release focused on AI Copilot reliability and more accurate in-app audit behavior.

## v1.1.1 - 2026-04-08

### Changed
- Updated runtime/app metadata from `1.1.0` to `1.1.1`.
- Refreshed the About App page copy to clearly identify the app as a stable release instead of a beta build.
- Added visible stable badges and version markers to the About App page.

### Validation
- `dotnet build wpf\HyperBoostX.csproj`

### Notes
- This is a small stable hotfix that packages the About App stable-state refresh into the installer and portable outputs.

## v1.1.0 - 2026-04-08

### Added
- Stable GitHub release target for the fully validated `1.1.0` line.

### Changed
- Promoted runtime/app metadata from `1.1.0-beta.5` to `1.1.0`.
- Finalized backend audit fixes so `Full QA Matrix` reaches `56/56 passed`.
- Removed the WPF compatibility-summary dependency on `System.Security.Principal.Windows` by switching admin detection to a Win32 membership check.
- Hardened page navigation and feature-audit cancellation behavior to reduce freeze risk when moving between heavy pages.

### Validation
- In-app `Feature Audit / Full QA Matrix`: `56/56 passed`
- `dotnet build wpf\HyperBoostX.csproj`

### Notes
- This release promotes the validated beta line to stable with the latest audit, navigation, and runtime hardening fixes included.

## v1.1.0-beta.5 - 2026-04-08

### Added
- App-side Discord notifications when HyperBoost X detects a newer release during update checks.
- `.github/workflows/discord-release-notify.yml` to send Discord notifications automatically when a GitHub release or prerelease is published.

### Changed
- Updated runtime/app metadata from `1.1.0-beta.4` to `1.1.0-beta.5`.
- Prepared a small prerelease dedicated to validating the end-to-end GitHub release to Discord notification flow.

### Notes
- This prerelease is primarily intended to verify Discord update announcements from both the app and GitHub release pipeline.

## v1.1.0-beta.4 - 2026-04-08

### Added
- `tests\test_shell_util.py` to verify admin-required commands return a clear message when HyperBoost X is not elevated.

### Changed
- Updated runtime/app metadata from `1.1.0-beta.3` to `1.1.0-beta.4`.
- Improved `ShellUtil.execute_command(..., admin=True)` so blocked admin commands now return a clear privilege message instead of a misleading generic shell failure.
- Added a more consistent PowerShell invocation profile for admin-targeted shell execution.

### Validation
- `app\venv\Scripts\pytest.exe tests/test_shell_util.py tests/test_startup_api.py tests/test_monitor_service.py tests/test_health_api.py`

### Notes
- This hotfix is mainly for clearer runtime diagnostics in non-admin sessions.

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
- HyperBoostX Copilot foundation with NVIDIA Copilot integration, safe action approval, session memory, reasoning summary, and automation creation flow.
- Discord webhook reporting for important errors and crash events.
- Modular localization foundation with `en-US` and `id-ID` language packs.
- Persistent app configuration shared across settings, automation, AI, and recovery-related modules.
- In-app release checker for detecting newer author builds from GitHub.
- Secure NVIDIA API key and Discord webhook persistence via Windows Credential Manager.
- Sociabuzz donation shortcut in About App.
- `release-notes-v1.1.0-beta.txt` for beta release documentation.

### Changed
- Reworked major menu modules into native in-app panels instead of mostly external shortcuts.
- Separated automation runtime mode from policy profile.
- Upgraded Scheduled Automation from summary UI to persistent rules, queue, deferred tasks, retry state, and audit tracking.
- Updated About App, binary metadata, and installer metadata to `1.1.0-beta`.
- Synced installer publisher/author metadata to `MR.4NONY - HYPERINDO CYBER TEAM`.
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

# Changelog

All notable changes to HyperBoostX are documented here.

## v1.1.0-beta - 2026-04-07

### Added
- HyperBoostX Copilot foundation with OpenAI integration, safe action approval, session memory, reasoning summary, and automation creation flow.
- Discord webhook reporting for important errors and crash events.
- Modular localization foundation with `en-US` and `id-ID` language packs.
- Persistent app configuration shared across settings, automation, AI, and recovery-related modules.
- `release-notes-v1.1.0-beta.txt` for beta release documentation.

### Changed
- Reworked major menu modules into native in-app panels instead of mostly external shortcuts.
- Separated automation runtime mode from policy profile.
- Upgraded Scheduled Automation from summary UI to persistent rules, queue, deferred tasks, retry state, and audit tracking.
- Updated About App, binary metadata, and installer metadata to `1.1.0-beta`.
- Synced installer publisher/author metadata to `MR.4NONY`.
- Improved runtime safety for PowerShell execution, backend API failure handling, and activity logging.
- Refreshed README to document the beta architecture and current feature scope.

### Notes
- This build is intended for internal beta testing and feature validation.
- Public stable release should still be preceded by broader end-to-end QA on admin-required flows, restore/update paths, and installer upgrade behavior.

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

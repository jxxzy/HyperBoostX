# Bugs Fixed - HyperBoostX v2.0.1

## v2.0.1 Runtime Flow And Legacy Regression Fixes

- Fixed Dashboard `One Click Safe Boost` by replacing the removed `/api/triple-ai/full-flow` call with the supported `/api/boost/plan` flow.
- Added WPF client methods for `CreateBoostPlanAsync`, `ApplyBoostPlanAsync`, and `UndoBoostPlanAsync`; old TripleAi wrapper methods now route to safe boost endpoints for compatibility.
- Converted generic cyber sidebar pages from static status cards into live v1.3/v1.4 feature adapters with Primary, Preview, and Export/Report actions plus an in-page Live Result panel.
- Wired AI Advisor, Auto Gaming, Game Library, Game Profiles, GPU Center, HyperBalance, One Click Boost, Process Analyzer, Startup Manager, Cleanup, Network Tools, Benchmark Lab, Performance History, Performance Report, Creator Mode, Gaming Essentials, Restore & Backup, Protected Apps, Knowledge Base, Feature Audit, and About to real backend endpoints.
- Added runtime API aliases required by the audit contract: Smart Scan, advisor plan/safe-actions, KB topics/search/topic, history reports/compare/trends/export, background pressure, streaming/creator recommendations, recovery, update check/latest, network DNS, action log, and restore preview/apply/verify aliases.
- Restored legacy Streaming Mode surface in WPF: Advanced Mic / Voice Meter, Mic Diagnostics, Voicemeeter detect/open/download, Windows sound/privacy/mixer shortcuts, Advanced Webcam Studio, camera presets, camera shortcuts, and OBS/TikTok/Discord profile output.
- Extended streaming backend status with a `legacy_toolkit` payload for mic, Voicemeeter, webcam, and streaming profile sections.
- Added WPF local config schema metadata: `ConfigSchemaVersion`, `MigrationHistory`, and `LastMigrationStatus`, with corrupt settings backup/regeneration.
- Updated active patch metadata to `2.0.1` in `VERSION`, backend constants, WPF assembly metadata, and NSIS installer metadata.
- Added runtime verification scripts for version sync, WPF navigation, backend routes, release artifact contents, installed runtime, and clean-install dry-run.
- Hardened `verify_installed_runtime.ps1` so it no longer hangs on shortcut/localhost checks and records installed-runtime mismatch evidence instead.
- Added Python regression tests for runtime routes and streaming legacy toolkit exposure.
- Added .NET regression tests for WPF safe boost endpoint wiring and Streaming Center legacy UI visibility.
- Added .NET regression coverage to prevent cyber sidebar pages from regressing back to mock/static panels without live backend endpoint mappings.
- Fixed shared sidebar result rendering for both object-shaped and array-shaped JSON responses so endpoints like `/api/advisor/safe-actions` no longer fail with a `JArray` cast error.
- Added `docs/LEGACY_FEATURE_MATRIX.md` and `docs/REGRESSION_AUDIT_FROM_V1.md` for the requested version-1-to-latest regression audit.

---

# Previous Bugs Fixed - HyperBoostX v2.0.0

## Version Sync

- Backend constants, package metadata, WPF project metadata, launcher metadata, installer metadata, About page, update User-Agent, and `VERSION` now report `2.0.0`.
- Active README, release guide, release notes, security docs, QA docs, audit docs, and API reference were updated for v2.0.0.

## v1.4 API Coverage

- Added `app/api/product_v14.py` and registered it in the Flask backend.
- Added safe endpoints for advisor, knowledge base, scores, games, overlays, protection, processes, benchmark, GPU, drivers, startup, cleanup, network, essentials, streaming, RGB detection, plugins, settings, restore, feature audit, storage, action log, and v2 roadmap.

## Storage And Privacy

- Added local JSON storage service with portable mode and corrupt JSON backup/regeneration.
- Added local action log with redaction.
- Added UI settings persistence including reduce motion and high contrast fields.

## Safety

- Added protected process evaluator tests for dangerous actions.
- Kept incomplete high-risk features as roadmap-only rather than exposing fake controls.

## WPF Cyber UI Integration

- Replaced the active legacy `MainWindow` UI with a shell-only layout.
- Added global cyber theme/style dictionaries and referenced them from `App.xaml`.
- Added real routed WPF views under `wpf/Views/*` for dashboard, gaming, GPU, reports, safety, settings, feature audit, and about pages.
- Added cyber dashboard actions that either read backend state, build a safe plan, or navigate to the relevant real page.
- Added persisted WPF UI settings for animations, Reduce Motion, accent color, and Beginner/Advanced/Expert Preview mode.

## Tests

- Python tests expanded from 43 to 52 and pass.
- .NET tests pass after v1.4 metadata update.

## Signing Script

- Moved `param(...)` to the top of `sign_release.ps1` so signing readiness checks fail for the correct reason: missing `-Thumbprint` or `-PfxPath`, not parser syntax.

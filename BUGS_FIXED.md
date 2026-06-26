# Bugs Fixed - HyperBoostX v1.4.0

## Version Sync

- Backend constants, package metadata, WPF project metadata, launcher metadata, installer metadata, About page, update User-Agent, and `VERSION` now report `1.4.0`.
- Active README, release guide, release notes, security docs, QA docs, audit docs, and API reference were updated for v1.4.0.

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

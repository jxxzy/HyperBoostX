# HyperBoostX Roadmap

## v1.4.0 Feature Expansion Stable

Completed in this branch:

- AI Performance Advisor local diagnosis.
- Knowledge Base for GPU/game performance terms.
- HyperBoost Score Engine with documented heuristic inputs.
- Performance history and timeline APIs.
- Game database and safe profile metadata flow.
- Overlay Detector and Protected Process evaluator.
- Process Analyzer, local benchmark history, GPU Center guidance, startup/cleanup/network facades, Gaming Essentials helper, Streaming Center, RGB detection, plugin registry foundation, restore sessions, and UI settings.
- Local JSON storage recovery, action logging, portable mode, and privacy redaction.

## v1.5.x Polish

- WPF MVVM split for the largest remaining MainWindow sections.
- More polished cyber dashboard widgets and responsive states.
- Better keyboard navigation and screen reader labels.
- Safe Mode / Recovery Mode entry points for users who need guided rollback.
- App Integrity Check for installed files and release manifest validation.
- Website starter or docs site with screenshots, download guidance, and SHA256 verification.

## v1.6.x Trust And Distribution

- Signed installer when a code-signing certificate is available.
- Auto updater that checks GitHub Release, downloads only after user approval, verifies SHA256/signature, installs, and rolls back on failure.
- Stable, Preview, Beta, and Developer update channels.
- Optional anonymous telemetry with explicit opt-in and local preview of the payload.
- Local crash report export with redaction improvements and optional manual share flow. Crash reports are not uploaded automatically.
- local crash report export with redaction remains a required trust feature for every release.

## v2.0.0 Vision 2026-2027

- Plugin SDK for third-party developers.
- Official plugin marketplace.
- Local LLM or small offline model option for AI diagnosis.
- Driver health analyzer.
- Hardware stress test.
- Verified HyperBoostX benchmark engine and similar-hardware dataset.
- Performance Monitor Overlay, only if it adds clear value beyond existing tools.
- HyperBoostX Cloud for optional profiles, reports, and settings sync.
- Optional professional license server, activation, lifetime plan, and admin dashboard.

## Roadmap-Only In v1.4.0

- RGB control is not implemented. v1.4 detects RGB apps only.
- Global benchmark comparison is not implemented without a verified dataset.
- Plugin SDK/marketplace is not active. v1.4 exposes a local registry foundation only.
- Cloud sync is not implemented.
- License activation is not implemented in v1.4.0. No v1.4.0 feature is locked behind a license.

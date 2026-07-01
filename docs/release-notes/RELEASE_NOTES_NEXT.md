# HyperBoostX v2.0.1 Flow Restoration & Runtime Fix Patch

Status: draft, not tagged or released yet.

## What Changed

- Restored the Dashboard safe boost runtime path. `One Click Safe Boost` now creates a supported `/api/boost/plan` instead of calling removed Triple AI endpoints.
- Restored functional v1.3/v1.4-style flow inside the cyber sidebar pages. Generic pages now expose Primary, Preview, and Export/Report actions and show live backend results instead of only static capability cards.
- Wired the sidebar features to real local endpoints for AI Advisor, Auto Gaming, Game Library, Game Profiles, GPU Center, HyperBalance, One Click Boost, Process Analyzer, Startup Manager, Cleanup, Network Tools, Benchmark Lab, Performance History, Performance Report, Creator Mode, Gaming Essentials, Restore & Backup, Protected Apps, Knowledge Base, Feature Audit, and About.
- Added route compatibility for the runtime audit contract, including Smart Scan, advisor plan/safe-actions, Knowledge Base aliases, history/report aliases, background pressure, streaming/creator recommendations, recovery, update, network DNS, action log, and restore aliases.
- Added v2.1 compatibility API aliases for the latest audit contract, including `/api/status`, `/api/settings`, dashboard summary/score/alerts/activity, scan system/quick/full, boost preview/history, performance plan/apply, process summary/preview-close, storage drives/scan/analyze, gaming/gpu/network aliases, AI fallback, audit, update preview, reports, and logs. These new aliases use a standard `ok/module/action/status/message/data/warnings/blocked_reasons/restore/report` envelope.
- Restored the legacy Streaming Mode surface in the v2 WPF Streaming Center:
  - Advanced Mic / Voice Meter
  - Mic Diagnostics
  - Voicemeeter detect/open/download
  - Windows Sound Settings, Volume Mixer, and microphone privacy shortcuts
  - Advanced Webcam Studio
  - Streaming, Low Light, and Sharp Face camera presets
  - Camera settings/privacy/Camera app/Device Manager shortcuts
  - OBS, TikTok LIVE Studio, and Discord profile output
- Added explicit LocalAppData UI config schema metadata and corrupt-settings recovery evidence.
- Added version sync, WPF navigation, backend route, release artifact, installed runtime, and clean-install dry-run verifier scripts.
- Hardened installed-runtime verification so it completes without hanging and reports local install drift such as `2.0.0` installed while source/package is `2.0.1`.
- Fixed shared sidebar result rendering for array-shaped JSON payloads so actions such as `Review Safe Actions` do not fail with a `JArray` cast error.
- Fixed a launcher/session-token mismatch path where WPF could open against an older token-protected backend and make sidebar POST actions fail with `Unauthorized local session`.
- Made sidebar status output understand `blocked`, `preview`, `partial`, `admin_required`, `ok=false`, and `blocked_reasons` responses so Safety Guard and approval states are visible instead of looking like generic failures.
- Made WPF HTTP route errors preserve backend response bodies, which surfaces actionable 401/404/409/blocked messages in the UI.
- Hardened the legacy `/api/tweaks/*` surface: Defender disable and permanent Windows Update disable are removed from the active catalog and hard-blocked before restore point or registry code can run; all remaining mutating tweak apply calls now require explicit confirmation.
- Reduced idle overhead by changing the shell backend pulse from 4 seconds to 10 seconds and the background log watcher from 12 seconds to 45 seconds.
- Reduced WPF publish size/noise by limiting satellite resources to `en-US;id-ID`; the rebuilt portable package is about `313.98 MB` and no longer ships unrelated WPF resource folders such as `cs`, `de`, `es`, or `fr`.
- Added `docs/LEGACY_FEATURE_MATRIX.md` and `docs/REGRESSION_AUDIT_FROM_V1.md`.
- Restored the v1.3 sidebar/menu surface in the cyber WPF shell with legacy-safe pages for Performance Boost, Background Apps, Storage, Gaming Booster, Advanced Mic Mixer, Webcam Studio, Camera Tracking, Network Booster, DNS & Latency, Privacy Center, Security & Health, Apps Manager, App Uninstaller, Tweaks Center, Windows Features/Services, Update Control, Repair Tools, Driver & Update Center, Power Optimization, Visual Effects, Restore Point Manager, Scheduled Automation, Utilities, Master Test Engine, and Feature Audit Matrix.
- Added v1.3 parity backend status/preview endpoints for restored menu categories. These endpoints are intentionally read-only, preview-only, credential-gated, or admin-gated where a real Windows mutation would be risky.
- Added `docs/FEATURE_PARITY_v1.3_vs_latest.md`, `docs/FULL_AUDIT_REPORT.md`, `docs/UI_UX_AUDIT.md`, and `docs/BACKEND_ROUTE_AUDIT.md`.
- Improved Dashboard live telemetry/scan score refresh, GPU Center auto-refresh, Network Tools honest empty state, dynamic About version display, generic loading/error states, and Apply/Undo confirmation UX.

## Local Validation

- Python tests: `58 passed`.
- .NET Debug tests: `33 passed` after v1.3 parity and lightweight-polling assertions.
- .NET Release tests: `33 passed`.
- Backend v1.3/v2.1 parity route contract: `4 passed`.
- UI/UX quality verifier: PASS for 50+ sidebar items, route coverage, button handlers, placeholder guard, quick actions, and settings persistence.
- Debug and Release solution builds: `0 Warning(s), 0 Error(s)`.
- Isolated local deploy/package build: PASS under `artifacts/local-deploy`.
- Packaged backend smoke: `/api/health` `ok`, `/api/version` `2.0.1`.
- Portable app smoke: `artifacts/local-deploy/app/HyperBoostX.exe` launched visible WPF through the launcher, backend returned `/api/health ok`, `/api/version 2.0.1`, `/api/status`, `/api/advisor/safe-actions`, `/api/processes/background-pressure`, `/api/startup/items`, `/api/cleanup/scan`, `/api/storage/status`, `/api/streaming/status`, `/api/feature-audit/matrix`, and `/api/tweaks/list` successfully; unauthenticated `POST /api/boost/plan` returned `401` as expected.

## Safety Boundary

- HyperBoostX still does not guarantee FPS or ping improvements.
- HyperBoostX does not overclock, undervolt, tune voltage, modify BIOS/UEFI, disable anti-cheat, disable Defender, or disable GPU/audio/network driver services.
- Streaming mic gain/gate/compressor and webcam sliders are profile guidance unless a supported Windows endpoint action is explicitly implemented and approved.
- Voicemeeter support is detect/launch/official-download guidance; HyperBoostX does not silently install or rewire virtual audio.

## Still Partial Or Blocked

- Real mic DSP apply and audio service reset need hardware/admin lab validation.
- Webcam hardware property writes are intentionally not forced.
- Startup Manager and Cleanup apply paths remain conservative until rollback/deletion boundaries are lab-tested.
- NVIDIA real API test needs the owner's key in Windows Credential Manager.
- Discord webhook delivery needs owner webhook credentials.
- Code signing needs owner certificate/PFX.
- Installed-app smoke must be rerun after installing the v2.0.1 build; the local installed runtime still reports `2.0.0`.

# HyperBoostX v1.3.0 Stable

HyperBoostX v1.3.0 upgrades the app toward a premium Universal Windows Gaming Optimizer with safer GPU-neutral detection, hardware-aware recommendations, before/after reporting, local job progress, and stronger local API security.

## Recommended Download

Download `HyperBoostXInstaller.exe`, run it, and follow the installer.

Optional: use `SHA256SUMS.txt` to verify the installer checksum.

## Major Improvements

- Added universal GPU vendor detection for NVIDIA GeForce GTX/RTX, AMD Radeon/RX/Vega, Intel Arc, Intel Iris Xe/UHD/iGPU, Microsoft Basic Display Adapter, and unknown fallback.
- Added GPU profile classification with safe vendor-aware profile recommendations.
- Added hardware profile engine with PC Health, Gaming Readiness, Streaming Readiness, and Startup Cleanliness scores.
- Added safe boost plan/apply/undo API flow that requires approval before applying actions.
- Added before/after report schema and JSON/TXT/Markdown export support.
- Added local crash report export with redaction for API keys, AI keys, tokens, GitHub tokens, usernames, sensitive paths, and future license keys.
- Added local backend job queue with job ID, progress, stage, log tail, cancel, and final result.
- Added launcher-generated local session token support through `X-HyperBoostX-Session` for mutating backend endpoints.
- Added localhost-only CORS header support for the session token.
- Added support docs, bug/feature request templates, troubleshooting, FAQ, and roadmap preparation.

## GPU Center

The v1.3.0 backend exposes GPU Center data through:

- `GET /api/hardware/gpu`
- `GET /api/hardware/vendors`
- `GET /api/hardware/overlays`
- `GET /api/hardware/profile`

The contract includes vendor badge, GPU model/family, active display GPU, dedicated/integrated/hybrid status, VRAM total/usage when available, GPU usage, temperature when available, driver version when available, overlay status, vendor software status, recommendations, safe actions, skipped actions, blocked risky actions, and profile recommendation.

## AI Doctor And Safety

AI and automation remain plan-first and approval-gated. HyperBoostX blocks unsafe driver hacks, forced Defender disablement, permanent Windows Update disablement, BIOS/UEFI changes, overclocking, undervolting, voltage changes, deleting user data, and irreversible changes without restore metadata.

## Backend Security

- Backend is intended to bind to `127.0.0.1`.
- Mutating endpoints reject unauthorized local sessions when `HYPERBOOSTX_SESSION_TOKEN` is present.
- Launcher generates the session token in memory and passes it to backend and WPF processes.
- Token is not logged and is not written to repo, reports, config, or release artifacts.

## Testing Result

Current workspace validation is recorded in `QA_RESULTS.md`. Python tests pass with 43 tests and .NET tests pass with 28 tests at the current checkpoint. Do not claim full multi-machine hardware support unless the lab matrix is completed and recorded.

## Roadmap-Only Items

Code signing, full auto updater, public website, opt-in anonymous diagnostics, and license activation are roadmap-only items. HyperBoostX v1.3.0 does not implement license activation, payment, feature locking, or a license server.

## Known Limitations

- Full MVVM rewrite of every WPF legacy page is not complete in this release.
- GPU telemetry fields depend on Windows, WMI, GPUtil/driver support, and may return `Unknown` or `0` safely.
- Multi-machine Windows 10/11 lab matrix is not claimed unless separately executed.
- If the installer is unsigned, Windows may show Unknown Publisher or SmartScreen.
- GitHub Release publishing requires authenticated repository permissions.

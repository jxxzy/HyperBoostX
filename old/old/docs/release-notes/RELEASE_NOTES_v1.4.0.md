# HyperBoostX v1.4.0 Feature Expansion Stable

HyperBoostX v1.4.0 expands the app from a safe Windows gaming optimizer into a more product-ready local performance assistant. This release adds real backend contracts for diagnosis, knowledge base, scores, profiles, reports, protected processes, local history, and roadmap-safe product foundations.

## Recommended Download

- `HyperBoostXInstaller.exe` from the official GitHub Release.
- Verify with `SHA256SUMS.txt` before installing.
- If the installer is unsigned, Windows may show Unknown Publisher or SmartScreen.

## New And Improved

- Real cyber WPF shell is active in the running app. `MainWindow` is shell-only, global resources are merged from `wpf/Themes/*` and `wpf/Styles/*`, and page content loads from `wpf/Views/*`.
- Dashboard now shows HyperBoostX Safe AI Windows Gaming Optimizer hero, score rings, CPU/RAM/GPU/VRAM/storage/network/power/restore/backend cards, Safety Guard, restore indicator, backend pulse, scanner line, and cyber action buttons.
- Settings now persists Enable Animations, Reduce Motion, Accent color, Beginner, Advanced, and Expert Preview through local `ui_settings.json`.
- AI Performance Advisor diagnoses GPU, CPU, VRAM, RAM, storage, startup, and general stutter causes from local counters.
- HyperBoostX Knowledge Base explains DLSS, FSR, XeSS, Resizable BAR, Game Mode, HAGS, VRR, V-Sync, G-Sync, FreeSync, Frame Generation, Reflex, AFMF, and HYPR-RX.
- HyperBoost Score Engine returns deterministic Gaming, AI, Health, Streaming, Storage, Network, and Security scores.
- Game Profile Manager adds local game database, custom game profiles, preview/apply/restore metadata, and session history.
- Overlay Conflict Detector exposes overlay status and recommendations.
- Protected Process List blocks anti-cheat, security, driver, and unsafe action targets.
- Process Analyzer exposes heavy-process, startup-impact, recommendation, and export endpoints.
- Benchmark Report supports manual input, CSV import, local history, latest result, and export.
- GPU Center adds vendor guide, recommendations, report export, and local hardware database output.
- Startup, Cleanup, Network, Gaming Essentials, Streaming, Restore/Backup, Feature Audit, Settings, RGB detection, and Plugin registry foundations are exposed through v1.4 APIs.
- Local storage now includes config, logs, reports, backups, profiles, sessions, and diagnostics folders.
- Portable mode can be enabled with `HYPERBOOSTX_PORTABLE_HOME`.
- Silent installer reinstall now skips the previous-install message box when running with `/S`.

## Safety Guard

HyperBoostX continues to block forced Defender disable, permanent Windows Update disable, anti-cheat changes, GPU/audio/network driver service changes, overclocking, undervolting, voltage changes, BIOS/UEFI actions, arbitrary shell execution, and destructive cleanup.

Mutating endpoints require `X-HyperBoostX-Session` when `HYPERBOOSTX_SESSION_TOKEN` is present.

## Honest Limitations

- No guaranteed FPS claim.
- No vendor partnership claim.
- Driver Recommendation Center does not fabricate latest stable driver numbers and does not auto-download drivers.
- Similar-hardware benchmark comparison is roadmap until a verified dataset exists.
- RGB control, plugin SDK/marketplace, performance overlay, cloud sync, and license activation are roadmap-only.
- Cleanup apply remains conservative in the v1.4 backend and does not perform destructive broad cleanup.

## Testing Summary

- Python: `52 passed`.
- .NET: `28 passed`.
- Debug build: passed.
- Release build: passed with 0 warnings after the WPF cyber shell integration.

See `QA_RESULTS.md` for the final command evidence from this release branch.

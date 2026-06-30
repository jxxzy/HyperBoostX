# HyperBoostX User Guide

Current recommended public stable: `HyperBoostX v1.3.0 Stable`

v2.x builds are development previews until feature parity, UI/UX restoration, backend audit, installer validation, and smoke testing pass.

## Beginner Path

1. Open HyperBoostX through the launcher.
2. Confirm backend status is connected.
3. Open Dashboard or AI Performance Advisor.
4. Run scan/diagnosis.
5. Read bottleneck analysis before applying any action.
6. Preview safe boost, game profile, startup, cleanup, or network actions.
7. Approve only actions you understand.
8. Export a before/after report.
9. Keep restore metadata visible.

## AI Performance Advisor

Advisor checks CPU, RAM, GPU, VRAM, disk, startup, and background pressure. It can identify patterns such as GPU bottleneck, CPU bottleneck, VRAM pressure, RAM pressure, or storage pressure.

Advisor does not run shell commands and does not apply changes without approval.

## Knowledge Base

Use the Knowledge Base to learn DLSS, FSR, XeSS, Resizable BAR, Game Mode, HAGS, VRR, V-Sync, G-Sync, FreeSync, Frame Generation, Reflex, AFMF, and HYPR-RX.

## Game Profiles

Game profiles provide guidance and safe metadata. In-game graphics changes remain user-controlled. HyperBoostX will not claim expected FPS because results depend on hardware, game version, drivers, and settings.

## Overlay Check

Overlay Detector lists detected overlays and recommends reviewing them. Pause overlays only if recording/streaming is not needed and only after approval.

## Process Analyzer

Process Analyzer is read-only. It helps identify heavy processes and startup pressure without killing protected services.

## Benchmark Report

Manual benchmark and CSV import store local history. Similar-hardware comparison is roadmap until a verified dataset exists.

## GPU Center

GPU Center supports NVIDIA, AMD Radeon, Intel Arc/iGPU, Microsoft Basic Display Adapter, and unknown fallback. Driver Recommendation Center does not auto-download drivers or fabricate latest stable versions.

## Cleanup

Cleanup scan and preview are conservative. v1.4 blocks destructive broad cleanup and does not delete user documents, downloads, desktop, pictures, videos, music, game saves, or system files.

## Restore And Backup

Supported actions create restore metadata. Use Restore & Backup to inspect sessions and verify what can be restored.

## Settings

Settings include theme/accent foundations, reduce motion, high contrast, font scaling, telemetry opt-in fields, and performance budget targets. Telemetry remains off by default.

## Troubleshooting

- Backend disconnected: open `http://127.0.0.1:5000/api/health` and restart through launcher.
- Unknown GPU: install official GPU/OEM driver manually if Windows exposes Microsoft Basic Display Adapter.
- SmartScreen: verify SHA256 and install only from official GitHub Release.
- Crash: export a local redacted crash report and review it before sharing.

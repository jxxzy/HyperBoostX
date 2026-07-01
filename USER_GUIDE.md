# HyperBoostX User Guide

Current stable release: `HyperBoostX v2.10.0 Stable Unsigned`

## Beginner Path

1. Open HyperBoostX through the installed launcher.
2. Confirm backend status is connected.
3. Open Dashboard, Smart Scan, or AI Performance Advisor.
4. Run scan/diagnosis.
5. Read bottleneck analysis before applying any action.
6. Preview safe boost, game profile, startup, cleanup, or network actions.
7. Approve only actions you understand.
8. Export a report after changes.
9. Keep restore metadata visible.

## Advanced / Expert Mode

Advanced and Expert modes expose more technical detail, raw logs, JSON, and diagnostics. They do not bypass Safety Guard.

## AI Performance Advisor

Advisor checks CPU, RAM, GPU, VRAM, disk, startup, and background pressure. It can identify patterns such as GPU bottleneck, CPU bottleneck, VRAM pressure, RAM pressure, or storage pressure.

Advisor does not run shell commands and does not apply changes without approval.

## GPU Center

GPU Center supports NVIDIA, AMD Radeon, Intel Arc/iGPU, Microsoft Basic Display Adapter, and unknown fallback. Driver Recommendation Center does not auto-download drivers or fabricate latest stable versions.

## Cleanup

Cleanup scan and preview are conservative. HyperBoostX blocks destructive broad cleanup and does not delete user documents, downloads, desktop, pictures, videos, music, game saves, or system files.

## Restore And Backup

Supported actions create restore metadata. Use Restore & Backup to inspect sessions and verify what can be restored.

## Reports

Reports can be exported in JSON, TXT, or Markdown where supported. Sensitive paths, usernames, and tokens are redacted.

## Troubleshooting

- Backend disconnected: start the app through the installed launcher and open `http://127.0.0.1:5000/api/health`.
- Unknown GPU: install the official GPU/OEM driver manually if Windows exposes Microsoft Basic Display Adapter.
- SmartScreen: the v2.10.0 installer is unsigned; verify SHA256 before installing.
- Crash: export a local redacted crash report and review it before sharing.

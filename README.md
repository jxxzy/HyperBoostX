# HyperBoostX

[![Stable](https://img.shields.io/badge/Stable-v2.10.0%20unsigned-16a34a?style=for-the-badge&logo=windows&logoColor=white)](#release-status)
[![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011-2563eb?style=for-the-badge&logo=windows&logoColor=white)](#requirements)
[![Safety Guard](https://img.shields.io/badge/Safety%20Guard-Always%20On-22c55e?style=for-the-badge)](#safety-guard)

HyperBoostX is a Windows desktop control center for safe local PC diagnosis, gaming readiness, hardware-aware recommendations, preview-first optimization, restore visibility, and report export.

## Release Status

Current release: **HyperBoostX v2.10.0 Stable Unsigned**.

This stable label is based on the owner/admin gate completed on **2026-07-01**:

- Registry `DisplayVersion` is `2.10.0`, not `1.3.0`.
- Installed app is under `C:\Program Files\HyperBoostX`.
- Desktop shortcut exists on Public Desktop and targets the installed launcher.
- Start Menu shortcut exists and targets the installed launcher.
- Installed backend `/api/health` passes on `http://127.0.0.1:5000`.
- Installed backend `/api/version` returns `2.10.0`.
- WPF installed smoke passes.
- Launcher/backend token sync passes.
- No orphan installed HyperBoostX process remains after validation.
- Silent uninstall and silent reinstall pass.

Code signing status: `SKIPPED_BY_OWNER_NO_CERT`. The installer is unsigned because no owner certificate/PFX was supplied. Verify SHA256 before installing.

## Artifact

Primary local artifact:

- `HyperBoostXInstaller.exe`

Final release bundle:

- `release/final/v2.10.0/`

Current installer SHA256:

```text
daec54b8ca059f9196c388811cd8ea0ad9fbff3c61f678f14bccd55f78ea3924  HyperBoostXInstaller.exe
```

## What Works

- 72 menus are mapped.
- 596 buttons/actions are active.
- 0 stable action-map buttons are marked partial/roadmap/guidance.
- 165 unique UI endpoints are referenced.
- Backend route audit reports 365 API route rules and 361 unique API paths.
- Python test suite passes: 72 tests.
- .NET test suite passes: 38 tests.
- Full QA gate, route contract, WPF handler verification, UI/UX guard, package guard, version sync, and installed-runtime gate pass.

## Safety Guard

HyperBoostX blocks dangerous or irreversible actions, including:

- forced Windows Defender disable
- permanent Windows Update disable
- anti-cheat process/service tweaks
- GPU/audio/network driver service disable
- BIOS/UEFI changes
- overclock, undervolt, voltage changes
- arbitrary AI-generated shell execution
- destructive cleanup of user files
- protected process kill

Supported mutating flows use preview, explicit approval, safety checks, restore metadata where applicable, report output, and undo/restore visibility where supported.

## Honest Feature Boundaries

HyperBoostX does not guarantee FPS, ping, latency, or smoothness improvement.

HyperBoostX is not official NVIDIA, AMD, Intel, MSI, ASUS, Corsair, Microsoft, or OEM software.

Feature boundaries:

- RGB is software/conflict detection and safe restart guidance, not full lighting device control.
- Driver features are hardware-aware guidance/report export, not automatic driver download/install.
- License/cloud is local-only boundary state, not a production cloud account system.
- Plugin marketplace is local catalog/manifest validation, not arbitrary remote plugin execution.
- Expert mode exposes more detail but never bypasses Safety Guard.

## Requirements

- Windows 10 or Windows 11.
- Installed release uses the packaged launcher, WPF runtime, and local backend.
- Backend binds to `127.0.0.1`.
- Installer is unsigned unless the owner supplies signing material.

## Privacy And Local Data

- Runtime data is stored under `%LocalAppData%\HyperBoost X`.
- Portable mode is supported through `HYPERBOOSTX_PORTABLE_HOME`.
- Logs and reports redact tokens, API keys, usernames, and sensitive paths.
- Crash reports are local manual exports.
- No silent cloud telemetry is enabled.

## Build And Test

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build_stable_release.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\owner_admin_stable_gate.ps1 -ExpectedVersion 2.10.0 -BackendPort 5000
app\venv\Scripts\python.exe -m pytest -q tests
dotnet test dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj -c Release
```

## Documentation

- [Release notes v2.10.0](RELEASE_NOTES_v2.10.0.md)
- [Final audit report](docs/FINAL_AUDIT_REPORT_v2.10.0.md)
- [Release gate result](docs/RELEASE_GATE_RESULT.md)
- [QA results](QA_RESULTS.md)
- [Feature matrix](FEATURE_MATRIX.md)
- [Security](SECURITY.md)
- [User guide](USER_GUIDE.md)
- [Troubleshooting](TROUBLESHOOTING.md)
- [API reference](API_REFERENCE.md)

## Known Limitations

- Code signing is skipped because no owner certificate/PFX was supplied.
- External hardware matrix should still be expanded beyond this machine.
- OS-level admin apply/rollback remains guarded and limited to supported flows.
- GitHub tag/release publication depends on repository credentials and owner approval.

## Disclaimer

HyperBoostX provides safe local optimization guidance and supported reversible actions where implemented. Hardware, drivers, Windows configuration, games, permissions, and background software differ per PC. No performance improvement is guaranteed.

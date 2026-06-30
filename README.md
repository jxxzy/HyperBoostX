# HyperBoostX

[![Stable](https://img.shields.io/badge/Stable-v1.3.0-16a34a?style=for-the-badge&logo=github&logoColor=white)](https://github.com/jxxzy/HyperBoostX/releases/tag/v1.3.0)
[![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011-2563eb?style=for-the-badge&logo=windows&logoColor=white)](#requirements)
[![Safety Guard](https://img.shields.io/badge/Safety%20Guard-Always%20On-22c55e?style=for-the-badge)](#safety-guard)
[![v2.x](https://img.shields.io/badge/v2.x-Development%20Preview-f59e0b?style=for-the-badge)](#v2x-development-direction)

HyperBoostX is a safe Windows gaming optimizer focused on local PC health, gaming readiness, GPU-aware recommendations, safe boost plans, restore visibility, and report export.

## Important Status Notice

Current recommended public stable version: **HyperBoostX v1.3.0 Stable**.

The v2.x line, including v2.0.0, v2.0.1, and the current v2.10.0 work, is a **Development Preview / Next Major Rebuild** until full v1.3/v1.4 feature parity, UI/UX restoration, backend audit, installer validation, and real smoke testing are complete.

`v2.10.0` is a future stable candidate only after validation. It must not be described as stable, final, public-ready, or complete before the release gates pass.

## Download

| Line | Status | Recommended for | Link |
| --- | --- | --- | --- |
| v1.3.0 | Current public stable baseline | Normal users | [GitHub Release v1.3.0](https://github.com/jxxzy/HyperBoostX/releases/tag/v1.3.0) |
| v2.x | Development preview / next major rebuild | Testers and contributors only | Use preview artifacts only when the owner marks them for testing |
| v2.10.0 | Future stable candidate | Not public stable yet | Requires feature parity, UI/UX, backend, installer, smoke, and release-gate validation |

If an installer is unsigned, Windows may show Unknown Publisher or SmartScreen. Use the official GitHub Release page and verify checksums when they are provided.

## What HyperBoostX Does

- Scans local PC pressure and explains findings.
- Builds safe boost plans for gaming-oriented workflows.
- Shows PC Health, Gaming Readiness, Streaming Readiness, and Startup Cleanliness signals.
- Provides GPU-aware recommendations for NVIDIA, AMD Radeon, Intel Arc/iGPU, Microsoft Basic Display Adapter, and unknown GPU fallback.
- Keeps restore metadata visible for supported changes.
- Exports before/after reports in JSON, TXT, or Markdown where supported.
- Uses a local backend intended to bind to `127.0.0.1`.
- Uses launcher-generated local session tokens for mutating backend calls when enabled.

## What HyperBoostX Does Not Do

HyperBoostX does not guarantee FPS, ping, latency, or smoothness improvement on every PC.

HyperBoostX is not:

- An overclocking tool.
- An undervolting tool.
- A BIOS/UEFI editor.
- An anti-cheat bypass.
- A driver modding tool.
- A tool for permanently disabling important Windows services.
- Official NVIDIA, AMD, Intel, MSI, Microsoft, or vendor software.

## v1.3.0 Stable Highlights

v1.3.0 is the current public stable baseline selected by the owner.

- Universal GPU vendor detection.
- NVIDIA / AMD Radeon / Intel Arc or iGPU / Microsoft Basic Display Adapter / unknown GPU fallback.
- Hardware profile engine.
- PC Health score.
- Gaming Readiness score.
- Streaming Readiness score.
- Startup Cleanliness score.
- Safe boost plan / apply / undo flow.
- Before/after report.
- JSON / TXT / Markdown report export.
- Crash report export with redaction.
- Backend job queue.
- Launcher-generated local session token.
- Localhost-only backend security flow.
- Troubleshooting, FAQ, roadmap, bug report, and feature request docs.

See [RELEASE_NOTES_v1.3.0.md](RELEASE_NOTES_v1.3.0.md) for the stable baseline details.

## GPU Center

The stable GPU baseline is vendor-aware and safe by design:

- NVIDIA GeForce GTX/RTX guidance.
- AMD Radeon RX/Vega/integrated graphics guidance.
- Intel Arc, Iris Xe, UHD, and iGPU guidance.
- Microsoft Basic Display Adapter fallback.
- Unknown GPU safe fallback.

GPU recommendations are guidance and compatibility signals. HyperBoostX does not auto-download drivers, fabricate latest driver numbers, disable GPU driver services, overclock, undervolt, or replace vendor control panels.

## Safety Guard

Safety Guard blocks or refuses high-risk actions, including:

- Force disable Windows Defender.
- Permanent disable Windows Update.
- Anti-cheat process or service changes.
- GPU driver service disabling.
- Audio driver service disabling.
- Network driver service disabling.
- BIOS/UEFI changes.
- Overclocking.
- Undervolting.
- Voltage changes.
- Arbitrary AI-generated shell execution.
- Destructive cleanup of user files.
- Irreversible system changes without restore metadata.

Safe flow:

```text
Scan PC
-> Explain findings
-> Build safe plan
-> Show Safety Guard result
-> Ask user approval
-> Apply supported safe action
-> Save restore metadata
-> Generate report
-> Show undo / restore visibility
```

## Beginner Guide

1. Download the public stable release from [v1.3.0](https://github.com/jxxzy/HyperBoostX/releases/tag/v1.3.0).
2. Start the app through the HyperBoostX launcher.
3. Run a scan first.
4. Read the findings and Safety Guard notes.
5. Apply only supported safe actions that you understand.
6. Export a report after changes.
7. Use undo/restore visibility if a supported change needs to be reverted.

## Recommended Use

Use HyperBoostX for safe local diagnosis, PC health review, GPU-aware recommendations, startup/background-app review, conservative boost planning, and report/restore workflows.

Do not use HyperBoostX as a substitute for vendor GPU tools, Windows security policy, OEM driver support, hardware tuning tools, or professional repair work.

## v2.x Development Direction

v2.x is the next major rebuild. It is not the current recommended public stable release.

Target direction for v2.x:

- Full v1.3 / v1.4 feature parity.
- Better cyber gaming UI.
- Cleaner dashboard.
- Complete sidebar navigation.
- More visible buttons and feature cards.
- No placeholder-only screen.
- Better backend API contract.
- Better error handling.
- Better local session token handling.
- Better feature audit screen.
- Better restore center.
- Better beginner mode.
- Better advanced mode.
- Better hardware-aware recommendations.
- Better installer and release validation.
- Cleaner docs and website.

The runtime `VERSION` file can point at a v2 development build, such as `2.10.0-beta.1`, while this README still identifies `v1.3.0` as the recommended public stable version. That split is intentional until v2 passes the full release gate.

## Roadmap-Only Items

Do not treat these as shipped stable features unless a future release explicitly validates them:

- Full auto updater.
- Public benchmark database.
- Similar-hardware comparison.
- Plugin marketplace.
- RGB device control.
- Real-time performance overlay.
- HyperBoostX Cloud sync.
- Paid license lock.
- Activation server.
- Automatic driver download/install.
- Code signing.
- Multi-machine validation matrix.

## Requirements

- Windows 10 or Windows 11.
- .NET 8 Windows Desktop Runtime for development builds.
- Local Python Flask backend for development builds.
- Release builds may package the backend/runtime components.
- Backend should bind to `127.0.0.1` only.

## Build And Test

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify_repo.ps1
app\venv\Scripts\python.exe -m pytest -q tests
dotnet build HyperBoostX.sln -v minimal
dotnet test dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj -c Debug
```

Additional v2 development gates may include version sync, route coverage, UI action map coverage, installer build, installed runtime verification, admin rollback smoke, hardware matrix testing, checksum generation, and secret scan.

## Release Policy

| Version line | Public status | Policy |
| --- | --- | --- |
| v1.3.0 | Stable | Current recommended public stable baseline |
| v1.4.x | Legacy / experimental unless fully validated | Keep as historical feature-expansion evidence, not the default public stable |
| v2.0.x | Development preview | Do not promote as stable public release |
| v2.10.0 | Next stable candidate | Stable label allowed only after full parity, UI/UX, backend, installer, smoke, signing/checksum, and release-gate validation |

## License Status

No currently documented public stable feature is described as license-locked here. Paid licensing, activation servers, license enforcement, and cloud account features are roadmap-only unless a future validated release states otherwise.

## Documentation

- [Release notes v1.3.0](RELEASE_NOTES_v1.3.0.md)
- [Changelog](CHANGELOG.md)
- [User Guide](USER_GUIDE.md)
- [Troubleshooting](TROUBLESHOOTING.md)
- [Security](SECURITY.md)
- [Roadmap](ROADMAP.md)
- [API Reference](docs/API_REFERENCE.md)

## Disclaimer

HyperBoostX provides safe local optimization guidance and supported reversible actions where implemented. Hardware, drivers, Windows configuration, permissions, games, and background software differ per PC. No FPS, ping, latency, or stability improvement is guaranteed.

## Project Status

Public stable baseline: **v1.3.0 Stable**.

Current development work: **v2.x Development Preview / Next Major Rebuild**.

Future candidate: **v2.10.0**, only after full feature parity, UI/UX restoration, backend audit, installer validation, real smoke testing, and release gate approval.

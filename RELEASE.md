# HyperBoostX v1.3.0 Release Guide

Target release: `HyperBoostX v1.3.0 Stable`
Tag target: `v1.3.0`
Branch: `main`

## Stable Rule

Do not publish a stable release unless critical gates pass. If a critical gate fails and cannot be fixed in the run, report `BLOCKED` and do not create the GitHub Release.

Full multi-machine Windows lab compatibility is not claimed unless the exact lab result is recorded in `QA_RESULTS.md`.

## Public Assets

Recommended public asset:

- `HyperBoostXInstaller.exe`

Optional public asset:

- `SHA256SUMS.txt`

Do not publish raw backend executables, raw launcher executables, debug packages, temp folders, logs, cache files, local state, or internal CI artifacts as normal-user downloads.

Recommended release-page text:

```text
Download HyperBoostXInstaller.exe, run it, and follow the installer.
```

## Required Validation Gates

- `scripts\verify_repo.ps1`
- Python tests
- Python warning-as-error when supported
- `.NET` restore, build, Release build, and tests
- WPF build
- backend build
- launcher build
- release package
- installer build
- packaged backend health with version `1.3.0`
- installer install, installed launch, close, uninstall, reinstall, launch
- no orphan process after close
- secret scan
- Safety Guard validation
- restore/undo validation
- AI approval validation
- NVIDIA/AMD/Intel/MicrosoftBasic/unknown GPU fallback tests
- docs synchronized
- checksum generation and verification for published assets
- support docs, FAQ, troubleshooting, roadmap, and local crash report redaction tests

## v1.3.0 Release Scope

- Universal GPU detection and profile classification.
- GPU Center backend contract.
- Hardware profile engine and readiness scores.
- Before/after report contract and export.
- Safe boost plan/apply/undo flow with approval required.
- Local backend job queue.
- Launcher-generated local API session token.
- CORS restricted to localhost origins.
- Installer metadata updated to `1.3.0`.
- Public release hygiene focused on installer plus optional checksum.
- Local crash report export with redaction.
- Support docs, FAQ, troubleshooting, and roadmap for post-release users.

## Known Limitations

- The WPF UI remains a large shell with legacy pages; v1.3.0 adds verified backend contracts and metadata needed for GPU Center/AI Doctor rather than a complete MVVM rewrite of every legacy view.
- Temperature, VRAM usage, and driver details depend on Windows/driver/WMI availability.
- Multi-machine lab validation must be run separately before claiming broad hardware certification.
- Code signing is not available in this workspace; unsigned installers may show Unknown Publisher or SmartScreen.
- Auto updater, public website, opt-in telemetry, and license activation are roadmap-only items.
- GitHub release creation depends on authenticated GitHub permissions.

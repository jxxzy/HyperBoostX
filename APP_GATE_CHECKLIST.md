# App Gate Checklist - HyperBoostX v1.3.0 Stable

## Version Gate

- [x] `VERSION` is `1.3.0`.
- [x] Backend health/version endpoints report `1.3.0`.
- [x] WPF metadata is `1.3.0`.
- [x] Launcher metadata is `1.3.0`.
- [x] Installer metadata is `1.3.0`.
- [x] README and release docs target `v1.3.0`.

## Safety Gate

- [x] Safety Guard risky-action list remains documented.
- [x] Boost apply requires user approval.
- [x] Unknown GPU fallback stays conservative.
- [x] No code path added for overclock, undervolt, voltage, BIOS/UEFI edits, forced Defender disable, or forced GPU driver service disable.

## API Gate

- [x] `GET /api/health`.
- [x] `GET /api/version`.
- [x] `GET /api/system/stats`.
- [x] `GET /api/system/info`.
- [x] `GET /api/system/startup`.
- [x] `GET /api/system/processes`.
- [x] `GET /api/hardware/profile`.
- [x] `GET /api/hardware/gpu`.
- [x] `GET /api/hardware/vendors`.
- [x] `GET /api/hardware/overlays`.
- [x] `POST /api/boost/plan`.
- [x] `POST /api/boost/apply`.
- [x] `POST /api/boost/undo`.
- [x] `GET /api/reports/latest`.
- [x] `POST /api/reports/export`.
- [x] `POST /api/reports/crash-export`.
- [x] `POST /api/jobs/start`.
- [x] `GET /api/jobs/{id}`.
- [x] `POST /api/jobs/{id}/cancel`.

## Test Gate

- [x] Python tests PASS: `43 passed`.
- [x] .NET tests PASS: `28 passed`.
- [x] WPF Debug build PASS.
- [x] Support docs/FAQ/roadmap tests PASS.
- [x] Crash report redaction tests PASS.
- [x] Release package PASS.
- [x] Installer build PASS.
- [x] Installer install/uninstall/reinstall PASS.
- [x] Packaged backend health PASS with version `1.3.0`.
- [x] Portable launch smoke PASS.
- [x] Secret scan PASS.
- [x] SHA256SUMS verification PASS.

## Lab Matrix

Not claimed in this workspace unless separately recorded in `QA_RESULTS.md`.

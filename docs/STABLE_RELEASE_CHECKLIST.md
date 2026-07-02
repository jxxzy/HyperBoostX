# Stable Release Checklist - HyperBoostX v2.10.0

Status date: 2026-07-01
Decision: `STABLE_READY_UNSIGNED`

## Completed Gates

- [x] Version synchronized to `2.10.0`.
- [x] Windows file version synchronized to `2.10.0.0`.
- [x] Backend `/api/version` returns `2.10.0`.
- [x] WPF action map generated with 73 menus and 606 active actions.
- [x] Partial/roadmap/guidance action-map count is 0.
- [x] Backend route contract passed.
- [x] WPF button handler verifier passed.
- [x] Placeholder/fake UI guard passed.
- [x] WPF UI/UX quality verifier passed.
- [x] Real usability verifier passed.
- [x] Python tests passed: 72.
- [x] .NET tests passed: 38.
- [x] Debug and Release builds passed.
- [x] PowerShell syntax scan passed.
- [x] Secret scan passed.
- [x] Release package contents verified.
- [x] Installer rebuilt.
- [x] SHA256 manifests regenerated.
- [x] Fresh installed runtime gate passed.
- [x] Registry `DisplayVersion=2.10.0`.
- [x] Desktop shortcut exists.
- [x] Start Menu shortcut exists.
- [x] Installed backend `/api/health` passed.
- [x] Installed backend `/api/version` passed.
- [x] WPF installed smoke passed.
- [x] Token sync passed.
- [x] No orphan process passed.
- [x] Silent uninstall passed.
- [x] Silent reinstall passed.
- [x] Code signing status documented as `SKIPPED_BY_OWNER_NO_CERT`.

## Remaining Non-blocking Items

- [ ] Owner certificate/PFX for signed installer.
- [ ] External hardware matrix expansion across more NVIDIA, AMD, Intel, no-GPU, and low-end machines.
- [ ] GitHub tag/release publication after owner approval.

## Stable Rule

`2.10.0` may be treated as stable unsigned. Do not claim a signed release unless signing material is provided and the signed artifact is verified.

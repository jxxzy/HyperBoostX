# HyperBoostX v2.10.0 Stable Release Notes

Release date: 2026-07-02
Status: `STABLE_READY_UNSIGNED`

## Highlights

- Promoted runtime, backend, WPF, launcher, installer, and package metadata to `2.10.0`.
- Rebuilt the NSIS installer from fresh backend/WPF/launcher output.
- Rebuilt clean premium WPF content placement from the v1.3 information architecture.
- Added Beginner, Advanced, and Expert sidebar filtering without bypassing Safety Guard.
- Replaced core generic wrappers with placement-aware pages that keep raw backend payloads behind `Technical Details`.
- Rebuilt Dashboard, Settings, About, and Streaming Center after installed screenshot review found stale/template UI evidence.
- Added installed-app screenshot evidence captured from the real installed runtime after the owner admin gate.
- Hardened the installer runtime gate so stale source/installer/runtime evidence cannot pass.
- Passed full QA: Python `72 passed`, .NET `39 passed`.
- Passed UI release gate: navigation, button handlers, theme resources, placement matrix, fake metric guard, generic wrapper guard, and UI/UX quality.
- Passed owner admin stable gate against the installed artifact.
- Confirmed registry `DisplayVersion=2.10.0`.
- Confirmed installed backend `/api/health` and `/api/version`.
- Confirmed installed feature registry via `/api/features/audit`, `/api/features/stable-visible`, and `/api/features/non-real`.
- Confirmed stable-visible feature count `72`, stable-visible button count `596`, and non-real stable-visible count `0`.
- Confirmed Desktop and Start Menu shortcuts.
- Confirmed WPF installed smoke, token sync, no orphan process, silent uninstall, and silent reinstall.
- Cleaned release readiness API so installed stable runtime reports `stable_ready_unsigned`.

## Feature Surface

- 72 menus.
- 596 active buttons/actions.
- 0 stable action-map buttons marked partial/roadmap/guidance.
- 165 unique UI endpoints.
- 366 backend Flask routes and 384 backend route methods.

## Safety

Safety Guard remains active. HyperBoostX blocks forced Defender disable, permanent Windows Update disable, anti-cheat tweaks, driver service disable, BIOS/OC/undervolt/voltage changes, destructive user-file deletion, protected process kill, and arbitrary AI shell execution.

## Unsigned Installer Notice

Code signing is `SKIPPED_BY_OWNER_NO_CERT`. Windows SmartScreen may warn because the installer is unsigned.

Verify SHA256 before installing:

```text
e0846546df9f62cb8a6a0d42d1d9d8cfc7dbb835632f47177c64bb931d8b9609  HyperBoostXInstaller.exe
```

## Known Limitations

- External hardware matrix should still be expanded beyond this machine.
- OS-level admin apply/rollback remains guarded and limited to supported flows.
- Full RGB device lighting control, production cloud sync, production license server, and automatic driver installation are not claimed.


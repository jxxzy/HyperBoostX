# HyperBoostX v2.10.0 Stable Release Notes

Release date: 2026-07-01
Status: `STABLE_READY_UNSIGNED`

## Highlights

- Promoted runtime, backend, WPF, launcher, installer, and package metadata to `2.10.0`.
- Rebuilt the NSIS installer from fresh backend/WPF/launcher output.
- Passed full QA: Python `72 passed`, .NET `38 passed`.
- Passed owner admin stable gate against the installed artifact.
- Confirmed registry `DisplayVersion=2.10.0`.
- Confirmed installed backend `/api/health` and `/api/version`.
- Confirmed Desktop and Start Menu shortcuts.
- Confirmed WPF installed smoke, token sync, no orphan process, silent uninstall, and silent reinstall.
- Cleaned release readiness API so installed stable runtime reports `stable_ready_unsigned`.

## Feature Surface

- 72 menus.
- 596 active buttons/actions.
- 0 stable action-map buttons marked partial/roadmap/guidance.
- 165 unique UI endpoints.
- 365 backend route rules and 361 unique backend API paths.

## Safety

Safety Guard remains active. HyperBoostX blocks forced Defender disable, permanent Windows Update disable, anti-cheat tweaks, driver service disable, BIOS/OC/undervolt/voltage changes, destructive user-file deletion, protected process kill, and arbitrary AI shell execution.

## Unsigned Installer Notice

Code signing is `SKIPPED_BY_OWNER_NO_CERT`. Windows SmartScreen may warn because the installer is unsigned.

Verify SHA256 before installing:

```text
daec54b8ca059f9196c388811cd8ea0ad9fbff3c61f678f14bccd55f78ea3924  HyperBoostXInstaller.exe
```

## Known Limitations

- External hardware matrix should still be expanded beyond this machine.
- OS-level admin apply/rollback remains guarded and limited to supported flows.
- Full RGB device lighting control, production cloud sync, production license server, and automatic driver installation are not claimed.

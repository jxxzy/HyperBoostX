# HyperBoostX v2.10.0 Owner Lab Candidate Notes

Release date: 2026-07-02
Status: `OWNER_LAB_RELEASE_CANDIDATE`

## Highlights

- Promoted runtime, backend, WPF, launcher, installer, and package metadata to `2.10.0`.
- Rebuilt clean premium WPF content placement from the v1.3 information architecture.
- Added Beginner, Advanced, and Expert sidebar filtering without bypassing Safety Guard.
- Replaced core generic wrappers with placement-aware pages that keep raw backend payloads behind `Technical Details`.
- Added feature-specific content for shared PlacementChrome pages so route-backed pages match their menu intent.
- Replaced the hidden generic fallback action-map entry with a real `Action History` page.
- Passed current source QA: Python `73 passed`, .NET `40 passed`.
- UI/action map now exposes 73 menus, 606 active buttons/actions, 170 unique UI endpoints, and 0 partial/roadmap/guidance buttons.

## Current Gate

v2.10.0 is Owner Lab / Release Candidate, not public stable. Public stable remains blocked until refreshed installed-runtime, rollback/admin, hardware matrix, and signing/public-release evidence pass.

Required refreshed evidence:

- Rebuild installer/package from the latest source.
- Recheck registry `DisplayVersion=2.10.0`.
- Recheck installed backend `/api/health`, `/api/version`, and feature registry endpoints.
- Recheck Desktop and Start Menu shortcuts.
- Rerun WPF installed smoke, token sync, no orphan process, silent uninstall, and silent reinstall.
- Regenerate installed-app screenshot evidence from the refreshed installed app.

## Feature Surface

- 73 menus.
- 606 active buttons/actions.
- 0 stable action-map buttons marked partial/roadmap/guidance.
- 17 guarded destructive buttons.
- 170 unique UI endpoints.
- 366 backend Flask routes and 384 backend route methods.

## Safety

Safety Guard remains active. HyperBoostX blocks forced Defender disable, permanent Windows Update disable, anti-cheat tweaks, driver service disable, BIOS/OC/undervolt/voltage changes, destructive user-file deletion, protected process kill, and arbitrary AI shell execution.

## Unsigned Installer Notice

Code signing is `SKIPPED_BY_OWNER_NO_CERT`. Windows SmartScreen may warn because the installer is unsigned. Do not publish v2.10.0 as public stable until refreshed release evidence passes.

## Known Limitations

- External hardware matrix should still be expanded beyond this machine.
- OS-level admin apply/rollback remains guarded and limited to supported flows.
- Full RGB device lighting control, production cloud sync, production license server, and automatic driver installation are not claimed.

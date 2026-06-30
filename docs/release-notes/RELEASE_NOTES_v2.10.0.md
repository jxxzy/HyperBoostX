# HyperBoostX v2.10.0 Stable Release Notes

Status: `DRAFT_BLOCKED_UNTIL_OWNER_ADMIN_GATE_PASS`

Do not publish these notes as stable until installed runtime verification passes after stable promotion.

## Stable Requirements

Before this release can be marked stable:

- Registry `DisplayVersion` must be `2.10.0`.
- Backend `http://127.0.0.1:5000/api/health` must pass.
- Backend `http://127.0.0.1:5000/api/version` must return `2.10.0`.
- Desktop and Start Menu shortcuts must exist and target installed `HyperBoostX.exe`.
- WPF installed smoke, token sync, and no-orphan checks must pass.
- Fresh install, silent install, silent uninstall, and silent reinstall must pass.
- Safe rollback/admin gate must pass or be explicitly handled by owner approval.

## Unsigned Installer Notice

Code signing is `SKIPPED_BY_OWNER_NO_CERT`. Windows SmartScreen may warn because the installer is unsigned. Verify SHA256 before installing.

## Current Draft Limitation

The current source remains `2.10.0-beta.1` until owner admin gate passes and stable promotion is intentionally applied.


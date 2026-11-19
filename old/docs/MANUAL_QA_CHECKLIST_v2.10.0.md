# Manual QA Checklist v2.10.0

## Required Before Stable

- Fresh install.
- Reinstall.
- Silent install.
- Silent uninstall.
- Launch after install.
- Backend health after install.
- Admin mode test.
- Non-admin mode test.
- Restore/rollback test.
- UI smoke all Stable-visible menus.
- Scaling 100%, 125%, 150%.
- Small screen test.
- Report export JSON/TXT/MD.
- Crash/error redaction.
- NVIDIA/AMD/Intel/no GPU/low-end matrix.
- Code signing verification.
- No orphan process.
- No version mismatch.
- No non-real feature visible in Stable UI.

## Current Automated Status

Automated visibility and route tests pass. Installed runtime and hardware matrix remain manual.

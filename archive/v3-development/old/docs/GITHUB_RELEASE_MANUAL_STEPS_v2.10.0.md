# GitHub Release Manual Steps v2.10.0

Status: `BLOCKED_UNTIL_OWNER_ADMIN_GATE_PASS`

Do not publish `HyperBoostX v2.10.0 Stable` until the owner admin stable gate passes after stable promotion.

## Required Local Commands

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\owner_admin_stable_gate.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build_stable_release.ps1
```

## If GitHub Credentials Are Available

1. Commit the stable promotion.
2. Tag `v2.10.0`.
3. Create a GitHub Release titled `HyperBoostX v2.10.0 Stable`.
4. Attach:
   - `HyperBoostXInstaller.exe`
   - `SHA256SUMS.txt`
   - `docs/release-notes/RELEASE_NOTES_v2.10.0.md`

## If GitHub Credentials Are Unavailable

Create `release/final/v2.10.0/` and include:

- `HyperBoostXInstaller.exe`
- `SHA256SUMS.txt`
- `docs/release-notes/RELEASE_NOTES_v2.10.0.md`
- `docs/FINAL_AUDIT_REPORT_v2.10.0.md`
- `docs/RELEASE_GATE_RESULT.md`
- `STABLE_RELEASE_CHECKLIST.md`
- `docs/OWNER_ADMIN_STABLE_GATE_RESULT_v2.10.0.md`

## Unsigned Installer Policy

Code signing is `SKIPPED_BY_OWNER_NO_CERT`. The release must say the installer is unsigned, SmartScreen may warn, and SHA256 must be verified before install.


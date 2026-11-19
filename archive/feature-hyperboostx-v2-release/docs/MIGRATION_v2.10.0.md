# Migration v2.10.0

> Public release policy: HyperBoostX v1.3.0 is the current recommended public stable baseline. The 2.10.0-beta.1 runtime is a Beta development build and must not be promoted as stable until installed runtime, admin rollback, hardware matrix, code signing, checksum, and smoke gates pass.

Generated: 2026-06-28 01.16.15 +07:00

## Scope

v2.10.0-beta.1 keeps the WPF shell + local Flask backend + launcher token model. Migration work is about preserving local user data and making v2 routes/UI auditable, not rewriting architecture.

## Data Locations

- Default data root: %LocalAppData%\HyperBoost X.
- Portable mode: HYPERBOOSTX_PORTABLE_HOME.
- Must preserve config, reports, backups, profiles, sessions, diagnostics, action logs, and UI settings.

## Required Migration Behavior

- Corrupt JSON must be backed up and replaced with safe defaults.
- Old restore sessions must remain visible or be clearly marked unreadable without crashing.
- Local reports must remain exportable and redacted.
- Session-token mismatch must show a restart/retry message, not a generic crash.
- Runtime VERSION may be v2 beta while public README stable remains v1.3.0.

## Upgrade Smoke

| Path | Status |
| --- | --- |
| v1.3.0 to v2.10 beta | Manual lab required |
| v1.4.x to v2.10 beta | Manual lab required |
| v2.0.x to v2.10 beta | Source/package work in progress |
| Fresh install | Requires admin installer lab |
| Reinstall | Requires admin installer lab |
| Silent uninstall | Requires admin installer lab |


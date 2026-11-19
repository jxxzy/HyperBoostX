# Migration v2.10.0

Status: `OWNER_LAB_RELEASE_CANDIDATE`

Current v2.10 status: HyperBoostX v2.10.0 Owner Lab / Release Candidate. Public stable remains v1.3.0 until installed runtime, admin rollback, hardware matrix, and signing/public-release evidence pass.

Generated: 2026-07-03 20.38.52 +07:00

## Scope

HyperBoostX v2.10 keeps the WPF shell, local Flask backend, packaged launcher, and launcher/backend token model. Migration work preserves local user data while keeping UI actions, backend routes, reports, and restore visibility auditable.

## Data Locations

- Default data root: `%LocalAppData%\HyperBoost X`.
- Portable mode: `HYPERBOOSTX_PORTABLE_HOME`.
- Preserve config, reports, backups, profiles, sessions, diagnostics, action logs, and UI settings.

## Required Migration Behavior

- Corrupt JSON is backed up and replaced with safe defaults.
- Old restore sessions remain visible or are clearly marked unreadable without crashing.
- Local reports remain exportable and redacted.
- Session-token mismatch shows restart/retry guidance.
- Installed runtime must report version `2.10.0`.
- Stable runtime must expose 73 menus and 606 mapped buttons.

## Upgrade Smoke

| Path | Status |
| --- | --- |
| Fresh install | Blocked until refreshed installed-runtime and owner/admin evidence pass |
| Reinstall | Blocked until refreshed installed-runtime and owner/admin evidence pass |
| Silent uninstall | Blocked until refreshed installed-runtime and owner/admin evidence pass |
| Silent reinstall | Blocked until refreshed installed-runtime and owner/admin evidence pass |
| User data preservation | Supported by local-first data policy; broader external user-data matrix remains recommended |


# Migration v2.10.0

Status: `STABLE_READY_UNSIGNED`

Current public release: HyperBoostX v2.10.0 Stable Unsigned. Code signing remains SKIPPED_BY_OWNER_NO_CERT, so this generator must not claim signed artifacts.

Generated: 2026-07-03 02.57.27 +07:00

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
| Fresh install | PASS in owner/admin gate evidence |
| Reinstall | PASS in owner/admin gate evidence |
| Silent uninstall | PASS in owner/admin gate evidence |
| Silent reinstall | PASS in owner/admin gate evidence |
| User data preservation | Supported by local-first data policy; broader external user-data matrix remains recommended |


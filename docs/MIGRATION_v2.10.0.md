# Migration v2.10.0

Status: `STABLE_READY_UNSIGNED`

Current public release: HyperBoostX v2.10.0 Stable Unsigned.

Generated: 2026-07-01

## Scope

v2.10.0 keeps the WPF shell, local Flask backend, packaged launcher, and launcher/backend token model. Migration work preserves local user data while making UI actions, backend routes, reports, and restore visibility auditable.

## Data Locations

- Default data root: `%LocalAppData%\HyperBoost X`.
- Portable mode: `HYPERBOOSTX_PORTABLE_HOME`.
- Preserve config, reports, backups, profiles, sessions, diagnostics, action logs, and UI settings.

## Required Migration Behavior

- Corrupt JSON must be backed up and replaced with safe defaults.
- Old restore sessions must remain visible or be clearly marked unreadable without crashing.
- Local reports must remain exportable and redacted.
- Session-token mismatch must show a restart/retry message, not a generic crash.
- Installed runtime must report version `2.10.0`.
- Installed runtime must expose 72 stable-visible menus and 596 stable-visible buttons.

## Upgrade Smoke

| Path | Status |
| --- | --- |
| Fresh install | PASS in owner/admin gate evidence |
| Reinstall | PASS in owner/admin gate evidence |
| Silent uninstall | PASS in owner/admin gate evidence |
| Silent reinstall | PASS in owner/admin gate evidence |
| v1.x/v2.x user data preservation | Supported by installer policy; additional external hardware/user-data matrix recommended |

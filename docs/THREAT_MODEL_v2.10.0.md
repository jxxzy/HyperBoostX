# Threat Model v2.10.0

Status: `STABLE_READY_UNSIGNED`

Current public release: HyperBoostX v2.10.0 Stable Unsigned. Code signing remains SKIPPED_BY_OWNER_NO_CERT, so this generator must not claim signed artifacts.

Generated: 2026-07-03 02.57.27 +07:00

## Primary Assets

- Local session token.
- Restore metadata.
- Reports and action logs.
- Local config and user privacy.
- Windows system state.

## Main Threats

| Threat | Mitigation |
| --- | --- |
| Local process calls mutating endpoint | `X-HyperBoostX-Session` is enforced when a token is configured |
| UI button calls dead endpoint | Action-map route tests validate UI-used endpoints |
| Risky optimizer action | Safety Guard blocks Defender/Firewall disable, permanent Windows Update disable, anti-cheat tweaks, driver-service edits, BIOS/OC/undervolt, protected process kills, and unreviewed personal file deletion |
| Token or username leaks into reports | Crash/report redaction tests and docs require redacted output |
| Feature overclaim | Public docs keep local-first, preview-first, restore-aware wording |
| Expert mode bypasses safety | Expert mode exposes detail only; it does not bypass Safety Guard |

## Release Security Gate

Stable unsigned release evidence must include token rejection, route coverage, destructive-action blocking, report redaction, installer install/uninstall, no orphan process, checksums, and explicit unsigned status.


# Threat Model v2.10.0

> Current release policy: HyperBoostX v2.10.0 is the stable unsigned public release. Code signing remains `SKIPPED_BY_OWNER_NO_CERT`; no signed-release claim is made.

## Primary Assets

- Local session token.
- Restore metadata.
- Reports and action logs.
- Local config and user privacy.
- Windows system state.

## Main Threats

| Threat | Mitigation |
| --- | --- |
| Local process calls mutating endpoint | X-HyperBoostX-Session enforced when token is present |
| UI button calls dead endpoint | tests/test_ui_action_map_v210.py validates action map routes |
| Risky optimizer action | Safety Guard blocks Defender, permanent Update disable, anti-cheat, driver service, BIOS/OC/undervolt, destructive cleanup |
| Token or username leaks into reports | Crash/report redaction tests and docs require redacted output |
| Feature overclaim | README and action map require Real-only public feature status |
| Expert mode bypasses safety | Explicit policy: expert mode never bypasses Safety Guard |
| Signed/unsigned confusion | Code signing readiness is documented; v2.10.0 is labeled Stable Unsigned |

## Release Security Gate

Stable unsigned gate requires token rejection, route coverage, destructive-action blocking, report redaction, installer install/uninstall, feature-registry counts, and checksum evidence. Signed release remains blocked until owner signing material is supplied.

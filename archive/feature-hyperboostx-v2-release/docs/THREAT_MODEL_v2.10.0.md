# Threat Model v2.10.0

> Public release policy: HyperBoostX v1.3.0 is the current recommended public stable baseline. The 2.10.0-beta.1 runtime is a Beta development build and must not be promoted as stable until installed runtime, admin rollback, hardware matrix, code signing, checksum, and smoke gates pass.

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
| Signed/unsigned confusion | Code signing readiness is documented; unsigned beta must stay labeled |

## Release Security Gate

Stable is NO-GO until token rejection, route coverage, destructive-action blocking, report redaction, installer install/uninstall, admin rollback, hardware matrix, and signing/checksum evidence are present.


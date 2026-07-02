# Release Gate Result

Audit date: 2026-07-02
Candidate version: `2.10.0`
Branch: `main`
Decision: `STABLE_READY_UNSIGNED`

## Gate Table

| Gate | Status | Evidence |
| --- | --- | --- |
| Version sync | PASS | `docs/runtime-audit/version_sync_report.json`, expected `2.10.0`, Windows file version `2.10.0.0`. |
| Secret scan | PASS | Full QA realistic token/webhook/private-key scan passed. |
| PowerShell syntax | PASS | Full QA PSParser scan passed. |
| .NET Release build/test | PASS | `39 passed`. |
| Python pytest | PASS | `72 passed`. |
| Backend route contract | PASS | `docs/runtime-audit/backend_routes_report.json`. |
| WPF UI/UX quality | PASS | Button handler, placeholder guard, and UI quality verifier passed. |
| Real usability | PASS | Route, WPF handler, placeholder, and .NET contract gates passed. |
| Release artifact contents | PASS | `docs/runtime-audit/release_artifact_contents_report.json`. |
| Installer package | PASS | `HyperBoostXInstaller.exe` rebuilt from fresh `release/package`. |
| Installed runtime | PASS | `docs/runtime-audit/owner_admin_stable_gate_report.json`. |
| Registry DisplayVersion | PASS | Installed registry reports `2.10.0`. |
| Desktop shortcut | PASS | Public Desktop shortcut exists and targets installed launcher. |
| Start Menu shortcut | PASS | Start Menu shortcut exists and targets installed launcher. |
| Backend health/version | PASS | Installed `/api/health` and `/api/version` pass on port `5000`. |
| Runtime feature registry | PASS | `/api/features/audit`, `/api/features/stable-visible`, and `/api/features/non-real` must match the v2.10.0 contract. |
| Package action map | PASS | Package verifier requires `release/package/wpf/Data/ui_action_map_v2_10.json` and validates JSON counts/status/safety. |
| Public evidence redaction | PASS | Public evidence must not expose raw repo path, user profile path, local username principal, tokens, or webhooks. |
| WPF installed smoke | PASS | Installed launcher/WPF/backend smoke passed. |
| Token sync | PASS | Token-required backend health plus WPF running from launcher passed. |
| No orphan process | PASS | Installed processes were stopped and no orphan remained. |
| Silent uninstall/reinstall | PASS | Owner admin stable gate passed both. |
| Installed screenshot evidence | PASS | `docs/runtime-audit/installed_screenshot_report.json` captured all 28 core pages plus Dashboard after-scroll from the installed app. |
| Final stable release gate | PASS | `docs/runtime-audit/final_stable_release_gate_report.json` reports `FINAL_STABLE_PASS`. |
| Code signing | SKIPPED_BY_OWNER_NO_CERT | No owner certificate/PFX was supplied; unsigned distribution requires checksum verification. |

## Current Metrics

| Metric | Count |
| --- | ---: |
| Total menu | 73 |
| Total buttons | 606 |
| Active buttons | 606 |
| Partial/roadmap/guidance buttons | 0 |
| Guarded destructive buttons | 21 |
| Unique UI endpoints used | 167 |
| Backend Flask routes | 366 |
| Backend route methods | 384 |
| Stable visible features | 73 |
| Stable visible buttons | 606 |
| Non-real visible in stable | 0 |
| Python tests passed | 72 |
| .NET tests passed | 39 |

## Release Artifact

```text
e0846546df9f62cb8a6a0d42d1d9d8cfc7dbb835632f47177c64bb931d8b9609  HyperBoostXInstaller.exe
```

Checksum manifests:

- `docs/release/checksums/SHA256SUMS.txt`
- `docs/release/checksums/SHA256SUMS_2.10.0.txt`

## Decision

`2.10.0` is approved as a local stable unsigned build by the completed source/package/install/runtime gates. It remains unsigned until the owner provides signing material.


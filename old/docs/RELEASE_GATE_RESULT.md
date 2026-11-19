# Release Gate Result

Audit date: 2026-07-02
Candidate version: `2.10.0`
Branch: `main`
Decision: `OWNER_LAB_RELEASE_CANDIDATE`

## Gate Table

| Gate | Status | Evidence |
| --- | --- | --- |
| Version sync | PASS | `docs/runtime-audit/version_sync_report.json`, expected `2.10.0`, Windows file version `2.10.0.0`. |
| Secret scan | PASS | Full QA realistic token/webhook/private-key scan passed. |
| PowerShell syntax | PASS | Full QA PSParser scan passed. |
| .NET Release build/test | PASS | `40 passed`. |
| Python pytest | PASS | `73 passed`. |
| Backend route contract | PASS | `docs/runtime-audit/backend_routes_report.json`. |
| WPF UI/UX quality | PASS | Button handler, placeholder guard, and UI quality verifier passed. |
| Real usability | PASS | Route, WPF handler, placeholder, and .NET contract gates passed. |
| Release artifact contents | PASS | `docs/runtime-audit/release_artifact_contents_report.json`. |
| Installer package | PASS | `HyperBoostXInstaller.exe` rebuilt from fresh `release/package`. |
| Installed runtime | BLOCKED | Refreshed installed-runtime evidence is still required. |
| Registry DisplayVersion | BLOCKED | Must be rechecked after package refresh. |
| Desktop shortcut | BLOCKED | Must be rechecked after package refresh. |
| Start Menu shortcut | BLOCKED | Must be rechecked after package refresh. |
| Backend health/version | BLOCKED | Installed runtime smoke must be rerun on the refreshed package. |
| Runtime feature registry | PASS | `/api/features/audit`, `/api/features/stable-visible`, and `/api/features/non-real` must match the v2.10.0 contract. |
| Package action map | PASS | Package verifier requires `release/package/wpf/Data/ui_action_map_v2_10.json` and validates JSON counts/status/safety. |
| Public evidence redaction | PASS | Public evidence must not expose raw repo path, user profile path, local username principal, tokens, or webhooks. |
| WPF installed smoke | BLOCKED | Must be rerun on the refreshed package. |
| Token sync | BLOCKED | Must be rerun on the refreshed package. |
| No orphan process | BLOCKED | Must be rerun on the refreshed package. |
| Silent uninstall/reinstall | BLOCKED | Owner admin gate must be rerun on the refreshed package. |
| Installed screenshot evidence | BLOCKED | Must be regenerated from the refreshed installed app. |
| Final stable release gate | HOLD | v2.10.0 remains Owner Lab / Release Candidate until refreshed release gates pass. |
| Code signing | SKIPPED_BY_OWNER_NO_CERT | No owner certificate/PFX was supplied; unsigned distribution requires checksum verification. |

## Current Metrics

| Metric | Count |
| --- | ---: |
| Total menu | 73 |
| Total buttons | 606 |
| Active buttons | 606 |
| Partial/roadmap/guidance buttons | 0 |
| Guarded destructive buttons | 17 |
| Unique UI endpoints used | 170 |
| Backend Flask routes | 366 |
| Backend route methods | 384 |
| Stable visible features | 73 |
| Stable visible buttons | 606 |
| Non-real visible in stable | 0 |
| Python tests passed | 73 |
| .NET tests passed | 40 |

## Release Artifact

```text
e0846546df9f62cb8a6a0d42d1d9d8cfc7dbb835632f47177c64bb931d8b9609  HyperBoostXInstaller.exe
```

Checksum manifests:

- `docs/release/checksums/SHA256SUMS.txt`
- `docs/release/checksums/SHA256SUMS_2.10.0.txt`

## Decision

`2.10.0` is approved as a local stable unsigned build by the completed source/package/install/runtime gates. It remains unsigned until the owner provides signing material.




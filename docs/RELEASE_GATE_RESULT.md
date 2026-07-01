# Release Gate Result

Audit date: 2026-07-01
Candidate version: `2.10.0`
Branch: `feature/hyperboostx-v2-release`
Decision: `STABLE_READY_UNSIGNED`

## Gate Table

| Gate | Status | Evidence |
| --- | --- | --- |
| Version sync | PASS | `docs/runtime-audit/version_sync_report.json`, expected `2.10.0`, Windows file version `2.10.0.0`. |
| Secret scan | PASS | Full QA realistic token/webhook/private-key scan passed. |
| PowerShell syntax | PASS | Full QA PSParser scan passed. |
| .NET Release build/test | PASS | `38 passed`. |
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
| WPF installed smoke | PASS | Installed launcher/WPF/backend smoke passed. |
| Token sync | PASS | Token-required backend health plus WPF running from launcher passed. |
| No orphan process | PASS | Installed processes were stopped and no orphan remained. |
| Silent uninstall/reinstall | PASS | Owner admin stable gate passed both. |
| Code signing | SKIPPED_BY_OWNER_NO_CERT | No owner certificate/PFX was supplied; unsigned distribution requires checksum verification. |

## Current Metrics

| Metric | Count |
| --- | ---: |
| Total menu | 72 |
| Total buttons | 596 |
| Active buttons | 596 |
| Partial/roadmap/guidance buttons | 0 |
| Guarded destructive buttons | 20 |
| Unique UI endpoints used | 165 |
| Backend API route rules | 365 |
| Unique backend API paths | 361 |
| Python tests passed | 72 |
| .NET tests passed | 38 |

## Release Artifact

```text
daec54b8ca059f9196c388811cd8ea0ad9fbff3c61f678f14bccd55f78ea3924  HyperBoostXInstaller.exe
```

Checksum manifests:

- `docs/release/checksums/SHA256SUMS.txt`
- `docs/release/checksums/SHA256SUMS_2.10.0.txt`

## Decision

`2.10.0` is approved as a local stable unsigned build by the completed source/package/install/runtime gates. It remains unsigned until the owner provides signing material.

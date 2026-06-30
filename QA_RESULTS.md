# QA Results - HyperBoostX v2.10.0-beta.1

Date: 2026-07-01
Branch: `feature/hyperboostx-v2-release`
Decision: `BETA_READY` / source-package stable candidate. Not public stable.

## Final Automated Gate

| Gate | Result | Evidence |
| --- | --- | --- |
| Full QA gate | PASS WITH INSTALL SKIP | `artifacts/qa/full_qa_summary.json` status `BETA_READY`. |
| Python tests | PASS | `72 passed` via `app\venv\Scripts\python.exe -m pytest -q tests`. |
| .NET tests | PASS | `38 passed` in Debug and Release gate runs. |
| Solution build | PASS | `dotnet build HyperBoostX.sln -v minimal` completed with `0 Warning(s), 0 Error(s)`. |
| Release build/test | PASS | Full QA Release build/test passed. |
| Backend route contract | PASS | `runtime_audit/backend_routes_report.json`; route smoke `8 passed`. |
| WPF UI/UX quality | PASS | Button handler verifier, placeholder guard, and UI/UX quality verifier passed. |
| Real usability | PASS | Route contract, WPF handlers, placeholder guard, and .NET contract tests passed. |
| Version sync | PASS | `runtime_audit/version_sync_report.json`, expected `2.10.0-beta.1` and Windows file version `2.10.0.0`. |
| Release artifact contents | PASS | `runtime_audit/release_artifact_contents_report.json`. |
| Secret scan | PASS | Full QA realistic token/webhook/private-key scan found no hits. |
| PowerShell syntax | PASS | Full QA PSParser scan passed. |
| Installer rebuild | PASS | `scripts/package_installer_v2.10.0.ps1` rebuilt installer and checksum manifest. |
| Installed runtime verification | FAIL/BLOCKER | `runtime_audit/runtime_audit_report.md`; installed registry reports `1.3.0` and backend health was not found. |

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

## Release Artifacts

| Artifact | Status |
| --- | --- |
| `HyperBoostXInstaller.exe` | Rebuilt locally. |
| `release/package` | Synced from fresh local deploy output. |
| `release/app` | Synced from fresh local deploy output. |
| `SHA256SUMS.txt` | Updated. |
| `SHA256SUMS_v2.10.0-beta.1.txt` | Updated. |

Installer SHA256:

```text
05e689131175efd1acfe40e6995b014f48228cd4156fcf99724283a3887f5a6d  HyperBoostXInstaller.exe
```

## Known Blockers Before Public Stable

- Installed runtime fresh install/reinstall/silent uninstall must be run as admin with the rebuilt installer.
- Admin apply/rollback lab is not executed in this session.
- Hardware matrix across NVIDIA/AMD/Intel/no-GPU/low-end machines is not executed.
- Code signing is `SKIPPED_BY_OWNER_NO_CERT`; unsigned distribution requires explicit owner approval and checksum verification.
- No stable tag or GitHub Release was created.

Public README policy remains: v1.3.0 is the recommended public stable baseline; v2.10.0-beta.1 is a beta/stable-candidate build for validation.

# Test Results Audit

Audit date: 2026-06-27

| Gate | Result | Evidence |
| --- | --- | --- |
| Python backend tests | PASS | `60 passed` via `app\venv\Scripts\python.exe -m pytest -q tests`. |
| .NET desktop tests | PASS | `35 passed` via `dotnet test dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj -c Debug`. |
| Full repo verify | PASS | `scripts/verify_repo.ps1`. |
| Backend route smoke | PASS | `runtime_audit/backend_routes_report.json`, `6 passed`. |
| WPF navigation | PASS | `runtime_audit/wpf_navigation_report.json`. |
| WPF button handlers | PASS | `scripts/verify_wpf_button_handlers.ps1`. |
| Placeholder/fake UI guard | PASS | `scripts/verify_placeholder_guard.ps1`. |
| UI/UX quality | PASS | `scripts/verify_ui_ux_quality.ps1`. |
| Real usability | PASS | `scripts/verify_real_usability.ps1`. |
| Pre-v2 preservation | PASS | `scripts/verify_pre_v2_feature_preservation.ps1`. |
| Version sync | PASS | `runtime_audit/version_sync_report.json`. |
| Release artifact contents | PASS | `runtime_audit/release_artifact_contents_report.json`. |
| Clean install dry-run | PASS | `runtime_audit/clean_install_verify_report.json`. |
| Installed runtime audit | BLOCKED | `runtime_audit/runtime_audit_report.json` shows installed registry `1.3.0`. |

## Note

Installed runtime failure is not a source failure. The rebuilt installer must be run on this machine with admin rights before installed runtime can pass.


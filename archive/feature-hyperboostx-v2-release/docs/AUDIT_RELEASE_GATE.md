# Release Gate Audit

Audit date: 2026-06-27
Version: `2.0.1`

## Artifact Status

| Artifact | Status | Evidence |
| --- | --- | --- |
| Backend exe | BUILT | `release/package/backend/hyperboost_backend.exe` |
| WPF runtime | BUILT | `release/package/wpf/HyperBoostX.exe` |
| Launcher | BUILT | `release/package/launcher/HyperBoostLauncher.exe` |
| Portable app | BUILT | `release/app/HyperBoostX.exe` |
| Installer | BUILT | `HyperBoostXInstaller.exe`, ProductVersion `2.0.1`, length `144316202`. |
| SHA256 | GENERATED | `SHA256SUMS.txt`. |

## SHA256

`HyperBoostXInstaller.exe`: `73e24cf5ae886166333e417bba32c3d25c47b6ec61df00836f096231566b4796`

## Gate Status

| Gate | Status |
| --- | --- |
| Source tests | PASS |
| Debug build | PASS |
| Release package build | PASS |
| NSIS installer build | PASS |
| Hash verification | PASS |
| Clean install dry-run | PASS |
| Installed runtime validation | BLOCKED_BY_OWNER_INSTALL |
| Code signing | SKIPPED_BY_OWNER_NO_CERT |
| GitHub release publish | BLOCKED_BY_OWNER_CREDENTIALS |

## Release Decision

Status: PARTIAL. Source and artifacts are ready for owner install validation. Do not mark public stable DONE until `scripts/clean_install_verify.ps1 -Execute` and `scripts/verify_installed_runtime.ps1` pass on the installed `2.0.1` runtime.



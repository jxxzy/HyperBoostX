# Installer Gate Report

Audit date: 2026-06-27

| Gate | Status | Evidence |
| --- | --- | --- |
| Release package layout | PASS | `release/package` contains backend, launcher, WPF. |
| Portable layout | PASS | `release/app/HyperBoostX.exe` exists. |
| NSIS installer build | PASS | `HyperBoostXInstaller.exe`, ProductVersion `2.0.1`. |
| SHA256 | PASS | `SHA256SUMS.txt` verified. |
| Clean install dry-run | PASS | `runtime_audit/clean_install_verify_report.json`. |
| Installed runtime | BLOCKED | Local HKLM uninstall key still reports `DisplayVersion=1.3.0`. |
| Desktop shortcut | BLOCKED | Installed-runtime report shows old desktop shortcut missing. |
| Backend installed health | BLOCKED | Old installed backend did not respond. |

## Decision

Installer/package build is PASS. Installed runtime validation is BLOCKED_BY_OWNER_INSTALL until `HyperBoostXInstaller.exe` is run as administrator and `verify_installed_runtime.ps1` passes.


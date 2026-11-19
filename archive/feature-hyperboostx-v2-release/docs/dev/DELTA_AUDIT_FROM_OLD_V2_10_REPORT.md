# Delta Audit From Old v2.10 Report

Generated: 2026-07-01 03:36 +07:00

| Area | Old report | Current source |
| --- | ---: | ---: |
| UI menus | 66 | 72 |
| Active UI buttons | 540 | 596 |
| Partial/roadmap buttons | 0 | 0 |
| Unique UI endpoints | 138 | 165 |
| Backend API route rules | 318 | 365 |
| Unique backend API paths | 314 | 361 |
| Python tests | 66 | 72 |
| .NET tests | 38 | 38 |

## Added Since Old Report

- System Reality Guard backend blueprint.
- LCD Performance Guard diagnostics for KANALI/TRCC/HiMOS style helper roles.
- Defender Scan Guard status, performance sample, exclusion preview, apply guard, and undo endpoint shell.
- CPU Turbo Diagnostic for base/current frequency, load, power plan, MSI mode, thermal and power-limit causes.
- MSI Safe Optimizer diagnostics and safe recommendations.
- Security Reality Audit for WSL, remote access, startup, PowerShell activity, and vendor service classification.
- Runtime verifier that checks registry, shortcuts, backend health/version, token inference, WPF launch smoke, and orphan process status.
- Installer payload verifier and clean-install dry-run/elevated lab script.

## Still Not Promoted To Stable

The source/package gates pass, but installed runtime verification does not. The local installed registry still reports `1.3.0`; therefore the correct status remains `SOURCE_BETA_READY` and `PUBLIC_STABLE_BLOCKED`.


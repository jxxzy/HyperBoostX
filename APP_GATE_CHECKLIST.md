# APP GATE CHECKLIST - HyperBoostX v1.2.13 Stable

Date: 2026-06-26
Branch: `main`

## Release Identity

- [x] VERSION synced to `1.2.13`
- [x] Tag target is `v1.2.13`
- [x] Release name is `HyperBoostX v1.2.13 Stable`
- [x] README current stable points to `v1.2.13`
- [x] About/app metadata points to `1.2.13`
- [x] Installer version points to `1.2.13`
- [x] Portable/runtime version points to `1.2.13`

## Critical Gate

- [x] No known Critical bug remains open
- [x] No known Major bug remains open
- [x] No release metadata mismatch remains
- [x] No source-vs-runtime mismatch remains
- [x] No broken active release/gate doc reference remains

## NVIDIA AI Gate

- [x] Legacy AI runtime dependency removed from active runtime
- [x] NVIDIA Copilot active
- [x] NVIDIA API provider configured
- [x] 10 NVIDIA models present
- [x] NVIDIA API key stored only in Windows Credential Manager for real test
- [x] NVIDIA API key not written to plaintext repo/config/log/state/crash report
- [x] Real NVIDIA connection test PASS from Windows Credential Manager
- [x] Default model PASS: `nvidia/nemotron-3-nano-30b-a3b`
- [x] Fallback model PASS: `nvidia/nvidia-nemotron-nano-9b-v2`
- [x] Auto fallback path covered by provider/service tests
- [x] Safety Guard verified
- [x] AI approval flow verified

## Safety Gate

- [x] AI cannot execute system action without user approval
- [x] High-risk actions require Safety Guard
- [x] One Click Boost safe by default
- [x] Restore/Undo metadata available where applicable
- [x] Registry tweaks have backup metadata coverage
- [x] Service/risky action requests without restore metadata are blocked or downgraded
- [x] DNS/network destructive reset not run during current-machine matrix
- [x] Startup changes have restore/undo coverage in regression tests
- [x] Power plan changes have backup metadata coverage
- [x] No forced Defender disable
- [x] No permanent Windows Update disable
- [x] No destructive cleanup target in matrix; cleanup used safe temp scope

## Build Gate

- [x] Python tests PASS - `24 passed, 0 warnings`
- [x] Python warning-as-error PASS
- [x] .NET tests PASS - `27 passed`
- [x] WPF Debug build PASS
- [x] WPF/launcher Release build PASS
- [x] Launcher build PASS
- [x] Backend package PASS
- [x] Portable package PASS
- [x] Installer build PASS

## Runtime Gate

- [x] Portable app launches
- [x] Installed app launches
- [x] Packaged backend health check PASS
- [x] Installed backend health check PASS
- [x] App exits cleanly
- [x] No backend/UI/launcher orphan process

## Installer Gate

- [x] Elevated automation shell available
- [x] Silent installer install PASS
- [x] Install path and installed exe verified
- [x] HKLM uninstall metadata verified with version `1.2.13`
- [x] Silent uninstall PASS
- [x] Install directory and uninstall registry cleanup verified
- [x] Silent reinstall PASS
- [x] Installed app relaunch after reinstall PASS

## QA Gate

- [x] Feature Audit regression PASS
- [x] Full QA Matrix regression PASS
- [x] Dashboard/window smoke test PASS
- [x] Safe boost safe-mode/profile load PASS
- [x] Cleanup safe test PASS
- [x] Network safe DNS test PASS
- [x] Restore/Undo metadata check PASS
- [x] NVIDIA AI connection test PASS
- [x] 10 model registry test PASS
- [x] AI approval flow test PASS
- [x] Safety Guard test PASS
- [x] Current-machine automated matrix PASS

## Release Asset Gate

- [x] Public GitHub Release exposes one installer download: `HyperBoostXInstaller.exe`
- [x] Installer asset generated
- [x] Internal QA portable asset generated
- [x] Internal QA backend asset generated
- [x] Internal QA launcher asset generated
- [x] SHA256SUMS.txt generated for local verification
- [x] Checksums verified against local final assets

## Matrix Scope

Full multi-machine Windows lab matrix: NOT CLAIMED
Current-machine automated matrix: PASS

## Final Statement

Automated validation passed.
Zero known Critical/Major bugs after automated validation.

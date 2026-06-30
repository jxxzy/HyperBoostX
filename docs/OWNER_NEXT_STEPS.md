# Owner Next Steps

Audit date: 2026-06-27

## Required Before Public Stable Release

1. Run the rebuilt installer as administrator: `HyperBoostXInstaller.exe`.
2. Run installed validation: `powershell -ExecutionPolicy Bypass -File .\scripts\verify_installed_runtime.ps1`.
3. If doing a destructive install smoke, run: `powershell -ExecutionPolicy Bypass -File .\scripts\clean_install_verify.ps1 -Execute`.
4. Confirm installed registry `DisplayVersion` is `2.0.1`.
5. Confirm desktop and Start Menu shortcuts launch the new launcher.
6. Launch WPF as normal user and admin; smoke every sidebar page.
7. Test hardware-dependent pages on NVIDIA, AMD, Intel, laptop, and desktop machines.
8. Add owner credentials only locally for NVIDIA provider and Discord webhook live tests.
9. Sign installer if a code-signing certificate is available.
10. Commit, tag, push, and draft GitHub release after installed validation passes.

## Current Blockers

| Blocker | Owner action |
| --- | --- |
| Installed runtime audit shows local installed `1.3.0` | Install rebuilt `2.0.1` package. |
| Code signing unavailable | Provide certificate or mark unsigned release clearly. |
| GitHub release not created | Push branch/tag and draft release with generated SHA. |
| Hardware lab not complete | Run manual smoke on target devices. |


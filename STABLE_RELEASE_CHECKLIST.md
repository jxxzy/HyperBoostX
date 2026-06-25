# Stable Release Checklist

Target release:
- `HyperBoostX v1.2.13`

Current release status:
- `Validated stable release gate`
- Current-machine automated matrix: PASS
- Full multi-machine Windows lab matrix: NOT CLAIMED

## Required Evidence

- [x] Python tests: `app\venv\Scripts\python.exe -m pytest -ra -W default`, 2026-06-26, Windows 11 10.0.26200 x64, `24 passed, 0 warnings`.
- [x] Python warning-as-error: `app\venv\Scripts\python.exe -m pytest -ra -W error`, 2026-06-26, Windows 11 10.0.26200 x64, passed.
- [x] WPF and launcher Release build: `dotnet build -c Release`, 2026-06-26, Windows 11 10.0.26200 x64, passed, `0 warnings, 0 errors`.
- [x] .NET tests: `dotnet test`, 2026-06-26, Windows 11 10.0.26200 x64, `27 passed`.
- [x] Repo verification: `scripts\verify_repo.ps1`, 2026-06-26, Windows 11 10.0.26200 x64, passed version sync, Python tests, and .NET desktop tests.
- [x] Backend build: `build_backend.bat`, 2026-06-26, passed, artifact `release\backend\hyperboost_backend.exe`.
- [x] Release/package build: `build_release.bat`, `build_launcher.bat`, `package_release.bat`, 2026-06-26, passed, artifacts `release\app\HyperBoostX.exe` and `release\launcher\HyperBoostLauncher.exe`.
- [x] Installer build: `build_installer.bat`, 2026-06-26, passed, artifact `HyperBoostXInstaller.exe`.
- [x] Installer E2E: silent install, installed launch, silent uninstall, silent reinstall, installed relaunch, and orphan cleanup passed from elevated automation shell.
- [x] Installer hash: SHA256 `4E64E080D25968CAE3E51328D3D190CF7B0CBD6A7F0E294F218CB86CB54E4E3D`, size `144632829` bytes, timestamp `2026-06-26`.
- [x] Packaged backend health: packaged runtime returned HTTP 200, status `ok`, version `1.2.13`.
- [x] Portable runtime launch: `release\app\HyperBoostX.exe`, 2026-06-26, launched launcher/WPF/backend in smoke and cleanup ended with 0 backend/UI/launcher orphan.
- [x] Installed runtime launch: installed `HyperBoostX.exe`, 2026-06-26, launched WPF/backend, health returned `1.2.13`, and cleanup ended with 0 orphan processes.
- [x] Real NVIDIA API connection: Windows Credential Manager target `HyperBoostX:NVIDIA:ApiKey`, default model `nvidia/nemotron-3-nano-30b-a3b` PASS, fallback model `nvidia/nvidia-nemotron-nano-9b-v2` PASS.
- [x] No plaintext secret test: app-state serialization tests pass; repo secret scan required before commit.
- [x] Registry/power-plan revert metadata test: automated unit coverage verifies booster profile registry and power-plan backups.
- [x] AI approval flow test: automated test verifies plan-only behavior before approval and action log/restore metadata after approval.
- [x] Safety Guard negative test: automated test blocks required unsafe action classes.
- [x] Current-machine matrix: Windows 11 Pro build 26200, admin, desktop/workstation, SSD/NVMe/HDD inventory, dashboard/window, backend health, safe profile load, cleanup safe scope, DNS safe test, restore metadata, and app exit cleanup PASS.

## Safety Gates

- [x] High-risk tweaks require Expert Mode.
- [x] High-risk tweaks require Administrator privileges.
- [x] High-risk tweaks require double confirmation.
- [x] High-risk tweaks create a real registry restore backup before mutation.
- [x] One Click Boost does not apply `disable_defender` or `disable_updates`.
- [x] Process-kill flows show a preview before closing apps.
- [x] Backend packaged health is bound to local runtime validation and returns version `1.2.13`.
- [x] Shell command execution is allowlisted and timeout-protected.
- [x] NVIDIA and Discord secrets are stored only in Windows Credential Manager.
- [x] Triple AI Engine runs Scan -> Analyzer -> Safety Guard -> Assistant -> Performance Report with local fallback when AI cloud is unavailable.
- [x] Triple AI Safety Guard blocks overclock, undervolt, Windows Security disable, permanent Windows Update disable, BIOS/UEFI, voltage, irreversible registry edits, and guaranteed FPS claims.
- [x] Triple AI Game Optimizer exposes safe manual NVIDIA/game setting recommendations without official NVIDIA partner/certified branding claims.

## Compatibility Matrix Scope

- [x] Current machine: Windows 11, admin.
- [x] Current machine: desktop/workstation.
- [x] Current machine: SSD/NVMe/HDD storage inventory detected.
- [ ] Windows 10 admin/non-admin lab: NOT CLAIMED.
- [ ] Windows 11 non-admin lab: NOT CLAIMED.
- [ ] Laptop battery/AC lab: NOT CLAIMED.

## Result

- [x] Ready to publish stable from current automated release gate.
- [x] Zero known Critical/Major bugs after automated validation.

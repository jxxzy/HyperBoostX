# QA Results

Date: 2026-06-26
Branch: `main`
Target release: `HyperBoostX v1.2.13 Stable`

## Automated Test Gates

| Check | Status | Evidence |
| --- | --- | --- |
| `scripts\verify_repo.ps1` | PASS | Version sync PASS, Python `24 passed`, .NET `27 passed` |
| Python tests via repo venv | PASS | `app\venv\Scripts\python.exe -m pytest -ra -W default` -> `24 passed, 0 warnings` |
| Python warning-as-error | PASS | `app\venv\Scripts\python.exe -m pytest -ra -W error` -> `24 passed` |
| `dotnet restore` | PASS | All projects restored/up-to-date |
| `dotnet build` | PASS | Debug build, `0 Warning(s)`, `0 Error(s)` |
| `dotnet build -c Release` | PASS | Release build, `0 Warning(s)`, `0 Error(s)` |
| `dotnet test` | PASS | `27 passed` |
| Targeted NVIDIA Copilot tests | PASS | `NvidiaCopilotServiceTests` -> `15 passed` |

## Build And Asset Gates

| Check | Status | Evidence |
| --- | --- | --- |
| `build_backend.bat` | PASS | Created `release\backend\hyperboost_backend.exe` |
| `build_release.bat` | PASS | Created WPF runtime package |
| `build_launcher.bat` | PASS | Created `release\launcher\HyperBoostLauncher.exe` |
| `package_release.bat` | PASS | Created `release\app\HyperBoostX.exe` |
| `build_installer.bat` | PASS | Created `HyperBoostXInstaller.exe` |
| `SHA256SUMS.txt` | PASS | Final installer/app/backend/launcher hashes verified against local artifacts |

## Runtime And Installer Gates

| Check | Status | Evidence |
| --- | --- | --- |
| Packaged backend health | PASS | `/api/health` returned status `ok`, version `1.2.13` |
| Portable app launch smoke | PASS | Portable launcher/WPF/backend started, health returned `1.2.13`, cleanup ended with 0 orphan processes |
| Elevated automation shell | PASS | Admin token detected for installer gate |
| Silent installer install | PASS | `HyperBoostXInstaller.exe /S` exit code `0`; install path and HKLM uninstall metadata version `1.2.13` verified |
| Installed app launch | PASS | Installed WPF/backend launched; backend health returned `1.2.13`; close ended with 0 orphan processes |
| Silent uninstall | PASS | `Uninstall.exe /S` exit code `0`; install directory and uninstall registry entries removed |
| Silent reinstall | PASS | Reinstall exit code `0`; installed app relaunch and backend health PASS; cleanup ended with 0 orphan processes |

## NVIDIA And Safety Gates

| Check | Status | Evidence |
| --- | --- | --- |
| NVIDIA Credential Manager storage | PASS | Target `HyperBoostX:NVIDIA:ApiKey` present; key loaded masked from Windows Credential Manager |
| Real NVIDIA API connection | PASS | `nvidia/nemotron-3-nano-30b-a3b` and `nvidia/nvidia-nemotron-nano-9b-v2` returned HTTP 200 with assistant content |
| 10 NVIDIA model registry | PASS | Registry count verified as 10 |
| Secret redaction | PASS | .NET tests cover NVIDIA/Bearer-style token redaction |
| AI approval flow | PASS | Automated test proves plan-only behavior until approval, approval required for non-scan actions, action log and restore metadata generated after approval |
| Safety Guard | PASS | Automated negative test blocks Defender disable, permanent Windows Update disable, registry/service changes without backup, arbitrary PowerShell, personal-file deletion, and risky boot config |
| No plaintext secret leak | PASS | Final secret scan required before commit; app-state serialization tests prevent plaintext secret persistence |

## Current-Machine Matrix

| Check | Status | Evidence |
| --- | --- | --- |
| Windows/build detection | PASS | Windows 11 Pro build `26200`, version `10.0.26200` |
| Hardware detection | PASS | Desktop/workstation, admin `True`, Intel i9-11900F, 31.88 GB RAM, NVIDIA GeForce RTX 3090 Ti, SSD/NVMe/HDD inventory detected |
| Dashboard/window smoke | PASS | Installed `HyperBoostX` process exposed a main window handle |
| Backend health | PASS | Installed backend returned version `1.2.13` |
| Safe boost safe-mode/profile load | PASS | Booster profiles endpoint returned 4 profiles without applying a profile |
| Cleanup safe test | PASS | `temp_files` cleanup scope completed successfully using a matrix temp marker |
| Network safe test | PASS | DNS test returned `Good` |
| Restore/undo metadata check | PASS | Matrix restore metadata recorded safe cleanup undo scope; no TCP reset executed |
| App exit cleanup | PASS | 0 HyperBoostX/launcher/backend orphan processes |

Full multi-machine Windows lab matrix: NOT CLAIMED
Current-machine automated matrix: PASS

## Final Statement

Automated validation passed.
Python tests: 24 passed, 0 warnings.
Installed app launch: PASS.
Installer uninstall/reinstall: PASS.
Real NVIDIA API connection: PASS.
AI approval flow: PASS.
Safety Guard: PASS.
Current-machine automated matrix: PASS.
Zero known Critical/Major bugs after automated validation.

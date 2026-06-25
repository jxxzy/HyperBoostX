# QA Results

Date: 2026-06-25
Branch: `fix/full-hyperboostx-audit-nvidia-ai`

## Automated Tests

| Check | Status | Notes |
| --- | --- | --- |
| Targeted Python safety tests | PASS | `14 passed` |
| Targeted NVIDIA .NET tests | PASS | `6 passed` |
| `scripts\verify_repo.ps1` | PASS | Version sync PASS, Python `40 passed`, .NET `20 passed` |
| Full Python tests | PASS | `app\venv\Scripts\python.exe -m pytest` -> `40 passed, 1 warning` |
| `dotnet restore` | PASS | All projects up-to-date |
| `dotnet build` | PASS | Debug build, 0 warnings, 0 errors |
| `dotnet build -c Release` | PASS | Release build, 0 warnings, 0 errors |
| `dotnet test` | PASS | `20 passed` |

## Build Scripts

| Script | Status | Notes |
| --- | --- | --- |
| `build_backend.bat` | PASS | Created `release\backend\hyperboost_backend.exe` |
| `build_release.bat` | PASS | Created `release\wpf` runtime |
| `build_launcher.bat` | PASS | Created `release\launcher\HyperBoostLauncher.exe` |
| `package_release.bat` | PASS | Created `release\package` and `release\app\HyperBoostX.exe` |
| `build_installer.bat` | PASS | Created `HyperBoostXInstaller.exe`; SHA256 updated |

## Runtime QA

| Check | Status | Notes |
| --- | --- | --- |
| Portable app launch | PASS | `release\app\HyperBoostX.exe` launched and closed in smoke |
| Installed app launch | PASS | Existing `C:\Program Files\HyperBoost X\HyperBoostX.exe` launched and closed in smoke |
| Packaged backend health | PASS | `/api/health` returned HTTP 200 with local token |
| App close clean/no backend orphan | PASS | Portable and installed smoke left no HyperBoostX/backend/launcher process |
| Feature Audit Full | PASS (automated regression) | `FeatureAuditRegressionTests` passed; in-app visual run still manual |
| Full QA Matrix | PASS (automated regression) | `FeatureAuditRegressionTests` passed; in-app visual run still manual |
| NVIDIA AI connection | NEEDS MANUAL QA | Must be tested through Settings so API key stays in Credential Manager |
| 10 model dropdown | PASS (code/test) | Model registry test confirms 10 required models; visual confirmation still manual |
| AI approval flow | PASS (code/test) | Approval service test confirms non-scan actions require approval |
| Safety Guard | PASS | Unit test coverage added |
| Restore/Undo | PASS (code/test) | Booster profile backup tests pass; full real-machine tweak matrix still manual |
| Installer build | PASS | NSIS produced `HyperBoostXInstaller.exe` |
| Installer uninstall/reinstall | NEEDS MANUAL QA | Not run to avoid altering existing owner install without a lab snapshot |

## Notes

No NVIDIA API key was written to files, logs, app-state, or command history during this audit.

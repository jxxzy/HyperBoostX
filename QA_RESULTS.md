# QA Results - HyperBoostX v1.3.0 Stable

Run date: 2026-06-26
Target release: `HyperBoostX v1.3.0 Stable`

## Automated Tests

| Gate | Status | Evidence |
|---|---|---|
| Python tests | PASS | `app\venv\Scripts\python.exe -m pytest -q tests` -> `43 passed` |
| .NET tests | PASS | `dotnet test dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj -c Debug` -> `28 passed` |
| NVIDIA/AMD/Intel/MicrosoftBasic/unknown GPU tests | PASS | Covered in `tests/test_v13_gpu_hardware.py` |
| Session-token API test | PASS | Unauthorized mutating endpoint returns `401`; matching `X-HyperBoostX-Session` accepted |
| Boost approval test | PASS | `/api/boost/apply` returns `409` until `user_approved` is true |
| Job queue lifecycle test | PASS | Start/status lifecycle covered in Python tests |
| Before/after report export test | PASS | Markdown export schema covered in Python tests |
| Crash report redaction test | PASS | Local crash export redacts API keys, bearer tokens, GitHub tokens, usernames, and sensitive paths |
| Support docs/FAQ/roadmap test | PASS | `SUPPORT.md`, templates, `TROUBLESHOOTING.md`, `FAQ.md`, and `ROADMAP.md` covered in Python tests |
| WPF Debug build | PASS | `dotnet build wpf\HyperBoostX.csproj -c Debug -v minimal` -> build succeeded with 0 warnings |

## Build And Release Gates

| Gate | Status | Evidence |
|---|---|---|
| `scripts\verify_repo.ps1` | PASS | Repository verification passed; Python 43/43 and .NET 28/28 |
| `dotnet restore` | PASS | `dotnet restore HyperBoostX.sln` |
| `dotnet build` | PASS | `dotnet build HyperBoostX.sln -v minimal` -> 0 warnings, 0 errors |
| `dotnet build -c Release` | PASS | `dotnet build HyperBoostX.sln -c Release -v minimal` -> 0 warnings, 0 errors |
| WPF Release build | PASS | `dotnet build wpf\HyperBoostX.csproj -c Release -v minimal` -> 0 warnings, 0 errors |
| Launcher Release build | PASS | `dotnet build launcher\HyperBoostLauncher.csproj -c Release -v minimal` -> 0 warnings, 0 errors |
| Backend build | PASS | `package_release.bat` built `release\backend\hyperboost_backend.exe` with PyInstaller |
| Release package | PASS | `package_release.bat` created `release\package` and portable `release\app\HyperBoostX.exe` |
| Installer build | PASS | `build_installer.bat` created `HyperBoostXInstaller.exe` |
| Packaged backend health `1.3.0` | PASS | `release\package\backend\hyperboost_backend.exe` returned `/api/health` and `/api/version` version `1.3.0` |
| Portable launch smoke | PASS | `release\app\HyperBoostX.exe` launched backend `1.3.0` and WPF runtime process; no orphan process remained after close |
| Installer install/uninstall/reinstall | PASS | Silent install, installed launch/health, silent uninstall, reinstall, reinstalled launch/health, final uninstall passed in `F:\HBX_SMOKE\HyperBoostX` |
| Secret scan | PASS | Source findings were placeholders/variable names only; `release\package` plaintext secret scan found no matches |
| SHA256SUMS | PASS | `16024ADF082ACEBA47387A6A32B9C574BBF2FBB722EC3610286494AC95D764A8  HyperBoostXInstaller.exe` verified |

## Hardware Matrix

Not claimed from this workspace alone:

- Windows 10 22H2
- Windows 11 23H2
- Windows 11 24H2
- Windows 11 25H2
- Intel CPU
- AMD Ryzen CPU
- NVIDIA GPU
- AMD Radeon GPU
- Intel Arc GPU
- Intel iGPU only
- 8GB/16GB/32GB RAM
- admin and non-admin users
- offline mode
- no dedicated GPU mode
- hybrid laptop mode

## Known Limitations

- Full multi-machine Windows lab validation is not complete in this workspace.
- Full WPF MVVM page split is not complete.
- Automated UI smoke covered WPF process launch and feature-audit regression tests; full manual click-through on multiple machines is not claimed.
- GPU telemetry may fall back to unknown values when WMI/driver counters are unavailable.
- Code signing certificate is not available in this workspace; Windows may show Unknown Publisher or SmartScreen until a signed future release.
- Auto updater, website, opt-in telemetry, and license activation remain roadmap-only items.

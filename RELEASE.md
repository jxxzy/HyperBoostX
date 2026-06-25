# HyperBoostX v1.2.13 Release Guide

Target release: `HyperBoostX v1.2.13 Stable`
Tag target: `v1.2.13`
Branch: `main`

## Release Gates

Do not publish a stable release unless the current validation has zero known Critical/Major bugs and the required gates in `APP_GATE_CHECKLIST.md` and `STABLE_RELEASE_CHECKLIST.md` are reviewed.

Completed automated gates in this workspace:

- `powershell -ExecutionPolicy Bypass -File .\scripts\verify_repo.ps1`: PASS
- Python tests via repo venv: PASS, `24 passed, 0 warnings`
- Python warning-as-error: PASS
- `dotnet restore`: PASS
- `dotnet build`: PASS
- `dotnet build -c Release`: PASS
- `dotnet test`: PASS, `27 passed`
- `build_backend.bat`: PASS
- `build_release.bat`: PASS
- `build_launcher.bat`: PASS
- `package_release.bat`: PASS
- `build_installer.bat`: PASS
- Packaged backend health: PASS, version `1.2.13`
- Portable launch smoke: PASS
- Elevated silent installer install: PASS
- Installed app launch: PASS
- Silent uninstall/reinstall: PASS
- Real NVIDIA API connection from Windows Credential Manager: PASS
- AI approval flow: PASS
- Safety Guard: PASS
- Current-machine automated matrix: PASS
- SHA256SUMS verification: PASS

Full multi-machine Windows lab matrix: NOT CLAIMED

## Release Assets

- `HyperBoostXInstaller.exe`
- `SHA256SUMS.txt`
- `release\app\HyperBoostX.exe`
- `release\backend\hyperboost_backend.exe`
- `release\launcher\HyperBoostLauncher.exe`

## Final Statement

Stable means zero known Critical/Major bugs after automated validation. Stable does not mean bug-free forever.

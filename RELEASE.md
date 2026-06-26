# HyperBoostX v2.0.0 Release Guide

Target release: `HyperBoostX v2.0.0 Ultimate Winner Edition`
Tag target: `v2.0.0`
Branch: `feature/hyperboostx-v2-release`

Do not publish a stable release unless required gates pass. If a gate cannot be completed because of missing credentials, signing certificate, or interactive installer access, mark the release as partial/blocker rather than claiming DONE.

## Required Commands

```powershell
git status
powershell -ExecutionPolicy Bypass -File .\scripts\verify_repo.ps1
app\venv\Scripts\python.exe -m pytest -q tests
dotnet restore HyperBoostX.sln
dotnet build HyperBoostX.sln -v minimal
dotnet build HyperBoostX.sln -c Release -v minimal
dotnet test dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj -c Debug
.\build_backend.bat
.\build_launcher.bat
.\build_release.bat
.\package_release.bat
.\build_installer.bat
```

## Trust Requirements

- Generate `SHA256SUMS.txt` for release artifacts.
- Sign only if a real code-signing certificate is available.
- If unsigned, keep Unknown Publisher and SmartScreen notes in release notes.
- Do not claim multi-machine validation unless actually tested.
- Do not claim GitHub Release publication unless assets were uploaded.
- Keep screenshot evidence in `docs/screenshots/` and `website/assets/` captured from the real WPF app.

## Release Scope

v2.0.0 includes the real WPF cyber shell, routed views, global cyber themes/styles, Settings motion/accent/mode controls, backend Ultimate Winner Edition foundations, safer product docs, metadata sync, expanded tests, installer packaging, portable/installed smoke validation, and static website starter.

Signed distribution, cloud sync, plugin marketplace, RGB control, global benchmark comparison, performance overlay, automatic driver download/install, and license activation remain roadmap or owner-environment work.

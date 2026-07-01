# HyperBoostX v2.x Development Release Guide

Public stable baseline: `HyperBoostX v2.10.0 Stable Unsigned`
Development preview line: `HyperBoostX v2.x`
Current stable tag: `v2.10.0`
Branch: `main`

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
powershell -ExecutionPolicy Bypass -File .\scripts\full_qa_gate.ps1 -SkipInstall
powershell -ExecutionPolicy Bypass -File .\scripts\build_stable_release.ps1 -SkipTests
```

## Trust Requirements

- Generate checksum manifests under `docs\release\checksums\`.
- Sign only if a real code-signing certificate is available.
- If unsigned, keep Unknown Publisher and SmartScreen notes in release notes.
- Do not claim multi-machine validation unless actually tested.
- Do not claim GitHub Release publication unless assets were uploaded.
- Keep screenshot evidence in `docs/screenshots/` and `website/assets/` captured from the real WPF app.

## Release Scope

The v2 development line includes the real WPF cyber shell, routed views, global cyber themes/styles, Settings motion/accent/mode controls, backend product foundations, safer product docs, metadata sync, expanded tests, installer packaging, portable/installed smoke validation, and static website starter.

Signed distribution, cloud sync, plugin marketplace, RGB control, global benchmark comparison, performance overlay, automatic driver download/install, and license activation remain roadmap or owner-environment work.

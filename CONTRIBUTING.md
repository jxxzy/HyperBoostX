# Contributing

Thanks for helping improve HyperBoostX.

## Rules

- Keep Safety Guard active.
- Do not add Defender disable, permanent Windows Update disable, anti-cheat changes, driver-service disabling, BIOS/UEFI edits, overclocking, undervolting, voltage tuning, arbitrary shell execution, or destructive cleanup.
- Do not claim guaranteed FPS gains.
- Do not add vendor partnership language unless it is real and documented.
- Add tests for backend contracts, storage, redaction, approval flow, or UI models when behavior changes.

## Local Checks

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify_repo.ps1
app\venv\Scripts\python.exe -m pytest -q tests
dotnet build HyperBoostX.sln -v minimal
dotnet test dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj -c Debug
```

## Pull Requests

Describe what changed, how it was tested, and whether the change mutates Windows state. Include screenshots for WPF UI changes.

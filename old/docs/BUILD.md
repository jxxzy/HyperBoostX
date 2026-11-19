# Build

Target version: `2.10.0`

## Verify

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify_repo.ps1
```

## Python Tests

```powershell
app\venv\Scripts\python.exe -m pytest -q tests
```

## .NET Build And Tests

```powershell
dotnet restore HyperBoostX.sln
dotnet build HyperBoostX.sln -v minimal
dotnet build HyperBoostX.sln -c Release -v minimal
dotnet test dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj -c Debug
```

## Release Artifacts

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build_stable_release.ps1 -SkipTests
```

Legacy batch wrappers are kept under `scripts\legacy-batch\` for local troubleshooting:

```powershell
.\scripts\legacy-batch\build_backend.bat
.\scripts\legacy-batch\build_launcher.bat
.\scripts\legacy-batch\build_release.bat
.\scripts\legacy-batch\package_release.bat
.\scripts\legacy-batch\build_installer.bat
```

Generated artifacts must be checked for secrets, signed only with a real certificate, and verified with SHA256 manifests in `docs\release\checksums\`.

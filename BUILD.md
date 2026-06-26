# Build

Target version: `2.0.0`

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
.\build_backend.bat
.\build_launcher.bat
.\build_release.bat
.\package_release.bat
.\build_installer.bat
```

Generated artifacts must be checked for secrets, signed only with a real certificate, and verified with SHA256.

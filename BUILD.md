# Build HyperBoostX

Target version: `1.3.0`

## Verification

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify_repo.ps1
```

Useful flags:

```powershell
-Configuration Release
-SkipPython
-SkipDotnet
-NoRestore
```

## Manual Gates

```powershell
app\venv\Scripts\python.exe -m pytest -q tests
dotnet restore HyperBoostX.sln
dotnet build HyperBoostX.sln
dotnet build HyperBoostX.sln -c Release
dotnet test dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj -c Debug
dotnet build wpf\HyperBoostX.csproj -c Release
dotnet build launcher\HyperBoostLauncher.csproj -c Release
```

## Release Scripts

```bat
build_backend.bat
build_release.bat
build_launcher.bat
package_release.bat
build_installer.bat
```

Expected public release asset:

- `HyperBoostXInstaller.exe`

Optional supporting asset:

- `SHA256SUMS.txt`

Do not publish raw backend, raw launcher, debug, temp, cache, log, or internal CI artifacts as normal-user downloads.

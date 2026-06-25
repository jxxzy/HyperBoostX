# HyperBoostX Release Process

## Version Sync

Before release, verify these locations agree:

- `VERSION`
- WPF assembly metadata
- Launcher metadata
- Backend `Config.VERSION`
- Installer metadata
- README, CHANGELOG, release notes, and checksum files

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify_version_sync.ps1
```

## Required Validation

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\verify_repo.ps1
app\venv\Scripts\python.exe -m pytest
dotnet restore
dotnet build
dotnet build -c Release
dotnet test
```

Then run build scripts:

```bat
build_backend.bat
build_release.bat
build_launcher.bat
package_release.bat
build_installer.bat
```

## Runtime QA

- Portable launch
- Installed launch
- Backend health from packaged runtime
- App close with no orphan backend
- Installer uninstall/reinstall
- Feature Audit Full
- Full QA Matrix
- Restore/Undo
- NVIDIA Copilot connection using a key saved through Settings

## GitHub Release

Do not publish stable until automated validation and Windows lab evidence are attached. If installer is unsigned, auto-install should remain blocked while manual install may be allowed only when checksum is valid and the UI explains the unsigned state.

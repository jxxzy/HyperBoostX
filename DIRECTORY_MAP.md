# Directory Map

Quick reference for the current HyperBoostX repository layout.

## Root

- `HyperBoostX.sln` - solution entry for the .NET projects
- `build_backend.bat` - build backend executable into `release\backend`
- `build_release.bat` - publish WPF UI into `release\wpf`
- `build_launcher.bat` - publish launcher into `release\launcher`
- `package_release.bat` - assemble `release\package` and `release\app`
- `build_installer.bat` - build `HyperBoostXInstaller.exe`
- `repair_uninstall.ps1` - force cleanup for broken installs
- `HyperBoostXInstaller.nsi` - NSIS installer script
- `HyperBoostXInstaller.exe` - latest built installer
- `README.md` - project overview
- `DIRECTORY_MAP.md` - this file

## Documentation folders

- `docs\API_REFERENCE.md` - backend API notes
- `docs\REALTIME_MONITORING.md` - monitoring architecture notes
- `docs\RELEASE_BLUEPRINT.md` - consolidated release and product plan
- `docs\release-notes` - archived historical release notes
- `docs\release-gates` - archived historical release gate documents

## Source folders

- `app`
  - Python Flask backend
  - API blueprints, services, config, optimization logic
- `wpf`
  - WPF UI project
  - main window, routing, backend client
- `launcher`
  - launcher executable project
  - backend lifecycle and app startup
- `tests`
  - tests and support assets

## Release folders

- `release\backend`
  - packaged backend executable
- `release\wpf`
  - published WPF executable
- `release\launcher`
  - published launcher executable
- `release\package`
  - installer input layout
  - `launcher\HyperBoostLauncher.exe`
  - `wpf\HyperBoostX.exe`
  - `backend\hyperboost_backend.exe`
- `release\app`
  - portable end-user layout
  - `HyperBoostX.exe`
  - `runtime\wpf\HyperBoostX.exe`
  - `runtime\backend\hyperboost_backend.exe`

## Runtime notes

- End-user logs are written to `%LocalAppData%\HyperBoostX\logs`
- The installed app entrypoint is `HyperBoostX.exe`
- The backend is an internal runtime, not a user-facing executable

## Recommended starting points

- UI changes: `wpf\MainWindow.xaml` and `wpf\MainWindow.xaml.cs`
- Launcher behavior: `launcher\Program.cs`
- Backend startup: `app\backend_server.py`
- API changes: `app\api`
- System logic: `app\services`

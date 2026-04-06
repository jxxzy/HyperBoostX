# HyperBoost X

HyperBoost X is a Windows optimization app built from three parts:

- a WPF desktop UI in `wpf`
- a Python Flask backend in `app`
- a .NET launcher in `launcher`

The launcher starts the backend, waits for health check readiness, opens the WPF UI, and shuts the backend down again when the UI exits.

## Runtime layout

- Installed app entrypoint: `HyperBoostX.exe`
- Internal UI runtime: `runtime\wpf\HyperBoostUI.exe`
- Internal backend runtime: `runtime\backend\hyperboost_backend.exe`
- User logs: `%LocalAppData%\HyperBoost X\logs`

## Main folders

- `app` - Python backend and services
- `wpf` - WPF frontend
- `launcher` - launcher/entrypoint
- `release` - packaged outputs
- `tests` - tests and support assets

## Build scripts

- `build_backend.bat` - builds `release\backend\hyperboost_backend.exe`
- `build_release.bat` - publishes the WPF UI into `release\wpf`
- `build_launcher.bat` - publishes the launcher into `release\launcher`
- `package_release.bat` - assembles `release\package` and `release\app`
- `build_installer.bat` - builds `HyperBoostXInstaller.exe`

## Development scripts

- `start_backend.bat` - run backend only
- `start_wpf_client.bat` - run WPF client only against a running backend

## Release outputs

- Portable app: `release\app\HyperBoostX.exe`
- Installer: `HyperBoostXInstaller.exe`

## Documentation

- `API_REFERENCE.md` - API overview
- `DIRECTORY_MAP.md` - current repo map

Older one-click batch launchers and snapshot-style docs were removed so the repository matches the current installer and launcher architecture.

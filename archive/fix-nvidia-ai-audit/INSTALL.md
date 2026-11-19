# HyperBoostX Install Guide

## Portable Run

Build the package first:

```bat
build_backend.bat
build_release.bat
build_launcher.bat
package_release.bat
```

Run:

```text
release\app\HyperBoostX.exe
```

## Installer Run

Build the installer:

```bat
build_installer.bat
```

Run:

```text
HyperBoostXInstaller.exe
```

## User Config

User config is stored under:

```text
%LocalAppData%\HyperBoost X\config
```

Backups and restore metadata are stored under:

```text
%LocalAppData%\HyperBoost X\backups
```

NVIDIA and Discord secrets are stored in Windows Credential Manager, not in app-state JSON.

## Uninstall And Reinstall

Use Windows Apps settings or the Start Menu uninstall entry. Reinstall should preserve `%LocalAppData%\HyperBoost X` so user config, logs, and backups remain available.

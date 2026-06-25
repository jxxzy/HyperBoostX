# HyperBoostX Install Guide

## Portable Runtime

Use `release\app\HyperBoostX.exe` for portable smoke testing. The launcher starts the WPF client and packaged backend from `release\app\runtime`.

Validated in this workspace:

- Portable launcher started
- WPF runtime started
- Packaged backend started
- Cleanup ended with 0 HyperBoostX/launcher/backend orphan processes

## Installer

Run `HyperBoostXInstaller.exe` from an elevated Windows shell or Explorer prompt. The installer writes to `Program Files` and HKLM uninstall metadata.

Installer validation status in this workspace:

- NSIS build: PASS
- Silent install: PASS
- Installed launch: PASS
- Silent uninstall: PASS
- Silent reinstall and relaunch: PASS
- Cleanup ended with 0 HyperBoostX/launcher/backend orphan processes

## User Data Preservation

The installer preserves user config, logs, backups, and automation state under `%LocalAppData%\HyperBoost X`.

## Checksums

Use `SHA256SUMS.txt` generated after the final build assets. Do not reuse old checksums.

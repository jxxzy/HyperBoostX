# HyperBoostX Install Guide

## Portable Runtime

Use `release\app\HyperBoostX.exe` only for internal portable smoke testing. Normal users should download and run the single public installer asset, `HyperBoostXInstaller.exe`.

Validated in this workspace:

- Portable launcher started
- WPF runtime started
- Packaged backend started
- Cleanup ended with 0 HyperBoostX/launcher/backend orphan processes

## Installer

Download the single public release asset, `HyperBoostXInstaller.exe`, then run it from Explorer or an elevated Windows shell. The installer writes to `Program Files` and HKLM uninstall metadata.

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

Use `SHA256SUMS.txt` generated after the final build assets for internal audit and release verification. Do not publish it as a separate user download unless a release explicitly needs public checksum files.

# Installer, updater and signing

`scripts/package.ps1` runs tests, publishes self-contained x64 WPF/CLI payloads, builds an NSIS installer, builds a portable ZIP and writes SHA256 checksums. Installer compilation treats warnings as errors. Exact payload file lists also generate uninstaller deletes; unrelated install-directory files are preserved.

The installer is per-user and creates desktop/Start Menu shortcuts plus an Apps uninstall entry. A running-app mutex blocks installation/uninstallation while the desktop is open. Reinstall replaces shipped binaries; app data and recovery remain separate. Silent options are `/S` for installer and uninstaller. Windows settings are kept; the interactive uninstaller explains how to review Restore before proceeding.

The installer and application are unsigned. A production certificate and signing validation have not been supplied. There is no automatic updater; rerun the verified installer manually. Signed manifest verification, update staging, health-check rollback and historical upgrade testing are incomplete and block Stable promotion.

# HyperBoostX Troubleshooting

## App Will Not Open

- Restart HyperBoostX from the Start Menu shortcut.
- If the backend does not connect, close HyperBoostX and start it again through the launcher.
- If the installer is unsigned, Windows may show Unknown Publisher or SmartScreen. This is expected until code signing is available.

## Backend Disconnected

- Make sure no firewall rule blocks `127.0.0.1` local traffic.
- Restart the app through the launcher so the local session token is regenerated.
- Reinstall with `HyperBoostXInstaller.exe` if backend files are missing.

## Restore Or Undo

- Open Restore & Backup from the sidebar.
- Use undo or restore metadata before applying more changes.
- Safe Mode / Recovery Mode is prepared in the roadmap; v1.3.0 keeps Restore & Backup accessible as the supported recovery path.

## GPU Detection Is Unknown

- Unknown GPU fallback is safe and expected when Windows, WMI, or driver counters do not expose adapter details.
- Update official GPU drivers from NVIDIA, AMD, Intel, or Microsoft if adapter data is missing.
- HyperBoostX does not use unsafe driver hacks to force detection.

## Export A Local Crash Report

Use the crash report export endpoint or app diagnostics flow. The report includes app version, Windows version, CPU, RAM, GPU vendor/model, error message, stack trace, last action, backend status, and timestamp.

The crash report redacts API keys, AI keys, tokens, GitHub tokens, usernames, sensitive paths, and future license keys. It is not uploaded automatically.

## Uninstall

Use Windows Settings > Apps > Installed apps, select HyperBoostX, and uninstall. You can reinstall with the latest `HyperBoostXInstaller.exe` from GitHub Releases.

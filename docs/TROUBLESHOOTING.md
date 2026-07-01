# Troubleshooting

## Backend Disconnected

- Start HyperBoostX through the launcher.
- Confirm the backend URL is `http://127.0.0.1:5000`.
- Open `http://127.0.0.1:5000/api/health` locally.
- If another app uses port 5000, restart HyperBoostX or update the backend URL in Settings.

## Unknown GPU Telemetry

Unknown GPU fallback is safe. HyperBoostX will avoid vendor-specific changes and show Unknown Safe GPU Mode. Install the official GPU/OEM driver manually if Windows only exposes Microsoft Basic Display Adapter.

## SmartScreen Or Unknown Publisher

v2.10.0 is stable unsigned because no owner code-signing certificate/PFX was supplied. Windows may show Unknown Publisher or SmartScreen. Verify `SHA256SUMS.txt` before installing.

## AI Advisor Looks Conservative

That is expected. AI Advisor is a local deterministic diagnosis engine and will not suggest unsafe tweaks, shell commands, Defender disable, anti-cheat changes, driver-service changes, overclocking, undervolting, voltage tuning, or BIOS edits.

## Crash Report

Crash reports are local and redacted. They are not uploaded automatically. Review the exported report before sharing it.

## Restore

Use Restore & Backup or restore-session endpoints to inspect metadata. v2.10.0 keeps restore metadata, preview, undo visibility, and guarded rollback routes as the supported recovery path.

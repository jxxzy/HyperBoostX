# Bugs Fixed - HyperBoostX v1.3.0

## Version And Metadata

- Backend version now comes from shared config and reports `1.3.0`.
- WPF, launcher, installer, update user-agent, about text, and active docs were updated to `1.3.0`.
- Installer metadata now writes `DisplayVersion` `1.3.0`.

## Universal GPU Support

- Added GPU vendor enum values: `Nvidia`, `Amd`, `Intel`, `MicrosoftBasic`, `Unknown`.
- Added GPU family detection for GeForce GTX/RTX, AMD Radeon RX/Vega/integrated, Intel Arc/Iris Xe/UHD/iGPU, Microsoft Basic Display, and unknown fallback.
- Added hybrid and multi-GPU detection metadata.
- Added vendor badge and dynamic accent recommendation metadata.
- Added conservative `Unknown Safe GPU Mode` fallback.

## GPU Center Inputs

- Added vendor/RGB/overlay/launcher/streaming process catalog and safety classification.
- Added API endpoints for GPU summary, vendor software, overlays, and hardware profile.

## Safe Boost And Reports

- Added safe boost plan/apply/undo API.
- Apply now requires user approval and returns `409` when approval is missing.
- Added before/after report snapshot and export contract for JSON, TXT, and Markdown.
- Added blocked risky action metadata to reports and profiles.

## Job Queue

- Added local in-memory job queue with progress, stage, logs, cancel, and completion state.
- Added job start/status/cancel endpoints.

## Local API Security

- Added optional session-token enforcement for mutating endpoints when `HYPERBOOSTX_SESSION_TOKEN` is configured.
- Launcher now generates a random session token and passes it to backend and WPF process environments.
- WPF backend client sends `X-HyperBoostX-Session` when present.
- CORS allows only localhost origins and includes `X-HyperBoostX-Session` in allowed headers.

## Tests Added

- NVIDIA RTX detection.
- AMD Radeon detection.
- Intel Arc detection.
- Intel iGPU/hybrid detection.
- Microsoft Basic Display fallback.
- Unknown GPU safe fallback.
- Vendor software and overlay classification.
- Hardware profile schema.
- v1.3.0 health/version endpoints.
- Session token rejection/approval.
- Boost approval required.
- Job queue lifecycle.
- Before/after report export schema.


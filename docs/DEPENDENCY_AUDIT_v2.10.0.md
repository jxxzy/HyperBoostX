# Dependency Audit v2.10.0

> Current release policy: HyperBoostX v2.10.0 is the Stable Unsigned public release. Code signing remains `SKIPPED_BY_OWNER_NO_CERT`; external hardware matrix expansion is recommended.

## Runtime Dependencies

- .NET 8 WPF desktop app.
- Python Flask backend and local services.
- Newtonsoft.Json for WPF JSON handling.
- NAudio/OpenCvSharp for existing media/camera features.
- NSIS for installer packaging.

## Policy

- No dependency may bypass Safety Guard.
- No dependency may upload telemetry silently.
- No bundled third-party driver installers.
- No plugin marketplace execution until signed/trusted plugin policy exists.

## Required Before Stable

- Regenerate SBOM.
- Verify packaged dependency versions.
- Run secret scan.
- Confirm installer ships the expected WPF/backend runtime files.

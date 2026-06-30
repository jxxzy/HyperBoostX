# Dependency Audit v2.10.0

> Public release policy: HyperBoostX v1.3.0 is the current recommended public stable baseline. The 2.10.0-beta.1 runtime is a Beta development build and must not be promoted as stable until installed runtime, admin rollback, hardware matrix, code signing, checksum, and smoke gates pass.

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


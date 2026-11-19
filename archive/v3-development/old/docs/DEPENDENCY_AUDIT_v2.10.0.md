# Dependency Audit v2.10.0

Status: `OWNER_LAB_RELEASE_CANDIDATE`

Current v2.10 status: HyperBoostX v2.10.0 Owner Lab / Release Candidate. Public stable remains v1.3.0 until installed runtime, admin rollback, hardware matrix, and signing/public-release evidence pass.

Generated: 2026-07-03 20.38.52 +07:00

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
- No unsigned plugin execution path is treated as production marketplace behavior.


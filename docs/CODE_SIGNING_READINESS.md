# Code Signing Readiness

> Public release policy: HyperBoostX v1.3.0 is the current recommended public stable baseline. The 2.10.0-beta.1 runtime is a Beta development build and must not be promoted as stable until installed runtime, admin rollback, hardware matrix, code signing, checksum, and smoke gates pass.

## Current Status

Status: BLOCKED_BY_OWNER_CERTIFICATE

No real code-signing certificate, thumbprint, or PFX was available in this workspace. v2.10.0-beta.1 artifacts must remain unsigned/testing-only unless the owner supplies signing material.

## Required Before Stable

- Obtain trusted code-signing certificate.
- Sign WPF executable, launcher, backend executable if packaged, and installer.
- Verify signature with Get-AuthenticodeSignature.
- Regenerate SHA256 after signing.
- Document SmartScreen/Unknown Publisher behavior if unsigned preview artifacts are shared.

## Command Template

`powershell
.\sign_release.ps1 -Thumbprint "<CERT_THUMBPRINT>"
Get-AuthenticodeSignature .\HyperBoostXInstaller.exe
`
"@

Write-Doc "PRIVACY.md" @"
# Privacy

> Public release policy: HyperBoostX v1.3.0 is the current recommended public stable baseline. The 2.10.0-beta.1 runtime is a Beta development build and must not be promoted as stable until installed runtime, admin rollback, hardware matrix, code signing, checksum, and smoke gates pass.

HyperBoostX is local-first. The stable baseline and v2 development line are documented as local desktop software, not a cloud account product.

## Data Handling

- Runtime data is stored under %LocalAppData%\HyperBoost X or HYPERBOOSTX_PORTABLE_HOME.
- Reports, crash exports, and diagnostics must redact tokens, API keys, webhooks, usernames, user-profile paths, and sensitive local paths.
- Telemetry is off by default.
- Cloud/license is a local beta boundary in v2.10.0-beta.1, not a production cloud-account claim.

## User Control

- Mutating actions require preview/approval where supported.
- Restore metadata must be visible for supported changes.
- Reports are manually exported by the user.


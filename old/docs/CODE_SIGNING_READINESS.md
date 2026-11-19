# Code Signing Readiness

Status: `OWNER_LAB_UNSIGNED`

Current v2.10 status: HyperBoostX v2.10.0 Owner Lab / Release Candidate. Public stable remains v1.3.0 until installed runtime, admin rollback, hardware matrix, and signing/public-release evidence pass.

Generated: 2026-07-03 20.38.52 +07:00

## Current Decision

- Installer v2.10.0 is distributed as unsigned.
- Windows can show Unknown Publisher or SmartScreen warning.
- Users should verify SHA256 before install.
- Signing can only be claimed after the owner supplies a real certificate/PFX and the artifacts are signed.

## Required Before Signed Release

- Obtain trusted code-signing certificate.
- Sign installer, launcher, WPF executable, and packaged backend executable.
- Verify signatures with `Get-AuthenticodeSignature`.
- Regenerate checksums after signing.
- Upload signed artifacts and matching checksums to the GitHub Release.


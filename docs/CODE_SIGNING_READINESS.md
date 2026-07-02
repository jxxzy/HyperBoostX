# Code Signing Readiness

Status: `STABLE_UNSIGNED`

Current public release: HyperBoostX v2.10.0 Stable Unsigned. Code signing remains SKIPPED_BY_OWNER_NO_CERT, so this generator must not claim signed artifacts.

Generated: 2026-07-03 02.57.27 +07:00

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


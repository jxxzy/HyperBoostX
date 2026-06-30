# Plugin Security v2.10.0

> Public release policy: HyperBoostX v1.3.0 is the current recommended public stable baseline. The 2.10.0-beta.1 runtime is a Beta development build and must not be promoted as stable until installed runtime, admin rollback, hardware matrix, code signing, checksum, and smoke gates pass.

Plugin marketplace is a local catalog and manifest-validation boundary; unsigned arbitrary execution remains blocked.

## Current Boundary

- Registry/status can be shown.
- Unsigned plugin install is blocked or Safety-Guard evaluated.
- No third-party plugin code should execute from marketplace UI in v2.10 beta.

## Future Requirement

- Signed plugin manifest.
- Hash verification.
- Permission declaration.
- Local-only execution boundary.
- Explicit owner approval.


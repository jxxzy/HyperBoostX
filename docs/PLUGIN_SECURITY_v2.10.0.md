# Plugin Security v2.10.0

> Current release policy: HyperBoostX v2.10.0 is the Stable Unsigned public release. Code signing remains `SKIPPED_BY_OWNER_NO_CERT`; external hardware matrix expansion is recommended.

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

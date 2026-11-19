# Owner Handoff v2.10.0

> Public release policy: HyperBoostX v1.3.0 is the current recommended public stable baseline. The 2.10.0-beta.1 runtime is a Beta development build and must not be promoted as stable until installed runtime, admin rollback, hardware matrix, code signing, checksum, and smoke gates pass.

## What Is Ready

- Runtime version/channel is beta-aware.
- UI action map covers 72 menus and 596 buttons.
- Action map routes are automated-test covered.
- README public status now points normal users to v1.3.0 stable.
- v2.10 beta docs and gate scripts are present.

## Owner Must Run Before Stable

1. Install generated build as administrator.
2. Run installed runtime verifier.
3. Run admin apply/rollback lab.
4. Run hardware matrix.
5. Provide code-signing certificate/PFX.
6. Generate signed artifacts and SHA256.
7. Confirm release notes and GitHub Release assets.

Stable status: NO-GO until those gates pass.



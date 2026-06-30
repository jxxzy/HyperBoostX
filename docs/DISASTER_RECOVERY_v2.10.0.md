# Disaster Recovery v2.10.0

> Public release policy: HyperBoostX v1.3.0 is the current recommended public stable baseline. The 2.10.0-beta.1 runtime is a Beta development build and must not be promoted as stable until installed runtime, admin rollback, hardware matrix, code signing, checksum, and smoke gates pass.

## Recovery Paths

- Backend offline: UI stays responsive and shows launcher restart guidance.
- Token mismatch: relaunch through HyperBoostX launcher.
- Corrupt config: backup corrupt JSON and load defaults.
- Failed apply: no unsupported system change should be applied; show safe failure.
- Bad release install: uninstall, reinstall public stable v1.3.0, preserve/export user reports when possible.

## Stable Gate

Admin rollback smoke and installed runtime verification must pass before v2.10 gets a stable label.


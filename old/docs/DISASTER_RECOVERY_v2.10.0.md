# Disaster Recovery v2.10.0

> Current release policy: HyperBoostX v2.10.0 is Owner Lab / Release Candidate. Public stable remains v1.3.0 until release gates pass. Code signing remains `SKIPPED_BY_OWNER_NO_CERT`; hardware matrix expansion is still required.

## Recovery Paths

- Backend offline: UI stays responsive and shows launcher restart guidance.
- Token mismatch: relaunch through HyperBoostX launcher.
- Corrupt config: backup corrupt JSON and load defaults.
- Failed apply: no unsupported system change should be applied; show safe failure.
- Bad release install: uninstall, reinstall public stable v1.3.0, preserve/export user reports when possible.

## Stable Gate

Admin rollback smoke and installed runtime verification must pass before v2.10 gets a stable label.


# Restore Rollback Spec v2.10.0

## Policy

Any action that mutates system/app state must create or reference restore metadata before apply. If restore metadata cannot be created or verified, risky actions remain blocked.

## Current Status

- Restore session metadata exists for supported safe flows.
- Restore preview/apply/verify/export/rollback routes are covered by route contract tests.
- Stable UI hides or blocks features whose rollback flow is not safe enough.
- OS-level rollback remains guarded and limited to supported flows.
- Windows System Restore creation still requires admin/user confirmation when used.

## Stable Position

Restore metadata and safe rollback routes are sufficient for v2.10.0 stable unsigned. HyperBoostX still must not promise universal OS rollback for unsupported third-party/system changes.

# Restore Rollback Spec v2.10.0

## Policy

Any action that mutates system/app state must create or reference restore metadata before apply. If restore metadata cannot be created or verified, risky actions remain blocked.

## Current Status

- Restore session metadata exists for supported safe flows.
- Stable UI hides features whose rollback flow is not complete.
- Windows System Restore creation still needs admin/manual lab evidence.

## Stable Blocker

v2.10.0 cannot be Stable until restore/rollback is tested in admin and non-admin sessions with real install/runtime evidence.

# Action engine

1. Resolve a registered ID and validate its target and environment.
2. Read current state and compute desired state.
3. Issue a single-use preview token with a ten-minute monotonic lifetime.
4. Require affirmative user approval.
5. Acquire an exclusive cross-process file handle for application operations.
6. Re-read state under the lock; reject stale previews.
7. Record AlreadyDesiredState without rewriting if appropriate.
8. Flush a DPAPI-protected intent file, then commit SQLite metadata.
9. Record Applying; write the OS state; read back and compare typed values.
10. Report Verified only on a match. This proves configuration, not FPS or latency.
11. On failure, compensate only when current state equals the attempted desired value. Otherwise preserve state and require recovery.

Rollback validates record ownership, action/target and current state. It restores only if current state equals the original or the applied value, then reads back the original state. An external third value yields ConflictDetected.

The lock prevents competing HyperBoostX processes; it cannot create an atomic compare-and-swap across independent Windows/vendor writers. External changes can still race between native calls. UI states and docs do not promise universal atomicity.

This version has one-change sessions. Multi-action transaction orchestration, reboot continuation and genuine crash/power-loss laboratory testing remain release blockers.

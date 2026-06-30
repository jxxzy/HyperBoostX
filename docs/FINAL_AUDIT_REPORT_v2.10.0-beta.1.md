# Final Audit Report v2.10.0-beta.1

Generated: 2026-07-01 03:36 +07:00

## Honest Status

`SOURCE_BETA_READY`, `PUBLIC_STABLE_BLOCKED`.

The source tree, tests, package contents, NSIS installer, checksums, UI action map, and backend route map passed source/package QA. Public stable is blocked because installed runtime verification still sees the existing installed app as `1.3.0` and elevated clean install lab was not run in this shell.

## Required Counts

| Metric | Count |
| --- | ---: |
| Total menu | 72 |
| Total buttons | 596 |
| Total active buttons | 596 |
| Total partial buttons | 0 |
| Total backend API route rules | 365 |
| Total unique backend API paths | 361 |
| Total endpoints used by UI | 165 |
| Total tests passed | 110 |
| Python tests passed | 72 |
| .NET tests passed | 38 |
| Total real feature/menu entries | 72 |
| Total preview-only feature/menu entries | 0 |
| Total roadmap feature/menu entries | 0 |

## Validation

| Gate | Result |
| --- | --- |
| Secret scan | PASS |
| Version sync | PASS |
| PowerShell syntax | PASS |
| .NET restore/build/test Release | PASS |
| Python pytest | PASS |
| Backend route contract | PASS |
| WPF UI/UX quality | PASS |
| Real usability | PASS |
| Release artifact contents | PASS |
| Installer payload | PASS |
| Runtime verifier | FAIL/BLOCKER on installed app |
| Clean install lab | BLOCKED_BY_ENVIRONMENT, non-admin shell |
| Code signing | SKIPPED_BY_OWNER_NO_CERT |

## Known Limitations

- Installed registry still reports `1.3.0`.
- Installed backend health/version was not found in runtime audit.
- Elevated install/reinstall/silent install/silent uninstall was not run.
- Hardware matrix remains owner-lab pending.
- UI scaling screenshots are owner-lab pending.
- Code signing is skipped because no owner certificate/PFX is available.

## Blockers

- Fresh elevated install must pass.
- Installed runtime must report `2.10.0-beta.1`.
- Installed backend `/api/health` and `/api/version` must pass.
- WPF launch smoke and token sync must pass.
- No orphan process gate must pass.

## Commit/Tag/Release Status

- Commit: beta safety commit created; use current `git HEAD` for the exact commit hash.
- Tag: not created.
- GitHub release: not created.
- Stable release: not created.



# ARCHIVED HISTORICAL BETA DOCUMENT - NOT CURRENT RELEASE STATUS

This document is retained only as historical v2.10.0-beta evidence. The current public release status is HyperBoostX v2.10.0 Stable Unsigned.

---

# Current Repo Snapshot Final

Generated: 2026-07-01 03:36 +07:00

## Status

- Version: `2.10.0-beta.1`
- Branch: `feature/hyperboostx-v2-release`
- HEAD: `c304646 release: prepare HyperBoostX v2.0.0`
- Honest release state: `SOURCE_BETA_READY`
- Public stable state: `PUBLIC_STABLE_BLOCKED`
- Reason public stable is blocked: installed runtime in `C:\Program Files\HyperBoostX` still reports registry `DisplayVersion=1.3.0`, backend health was not found, and elevated clean install lab was not run in this non-admin shell.

## Current Evidence

| Area | Result |
| --- | --- |
| UI action map | 72 menus, 596 active buttons, 0 partial/roadmap buttons, 165 unique UI-used endpoints |
| Backend routes | 365 API route rules, 361 unique API paths |
| Python tests | 72 passed |
| .NET tests | 38 passed |
| Full QA gate | PASS with `-SkipInstall` |
| Installer payload | PASS |
| Installer SHA256 | `05e689131175efd1acfe40e6995b014f48228cd4156fcf99724283a3887f5a6d` |
| Code signing | `SKIPPED_BY_OWNER_NO_CERT` |
| Commit/tag/release | Not created |

## Primary Artifacts

- `HyperBoostXInstaller.exe`
- `SHA256SUMS_v2.10.0-beta.1.txt`
- `release/app/HyperBoostX.exe`
- `release/app/runtime/backend/hyperboost_backend.exe`
- `release/app/runtime/wpf/HyperBoostX.exe`


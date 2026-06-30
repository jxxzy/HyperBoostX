# Rollback Coverage Audit

Audit date: 2026-06-27

## Implemented Rollback Surfaces

| Area | Evidence | Status |
| --- | --- | --- |
| Boost undo | `/api/boost/undo`, `BoostPlanService.undo()` | WIRED |
| Restore sessions | `RestoreService.create_session`, `/api/restore/sessions`, `/api/restore/preview`, `/api/restore/apply` | WIRED |
| Startup restore | `/api/startup/restore` | WIRED |
| Game profile restore | `/api/games/profile/restore`, `/api/games/session/history` | WIRED |
| Auto gaming restore | `/api/auto-gaming/restore` | WIRED |
| Reports | `ReportService.build_report`, `/api/reports/export`, `/api/report/export` | WIRED |

## Limitations

| Area | Status | Reason |
| --- | --- | --- |
| Windows restore point creation | NEEDS_ADMIN | Requires elevated lab validation. |
| Registry/service rollback | PREVIEW_ONLY | Restore metadata is present, direct mutation is guarded. |
| Driver rollback | MANUAL | Must remain OEM/Device Manager safe. |
| Cleanup restore | PARTIAL | Safe temp preview exists; destructive deletion remains blocked. |
| DNS rollback | PREVIEW_ONLY | Apply is blocked until adapter-specific restore metadata is verified. |

## Release Gate

Rollback metadata is sufficient for source/package `2.0.1`; full machine rollback is blocked until admin/hardware installed validation is completed.


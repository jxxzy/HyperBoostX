# Action Registry

Audit date: 2026-06-27

## Registry Status

Current registry is code-backed, not yet persisted as a single generated `action_registry.json`. Action metadata lives in:

- `app/services/optimization/boost_plan_service.py`
- `app/services/product_features.py`
- `app/api/contract_v21.py`
- `wpf/ViewModels/LegacyFeatureCatalog.cs`

## Required Metadata Coverage

| Field class | Current status |
| --- | --- |
| ID/name/page/section | WIRED through routes and WPF legacy catalog. |
| Risk/approval/reversible | WIRED for boost plan and guarded product features. |
| Preview/apply/blocked | WIRED in route responses and shared WPF page chrome. |
| Rollback/report | WIRED for boost, restore, reports; partial for OS-level mutation. |
| Admin/reboot | PARTIAL; present in preview data and route warnings, not universal. |
| Protected process check | WIRED through ProtectionService. |
| Signed profile trust | NOT_IMPLEMENTED; future profile-pack gate. |

## Canonical Actions For 2.0.1

| Action ID | Endpoint | Status |
| --- | --- | --- |
| `create_restore_metadata` | `/api/boost/plan` | APPLY_READY |
| `capture_before_after_report` | `/api/boost/apply` | APPLY_READY |
| `review_overlays` | `/api/protection/evaluate-action` | PREVIEW_ONLY |
| `review_startup_apps` | `/api/startup/preview` | PREVIEW_ONLY |
| `cleanup.safe_temp_preview` | `/api/cleanup/preview` | PREVIEW_ONLY |
| `network.flush_dns` | `/api/network/flush-dns` | NEEDS_ADMIN when Windows requires elevation |
| `repair.sfc_preview` | `/api/repair/preview` | NEEDS_ADMIN |
| `windows.services.apply` | `/api/windows/services/apply` | BLOCKED_BY_SAFETY |


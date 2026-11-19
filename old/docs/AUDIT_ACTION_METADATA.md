# Action Metadata Audit

Audit date: 2026-06-27

## Implemented Metadata Sources

| Source | Metadata present |
| --- | --- |
| `app/services/optimization/boost_plan_service.py` | `id`, `title`, `risk_level`, `requires_approval`, `reversible`, `reason`, `skipped_actions`, `safety_guard`, report/undo fields. |
| `app/services/product_features.py` | Preview/apply metadata for startup, cleanup, protection, game profiles, auto gaming, restore, streaming, reports. |
| `app/api/contract_v21.py` | Standard envelope fields: `ok`, `module`, `action`, `action_id`, `page`, `status`, `message`, `data`, `warnings`, `blocked_reasons`, `requires_admin`, `requires_reboot`, `rollback_available`, `report_available`. |
| `wpf/ViewModels/LegacyFeatureCatalog.cs` | Legacy page, section, route, safety label, and flow mapping. |

## Representative Action Registry

| Action ID | Page | Risk | Preview | Approval | Rollback | Report | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `create_restore_metadata` | Boost | SAFE | Yes | No | Yes | Yes | APPLY_READY |
| `capture_before_after_report` | Boost | SAFE | Yes | No | Yes | Yes | APPLY_READY |
| `pause_<overlay>` | Boost/Gaming | SAFE/MODERATE | Yes | Yes | Yes | Yes | PREVIEW_ONLY unless approved |
| `startup_manager.preview` | Startup | MODERATE | Yes | Yes | Yes | Yes | WIRED |
| `cleanup.preview` | Cleanup | MODERATE | Yes | Yes | Limited | Yes | WIRED |
| `network.dns-preview` | Network | MODERATE | Yes | Yes | Required | Yes | PREVIEW_ONLY |
| `repair.sfc-preview` | Repair | MODERATE | Yes | Yes | N/A | Yes | NEEDS_ADMIN |
| `windows.services.apply` | Windows Services | EXPERT | Yes | Yes | Required | Yes | BLOCKED_BY_SAFETY |

## Gap

There is no single persisted `action_registry.json` generated at runtime yet. The current registry is split across service code, route matrix, and WPF legacy mapping. This is acceptable for `2.0.1` source gate but remains a P1 hardening task before claiming a complete signed profile-pack engine.


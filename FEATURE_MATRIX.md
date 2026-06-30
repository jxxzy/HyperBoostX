# HyperBoostX Feature Matrix

Status date: 2026-06-28
Active working version: `2.10.0-beta.1`
Public stable baseline: `v1.3.0`

## v2.10 Action Surface

| Metric | Count |
| --- | ---: |
| Menus | 72 |
| Buttons/actions | 596 |
| Active buttons/actions | 596 |
| Partial/roadmap/guidance buttons | 0 |
| Guarded destructive buttons | 20 |
| Unique UI endpoints used | 165 |
| Backend API route rules | 365 |
| Unique backend API paths | 361 |

## Feature Status

| Area | v2.10 status | Notes |
| --- | --- | --- |
| Dashboard/system overview | Real | Local health, scores, backend pulse, scan/report access. |
| One Click Boost | Real-safe | Plan/apply/undo path remains approval and Safety Guard gated. |
| Smart Scan / Advisor | Real | Local scans, recommendations, route contract covered. |
| Game library/profiles | Real-safe | Local library/profile flow; risky game tweaks remain guarded. |
| GPU Center | Real guidance | Hardware/vendor detection and driver guidance; no automatic driver install. |
| Process Analyzer | Real | Local process pressure, heavy process lists, protected process blocks. |
| Startup Manager | Real-safe | Preview/apply/restore, guarded against protected/system entries. |
| Cleanup | Real-safe | Preview/apply/report, no user-file deletion by default. |
| Network Tools | Real-safe | Diagnostics, DNS benchmark/apply/restore with approval boundaries. |
| Power/visual effects | Real-safe | Plan/apply/restore with admin/approval boundaries. |
| Apps/Uninstaller | Real-safe | Inventory and uninstall plan/apply; no silent arbitrary removal. |
| Windows features/services | Real-safe | Planning/status plus guarded service start/stop endpoints. |
| Security status | Real | Defender/firewall/update status; forced disable remains blocked. |
| Repair tools | Real-safe | SFC/DISM/CHKDSK command paths are explicit approval/admin gated. |
| Restore/rollback | Real | Session metadata, preview/apply/verify/export/rollback routes. |
| Reports/logs | Real | JSON/TXT/MD export and redacted local log export. |
| Automation | Real-safe | Local task metadata and enable/disable/delete, not arbitrary shell execution. |
| Beginner/Advanced/Expert modes | Real | Expert exposes detail but does not bypass Safety Guard. |
| RGB | Real-safe boundary | Software/conflict detection and approved restart guidance, not device lighting control. |
| License/cloud | Real-safe boundary | Local beta license state, not production cloud sync. |
| Plugins | Real-safe boundary | Local catalog/manifest validation; unsigned/arbitrary execution blocked. |
| Documentation/audit | Real | Feature, route, UI, safety, QA, release, and root-folder audit docs present. |

## Claims Boundary

Allowed claims:

- safe local optimization
- preview-first changes
- restoreable local actions
- hardware-aware recommendations
- local-first control center

Blocked claims:

- guaranteed FPS increase
- guaranteed ping reduction
- official NVIDIA/AMD/Intel partnership
- anti-drop 100%
- auto-fix everything
- automatic latest driver install

Canonical detail files:

- `docs/FEATURE_TRUTH_MATRIX_v2.10.0.md`
- `docs/UI_ACTION_MAP_v2.10.0.md`
- `wpf/Data/ui_action_map_v2_10.json`

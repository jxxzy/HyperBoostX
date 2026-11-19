# Feature Matrix

Audit date: 2026-06-27

| Page | Section | Legacy feature | v2 control | Has backend | Endpoint | Risk | Admin | Preview | Approval | Rollback | Report | Test | UI status | Backend status | Safety status | Fix status | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Dashboard | Quick actions | Smart Scan / Safe Boost / Restore | Buttons + live result | Yes | `/api/scan/smart`, `/api/boost/plan`, `/api/restore/sessions` | SAFE | No | Yes | Yes | Yes | Yes | Yes | RESTORED | WIRED | TESTED | TESTED | Uses real backend data. |
| One Click Boost | Presets | Safe/Balanced/Extreme/Custom | Preset action body | Yes | `/api/boost/plan`, `/api/boost/apply`, `/api/boost/undo` | SAFE to EXPERT | Some | Yes | Yes | Yes | Yes | Yes | RESTORED | WIRED | TESTED | TESTED | Extreme remains gated. |
| Performance Boost | Profiles | Daily/Work/Gaming/Streaming/Extreme | Legacy functional page | Yes | `/api/performance/plan`, `/api/boost/plan` | SAFE to EXPERT | Some | Yes | Yes | Yes | Yes | Yes | RESTORED | WIRED | TESTED | TESTED | No fake FPS claims. |
| Startup Manager | Inventory | View/disable/delay/restore startup | Functional page | Yes | `/api/startup/items`, `/api/startup/preview`, `/api/startup/restore` | MODERATE | Some | Yes | Yes | Yes | Yes | Yes | RESTORED | WIRED | TESTED | TESTED | Direct disable is guarded. |
| Background Apps | Process pressure | Top processes, whitelist, export | Functional page | Yes | `/api/processes/heavy`, `/api/protection/evaluate-action` | MODERATE | No | Yes | Yes | N/A | Yes | Yes | RESTORED | WIRED | TESTED | TESTED | No auto-kill in Beginner. |
| Cleanup | Safe cleaner | Temp/cache/log scan and preview | Functional page | Yes | `/api/cleanup/scan`, `/api/cleanup/preview`, `/api/cleanup/apply` | MODERATE | Some | Yes | Yes | Partial | Yes | Yes | RESTORED | WIRED | TESTED | TESTED | Personal folders excluded. |
| GPU Center | Hardware | Vendor/VRAM/driver/status | Dedicated page | Yes | `/api/gpu/status`, `/api/gpu/recommendations` | SAFE | No | Read-only | N/A | N/A | Yes | Yes | RESTORED | WIRED | TESTED | TESTED | Hardware lab still needed. |
| Network | DNS/latency | DNS, flush, ping, reset | Functional page | Yes | `/api/network/dns`, `/api/network/flush-dns`, `/api/network/ping` | MODERATE | Some | Yes | Yes | Partial | Yes | Yes | RESTORED | WIRED | TESTED | TESTED | DNS apply blocked until adapter rollback. |
| Repair | SFC/DISM | Repair previews | Functional page | Yes | `/api/repair/preview`, `/api/repair/sfc-preview` | MODERATE | Yes | Yes | Yes | N/A | Yes | Yes | RESTORED | WIRED | NEEDS_ADMIN | TESTED | Direct run blocked without elevated runner. |
| Restore | Rollback center | Sessions/preview/apply/export | Dedicated page | Yes | `/api/restore/sessions`, `/api/restore/preview`, `/api/restore/apply` | SAFE | Some | Yes | Yes | Yes | Yes | Yes | RESTORED | WIRED | TESTED | TESTED | System-level apply limited to supported actions. |
| Profile/automation | Profile Hub equivalent | Rules/dry-run/profile concepts | Legacy functional pages | Yes | `/api/automation/preview`, `/api/automation/rules` | MODERATE to EXPERT | Some | Yes | Yes | Yes | Yes | Yes | RESTORED | WIRED | TESTED | TESTED | Signed profile pack remains P1. |

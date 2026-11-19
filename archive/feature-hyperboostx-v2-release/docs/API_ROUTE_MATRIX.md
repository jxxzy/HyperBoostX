# API Route Matrix

Audit date: 2026-06-27

Live Flask route count: 365 `/api/*` route rules, 361 unique API paths.
Route smoke: PASS via `tests/test_runtime_route_contract.py` and `scripts/verify_backend_routes.ps1`.

| Method | Path | Purpose | Used By UI | Requires Token | Requires Admin | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/health` | Backend health | Yes | No | No | PASS | Live HTTP smoke passed. |
| GET | `/api/version` | Source/backend version | Yes | No | No | PASS | Live HTTP smoke returned `2.0.1`. |
| GET | `/api/status` | v2.1 status envelope | Yes | No | No | PASS | Live HTTP smoke returned success. |
| GET | `/api/system/info` | System info | Yes | No | No | PASS | Route contract pass. |
| GET | `/api/system/stats` | Telemetry/stats | Yes | No | No | PASS | Route contract pass. |
| POST | `/api/boost/plan` | Boost preview/plan | Yes | Yes when token env set | No | PASS | Missing token 401, valid token accepted. |
| POST | `/api/boost/apply` | Apply approved safe boost | Yes | Yes | Some actions | PASS | Approval required. |
| POST | `/api/boost/undo` | Undo/restore | Yes | Yes | Some actions | PASS | Metadata restore. |
| POST | `/api/boost/preview` | Compatibility preview | Yes | Yes | No | PASS | Standard envelope. |
| GET | `/api/advisor/safe-actions` | Safe recommendation queue | Yes | No | No | PASS | Route contract pass. |
| GET | `/api/games/library` | Game library | Yes | No | No | PASS | Route contract pass. |
| POST | `/api/games/profile/preview` | Game profile preview | Yes | Yes | No | PASS | Preview/approval. |
| GET | `/api/gpu/status` | GPU status | Yes | No | No | PASS | Vendor-aware fallback. |
| GET | `/api/processes/heavy` | Heavy process list | Yes | No | No | PASS | Protected process rules. |
| GET | `/api/startup/items` | Startup inventory | Yes | No | No | PASS | Route contract pass. |
| POST | `/api/startup/preview` | Startup preview | Yes | Yes | Some actions | PASS | Restore metadata. |
| POST | `/api/cleanup/scan` | Cleanup scan | Yes | Yes | No | PASS | Safe roots only. |
| POST | `/api/cleanup/preview` | Cleanup preview | Yes | Yes | No | PASS | Blocks user data deletion. |
| GET | `/api/network/dns` | DNS status | Yes | No | No | PASS | Route contract pass. |
| POST | `/api/network/flush-dns` | DNS flush | Yes | Yes | Often | PASS | Structured admin path. |
| POST | `/api/network/dns-apply` | DNS apply compatibility | Yes | Yes | Yes | BLOCKED_BY_SAFETY | Blocked until adapter rollback metadata. |
| GET | `/api/privacy/status` | Privacy status | Yes | No | No | PASS | Route contract pass. |
| GET | `/api/apps/list` | Apps list | Yes | No | No | PASS | Critical apps protected. |
| POST | `/api/windows/services/apply` | Service mutation | Yes | Yes | Yes | BLOCKED_BY_SAFETY | No silent protected service changes. |
| POST | `/api/repair/sfc-preview` | Repair preview | Yes | Yes | Yes | PASS | Direct run blocked without elevated runner. |
| GET | `/api/restore/sessions` | Restore sessions | Yes | No | No | PASS | Route contract pass. |
| POST | `/api/reports/export` | Export report | Yes | Yes | No | PASS | Route contract pass. |
| GET | `/api/logs/recent` | Recent logs | Yes | No | No | PASS | v2.1 envelope. |

Full machine-readable route smoke evidence: `runtime_audit/backend_routes_report.json`.



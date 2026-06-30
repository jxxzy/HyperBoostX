# Missing Routes Audit

Audit date: 2026-06-27

## Evidence

- Live Flask app exposes 245 `/api/*` routes.
- Backend route smoke: `tests/test_runtime_route_contract.py` passed 6 tests.
- `scripts/verify_backend_routes.ps1` generated `runtime_audit/backend_routes_report.json` with `ok: true`.
- WPF navigation route registration count: 55.

## Required Contract Coverage

| Required family | Current status | Representative routes |
| --- | --- | --- |
| Health/version/system | WIRED | `/api/health`, `/api/version`, `/api/system/*` |
| Boost/actions/advisor | WIRED | `/api/boost/plan`, `/api/boost/apply`, `/api/advisor/plan`, `/api/advisor/safe-actions` |
| Protection | WIRED | `/api/protection/processes`, `/api/protection/evaluate-action` |
| Gaming | WIRED | `/api/games/library`, `/api/games/running`, `/api/auto-gaming/preview` |
| Processes/startup | WIRED | `/api/processes/heavy`, `/api/startup/items`, `/api/startup/preview` |
| Cleanup/storage | WIRED | `/api/cleanup/scan`, `/api/storage/status`, `/api/storage/drives` |
| Network | WIRED | `/api/network/dns`, `/api/network/flush-dns`, `/api/network/ping` |
| Privacy/apps/tweaks | WIRED | `/api/privacy/status`, `/api/apps/list`, `/api/system-config/tweaks` |
| Repair/restore/reports/logs | WIRED | `/api/repair/preview`, `/api/restore/sessions`, `/api/reports/export`, `/api/action-log` |
| v2.1 compatibility | WIRED | `/api/status`, `/api/dashboard/summary`, `/api/performance/plan`, `/api/logs/export` |

## Route Failures

No route 404/405/500 failure is known from the current automated route smoke. Installed runtime route health is blocked because the locally installed Program Files copy has not been upgraded from `1.3.0`.


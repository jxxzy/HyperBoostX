# Route Matrix

Audit date: 2026-06-27

Live route count from `HyperBoostBackendServer().app.url_map`: 245 `/api/*` routes.

| Family | Representative routes | Status |
| --- | --- | --- |
| Core | `/api/health`, `/api/version`, `/api/status`, `/api/settings` | WIRED |
| Dashboard | `/api/dashboard/summary`, `/api/dashboard/score`, `/api/dashboard/alerts`, `/api/dashboard/activity` | WIRED |
| Scan/boost | `/api/scan/smart`, `/api/scan/quick`, `/api/boost/plan`, `/api/boost/apply`, `/api/boost/undo` | WIRED |
| Advisor/AI | `/api/advisor/plan`, `/api/advisor/safe-actions`, `/api/ai/status`, `/api/ai/plan` | WIRED |
| Gaming | `/api/games/library`, `/api/games/running`, `/api/games/profile/preview`, `/api/auto-gaming/preview` | WIRED |
| GPU/NVIDIA | `/api/gpu/status`, `/api/gpu/health`, `/api/gpu/recommendations`, `/api/nvidia/test-connection` | WIRED |
| Processes/startup | `/api/processes/heavy`, `/api/processes/background-pressure`, `/api/startup/items`, `/api/startup/preview` | WIRED |
| Cleanup/storage | `/api/cleanup/scan`, `/api/cleanup/preview`, `/api/storage/status`, `/api/storage/drives` | WIRED |
| Network | `/api/network/diagnostics`, `/api/network/dns`, `/api/network/flush-dns`, `/api/network/ping` | WIRED |
| Privacy/apps/tweaks | `/api/privacy/status`, `/api/apps/list`, `/api/system-config/tweaks`, `/api/windows/features` | WIRED |
| Repair/restore | `/api/repair/status`, `/api/repair/preview`, `/api/restore/sessions`, `/api/restore/apply` | WIRED |
| Reports/logs | `/api/reports/export`, `/api/report/export`, `/api/logs/recent`, `/api/action-log` | WIRED |

Route smoke status: PASS.


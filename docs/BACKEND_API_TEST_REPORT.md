# Backend API Test Report

Audit date: 2026-06-27

## Automated Results

| Test | Result | Evidence |
| --- | --- | --- |
| Python route/unit suite | PASS | `60 passed in 108.81s`. |
| Runtime route contract | PASS | `6 passed in 91.92s`; refreshed by `verify_backend_routes.ps1`. |
| Token missing/wrong gate | PASS | Route tests and live HTTP smoke. |
| Unauthorized clean error | PASS | `status: unauthorized_local_session`, `can_retry: true` now asserted. |
| v2.1 envelope | PASS | Contract routes assert required envelope keys. |

## Live Backend HTTP Smoke

| Check | Result |
| --- | --- |
| Port | 5099 |
| `/api/health` | `status=ok` |
| `/api/version` | `2.0.1` |
| `/api/status` | `ok=true`, `status=success` |
| `POST /api/boost/plan` missing token | `401` |
| `POST /api/boost/plan` valid token | Accepted |
| 50 repeated health checks | 0 failures, 0.13s total |
| Orphan process after smoke | None found |

Evidence file: `docs/runtime-audit/backend-live-smoke.json`.

## Limitations

Admin-required endpoints were not destructively applied because this session is non-elevated. They are tested as preview/blocked/admin-required flows.


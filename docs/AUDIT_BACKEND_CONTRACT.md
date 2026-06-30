# Backend Contract Audit

Audit date: 2026-06-27

## Contract Summary

| Contract area | Status |
| --- | --- |
| Route availability | WIRED, 245 `/api/*` routes. |
| Mutating token gate | WIRED for POST/PUT/PATCH/DELETE when `HYPERBOOSTX_SESSION_TOKEN` is set. |
| Unauthorized response | WIRED with `unauthorized_local_session` envelope. |
| v2.1 compatibility envelope | WIRED with stable keys and legacy-compatible aliases. |
| Error rendering | PARTIAL; middleware returns JSON, raw exceptions are not exposed, but not every legacy route uses the full v2.1 envelope. |

## Standard Envelope Fields

`contract_v21.py` returns: `ok`, `module`, `action`, `action_id`, `page`, `status`, `message`, `data`, `warnings`, `blocked_reasons`, `requires_admin`, `requires_reboot`, `rollback_available`, `restore_available`, `restore_session_id`, `report_available`, `report_id`.

## Tests

- `tests/test_runtime_route_contract.py`: PASS.
- `scripts/verify_backend_routes.ps1`: PASS.
- Unauthorized token test validates `ok: false`, `status: unauthorized_local_session`, `can_retry: true`.


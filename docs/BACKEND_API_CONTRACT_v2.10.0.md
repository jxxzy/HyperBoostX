# Backend API Contract v2.10.0

## Required Local Policy

- Backend binds to `127.0.0.1`.
- Sensitive routes must respect local session token policy when token is configured.
- Errors must be human-friendly JSON.
- Mutating actions must be preview/approval/guard-first.
- Dangerous actions are blocked instead of silently executed.

## Feature Registry Endpoints

| Endpoint | Purpose |
| --- | --- |
| `GET /api/features` | Full action-map backed registry. |
| `GET /api/features/audit` | Stable/Dev visibility audit. |
| `GET /api/features/stable-visible` | Real features allowed in Stable UI. |
| `GET /api/features/non-real` | Must return zero entries for the v2.10 Real-only contract. |

## Current Evidence

- Runtime route contract tests pass.
- Stable audit returns `non_real_visible_in_stable = 0`.
- 404/405/500 responses are mapped to readable JSON.

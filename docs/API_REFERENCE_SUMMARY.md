# HyperBoostX API Reference

The full current backend contract is maintained in [docs/API_REFERENCE.md](API_REFERENCE.md).

Base URL: `http://127.0.0.1:5000`

Current release: `HyperBoostX v2.10.0 Stable Unsigned`

Mutating endpoints require `X-HyperBoostX-Session` when `HYPERBOOSTX_SESSION_TOKEN` is present. v2.10.0 keeps existing v2 route shapes and compatibility aliases with a standard response envelope. Preview/read-only/blocked responses are intentional safety states, not fake automation.

Core health endpoints:

- `GET /api/health`
- `GET /api/version`
- `GET /api/release/readiness`

Final installed runtime evidence:

- `/api/health` returns version `2.10.0`.
- `/api/version` returns channel `Stable`.
- `/api/release/readiness` returns `stable_ready_unsigned`.

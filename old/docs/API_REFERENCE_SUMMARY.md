# HyperBoostX API Reference

The full current backend contract is maintained in [docs/API_REFERENCE.md](API_REFERENCE.md).

Base URL: `http://127.0.0.1:5000`

Current v2.10 status: `HyperBoostX v2.10.0 Owner Lab / Release Candidate`

Mutating endpoints require `X-HyperBoostX-Session` when `HYPERBOOSTX_SESSION_TOKEN` is present. v2.10.0 keeps existing v2 route shapes and compatibility aliases with a standard response envelope. Preview/read-only/blocked responses are intentional safety states, not fake automation.

Core health endpoints:

- `GET /api/health`
- `GET /api/version`
- `GET /api/release/readiness`

Current owner-lab runtime expectation:

- `/api/health` returns version `2.10.0`.
- `/api/version` returns channel `Owner Lab`.
- `/api/release/readiness` returns `BETA_READY` / owner-lab only until public release gates pass.


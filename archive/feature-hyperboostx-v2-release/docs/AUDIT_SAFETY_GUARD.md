# Safety Guard Audit

Audit date: 2026-06-27

## Implemented Guardrails

| Guardrail | Evidence | Status |
| --- | --- | --- |
| Mutating token gate | `app/api/middleware.py`, `X-HyperBoostX-Session` | WIRED |
| Unauthorized envelope | `status: unauthorized_local_session` with retry message | WIRED |
| Protected process evaluation | `/api/protection/evaluate-action`, `/api/protection/processes` | WIRED |
| Boost Safety Guard | `BoostPlanService.create_plan()` blocks driver/registry changes without rollback metadata | WIRED |
| AI approval guard | .NET NVIDIA Copilot tests enforce plan-only until approval | TESTED |
| Placeholder/fake UI guard | `verify_placeholder_guard.ps1` | TESTED |
| No fake FPS/ping | Dashboard/report text uses local counters and no guarantee notes | TESTED |

## Blocked In Beginner/Safe Mode

Defender/SmartScreen disable, permanent Windows Update disable, HPET/BCD/kernel timer changes, mitigation disable, root cert install, raw shell from unsigned profiles, security/anti-cheat/driver process kills, user document deletion, browser password/session deletion by default, game-save deletion, GPU driver component removal, and core audio/network service disable.

## Gate Results

- `dotnet test`: Safety-related NVIDIA Copilot tests passed.
- `pytest`: route token and clean unauthorized response tests passed.
- Risky apply routes return preview/blocked/admin/manual status instead of fake success.


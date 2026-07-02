# Module Ownership v2.10.0

Status: `STABLE_READY_UNSIGNED`

Current public release: HyperBoostX v2.10.0 Stable Unsigned. Code signing remains SKIPPED_BY_OWNER_NO_CERT, so this generator must not claim signed artifacts.

Generated: 2026-07-03 02.57.27 +07:00

| Module | Owner Role | Notes |
| --- | --- | --- |
| WPF Shell/UI | Windows desktop engineer | Sidebar, dashboard, core pages, UI state |
| Backend API | Python backend engineer | Flask route contract, local-only API, safe envelopes |
| Safety Guard | Security/QA owner | Blocked categories, preview/apply/restore policy |
| Installer/Release | Release engineer | NSIS, package, hash, install smoke, unsigned status |
| Docs/Claims | Product owner + QA | Public stable status, local-safe boundary claims |
| Hardware Lab | Owner/manual QA | External NVIDIA/AMD/Intel/no-GPU/admin/no-admin/scaling expansion |

No module may mark itself complete without test or runtime evidence.


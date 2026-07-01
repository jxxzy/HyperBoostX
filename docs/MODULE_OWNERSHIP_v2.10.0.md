# Module Ownership v2.10.0

> Current release policy: HyperBoostX v2.10.0 is the Stable Unsigned public release. Code signing remains `SKIPPED_BY_OWNER_NO_CERT`; external hardware matrix expansion is recommended.

| Module | Owner Role | Notes |
| --- | --- | --- |
| WPF Shell/UI | Windows desktop engineer | Sidebar, dashboard, CyberPageChrome, UI state |
| Backend API | Python backend engineer | Flask route contract, local-only API, safe envelopes |
| Safety Guard | Security/QA owner | Blocked categories, preview/apply/restore policy |
| Installer/Release | Release engineer | NSIS, package, hash, signing, install smoke |
| Docs/Claims | Product owner + QA | Public stable status, local-safe boundary claims |
| Hardware Lab | Owner/manual QA | NVIDIA/AMD/Intel/no-GPU/admin/no-admin/scaling matrix |

No module may mark itself stable without evidence in QA results.

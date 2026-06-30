# Module Ownership v2.10.0

> Public release policy: HyperBoostX v1.3.0 is the current recommended public stable baseline. The 2.10.0-beta.1 runtime is a Beta development build and must not be promoted as stable until installed runtime, admin rollback, hardware matrix, code signing, checksum, and smoke gates pass.

| Module | Owner Role | Notes |
| --- | --- | --- |
| WPF Shell/UI | Windows desktop engineer | Sidebar, dashboard, CyberPageChrome, UI state |
| Backend API | Python backend engineer | Flask route contract, local-only API, safe envelopes |
| Safety Guard | Security/QA owner | Blocked categories, preview/apply/restore policy |
| Installer/Release | Release engineer | NSIS, package, hash, signing, install smoke |
| Docs/Claims | Product owner + QA | Public stable status, local-safe boundary claims |
| Hardware Lab | Owner/manual QA | NVIDIA/AMD/Intel/no-GPU/admin/no-admin/scaling matrix |

No module may mark itself stable without evidence in QA results.


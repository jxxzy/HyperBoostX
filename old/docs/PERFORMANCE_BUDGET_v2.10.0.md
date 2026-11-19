# Performance Budget v2.10.0

Status: `OWNER_LAB_RELEASE_CANDIDATE`

Current v2.10 status: HyperBoostX v2.10.0 Owner Lab / Release Candidate. Public stable remains v1.3.0 until installed runtime, admin rollback, hardware matrix, and signing/public-release evidence pass.

Generated: 2026-07-03 20.38.52 +07:00

| Area | Budget |
| --- | --- |
| Backend health poll | Low-frequency; avoid UI spam |
| WPF dashboard refresh | User-initiated or conservative polling |
| Startup time | UI remains responsive with backend offline |
| Large JSON output | Kept inside Advanced Details |
| Animations | Respect Reduce Motion |


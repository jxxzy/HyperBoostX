# Performance Budget v2.10.0

Status: `STABLE_READY_UNSIGNED`

Current public release: HyperBoostX v2.10.0 Stable Unsigned. Code signing remains SKIPPED_BY_OWNER_NO_CERT, so this generator must not claim signed artifacts.

Generated: 2026-07-03 02.57.27 +07:00

| Area | Budget |
| --- | --- |
| Backend health poll | Low-frequency; avoid UI spam |
| WPF dashboard refresh | User-initiated or conservative polling |
| Startup time | UI remains responsive with backend offline |
| Large JSON output | Kept inside Advanced Details |
| Animations | Respect Reduce Motion |


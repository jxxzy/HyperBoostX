# Performance Budget v2.10.0

> Public release policy: HyperBoostX v1.3.0 is the current recommended public stable baseline. The 2.10.0-beta.1 runtime is a Beta development build and must not be promoted as stable until installed runtime, admin rollback, hardware matrix, code signing, checksum, and smoke gates pass.

## Budgets

| Area | Budget |
| --- | --- |
| Backend health poll | Low-frequency; avoid UI spam |
| WPF dashboard refresh | User-initiated or conservative polling |
| Startup time | Must remain responsive with backend offline |
| Large JSON output | Truncated in UI result panel |
| Animations | Must respect Reduce Motion |

## Stable Requirement

Run desktop smoke on low-end PC profile, normal desktop, backend offline, and high-DPI scaling before stable promotion.


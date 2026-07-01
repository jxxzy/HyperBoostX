# Performance Budget v2.10.0

> Current release policy: HyperBoostX v2.10.0 is the Stable Unsigned public release. Code signing remains `SKIPPED_BY_OWNER_NO_CERT`; external hardware matrix expansion is recommended.

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

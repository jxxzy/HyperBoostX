# UI/UX Spec v2.10.0

## Stable UI

- Dark cyber Windows desktop theme remains default.
- Sidebar is grouped by category.
- Stable mode shows Real features only.
- Dev mode may show internal diagnostics, but public feature readiness labels remain Real-only.
- Header shows Safety Guard and runtime mode.
- Pages must not be blank; unavailable data must render a useful safe state.
- Backend offline, error, blocked, and admin-required states must be human-friendly.

## Required States

Every visible Stable action must have loading, success, and error state metadata in `wpf/Data/ui_action_map_v2_10.json`.

## Current Automated Result

- Stable visible features: 72.
- Stable visible buttons: 540.
- Non-real visible in Stable: 0.
- Manual scaling checks at 100%, 125%, 150% remain manual lab work.


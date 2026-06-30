# Missing Buttons Audit

Audit date: 2026-06-27

## Counts

| Source metric | Count |
| --- | ---: |
| Navigation items | 52 |
| Registered WPF routes | 55 |
| Legacy mapped tools | 250 |
| XAML files audited | 136 |
| `<Button>` controls | 41 |
| `<CheckBox>` controls | 2 |
| List/table style controls | 19 |

## Result

No required sidebar page is missing from the current v1.3/v1.4 parity shell. `verify_ui_ux_quality.ps1`, `verify_wpf_navigation.ps1`, and `verify_wpf_button_handlers.ps1` pass.

## Not Counted As Missing

| Control family | Status | Reason |
| --- | --- | --- |
| Security/Defender disable | BLOCKED_BY_SAFETY | Unsafe for Beginner/Safe mode. |
| Driver install/update all | REMOVED_WITH_REASON | Requires official vendor/manual owner flow; no silent install. |
| Audio/network/GPU service disable | BLOCKED_BY_SAFETY | Can break core devices. |
| Cookies/session cleanup default | PREVIEW_ONLY | Destructive privacy side effects. |
| SFC/DISM direct run | NEEDS_ADMIN | Preview available; run requires elevated approved job runner. |


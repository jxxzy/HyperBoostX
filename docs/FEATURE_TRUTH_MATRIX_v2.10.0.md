# Feature Truth Matrix v2.10.0

Status: `STABLE_READY_UNSIGNED`

Current public release: HyperBoostX v2.10.0 Stable Unsigned.

## Release Truth

- v2.10.0 is the current stable unsigned public release.
- Code signing is `SKIPPED_BY_OWNER_NO_CERT`; do not claim signed distribution.
- Stable UI must expose only Real features with real visible actions.
- Dev/diagnostic mode may expose additional detail, but it must not bypass Safety Guard.
- No feature may claim guaranteed FPS, guaranteed ping, official vendor partnership, automatic driver installation, full RGB control, production cloud sync, or production license server.

## Counts

| Metric | Count |
| --- | ---: |
| Action-map menus | 72 |
| Stable-visible features | 72 |
| Hidden from Stable UI | 0 |
| Non-real visible in Stable UI | 0 |
| Stable-visible buttons | 596 |
| Stable action-map buttons | 596 |
| Partial/roadmap/guidance buttons | 0 |
| Unique UI endpoints | 165 |

## Stable Visible Features

All 72 action-map entries are classified Real. The WPF sidebar exposes the user-facing routes, while internal fallback routes remain safety/support infrastructure.

## Boundary-Safe Features

RGB software detection, cloud/license boundary state, plugin catalog validation, and driver guidance are valid only as local-safe feature boundaries. They are not claimed as full device lighting control, production cloud sync, paid activation infrastructure, remote plugin execution, or automatic driver installation.

## Rule

Real means the UI action has a command, handler/endpoint, loading/success/error state, test coverage, and Safety Guard behavior. Real does not mean risky system changes are forced or that expert mode can bypass safety.

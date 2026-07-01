# Release Blockers v2.10.0

Generated: 2026-07-02 02:31:00 +07:00

## Current Status

No active P0/P1 blocker remains after the final audit run.

## Cleared Blockers

| Blocker | Resolution |
| --- | --- |
| Installed app still showed old Dashboard UI after source changes | Rebuilt installer, reran owner admin stable gate, and refreshed installed-app screenshots. |
| Installer runtime gate could pass stale evidence | Gate now checks installer freshness against release inputs, owner evidence freshness against installer timestamp, and screenshot freshness against owner gate timestamp. |
| Dashboard showed fake scan rings/template support panels | Dashboard rebuilt around live hardware snapshot, Smart Scan results, recommendations, and activity. |
| Stable pages showed template placement notes | Core placement chrome now uses purpose-specific content with Technical Details and no visible Placement Notes. |
| Settings and About looked like generic placement pages | Both pages rebuilt as purpose-specific views. |
| Dark UI controls fell back to bright Windows controls | Added shared dark ComboBox and ScrollBar styles. |

## Remaining Non-Blocking Limitations

- Stable installer is unsigned until an owner certificate/PFX is available.
- Hardware results are not guaranteed and must be validated on the owner's target PC.
- Cloud, marketplace, driver install, RGB control, and license features must stay honestly described according to the actual local route-backed behavior.

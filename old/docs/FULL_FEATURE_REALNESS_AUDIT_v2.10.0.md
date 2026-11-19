# Full Feature Realness Audit v2.10.0

Generated: 2026-07-03 21.57.12 +07:00

Status: `PASS`

Release status: `OWNER_LAB_RELEASE_CANDIDATE_RUNTIME_BLOCKED`. The UI/action/source surface is audited as real and route-backed, but public stable remains blocked until installed-runtime, rollback/admin, hardware matrix, and signing/public-release evidence pass.

## Metrics

| Metric | Count |
| --- | ---: |
| menus | 73 |
| buttons | 606 |
| active buttons | 606 |
| unique endpoints | 173 |
| partial or roadmap buttons | 0 |
| guarded destructive buttons | 17 |
| mutating buttons | 150 |
| preview required buttons | 118 |
| restore boundary buttons | 183 |
| admin required buttons | 18 |

## Checks

| Check | Result | Evidence |
| --- | --- | --- |
| action map menu count | PASS | actual=73; expected=73 |
| action map button count | PASS | actual=606; expected=606 |
| action map unique endpoints | PASS | actual=173; expected=173 |
| all menu statuses are Real | PASS | non_real_menus=0 |
| all action statuses are Real | PASS | non_real_actions=0 |
| no Default/Fallback menu remains | PASS | menus=0; actions=0 |
| all menus are visible in WPF navigation | PASS | none |
| all menus have WPF routes | PASS | none |
| non-GET actions keep Safety Guard | PASS | unguarded_mutations=0 |
| non-preview mutations require preview state | PASS | missing_preview=0 |
| apply/restore/destructive actions expose restore boundary | PASS | missing_restore=0 |
| required owner-lab menus exist | PASS | none |
| required real endpoints exist | PASS | none |
| non-archive docs have no stale release/fallback claims | PASS | stale_files=0 |

## Realness Boundary

- `Real` means visible, route-backed, locally testable, and Safety Guard aware.
- Driver install, plugin installation, production license enforcement, cloud sync, vendor RGB/LCD control, Defender disable, permanent Windows Update disable, BIOS/OC/undervolt, and broad personal-file cleanup are not claimed as automatic production actions.
- Safe optimizer behavior intentionally returns preview, manual handoff, blocked, or restore-required states when a Windows action is risky.

## Answer

All mapped v2.10 pages and buttons are real as an owner-lab release candidate: 73 menus, 606 active buttons, 173 unique API endpoints, no Default/Fallback placeholder, no non-real stable-visible action, and all mutating actions guarded.

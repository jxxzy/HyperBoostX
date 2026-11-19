# v2 Facade Page Audit

Audit date: 2026-06-27

## Finding

The original v2 shell was premium, but several pages risked feeling generic because controls were abstracted into Preview/Apply/Undo/Export patterns. Current source now exposes legacy-specific controls through:

- `wpf/ViewModels/LegacyFeatureCatalog.cs`: 250 mapped tools.
- `wpf/Views/LegacyFeatureView.xaml`: reusable functional body for restored legacy categories.
- `wpf/Views/CyberPageChrome.xaml`: shared preview/apply/undo/export/live-result workflow.
- `wpf/ViewModels/MainWindowViewModel.cs`: 52 grouped navigation items.

## Facade Risk Matrix

| Page group | Previous risk | Current status | Evidence |
| --- | --- | --- | --- |
| Performance | Generic cards only | RESTORED | Dedicated group `Performance`; startup/process/cleanup/storage routes registered. |
| Gaming | Missing dense controls | RESTORED | Game Library, Game Profiles, Gaming Booster, Streaming, Creator, Mic/Webcam. |
| Network | Too broad | RESTORED | DNS latency, Network Booster, Network Optimization, Network Tools. |
| System config | Risky if direct apply | PREVIEW_ONLY / BLOCKED_BY_SAFETY | Windows Features/Services/Advanced Tweaks use guarded routes. |
| Repair | Risky long-running commands | PREVIEW_ONLY / NEEDS_ADMIN | SFC/DISM preview routes block silent run without elevated job runner. |

## Gate

`verify_ui_ux_quality.ps1`: PASS after sidebar group normalization to `Performance`.


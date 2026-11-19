# Competitor Benchmark Audit

Audit date: 2026-06-27
Method: public repository/release review only. No competitor source, UI assets, names, branding, or implementation were copied.

## Sources

| Competitor | Public reference | Use in HyperBoostX audit |
| --- | --- | --- |
| Platinum Optimizer | https://github.com/Aledect/Platinum-Optimizer/blob/main/Release/Platinum%2BOptimizer.8.5.Beta.V3.bat | Coverage benchmark for broad Windows tweak categories. |
| optimizerDuck | https://github.com/itsfatduck/optimizerDuck | Safety benchmark for simpler, reversible optimizer flows. |
| hellzerg/optimizer | https://github.com/hellzerg/optimizer | Toolbox benchmark for breadth of Windows utility categories. |
| optimizerNXT | https://github.com/hellzerg/optimizerNXT | Modern profile/toolbox benchmark. |
| Ultimate Optimizer | https://github.com/CRTYPUBG/ultimate-optimizer/releases | Gaming/emulator focus benchmark. |
| ET-Optimizer | https://github.com/semazurek/ET-Optimizer | Coverage and multilingual/CLI-style benchmark. |

## HyperBoostX Response

| Benchmark expectation | HyperBoostX implementation status |
| --- | --- |
| Broad Windows coverage | 52 sidebar items, 55 registered WPF routes, 245 backend API routes. |
| Safer optimization path | Mutating flows use preview/approval/restore/report language; dangerous security/driver/kernel actions are blocked or expert/manual only. |
| Gaming focus | Game Library, Game Profiles, Auto Gaming, Gaming Booster, GPU Center, Streaming Center, Creator Mode, Gaming Essentials. |
| Toolbox density | Startup, Background Apps, Processes, Cleanup, Storage, Network, Privacy, Apps, Tweaks, Repair, Restore, Logs/audit surfaces. |
| Profile concept | Boost plans and game/auto-gaming profiles call known action IDs instead of raw shell. |
| Honest claims | Local reports avoid guaranteed FPS/ping claims and expose hardware/provider limitations. |

## Legal Notes

- GPL or all-rights-reserved competitor code was not imported.
- HyperBoostX docs and UI use original wording and route names.
- Competitor review is used only as product coverage inspiration.


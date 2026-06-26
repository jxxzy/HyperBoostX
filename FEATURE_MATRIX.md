# HyperBoostX Feature Matrix

Status date: `2026-06-26`
Active version: `1.4.0`

| Feature | Backend | WPF UI | Tests | Status |
| --- | --- | --- | --- | --- |
| Cyber WPF shell | N/A | `MainWindow` shell + `Views/*` | .NET feature audit regression | Complete |
| Dashboard scores/cards | Score and system APIs | `DashboardView` | Python score tests, .NET shell tests | Complete |
| AI Performance Advisor | `/api/advisor/performance` | `AIPerformanceAdvisorView` | Python advisor tests | Complete |
| Knowledge Base | `/api/knowledge/terms` | `KnowledgeBaseView` | Python KB tests | Complete |
| Game Library | `/api/games/library` | `GameLibraryView` | Python API tests | Complete |
| Game Profiles | preview/apply/restore endpoints | `GameProfilesView` | Python safety/API tests | Partial apply facade |
| Auto Gaming Mode | settings/preview/apply/restore endpoints | `AutoGamingModeView` | Python API tests | Partial automation facade |
| GPU Center | vendor guide/recommendation endpoints | `GpuCenterView` | Python GPU tests | Complete guidance, no driver install |
| HyperBalance | process recommendation endpoints | `HyperBalanceView` | Python process tests | Partial safe analyzer |
| One Click Boost | safe boost/triple AI plan APIs | `OneClickBoostView`, dashboard plan button | Python/session tests | Plan-first complete, apply remains approval-gated |
| Process Analyzer | heavy/startup/recommendation endpoints | `ProcessAnalyzerView` | Python process tests | Complete read-only |
| Startup Manager | list/preview/apply/restore endpoints | `StartupManagerView` | Python API tests | Partial facade, conservative apply |
| Cleanup | scan/preview/apply/report endpoints | `CleanupView` | Python cleanup safety tests | Partial conservative cleanup |
| Network Tools | diagnostics/ping/DNS endpoints | `NetworkToolsView` | Python API tests | Complete diagnostics, no magic ping claim |
| Benchmark Lab | manual/import/history/export endpoints | `BenchmarkLabView` | Python benchmark tests | Local-only complete |
| Performance History | history/timeline endpoints | `PerformanceHistoryView` | Python history tests | Complete local history |
| Streaming Center | status endpoint | `StreamingCenterView` | Python API tests | Partial status/recommendation |
| Creator Mode | roadmap/service coverage | `CreatorModeView` | Limited | Partial/roadmap |
| Gaming Essentials | list/check/install-preview endpoints | `GamingEssentialsView` | Python API tests | Preview complete, install gated |
| Restore & Backup | sessions/preview/apply/verify/export endpoints | `RestoreBackupView` | Python restore tests | Partial metadata facade |
| Protected Apps | protection endpoints | `ProtectedAppsView` | Python protected process tests | Complete |
| Settings | `/api/settings/ui`, local `ui_settings.json` | `SettingsView` | Python reduce motion tests, WPF persistence smoke | Complete |
| Feature Audit | `/api/feature-audit/run` | `FeatureAuditView` | .NET regression | Complete read-only surface |
| External Monitor overlay | Not implemented | Not active | N/A | Roadmap |
| RGB control | Detection only | Roadmap surfaced | Python RGB detection | Roadmap; no control |
| Plugin marketplace | Registry foundation only | Roadmap surfaced | Python registry tests | Roadmap; no remote code |
| Cloud sync/license | Not implemented | Not active | N/A | Roadmap |

## Claims Boundary

HyperBoostX can claim safe local analysis, plan-first optimization, restore visibility, and cyber WPF UI integration. It must not claim guaranteed FPS, guaranteed ping reduction, official vendor partnership, overclocking, driver replacement, anti-cheat bypass, or universal compatibility.

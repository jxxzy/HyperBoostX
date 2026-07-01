# UI Action Map v2.10.0

Version: 2.10.0
Channel: Stable
Generated: 2026-07-01 15.05.04 +07:00

This map is the v2.10.0 source of truth for WPF menu buttons. All visible v2.10 actions are classified Real; risky operations still return Safety Guard blocks when unsafe or not approved.

## Summary

| Metric | Count |
|---|---:|
| Total menus | 72 |
| Total buttons | 596 |
| Active buttons | 596 |
| Partial/roadmap/guidance buttons | 0 |
| Guarded destructive buttons | 20 |
| Unique endpoints used by UI | 165 |

## Button Map

| Menu | Button | WPF command | Method | Endpoint | Admin | Preview | Safety | Restore | Test | Status |
|---|---|---|---|---|---:|---:|---:|---:|---|---|
| Dashboard | Run Dashboard | DashboardPrimaryCommand | POST | /api/scan/smart | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Dashboard | Preview Dashboard | DashboardPreviewCommand | GET | /api/dashboard/summary | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Dashboard | Apply Approved Dashboard | DashboardApplyCommand | POST | /api/boost/apply | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Dashboard | Restore Dashboard | DashboardRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Dashboard | Export Dashboard | DashboardExportCommand | POST | /api/reports/export | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Dashboard | Refresh Backend | DashboardRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Dashboard | Open Action Log | DashboardLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Dashboard | Release Readiness | DashboardReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Dashboard | Feature Audit Status | DashboardAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Dashboard | Safety Help | DashboardHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Smart Scan | Run Smart Scan | SmartScanPrimaryCommand | POST | /api/system/scan | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Smart Scan | Preview Smart Scan | SmartScanPreviewCommand | GET | /api/smart-scan/latest | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Smart Scan | Apply Approved Smart Scan | SmartScanApplyCommand | POST | /api/scan/smart | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Smart Scan | Restore Smart Scan | SmartScanRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Smart Scan | Export Smart Scan | SmartScanExportCommand | POST | /api/reports/export | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Smart Scan | Refresh Backend | SmartScanRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Smart Scan | Open Action Log | SmartScanLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Smart Scan | Release Readiness | SmartScanReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Smart Scan | Feature Audit Status | SmartScanAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Smart Scan | Safety Help | SmartScanHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| HyperBoost Score | Run HyperBoost Score | HyperBoostScorePrimaryCommand | GET | /api/score/engine | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| HyperBoost Score | Preview HyperBoost Score | HyperBoostScorePreviewCommand | GET | /api/history/compare | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| HyperBoost Score | Apply Approved HyperBoost Score | HyperBoostScoreApplyCommand | POST | /api/history/scans | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| HyperBoost Score | Restore HyperBoost Score | HyperBoostScoreRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| HyperBoost Score | Export HyperBoost Score | HyperBoostScoreExportCommand | GET | /api/history/export | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| HyperBoost Score | Refresh Backend | HyperBoostScoreRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| HyperBoost Score | Open Action Log | HyperBoostScoreLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| HyperBoost Score | Release Readiness | HyperBoostScoreReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| HyperBoost Score | Feature Audit Status | HyperBoostScoreAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| HyperBoost Score | Safety Help | HyperBoostScoreHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| AI Performance Advisor | Run AI Performance Advisor | AIPerformanceAdvisorPrimaryCommand | POST | /api/scan/smart | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| AI Performance Advisor | Preview AI Performance Advisor | AIPerformanceAdvisorPreviewCommand | POST | /api/advisor/plan | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| AI Performance Advisor | Apply Approved AI Performance Advisor | AIPerformanceAdvisorApplyCommand | POST | /api/boost/apply | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| AI Performance Advisor | Restore AI Performance Advisor | AIPerformanceAdvisorRestoreCommand | POST | /api/boost/undo | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| AI Performance Advisor | Export AI Performance Advisor | AIPerformanceAdvisorExportCommand | GET | /api/advisor/safe-actions | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| AI Performance Advisor | Refresh Backend | AIPerformanceAdvisorRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| AI Performance Advisor | Open Action Log | AIPerformanceAdvisorLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| AI Performance Advisor | Release Readiness | AIPerformanceAdvisorReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| AI Performance Advisor | Feature Audit Status | AIPerformanceAdvisorAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| AI Performance Advisor | Safety Help | AIPerformanceAdvisorHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| AI Center | Run AI Center | AICenterPrimaryCommand | GET | /api/ai/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| AI Center | Preview AI Center | AICenterPreviewCommand | POST | /api/ai/plan | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| AI Center | Apply Approved AI Center | AICenterApplyCommand | POST | /api/ai/approve | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| AI Center | Restore AI Center | AICenterRestoreCommand | POST | /api/ai/reject | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| AI Center | Export AI Center | AICenterExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| AI Center | Refresh Backend | AICenterRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| AI Center | Open Action Log | AICenterLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| AI Center | Release Readiness | AICenterReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| AI Center | Feature Audit Status | AICenterAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| AI Center | Safety Help | AICenterHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| NVIDIA Copilot | Run NVIDIA Copilot | NvidiaCopilotPrimaryCommand | POST | /api/nvidia/test-connection | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| NVIDIA Copilot | Preview NVIDIA Copilot | NvidiaCopilotPreviewCommand | GET | /api/settings | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| NVIDIA Copilot | Apply Approved NVIDIA Copilot | NvidiaCopilotApplyCommand | POST | /api/protection/evaluate-action | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| NVIDIA Copilot | Restore NVIDIA Copilot | NvidiaCopilotRestoreCommand | GET | /api/ai/status | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| NVIDIA Copilot | Export NVIDIA Copilot | NvidiaCopilotExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| NVIDIA Copilot | Refresh Backend | NvidiaCopilotRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| One Click Boost | Run One Click Boost | OneClickBoostPrimaryCommand | POST | /api/boost/plan | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| One Click Boost | Preview One Click Boost | OneClickBoostPreviewCommand | GET | /api/advisor/safe-actions | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| One Click Boost | Apply Approved One Click Boost | OneClickBoostApplyCommand | POST | /api/boost/apply | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| One Click Boost | Restore One Click Boost | OneClickBoostRestoreCommand | POST | /api/boost/undo | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| One Click Boost | Export One Click Boost | OneClickBoostExportCommand | POST | /api/reports/export | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| One Click Boost | Refresh Backend | OneClickBoostRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| One Click Boost | Open Action Log | OneClickBoostLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| One Click Boost | Release Readiness | OneClickBoostReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| One Click Boost | Feature Audit Status | OneClickBoostAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| One Click Boost | Safety Help | OneClickBoostHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Auto Gaming Mode | Run Auto Gaming Mode | AutoGamingModePrimaryCommand | POST | /api/auto-gaming/preview | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Auto Gaming Mode | Preview Auto Gaming Mode | AutoGamingModePreviewCommand | GET | /api/auto-gaming/settings | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Auto Gaming Mode | Apply Approved Auto Gaming Mode | AutoGamingModeApplyCommand | POST | /api/auto-gaming/apply | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Auto Gaming Mode | Restore Auto Gaming Mode | AutoGamingModeRestoreCommand | POST | /api/auto-gaming/restore | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Auto Gaming Mode | Export Auto Gaming Mode | AutoGamingModeExportCommand | GET | /api/restore/export | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Auto Gaming Mode | Refresh Backend | AutoGamingModeRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Auto Gaming Mode | Open Action Log | AutoGamingModeLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Auto Gaming Mode | Release Readiness | AutoGamingModeReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Auto Gaming Mode | Feature Audit Status | AutoGamingModeAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Auto Gaming Mode | Safety Help | AutoGamingModeHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Performance Boost | Run Performance Boost | PerformanceBoostPrimaryCommand | POST | /api/boost/plan | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Performance Boost | Preview Performance Boost | PerformanceBoostPreviewCommand | GET | /api/processes/background-pressure | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Performance Boost | Apply Approved Performance Boost | PerformanceBoostApplyCommand | POST | /api/boost/apply | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Performance Boost | Restore Performance Boost | PerformanceBoostRestoreCommand | POST | /api/boost/undo | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Performance Boost | Export Performance Boost | PerformanceBoostExportCommand | POST | /api/reports/export | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Performance Boost | Refresh Backend | PerformanceBoostRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Performance Boost | Open Action Log | PerformanceBoostLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Performance Boost | Release Readiness | PerformanceBoostReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Performance Boost | Feature Audit Status | PerformanceBoostAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Performance Boost | Safety Help | PerformanceBoostHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| CPU/RAM Optimizer | Run CPU/RAM Optimizer | CpuRamOptimizerPrimaryCommand | GET | /api/processes/analyze | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| CPU/RAM Optimizer | Preview CPU/RAM Optimizer | CpuRamOptimizerPreviewCommand | POST | /api/processes/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| CPU/RAM Optimizer | Apply Approved CPU/RAM Optimizer | CpuRamOptimizerApplyCommand | POST | /api/processes/apply | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| CPU/RAM Optimizer | Restore CPU/RAM Optimizer | CpuRamOptimizerRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| CPU/RAM Optimizer | Export CPU/RAM Optimizer | CpuRamOptimizerExportCommand | POST | /api/processes/export-report | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| CPU/RAM Optimizer | Refresh Backend | CpuRamOptimizerRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| CPU/RAM Optimizer | Open Action Log | CpuRamOptimizerLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| CPU/RAM Optimizer | Release Readiness | CpuRamOptimizerReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| CPU/RAM Optimizer | Feature Audit Status | CpuRamOptimizerAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| CPU/RAM Optimizer | Safety Help | CpuRamOptimizerHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| HyperBalance | Run HyperBalance | HyperBalancePrimaryCommand | GET | /api/processes/background-pressure | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| HyperBalance | Preview HyperBalance | HyperBalancePreviewCommand | GET | /api/processes/recommendations | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| HyperBalance | Apply Approved HyperBalance | HyperBalanceApplyCommand | POST | /api/processes/preview | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| HyperBalance | Restore HyperBalance | HyperBalanceRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| HyperBalance | Export HyperBalance | HyperBalanceExportCommand | POST | /api/processes/export-report | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| HyperBalance | Refresh Backend | HyperBalanceRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| HyperBalance | Open Action Log | HyperBalanceLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| HyperBalance | Release Readiness | HyperBalanceReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| HyperBalance | Feature Audit Status | HyperBalanceAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| HyperBalance | Safety Help | HyperBalanceHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Process Analyzer | Run Process Analyzer | ProcessAnalyzerPrimaryCommand | GET | /api/processes/analyze | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Process Analyzer | Preview Process Analyzer | ProcessAnalyzerPreviewCommand | POST | /api/processes/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Process Analyzer | Apply Approved Process Analyzer | ProcessAnalyzerApplyCommand | POST | /api/processes/apply | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Process Analyzer | Restore Process Analyzer | ProcessAnalyzerRestoreCommand | GET | /api/protection/processes | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Process Analyzer | Export Process Analyzer | ProcessAnalyzerExportCommand | POST | /api/processes/export-report | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Process Analyzer | Refresh Backend | ProcessAnalyzerRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Process Analyzer | Open Action Log | ProcessAnalyzerLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Process Analyzer | Release Readiness | ProcessAnalyzerReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Process Analyzer | Feature Audit Status | ProcessAnalyzerAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Process Analyzer | Safety Help | ProcessAnalyzerHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Background Apps | Run Background Apps | BackgroundAppsPrimaryCommand | GET | /api/processes/background-pressure | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Background Apps | Preview Background Apps | BackgroundAppsPreviewCommand | GET | /api/processes/heavy | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Background Apps | Apply Approved Background Apps | BackgroundAppsApplyCommand | POST | /api/protection/evaluate-action | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Background Apps | Restore Background Apps | BackgroundAppsRestoreCommand | GET | /api/protection/processes | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Background Apps | Export Background Apps | BackgroundAppsExportCommand | POST | /api/processes/export-report | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Background Apps | Refresh Backend | BackgroundAppsRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Startup Manager | Run Startup Manager | StartupManagerPrimaryCommand | GET | /api/startup/items | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Startup Manager | Preview Startup Manager | StartupManagerPreviewCommand | POST | /api/startup/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Startup Manager | Apply Approved Startup Manager | StartupManagerApplyCommand | POST | /api/startup/apply | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Startup Manager | Restore Startup Manager | StartupManagerRestoreCommand | POST | /api/startup/restore | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Startup Manager | Export Startup Manager | StartupManagerExportCommand | GET | /api/startup/export-report | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Startup Manager | Refresh Backend | StartupManagerRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Startup Manager | Open Action Log | StartupManagerLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Startup Manager | Release Readiness | StartupManagerReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Startup Manager | Feature Audit Status | StartupManagerAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Startup Manager | Safety Help | StartupManagerHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Cleanup | Run Cleanup | CleanupPrimaryCommand | GET | /api/cleanup/scan | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Cleanup | Preview Cleanup | CleanupPreviewCommand | POST | /api/cleanup/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Cleanup | Apply Approved Cleanup | CleanupApplyCommand | POST | /api/cleanup/apply | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Cleanup | Restore Cleanup | CleanupRestoreCommand | GET | /api/cleanup/report | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Cleanup | Export Cleanup | CleanupExportCommand | GET | /api/cleanup/export-report | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Cleanup | Refresh Backend | CleanupRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Cleanup | Open Action Log | CleanupLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Cleanup | Release Readiness | CleanupReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Cleanup | Feature Audit Status | CleanupAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Cleanup | Safety Help | CleanupHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Storage | Run Storage | StoragePrimaryCommand | GET | /api/storage/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Storage | Preview Storage | StoragePreviewCommand | POST | /api/cleanup/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Storage | Apply Approved Storage | StorageApplyCommand | POST | /api/cleanup/apply | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Storage | Restore Storage | StorageRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Storage | Export Storage | StorageExportCommand | GET | /api/cleanup/export-report | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Storage | Refresh Backend | StorageRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Tools | Run Network Tools | NetworkToolsPrimaryCommand | GET | /api/network/diagnostics | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Tools | Preview Network Tools | NetworkToolsPreviewCommand | POST | /api/network/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Tools | Apply Approved Network Tools | NetworkToolsApplyCommand | POST | /api/network/apply | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Network Tools | Restore Network Tools | NetworkToolsRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Network Tools | Export Network Tools | NetworkToolsExportCommand | GET | /api/network/export-report | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Tools | Refresh Backend | NetworkToolsRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Tools | Open Action Log | NetworkToolsLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Tools | Release Readiness | NetworkToolsReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Tools | Feature Audit Status | NetworkToolsAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Tools | Safety Help | NetworkToolsHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Booster | Run Network Booster | NetworkBoosterPrimaryCommand | GET | /api/network/diagnostics | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Booster | Preview Network Booster | NetworkBoosterPreviewCommand | POST | /api/network/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Booster | Apply Approved Network Booster | NetworkBoosterApplyCommand | POST | /api/network/apply | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Network Booster | Restore Network Booster | NetworkBoosterRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Network Booster | Export Network Booster | NetworkBoosterExportCommand | GET | /api/network/export-report | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Booster | Refresh Backend | NetworkBoosterRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Booster | Open Action Log | NetworkBoosterLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Booster | Release Readiness | NetworkBoosterReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Booster | Feature Audit Status | NetworkBoosterAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Booster | Safety Help | NetworkBoosterHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| DNS & Latency Tools | Run DNS & Latency Tools | DnsLatencyToolsPrimaryCommand | GET | /api/network/dns | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| DNS & Latency Tools | Preview DNS & Latency Tools | DnsLatencyToolsPreviewCommand | GET | /api/network/ping?host=1.1.1.1 | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| DNS & Latency Tools | Apply Approved DNS & Latency Tools | DnsLatencyToolsApplyCommand | POST | /api/network/apply | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| DNS & Latency Tools | Restore DNS & Latency Tools | DnsLatencyToolsRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| DNS & Latency Tools | Export DNS & Latency Tools | DnsLatencyToolsExportCommand | GET | /api/network/export-report | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| DNS & Latency Tools | Refresh Backend | DnsLatencyToolsRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Optimization | Run Network Optimization | NetworkOptimizationPrimaryCommand | GET | /api/network/diagnostics | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Optimization | Preview Network Optimization | NetworkOptimizationPreviewCommand | POST | /api/network/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Optimization | Apply Approved Network Optimization | NetworkOptimizationApplyCommand | POST | /api/protection/evaluate-action | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Network Optimization | Restore Network Optimization | NetworkOptimizationRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Network Optimization | Export Network Optimization | NetworkOptimizationExportCommand | GET | /api/network/export-report | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Network Optimization | Refresh Backend | NetworkOptimizationRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| GPU Center | Run GPU Center | GpuCenterPrimaryCommand | GET | /api/gpu/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| GPU Center | Preview GPU Center | GpuCenterPreviewCommand | GET | /api/gpu/recommendations | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| GPU Center | Apply Approved GPU Center | GpuCenterApplyCommand | POST | /api/protection/evaluate-action | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| GPU Center | Restore GPU Center | GpuCenterRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| GPU Center | Export GPU Center | GpuCenterExportCommand | POST | /api/gpu/export-report | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| GPU Center | Refresh Backend | GpuCenterRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| GPU Center | Open Action Log | GpuCenterLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| GPU Center | Release Readiness | GpuCenterReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| GPU Center | Feature Audit Status | GpuCenterAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| GPU Center | Safety Help | GpuCenterHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Driver Recommendation | Run Driver Recommendation | DriverRecommendationPrimaryCommand | GET | /api/drivers/recommendation | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Driver Recommendation | Preview Driver Recommendation | DriverRecommendationPreviewCommand | GET | /api/gpu/vendor-guide | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Driver Recommendation | Apply Approved Driver Recommendation | DriverRecommendationApplyCommand | POST | /api/protection/evaluate-action | True | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Driver Recommendation | Restore Driver Recommendation | DriverRecommendationRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Driver Recommendation | Export Driver Recommendation | DriverRecommendationExportCommand | POST | /api/gpu/export-report | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Driver Recommendation | Refresh Backend | DriverRecommendationRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Driver Recommendation | Open Action Log | DriverRecommendationLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Driver Recommendation | Release Readiness | DriverRecommendationReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Driver Recommendation | Feature Audit Status | DriverRecommendationAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Driver Recommendation | Safety Help | DriverRecommendationHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Driver & Update Center | Run Driver & Update Center | DriverUpdateCenterPrimaryCommand | GET | /api/drivers/recommendation | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Driver & Update Center | Preview Driver & Update Center | DriverUpdateCenterPreviewCommand | GET | /api/drivers/list | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Driver & Update Center | Apply Approved Driver & Update Center | DriverUpdateCenterApplyCommand | POST | /api/protection/evaluate-action | True | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Driver & Update Center | Restore Driver & Update Center | DriverUpdateCenterRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Driver & Update Center | Export Driver & Update Center | DriverUpdateCenterExportCommand | POST | /api/gpu/export-report | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Driver & Update Center | Refresh Backend | DriverUpdateCenterRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Driver & Update Center | Open Action Log | DriverUpdateCenterLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Driver & Update Center | Release Readiness | DriverUpdateCenterReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Driver & Update Center | Feature Audit Status | DriverUpdateCenterAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Driver & Update Center | Safety Help | DriverUpdateCenterHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Overlay Conflict Detector | Run Overlay Conflict Detector | OverlayConflictDetectorPrimaryCommand | GET | /api/overlays/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Overlay Conflict Detector | Preview Overlay Conflict Detector | OverlayConflictDetectorPreviewCommand | GET | /api/overlays/recommendations | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Overlay Conflict Detector | Apply Approved Overlay Conflict Detector | OverlayConflictDetectorApplyCommand | POST | /api/protection/evaluate-action | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Overlay Conflict Detector | Restore Overlay Conflict Detector | OverlayConflictDetectorRestoreCommand | GET | /api/protection/processes | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Overlay Conflict Detector | Export Overlay Conflict Detector | OverlayConflictDetectorExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Overlay Conflict Detector | Refresh Backend | OverlayConflictDetectorRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Overlay Conflict Detector | Open Action Log | OverlayConflictDetectorLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Overlay Conflict Detector | Release Readiness | OverlayConflictDetectorReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Overlay Conflict Detector | Feature Audit Status | OverlayConflictDetectorAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Overlay Conflict Detector | Safety Help | OverlayConflictDetectorHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| RGB Software Detector | Run RGB Software Detector | RgbSoftwareDetectorPrimaryCommand | GET | /api/rgb/detect | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| RGB Software Detector | Preview RGB Software Detector | RgbSoftwareDetectorPreviewCommand | GET | /api/rgb/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| RGB Software Detector | Apply Approved RGB Software Detector | RgbSoftwareDetectorApplyCommand | POST | /api/protection/evaluate-action | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| RGB Software Detector | Restore RGB Software Detector | RgbSoftwareDetectorRestoreCommand | GET | /api/protection/processes | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| RGB Software Detector | Export RGB Software Detector | RgbSoftwareDetectorExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| RGB Software Detector | Refresh Backend | RgbSoftwareDetectorRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Game Library | Run Game Library | GameLibraryPrimaryCommand | POST | /api/games/scan | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Game Library | Preview Game Library | GameLibraryPreviewCommand | GET | /api/games/library | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Game Library | Apply Approved Game Library | GameLibraryApplyCommand | POST | /api/games/add | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Game Library | Restore Game Library | GameLibraryRestoreCommand | POST | /api/games/remove | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Game Library | Export Game Library | GameLibraryExportCommand | POST | /api/games/session/export | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Game Library | Refresh Backend | GameLibraryRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Game Library | Open Action Log | GameLibraryLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Game Library | Release Readiness | GameLibraryReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Game Library | Feature Audit Status | GameLibraryAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Game Library | Safety Help | GameLibraryHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Game Profiles | Run Game Profiles | GameProfilesPrimaryCommand | POST | /api/games/profile/preview | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Game Profiles | Preview Game Profiles | GameProfilesPreviewCommand | GET | /api/games/library | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Game Profiles | Apply Approved Game Profiles | GameProfilesApplyCommand | POST | /api/games/profile/apply | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Game Profiles | Restore Game Profiles | GameProfilesRestoreCommand | POST | /api/games/profile/restore | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Game Profiles | Export Game Profiles | GameProfilesExportCommand | POST | /api/games/session/export | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Game Profiles | Refresh Backend | GameProfilesRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Game Profiles | Open Action Log | GameProfilesLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Game Profiles | Release Readiness | GameProfilesReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Game Profiles | Feature Audit Status | GameProfilesAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Game Profiles | Safety Help | GameProfilesHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Gaming Booster | Run Gaming Booster | GamingBoosterPrimaryCommand | POST | /api/boost/plan | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Gaming Booster | Preview Gaming Booster | GamingBoosterPreviewCommand | GET | /api/games/running | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Gaming Booster | Apply Approved Gaming Booster | GamingBoosterApplyCommand | POST | /api/boost/apply | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Gaming Booster | Restore Gaming Booster | GamingBoosterRestoreCommand | POST | /api/boost/undo | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Gaming Booster | Export Gaming Booster | GamingBoosterExportCommand | POST | /api/reports/export | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Gaming Booster | Refresh Backend | GamingBoosterRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Gaming Booster | Open Action Log | GamingBoosterLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Gaming Booster | Release Readiness | GamingBoosterReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Gaming Booster | Feature Audit Status | GamingBoosterAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Gaming Booster | Safety Help | GamingBoosterHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Gaming Essentials | Run Gaming Essentials | GamingEssentialsPrimaryCommand | GET | /api/gaming-essentials/check | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Gaming Essentials | Preview Gaming Essentials | GamingEssentialsPreviewCommand | GET | /api/essentials/list | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Gaming Essentials | Apply Approved Gaming Essentials | GamingEssentialsApplyCommand | POST | /api/essentials/install-preview | True | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Gaming Essentials | Restore Gaming Essentials | GamingEssentialsRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Gaming Essentials | Export Gaming Essentials | GamingEssentialsExportCommand | POST | /api/essentials/install-preview | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Gaming Essentials | Refresh Backend | GamingEssentialsRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Streaming Center | Run Streaming Center | StreamingCenterPrimaryCommand | GET | /api/streaming/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Streaming Center | Preview Streaming Center | StreamingCenterPreviewCommand | GET | /api/streaming/recommendations | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Streaming Center | Apply Approved Streaming Center | StreamingCenterApplyCommand | POST | /api/streaming/export-profile | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Streaming Center | Restore Streaming Center | StreamingCenterRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Streaming Center | Export Streaming Center | StreamingCenterExportCommand | POST | /api/streaming/export-profile | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Streaming Center | Refresh Backend | StreamingCenterRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Creator Mode | Run Creator Mode | CreatorModePrimaryCommand | GET | /api/creator/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Creator Mode | Preview Creator Mode | CreatorModePreviewCommand | GET | /api/creator/recommendations | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Creator Mode | Apply Approved Creator Mode | CreatorModeApplyCommand | GET | /api/processes/background-pressure | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Creator Mode | Restore Creator Mode | CreatorModeRestoreCommand | GET | /api/streaming/status | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Creator Mode | Export Creator Mode | CreatorModeExportCommand | POST | /api/reports/export | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Creator Mode | Refresh Backend | CreatorModeRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Advanced Mic Mixer | Run Advanced Mic Mixer | AdvancedMicMixerPrimaryCommand | GET | /api/streaming/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Advanced Mic Mixer | Preview Advanced Mic Mixer | AdvancedMicMixerPreviewCommand | GET | /api/streaming/recommendations | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Advanced Mic Mixer | Apply Approved Advanced Mic Mixer | AdvancedMicMixerApplyCommand | POST | /api/streaming/export-profile | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Advanced Mic Mixer | Restore Advanced Mic Mixer | AdvancedMicMixerRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Advanced Mic Mixer | Export Advanced Mic Mixer | AdvancedMicMixerExportCommand | POST | /api/streaming/export-profile | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Advanced Mic Mixer | Refresh Backend | AdvancedMicMixerRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Webcam Studio | Run Webcam Studio | WebcamStudioPrimaryCommand | GET | /api/streaming/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Webcam Studio | Preview Webcam Studio | WebcamStudioPreviewCommand | GET | /api/camera-tracking/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Webcam Studio | Apply Approved Webcam Studio | WebcamStudioApplyCommand | POST | /api/streaming/export-profile | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Webcam Studio | Restore Webcam Studio | WebcamStudioRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Webcam Studio | Export Webcam Studio | WebcamStudioExportCommand | POST | /api/streaming/export-profile | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Webcam Studio | Refresh Backend | WebcamStudioRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Camera Tracking | Run Camera Tracking | CameraTrackingPrimaryCommand | GET | /api/camera-tracking/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Camera Tracking | Preview Camera Tracking | CameraTrackingPreviewCommand | POST | /api/camera-tracking/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Camera Tracking | Apply Approved Camera Tracking | CameraTrackingApplyCommand | POST | /api/camera-tracking/preview | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Camera Tracking | Restore Camera Tracking | CameraTrackingRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Camera Tracking | Export Camera Tracking | CameraTrackingExportCommand | POST | /api/streaming/export-profile | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Camera Tracking | Refresh Backend | CameraTrackingRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Privacy Center | Run Privacy Center | PrivacyCenterPrimaryCommand | GET | /api/privacy/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Privacy Center | Preview Privacy Center | PrivacyCenterPreviewCommand | POST | /api/privacy/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Privacy Center | Apply Approved Privacy Center | PrivacyCenterApplyCommand | POST | /api/privacy/apply | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Privacy Center | Restore Privacy Center | PrivacyCenterRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Privacy Center | Export Privacy Center | PrivacyCenterExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Privacy Center | Refresh Backend | PrivacyCenterRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Security & Health | Run Security & Health | SecurityHealthPrimaryCommand | GET | /api/security/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Security & Health | Preview Security & Health | SecurityHealthPreviewCommand | POST | /api/protection/evaluate-action | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Security & Health | Apply Approved Security & Health | SecurityHealthApplyCommand | POST | /api/protection/evaluate-action | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Security & Health | Restore Security & Health | SecurityHealthRestoreCommand | GET | /api/protection/processes | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Security & Health | Export Security & Health | SecurityHealthExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Security & Health | Refresh Backend | SecurityHealthRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Security & Health | Open Action Log | SecurityHealthLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Security & Health | Release Readiness | SecurityHealthReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Security & Health | Feature Audit Status | SecurityHealthAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Security & Health | Safety Help | SecurityHealthHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| System Reality Guard | Run System Reality Guard | SystemRealityGuardPrimaryCommand | GET | /api/system-reality/overview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| System Reality Guard | Preview System Reality Guard | SystemRealityGuardPreviewCommand | POST | /api/system-reality/scan | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| System Reality Guard | Apply Approved System Reality Guard | SystemRealityGuardApplyCommand | POST | /api/system-reality/before-after/start | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| System Reality Guard | Restore System Reality Guard | SystemRealityGuardRestoreCommand | POST | /api/system-reality/before-after/stop | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| System Reality Guard | Export System Reality Guard | SystemRealityGuardExportCommand | GET | /api/system-reality/report | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| System Reality Guard | Refresh Backend | SystemRealityGuardRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| System Reality Guard | Open Action Log | SystemRealityGuardLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| System Reality Guard | Release Readiness | SystemRealityGuardReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| System Reality Guard | Feature Audit Status | SystemRealityGuardAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| System Reality Guard | Safety Help | SystemRealityGuardHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| LCD Performance Guard | Run LCD Performance Guard | LcdPerformanceGuardPrimaryCommand | GET | /api/lcd/apps | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| LCD Performance Guard | Preview LCD Performance Guard | LcdPerformanceGuardPreviewCommand | POST | /api/lcd/hybrid/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| LCD Performance Guard | Apply Approved LCD Performance Guard | LcdPerformanceGuardApplyCommand | POST | /api/lcd/hybrid/apply | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| LCD Performance Guard | Restore LCD Performance Guard | LcdPerformanceGuardRestoreCommand | POST | /api/lcd/safe-mode/preview | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| LCD Performance Guard | Export LCD Performance Guard | LcdPerformanceGuardExportCommand | GET | /api/lcd/vendors/trcc/helpers | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| LCD Performance Guard | Refresh Backend | LcdPerformanceGuardRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| LCD Performance Guard | Open Action Log | LcdPerformanceGuardLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| LCD Performance Guard | Release Readiness | LcdPerformanceGuardReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| LCD Performance Guard | Feature Audit Status | LcdPerformanceGuardAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| LCD Performance Guard | Safety Help | LcdPerformanceGuardHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Defender Scan Guard | Run Defender Scan Guard | DefenderScanGuardPrimaryCommand | GET | /api/defender/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Defender Scan Guard | Preview Defender Scan Guard | DefenderScanGuardPreviewCommand | POST | /api/defender/performance/start | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Defender Scan Guard | Apply Approved Defender Scan Guard | DefenderScanGuardApplyCommand | POST | /api/defender/exclusions/preview | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Defender Scan Guard | Restore Defender Scan Guard | DefenderScanGuardRestoreCommand | POST | /api/defender/exclusions/undo | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Defender Scan Guard | Export Defender Scan Guard | DefenderScanGuardExportCommand | GET | /api/defender/performance/report | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Defender Scan Guard | Refresh Backend | DefenderScanGuardRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Defender Scan Guard | Open Action Log | DefenderScanGuardLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Defender Scan Guard | Release Readiness | DefenderScanGuardReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Defender Scan Guard | Feature Audit Status | DefenderScanGuardAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Defender Scan Guard | Safety Help | DefenderScanGuardHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| CPU Turbo Diagnostic | Run CPU Turbo Diagnostic | CpuTurboDiagnosticPrimaryCommand | GET | /api/cpu/turbo/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| CPU Turbo Diagnostic | Preview CPU Turbo Diagnostic | CpuTurboDiagnosticPreviewCommand | POST | /api/cpu/turbo/stress-sample | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| CPU Turbo Diagnostic | Apply Approved CPU Turbo Diagnostic | CpuTurboDiagnosticApplyCommand | POST | /api/cpu/power-plan/preview | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| CPU Turbo Diagnostic | Restore CPU Turbo Diagnostic | CpuTurboDiagnosticRestoreCommand | POST | /api/cpu/power-plan/apply | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| CPU Turbo Diagnostic | Export CPU Turbo Diagnostic | CpuTurboDiagnosticExportCommand | GET | /api/cpu/turbo/bios-checklist | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| CPU Turbo Diagnostic | Refresh Backend | CpuTurboDiagnosticRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| CPU Turbo Diagnostic | Open Action Log | CpuTurboDiagnosticLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| CPU Turbo Diagnostic | Release Readiness | CpuTurboDiagnosticReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| CPU Turbo Diagnostic | Feature Audit Status | CpuTurboDiagnosticAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| CPU Turbo Diagnostic | Safety Help | CpuTurboDiagnosticHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| MSI Safe Optimizer | Run MSI Safe Optimizer | MsiSafeOptimizerPrimaryCommand | GET | /api/msi/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| MSI Safe Optimizer | Preview MSI Safe Optimizer | MsiSafeOptimizerPreviewCommand | GET | /api/msi/recommendations | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| MSI Safe Optimizer | Apply Approved MSI Safe Optimizer | MsiSafeOptimizerApplyCommand | POST | /api/protection/evaluate-action | True | True | True | True | tests/test_ui_action_map_v210.py | Real |
| MSI Safe Optimizer | Restore MSI Safe Optimizer | MsiSafeOptimizerRestoreCommand | GET | /api/protection/processes | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| MSI Safe Optimizer | Export MSI Safe Optimizer | MsiSafeOptimizerExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| MSI Safe Optimizer | Refresh Backend | MsiSafeOptimizerRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Security Reality Audit | Run Security Reality Audit | SecurityRealityAuditPrimaryCommand | GET | /api/security/reality-audit | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Security Reality Audit | Preview Security Reality Audit | SecurityRealityAuditPreviewCommand | POST | /api/security/reality-audit/run | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Security Reality Audit | Apply Approved Security Reality Audit | SecurityRealityAuditApplyCommand | GET | /api/security/powershell/activity | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Security Reality Audit | Restore Security Reality Audit | SecurityRealityAuditRestoreCommand | GET | /api/security/vendor-services/classify | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Security Reality Audit | Export Security Reality Audit | SecurityRealityAuditExportCommand | GET | /api/security/remote-access/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Security Reality Audit | Refresh Backend | SecurityRealityAuditRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Security Reality Audit | Open Action Log | SecurityRealityAuditLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Security Reality Audit | Release Readiness | SecurityRealityAuditReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Security Reality Audit | Feature Audit Status | SecurityRealityAuditAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Security Reality Audit | Safety Help | SecurityRealityAuditHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Protected Apps | Run Protected Apps | ProtectedAppsPrimaryCommand | GET | /api/protection/processes | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Protected Apps | Preview Protected Apps | ProtectedAppsPreviewCommand | POST | /api/protection/evaluate-action | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Protected Apps | Apply Approved Protected Apps | ProtectedAppsApplyCommand | POST | /api/protection/reset-defaults | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Protected Apps | Restore Protected Apps | ProtectedAppsRestoreCommand | GET | /api/protection/processes | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Protected Apps | Export Protected Apps | ProtectedAppsExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Protected Apps | Refresh Backend | ProtectedAppsRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Apps Manager | Run Apps Manager | AppsManagerPrimaryCommand | GET | /api/apps/list | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Apps Manager | Preview Apps Manager | AppsManagerPreviewCommand | GET | /api/apps/impact | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Apps Manager | Apply Approved Apps Manager | AppsManagerApplyCommand | POST | /api/apps/uninstall-preview | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Apps Manager | Restore Apps Manager | AppsManagerRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Apps Manager | Export Apps Manager | AppsManagerExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Apps Manager | Refresh Backend | AppsManagerRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| App Uninstaller | Run App Uninstaller | AppUninstallerPrimaryCommand | GET | /api/apps/list | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| App Uninstaller | Preview App Uninstaller | AppUninstallerPreviewCommand | POST | /api/apps/uninstall-preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| App Uninstaller | Apply Approved App Uninstaller | AppUninstallerApplyCommand | POST | /api/apps/uninstall-preview | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| App Uninstaller | Restore App Uninstaller | AppUninstallerRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| App Uninstaller | Export App Uninstaller | AppUninstallerExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| App Uninstaller | Refresh Backend | AppUninstallerRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Tweaks Center | Run Tweaks Center | TweaksCenterPrimaryCommand | GET | /api/system-config/tweaks | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Tweaks Center | Preview Tweaks Center | TweaksCenterPreviewCommand | POST | /api/system-config/tweaks/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Tweaks Center | Apply Approved Tweaks Center | TweaksCenterApplyCommand | POST | /api/protection/evaluate-action | True | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Tweaks Center | Restore Tweaks Center | TweaksCenterRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Tweaks Center | Export Tweaks Center | TweaksCenterExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Tweaks Center | Refresh Backend | TweaksCenterRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Tweaks Center | Open Action Log | TweaksCenterLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Tweaks Center | Release Readiness | TweaksCenterReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Tweaks Center | Feature Audit Status | TweaksCenterAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Tweaks Center | Safety Help | TweaksCenterHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Advanced Tweaks | Run Advanced Tweaks | AdvancedTweaksPrimaryCommand | GET | /api/system-config/tweaks | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Advanced Tweaks | Preview Advanced Tweaks | AdvancedTweaksPreviewCommand | POST | /api/system-config/tweaks/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Advanced Tweaks | Apply Approved Advanced Tweaks | AdvancedTweaksApplyCommand | POST | /api/protection/evaluate-action | True | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Advanced Tweaks | Restore Advanced Tweaks | AdvancedTweaksRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Advanced Tweaks | Export Advanced Tweaks | AdvancedTweaksExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Advanced Tweaks | Refresh Backend | AdvancedTweaksRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Advanced Tweaks | Open Action Log | AdvancedTweaksLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Advanced Tweaks | Release Readiness | AdvancedTweaksReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Advanced Tweaks | Feature Audit Status | AdvancedTweaksAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Advanced Tweaks | Safety Help | AdvancedTweaksHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Windows Features | Run Windows Features | WindowsFeaturesPrimaryCommand | GET | /api/windows/features | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Windows Features | Preview Windows Features | WindowsFeaturesPreviewCommand | POST | /api/windows/features/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Windows Features | Apply Approved Windows Features | WindowsFeaturesApplyCommand | POST | /api/windows/features/preview | True | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Windows Features | Restore Windows Features | WindowsFeaturesRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Windows Features | Export Windows Features | WindowsFeaturesExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Windows Features | Refresh Backend | WindowsFeaturesRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Windows Services | Run Windows Services | WindowsServicesPrimaryCommand | GET | /api/windows/services | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Windows Services | Preview Windows Services | WindowsServicesPreviewCommand | POST | /api/windows/services/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Windows Services | Apply Approved Windows Services | WindowsServicesApplyCommand | POST | /api/protection/evaluate-action | True | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Windows Services | Restore Windows Services | WindowsServicesRestoreCommand | GET | /api/protection/processes | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Windows Services | Export Windows Services | WindowsServicesExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Windows Services | Refresh Backend | WindowsServicesRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Windows Services | Open Action Log | WindowsServicesLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Windows Services | Release Readiness | WindowsServicesReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Windows Services | Feature Audit Status | WindowsServicesAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Windows Services | Safety Help | WindowsServicesHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Update Control | Run Update Control | UpdateControlPrimaryCommand | GET | /api/update-control/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Update Control | Preview Update Control | UpdateControlPreviewCommand | POST | /api/update-control/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Update Control | Apply Approved Update Control | UpdateControlApplyCommand | POST | /api/protection/evaluate-action | True | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Update Control | Restore Update Control | UpdateControlRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Update Control | Export Update Control | UpdateControlExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Update Control | Refresh Backend | UpdateControlRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Update Control | Open Action Log | UpdateControlLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Update Control | Release Readiness | UpdateControlReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Update Control | Feature Audit Status | UpdateControlAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Update Control | Safety Help | UpdateControlHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Repair Tools | Run Repair Tools | RepairToolsPrimaryCommand | GET | /api/repair/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Repair Tools | Preview Repair Tools | RepairToolsPreviewCommand | POST | /api/repair/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Repair Tools | Apply Approved Repair Tools | RepairToolsApplyCommand | POST | /api/repair/preview | True | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Repair Tools | Restore Repair Tools | RepairToolsRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Repair Tools | Export Repair Tools | RepairToolsExportCommand | POST | /api/reports/export | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Repair Tools | Refresh Backend | RepairToolsRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Power Optimization | Run Power Optimization | PowerOptimizationPrimaryCommand | GET | /api/power/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Power Optimization | Preview Power Optimization | PowerOptimizationPreviewCommand | POST | /api/power/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Power Optimization | Apply Approved Power Optimization | PowerOptimizationApplyCommand | POST | /api/power/preview | True | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Power Optimization | Restore Power Optimization | PowerOptimizationRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Power Optimization | Export Power Optimization | PowerOptimizationExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Power Optimization | Refresh Backend | PowerOptimizationRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Visual Effects | Run Visual Effects | VisualEffectsPrimaryCommand | GET | /api/visual-effects/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Visual Effects | Preview Visual Effects | VisualEffectsPreviewCommand | POST | /api/visual-effects/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Visual Effects | Apply Approved Visual Effects | VisualEffectsApplyCommand | POST | /api/visual-effects/preview | True | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Visual Effects | Restore Visual Effects | VisualEffectsRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Visual Effects | Export Visual Effects | VisualEffectsExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Visual Effects | Refresh Backend | VisualEffectsRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Restore & Backup | Run Restore & Backup | RestoreBackupPrimaryCommand | GET | /api/restore/sessions | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Restore & Backup | Preview Restore & Backup | RestoreBackupPreviewCommand | POST | /api/restore/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Restore & Backup | Apply Approved Restore & Backup | RestoreBackupApplyCommand | POST | /api/restore/apply | True | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Restore & Backup | Restore Restore & Backup | RestoreBackupRestoreCommand | POST | /api/restore/verify | True | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Restore & Backup | Export Restore & Backup | RestoreBackupExportCommand | GET | /api/restore/export | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Restore & Backup | Refresh Backend | RestoreBackupRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Restore & Backup | Open Action Log | RestoreBackupLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Restore & Backup | Release Readiness | RestoreBackupReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Restore & Backup | Feature Audit Status | RestoreBackupAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Restore & Backup | Safety Help | RestoreBackupHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Restore Point Manager | Run Restore Point Manager | RestorePointManagerPrimaryCommand | GET | /api/restore-points/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Restore Point Manager | Preview Restore Point Manager | RestorePointManagerPreviewCommand | POST | /api/restore-points/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Restore Point Manager | Apply Approved Restore Point Manager | RestorePointManagerApplyCommand | POST | /api/restore-points/preview | True | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Restore Point Manager | Restore Restore Point Manager | RestorePointManagerRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Restore Point Manager | Export Restore Point Manager | RestorePointManagerExportCommand | GET | /api/restore/export | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Restore Point Manager | Refresh Backend | RestorePointManagerRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Reports | Run Reports | ReportsPrimaryCommand | GET | /api/reports/list | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Reports | Preview Reports | ReportsPreviewCommand | GET | /api/reports/latest | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Reports | Apply Approved Reports | ReportsApplyCommand | POST | /api/reports/export | True | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Reports | Restore Reports | ReportsRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Reports | Export Reports | ReportsExportCommand | POST | /api/reports/export | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Reports | Refresh Backend | ReportsRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Reports | Open Action Log | ReportsLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Reports | Release Readiness | ReportsReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Reports | Feature Audit Status | ReportsAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Reports | Safety Help | ReportsHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Performance History | Run Performance History | PerformanceHistoryPrimaryCommand | GET | /api/performance/history | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Performance History | Preview Performance History | PerformanceHistoryPreviewCommand | GET | /api/history/trends | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Performance History | Apply Approved Performance History | PerformanceHistoryApplyCommand | POST | /api/history/scans | True | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Performance History | Restore Performance History | PerformanceHistoryRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Performance History | Export Performance History | PerformanceHistoryExportCommand | GET | /api/history/export | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Performance History | Refresh Backend | PerformanceHistoryRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Performance Report | Run Performance Report | PerformanceReportPrimaryCommand | GET | /api/reports/latest | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Performance Report | Preview Performance Report | PerformanceReportPreviewCommand | GET | /api/history/compare | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Performance Report | Apply Approved Performance Report | PerformanceReportApplyCommand | POST | /api/reports/export | True | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Performance Report | Restore Performance Report | PerformanceReportRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Performance Report | Export Performance Report | PerformanceReportExportCommand | POST | /api/reports/export | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Performance Report | Refresh Backend | PerformanceReportRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Scheduled Automation | Run Scheduled Automation | ScheduledAutomationPrimaryCommand | GET | /api/automation/rules | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Scheduled Automation | Preview Scheduled Automation | ScheduledAutomationPreviewCommand | POST | /api/automation/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Scheduled Automation | Apply Approved Scheduled Automation | ScheduledAutomationApplyCommand | POST | /api/automation/preview | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Scheduled Automation | Restore Scheduled Automation | ScheduledAutomationRestoreCommand | GET | /api/action-log | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Scheduled Automation | Export Scheduled Automation | ScheduledAutomationExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Scheduled Automation | Refresh Backend | ScheduledAutomationRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Task & Rule System | Run Task & Rule System | TaskRuleSystemPrimaryCommand | GET | /api/automation/rules | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Task & Rule System | Preview Task & Rule System | TaskRuleSystemPreviewCommand | POST | /api/automation/preview | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Task & Rule System | Apply Approved Task & Rule System | TaskRuleSystemApplyCommand | POST | /api/automation/preview | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Task & Rule System | Restore Task & Rule System | TaskRuleSystemRestoreCommand | GET | /api/action-log | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Task & Rule System | Export Task & Rule System | TaskRuleSystemExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Task & Rule System | Refresh Backend | TaskRuleSystemRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Utilities Tools | Run Utilities Tools | UtilitiesToolsPrimaryCommand | GET | /api/utilities/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Utilities Tools | Preview Utilities Tools | UtilitiesToolsPreviewCommand | GET | /api/product/storage | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Utilities Tools | Apply Approved Utilities Tools | UtilitiesToolsApplyCommand | POST | /api/protection/evaluate-action | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Utilities Tools | Restore Utilities Tools | UtilitiesToolsRestoreCommand | GET | /api/action-log | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Utilities Tools | Export Utilities Tools | UtilitiesToolsExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Utilities Tools | Refresh Backend | UtilitiesToolsRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Feature Audit | Run Feature Audit | FeatureAuditPrimaryCommand | GET | /api/feature-audit/run | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Feature Audit | Preview Feature Audit | FeatureAuditPreviewCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Feature Audit | Apply Approved Feature Audit | FeatureAuditApplyCommand | GET | /api/update/check | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Feature Audit | Restore Feature Audit | FeatureAuditRestoreCommand | GET | /api/recovery/incomplete-jobs | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Feature Audit | Export Feature Audit | FeatureAuditExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Feature Audit | Refresh Backend | FeatureAuditRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Feature Audit | Open Action Log | FeatureAuditLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Feature Audit | Release Readiness | FeatureAuditReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Feature Audit | Feature Audit Status | FeatureAuditAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Feature Audit | Safety Help | FeatureAuditHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Master Test Engine | Run Master Test Engine | MasterTestEnginePrimaryCommand | GET | /api/master-test/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Master Test Engine | Preview Master Test Engine | MasterTestEnginePreviewCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Master Test Engine | Apply Approved Master Test Engine | MasterTestEngineApplyCommand | POST | /api/master-test/run | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Master Test Engine | Restore Master Test Engine | MasterTestEngineRestoreCommand | GET | /api/update/check | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Master Test Engine | Export Master Test Engine | MasterTestEngineExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Master Test Engine | Refresh Backend | MasterTestEngineRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Master Test Engine | Open Action Log | MasterTestEngineLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Master Test Engine | Release Readiness | MasterTestEngineReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Master Test Engine | Feature Audit Status | MasterTestEngineAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Master Test Engine | Safety Help | MasterTestEngineHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Feature Audit Matrix | Run Feature Audit Matrix | FeatureAuditMatrixPrimaryCommand | GET | /api/feature-audit/matrix | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Feature Audit Matrix | Preview Feature Audit Matrix | FeatureAuditMatrixPreviewCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Feature Audit Matrix | Apply Approved Feature Audit Matrix | FeatureAuditMatrixApplyCommand | GET | /api/update/check | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Feature Audit Matrix | Restore Feature Audit Matrix | FeatureAuditMatrixRestoreCommand | GET | /api/recovery/incomplete-jobs | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Feature Audit Matrix | Export Feature Audit Matrix | FeatureAuditMatrixExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Feature Audit Matrix | Refresh Backend | FeatureAuditMatrixRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Feature Audit Matrix | Open Action Log | FeatureAuditMatrixLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Feature Audit Matrix | Release Readiness | FeatureAuditMatrixReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Feature Audit Matrix | Feature Audit Status | FeatureAuditMatrixAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Feature Audit Matrix | Safety Help | FeatureAuditMatrixHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Plugin Marketplace | Run Plugin Marketplace | PluginMarketplacePrimaryCommand | GET | /api/plugins/registry | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Plugin Marketplace | Preview Plugin Marketplace | PluginMarketplacePreviewCommand | GET | /api/product/roadmap | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Plugin Marketplace | Apply Approved Plugin Marketplace | PluginMarketplaceApplyCommand | POST | /api/protection/evaluate-action | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Plugin Marketplace | Restore Plugin Marketplace | PluginMarketplaceRestoreCommand | GET | /api/action-log | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Plugin Marketplace | Export Plugin Marketplace | PluginMarketplaceExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Plugin Marketplace | Refresh Backend | PluginMarketplaceRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Plugin Marketplace | Open Action Log | PluginMarketplaceLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Plugin Marketplace | Release Readiness | PluginMarketplaceReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Plugin Marketplace | Feature Audit Status | PluginMarketplaceAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Plugin Marketplace | Safety Help | PluginMarketplaceHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Cloud Sync & License Boundary | Run Cloud Sync & License Boundary | CloudSyncLicensePrimaryCommand | GET | /api/product/roadmap | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Cloud Sync & License Boundary | Preview Cloud Sync & License Boundary | CloudSyncLicensePreviewCommand | GET | /api/update/check | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Cloud Sync & License Boundary | Apply Approved Cloud Sync & License Boundary | CloudSyncLicenseApplyCommand | POST | /api/protection/evaluate-action | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Cloud Sync & License Boundary | Restore Cloud Sync & License Boundary | CloudSyncLicenseRestoreCommand | GET | /api/action-log | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Cloud Sync & License Boundary | Export Cloud Sync & License Boundary | CloudSyncLicenseExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Cloud Sync & License Boundary | Refresh Backend | CloudSyncLicenseRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Cloud Sync & License Boundary | Open Action Log | CloudSyncLicenseLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Cloud Sync & License Boundary | Release Readiness | CloudSyncLicenseReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Cloud Sync & License Boundary | Feature Audit Status | CloudSyncLicenseAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Cloud Sync & License Boundary | Safety Help | CloudSyncLicenseHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Release Readiness | Run Release Readiness | ReleaseReadinessPrimaryCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Release Readiness | Preview Release Readiness | ReleaseReadinessPreviewCommand | GET | /api/update/check | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Release Readiness | Apply Approved Release Readiness | ReleaseReadinessApplyCommand | GET | /api/master-test/status | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Release Readiness | Restore Release Readiness | ReleaseReadinessRestoreCommand | GET | /api/feature-audit/status | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Release Readiness | Export Release Readiness | ReleaseReadinessExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Release Readiness | Refresh Backend | ReleaseReadinessRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Release Readiness | Open Action Log | ReleaseReadinessLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Release Readiness | Release Readiness | ReleaseReadinessReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Release Readiness | Feature Audit Status | ReleaseReadinessAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Release Readiness | Safety Help | ReleaseReadinessHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Benchmark Lab | Run Benchmark Lab | BenchmarkLabPrimaryCommand | GET | /api/benchmark/latest | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Benchmark Lab | Preview Benchmark Lab | BenchmarkLabPreviewCommand | GET | /api/benchmark/history | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Benchmark Lab | Apply Approved Benchmark Lab | BenchmarkLabApplyCommand | POST | /api/benchmark/manual | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| Benchmark Lab | Restore Benchmark Lab | BenchmarkLabRestoreCommand | GET | /api/benchmark/history | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Benchmark Lab | Export Benchmark Lab | BenchmarkLabExportCommand | GET | /api/benchmark/export | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Benchmark Lab | Refresh Backend | BenchmarkLabRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Knowledge Base | Run Knowledge Base | KnowledgeBasePrimaryCommand | GET | /api/kb/topics | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Knowledge Base | Preview Knowledge Base | KnowledgeBasePreviewCommand | GET | /api/kb/search?q=dlss | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Knowledge Base | Apply Approved Knowledge Base | KnowledgeBaseApplyCommand | GET | /api/kb/search?q=safety | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Knowledge Base | Restore Knowledge Base | KnowledgeBaseRestoreCommand | GET | /api/kb/search?q=safety | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Knowledge Base | Export Knowledge Base | KnowledgeBaseExportCommand | GET | /api/kb/topics | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Knowledge Base | Refresh Backend | KnowledgeBaseRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| App Settings | Run App Settings | SettingsPrimaryCommand | GET | /api/settings/ui | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| App Settings | Preview App Settings | SettingsPreviewCommand | GET | /api/settings | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| App Settings | Apply Approved App Settings | SettingsApplyCommand | POST | /api/settings/ui | False | True | True | True | tests/test_ui_action_map_v210.py | Real |
| App Settings | Restore App Settings | SettingsRestoreCommand | GET | /api/settings | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| App Settings | Export App Settings | SettingsExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| App Settings | Refresh Backend | SettingsRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| App Settings | Open Action Log | SettingsLogCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| App Settings | Release Readiness | SettingsReadinessCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| App Settings | Feature Audit Status | SettingsAuditCommand | GET | /api/feature-audit/status | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| App Settings | Safety Help | SettingsHelpCommand | GET | /api/kb/search?q=safety | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| About App | Run About App | AboutPrimaryCommand | GET | /api/version | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| About App | Preview About App | AboutPreviewCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| About App | Apply Approved About App | AboutApplyCommand | GET | /api/update/check | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| About App | Restore About App | AboutRestoreCommand | GET | /api/update/latest | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| About App | Export About App | AboutExportCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| About App | Refresh Backend | AboutRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Default Fallback | Run Default Fallback | DefaultPrimaryCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Default Fallback | Preview Default Fallback | DefaultPreviewCommand | GET | /api/release/readiness | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Default Fallback | Apply Approved Default Fallback | DefaultApplyCommand | GET | /api/feature-audit/status | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Default Fallback | Restore Default Fallback | DefaultRestoreCommand | GET | /api/restore/sessions | False | False | True | True | tests/test_ui_action_map_v210.py | Real |
| Default Fallback | Export Default Fallback | DefaultExportCommand | GET | /api/action-log | False | False | True | False | tests/test_ui_action_map_v210.py | Real |
| Default Fallback | Refresh Backend | DefaultRefreshCommand | GET | /api/health | False | False | True | False | tests/test_ui_action_map_v210.py | Real |

## Release Rules

- Every menu has at least six active buttons.
- Big menus have at least ten active buttons.
- Mutating actions are preview/confirmation/safety-guard gated.
- Former roadmap/guidance surfaces now land on real local-safe boundary handlers such as local license state, plugin manifest validation, and RGB conflict detection.
- Stable label is allowed only with attached installed-runtime, admin rollback or owner waiver, hardware matrix, checksum, and unsigned-release evidence.

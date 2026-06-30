# Bugs Found - HyperBoostX v2.0.1 Runtime Flow Audit

## v2.0.1 Findings From 2026-06-26

1. Dashboard `One Click Safe Boost` called the removed `/api/triple-ai/full-flow` route from `wpf/Views/DashboardView.xaml.cs` through `HyperBoostBackendClient.RunTripleAiFlowAsync`, so the visible primary boost button could fail even while backend health was OK.
2. Runtime audit endpoints required by the master flow were missing or method-mismatched, including `/api/scan/smart`, `/api/advisor/plan`, `/api/advisor/safe-actions`, `/api/processes/background-pressure`, `/api/kb/*`, `/api/feature-audit/status`, `/api/update/*`, and several `GET` export aliases.
3. Route tests were too shallow: existing Python tests passed while the dashboard boost endpoint was dead and many runtime-audit aliases returned 404/405.
4. Local WPF UI config had no explicit schema version, migration history, or migration status, so old/corrupt LocalAppData could recover silently without evidence.
5. Source/package/installer version stayed at `2.0.0` while the requested fix scope is a runtime/flow patch suited to `2.0.1`.
6. Runtime verification scripts requested by the audit did not exist, so shortcut/path/backend/menu regressions were not independently checkable.
7. Current turn cannot honestly claim v2.0.1 installed-app screenshot/clean-install validation: isolated local deploy/package artifacts were rebuilt, but the installed runtime on this machine still reports `2.0.0` and `clean_install_verify.ps1 -Execute` was not run.
8. The `AUDIT_2000_FINDINGS` target cannot be met honestly from confirmed evidence in this pass; padding fake bugs would violate the user's instruction.
9. Cyber sidebar pages such as AI Advisor, GPU Center, Game Library, Startup Manager, Cleanup, Network Tools, Reports, Restore, Protected Apps, Knowledge Base, Feature Audit, and About rendered the v2 cyber chrome but mostly exposed only a generic backend refresh/status button. The UI looked complete but did not feel like the v1.3/v1.4 functional flows.
10. The new shared sidebar feature adapter initially failed for some array-shaped backend responses, such as `GET /api/advisor/safe-actions`, with `Cannot cast Newtonsoft.Json.Linq.JArray to Newtonsoft.Json.Linq.JToken`.

---

# Previous Bugs Found - HyperBoostX v2.0.0 Audit

## Active Version Drift

Active source and docs still reported `1.3.0` while the requested target was `2.0.0`.

## Missing v1.4 API Surface

The backend did not expose the requested v1.4 endpoints for AI Performance Advisor, Knowledge Base, Performance History, Game Profiles, Overlay Detector, Protected Process List, Benchmark Reports, GPU recommendations, Cleanup, Network diagnostics, Gaming Essentials, Restore sessions, Settings, Feature Audit, and roadmap foundations.

## Product Storage Gaps

Runtime config created only the older config/data/logs/backups folders. v1.4 needed reports, profiles, sessions, and diagnostics folders plus corrupted JSON recovery.

## Roadmap Risk

Some requested additions, such as global similar-hardware benchmark comparison, RGB control, cloud sync, plugin marketplace, and paid licensing, could become fake features if presented as complete without backend/data support.

## Trust Docs Missing

Root privacy, disclaimer, contributing, GitHub issue templates, PR template, and v1.4 API reference were missing or incomplete.

## Legacy WPF UI Still Active

The running WPF client still used the large legacy `MainWindow.xaml`/`MainWindow.xaml.cs` layout, old sidebar structure, and old local color resources. New cyber UI work was not fully integrated into the app shell or installer output.

## Release Blockers

Code signing, GitHub release publishing, installed app smoke, uninstall/reinstall smoke, and multi-machine validation require owner credentials or an interactive release environment.

## Signing Script Parser Error

`sign_release.ps1` placed `$ErrorActionPreference` before `param(...)`, which causes a PowerShell parser error and prevents proper signing readiness checks.

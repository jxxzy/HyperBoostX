# HyperBoostX Regression Audit From v1 To Latest

Audit date: `2026-06-26`
Branch audited: `feature/hyperboostx-v2-release`
Latest patch target: `v2.0.1 Flow Restoration & Runtime Fix Patch`

## Version History Evidence

`git fetch --all --tags --prune` completed successfully during this audit.

| Version | Evidence | Status |
| --- | --- | --- |
| Initial commit | `da1042118ff3616ce9041a79b39b0b2af4df0dd2` | Found |
| v1.0.0 | Tag exists | Found |
| v1.0.1 | Tag exists | Found |
| v1.0.2 | Tag exists | Found |
| v1.1.0 beta line | Tags `v1.1.0-beta` through `v1.1.0-beta.5` | Found |
| v1.1.0-v1.1.9 | Tags exist except `v1.1.6` tag absent while commit/release note exists | Partial tag gap |
| v1.2.0-v1.2.14 | Tags exist except `v1.2.3` tag absent while commit exists | Partial tag gap |
| v1.3.0 | Tag `v1.3.0`, commit `85cc160` | Found |
| v1.4.0 | Branch `feature/v1.4.0-ultra-complete-update`, root `RELEASE_NOTES_v1.4.0.md`; no public tag in `git tag` | Branch/docs only, tag missing |
| v2.0.0 | Tag `v2.0.0`, commit `c304646` | Found |
| latest HEAD | Working tree patch target `v2.0.1`; not committed/tagged yet | In progress |

## Major Feature Carry-Forward

| Area | Legacy Source | Latest Status | Regression Result |
| --- | --- | --- | --- |
| Core scan/plan/apply/report/restore flow | v1.0-v1.3 changelog and backend APIs | Dashboard, boost routes, reports, restore routes present | Mostly safe; dashboard boost route regression fixed in v2.0.1 |
| Streaming Mode mic/webcam tools | v1.2.8-v1.2.10 changelog and `v1.2.10:wpf/MainWindow.xaml` | Streaming Center now restores mic, Voicemeeter, webcam, presets, shortcuts, OBS/TikTok/Discord output | Regression found and fixed at UI surface level; real hardware/DSP remains partial |
| Discord webhook | v1.1.0-v1.2.12 service/tests/docs | `DiscordWebhookService`, `SecureSecretStoreService`, tests remain | No code regression found; real delivery needs owner webhook credential |
| NVIDIA Copilot | v1.2.13-v1.2.14 docs/services/tests | `NvidiaCopilotService`, 10 model registry, redaction, tests remain | No code regression found; real connection needs owner key |
| GPU Center universal vendor support | v1.3.0 tag/docs/tests | Hardware/GPU APIs and tests remain | No API regression found; hardware lab still needed for real sensors |
| Security/session token | v1.3.0 APIs/tests | Middleware, localhost bind, CORS lock, token tests remain | No regression found |
| v1.4 cyber WPF shell | v1.4 branch/docs | WPF shell/View architecture present in latest | v1.4 tag/release metadata gap remains |
| Roadmap boundaries | v1.4/v2 docs | RGB, plugins, cloud, license, external overlay remain roadmap/foundation | No overclaim should be made |

## Regressions Found In This Pass

1. `One Click Safe Boost` on the Dashboard called the removed `/api/triple-ai/full-flow` route. The route did not exist in the backend, so the primary boost CTA could fail despite the app looking feature-complete.
2. Runtime audit route aliases were missing: `/api/scan/smart`, `/api/advisor/plan`, `/api/advisor/safe-actions`, `/api/processes/background-pressure`, `/api/kb/*`, `/api/feature-audit/status`, `/api/update/*`, `/api/network/dns`, `/api/action-log`, and restore aliases without session id in the path.
3. Streaming Center regressed from the dense v1.2.10/v1.3.0 legacy Streaming Mode into a generic cyber page that did not visibly expose Advanced Mic, Voice Meter, Voicemeeter, Advanced Webcam Studio, camera presets, or OBS/TikTok/Discord profile output.
4. Local WPF UI config did not include explicit `config_schema_version`, migration history, or migration status, making old/corrupt LocalAppData recovery harder to audit.
5. Tests passed while the dashboard boost route and several runtime route aliases were broken, meaning route coverage was too shallow.

## Fixes Applied In v2.0.1 Working Tree

- Dashboard `One Click Safe Boost` now uses `CreateBoostPlanAsync`, which posts to `/api/boost/plan`.
- WPF backend client now exposes `CreateBoostPlanAsync`, `ApplyBoostPlanAsync`, and `UndoBoostPlanAsync`; legacy TripleAi wrappers map to the safe boost endpoints for compatibility.
- Backend route contract expanded with smart scan, advisor plan/safe-actions, KB aliases, history reports/compare/trends/export, process background pressure, creator/streaming recommendations, recovery, update, action-log, network DNS, and restore aliases.
- Streaming backend status now exposes `legacy_toolkit` for mic, Voicemeeter, webcam, and streaming profiles.
- Streaming Center WPF page now displays restored legacy mic/webcam/Voicemeeter/profile controls and safe shortcuts.
- WPF local config now writes `ConfigSchemaVersion`, `MigrationHistory`, and `LastMigrationStatus`, and backs up/rebuilds corrupt settings.
- Version metadata moved to `2.0.1` across `VERSION`, backend constants, WPF assembly metadata, and installer metadata.
- Added runtime verification scripts for backend routes, WPF navigation, version sync, installed runtime, clean-install dry-run, and release artifact contents.
- Added Python route contract tests and .NET static regression tests for safe boost endpoint wiring and streaming legacy UI visibility.

## Remaining Partial Or Blocked Areas

| Area | Reason |
| --- | --- |
| Streaming real mic DSP | Sliders are restored as profile guidance; real DSP/gate/compressor apply requires device-specific implementation and audio lab tests. |
| Streaming audio service reset | Admin-level service restart was not restored as an active button to avoid unsafe changes without elevated lab validation. |
| Webcam hardware property writes | Current page intentionally does not force camera driver writes; profile output is safer and honest. |
| Startup Manager apply | Conservative metadata facade; needs per-item enable/disable rollback lab coverage. |
| Cleanup apply | Conservative safe-temp/report behavior; needs deletion-boundary lab coverage before broader claims. |
| Restore/Undo | Metadata/preview/apply routes exist, but full Windows restore rollback remains lab-bound. |
| NVIDIA real API gate | Requires owner NVIDIA API key in Windows Credential Manager. |
| Discord webhook delivery | Requires owner Discord webhook credentials. |
| Code signing | Requires owner certificate/PFX. |
| Multi-machine GPU/camera/audio lab | Requires real AMD/Intel/NVIDIA, webcam, mic, and low-end/high-end Windows devices. |
| GitHub release/tag | Not performed without explicit release approval after this patch is committed. |

## Validation Evidence From This Pass

- `powershell -ExecutionPolicy Bypass -File .\scripts\verify_version_sync.ps1`: PASS.
- `powershell -ExecutionPolicy Bypass -File .\scripts\verify_wpf_navigation.ps1`: PASS.
- `powershell -ExecutionPolicy Bypass -File .\scripts\clean_install_verify.ps1`: PASS dry-run; no uninstall/install performed without `-Execute`.
- `powershell -ExecutionPolicy Bypass -File .\scripts\verify_backend_routes.ps1`: PASS; route contract `3 passed in 41.47s`.
- `powershell -ExecutionPolicy Bypass -File .\scripts\verify_repo.ps1`: PASS; Python `55 passed`, .NET Debug `30 passed`.
- `dotnet build HyperBoostX.sln -c Release -v minimal`: PASS with `0 Warning(s), 0 Error(s)`.
- `dotnet test dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj -c Release -v minimal`: PASS with `30 passed`.
- `powershell -ExecutionPolicy Bypass -File .\scripts\build_release_local.ps1`: PASS; isolated package generated under `artifacts\local-deploy`.
- `powershell -ExecutionPolicy Bypass -File .\scripts\verify_release_artifact_contents.ps1 -PackageRoot .\artifacts\local-deploy\package`: PASS.
- Packaged backend smoke from `artifacts\local-deploy\package\backend\hyperboost_backend.exe`: PASS; `/api/health` `ok`, `/api/version` `2.0.1`.
- `powershell -ExecutionPolicy Bypass -File .\scripts\verify_installed_runtime.ps1`: FAIL/ENVIRONMENT; local installed registry still reports `DisplayVersion=2.0.0`, desktop shortcut is missing, and backend is not running.

## Release Readiness Decision

Status: `PARTIAL` for installed-release readiness, `DONE` for source/package-level regression restoration.

The source-level runtime regressions found in this pass have been fixed and package-level evidence was regenerated for `v2.0.1`. The final installed release cannot honestly be called `DONE` until the v2.0.1 installer is run and installed-app smoke passes.

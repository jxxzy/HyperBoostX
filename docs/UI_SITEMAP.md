# UI sitemap and route implementation status

The native WPF shell embeds `src/HyperBoostX.App/Assets/navigation.json`, the 73-route catalog from the NEXORAID complete UI reference. Each route receives a deterministic `FeatureId` (`ui.<group>.<route>`) and a native surface binding in `FeatureRouteCatalog.cs`. Existing `ActionInfo.Id` values remain the only executable mutation identifiers; page labels and preview categories are never treated as Windows commands.

## Reference navigation

- **COMMAND CENTER (5):** Dashboard; Smart Scan; One Click Boost; HyperBoost Score; Activity & Notifications.
- **AI INTELLIGENCE (3):** AI Assistant; AI Provider Center; GPU AI Assistant.
- **OPTIMIZATION (8):** Performance Profiles; Safe Tweak Library; Advanced Tweaks; CPU Optimization; CPU Turbo Diagnostics; Smart Memory Guard; Power Optimization; Hardware Tuner.
- **GAMING (7):** Game Boost Engine; Game Library; Per-Game Profiles; Auto Gaming Mode; FPS & Frame Time; Stutter Analyzer; Overlay Conflict Detector.
- **HARDWARE (8):** Hardware Inventory; GPU Center; Hardware Vendor Center; Driver Health; Device Manager; RGB Software Detector; MSI Safe Optimizer; Laptop & Battery.
- **WINDOWS (10):** Windows Optimization; Startup Manager; Boot Optimizer; App Load Optimizer; Process & Task Manager; Windows Services; Scheduled Tasks; Debloat Center; Windows Update; Repair & Troubleshooting.
- **STORAGE (4):** Storage Center; Cleanup Center; Duplicate & Large Files; Drive Optimization.
- **NETWORK (3):** Network Center; DNS & Latency; Network Profiles.
- **CREATOR (3):** Streaming Center; Audio & DJ Performance; Webcam Diagnostics.
- **PRIVACY & SECURITY (4):** Privacy Center; Security Health; LCD Performance Guard; Trust Center.
- **AUTOMATION (2):** Scheduled Automation; Task & Rule System.
- **BACKUP & RECOVERY (3):** Backup Snapshot Center; Restore & Rollback; Emergency Recovery.
- **REPORTS & TESTS (6):** Benchmark Center; Before / After Report; Research Mode; Reports Center; Test Center; Application Health.
- **TOOLS & SETTINGS (7):** Plugin Manager; Discord Webhook; License Center; Update Center; Knowledge Base; Settings; About HyperBoostX.

The route catalog is navigation/content metadata, not a feature/action inventory. Each route states whether the mapped native surface is **IMPLEMENTED**, **LIMITED**, or **UNAVAILABLE**, and explains the actual scope. Unsupported reference items do not gain controls or sample results. In particular, frame-time/FPS capture, system readiness scoring, CPU/GPU temperature and VRAM telemetry, automatic game detection, Windows service/task mutations, duplicate-file analysis, plugin execution, and signed updating are not claimed as implemented.

## Native interaction

- Search filters all 73 route names, group names, short labels, descriptions, actions and reference feature terms. Ctrl+K focuses the search field.
- Grouped navigation uses keyboard-accessible native WPF selection and exposes each route's stable `FeatureId` through UI Automation.
- Route-specific diagnostic, integration, recovery, game library, benchmark and settings panels bind to existing application providers. Routes mapped to shared surfaces show the route's supported scope and limitations above those controls.
- Test Center and Application Health use a dedicated native panel. Safe checks cover the actual runtime, 73 route bindings, seven registered actions, read-only Startup diagnostics, current-user permissions, runtime dependencies, SQLite integrity, encrypted document/configuration integrity, recovery journal, telemetry, local error history, process resource counters and backup integrity. Results are timestamped, include duration/evidence, persist locally through the protected document store, and record remaining checks as `NOT RUN` after cancellation.
- The Test Center does not perform elevated mutations or unconsented network requests. AI/webhook connection checks are `SKIPPED` until explicitly run with consent. Privileged worker/IPC, signed file-integrity validation and automated repair are explicitly `UNSUPPORTED` in this build; no healthy/trusted status is fabricated.
- Settings export/import uses versioned schema 1 and includes only theme, goal, detail level, language and sidebar preference. Imports validate values, show a diff and require confirmation before an atomic SQLite update; credentials, profiles, reports and rollback history are excluded. Reset requires confirmation and leaves Windows configuration and recovery history intact. Profile/rule editors, broader migrations and conflict resolution remain incomplete.
- Mutations remain bound to registered `ActionInfo.Id` values and the central preview, backup, approval, apply, verification and rollback engine. No reference category or free-form prompt becomes an action.
- Telemetry uses measured CPU, memory and aggregate network values; unavailable GPU sensor readings and readiness scores remain explicitly unavailable/unscored.
- The NEXORAID palette supports dark/light and system high-contrast modes, compact/expanded sidebar, native scalable WPF layout, keyboard focus, and branding for HyperBoostX V3, NEXORAID, MR.4NONY and Unified Full Edition.

The UI is **not fully localized**: the reference catalog and multiple native controls remain English, alongside existing Indonesian guidance. System tray, a supported external monitor, broad per-route bespoke interactions, and all 73 feature implementations are not complete. See `RELEASE_STATUS.md` for the full release blockers.

## Verification

`tests/HyperBoostX.Tests/ShellTests.cs` validates all 73 route IDs, uniqueness, bindings, command availability, safe Test Center results, persistence and cancellation. `tests/HyperBoostX.Tests/EngineTests.cs` verifies read-only database, backup and encrypted-document integrity checks. `tests/HyperBoostX.Tests/RenderTests.cs` renders all 73 native route states and rejects WPF binding errors. `scripts/ui-smoke.ps1` exercises route selection and the Test Center through Windows UI Automation. These checks verify route presentation and the listed safe health checks, not implementation of every feature named by the prototype.

The canonical source-derived traceability matrix is `docs/registry/requirement-traceability.json`; regenerate it with `scripts/generate_traceability.py`. `scripts/validate_stable_gate.py` must fail when a mandatory requirement lacks verified implementation and evidence. The supplied `CODEX-UI-IMPLEMENTATION.md` was not present in the local prototype directory and is recorded as a source gap.

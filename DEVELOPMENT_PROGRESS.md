# HyperBoostX V3 development checkpoint

Updated: 2026-11-16 (Asia/Jakarta). Active requirements: `docs/MASTER_DEVELOPMENT_TASK.md`.

Latest continuation: implemented and UI-smoke-verified per-user single-instance activation: a second launch against the same isolated data directory restores the existing minimized window, while a distinct data directory runs an independent instance. Added a narrow shutdown-race guard to activation dispatch. The supplied technical reference matrix is checked into `docs/registry/technical-reference-matrix.json`; the canonical generator registers all 365 assignments across 73 menus and 158 distinct URLs (109 source-tagged web-confirmed; 49 candidate-direct-link). All 158 distinct URLs received limited source-level checks; five supplied URLs returned 404, with alternatives identified for three, and one vendor page returned 403. These checks do not establish API/license/platform compatibility. Added `docs/SECURITY_THREAT_MODEL.md`, a source-grounded description of the current asInvoker/in-process boundary, threats, required broker protocol, and isolated test gates. It does not claim that a privileged worker, authenticated IPC, or security audit exists. The generator records the threat-model document as verified, privilege separation as partial, and privileged IPC as unsupported. Current matrix: 4,039 requirements; 4,022 mandatory items remain unverified. Stable remains blocked. The persistent ordered backlog is `docs/registry/development-backlog.json`.

## Objective and scope

Migrate supported V1–V2.10 capabilities into working native V3 features, verify mutation/rollback, complete UI, test and package. No public deployment or destructive host testing is authorized. Stable is blocked until the full master gate is met. Unsupported hardware/permissions must be reported accurately.

## Workspace

- Active source: `v3 stable final` (.NET 8, WPF, native Windows providers, SQLite/DPAPI, NSIS).
- `V1` contains latest legacy V2.10 Python/WPF source despite its directory name.
- Git tags contain V1.0–V2.10. V3 beta is another prototype, not a proven complete implementation.
- Root Git had hundreds of deleted old paths and untracked version directories before this work. Do not restore or discard those user changes.

## Completed and verified previously

90 tests passed, including session restore cancellation/retry, Game Library import, runtime-action registry parity, per-process CPU calculations and grouped-navigation/sidebar persistence. The host has exercised 17 diagnostic centers; the native shell now exposes all 73 reference routes, with route-by-route limitation states and verified bindings to existing native panels. This is navigation parity, not implementation of all 73 feature sets. Recovery, report history, export redaction, selection, startup refresh, packaging and CI repairs are described in `docs/AUDIT_FIX_2026-11-16.md`. The local unsigned installer and portable archive have been rebuilt from the 73-route source; installer lifecycle testing is still pending.

## Current work in progress

- Added encrypted `DocumentStore` for domain data.
- Added native GameLibrary: validated executable registration, duplicate checks, profile persistence, remove/export, exact executable process matching.
- Added GameSessions: approved preview batch, durable session status, engine-backed apply/rollback, partial failure compensation, crash-recovery lookup, external drift protection.
- Added UI launch/watch/restore lifecycle. Auto-restore operates while app remains open; direct game executables are required, not short-lived launcher processes. Close warns about outstanding session.
- Added Benchmark Lab: validated manual measurements, transactional CSV import, 200-entry history, before/after comparison and export through reports. Input remains labelled user-reported, never measured automatically.
- Added GameLibraryPanel and BenchmarkPanel plus native WPF commands. Game Library JSON import now validates every local executable/profile before atomically merging, reassigns IDs, skips duplicate paths, and caps import size; export includes usable executable paths only after an explicit privacy warning.
- Updated the runtime action catalog from six to seven registered actions to include the real temporary-file quarantine provider; a runtime test now checks the full typed catalog against executable handlers.
- Processes diagnostics now samples per-process CPU over two observations, normalizes against logical processors, reports unavailable processes, and labels the value as an interval average rather than cumulative time or continuous telemetry.
- A canceled session restore now propagates cancellation while persisting `RecoveryRequired`, so remaining changes can be retried rather than presented as a completed restore.
- Reworked the WPF shell to the NEXORAID dark/light palette with grouped, searchable, collapsible navigation; a compact sidebar; breadcrumb and header actions; persisted theme, sidebar and last-page choices; and responsive dashboard telemetry cards. The readiness score is explicitly unmeasured, CPU chart waits for a second sample, and GPU sensors remain unavailable rather than displaying prototype values.
- Embedded the reference `navigation.json` with all 73 routes, stable `FeatureId` values, complete route-to-native-surface bindings and explicit Implemented/Limited/Unsupported disclosures. A route only exposes registered native actions for its bound surface; unsupported routes cannot scan, apply or launch mapped Windows tools. Existing feature IDs remain the only mutation IDs.
- Expanded `scripts/ui-smoke.ps1` to select all 73 reference routes through desktop UI Automation and verify each page heading actually changes. Latest run passed 73/73, and captured dark/light dashboard, compact sidebar, Game Boost Engine, Game Library and Settings states in `artifacts/ui-73-route-smoke-1182fead3a014658ad3112b1f98b5d37`.
- `RenderTests` rendered every reference route and detected no WPF binding errors. `ShellTests` checks 73 unique route names/IDs, complete route mappings, command availability, and that every registered executable action is reachable from a native route.
- `dotnet build HyperBoostX.sln -c Release --no-restore` succeeds with 0 warnings/errors. Latest `dotnet test HyperBoostX.sln -c Release --no-restore` passes 103 tests, 0 failures, including 73-route rendering and action binding coverage.
- `scripts/package.ps1` passed the 90-test gate and rebuilt self-contained win-x64 publish, NSIS installer and portable ZIP. UI Automation against the published `artifacts/publish/win-x64/HyperBoostX.exe` passed all 73 route selections and page-heading checks; evidence is in `artifacts/published-ui-73-smoke-76d51cf0f4f441108b3d14752c4fda38`. Both package SHA-256 values were independently checked against `dist/SHA256SUMS.txt`; portable contents include the desktop app and CLI. Installer signature status is `NotSigned`; it was not run.
- Current-host CLI smoke: `hbx scan Processes --data-dir <isolated temp>` returned 141 sampled process rows and an explicit approximately 500 ms interval methodology; the isolated data directory was removed.

## Latest continuation (2026-11-16)

- Added a real DNS-resolution action to the native DNS & Latency route. It validates host/IP input, uses the Windows system resolver, records measured elapsed time and returned addresses, explicitly reports resolver failures, and makes no network configuration changes. IP input is identified as not requiring a DNS query.
- Added localized control/input/status labels for all six language packs. Added regression coverage for localhost resolution, IP inputs, URL rejection, cancellation, and route-specific command availability.
- Ported the read-only Windows boot Event 100 measurement concept from the later V3 beta into Startup diagnostics. Boot, main-path, and post-boot times are reported only when the event provides valid non-negative durations; otherwise each value is explicitly unavailable. No boot changes are applied.
- Ported the safe part of V1 StartupService coverage by enumerating automatic Windows services and scheduled tasks through read-only WMI; the Windows Services and Scheduled Tasks pages now expose bounded read-only inventories through the same provider. V1's suggested startup impact scores and enable/disable methods were not ported because the scores were heuristics and those methods did not mutate Windows.
- Added a native observation filter that searches diagnostic name, value, source, quality and availability fields; selecting a row shows its details in the active language, and changing routes clears stale selection/filter state.
- Integrated a localized Windows system-tray menu using the executable's embedded icon, with Show and Exit actions; window close still honors active-operation and game-session recovery prompts.
- `dotnet test HyperBoostX.sln -c Release --no-restore` passed 98/98. The latest self-contained published UI smoke passed all 73 routes, resolved `localhost`, scanned and filtered Windows service/task inventories, selected rows, verified visible item details, and exited cleanly with the tray icon initialized/disposed.
- Compared the DNS route with the referenced NEXORAID browser prototype. Resolver comparison/selection and DNS mutation were not copied: they require provider disclosure/consent, elevated write authorization, backup, rollback and verified recovery that this build does not provide. DNS provider selection and changes remain explicitly unsupported.

## Next priority

- The P0 elevated worker and authenticated, allowlisted IPC are not implemented. Do not expose a generic command runner or add elevated mutations until request validation, identity/consent, durable backups, verification, rollback, and isolated Windows tests are designed and exercised.
- No disposable Windows VM is available in this session; privileged service/task/DNS mutation, crash/reboot recovery, installer lifecycle, hardware compatibility, and soak tests remain NOT RUN.
- Next unblocked task: validate API signatures, licenses, Windows support, dependencies, provider limits, and acceptance evidence for the 365 individual reference assignments. The P0 privileged-worker implementation is blocked until a disposable Windows environment is available. See `docs/registry/development-backlog.json`.

## Outstanding

Historical 606-action mapping to verified handler/test equivalence; implementation of most of the 73 route feature sets; full Indonesian/English localization; supported external monitor; process controls and full sensor coverage (GPU/VRAM/temperature/vendor providers are not available); cleanup beyond the bounded old-.tmp quarantine; supported privileged workflows; rule scheduling, plugin isolation, signed updater and compatibility packaging; installed-runtime/VM/hardware testing; remaining release evidence. CPU/RAM/network sampling and per-process interval CPU estimates exist but do not satisfy the broader master requirements.

## Test constraints and next verification

Use isolated local fixtures for mutations. Real user power/startup/cleanup settings must not be changed by tests. Computer Use runtime is unavailable in this session; WPF offscreen rendering and component tests are available. Record interactive UI, reboot, hardware matrix, installer lifecycle, live credentials/provider tests as NOT RUN until actually performed.

Run `dotnet test HyperBoostX.sln -c Release` after feature tests and use the isolated UI smoke for WPF navigation/rendering. Continue implementation; update this checkpoint before context handoff. Do not label these additions Stable from compilation alone.

## Continuation (2026-11-16) — bounded duplicate hashing
- Added a 2 GiB cumulative hash-read budget to duplicate scanning. Scans stop hashing when the budget would be exceeded and mark the result truncated, so coverage is not reported complete; large-file listing remains available.
- Focused verification: dotnet test tests/HyperBoostX.Tests/HyperBoostX.Tests.csproj -c Release --no-restore passed 112/112.
- Remaining limitation: a dedicated boundary test with sparse large fixtures was not added. Stable Release remains blocked by the outstanding items recorded in RELEASE_STATUS.md; this change does not establish production readiness.

## Continuation (2026-11-16) — testable scan bound and privileged protocol foundation

- Made the 2 GiB duplicate hash-read ceiling testable through an internal overload and added boundary/invalid-budget coverage. A scan stops before exceeding its budget, reports `Truncated`, and cannot report complete coverage.
- Added a versioned privileged-request envelope and validator foundation. It enforces a 256-bit secret, HMAC-SHA256 authentication, request/action/target/body bounds, exact SID and session binding, a two-minute timestamp window, 256-bit nonces, action-specific payload validation, and replay rejection. Unknown action IDs are denied; the protocol contains no command line, executable, or script field. Null/malformed fields, weak signing secrets, and exceptions from action validators fail closed.
- Verification: `dotnet test tests/HyperBoostX.Tests/HyperBoostX.Tests.csproj -c Release --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1` passed 119/119. The first ordinary test invocation was blocked by an IDE/build-process lock on a temporary WPF NuGet props file; the isolated build-server run passed.
- This is protocol validation only. Named-pipe transport, Windows pipe ACLs, connected-client token/SID/session verification, UAC launch, worker lifecycle, secret handoff, shutdown, action handlers, and disposable-VM security/integration tests remain unimplemented. No elevated process or Windows mutation was executed.

## Continuation (2026-11-16) — bounded IPC framing and Windows client identity

- Added a 16 KiB length-prefixed JSON frame codec. It reads exact frames, rejects zero/oversized/truncated frames, uses case-sensitive fields, rejects unknown fields, limits JSON depth, and never allocates from an unvalidated frame length.
- Added Windows named-pipe client identity inspection using the documented `GetNamedPipeClientProcessId`, `GetNamedPipeClientSessionId`, and client impersonation APIs. A local integration test verifies the connected process ID, Windows session, SID, and account name.
- Added an explicit protected pipe factory for the cross-elevation design. Its DACL is inheritance-protected and grants read/write only to the initiating user's SID; local integration verifies the authorized SID can connect. The official Microsoft `System.IO.Pipes.AccessControl` 5.0.0 package is locked as the ACL provider. `PipeOptions.CurrentUserOnly` remains suitable for same-elevation tests but is intentionally not used for the future cross-elevation endpoint because Windows also checks elevation level.
- Verification: focused suite passed 123/123 after correcting an initially invalid extra handle-right parameter. No UAC prompt, elevated process, or system mutation was run. Worker launch, two-way PID handshake, secret transfer, request dispatch, timeout/shutdown, audit integration, and disposable-VM negative tests remain outstanding.
- Dependency advisory check: `dotnet list HyperBoostX.sln package --vulnerable --include-transitive` reported no vulnerable packages for Core, Windows, App, CLI, or Tests using the NuGet.org advisory source.

## Continuation (2026-11-16) — authenticated one-request broker endpoint

- Added authenticated response envelopes with a separate HMAC domain, request-ID and request-nonce binding, fixed-time tag validation, strict framing, and tamper rejection.
- Added a one-request Windows pipe endpoint that verifies the connected PID, session, and SID against its authorized launch context before reading a request. It validates authentication, replay, allowlist, and action payload before dispatch; rejected requests never reach the handler.
- Dispatch has a bounded timeout, bounded result code/message/body, fail-closed invalid-result handling, and generic external errors that do not expose handler exception details. Responses are signed even for authentication or dispatch failures so the desktop can distinguish the expected worker from a spoofed responder.
- Same-elevation integration tests exercise accepted dispatch, tampered-request denial with zero dispatch calls, authenticated response binding/framing, and exception-detail suppression. Focused suite passed 127/127. The endpoint has no production mutation handlers and is not launched elevated; cross-elevation handshake and VM attack tests remain outstanding.

## Continuation (2026-11-16) — peer-bound ephemeral secret bootstrap

- Added server-process/session inspection for the desktop side and a two-way ECDH P-256 bootstrap. The worker side first verifies the desktop PID, SID, and session; the desktop side verifies the server PID and session before either accepts the peer key.
- The derived 256-bit key is domain-separated and bound to the random pipe name plus both ephemeral public keys. Shared ECDH material is zeroed after derivation. No secret is placed in command-line arguments, environment variables, or files.
- Local Windows integration proves both verified peers derive the same nonzero key and that an unexpected client or server PID is rejected before key exchange. Focused suite passed 129/129. The elevated executable/launch path and cross-elevation VM validation are still absent; this test uses two ends owned by the current non-elevated test process.

## Continuation (2026-11-16) — separate worker lifecycle and packaged artifacts

- Added `HyperBoostX.Worker`, a separate bounded process with strict launch arguments, same-user SID enforcement, 30-second connect/idle timeouts, a maximum of 64 requests, ECDH bootstrap, authenticated health, and authenticated shutdown. Its current allowlist contains only `worker.health` and `worker.shutdown`; it has no generic execution surface and no Windows mutation handler.
- Added `PrivilegedWorkerClient` with packaged-path validation, optional `runas`, explicit UAC-cancellation reporting, process-exit detection, PID/session-bound bootstrap, serialized signed requests, authenticated response validation, secure shutdown, session-key zeroing, and owned-process cleanup.
- The in-app Test Center now launches the packaged worker without elevation, performs authenticated health IPC, and shuts it down. Tests cover the lower-level separate process lifecycle and the production client lifecycle. Full suite passed 131/131.
- Updated `scripts/package.ps1` to publish the worker self-contained beside the desktop and CLI and fail if its executable is absent. Rebuilt installer and portable artifacts. `HyperBoostX.Worker.exe` is present in publish and portable ZIP; both dist artifact hashes independently match `SHA256SUMS.txt`.
- Current artifacts: installer 50,711,571 bytes; portable ZIP 71,671,653 bytes. Worker signature is accurately `NotSigned`. No installer execution, UAC prompt, elevated handler, or cross-elevation VM test was performed.
- Published self-contained UI smoke passed all 73 routes, Test Center execution, settings export/import/reset persistence, duplicate-instance activation, distinct-data-directory isolation, and clean exit code 0. Evidence directory: `artifacts/ui-worker-smoke-e6d096957c1e40b0be6269e04152c9eb`.

## Continuation (2026-11-16) — Duplicate & Large Files fully wired

- Replaced the route's obsolete `Unsupported` binding with an implemented native workflow. Users can select a local folder, configure the large-file threshold, run/cancel a bounded scan, inspect SHA-256-confirmed duplicate groups, review potential duplicate bytes, list large files, and see skipped/error coverage. The UI provides no deletion action; files remain unchanged.
- Added route-level integration coverage with two 1 MiB identical files. Full suite passed 132/132 and verifies one duplicate group, two large-file rows, complete coverage, and unchanged content.
- Rebuilt self-contained app, worker, CLI, NSIS installer, portable ZIP, and checksums. Published UI smoke exercised the actual file-analysis controls, rendered one duplicate group and two large files, verified fixture hashes remained equal, passed all 73 routes, settings and single-instance workflows, and exited cleanly. Evidence: `artifacts/ui-file-analysis-smoke-0f2dca689122457ab146921e0a91265d`.
- Regenerated canonical traceability: 4,043 requirements, 24 `IMPLEMENTED_VERIFIED`, 5 `PARTIAL_VERIFIED`, and 4,019 mandatory requirements not yet verified. Folder selection, duplicate grouping, and large-file inventory prototype requirements are now evidence-backed; exclusions remain partial pending explicit reparse-point integration evidence.

## Continuation (2026-11-16) — RGB Software Detector implemented

- Replaced the RGB route's `Unsupported` binding with a read-only native provider. It recognizes explicit signatures for SignalRGB, MSI Mystic Light/Center, OpenRGB, Corsair iCUE, ASUS Armoury Crate/Aura, Razer Synapse/Chroma, Logitech G HUB, GIGABYTE RGB Fusion, ASRock Polychrome, NZXT CAM, and Cooler Master MasterPlus across running processes, `Win32_Service`, and uninstall records.
- Running controller processes receive a real 500 ms two-sample normalized CPU measurement plus working-set memory. Multiple active controllers produce an evidence-based overlap finding; no process, service, lighting, cooling, or vendor configuration is changed.
- Tests use a real temporary OpenRGB-named process and verify measured CPU/memory evidence, known/unknown classification, provider fallback, route binding and scan availability. Full suite passed 141/141 without warnings.
- Rebuilt installer/portable/checksums. Published UI smoke invoked RGB scan, required visible observations, exercised Duplicate & Large Files, Test Center, settings and single-instance workflows, passed all 73 routes and exited cleanly. Evidence: `artifacts/ui-rgb-smoke-1d6ddf1b1f544c1d83e75a8e418f77f2`.
- Regenerated traceability: 4,044 requirements; 31 `IMPLEMENTED_VERIFIED`; 5 `PARTIAL_VERIFIED`; 4,013 mandatory requirements remain unverified. The RGB route and its five prototype controller entries now have handler/test/evidence mappings.

## Continuation (2026-11-16) — Overlay Conflict Detector implemented

- Replaced the overlay route's `Unsupported` binding with a read-only native provider for Discord, Steam, NVIDIA, AMD, RTSS/RivaTuner/MSI Afterburner, Xbox Game Bar, OBS, Overwolf, Medal, and SteelSeries GG/Moments signatures.
- Running overlay-capable processes receive a real 500 ms normalized CPU sample and working-set measurement. The route reports aggregate observed load and overlapping providers while explicitly stating that process presence does not prove an overlay is enabled or caused a performance change. No process or overlay setting is changed.
- Tests launch a real temporary Discord-named process and select its exact PID while preserving any real user Discord processes. Signature coverage includes all six prototype providers. Full suite passed 149/149.
- Rebuilt installer/portable/checksums. Published UI smoke invoked both RGB and Overlay scans, exercised file analysis, Test Center, settings and single-instance workflows, passed 73/73 routes and exited cleanly. Evidence: `artifacts/ui-overlay-smoke-732ccc56bb2d403ab9dcdde4f5f9dc93`.
- Traceability now contains 4,045 requirements, 39 `IMPLEMENTED_VERIFIED`, 5 `PARTIAL_VERIFIED`, and 4,006 mandatory requirements not verified.

## Continuation (2026-11-16) — App Load Optimizer operational

- Replaced the route's unavailable placeholder with a native launch-measurement workflow. It accepts only an existing absolute local `.exe`, launches it without arguments after explicit confirmation, records process-creation time, records GUI input-idle time only when Windows exposes that signal, and samples initial working set, private memory, and cumulative CPU time.
- HyperBoostX never closes or reconfigures the launched process. Early process exit and missing GUI readiness are reported explicitly instead of being converted into a fabricated timing result. Each result is retained in the local report history.
- Validation tests cover blank, relative, missing, network, and non-executable paths. A real local executable is launched in the provider test. The full suite passed 156/156.
- Rebuilt the self-contained app, worker, CLI, installer, portable ZIP, and checksum manifest. Published UI Automation selected the packaged worker as a safe short-lived fixture, approved the launch dialog, required a visible measured process-creation result, and passed all 73 routes. Evidence: `artifacts/ui-app-load-smoke-f34509d98de246d5949adb5a0f624388`.
- Traceability contains 4,046 requirements, 43 `IMPLEMENTED_VERIFIED`, 7 `PARTIAL_VERIFIED`, and 4,003 mandatory requirements not verified. App inventory remains partial because the current production workflow measures a user-selected executable rather than claiming a complete installed-app catalog.

## Continuation (2026-11-16) — Boot Optimizer evidence-backed analysis

- Replaced the unavailable boot placeholder with a bounded native provider for up to ten Diagnostics-Performance Event 100 boot-history records, up to fifty Event 101–110 startup degradation records, and up to 200 automatic-service dependency records from `Win32_Service.Dependencies`.
- Results distinguish missing logs, denied providers, and unavailable fields. Findings are created only from recorded degradation of at least one second and explicitly avoid causal claims. No service, task, boot setting, or Windows state is changed.
- Full suite passed 158/158. Rebuilt installer and portable artifacts; UI Automation invoked the Boot scan, required visible results, and passed 73/73 routes. Evidence: `artifacts/ui-boot-smoke-48ea6a86567444539073ed470374c202`.
- Traceability now contains 4,047 requirements, 49 `IMPLEMENTED_VERIFIED`, 7 `PARTIAL_VERIFIED`, and 3,998 mandatory requirements not verified. Stable remains blocked by the remaining registry and release gates.

## Continuation (2026-11-16) — Drive Optimization operational

- Implemented bounded read-only inspection of `MSFT_PhysicalDisk`, `MSFT_Volume`, the Windows `ScheduledDefrag` task, and `fsutil behavior query DisableDeleteNotify`. Media type, volume data, health, operational state, TRIM policy, and schedule are shown as direct observations or explicit unavailable results.
- Added an explicit allowlisted handoff to Windows Optimize Drives (`dfrgui.exe`). HyperBoostX does not automatically run defrag, retrim, or storage mutation, avoiding SSD/HDD policy guesses and hidden disk work.
- Full suite passed 160/160. Rebuilt installer/portable/checksums and published UI smoke invoked the Drive route, rendered observations, passed 73/73 routes, settings, worker, and single-instance flows. Evidence: `artifacts/ui-drive-smoke-5a3c7f2dad5f4b9a8fcf0e81f6ace5a7`.
- Traceability contains 4,048 requirements, 56 `IMPLEMENTED_VERIFIED`, 7 `PARTIAL_VERIFIED`, and 3,992 mandatory requirements not verified.

## Continuation (2026-11-16) — LCD Performance Guard implemented

- Added explicit signatures for Corsair iCUE, NZXT CAM/Kraken, Lian Li L-Connect, Thermaltake TT RGB Plus, ASUS Armoury Crate, MSI Center, AIDA64 SensorPanel, HWiNFO, and Logitech G HUB display utilities.
- The guard performs a real 500 ms normalized CPU and working-set sample, supports configurable CPU/memory thresholds, and can gate alerts to active HyperBoostX gaming sessions. Cooling/fan/pump-capable utilities are explicitly marked protected; the app never stops or reconfigures any detected process.
- A real temporary AIDA64-named process verifies measurement and gaming alert gating. Full suite passed 167/167. Rebuilt release artifacts and UI smoke invoked the route and passed 73/73 routes. Evidence: `artifacts/ui-lcd-smoke-d5bebed15f7b4bdc945f5ddb56892ebf`.
- Traceability contains 4,049 requirements, 62 `IMPLEMENTED_VERIFIED`, 7 `PARTIAL_VERIFIED`, and 3,987 mandatory requirements not verified.

## Continuation (2026-11-16) — Stutter Analyzer implemented

- Added a bounded frametime CSV analyzer accepting up to 500,000 samples and 64 MiB. It validates `frametime_ms` plus optional CPU, GPU, disk-I/O, and temperature columns before producing any result.
- Calculates average FPS, 1%/0.1% lows, mean/median/P95/P99/P99.9 frametime, documented spike counts, and Pearson correlations for optional paired telemetry. Imported captures are explicitly distinguished from live capture and correlations are not presented as causation.
- Deterministic parser tests and route tests raised the suite to 170/170. Published UI smoke imported a 100-row fixture through the native file dialog, required calculated Average FPS, and passed 73/73 routes. Evidence: `artifacts/ui-stutter-smoke-5829efa663dc4ec2a0a2a8b34b692009`.
- Traceability contains 4,050 requirements, 69 `IMPLEMENTED_VERIFIED`, 7 `PARTIAL_VERIFIED`, and 3,981 mandatory requirements not verified.

## Continuation (2026-11-16) — App Load inventory completed

- Added a bounded installed-application inventory across current-user/local-machine and 32/64-bit uninstall registry views. Records are deduplicated by name/version/publisher, capped at 2,000, and expose explicit truncation/provider availability.
- App Load Optimizer now covers app inventory, confirmed launch timing, initial resource sampling, and persisted history. Full suite passed 171/171.
- Published UI smoke rendered inventory rows, launched and measured the packaged short-lived worker fixture, and passed 73/73 routes. Evidence: `artifacts/ui-app-inventory-smoke-e3e18fe8a34f49838eebc03d48212536`.
- Traceability moved App Load route/inventory from partial to implemented-verified: 71 `IMPLEMENTED_VERIFIED`, 5 `PARTIAL_VERIFIED`, and 3,979 mandatory requirements not verified.

## Continuation (2026-11-16) — Research Mode implemented

- Added encrypted local A/B/C/D repeat-trial storage with validated experiment/workload/metric/unit/value/notes fields and a 2,000-trial bound.
- Experiment reports calculate means and sample counts per variant and deltas against measured variant A. SSD/RAM variants are never simulated; users must record actual measurements, and reports explicitly reject causal conclusions without controlled methodology.
- Core persistence/report validation and route tests bring the suite to 174/174. Packaged UI smoke saved a variant A trial, waited for analysis availability, rendered the summary, and passed 73/73 routes. Evidence: `artifacts/ui-research-smoke-7fd53e99464041c1a84329c1188b6af2`.
- Traceability contains 4,051 requirements, 78 `IMPLEMENTED_VERIFIED`, 5 `PARTIAL_VERIFIED`, and 3,973 mandatory requirements not verified.


## Continuation (2026-11-19) — personal repository migration and Research Mode correction

- Imported all 143 tracked source files from the organization repository at 2ab5b4bf3a7049b898c64e322339e8398a3bcca7 into jxxzy/HyperBoostX branch v3-development. Compared every source blob SHA; no missing or changed original file at the migration commit 82e973fd796d64fc90266fb7c33a37ed18327812. Legacy main remains untouched. Removed the temporary write-enabled migration job after the import; routine CI has contents:read only.
- Fixed Research Mode grouping: repeat-trial means and A baseline deltas are now isolated by workload, metric and unit within an experiment. Multiple workload labels are distinguished, missing matching baselines are reported, and numeric deltas use invariant culture. Added a regression test for two different workload baselines and a third workload without baseline. Windows test result pending; do not count it as verified yet.
- Hardened Stable requirement gate against empty/deleted/demoted inventory, malformed flags/evidence and inconsistent metadata. Added twelve synthetic Python regression tests; the local patch suite passed 12/12. CI runs them before checking the actual matrix. No increase in implemented-verified requirement counts is claimed.
- Migration Actions job succeeded; initial Stable requirement gate failed as expected; Windows build was in progress when this checkpoint was authored. See Actions for actual execution evidence. Signing, real Windows UI/VM/hardware and remaining requirement implementation continue to block Stable.

- Initial Windows CI stopped at NuGet NU1004: five shipped projects had win-x64 lock targets but no declared RuntimeIdentifiers. Added win-x64 to the shared props for non-test projects, leaving the test lock framework-only; CI now rejects package-lock drift after packaging. Awaiting actual rerun verification; no build success claimed.


## Continuation (2026-11-19) — GPU Center counter snapshots

- Integrated bounded WDDM formatted Windows performance-counter queries into GPU Center via the existing diagnostic/report lifecycle. Per-process/engine utilization and adapter dedicated/shared bytes are separately labelled; zero is valid, unknown/out-of-range metrics are unavailable, aggregates are excluded and truncated enumeration is explicit. No driver changes, sensor values or total GPU utilization are invented.
- Technical references: https://devblogs.microsoft.com/directx/gpus-in-the-task-manager/ and https://github.com/prometheus-community/windows_exporter/blob/main/docs/collector.gpu.md explain WDDM engine and memory semantics; exact WMI classes also used in Chromium crossbench Windows platform tests. Compatibility still requires actual Windows GPU validation.
- Added five regression/integration tests. Source implementation only at commit time; CI and hardware evidence pending. Dashboard live GPU, thermal, clocks, adapter-name mapping and capacity remain outstanding. No registry verification promotion.


## Continuation (2026-11-19) — installed game discovery

- Added bounded local Steam registry/default-library and libraryfolders.vdf/appmanifest discovery, plus Epic Games Launcher .item manifest discovery. Existing install directories become candidates; Epic executable hints are checked for local containment and existence. No game is launched or automatically registered. Missing, malformed, oversized and inaccessible manifest data is handled without shell execution; 32 roots, 500 files and 1 MiB per file limits apply.
- Added a Game Library discovery command/button and persisted diagnostic report with coverage/limitations, plus four fixture-based tests for secondary Steam libraries, Epic manifests, path escape, invalid inputs and cancellation. Rockstar/Xbox providers and automatic profiles are not implemented by this change. Source and tests pending new Windows CI evidence. No verification promotion.


## Continuation (2026-11-19) — Safe Boost batch workflow

- One Click Boost now offers a real selectable batch of two existing reversible current-user Windows preferences (transparency and game capture). Each action's original and desired state is previewed; unapproved, altered, expired, duplicate or non-allowlisted previews are rejected before mutation. Verified changes remain recoverable in a shared action-journal session; failure/cancellation attempts compensating rollback and preserves explicit recovery-required status on conflicts. No Windows transaction or performance benefit is claimed.
- Four isolated fake-action tests added for verified shared session, failure rollback, tampered/declined previews and cancellation. No real host mutation executed. Source/test execution pending subsequent CI; broader presets and elevated optimizations remain incomplete.
- Independently observed successful Windows CI at 19796bc: 175/175 tests passed, 0 build warnings/errors, unchanged lock files, installer and portable ZIP with verified hashes. Artifacts from run 44453637972 remain unsigned development packages. The two subsequent feature commits and this batch are not included in that result.

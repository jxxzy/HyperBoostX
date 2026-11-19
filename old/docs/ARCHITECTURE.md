# Architecture and decisions

`HyperBoostX.App` is the WPF presentation/MVVM layer. `HyperBoostX.Core` owns typed state, the mandatory action lifecycle, safety, journaling, redaction and file boundaries. `HyperBoostX.Windows` owns native power APIs, Registry adapters, WMI/COM/Event Log and known Windows-tool handoffs. `HyperBoostX.Cli` reuses the runtime and recovery engine without WPF. Tests include failure injection plus an isolated Windows Registry round trip.

The app's architecture remains separate from V1 and V3 beta; reviewed behaviors are ported individually against the typed native providers. No Flask process, listening HTTP server, beta service, secret directory, or unsigned third-party optimizer binary was copied.

Decisions:

- Full .NET avoids the old Python/HTTP lifecycle. SDK 8.0.425 is the installed, exercised toolchain; .NET 10 migration is not asserted.
- WPF enables a native offline Windows application. Software rendering is selected because the observed hardware-rendering path produced a white window during QA.
- SQLite provides transactional metadata. OS writes cannot share a SQLite transaction, so a durable encrypted intent record is written separately before mutation.
- DPAPI CurrentUser restricts recovery material to the current Windows user; the record additionally binds machine/user identity by hash.
- Fixed action instances expose no user-supplied Registry paths, arbitrary commands or plugin execution. Safety applies in all UI modes and CLI recovery.
- System diagnostics deliberately accept partial provider availability.

The elevated broker is **not implemented**. This build exposes only non-elevated registered configuration changes; elevated tools are explicit external Windows handoffs. Adding elevated mutations requires authenticated, versioned, bounded IPC and independent broker validation before promotion.

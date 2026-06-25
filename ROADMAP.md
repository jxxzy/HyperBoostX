# HyperBoostX Roadmap

## v1.3.0 Stable

- Universal GPU detection for NVIDIA GeForce GTX/RTX, AMD Radeon/RX/Vega, Intel Arc/iGPU, Microsoft Basic Display Adapter, and unknown fallback.
- GPU Center with vendor badge, profile recommendation, overlay detector, vendor/RGB software detector, and safe action guidance.
- PC Health, Gaming Readiness, Streaming Readiness, and Startup Cleanliness scoring through the hardware profile engine.
- Plan-first boost flow, before/after reports, local API session token security, backend job queue, Safety Guard, restore/undo protection, and local crash report export with redaction.

## v1.3.1 Hotfix

- Real user bug fixes.
- Installer polish.
- GPU detection fixes.
- UI scaling fixes.
- Backend health timeout fixes.
- Dashboard freeze fixes.
- Restore/undo bug fixes.

## v1.3.2 Stability

- Improve AMD and Intel detection.
- Reduce UI animation load.
- Improve report export.
- Improve AI Doctor prompts.
- Improve vendor/RGB software detection.

## v1.4.0 Auto Game Mode

- Auto profile switching when a game opens.
- Tray mode.
- Scheduled cleanup.
- Game-specific profiles.
- Auto revert when a game closes.
- Safe Discord, OBS, launcher, and overlay handling.

## v1.5.0 Update And Diagnostics

- App Integrity Check for backend, launcher, WPF app, config, version match, required files, package health, and backend recovery.
- Safe Mode / Recovery Mode that disables heavy animations, skips AI startup calls, skips deep scan on launch, opens Restore first, allows undo, exports redacted diagnostics, resets app settings, restarts backend, and shows repair install guidance.
- Privacy-friendly optional diagnostics. Diagnostics must be opt-in and anonymous if implemented.
- Update checker foundation: latest GitHub release, latest version, download link, and checksum guidance.

## v1.6.0 Website And Community

- Landing page content for HyperBoostX Universal Windows Gaming Optimizer.
- Download button, feature list, screenshots, before/after examples, Safety Guard, Restore/Undo, FAQ, changelog, Discord support, and GitHub Release link.
- Documentation website and tester program.

## Future Code Signing

Future commercial releases should use a code signing certificate to reduce Windows SmartScreen and Unknown Publisher warnings. Do not fake publisher signatures.

## Future Auto Updater

Future auto updater should check updates, download releases, verify SHA256 or signature, install updates, rollback on failure, and support Stable, Preview, Beta, and Developer channels. Manual update via GitHub Release is acceptable for v1.3.0.

## v2.0.0 Commercial Roadmap Only

License activation is not implemented in v1.3.0. Future v2.0.0 or later may include license activation, license server, signed license token, offline grace, device limit, Pro/Lifetime plans, admin license dashboard, and payment integration.

No v1.3.0 feature is locked behind a license.

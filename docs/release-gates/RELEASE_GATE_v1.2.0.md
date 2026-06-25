# Release Gate `v1.2.0`

This gate defines the minimum bar for shipping `HyperBoostX v1.2.0`.

Important note:

- We should not promise "literally zero bugs" because that cannot be proven on every machine.
- We can and should require:
  - zero known critical bugs
  - zero known major bugs
  - zero failing core audits on the reference machine
  - zero release-consistency mistakes in assets, checksums, and runtime packaging

If any required item below is not satisfied, `v1.2.0` must not be published.

## 1. Non-Negotiable Exit Criteria

- [ ] No known `Critical` bug remains open.
- [ ] No known `Major` bug remains open.
- [ ] No known audit fail remains in `Full`.
- [ ] No known false-negative fail remains in `Full QA Matrix`.
- [ ] No release asset mismatch remains between installer, checksums, and GitHub assets.
- [ ] No source-vs-runtime mismatch remains for the build intended for release.

## 2. Core Feature Stability

- [ ] `Dashboard` refreshes without empty state, crash, or blocking error.
- [ ] `Smart Recommendation` completes and renders recommendations without null/empty summary.
- [ ] `One Click Boost` completes without runtime exception.
- [ ] `Startup Manager` loads and basic actions remain stable.
- [ ] `Cleanup` loads and safe cleanup flow works.
- [ ] `Storage` loads and reports valid device/space breakdown.
- [ ] `Network Booster` diagnostics render and safe actions return feedback.
- [ ] `Privacy Center` renders summary without missing state.
- [ ] `Security & Health` renders without stale or broken status text.
- [ ] `Apps Manager` loads process list without failure.
- [ ] `Scheduled Automation` renders persistent rules and state safely.
- [ ] `Utilities Tools` renders all primary panels without shared-state regression.
- [ ] `Settings` renders and persists expected settings state.
- [ ] `AI Copilot` handles unavailable/quota/auth errors without crash or false fatal classification.
- [ ] `About App` renders correct version and release state.

## 3. Advanced Feature Stability

- [ ] `Gaming Booster Hub` renders and actions remain safe.
- [ ] `Gaming Booster` snapshot loads without broken state.
- [ ] `Streaming Mode` snapshot loads without broken state.
- [ ] `Creator Mode` snapshot loads without broken state.
- [ ] `Tweaks Center` renders tweak list safely.
- [ ] `Windows Features` safe flow completes without unhandled exception.
- [ ] `Update Control` loads status without broken summary.
- [ ] `Repair Tools` renders stable repair state.
- [ ] `Driver & Update Center` loads scan summary safely.
- [ ] `App Uninstaller` loads installed app inventory without crash.
- [ ] `Advanced Tweaks` renders with clear warning state.
- [ ] `Windows Services` loads safely even when service enumeration is partial.
- [ ] `Power Optimization` renders and actions remain safe.
- [ ] `Visual Effects` renders and actions remain safe.
- [ ] `Restore & Backup` renders current backup state safely.
- [ ] `Restore Point Manager` renders restore state safely.

## 4. Audit Reliability

- [ ] `Feature Audit Full` passes with `0` failures on reference machine.
- [ ] `Full QA Matrix` passes with `0` failures on reference machine.
- [ ] `Utilities Workflow Actions` no longer fails.
- [ ] `Dashboard Refresh` no longer fails due to unrealistic threshold.
- [ ] Audit logs clearly reflect the current run only, not stale incidents.
- [ ] Audit result summaries match the actual executed runtime.

## 5. Updater and Release Trust

- [ ] Checksum validation passes for the published installer asset.
- [ ] Release assets match the files referenced in `SHA256SUMS.txt`.
- [ ] Update UI distinguishes:
  - `Auto install allowed`
  - `Manual install ready`
  - `Blocked`
- [ ] Unsigned-but-valid installer is not misreported as a generic corrupt download.
- [ ] Automatic install policy remains explicit and predictable.
- [ ] Release metadata and in-app version text match exactly.

## 6. Adaptive Optimization Requirements

These are the product requirements that make `v1.2.0` meaningfully better than `v1.1.x`.

- [ ] `Device Classification` is available in runtime output.
- [ ] `System drive` type is detected for the active Windows drive when possible.
- [ ] `Bottleneck Detector` returns a usable primary bottleneck classification.
- [ ] Dashboard shows adaptive class and recommended profile.
- [ ] Smart Recommendation includes adaptive profile guidance.
- [ ] `System Info` exposes device profile details.
- [ ] SSD/HDD result messaging is more honest than generic score-only wording.
- [ ] Laptop/desktop context is reflected in recommendations when available.

## 7. UX and Messaging Requirements

- [ ] No major feature shows misleading hard-error messaging for recoverable states.
- [ ] Result messaging distinguishes estimated gain vs realistic expectation.
- [ ] HDD users are explicitly told when storage hardware remains the main limit.
- [ ] Update status does not confuse "latest already installed" with "asset unavailable" in a misleading way.
- [ ] Notifications appear without the previous visible delay regression.

## 8. Build, Package, and Runtime Verification

- [ ] WPF release build passes.
- [ ] Launcher publish passes.
- [ ] Backend packaged build passes.
- [ ] Portable/runtime layout is assembled successfully.
- [ ] Installer build passes.
- [ ] Installed runtime launches successfully.
- [ ] Portable runtime launches successfully.
- [ ] Backend health check succeeds in packaged runtime.
- [ ] App exits cleanly without backend orphaning.

## 9. Pre-Publish Manual Verification

- [ ] Test on at least one reference desktop.
- [ ] Test on at least one reference laptop.
- [ ] Test on at least one SSD system-drive machine.
- [ ] Test on at least one HDD system-drive machine if still supported.
- [ ] Test on at least one Windows 10 machine if still supported.
- [ ] Test on at least one Windows 11 machine.
- [ ] Verify upgrade path from current public stable version.
- [ ] Verify uninstall and reinstall behavior.

## 10. Publish Decision

Release `v1.2.0` only if:

- [ ] all non-negotiable exit criteria pass
- [ ] all release artifacts are verified
- [ ] no critical or major regression is discovered in final smoke test

Hold `v1.2.0` and continue fixing if any required item above remains unchecked.


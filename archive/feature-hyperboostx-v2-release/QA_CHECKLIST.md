# QA Checklist

Release target:
- `HyperBoostX v1.1.0-beta`

Author:
- `MR.4NONY - HYPERINDO CYBER TEAM`

Use this checklist before promoting a beta build to a wider release.

## 1. Installer And Runtime

- [ ] Install from `HyperBoostXInstaller.exe` on a clean Windows machine.
- [ ] Verify desktop shortcut works.
- [ ] Verify Start Menu shortcut works.
- [ ] Verify uninstall works cleanly.
- [ ] Verify reinstall after uninstall works.
- [ ] Verify upgrade from an older installed version works.
- [ ] Verify launcher starts backend and WPF UI correctly.
- [ ] Verify backend shuts down when UI exits.

## 2. Core App Startup

- [ ] Launch app with internet available.
- [ ] Launch app with internet unavailable.
- [ ] Verify backend health badge updates correctly.
- [ ] Verify app does not hang on startup when backend is slow or unavailable.
- [ ] Verify action status cards show useful errors instead of crashing.

## 3. Settings And Persistence

- [ ] Change automation mode and restart app.
- [ ] Change policy profile and restart app.
- [ ] Confirm automation mode stays separate from policy profile.
- [ ] Change language and restart app.
- [ ] Change Discord webhook settings and restart app.
- [ ] Change AI settings and restart app.
- [ ] Confirm config is saved under `%LocalAppData%\\HyperBoost X\\config`.

## 4. Automation

- [ ] Create a real automation rule.
- [ ] Restart app and confirm rule persists.
- [ ] Verify deferred tasks remain queued correctly.
- [ ] Verify retry state works after a failed task.
- [ ] Verify audit log records why tasks ran, deferred, or failed.
- [ ] Verify maintenance windows affect execution timing.
- [ ] Verify manual override can pause automation.

## 5. AI Copilot

- [ ] Test NVIDIA connection from Settings.
- [ ] Send a normal prompt in AI Copilot.
- [ ] Verify context-aware response appears.
- [ ] Verify safe actions are queued for review.
- [ ] Approve one action only and confirm granular execution works.
- [ ] Skip one action and confirm queue advances safely.
- [ ] Reject plan and confirm pending queue clears.
- [ ] Create automation from AI and confirm real rule creation.
- [ ] Verify AI memory persists across restart.
- [ ] Verify AI memory clear resets stored history.

## 6. Discord Reporting

- [ ] Send test Discord webhook message.
- [ ] Trigger one handled app error and verify report delivery.
- [ ] Verify minimum severity filter works.
- [ ] Verify cooldown prevents duplicate spam.
- [ ] Verify no sensitive local data is exposed unexpectedly.

## 7. Recovery And Safety

- [ ] Create a backup snapshot.
- [ ] Create a restore point.
- [ ] Verify backup history appears.
- [ ] Verify restore point history appears.
- [ ] Verify restore actions handle disabled system protection gracefully.
- [ ] Verify risky actions create restore point when required.

## 8. Admin-Sensitive Modules

- [ ] Test Windows Services actions with admin rights.
- [ ] Test Power Optimization actions with admin rights.
- [ ] Test Repair Tools actions with admin rights.
- [ ] Test Windows Features actions with admin rights.
- [ ] Test Driver and Update actions with admin rights.
- [ ] Verify failures in admin-required actions are reported cleanly.

## 9. Main Feature Modules

- [ ] One Click Boost runs and shows result summary.
- [ ] Performance Boost actions run without UI freeze.
- [ ] Startup Manager reads entries and applies safe changes.
- [ ] Cleanup frees temporary files and updates history.
- [ ] Storage scan reads drives and shows analysis.
- [ ] Gaming Booster can detect or target a game process.
- [ ] Streaming Mode can detect streaming apps.
- [ ] Creator Mode can detect creator apps.
- [ ] Network Booster and DNS tools report useful status.
- [ ] Privacy, Security, and Repair pages open and run primary actions.

## 10. Localization

- [ ] Switch to `id-ID` and verify core navigation text updates.
- [ ] Switch back to `en-US` and verify core navigation text updates.
- [ ] Verify no empty string appears when a translation key is missing.
- [ ] Verify fallback to English works.
- [ ] Verify long translated text does not break the layout badly.

## 11. Logs And Diagnostics

- [ ] Confirm logs are written under `%LocalAppData%\\HyperBoost X\\logs`.
- [ ] Confirm no infinite error loop is written to logs.
- [ ] Confirm activity log reflects major actions and failures.
- [ ] Confirm unhandled exception reporting works without crashing the reporting path itself.

## 12. High-Risk Focus Checks

- [ ] Service changes do not fail silently.
- [ ] PowerShell actions do not hang forever.
- [ ] Restore and repair flows handle blocked Windows states safely.
- [ ] AI actions are blocked when safety conditions require it.
- [ ] Installer and uninstaller do not leave stale runtime files behind.

## Result

- [ ] Beta ready for wider testing
- [ ] Needs fixes before wider testing
- [ ] Ready for public stable release


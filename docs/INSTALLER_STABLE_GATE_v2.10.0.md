# Installer Stable Gate v2.10.0

Generated: 2026-07-03 03.18.39 +07:00
Expected version: 2.10.0
Status: PASS

| Check | Status | Detail |
| --- | --- | --- |
| installer artifact exists | PASS | F:\BOOSTER BY MR.4NONY\HyperBoostXInstaller.exe |
| installer SHA256 available | PASS | DA445C1DBDEB608AB3958E9C1922599585798A6BF1F46A65D3AA22FB02B92AD0 |
| installer newer than release inputs | PASS | installer_utc=2026-07-02T19:59:50.3720076Z; latest_input_utc=2026-07-02T19:57:28.2245913Z; latest_input=F:\BOOSTER BY MR.4NONY\wpf\Data\ui_action_map_v2_10.json |
| owner admin evidence present | PASS | F:\BOOSTER BY MR.4NONY\docs\runtime-audit\owner_admin_stable_gate_report.json |
| owner evidence newer than installer | PASS | report_utc=2026-07-02T20:00:51.4244368Z; installer_utc=2026-07-02T19:59:50.3720076Z |
| owner admin evidence expected version | PASS | actual=2.10.0; expected=2.10.0 |
| owner admin evidence ok | PASS | ok=True |
| runtime verifier exit 0 | PASS | exit=0 |
| registry DisplayVersion matches expected | PASS | 2.10.0 |
| desktop shortcut targets launcher | PASS | [{"path":"<USER_DESKTOP>\\HyperBoostX.lnk","exists":false,"target":null,"working_directory":null,"icon_location":null,"error":null},{"path":"<PUBLIC_DESKTOP>\\HyperBoostX.lnk","exists":true,"target":"<INSTALL_DIR>\\HyperBoostX.exe","working_directory":"<INSTALL_DIR>\\runtime\\backend","icon_location":",0","error":null}] |
| start menu shortcut targets launcher | PASS | {"path":"<START_MENU>\\Programs\\HyperBoostX\\HyperBoostX.lnk","exists":true,"target":"<INSTALL_DIR>\\HyperBoostX.exe","working_directory":"<INSTALL_DIR>\\runtime\\backend","icon_location":",0","error":null} |
| backend health on port 5000 | PASS | {"ok":true,"uri":"http://127.0.0.1:5000/api/health","error":null,"data":{"backend_mode":"stable","feature_registry_status":{"action_map_found":true,"action_map_source":"<INSTALL_DIR>\\runtime\\wpf\\Data\\ui_action_map_v2_10.json","errors":[],"expected":{"expected_non_real_visible_in_stable":0,"expected_stable_buttons":606,"expected_stable_menus":73,"expected_unique_ui_endpoints":167},"non_real_visible_in_stable":0,"stable_ui_ok":true,"stable_visible_buttons":606,"stable_visible_features":73,"warnings":[]},"local_only":true,"service":"HyperBoostX Backend","session_token_required":true,"status":"ok","version":"2.10.0"}} |
| backend version matches expected | PASS | 2.10.0 |
| WPF installed smoke | PASS | [{"name":"HyperBoostX","id":8660,"path":"<INSTALL_DIR>\\runtime\\wpf\\HyperBoostX.exe","from_install":true},{"name":"HyperBoostX","id":29392,"path":"<INSTALL_DIR>\\HyperBoostX.exe","from_install":true},{"name":"hyperboost_backend","id":1408,"path":"<INSTALL_DIR>\\runtime\\backend\\hyperboost_backend.exe","from_install":true},{"name":"hyperboost_backend","id":27584,"path":"<INSTALL_DIR>\\runtime\\backend\\hyperboost_backend.exe","from_install":true}] |
| token sync inferred | PASS | session_token_required=True; wpf_running=True |
| no orphan installed processes | PASS |  |
| silent uninstall | PASS | Quiet uninstall completed. |
| silent reinstall | PASS | <REPO_ROOT>\HyperBoostXInstaller.exe |
| runtime verifier after reinstall | PASS | exit=0 |
| installed screenshot present: dashboard.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\dashboard.png |
| installed screenshot newer than owner gate: dashboard.png | PASS | screenshot_utc=2026-07-02T20:02:33.6908013Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: dashboard-after-scroll.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\dashboard-after-scroll.png |
| installed screenshot newer than owner gate: dashboard-after-scroll.png | PASS | screenshot_utc=2026-07-02T20:02:36.3659271Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: performance.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\performance.png |
| installed screenshot newer than owner gate: performance.png | PASS | screenshot_utc=2026-07-02T20:02:37.8634965Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: startup.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\startup.png |
| installed screenshot newer than owner gate: startup.png | PASS | screenshot_utc=2026-07-02T20:02:39.3240221Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: background-apps.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\background-apps.png |
| installed screenshot newer than owner gate: background-apps.png | PASS | screenshot_utc=2026-07-02T20:02:40.7734991Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: cleanup.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\cleanup.png |
| installed screenshot newer than owner gate: cleanup.png | PASS | screenshot_utc=2026-07-02T20:02:42.2309333Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: storage.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\storage.png |
| installed screenshot newer than owner gate: storage.png | PASS | screenshot_utc=2026-07-02T20:02:43.6803641Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: one-click-boost.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\one-click-boost.png |
| installed screenshot newer than owner gate: one-click-boost.png | PASS | screenshot_utc=2026-07-02T20:02:45.1297963Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: gaming-mode.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\gaming-mode.png |
| installed screenshot newer than owner gate: gaming-mode.png | PASS | screenshot_utc=2026-07-02T20:02:46.5854854Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: smart-recommendation.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\smart-recommendation.png |
| installed screenshot newer than owner gate: smart-recommendation.png | PASS | screenshot_utc=2026-07-02T20:02:48.0440030Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: gpu-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\gpu-center.png |
| installed screenshot newer than owner gate: gpu-center.png | PASS | screenshot_utc=2026-07-02T20:02:49.5324437Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: hardware-vendor-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\hardware-vendor-center.png |
| installed screenshot newer than owner gate: hardware-vendor-center.png | PASS | screenshot_utc=2026-07-02T20:02:50.9900292Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: gaming-booster.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\gaming-booster.png |
| installed screenshot newer than owner gate: gaming-booster.png | PASS | screenshot_utc=2026-07-02T20:02:52.4624257Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: streaming-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\streaming-center.png |
| installed screenshot newer than owner gate: streaming-center.png | PASS | screenshot_utc=2026-07-02T20:02:53.9235265Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: creator-mode.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\creator-mode.png |
| installed screenshot newer than owner gate: creator-mode.png | PASS | screenshot_utc=2026-07-02T20:02:55.3888994Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: network-booster.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\network-booster.png |
| installed screenshot newer than owner gate: network-booster.png | PASS | screenshot_utc=2026-07-02T20:02:56.8818585Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: dns-latency-tools.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\dns-latency-tools.png |
| installed screenshot newer than owner gate: dns-latency-tools.png | PASS | screenshot_utc=2026-07-02T20:02:58.3903326Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: privacy-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\privacy-center.png |
| installed screenshot newer than owner gate: privacy-center.png | PASS | screenshot_utc=2026-07-02T20:02:59.8705224Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: security-health.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\security-health.png |
| installed screenshot newer than owner gate: security-health.png | PASS | screenshot_utc=2026-07-02T20:03:01.3358175Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: apps-manager.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\apps-manager.png |
| installed screenshot newer than owner gate: apps-manager.png | PASS | screenshot_utc=2026-07-02T20:03:02.8058309Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: tweaks-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\tweaks-center.png |
| installed screenshot newer than owner gate: tweaks-center.png | PASS | screenshot_utc=2026-07-02T20:03:04.2933978Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: windows-features.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\windows-features.png |
| installed screenshot newer than owner gate: windows-features.png | PASS | screenshot_utc=2026-07-02T20:03:05.7734166Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: update-control.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\update-control.png |
| installed screenshot newer than owner gate: update-control.png | PASS | screenshot_utc=2026-07-02T20:03:07.2566928Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: repair-tools.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\repair-tools.png |
| installed screenshot newer than owner gate: repair-tools.png | PASS | screenshot_utc=2026-07-02T20:03:08.7604763Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: driver-update-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\driver-update-center.png |
| installed screenshot newer than owner gate: driver-update-center.png | PASS | screenshot_utc=2026-07-02T20:03:10.2452799Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: app-uninstaller.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\app-uninstaller.png |
| installed screenshot newer than owner gate: app-uninstaller.png | PASS | screenshot_utc=2026-07-02T20:03:11.7315022Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: restore-backup.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\restore-backup.png |
| installed screenshot newer than owner gate: restore-backup.png | PASS | screenshot_utc=2026-07-02T20:03:13.2566983Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: settings.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\settings.png |
| installed screenshot newer than owner gate: settings.png | PASS | screenshot_utc=2026-07-02T20:03:14.7519625Z; report_utc=2026-07-02T20:00:51.4244368Z |
| installed screenshot present: about.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\about.png |
| installed screenshot newer than owner gate: about.png | PASS | screenshot_utc=2026-07-02T20:03:16.2566959Z; report_utc=2026-07-02T20:00:51.4244368Z |

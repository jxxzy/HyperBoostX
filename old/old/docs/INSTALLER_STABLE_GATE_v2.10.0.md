# Installer Stable Gate v2.10.0

Generated: 2026-07-03 21.56.56 +07:00
Expected version: 2.10.0
Status: PASS

| Check | Status | Detail |
| --- | --- | --- |
| installer artifact exists | PASS | F:\BOOSTER BY MR.4NONY\HyperBoostXInstaller.exe |
| installer SHA256 available | PASS | 65E8A0E5F5ADFF0FBE4A83BBB2A278466C84BF6E071D8E5AAAFA42A601D3B42E |
| installer newer than release inputs | PASS | installer_utc=2026-07-03T14:52:53.7160107Z; latest_input_utc=2026-07-03T14:49:54.5958082Z; latest_input=F:\BOOSTER BY MR.4NONY\wpf\Data\ui_action_map_v2_10.json |
| owner admin evidence present | PASS | F:\BOOSTER BY MR.4NONY\docs\runtime-audit\owner_admin_stable_gate_report.json |
| owner evidence newer than installer | PASS | report_utc=2026-07-03T14:53:52.8534086Z; installer_utc=2026-07-03T14:52:53.7160107Z |
| owner admin evidence expected version | PASS | actual=2.10.0; expected=2.10.0 |
| owner admin evidence ok | PASS | ok=True |
| runtime verifier exit 0 | PASS | exit=0 |
| registry DisplayVersion matches expected | PASS | 2.10.0 |
| desktop shortcut targets launcher | PASS | [{"path":"C:\\Users\\jxxzy\\OneDrive\\Desktop\\HyperBoostX.lnk","exists":false,"target":null,"working_directory":null,"icon_location":null,"error":null},{"path":"C:\\Users\\Public\\Desktop\\HyperBoostX.lnk","exists":true,"target":"C:\\Program Files\\HyperBoostX\\HyperBoostX.exe","working_directory":"C:\\Program Files\\HyperBoostX\\runtime\\backend","icon_location":",0","error":null}] |
| start menu shortcut targets launcher | PASS | {"path":"C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\HyperBoostX\\HyperBoostX.lnk","exists":true,"target":"C:\\Program Files\\HyperBoostX\\HyperBoostX.exe","working_directory":"C:\\Program Files\\HyperBoostX\\runtime\\backend","icon_location":",0","error":null} |
| backend health on port 5000 | PASS | {"ok":true,"uri":"http://127.0.0.1:5000/api/health","error":null,"data":{"backend_mode":"stable","feature_registry_status":{"action_map_found":true,"action_map_source":"C:\\Program Files\\HyperBoostX\\runtime\\wpf\\Data\\ui_action_map_v2_10.json","errors":[],"expected":{"expected_non_real_visible_in_stable":0,"expected_stable_buttons":606,"expected_stable_menus":73,"expected_unique_ui_endpoints":173},"non_real_visible_in_stable":0,"stable_ui_ok":true,"stable_visible_buttons":606,"stable_visible_features":73,"warnings":[]},"local_only":true,"service":"HyperBoostX Backend","session_token_required":true,"status":"ok","version":"2.10.0"}} |
| backend version matches expected | PASS | 2.10.0 |
| WPF installed smoke | PASS | [{"name":"HyperBoostX","id":5920,"path":"C:\\Program Files\\HyperBoostX\\HyperBoostX.exe","from_install":true},{"name":"HyperBoostX","id":18092,"path":"C:\\Program Files\\HyperBoostX\\runtime\\wpf\\HyperBoostX.exe","from_install":true},{"name":"hyperboost_backend","id":24116,"path":"C:\\Program Files\\HyperBoostX\\runtime\\backend\\hyperboost_backend.exe","from_install":true},{"name":"hyperboost_backend","id":30564,"path":"C:\\Program Files\\HyperBoostX\\runtime\\backend\\hyperboost_backend.exe","from_install":true}] |
| token sync inferred | PASS | session_token_required=True; wpf_running=True |
| no orphan installed processes | PASS |  |
| silent uninstall | PASS | Quiet uninstall completed. |
| silent reinstall | PASS | F:\BOOSTER BY MR.4NONY\HyperBoostXInstaller.exe |
| runtime verifier after reinstall | PASS | exit=0 |
| installed screenshot present: dashboard.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\dashboard.png |
| installed screenshot newer than owner gate: dashboard.png | PASS | screenshot_utc=2026-07-03T14:55:38.6249382Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: dashboard-after-scroll.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\dashboard-after-scroll.png |
| installed screenshot newer than owner gate: dashboard-after-scroll.png | PASS | screenshot_utc=2026-07-03T14:55:41.3328186Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: performance.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\performance.png |
| installed screenshot newer than owner gate: performance.png | PASS | screenshot_utc=2026-07-03T14:55:42.8177390Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: startup.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\startup.png |
| installed screenshot newer than owner gate: startup.png | PASS | screenshot_utc=2026-07-03T14:55:44.2889543Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: background-apps.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\background-apps.png |
| installed screenshot newer than owner gate: background-apps.png | PASS | screenshot_utc=2026-07-03T14:55:45.7612397Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: cleanup.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\cleanup.png |
| installed screenshot newer than owner gate: cleanup.png | PASS | screenshot_utc=2026-07-03T14:55:47.2508397Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: storage.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\storage.png |
| installed screenshot newer than owner gate: storage.png | PASS | screenshot_utc=2026-07-03T14:55:48.7426351Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: one-click-boost.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\one-click-boost.png |
| installed screenshot newer than owner gate: one-click-boost.png | PASS | screenshot_utc=2026-07-03T14:55:50.1968742Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: gaming-mode.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\gaming-mode.png |
| installed screenshot newer than owner gate: gaming-mode.png | PASS | screenshot_utc=2026-07-03T14:55:51.6628414Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: smart-recommendation.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\smart-recommendation.png |
| installed screenshot newer than owner gate: smart-recommendation.png | PASS | screenshot_utc=2026-07-03T14:55:53.1309512Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: gpu-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\gpu-center.png |
| installed screenshot newer than owner gate: gpu-center.png | PASS | screenshot_utc=2026-07-03T14:55:54.6157823Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: hardware-vendor-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\hardware-vendor-center.png |
| installed screenshot newer than owner gate: hardware-vendor-center.png | PASS | screenshot_utc=2026-07-03T14:55:56.1228911Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: gaming-booster.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\gaming-booster.png |
| installed screenshot newer than owner gate: gaming-booster.png | PASS | screenshot_utc=2026-07-03T14:55:57.6163534Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: streaming-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\streaming-center.png |
| installed screenshot newer than owner gate: streaming-center.png | PASS | screenshot_utc=2026-07-03T14:55:59.1115954Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: creator-mode.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\creator-mode.png |
| installed screenshot newer than owner gate: creator-mode.png | PASS | screenshot_utc=2026-07-03T14:56:00.6212286Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: network-booster.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\network-booster.png |
| installed screenshot newer than owner gate: network-booster.png | PASS | screenshot_utc=2026-07-03T14:56:02.1472149Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: dns-latency-tools.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\dns-latency-tools.png |
| installed screenshot newer than owner gate: dns-latency-tools.png | PASS | screenshot_utc=2026-07-03T14:56:03.7055870Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: privacy-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\privacy-center.png |
| installed screenshot newer than owner gate: privacy-center.png | PASS | screenshot_utc=2026-07-03T14:56:05.2290427Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: security-health.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\security-health.png |
| installed screenshot newer than owner gate: security-health.png | PASS | screenshot_utc=2026-07-03T14:56:06.7417594Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: apps-manager.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\apps-manager.png |
| installed screenshot newer than owner gate: apps-manager.png | PASS | screenshot_utc=2026-07-03T14:56:08.2825956Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: tweaks-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\tweaks-center.png |
| installed screenshot newer than owner gate: tweaks-center.png | PASS | screenshot_utc=2026-07-03T14:56:09.8205037Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: windows-features.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\windows-features.png |
| installed screenshot newer than owner gate: windows-features.png | PASS | screenshot_utc=2026-07-03T14:56:11.3501147Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: update-control.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\update-control.png |
| installed screenshot newer than owner gate: update-control.png | PASS | screenshot_utc=2026-07-03T14:56:12.8893989Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: repair-tools.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\repair-tools.png |
| installed screenshot newer than owner gate: repair-tools.png | PASS | screenshot_utc=2026-07-03T14:56:14.4262939Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: driver-update-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\driver-update-center.png |
| installed screenshot newer than owner gate: driver-update-center.png | PASS | screenshot_utc=2026-07-03T14:56:15.9200248Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: app-uninstaller.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\app-uninstaller.png |
| installed screenshot newer than owner gate: app-uninstaller.png | PASS | screenshot_utc=2026-07-03T14:56:17.4447819Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: restore-backup.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\restore-backup.png |
| installed screenshot newer than owner gate: restore-backup.png | PASS | screenshot_utc=2026-07-03T14:56:18.9974257Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: settings.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\settings.png |
| installed screenshot newer than owner gate: settings.png | PASS | screenshot_utc=2026-07-03T14:56:20.5833420Z; report_utc=2026-07-03T14:53:52.8534086Z |
| installed screenshot present: about.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\about.png |
| installed screenshot newer than owner gate: about.png | PASS | screenshot_utc=2026-07-03T14:56:22.1682743Z; report_utc=2026-07-03T14:53:52.8534086Z |

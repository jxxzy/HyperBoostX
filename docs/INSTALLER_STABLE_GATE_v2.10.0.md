# Installer Stable Gate v2.10.0

Generated: 2026-07-02 15.58.13 +07:00
Expected version: 2.10.0
Status: PASS

| Check | Status | Detail |
| --- | --- | --- |
| installer artifact exists | PASS | F:\BOOSTER BY MR.4NONY\HyperBoostXInstaller.exe |
| installer SHA256 available | PASS | E0846546DF9F62CB8A6A0D42D1D9D8CFC7DBB835632F47177C64BB931D8B9609 |
| installer newer than release inputs | PASS | installer_utc=2026-07-02T08:46:28.8024212Z; latest_input_utc=2026-07-02T08:34:27.4732513Z; latest_input=F:\BOOSTER BY MR.4NONY\wpf\Views\SettingsView.xaml |
| owner admin evidence present | PASS | F:\BOOSTER BY MR.4NONY\docs\runtime-audit\owner_admin_stable_gate_report.json |
| owner evidence newer than installer | PASS | report_utc=2026-07-02T08:47:34.2868651Z; installer_utc=2026-07-02T08:46:28.8024212Z |
| owner admin evidence expected version | PASS | actual=2.10.0; expected=2.10.0 |
| owner admin evidence ok | PASS | ok=True |
| runtime verifier exit 0 | PASS | exit=0 |
| registry DisplayVersion matches expected | PASS | 2.10.0 |
| desktop shortcut targets launcher | PASS | [{"path":"C:\\Users\\jxxzy\\OneDrive\\Desktop\\HyperBoostX.lnk","exists":false,"target":null,"working_directory":null,"icon_location":null,"error":null},{"path":"C:\\Users\\Public\\Desktop\\HyperBoostX.lnk","exists":true,"target":"C:\\Program Files\\HyperBoostX\\HyperBoostX.exe","working_directory":"C:\\Program Files\\HyperBoostX\\runtime\\backend","icon_location":",0","error":null}] |
| start menu shortcut targets launcher | PASS | {"path":"C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\HyperBoostX\\HyperBoostX.lnk","exists":true,"target":"C:\\Program Files\\HyperBoostX\\HyperBoostX.exe","working_directory":"C:\\Program Files\\HyperBoostX\\runtime\\backend","icon_location":",0","error":null} |
| backend health on port 5000 | PASS | {"ok":true,"uri":"http://127.0.0.1:5000/api/health","error":null,"data":{"backend_mode":"stable","feature_registry_status":{"action_map_found":true,"action_map_source":"C:\\Program Files\\HyperBoostX\\runtime\\wpf\\Data\\ui_action_map_v2_10.json","errors":[],"expected":{"expected_non_real_visible_in_stable":0,"expected_stable_buttons":596,"expected_stable_menus":72,"expected_unique_ui_endpoints":165},"non_real_visible_in_stable":0,"stable_ui_ok":true,"stable_visible_buttons":596,"stable_visible_features":72,"warnings":[]},"local_only":true,"service":"HyperBoostX Backend","session_token_required":true,"status":"ok","version":"2.10.0"}} |
| backend version matches expected | PASS | 2.10.0 |
| WPF installed smoke | PASS | [{"name":"HyperBoostX","id":27164,"path":"C:\\Program Files\\HyperBoostX\\HyperBoostX.exe","from_install":true},{"name":"hyperboost_backend","id":2792,"path":"C:\\Program Files\\HyperBoostX\\runtime\\backend\\hyperboost_backend.exe","from_install":true},{"name":"hyperboost_backend","id":25920,"path":"C:\\Program Files\\HyperBoostX\\runtime\\backend\\hyperboost_backend.exe","from_install":true}] |
| token sync inferred | PASS | session_token_required=True; wpf_running=True |
| no orphan installed processes | PASS |  |
| silent uninstall | PASS | Quiet uninstall completed. |
| silent reinstall | PASS | F:\BOOSTER BY MR.4NONY\HyperBoostXInstaller.exe |
| runtime verifier after reinstall | PASS | exit=0 |
| installed screenshot present: dashboard.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\dashboard.png |
| installed screenshot newer than owner gate: dashboard.png | PASS | screenshot_utc=2026-07-02T08:47:53.0063743Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: dashboard-after-scroll.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\dashboard-after-scroll.png |
| installed screenshot newer than owner gate: dashboard-after-scroll.png | PASS | screenshot_utc=2026-07-02T08:47:55.7237150Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: performance.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\performance.png |
| installed screenshot newer than owner gate: performance.png | PASS | screenshot_utc=2026-07-02T08:47:57.1905499Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: startup.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\startup.png |
| installed screenshot newer than owner gate: startup.png | PASS | screenshot_utc=2026-07-02T08:47:58.6695567Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: background-apps.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\background-apps.png |
| installed screenshot newer than owner gate: background-apps.png | PASS | screenshot_utc=2026-07-02T08:48:00.1421555Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: cleanup.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\cleanup.png |
| installed screenshot newer than owner gate: cleanup.png | PASS | screenshot_utc=2026-07-02T08:48:01.6223619Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: storage.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\storage.png |
| installed screenshot newer than owner gate: storage.png | PASS | screenshot_utc=2026-07-02T08:48:03.0878163Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: one-click-boost.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\one-click-boost.png |
| installed screenshot newer than owner gate: one-click-boost.png | PASS | screenshot_utc=2026-07-02T08:48:04.5413019Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: gaming-mode.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\gaming-mode.png |
| installed screenshot newer than owner gate: gaming-mode.png | PASS | screenshot_utc=2026-07-02T08:48:05.9857673Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: smart-recommendation.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\smart-recommendation.png |
| installed screenshot newer than owner gate: smart-recommendation.png | PASS | screenshot_utc=2026-07-02T08:48:07.4342316Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: gpu-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\gpu-center.png |
| installed screenshot newer than owner gate: gpu-center.png | PASS | screenshot_utc=2026-07-02T08:48:08.9009796Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: gaming-booster.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\gaming-booster.png |
| installed screenshot newer than owner gate: gaming-booster.png | PASS | screenshot_utc=2026-07-02T08:48:10.3834669Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: streaming-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\streaming-center.png |
| installed screenshot newer than owner gate: streaming-center.png | PASS | screenshot_utc=2026-07-02T08:48:11.8697025Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: creator-mode.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\creator-mode.png |
| installed screenshot newer than owner gate: creator-mode.png | PASS | screenshot_utc=2026-07-02T08:48:13.3569892Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: network-booster.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\network-booster.png |
| installed screenshot newer than owner gate: network-booster.png | PASS | screenshot_utc=2026-07-02T08:48:14.8388924Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: dns-latency-tools.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\dns-latency-tools.png |
| installed screenshot newer than owner gate: dns-latency-tools.png | PASS | screenshot_utc=2026-07-02T08:48:16.3248763Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: privacy-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\privacy-center.png |
| installed screenshot newer than owner gate: privacy-center.png | PASS | screenshot_utc=2026-07-02T08:48:17.8202173Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: security-health.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\security-health.png |
| installed screenshot newer than owner gate: security-health.png | PASS | screenshot_utc=2026-07-02T08:48:19.3063771Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: apps-manager.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\apps-manager.png |
| installed screenshot newer than owner gate: apps-manager.png | PASS | screenshot_utc=2026-07-02T08:48:20.8024174Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: tweaks-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\tweaks-center.png |
| installed screenshot newer than owner gate: tweaks-center.png | PASS | screenshot_utc=2026-07-02T08:48:22.3002161Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: windows-features.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\windows-features.png |
| installed screenshot newer than owner gate: windows-features.png | PASS | screenshot_utc=2026-07-02T08:48:23.8022628Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: update-control.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\update-control.png |
| installed screenshot newer than owner gate: update-control.png | PASS | screenshot_utc=2026-07-02T08:48:25.3058873Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: repair-tools.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\repair-tools.png |
| installed screenshot newer than owner gate: repair-tools.png | PASS | screenshot_utc=2026-07-02T08:48:26.8393437Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: driver-update-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\driver-update-center.png |
| installed screenshot newer than owner gate: driver-update-center.png | PASS | screenshot_utc=2026-07-02T08:48:28.3557047Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: app-uninstaller.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\app-uninstaller.png |
| installed screenshot newer than owner gate: app-uninstaller.png | PASS | screenshot_utc=2026-07-02T08:48:29.8730097Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: restore-backup.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\restore-backup.png |
| installed screenshot newer than owner gate: restore-backup.png | PASS | screenshot_utc=2026-07-02T08:48:31.4029992Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: settings.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\settings.png |
| installed screenshot newer than owner gate: settings.png | PASS | screenshot_utc=2026-07-02T08:48:32.9470808Z; report_utc=2026-07-02T08:47:34.2868651Z |
| installed screenshot present: about.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\about.png |
| installed screenshot newer than owner gate: about.png | PASS | screenshot_utc=2026-07-02T08:48:34.5037602Z; report_utc=2026-07-02T08:47:34.2868651Z |

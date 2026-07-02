# Installer Stable Gate v2.10.0

Generated: 2026-07-02 18.41.45 +07:00
Expected version: 2.10.0
Status: PASS

| Check | Status | Detail |
| --- | --- | --- |
| installer artifact exists | PASS | F:\BOOSTER BY MR.4NONY\HyperBoostXInstaller.exe |
| installer SHA256 available | PASS | D83B62AEA35323E21BC81584821180C16433B34136E6853C3D5E992E518CA05F |
| installer newer than release inputs | PASS | installer_utc=2026-07-02T11:36:33.7006203Z; latest_input_utc=2026-07-02T11:35:05.3985720Z; latest_input=F:\BOOSTER BY MR.4NONY\wpf\ViewModels\MainWindowViewModel.cs |
| owner admin evidence present | PASS | F:\BOOSTER BY MR.4NONY\docs\runtime-audit\owner_admin_stable_gate_report.json |
| owner evidence newer than installer | PASS | report_utc=2026-07-02T11:37:33.8267646Z; installer_utc=2026-07-02T11:36:33.7006203Z |
| owner admin evidence expected version | PASS | actual=2.10.0; expected=2.10.0 |
| owner admin evidence ok | PASS | ok=True |
| runtime verifier exit 0 | PASS | exit=0 |
| registry DisplayVersion matches expected | PASS | 2.10.0 |
| desktop shortcut targets launcher | PASS | [{"path":"C:\\Users\\jxxzy\\OneDrive\\Desktop\\HyperBoostX.lnk","exists":false,"target":null,"working_directory":null,"icon_location":null,"error":null},{"path":"C:\\Users\\Public\\Desktop\\HyperBoostX.lnk","exists":true,"target":"C:\\Program Files\\HyperBoostX\\HyperBoostX.exe","working_directory":"C:\\Program Files\\HyperBoostX\\runtime\\backend","icon_location":",0","error":null}] |
| start menu shortcut targets launcher | PASS | {"path":"C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\HyperBoostX\\HyperBoostX.lnk","exists":true,"target":"C:\\Program Files\\HyperBoostX\\HyperBoostX.exe","working_directory":"C:\\Program Files\\HyperBoostX\\runtime\\backend","icon_location":",0","error":null} |
| backend health on port 5000 | PASS | {"ok":true,"uri":"http://127.0.0.1:5000/api/health","error":null,"data":{"backend_mode":"stable","feature_registry_status":{"action_map_found":true,"action_map_source":"C:\\Program Files\\HyperBoostX\\runtime\\wpf\\Data\\ui_action_map_v2_10.json","errors":[],"expected":{"expected_non_real_visible_in_stable":0,"expected_stable_buttons":596,"expected_stable_menus":72,"expected_unique_ui_endpoints":165},"non_real_visible_in_stable":0,"stable_ui_ok":true,"stable_visible_buttons":596,"stable_visible_features":72,"warnings":[]},"local_only":true,"service":"HyperBoostX Backend","session_token_required":true,"status":"ok","version":"2.10.0"}} |
| backend version matches expected | PASS | 2.10.0 |
| WPF installed smoke | PASS | [{"name":"HyperBoostX","id":22260,"path":"C:\\Program Files\\HyperBoostX\\runtime\\wpf\\HyperBoostX.exe","from_install":true},{"name":"HyperBoostX","id":25380,"path":"C:\\Program Files\\HyperBoostX\\HyperBoostX.exe","from_install":true},{"name":"hyperboost_backend","id":20916,"path":"C:\\Program Files\\HyperBoostX\\runtime\\backend\\hyperboost_backend.exe","from_install":true},{"name":"hyperboost_backend","id":29836,"path":"C:\\Program Files\\HyperBoostX\\runtime\\backend\\hyperboost_backend.exe","from_install":true}] |
| token sync inferred | PASS | session_token_required=True; wpf_running=True |
| no orphan installed processes | PASS |  |
| silent uninstall | PASS | Quiet uninstall completed. |
| silent reinstall | PASS | F:\BOOSTER BY MR.4NONY\HyperBoostXInstaller.exe |
| runtime verifier after reinstall | PASS | exit=0 |
| installed screenshot present: dashboard.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\dashboard.png |
| installed screenshot newer than owner gate: dashboard.png | PASS | screenshot_utc=2026-07-02T11:37:56.3393184Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: dashboard-after-scroll.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\dashboard-after-scroll.png |
| installed screenshot newer than owner gate: dashboard-after-scroll.png | PASS | screenshot_utc=2026-07-02T11:37:58.9871656Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: performance.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\performance.png |
| installed screenshot newer than owner gate: performance.png | PASS | screenshot_utc=2026-07-02T11:38:00.4464225Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: startup.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\startup.png |
| installed screenshot newer than owner gate: startup.png | PASS | screenshot_utc=2026-07-02T11:38:01.8996233Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: background-apps.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\background-apps.png |
| installed screenshot newer than owner gate: background-apps.png | PASS | screenshot_utc=2026-07-02T11:38:03.3486474Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: cleanup.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\cleanup.png |
| installed screenshot newer than owner gate: cleanup.png | PASS | screenshot_utc=2026-07-02T11:38:04.8039093Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: storage.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\storage.png |
| installed screenshot newer than owner gate: storage.png | PASS | screenshot_utc=2026-07-02T11:38:06.2536131Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: one-click-boost.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\one-click-boost.png |
| installed screenshot newer than owner gate: one-click-boost.png | PASS | screenshot_utc=2026-07-02T11:38:07.7080656Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: gaming-mode.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\gaming-mode.png |
| installed screenshot newer than owner gate: gaming-mode.png | PASS | screenshot_utc=2026-07-02T11:38:09.1628415Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: smart-recommendation.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\smart-recommendation.png |
| installed screenshot newer than owner gate: smart-recommendation.png | PASS | screenshot_utc=2026-07-02T11:38:10.6342989Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: gpu-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\gpu-center.png |
| installed screenshot newer than owner gate: gpu-center.png | PASS | screenshot_utc=2026-07-02T11:38:12.1003428Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: gaming-booster.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\gaming-booster.png |
| installed screenshot newer than owner gate: gaming-booster.png | PASS | screenshot_utc=2026-07-02T11:38:13.5736354Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: streaming-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\streaming-center.png |
| installed screenshot newer than owner gate: streaming-center.png | PASS | screenshot_utc=2026-07-02T11:38:15.0424205Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: creator-mode.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\creator-mode.png |
| installed screenshot newer than owner gate: creator-mode.png | PASS | screenshot_utc=2026-07-02T11:38:16.5080455Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: network-booster.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\network-booster.png |
| installed screenshot newer than owner gate: network-booster.png | PASS | screenshot_utc=2026-07-02T11:38:17.9801526Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: dns-latency-tools.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\dns-latency-tools.png |
| installed screenshot newer than owner gate: dns-latency-tools.png | PASS | screenshot_utc=2026-07-02T11:38:19.4483613Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: privacy-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\privacy-center.png |
| installed screenshot newer than owner gate: privacy-center.png | PASS | screenshot_utc=2026-07-02T11:38:20.9198767Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: security-health.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\security-health.png |
| installed screenshot newer than owner gate: security-health.png | PASS | screenshot_utc=2026-07-02T11:38:22.3867678Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: apps-manager.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\apps-manager.png |
| installed screenshot newer than owner gate: apps-manager.png | PASS | screenshot_utc=2026-07-02T11:38:23.8523095Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: tweaks-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\tweaks-center.png |
| installed screenshot newer than owner gate: tweaks-center.png | PASS | screenshot_utc=2026-07-02T11:38:25.3179114Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: windows-features.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\windows-features.png |
| installed screenshot newer than owner gate: windows-features.png | PASS | screenshot_utc=2026-07-02T11:38:26.7884752Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: update-control.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\update-control.png |
| installed screenshot newer than owner gate: update-control.png | PASS | screenshot_utc=2026-07-02T11:38:28.2725588Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: repair-tools.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\repair-tools.png |
| installed screenshot newer than owner gate: repair-tools.png | PASS | screenshot_utc=2026-07-02T11:38:29.7425618Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: driver-update-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\driver-update-center.png |
| installed screenshot newer than owner gate: driver-update-center.png | PASS | screenshot_utc=2026-07-02T11:38:31.2291128Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: app-uninstaller.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\app-uninstaller.png |
| installed screenshot newer than owner gate: app-uninstaller.png | PASS | screenshot_utc=2026-07-02T11:38:32.7066835Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: restore-backup.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\restore-backup.png |
| installed screenshot newer than owner gate: restore-backup.png | PASS | screenshot_utc=2026-07-02T11:38:34.1964462Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: settings.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\settings.png |
| installed screenshot newer than owner gate: settings.png | PASS | screenshot_utc=2026-07-02T11:38:35.6770042Z; report_utc=2026-07-02T11:37:33.8267646Z |
| installed screenshot present: about.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\about.png |
| installed screenshot newer than owner gate: about.png | PASS | screenshot_utc=2026-07-02T11:38:37.1685179Z; report_utc=2026-07-02T11:37:33.8267646Z |

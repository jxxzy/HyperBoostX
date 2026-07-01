# Installer Stable Gate v2.10.0

Generated: 2026-07-02 02.36.40 +07:00
Expected version: 2.10.0
Status: PASS

| Check | Status | Detail |
| --- | --- | --- |
| installer artifact exists | PASS | F:\BOOSTER BY MR.4NONY\HyperBoostXInstaller.exe |
| installer SHA256 available | PASS | 8960200B125DBF9A2E12A77A1C7CABFDC386DBF0628CA066ACC6BE1C7B88B4F4 |
| installer newer than release inputs | PASS | installer_utc=2026-07-01T19:20:55.1421778Z; latest_input_utc=2026-07-01T18:59:34.3910293Z; latest_input=F:\BOOSTER BY MR.4NONY\wpf\Data\ui_action_map_v2_10.json |
| owner admin evidence present | PASS | F:\BOOSTER BY MR.4NONY\docs\runtime-audit\owner_admin_stable_gate_report.json |
| owner evidence newer than installer | PASS | report_utc=2026-07-01T19:21:41.3553113Z; installer_utc=2026-07-01T19:20:55.1421778Z |
| owner admin evidence expected version | PASS | actual=2.10.0; expected=2.10.0 |
| owner admin evidence ok | PASS | ok=True |
| runtime verifier exit 0 | PASS | exit=0 |
| registry DisplayVersion matches expected | PASS | 2.10.0 |
| desktop shortcut targets launcher | PASS | [{"path":"C:\\Users\\jxxzy\\OneDrive\\Desktop\\HyperBoostX.lnk","exists":false,"target":null,"working_directory":null,"icon_location":null,"error":null},{"path":"C:\\Users\\Public\\Desktop\\HyperBoostX.lnk","exists":true,"target":"C:\\Program Files\\HyperBoostX\\HyperBoostX.exe","working_directory":"C:\\Program Files\\HyperBoostX\\runtime\\backend","icon_location":",0","error":null}] |
| start menu shortcut targets launcher | PASS | {"path":"C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\HyperBoostX\\HyperBoostX.lnk","exists":true,"target":"C:\\Program Files\\HyperBoostX\\HyperBoostX.exe","working_directory":"C:\\Program Files\\HyperBoostX\\runtime\\backend","icon_location":",0","error":null} |
| backend health on port 5000 | PASS | {"ok":true,"uri":"http://127.0.0.1:5000/api/health","error":null,"data":{"backend_mode":"stable","feature_registry_status":{"action_map_found":true,"action_map_source":"C:\\Program Files\\HyperBoostX\\runtime\\wpf\\Data\\ui_action_map_v2_10.json","errors":[],"expected":{"expected_non_real_visible_in_stable":0,"expected_stable_buttons":596,"expected_stable_menus":72,"expected_unique_ui_endpoints":165},"non_real_visible_in_stable":0,"stable_ui_ok":true,"stable_visible_buttons":596,"stable_visible_features":72,"warnings":[]},"local_only":true,"service":"HyperBoostX Backend","session_token_required":true,"status":"ok","version":"2.10.0"}} |
| backend version matches expected | PASS | 2.10.0 |
| WPF installed smoke | PASS | [{"name":"HyperBoostX","id":20148,"path":"C:\\Program Files\\HyperBoostX\\HyperBoostX.exe","from_install":true},{"name":"hyperboost_backend","id":25312,"path":"C:\\Program Files\\HyperBoostX\\runtime\\backend\\hyperboost_backend.exe","from_install":true},{"name":"hyperboost_backend","id":26136,"path":"C:\\Program Files\\HyperBoostX\\runtime\\backend\\hyperboost_backend.exe","from_install":true}] |
| token sync inferred | PASS | session_token_required=True; wpf_running=True |
| no orphan installed processes | PASS |  |
| silent uninstall | PASS | Quiet uninstall completed. |
| silent reinstall | PASS | F:\BOOSTER BY MR.4NONY\HyperBoostXInstaller.exe |
| runtime verifier after reinstall | PASS | exit=0 |
| installed screenshot present: dashboard.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\dashboard.png |
| installed screenshot newer than owner gate: dashboard.png | PASS | screenshot_utc=2026-07-01T19:24:48.8631059Z; report_utc=2026-07-01T19:21:41.3553113Z |
| installed screenshot present: performance.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\performance.png |
| installed screenshot newer than owner gate: performance.png | PASS | screenshot_utc=2026-07-01T19:24:50.3983923Z; report_utc=2026-07-01T19:21:41.3553113Z |
| installed screenshot present: gpu-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\gpu-center.png |
| installed screenshot newer than owner gate: gpu-center.png | PASS | screenshot_utc=2026-07-01T19:24:51.8724754Z; report_utc=2026-07-01T19:21:41.3553113Z |
| installed screenshot present: streaming-center.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\streaming-center.png |
| installed screenshot newer than owner gate: streaming-center.png | PASS | screenshot_utc=2026-07-01T19:24:53.3316005Z; report_utc=2026-07-01T19:21:41.3553113Z |
| installed screenshot present: settings.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\settings.png |
| installed screenshot newer than owner gate: settings.png | PASS | screenshot_utc=2026-07-01T19:24:54.8216645Z; report_utc=2026-07-01T19:21:41.3553113Z |
| installed screenshot present: about.png | PASS | F:\BOOSTER BY MR.4NONY\docs\screenshots\v2.10.0-final\about.png |
| installed screenshot newer than owner gate: about.png | PASS | screenshot_utc=2026-07-01T19:24:56.3076543Z; report_utc=2026-07-01T19:21:41.3553113Z |

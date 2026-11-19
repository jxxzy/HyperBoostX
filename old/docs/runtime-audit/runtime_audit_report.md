# HyperBoostX Installed Runtime Audit

Expected version: 2.10.0
Install location: C:\Program Files\HyperBoostX
Launch installed app: True
Stop after probe: True
Backend: http://127.0.0.1:5000

| Check | Status | Evidence |
| --- | --- | --- |
| registry installed | PASS | C:\Program Files\HyperBoostX |
| DisplayVersion matches source | PASS | 2.10.0 |
| launcher exists | PASS | C:\Program Files\HyperBoostX\HyperBoostX.exe |
| WPF runtime exists | PASS | C:\Program Files\HyperBoostX\runtime\wpf\HyperBoostX.exe |
| backend runtime exists | PASS | C:\Program Files\HyperBoostX\runtime\backend\hyperboost_backend.exe |
| desktop shortcut exists | PASS | [{"path":"C:\\Users\\jxxzy\\OneDrive\\Desktop\\HyperBoostX.lnk","exists":false,"target":null,"working_directory":null,"icon_location":null,"error":null},{"path":"C:\\Users\\Public\\Desktop\\HyperBoostX.lnk","exists":true,"target":"exists; target resolution skipped to avoid COM shell hangs","working_directory":null,"icon_location":null,"error":null}] |
| start menu shortcut exists | PASS | {"path":"C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\HyperBoostX\\HyperBoostX.lnk","exists":true,"target":"exists; target resolution skipped to avoid COM shell hangs","working_directory":null,"icon_location":null,"error":null} |
| backend health works | PASS | http://127.0.0.1:5000 |
| backend version matches source | PASS | 2.10.0 |
| installed action map exists | PASS | C:\Program Files\HyperBoostX\runtime\wpf\Data\ui_action_map_v2_10.json |
| installed action map exists | PASS | C:\Program Files\HyperBoostX\runtime\wpf\Data\ui_action_map_v2_10.json |
| installed action map JSON parses | PASS | C:\Program Files\HyperBoostX\runtime\wpf\Data\ui_action_map_v2_10.json |
| installed action map app_version matches VERSION | PASS | actual=2.10.0; expected=2.10.0 |
| installed action map channel matches VERSION | PASS | actual=Stable; expected=Stable |
| installed action map summary total_menus | PASS | actual=73; expected=73 |
| installed action map summary total_buttons | PASS | actual=606; expected=606 |
| installed action map summary total_active_buttons | PASS | actual=606; expected=606 |
| installed action map summary total_partial_or_roadmap_buttons | PASS | actual=0; expected=0 |
| installed action map summary total_unique_endpoints_used | PASS | actual=173; expected=173 |
| installed action map menus length | PASS | actual=73; expected=73 |
| installed action map computed button count | PASS | actual=606; expected=606 |
| installed action map computed unique endpoint count | PASS | actual=173; expected=173 |
| installed action map all menus are Real | PASS | non_real_menus=0 |
| installed action map all actions are Real | PASS | non_real_actions=0 |
| installed action map all action paths start with /api/ | PASS | bad_paths=0 |
| installed action map non-GET actions have safety_guard | PASS | unguarded_mutations=0 |
| feature audit endpoint works | PASS | http://127.0.0.1:5000/api/features/audit |
| feature stable-visible endpoint works | PASS | http://127.0.0.1:5000/api/features/stable-visible |
| feature non-real endpoint works | PASS | http://127.0.0.1:5000/api/features/non-real |
| feature audit stable_ui_ok true | PASS | ok=True |
| feature audit stable_visible_features matches contract | PASS | actual=73; expected=73 |
| feature audit stable_visible_buttons matches contract | PASS | actual=606; expected=606 |
| feature audit non_real_visible_in_stable is 0 | PASS | actual=0; expected=0 |
| stable-visible count matches contract | PASS | actual=73; expected=73 |
| non-real count is 0 | PASS | actual=0; expected=0 |
| WPF installed smoke | PASS | wpf_running_from_install=True launch_error= |
| token sync | PASS | INFERRED_FROM_LAUNCHER_ENV_AND_TOKEN_REQUIRED_HEALTH |
| no orphan process | PASS |  |
| legacy active runtime not detected | PASS | [{"name":"HyperBoostX","id":21192,"path":"C:\\Program Files\\HyperBoostX\\runtime\\wpf\\HyperBoostX.exe","from_install":true},{"name":"HyperBoostX","id":32220,"path":"C:\\Program Files\\HyperBoostX\\HyperBoostX.exe","from_install":true},{"name":"hyperboost_backend","id":29724,"path":"C:\\Program Files\\HyperBoostX\\runtime\\backend\\hyperboost_backend.exe","from_install":true},{"name":"hyperboost_backend","id":30332,"path":"C:\\Program Files\\HyperBoostX\\runtime\\backend\\hyperboost_backend.exe","from_install":true}] |

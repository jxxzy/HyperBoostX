# Owner Admin Stable Gate Result v2.10.0

Expected version: 2.10.0
Backend port: 5000
Installer: <REPO_ROOT>\HyperBoostXInstaller.exe
Status: PASS

| Step | Status | Detail |
| --- | --- | --- |
| administrator shell | PASS | Running elevated. |
| installer exists | PASS | <REPO_ROOT>\HyperBoostXInstaller.exe |
| record old installed version | PASS | 2.10.0 at <INSTALL_DIR> |
| stop existing HyperBoostX processes | PASS |  |
| uninstall previous HyperBoostX | PASS | 2.10.0 via registry uninstall command |
| old registry entry removed | PASS |  |
| silent install current installer | PASS | <REPO_ROOT>\HyperBoostXInstaller.exe |
| registry DisplayVersion matches expected | PASS | 2.10.0 |
| registry Publisher recorded | PASS | HyperBoostX / jxxzy |
| launcher installed | PASS | <INSTALL_DIR>\HyperBoostX.exe |
| WPF runtime installed | PASS | <INSTALL_DIR>\runtime\wpf\HyperBoostX.exe |
| backend runtime installed | PASS | <INSTALL_DIR>\runtime\backend\hyperboost_backend.exe |
| installed action map present | PASS | <INSTALL_DIR>\runtime\wpf\Data\ui_action_map_v2_10.json |
| installed action map exists | PASS | <INSTALL_DIR>\runtime\wpf\Data\ui_action_map_v2_10.json |
| installed action map JSON parses | PASS | <INSTALL_DIR>\runtime\wpf\Data\ui_action_map_v2_10.json |
| installed action map app_version matches VERSION | PASS | actual=2.10.0; expected=2.10.0 |
| installed action map channel matches VERSION | PASS | actual=Stable; expected=Stable |
| installed action map summary total_menus | PASS | actual=73; expected=73 |
| installed action map summary total_buttons | PASS | actual=606; expected=606 |
| installed action map summary total_active_buttons | PASS | actual=606; expected=606 |
| installed action map summary total_partial_or_roadmap_buttons | PASS | actual=0; expected=0 |
| installed action map summary total_unique_endpoints_used | PASS | actual=167; expected=167 |
| installed action map menus length | PASS | actual=73; expected=73 |
| installed action map computed button count | PASS | actual=606; expected=606 |
| installed action map computed unique endpoint count | PASS | actual=167; expected=167 |
| installed action map all menus are Real | PASS | non_real_menus=0 |
| installed action map all actions are Real | PASS | non_real_actions=0 |
| installed action map all action paths start with /api/ | PASS | bad_paths=0 |
| installed action map non-GET actions have safety_guard | PASS | unguarded_mutations=0 |
| desktop shortcut targets launcher | PASS | [{"path":"<USER_DESKTOP>\\HyperBoostX.lnk","exists":false,"target":null,"working_directory":null,"icon_location":null,"error":null},{"path":"<PUBLIC_DESKTOP>\\HyperBoostX.lnk","exists":true,"target":"<INSTALL_DIR>\\HyperBoostX.exe","working_directory":"<INSTALL_DIR>\\runtime\\backend","icon_location":",0","error":null}] |
| start menu shortcut targets launcher | PASS | {"path":"<START_MENU>\\Programs\\HyperBoostX\\HyperBoostX.lnk","exists":true,"target":"<INSTALL_DIR>\\HyperBoostX.exe","working_directory":"<INSTALL_DIR>\\runtime\\backend","icon_location":",0","error":null} |
| launch installed HyperBoostX | PASS | pid=29392 |
| backend health on port 5000 | PASS | {"ok":true,"uri":"http://127.0.0.1:5000/api/health","error":null,"data":{"backend_mode":"stable","feature_registry_status":{"action_map_found":true,"action_map_source":"<INSTALL_DIR>\\runtime\\wpf\\Data\\ui_action_map_v2_10.json","errors":[],"expected":{"expected_non_real_visible_in_stable":0,"expected_stable_buttons":606,"expected_stable_menus":73,"expected_unique_ui_endpoints":167},"non_real_visible_in_stable":0,"stable_ui_ok":true,"stable_visible_buttons":606,"stable_visible_features":73,"warnings":[]},"local_only":true,"service":"HyperBoostX Backend","session_token_required":true,"status":"ok","version":"2.10.0"}} |
| backend version matches expected | PASS | 2.10.0 |
| feature audit endpoint works | PASS | http://127.0.0.1:5000/api/features/audit |
| feature stable-visible endpoint works | PASS | http://127.0.0.1:5000/api/features/stable-visible |
| feature non-real endpoint works | PASS | http://127.0.0.1:5000/api/features/non-real |
| feature audit stable_ui_ok true | PASS | ok=True |
| feature audit stable_visible_features matches contract | PASS | actual=73; expected=73 |
| feature audit stable_visible_buttons matches contract | PASS | actual=606; expected=606 |
| feature audit non_real_visible_in_stable is 0 | PASS | actual=0; expected=0 |
| stable-visible count matches contract | PASS | actual=73; expected=73 |
| non-real count is 0 | PASS | actual=0; expected=0 |
| WPF installed smoke | PASS | [{"name":"HyperBoostX","id":8660,"path":"<INSTALL_DIR>\\runtime\\wpf\\HyperBoostX.exe","from_install":true},{"name":"HyperBoostX","id":29392,"path":"<INSTALL_DIR>\\HyperBoostX.exe","from_install":true},{"name":"hyperboost_backend","id":1408,"path":"<INSTALL_DIR>\\runtime\\backend\\hyperboost_backend.exe","from_install":true},{"name":"hyperboost_backend","id":27584,"path":"<INSTALL_DIR>\\runtime\\backend\\hyperboost_backend.exe","from_install":true}] |
| token sync inferred | PASS | session_token_required=True; wpf_running=True |
| close installed app | PASS | [{"name":"HyperBoostX","id":8660,"path":"<INSTALL_DIR>\\runtime\\wpf\\HyperBoostX.exe","from_install":true},{"name":"HyperBoostX","id":29392,"path":"<INSTALL_DIR>\\HyperBoostX.exe","from_install":true},{"name":"hyperboost_backend","id":1408,"path":"<INSTALL_DIR>\\runtime\\backend\\hyperboost_backend.exe","from_install":true},{"name":"hyperboost_backend","id":27584,"path":"<INSTALL_DIR>\\runtime\\backend\\hyperboost_backend.exe","from_install":true}] |
| no orphan installed processes | PASS |  |
| silent uninstall | PASS | Quiet uninstall completed. |
| silent uninstall removed registry | PASS |  |
| silent reinstall | PASS | <REPO_ROOT>\HyperBoostXInstaller.exe |
| runtime verifier after reinstall | PASS | exit=0 |

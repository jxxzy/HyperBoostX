# HyperBoostX Installer Payload Verification

Expected version: 2.10.0
Installer: F:\BOOSTER BY MR.4NONY\HyperBoostXInstaller.exe

| Check | Status | Evidence |
| --- | --- | --- |
| version sync script passed | PASS | scripts\\verify_version_sync.ps1 |
| release artifact content script passed | PASS | scripts\\verify_release_artifact_contents.ps1 |
| NSIS file exists | PASS | F:\BOOSTER BY MR.4NONY\HyperBoostXInstaller.nsi |
| NSIS DisplayVersion uses current version | PASS | expected DisplayVersion 2.10.0 |
| NSIS writes HKLM uninstall metadata | PASS | uninstall registry entry |
| NSIS writes QuietUninstallString | PASS | silent uninstall metadata |
| NSIS writes owner publisher | PASS | publisher metadata |
| NSIS creates Start Menu shortcut | PASS | Start Menu shortcut |
| NSIS creates desktop shortcut | PASS | desktop shortcut |
| NSIS installs WPF runtime recursively | PASS | File /r "release\package\wpf\*" |
| NSIS installed action map target path | PASS | $INSTDIR\runtime\wpf\Data\ui_action_map_v2_10.json |
| installer exists | PASS | F:\BOOSTER BY MR.4NONY\HyperBoostXInstaller.exe |
| installer hash available | PASS | e0846546df9f62cb8a6a0d42d1d9d8cfc7dbb835632f47177c64bb931d8b9609 |
| v2.10 checksum file exists | PASS | F:\BOOSTER BY MR.4NONY\docs\release\checksums\SHA256SUMS_2.10.0.txt |
| checksum file includes current installer hash | PASS | e0846546df9f62cb8a6a0d42d1d9d8cfc7dbb835632f47177c64bb931d8b9609 |
| source action map exists | PASS | F:\BOOSTER BY MR.4NONY\wpf\Data\ui_action_map_v2_10.json |
| source action map JSON parses | PASS | F:\BOOSTER BY MR.4NONY\wpf\Data\ui_action_map_v2_10.json |
| source action map app_version matches VERSION | PASS | actual=2.10.0; expected=2.10.0 |
| source action map channel matches VERSION | PASS | actual=Stable; expected=Stable |
| source action map summary total_menus | PASS | actual=72; expected=72 |
| source action map summary total_buttons | PASS | actual=596; expected=596 |
| source action map summary total_active_buttons | PASS | actual=596; expected=596 |
| source action map summary total_partial_or_roadmap_buttons | PASS | actual=0; expected=0 |
| source action map summary total_unique_endpoints_used | PASS | actual=165; expected=165 |
| source action map menus length | PASS | actual=72; expected=72 |
| source action map computed button count | PASS | actual=596; expected=596 |
| source action map computed unique endpoint count | PASS | actual=165; expected=165 |
| source action map all menus are Real | PASS | non_real_menus=0 |
| source action map all actions are Real | PASS | non_real_actions=0 |
| source action map all action paths start with /api/ | PASS | bad_paths=0 |
| source action map non-GET actions have safety_guard | PASS | unguarded_mutations=0 |
| package action map exists | PASS | F:\BOOSTER BY MR.4NONY\release\package\wpf\Data\ui_action_map_v2_10.json |
| package action map JSON parses | PASS | F:\BOOSTER BY MR.4NONY\release\package\wpf\Data\ui_action_map_v2_10.json |
| package action map app_version matches VERSION | PASS | actual=2.10.0; expected=2.10.0 |
| package action map channel matches VERSION | PASS | actual=Stable; expected=Stable |
| package action map summary total_menus | PASS | actual=72; expected=72 |
| package action map summary total_buttons | PASS | actual=596; expected=596 |
| package action map summary total_active_buttons | PASS | actual=596; expected=596 |
| package action map summary total_partial_or_roadmap_buttons | PASS | actual=0; expected=0 |
| package action map summary total_unique_endpoints_used | PASS | actual=165; expected=165 |
| package action map menus length | PASS | actual=72; expected=72 |
| package action map computed button count | PASS | actual=596; expected=596 |
| package action map computed unique endpoint count | PASS | actual=165; expected=165 |
| package action map all menus are Real | PASS | non_real_menus=0 |
| package action map all actions are Real | PASS | non_real_actions=0 |
| package action map all action paths start with /api/ | PASS | bad_paths=0 |
| package action map non-GET actions have safety_guard | PASS | unguarded_mutations=0 |

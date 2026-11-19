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
| WPF installed smoke | PASS | wpf_running_from_install=True launch_error= |
| token sync | PASS | INFERRED_FROM_LAUNCHER_ENV_AND_TOKEN_REQUIRED_HEALTH |
| no orphan process | PASS |  |
| legacy active runtime not detected | PASS | [{"name":"HyperBoostX","id":8516,"path":"C:\\Program Files\\HyperBoostX\\runtime\\wpf\\HyperBoostX.exe","from_install":true},{"name":"HyperBoostX","id":26480,"path":"C:\\Program Files\\HyperBoostX\\HyperBoostX.exe","from_install":true},{"name":"hyperboost_backend","id":19400,"path":"C:\\Program Files\\HyperBoostX\\runtime\\backend\\hyperboost_backend.exe","from_install":true},{"name":"hyperboost_backend","id":25984,"path":"C:\\Program Files\\HyperBoostX\\runtime\\backend\\hyperboost_backend.exe","from_install":true}] |

# HyperBoostX Installed Runtime Audit

Expected version: 2.10.0-beta.1
Install location: C:\Program Files\HyperBoostX
Launch installed app: False
Stop after probe: False
Backend: not found

| Check | Status | Evidence |
| --- | --- | --- |
| registry installed | PASS | C:\Program Files\HyperBoostX |
| DisplayVersion matches source | FAIL | 1.3.0 |
| launcher exists | PASS | C:\Program Files\HyperBoostX\HyperBoostX.exe |
| WPF runtime exists | PASS | C:\Program Files\HyperBoostX\runtime\wpf\HyperBoostX.exe |
| backend runtime exists | PASS | C:\Program Files\HyperBoostX\runtime\backend\hyperboost_backend.exe |
| desktop shortcut exists | PASS | [{"path":"C:\\Users\\jxxzy\\OneDrive\\Desktop\\HyperBoostX.lnk","exists":false,"target":null,"working_directory":null,"icon_location":null,"error":null},{"path":"C:\\Users\\Public\\Desktop\\HyperBoostX.lnk","exists":true,"target":"exists; target resolution skipped to avoid COM shell hangs","working_directory":null,"icon_location":null,"error":null}] |
| start menu shortcut exists | PASS | {"path":"C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\HyperBoostX\\HyperBoostX.lnk","exists":true,"target":"exists; target resolution skipped to avoid COM shell hangs","working_directory":null,"icon_location":null,"error":null} |
| backend health works | FAIL | not found |
| backend version matches source | FAIL | missing |
| WPF installed smoke | FAIL | not launched |
| token sync | FAIL | NOT_TESTED_NO_LAUNCH |
| no orphan process | FAIL | not tested without -StopAfterProbe |
| legacy active runtime not detected | PASS |  |

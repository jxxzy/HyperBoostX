# Installed Runtime Failure Audit v2.10.0-beta.1

Generated: 2026-07-01 03:36 +07:00

Source report: `runtime_audit/runtime_audit_report.md`

## Result

Installed runtime verification failed. This is expected in the current non-admin shell because the rebuilt installer was not run through a fresh elevated install lab.

| Check | Result | Evidence |
| --- | --- | --- |
| Registry installed | PASS | `C:\Program Files\HyperBoostX` |
| DisplayVersion matches source | FAIL | Installed registry reports `1.3.0`; source expects `2.10.0-beta.1` |
| Launcher exists | PASS | `C:\Program Files\HyperBoostX\HyperBoostX.exe` |
| WPF runtime exists | PASS | `C:\Program Files\HyperBoostX\runtime\wpf\HyperBoostX.exe` |
| Backend runtime exists | PASS | `C:\Program Files\HyperBoostX\runtime\backend\hyperboost_backend.exe` |
| Desktop shortcut | PASS | Public desktop shortcut exists |
| Start Menu shortcut | PASS | Start Menu shortcut exists |
| Backend health | FAIL | Backend not found during probe |
| Backend version | FAIL | Missing because backend health was not found |
| WPF installed smoke | FAIL | Not launched in this non-admin verifier run |
| Token sync | FAIL | Not tested because WPF was not launched |
| No orphan process | FAIL | Not tested without `-StopAfterProbe` |

## Owner Lab Command

Run from an elevated PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\clean_install_verify.ps1 -Execute
```

Then verify:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\runtime_verifier.ps1 -LaunchInstalledApp -StopAfterProbe
```


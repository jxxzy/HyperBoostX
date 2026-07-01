# Hardware Matrix v2.10.0

Status: `CURRENT_MACHINE_PASS_EXTERNAL_EXPANSION_RECOMMENDED`

## Validated In This Release Gate

| Scenario | Status | Evidence |
| --- | --- | --- |
| Owner Windows machine install/runtime | PASS | `runtime_audit/owner_admin_stable_gate_report.json` |
| Installed backend health/version | PASS | `/api/health`, `/api/version` on port `5000` |
| WPF installed smoke | PASS | Owner admin stable gate |
| Token sync | PASS | Owner admin stable gate |
| Silent uninstall/reinstall | PASS | Owner admin stable gate |

## External Matrix Still Recommended

| Scenario | Status |
| --- | --- |
| NVIDIA GPU | Recommended external lab expansion |
| AMD Radeon GPU | Recommended external lab expansion |
| Intel GPU/iGPU/Arc | Recommended external lab expansion |
| Microsoft Basic Display Adapter | Recommended external lab expansion |
| No dedicated GPU | Recommended external lab expansion |
| Low-end PC profile | Recommended external lab expansion |

## Rule

HyperBoostX may claim hardware-aware recommendations and safe fallback handling. It must not claim guaranteed FPS, guaranteed ping, official vendor partnership, or universal performance improvement.

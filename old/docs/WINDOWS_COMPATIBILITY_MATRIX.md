# Windows compatibility

| Environment | Current evidence |
| --- | --- |
| Windows 11 Pro x64 build 26200 | Local build, provider scans, UI smoke tests |
| Windows 10 x64 build 19045+ | Source compatibility target; separate machine testing pending |
| ARM64/x86 | No package provided or validation claimed |
| Domain/MDM managed | Detected cases block normal mutations; broad enterprise validation pending |
| RDP/Safe Mode | Normal mutations blocked; actual session matrix pending |
| Standard user | asInvoker manifest and per-user installer; separate standard-account test pending |

Capabilities are probed: existing Registry values/types, enumerated power plans, provider availability. Missing/denied sensor values are unavailable, never zero by default. Windows-tool URI support can vary by build; launch failures are surfaced.

Native power API reference: https://learn.microsoft.com/en-us/windows/win32/power/managing-power-schemes

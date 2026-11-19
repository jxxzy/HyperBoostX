# Integration engineering references (2026-11-19)

These sources guide implementation and release tests; a reference is not evidence that an action passed.

| Area | Official reference | Engineering constraint |
|---|---|---|
| Named pipe access | https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipe-security-and-access-rights | Peer identity and restrictive ACLs; verify cross-elevation with isolated negative tests |
| Impersonation | https://learn.microsoft.com/en-us/windows/win32/ipc/impersonating-a-named-pipe-client | Validate impersonation context and privileges rather than trust claims |
| DNS write | https://learn.microsoft.com/en-us/windows/win32/api/netioapi/nf-netioapi-setinterfacednssettings | Typed interface settings; version and OS support; explicit consent |
| DNS read | https://learn.microsoft.com/en-us/windows/win32/api/netioapi/nf-netioapi-getinterfacednssettings | Read actual settings before and after; preserve automatic/static distinction |
| DNS schema | https://learn.microsoft.com/en-us/windows/win32/api/netioapi/ns-netioapi-dns_interface_settings | Validate flags and schema; restore exact state |
| Power plans | https://learn.microsoft.com/en-us/windows/win32/api/powersetting/nf-powersetting-powersetactivescheme | Select only enumerated existing GUIDs; read-back |
| Power enumeration | https://learn.microsoft.com/en-us/windows/win32/power/managing-power-schemes | Enumerate supported plans |
| GPU WDDM | https://learn.microsoft.com/en-us/windows-hardware/drivers/display/windows-vista-display-driver-model-design-guide | Device/driver dependent evidence |
| GPU measurement | https://learn.microsoft.com/en-us/windows/win32/direct3dtools/pix/articles/timing-captures/pix-timing-captures | Engine-specific counters, not fabricated aggregate |
| GPU caveat | https://learn.microsoft.com/en-us/troubleshoot/windows-client/performance/gpu-process-memory-counters-report-wrong-value | Known process-memory counter discrepancy; use adapter-specific and independently validated data |
| Installer testing | https://learn.microsoft.com/en-us/windows/compatibility/windows-app-certification-kit | Test install/uninstall, performance and compatibility |
| Desktop UI automation | https://learn.microsoft.com/en-us/windows/apps/develop/testing/ | Windows UI Automation; test control behavior, not just existence |
| Shipping checklist | https://learn.microsoft.com/en-us/windows/apps/develop/test-before-you-ship | Separate qualification from development CI |

Existing technical reference registry is preserved; no references promoted to verified behavioral evidence.


## Independent implementation comparison

- https://git.zx2c4.com/wireguard-windows/tree/tunnel/winipcfg/luid.go — corroborates a UTF-16 comma-separated resolver list for the native DNS interface. Reviewed as reference, no third-party code copied.
- https://learn.microsoft.com/en-us/windows/win32/api/netioapi/ns-netioapi-dns_interface_settings — default stack IPv4, nameserver flag scopes mutation to the static list.
- https://learn.microsoft.com/en-us/windows/win32/api/netioapi/nf-netioapi-getinterfacednssettings — set version, leave flags zero, free returned configuration.

## Dashboard load methodology reference

- https://devblogs.microsoft.com/directx/gpus-in-the-task-manager/ — Microsoft describes using the busiest engine to represent utilization instead of summing independent engines. HyperBoostX uses the busiest *observed* engine across validated WDDM instances, marks partial coverage, and does not claim identical Task Manager measurements.

## Privacy and driver references

- https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy — documented machine policy values and precedence limitations.
- https://learn.microsoft.com/en-us/windows/privacy/configure-windows-diagnostic-data-in-your-organization — edition/version-specific diagnostic data policy values.
- https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-pnpentity — actual Configuration Manager error codes.
- https://learn.microsoft.com/en-us/previous-versions/windows/desktop/legacy/aa394354(v=vs.85) — Windows-reported driver metadata and signature flag; independent certificate trust is separate.

## Memory pressure / processor-group coverage

- https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-creatememoryresourcenotification
- https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-querymemoryresourcenotification
- https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getsystemtimes — primary processor-group scope on multi-group systems; aggregate not inferred.

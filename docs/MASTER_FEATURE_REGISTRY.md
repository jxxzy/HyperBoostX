# Master feature registry

Generated from the runtime registry, not menu-button counts. These classifications describe the implemented scope; Stable promotion remains blocked.

| Center | Classification | Evidence |
| --- | --- | --- |
| Dashboard | READ_ONLY_REAL | Windows provider scan; local smoke evidence in artifacts/diagnostic-smoke.json |
| Smart Scan | READ_ONLY_REAL | Windows provider scan; local smoke evidence in artifacts/diagnostic-smoke.json |
| Optimize | READ_ONLY_REAL | Windows provider scan; local smoke evidence in artifacts/diagnostic-smoke.json |
| Gaming | READ_ONLY_REAL | Windows provider scan; local smoke evidence in artifacts/diagnostic-smoke.json |
| Processes | READ_ONLY_REAL | Windows provider scan; local smoke evidence in artifacts/diagnostic-smoke.json |
| Hardware | READ_ONLY_REAL | Windows provider scan; local smoke evidence in artifacts/diagnostic-smoke.json |
| GPU | READ_ONLY_REAL | Windows provider scan; local smoke evidence in artifacts/diagnostic-smoke.json |
| Storage | READ_ONLY_REAL | Windows provider scan; local smoke evidence in artifacts/diagnostic-smoke.json |
| Network | READ_ONLY_REAL | Windows provider scan; local smoke evidence in artifacts/diagnostic-smoke.json |
| Startup | READ_ONLY_REAL | Windows provider scan; local smoke evidence in artifacts/diagnostic-smoke.json |
| Apps | READ_ONLY_REAL | Windows provider scan; local smoke evidence in artifacts/diagnostic-smoke.json |
| Devices | READ_ONLY_REAL | Windows provider scan; local smoke evidence in artifacts/diagnostic-smoke.json |
| Security | READ_ONLY_REAL | Windows provider scan; local smoke evidence in artifacts/diagnostic-smoke.json |
| Windows | READ_ONLY_REAL | Windows provider scan; local smoke evidence in artifacts/diagnostic-smoke.json |
| Power | READ_ONLY_REAL | Windows provider scan; local smoke evidence in artifacts/diagnostic-smoke.json |
| Creator | READ_ONLY_REAL | Windows provider scan; local smoke evidence in artifacts/diagnostic-smoke.json |
| Reliability | READ_ONLY_REAL | Windows provider scan; local smoke evidence in artifacts/diagnostic-smoke.json |

## Registered reversible configuration actions

| ID | Scope | Risk | Verification |
| --- | --- | --- | --- |
| windows.visual.transparency.disable | Current user | Low · reversible | Typed native read-back; production target hardware qualification pending |
| windows.visual.transparency.enable | Current user | Low · reversible | Typed native read-back; production target hardware qualification pending |
| gaming.capture.disable | Current user | Low · reversible | Typed native read-back; production target hardware qualification pending |
| gaming.capture.enable | Current user | Low · reversible | Typed native read-back; production target hardware qualification pending |
| windows.power.set_plan | Current user / PC power policy | Medium · reversible | Typed native read-back; production target hardware qualification pending |
| startup.current_user.disable | Current user | Medium · reversible | Typed native read-back; production target hardware qualification pending |
| storage.temp.quarantine | Current user's temporary folder | Moderate · reversible | Typed native read-back; production target hardware qualification pending |

## External Windows handoffs

Launching these tools is the verified behavior. Their operations are not counted as native HyperBoostX mutations.

- Gaming: Windows Game Mode (`ms-settings:gaming-gamemode`)
- Gaming: Game captures (`ms-settings:gaming-gamedvr`)
- Gaming: Graphics preference (`ms-settings:display-advancedgraphics`)
- GPU: Graphics preference (`ms-settings:display-advancedgraphics`)
- GPU: Display settings (`ms-settings:display`)
- Storage: Windows Storage Settings (`ms-settings:storagesense`)
- Storage: Disk Cleanup (`cleanmgr.exe`)
- Network: Network settings (`ms-settings:network-status`)
- Network: Advanced network settings (`ms-settings:network-advancedsettings`)
- Startup: Windows Startup Apps (`ms-settings:startupapps`)
- Startup: Task Scheduler (`taskschd.msc`)
- Apps: Installed apps / uninstall (`ms-settings:appsfeatures`)
- Devices: Device Manager (`devmgmt.msc`)
- Devices: Windows Update (`ms-settings:windowsupdate`)
- Security: Windows Security / scan (`windowsdefender:`)
- Security: Firewall settings (`ms-settings:windowsdefender`)
- Windows: Windows Update (`ms-settings:windowsupdate`)
- Windows: Optional features (`optionalfeatures.exe`)
- Windows: Services (`services.msc`)
- Power: Power and battery settings (`ms-settings:powersleep`)
- Creator: Sound settings (`ms-settings:sound`)
- Creator: Volume mixer (`ms-settings:apps-volume`)
- Creator: Microphone privacy (`ms-settings:privacy-microphone`)
- Creator: Camera privacy (`ms-settings:privacy-webcam`)
- Reliability: Event Viewer (`eventvwr.msc`)
- Repair: Windows recovery options (`ms-settings:recovery`)
- Repair: Troubleshooting settings (`ms-settings:troubleshoot`)
- Repair: System Restore (`rstrui.exe`)
- Repair: Windows Update (`ms-settings:windowsupdate`)

## Historical coverage

The raw inventory includes 606 action-map entries and XAML button labels from 35 tags. All action-map entries have a proposed destination or explicitly unimplemented destination. They remain NOT_STARTED at individual legacy-behavior granularity until an equivalence test exists; overlapping modern functionality does not imply parity.

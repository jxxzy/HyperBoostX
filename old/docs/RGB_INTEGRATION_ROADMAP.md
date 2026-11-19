# RGB Integration Roadmap

HyperBoostX v2.0.0 detects RGB-related software where safe, but it does not control RGB devices or modify RGB services.

## Safe Detection Targets

- SignalRGB installed status
- OpenRGB installed status
- MSI Center installed status
- Vendor RGB utility presence where detectable

## Future Requirements

- Read-only device inventory first.
- Explicit vendor/API compatibility list.
- No service stop/start without approval and restore metadata.
- No low-level USB/HID writes without a reviewed implementation.
- Conflict warnings only until reliable control is proven.

## Current Status

Detection/roadmap only. RGB control is not a v2.0.0 feature.

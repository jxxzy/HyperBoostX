# Former Roadmap Converted To Real-Safe Boundaries

Status: DONE for v2.10.0 Stable Unsigned source/route contract.

The following entries no longer appear as Roadmap-only features in the action map:

- Cloud Sync & License Boundary now has local beta license status, activation, and deactivation endpoints.
- Plugin Marketplace now has local catalog, manifest validation, install-to-disabled-catalog, and uninstall endpoints.
- RGB remains an RGB Conflict Detector unless OpenRGB/device control is integrated later.
- Driver update surfaces use official vendor links and reports only; no fake auto-install.
- Unsigned plugin execution stays blocked.

## Reason

The owner requested every visible v2.10 feature to be real. The safe implementation is a real local boundary with Safety Guard blocks for dangerous actions, not a fake production cloud, fake license server, or arbitrary plugin execution.

## Release Rule

No feature may use Roadmap, Preview only, Guidance only, or Partial status in the v2.10 action map. Stable release still requires manual lab evidence.

# LCD Native Engine Design

Generated: 2026-07-01 03:36 +07:00

## Current v2.10.0-beta.1 Scope

HyperBoostX detects LCD-related vendor helpers and classifies roles such as main LCD app, live wallpaper decoder, sensor helper, and vendor bridge. It provides safe-mode preview, hybrid preview/apply guard, native compatibility status, wallpaper analyze, and convert preview endpoints.

## Non-Goals For This Beta

- Do not replace vendor LCD drivers.
- Do not patch KANALI/TRCC/HiMOS binaries.
- Do not kill required LCD helper processes.
- Do not disable required startup entries.
- Do not claim full native LCD control.

## Safe Future Path

1. Keep vendor apps as the default source of truth.
2. Add device capability detection with read-only USB/HID probes.
3. Add a sandboxed preview renderer.
4. Add opt-in export profiles for vendor apps.
5. Only add direct native control after hardware-specific owner lab validation and rollback evidence.


# Safety Guard Spec v2.10.0

## Stable Rule

Stable mode blocks or hides actions that are not proven safe, reversible, and real.

## Blocked By Default

- Force-disabling Windows Defender.
- Permanently disabling Windows Update.
- Stopping or disabling anti-cheat, GPU driver, audio, network, or security services.
- BIOS/UEFI, overclock, undervolt, voltage, or driver mod changes.
- Arbitrary shell execution from AI.
- Destructive cleanup of user personal files.
- Unsigned plugin execution.
- Hidden scheduled tasks or hidden telemetry/upload.

## Required For Mutating Actions

- Preview.
- Explicit user approval.
- Safety guard evaluation.
- Restore metadata when system state changes.
- Report/log output.
- Human-friendly blocked state.

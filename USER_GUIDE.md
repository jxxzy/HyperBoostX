# HyperBoostX User Guide

## Dashboard

Use Dashboard for CPU, RAM, disk, network, device profile, bottleneck, recommendation, and last boost status. If a status is `Partial`, read the details before rerunning.

## Safe Boost

Use Safe Boost first. It focuses on safe cleanup, DNS refresh, cache cleanup, background-app preview, and reversible power/profile actions.

## Restore And Undo

Use Restore & Backup to review restore points, action/session metadata, and backup history. High-risk actions should show `Undo Available` or explain why undo is unavailable.

## NVIDIA Copilot

Open Settings / AI:

1. Confirm provider is NVIDIA.
2. Paste the NVIDIA API key into the masked input.
3. Save API key.
4. Select default and fallback models.
5. Keep Auto Fallback, Safety Guard, and Require Approval enabled.
6. Run Test NVIDIA Connection.

HyperBoostX NVIDIA Copilot produces a plan with actions, risk level, admin requirement, restore availability, expected result, skipped unsafe actions, and approval state. It does not run system actions until the user approves.

## Model Selection

Default model:

- `nvidia/nemotron-3-nano-30b-a3b`

Fallback model:

- `nvidia/nvidia-nemotron-nano-9b-v2`

Use heavier models only when troubleshooting needs deeper reasoning.

## Safe Expectations

HyperBoostX reports estimated and measured gains when available. It does not guarantee FPS increases or permanent Windows repair outcomes.

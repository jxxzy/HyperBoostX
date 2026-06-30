# Security Policy

Current public stable baseline: `HyperBoostX v1.3.0 Stable`

The v2.x line is a development preview until full validation is complete. Security boundaries below apply to both stable and preview lines.

## Local Backend

- Backend binds to `127.0.0.1` only.
- CORS is limited to localhost/127.0.0.1 origins.
- WPF should normally be launched through the .NET launcher so lifecycle and session token setup are consistent.

## Session Token

When `HYPERBOOSTX_SESSION_TOKEN` is present, mutating methods require `X-HyperBoostX-Session`. Missing or invalid tokens return `401`.

## Safety Guard

HyperBoostX blocks:

- Forced Windows Defender disable.
- Permanent Windows Update disable.
- Anti-cheat process/service changes.
- GPU, audio, or network driver service disabling.
- BIOS/UEFI, overclock, undervolt, or voltage actions.
- Arbitrary AI-generated shell execution.
- Destructive cleanup of user documents, downloads, desktop, pictures, videos, music, game saves, or system files.

## AI Restrictions

The v1.4 AI Performance Advisor is a local deterministic diagnosis engine. It may suggest allowlisted safe action IDs, but it does not execute shell commands, bypass Safety Guard, or apply actions without approval.

## Protected Processes

The Protected Process List includes anti-cheat, security, audio, and GPU/driver-related processes. `/api/protection/evaluate-action` blocks dangerous or protected targets.

## Redaction

Crash reports, action logs, exported reports, and diagnostics redact API keys, AI keys, GitHub tokens, Discord webhooks/tokens, bearer tokens, local session tokens, Windows usernames, user profile paths, sensitive local paths, and future license keys.

## Telemetry

Telemetry is off by default. v1.4 includes settings fields for future anonymous usage opt-in, but no online telemetry is sent silently.

## Driver And Installer Safety

HyperBoostX does not auto-download drivers, does not silently install third-party software, and does not claim official NVIDIA/AMD/Intel partnership. Unsigned installer builds must be documented with Unknown Publisher and SmartScreen guidance.

## Reporting Issues

Open a GitHub issue with the template. Do not paste secrets, tokens, private logs, or unredacted crash dumps.

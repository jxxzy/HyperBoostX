# Security Policy

Current stable release: `HyperBoostX v2.10.0 Stable Unsigned`

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

Advisor flows are local deterministic diagnosis and planning surfaces. They may suggest allowlisted safe action IDs, but they do not execute shell commands, bypass Safety Guard, or apply actions without approval.

## Protected Processes

The Protected Process List includes anti-cheat, security, audio, and GPU/driver-related processes. `/api/protection/evaluate-action` blocks dangerous or protected targets.

## Redaction

Crash reports, action logs, exported reports, and diagnostics redact API keys, AI keys, GitHub tokens, Discord webhooks/tokens, bearer tokens, local session tokens, Windows usernames, user profile paths, sensitive local paths, and future license keys.

## Telemetry

Telemetry is off by default. No online telemetry is sent silently.

## Driver And Installer Safety

HyperBoostX does not auto-download drivers, does not silently install third-party software, and does not claim official NVIDIA/AMD/Intel partnership.

The v2.10.0 installer is unsigned because code signing is `SKIPPED_BY_OWNER_NO_CERT`. Distribute it with checksum and Unknown Publisher / SmartScreen guidance.

## Reporting Issues

Open a GitHub issue with the template. Do not paste secrets, tokens, private logs, or unredacted crash dumps.

# Security Reality Audit Design

Generated: 2026-07-01 03:36 +07:00

## Current Scope

Security Reality Audit provides read-only checks and human-friendly classification for:

- WSL status
- Remote access status
- Startup entries
- Recent PowerShell activity
- Vendor service classification
- Suspicious path classification

## Safety Boundaries

The audit does not label normal vendor software as malware without evidence. It classifies components as normal vendor component, suspicious needs review, or manual review based on path and context.

## Endpoints

- `GET /api/security/reality-audit`
- `POST /api/security/reality-audit/run`
- `GET /api/security/wsl/status`
- `GET /api/security/remote-access/status`
- `GET /api/security/startup/status`
- `GET /api/security/powershell/activity`
- `GET /api/security/vendor-services/classify`


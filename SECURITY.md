# HyperBoostX Security

Target release: `HyperBoostX v1.3.0 Stable`

## Local Backend

The backend is intended for local runtime use on `127.0.0.1` only. CORS is limited to localhost origins.

When the packaged launcher starts the backend, it generates a random in-memory session token and passes it to the backend and WPF client through `HYPERBOOSTX_SESSION_TOKEN`. Mutating endpoints require the matching header:

```http
X-HyperBoostX-Session: <token>
```

The token must not be logged, persisted, committed, packaged as plaintext, or included in reports.

## Credential Storage

NVIDIA API keys and Discord webhook URLs must be stored through Windows Credential Manager. They must not be written to plaintext config, logs, reports, crash dumps, release packages, installer artifacts, or repository files.

Known Credential Manager targets:

- `HyperBoostX:NVIDIA:ApiKey`
- `HyperBoostX:Discord:WebhookUrl`
- `HyperBoostX:Discord:UpdateWebhookUrl`

## Safety Guard

Safety Guard blocks or downgrades destructive actions including:

- forced Defender disablement
- permanent Windows Update disablement
- GPU driver service disablement without an explicit safe rollback path
- BIOS/UEFI edits
- voltage changes
- overclocking
- undervolting
- deleting system files
- deleting user data
- irreversible registry edits without restore metadata
- arbitrary AI-generated shell actions

AI and automation must generate a plan first, explain risk, require user approval, and preserve undo/restore metadata where applicable.

## Secret Scan Policy

Before release, scan tracked source and release assets for plaintext secrets. Do not ship if any real API key, Discord webhook, GitHub token, NVIDIA/AMD/Intel credential, bearer token, local session token, or machine-specific secret is found.

## Known Security Limitations

- Local token enforcement is active when the launcher supplies `HYPERBOOSTX_SESSION_TOKEN`. Developer-mode backend sessions without this environment variable remain compatible with existing local tests.
- HyperBoostX is not a sandbox and should not be used to run untrusted scripts.

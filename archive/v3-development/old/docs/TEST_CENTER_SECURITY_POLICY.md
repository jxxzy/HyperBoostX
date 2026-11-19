# Test Center Security Policy v2.10

Status: PASS_SOURCE_POLICY

- Known suite IDs only.
- No arbitrary PowerShell or shell command input from UI.
- Mutating routes remain protected by local session token middleware.
- Discord send is opt-in and dry-run by default.
- Action History stores redacted local evidence only.

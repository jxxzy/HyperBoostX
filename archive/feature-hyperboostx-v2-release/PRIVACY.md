# Privacy

HyperBoostX v2.0.0 is local-first.

- Backend runs on `127.0.0.1`.
- Reports, settings, profiles, sessions, backups, logs, and diagnostics are stored locally under `%LocalAppData%\HyperBoost X` by default.
- Portable mode can use `HYPERBOOSTX_PORTABLE_HOME` to keep config beside a portable build.
- Anonymous Usage and telemetry are off by default.
- Crash reports are local manual exports and are not uploaded automatically.
- Redaction removes tokens, API keys, Discord webhooks, bearer tokens, usernames, user profile paths, sensitive local paths, and future license keys from exported diagnostics.

Do not share exported logs or reports publicly until you have reviewed them.

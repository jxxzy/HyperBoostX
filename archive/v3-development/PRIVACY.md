# HyperBoostX v3 privacy

Core functionality is local. This build does not enable cloud sync, AI requests, analytics, crash uploads, Discord messages or automatic update downloads.

Diagnostic scans read Windows information such as hardware names, process names, installed apps, startup commands, network addresses and event metadata. Scan reports and action records remain under the current user's local application data. Reports and recovery records are encrypted with Windows DPAPI. Preferences do not contain credentials.

The application does not start camera or microphone capture. Ping sends ICMP packets only when the user requests it. Windows tools opened from the application have their own behavior and privacy policies.

Exported reports mask common user-profile paths, IPv4 addresses and token-like strings. Redaction is best-effort: device names, app names or IPv6 addresses may remain identifying. Review reports before sharing. The support ZIP is deliberately restricted to app/OS version and action ID, status, and timestamps; raw startup commands, diagnostics and backups are excluded.

No upload is performed after export. The user chooses the output path. CSV values that might be interpreted as spreadsheet formulas are escaped.

The latest 100 scan reports are retained locally. Action/recovery history is retained until deliberately managed by the user; uninstall keeps it. Runtime logs rotate at 1 MiB with one previous file. Database backup is explicit. Keep recovery data if you still need rollback.

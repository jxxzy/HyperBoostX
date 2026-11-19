# Test Center Design v2.10

Status: IMPLEMENTED_SOURCE_LEVEL

The Owner Lab Test Center is an embedded WPF surface backed by `/api/test-center/*`. It exposes known suite IDs only: Owner Lab gate, Public Candidate gate, Full App Smoke Test, Full Feature Integrity Test, UI Action Map Test, Backend Endpoint Test, Safety Guard Runtime, Restore & Backup Runtime, Report Generation, Action History, LCD Guard, License Center, AI Universal, Installer Runtime, Reinstall/Uninstall QA, Hardware Matrix, Long Runtime Stability, Discord Webhook, Webhook Redaction, Webhook Rate Limit, No Legacy Label Scan, and UI Properness Gate.

No arbitrary command execution is exposed from the app.

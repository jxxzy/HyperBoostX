# Test strategy and evidence

`dotnet test` exercises actual engine state transitions, failure compensation, consent, replay/stale previews, protected targets, owner binding, DPAPI tamper, safe paths, CSV export, SQLite corruption recovery and an actual isolated HKCU Registry round trip. No test toggles the user's normal transparency, game capture, startup apps or active power plan.

CLI smoke scans exercise the diagnostic Centers on the current Windows machine and retain summary counts/provider-unavailability counts. UI Automation opens pages via search, captures a screenshot and verifies clean exit. A passing automation tree is not sufficient: screenshots are visually inspected, which revealed the initial rendering problem.

`scripts/installer-smoke.ps1` checks fresh silent install, same-version reinstall/repair, uninstall command quoting, installed UI, recovery CLI, silent uninstall and preservation of an unrelated file/application data. It refuses to overwrite an already registered v3 install.

Pending: genuine reboot interruption, process-kill recovery, multi-user privilege matrix, historical upgrade migrations, 24–72 hour soak, hardware matrix and complete accessibility/DPI matrix. Failure injection is not a substitute for those tests.

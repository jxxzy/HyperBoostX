# Broken Buttons Audit

Audit date: 2026-06-27

| Button/action area | Status | Evidence |
| --- | --- | --- |
| Sidebar navigation | PASS | Every sidebar key has a registered route. |
| MainWindow quick Smart Scan | PASS | UI/UX verifier checks quick action handlers. |
| MainWindow quick Safe Boost | PASS | UI/UX verifier checks quick action handlers. |
| MainWindow quick Restore | PASS | UI/UX verifier checks quick action handlers. |
| CyberPageChrome Preview/Apply/Undo/Export | PASS | Button handler verifier and .NET backend client tests. |
| Legacy functional page route buttons | PASS | Legacy catalog routes and shared page chrome wired. |
| Streaming/mic/webcam controls | PASS/PARTIAL | Surface restored; hardware-dependent controls need device lab. |

Current broken active buttons found by automation: none.

Manual installed WPF click-through remains required before a public stable tag.


# Performance / Stability Report

Audit date: 2026-06-27

## Live Backend Stability

| Check | Result |
| --- | --- |
| Backend start on alternate port | PASS |
| Health ready within retry loop | PASS |
| 50 repeated health requests | PASS, 0 failures |
| Health check total time | 0.13s |
| Backend stopped after smoke | PASS |
| Orphan HyperBoost process after smoke | None found |

## Build/Test Stability

| Check | Result |
| --- | --- |
| Python pytest | PASS, 60 tests |
| .NET Release tests | PASS, 35 tests |
| .NET Release build | PASS |
| UI/UX static gate | PASS |
| Real usability gate | PASS |

## Limitation

The requested 5x visible app launch/close and 5-10 minute idle test were not fully performed in this non-interactive pass. Existing portable smoke evidence in `QA_RESULTS.md` plus live backend smoke covers core lifecycle, but installed WPF idle lab remains manual.


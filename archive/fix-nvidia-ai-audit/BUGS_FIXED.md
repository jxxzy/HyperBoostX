# Bugs Fixed

## Summary

Fixed bugs in this pass: `4`

| Bug ID | Severity | Status |
| --- | --- | --- |
| BUG-HBX-001 | Major | Fixed in source |
| BUG-HBX-002 | Medium | Fixed in source |
| BUG-HBX-003 | Low | Fixed in source |
| BUG-HBX-004 | Low | Fixed in source |

## Critical Fixes

No new Critical bugs were reproduced in this pass.

## Major Fixes

- Added restore metadata for booster profile registry and power-plan mutations.

## Medium Fixes

- Added the missing strict allowlist entry for the built-in battery display timeout action.

## Low Fixes

- Removed stale AI provider wording from docs.
- Removed a local absolute path from README.

## Validation

Initial targeted validation:

- `app\venv\Scripts\python.exe -m pytest tests/test_booster_service.py tests/test_shell_util.py -q` -> `14 passed`
- `dotnet test dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj --filter NvidiaCopilotServiceTests` -> `6 passed`

Full validation is tracked in `QA_RESULTS.md`.

Final automated validation snapshot:

- `powershell -ExecutionPolicy Bypass -File .\scripts\verify_repo.ps1` -> PASS
- `app\venv\Scripts\python.exe -m pytest` -> `40 passed, 1 warning`
- `dotnet restore` -> PASS
- `dotnet build` -> PASS
- `dotnet build -c Release` -> PASS
- `dotnet test` -> `20 passed`
- Build scripts and installer build -> PASS
- Packaged backend, portable app, and installed app smoke -> PASS

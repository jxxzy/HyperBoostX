# Bugs Fixed

## Summary

Fixed bugs in this pass: `6`

| Bug ID | Severity | Status |
| --- | --- | --- |
| BUG-HBX-001 | Major | Fixed in source |
| BUG-HBX-002 | Medium | Fixed in source |
| BUG-HBX-003 | Low | Fixed in source |
| BUG-HBX-004 | Low | Fixed in source |
| BUG-HBX-005 | Medium | Fixed in source |
| BUG-HBX-006 | Low | Fixed in source |

## Critical Fixes

No new Critical bugs were reproduced in this pass.

## Major Fixes

- Added restore metadata for booster profile registry and power-plan mutations.

## Medium Fixes

- Added the missing strict allowlist entry for the built-in battery display timeout action.
- Completed NVIDIA Copilot Settings control/status contract and exposed `AiSecretRedactor`.

## Low Fixes

- Removed stale AI provider wording from docs.
- Removed a local absolute path from README.
- Fixed the Python test warning before HyperBoostX v1.2.14 stable release by filtering the GPUtil dependency warning only around the optional GPUtil import.

## Validation

Initial targeted validation:

- `app\venv\Scripts\python.exe -m pytest tests/test_booster_service.py tests/test_shell_util.py -q` -> `14 passed`
- `dotnet test dotnet-tests\HyperBoostX.Tests\HyperBoostX.Tests.csproj --filter "NvidiaCopilotServiceTests"` -> `15 passed`

Full validation is tracked in `QA_RESULTS.md`.

Final automated validation snapshot:

- `powershell -ExecutionPolicy Bypass -File .\scripts\verify_repo.ps1` -> PASS
- `app\venv\Scripts\python.exe -m pytest -ra -W default` -> `26 passed, 0 warnings`
- `dotnet restore` -> PASS
- `dotnet build` -> PASS
- `dotnet build -c Release` -> PASS
- `dotnet test` -> `28 passed`
- Build scripts and installer build -> PASS
- Packaged backend health and portable app smoke -> PASS
- Elevated silent installer install/uninstall/reinstall -> PASS
- Installed app smoke -> PASS
- Real NVIDIA API connection from Windows Credential Manager -> PASS
- AI approval flow and Safety Guard release gate tests -> PASS
- Current-machine automated matrix -> PASS
- Full multi-machine Windows lab matrix -> NOT CLAIMED
- Final statement: Zero known Critical/Major bugs after automated validation.


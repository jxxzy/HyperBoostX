# Hardware Matrix v2.10.0-beta.1

Generated: 2026-07-01 03:36 +07:00

| Scenario | Status | Notes |
| --- | --- | --- |
| Low-end PC profile | NOT_TESTED | Requires owner hardware lab |
| NVIDIA GPU | NOT_TESTED | Source detection routes exist |
| AMD GPU | NOT_TESTED | Source detection routes exist |
| Intel GPU | NOT_TESTED | Source detection routes exist |
| No GPU detected | NOT_TESTED | Needs hardware/VM validation |
| MSI Center present | NOT_TESTED | MSI Safe Optimizer diagnostics exist |
| KANALI/TRCC/HiMOS LCD stack | NOT_TESTED | LCD role detector unit-tested with fake processes |
| No admin | PARTIAL | Current shell is non-admin; source/package gates passed |
| Admin mode | NOT_TESTED | Requires elevated owner lab |
| Backend offline | PASS_SOURCE | UI/backend friendly error contracts tested |
| Token mismatch | PASS_SOURCE | Route contract includes unauthorized local session test |
| Corrupt config | PASS_SOURCE | Existing config tests cover corrupt JSON behavior |
| Missing reports folder | PASS_SOURCE | Reports folder creation covered by source behavior |
| Windows scaling 100/125/150 | NOT_TESTED | Requires installed WPF screenshot pass |
| Empty game library | PASS_SOURCE | Route contract covers empty library flows |
| Many startup items | NOT_TESTED | Needs owner data set |
| Protected process action blocked | PASS_SOURCE | Safety guard tests cover blocked process/LCD/vendor actions |

Public stable is blocked until the NOT_TESTED rows are resolved or explicitly accepted by owner as known limitations.


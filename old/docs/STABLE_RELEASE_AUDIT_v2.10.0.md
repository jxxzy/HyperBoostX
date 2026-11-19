# Owner Lab Release Gate Audit v2.10.0

Generated: 2026-07-03 21.56.56 +07:00
Expected version: 2.10.0
Final status: OWNER_LAB_PASS

| Gate | Status | Duration |
| --- | --- | ---: |
| version consistency | PASS | 685 ms |
| UI release gate | PASS | 9374 ms |
| no template UI regression | PASS | 346 ms |
| UI page body markers | PASS | 221 ms |
| UI button labels | PASS | 296 ms |
| no placement notes | PASS | 197 ms |
| dense page content | PASS | 218 ms |
| page action rows | PASS | 235 ms |
| right log/evidence panel | PASS | 242 ms |
| button state machine | PASS | 483 ms |
| report/restore schema | PASS | 333 ms |
| AI Assistant contract | PASS | 2335 ms |
| AI universal layer | PASS | 764 ms |
| AI 10-model registry | PASS | 194 ms |
| AI model router | PASS | 189 ms |
| AI redaction | PASS | 189 ms |
| AI action mapper | PASS | 285 ms |
| AI no auto apply | PASS | 275 ms |
| AI feature panels | PASS | 214 ms |
| Expert mode surface | PASS | 358 ms |
| feature maturity labels | PASS | 475 ms |
| no gimmick features | PASS | 290 ms |
| Expert pages not dead | PASS | 281 ms |
| P0 competitor-gap modules | PASS | 225 ms |
| release docs consistency | PASS | 2327 ms |
| installer runtime evidence | PASS | 1820 ms |

Meaning:
- OWNER_LAB_PASS: all automated source, docs, UI, and installed-runtime evidence gates passed; public stable promotion still needs explicit owner approval.
- OWNER_LAB_PARTIAL: source/docs/UI passed but one or more owner/install evidence gates were not current or not run.
- OWNER_LAB_BLOCKED: at least one release blocker gate failed.

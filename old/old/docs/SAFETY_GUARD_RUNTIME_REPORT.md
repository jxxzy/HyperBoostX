# Safety Guard Runtime

Suite: `safety_guard_runtime`
Status: `pass`
Finished: `2026-07-03T14:46:28.253186+00:00`

Message: Safety Guard runtime policy blocks risky classes.

```json
{
    "test_run_id":  "tc_0c00fdb2d1",
    "suite_id":  "safety_guard_runtime",
    "suite_name":  "Safety Guard Runtime",
    "status":  "pass",
    "ok":  true,
    "message":  "Safety Guard runtime policy blocks risky classes.",
    "evidence":  {
                     "blocked_examples":  [
                                              "disable defender",
                                              "kill anti cheat",
                                              "install unsigned plugin",
                                              "run raw powershell"
                                          ],
                     "mutating_requires_preview":  true
                 },
    "started_at":  "2026-07-03T14:46:28.253168+00:00",
    "finished_at":  "2026-07-03T14:46:28.253186+00:00",
    "duration_ms":  0
}
```

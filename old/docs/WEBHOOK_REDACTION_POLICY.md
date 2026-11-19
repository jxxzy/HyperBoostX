# Webhook Redaction

Suite: `webhook_redaction`
Status: `pass`
Finished: `2026-07-03T14:57:12.960508+00:00`

Message: Webhook redaction removes secrets and local user names.

```json
{
    "test_run_id":  "tc_9f902d3ea9",
    "suite_id":  "webhook_redaction",
    "suite_name":  "Webhook Redaction",
    "status":  "pass",
    "ok":  true,
    "message":  "Webhook redaction removes secrets and local user names.",
    "evidence":  {
                     "record":  {
                                    "id":  "ah_e9600487dd23",
                                    "timestamp":  "2026-07-03T14:57:12.958956+00:00",
                                    "test_run_id":  "",
                                    "suite_id":  "",
                                    "test_id":  "",
                                    "feature":  "Test Center",
                                    "menu":  "Owner Lab",
                                    "action_id":  "",
                                    "endpoint":  "",
                                    "action":  "Webhook redaction probe",
                                    "status":  "success",
                                    "severity":  "info",
                                    "started_at":  "2026-07-03T14:57:12.958956+00:00",
                                    "finished_at":  "2026-07-03T14:57:12.958956+00:00",
                                    "duration_ms":  0,
                                    "error":  "",
                                    "discord_status":  "",
                                    "report_path":  "",
                                    "restore_status":  "",
                                    "safety_status":  "",
                                    "details":  {
                                                    "sample":  "secret https://discord.com/api/webhooks/[redacted] token=[redacted] C:\\Users\\[redacted-user]\\file.txt"
                                                }
                                }
                 },
    "started_at":  "2026-07-03T14:57:12.951788+00:00",
    "finished_at":  "2026-07-03T14:57:12.960508+00:00",
    "duration_ms":  8
}
```

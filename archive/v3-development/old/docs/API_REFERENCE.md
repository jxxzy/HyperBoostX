# HyperBoostX v2.10.0 Stable API Reference

Base URL: `http://127.0.0.1:5000`

The backend is local-only. Mutating endpoints require `X-HyperBoostX-Session` when `HYPERBOOSTX_SESSION_TOKEN` is present. All reports, logs, crash exports, and action logs apply privacy redaction for tokens, usernames, and sensitive local paths.

This document describes the current v2.10.0 stable unsigned backend contract. Existing v2 route shapes remain supported; v2.1 compatibility aliases are exposed for the restored WPF sidebar and future clients. Preview/read-only/blocked responses are intentional safety states, not release claims for destructive automation.

## Standard v2.1 Envelope

Compatibility aliases return this envelope shape:

```json
{
  "ok": true,
  "module": "boost",
  "action": "preview",
  "status": "preview",
  "message": "Preview generated. No system change has been applied.",
  "data": {},
  "warnings": [],
  "blocked_reasons": [],
  "restore_available": true,
  "restore_session_id": null,
  "report_id": null
}
```

`status` can be `success`, `preview`, `partial`, `blocked`, or `error`. `blocked` means Safety Guard intentionally refused a risky action.

## Health

`GET /api/health`

```json
{"status":"ok","version":"2.10.0","local_only":true,"session_token_required":false}
```

`GET /api/version`

```json
{"name":"HyperBoostX","version":"2.10.0","release":"HyperBoostX v2.10.0 Stable","channel":"Stable","stable":true}
```

## Existing Core APIs

- `GET /api/system/stats`
- `GET /api/system/info`
- `GET /api/system/startup`
- `GET /api/system/processes`
- `GET /api/hardware/profile`
- `GET /api/hardware/gpu`
- `GET /api/hardware/vendors`
- `GET /api/hardware/overlays`
- `POST /api/boost/plan` - session token required when enabled.
- `POST /api/boost/apply` - session token and user approval required.
- `POST /api/boost/undo` - session token required when enabled.
- `GET /api/reports/latest`
- `POST /api/reports/export` - exports JSON/TXT/Markdown.
- `POST /api/reports/crash-export` - privacy redaction enabled.
- `POST /api/jobs/start`
- `GET /api/jobs/{id}`
- `POST /api/jobs/{id}/cancel`

## v2.1 Compatibility Aliases

These routes exist to remove client/docs drift and give every restored WPF sidebar page a safe backend target. Mutating aliases are preview/approval/blocked by design unless a supported safe handler exists.

- Core: `GET /api/status`, `GET|POST /api/settings`
- Dashboard: `GET /api/dashboard/summary`, `GET /api/dashboard/score`, `GET /api/dashboard/alerts`, `GET /api/dashboard/activity`
- Scan: `POST /api/scan/system`, `POST /api/scan/quick`, `POST /api/scan/full`, `POST /api/scan/smart`
- Boost: `POST /api/boost/preview`, `GET /api/boost/last-result`, `GET /api/boost/history`
- Performance: `GET /api/performance/summary`, `POST /api/performance/plan`, `POST /api/performance/apply`
- Processes: `GET /api/processes`, `GET /api/processes/summary`, `POST /api/processes/preview-close`, `POST /api/processes/close-selected`
- Storage/Cleanup: `GET /api/storage/drives`, `POST /api/storage/scan`, `POST /api/storage/analyze`, `POST /api/storage/cleanup-preview`, `GET /api/cleanup/history`
- Gaming: `GET /api/gaming/detect`, `GET /api/gaming/profiles`, `POST /api/gaming/profile/apply`, `POST /api/gaming/profile/restore`, `POST /api/gaming/overlay/scan`, `POST /api/gaming/boost/preview`, `POST /api/gaming/boost/apply`
- GPU/Network/Security: `GET /api/gpu/info`, `GET /api/gpu/health`, `GET /api/network/status`, `POST /api/network/ping-test`, `POST /api/network/dns-preview`, `POST /api/network/dns-apply`, `POST /api/network/reset-preview`, `GET /api/security/health`
- Apps/Windows/Repair: `POST /api/apps/uninstall`, `POST /api/windows/features/apply`, `POST /api/windows/services/apply`, `POST /api/repair/sfc-preview`, `POST /api/repair/dism-preview`, `POST /api/repair/sfc-run`, `POST /api/repair/dism-run`
- Restore/Automation/AI/Audit: `POST /api/restore/create`, `POST /api/restore/undo-last`, `POST /api/automation/create`, `POST /api/automation/dry-run`, `POST /api/automation/enable`, `POST /api/automation/disable`, `POST /api/automation/delete`, `GET /api/ai/status`, `POST /api/ai/ask`, `POST /api/ai/plan`, `POST /api/ai/approve`, `POST /api/ai/reject`, `GET /api/audit/features`, `POST /api/audit/run`, `GET /api/audit/report`
- Update/Reports/Logs: `POST /api/update/download-preview`, `POST /api/update/download`, `POST /api/update/install-preview`, `GET /api/reports`, `GET /api/reports/{report_id}`, `GET /api/logs/recent`, `POST /api/logs/export`

## AI Advisor And Knowledge Base

- `GET /api/advisor/performance`
- `POST /api/advisor/performance`
- `GET /api/knowledge/terms`
- `GET /api/knowledge/terms/{term}`
- `GET /api/score/engine`

Advisor output:

```json
{
  "title": "HyperBoost AI Performance Advisor",
  "diagnosis_mode": "local_deterministic_advisor",
  "analysis": [{"type":"gpu_bottleneck","severity":"high","message":"GPU is saturated while CPU headroom remains."}],
  "safe_plan": [{"action_id":"capture_before_after_report","requires_approval":false,"risk":"low"}],
  "blocked_or_risky_actions": ["Disable Defender", "Permanent Windows Update disable"],
  "requires_user_approval": true
}
```

## Games And Profiles

- `GET /api/games/library`
- `GET /api/games/running`
- `POST /api/games/scan`
- `POST /api/games/add`
- `POST /api/games/remove`
- `POST /api/games/profile/preview`
- `POST /api/games/profile/apply`
- `POST /api/games/profile/restore`
- `GET /api/games/session/latest`
- `GET /api/games/session/history`
- `POST /api/games/session/export`

Mutating profile endpoints require session token when enabled. `profile/apply` also requires `user_approved: true`.

## Overlay And Protection

- `GET /api/overlays/status`
- `GET /api/overlays/recommendations`
- `GET /api/protection/processes`
- `POST /api/protection/add`
- `POST /api/protection/remove`
- `POST /api/protection/reset-defaults`
- `POST /api/protection/evaluate-action`

Blocked action example:

```json
{"allowed":false,"blocked":true,"reason":"Safety Guard blocked dangerous/protected action.","requires_approval":true}
```

## Process, Benchmark, GPU, Driver

- `GET /api/processes/heavy`
- `GET /api/processes/startup-impact`
- `GET /api/processes/recommendations`
- `POST /api/processes/export-report`
- `POST /api/benchmark/manual`
- `POST /api/benchmark/import-csv`
- `GET /api/benchmark/latest`
- `GET /api/benchmark/history`
- `POST /api/benchmark/export`
- `GET /api/gpu/vendor-guide`
- `GET /api/gpu/recommendations`
- `POST /api/gpu/export-report`
- `GET /api/gpu/hardware-database`
- `GET /api/drivers/recommendation`

Driver endpoint safety note: it returns local current-driver data and official-source guidance only. It does not fabricate latest stable driver numbers and does not auto-download drivers.

## Startup, Cleanup, Network

- `GET /api/startup/list` - legacy client alias.
- `GET /api/startup/items`
- `POST /api/startup/preview`
- `POST /api/startup/apply`
- `POST /api/startup/restore`
- `POST /api/startup/export-report`
- `GET /api/cleanup/scan`
- `POST /api/cleanup/preview`
- `POST /api/cleanup/apply`
- `GET /api/cleanup/report`
- `POST /api/cleanup/export-report`
- `GET /api/network/diagnostics`
- `POST /api/network/ping`
- `GET /api/network/dns-test`
- `POST /api/network/flush-dns`
- `POST /api/network/export-report`

Cleanup apply is conservative in v1.4 and does not perform broad destructive deletion. Network mutation may return `admin_required` when Windows requires elevation.

## Essentials, Streaming, Settings, Restore

- `GET /api/essentials/list`
- `GET /api/essentials/check`
- `POST /api/essentials/install-preview`
- `POST /api/essentials/install`
- `GET /api/streaming/status`
- `GET /api/rgb/status`
- `GET /api/plugins/registry`
- `GET /api/settings/ui`
- `POST /api/settings/ui`
- `GET /api/restore/sessions`
- `GET /api/restore/session/{id}`
- `POST /api/restore/session/{id}/preview`
- `POST /api/restore/session/{id}/apply`
- `GET /api/restore/session/{id}/verify`
- `POST /api/restore/export`

`/api/essentials/install` is manual-only in v1.4 and returns a blocked/manual response instead of running silent installers.

## Product Foundation

- `GET /api/history/scans`
- `POST /api/history/scans`
- `GET /api/history/timeline`
- `GET /api/product/storage`
- `GET /api/product/action-log`
- `GET /api/product/v2-roadmap`
- `GET|POST /api/feature-audit/run`

Feature Audit is read-only and does not run destructive actions.

# HyperBoostX v1.4.0 API Reference

Base URL: `http://127.0.0.1:5000`

The backend is local-only. Mutating endpoints require `X-HyperBoostX-Session` when `HYPERBOOSTX_SESSION_TOKEN` is present. All reports, logs, crash exports, and action logs apply privacy redaction for tokens, usernames, and sensitive local paths.

## Health

`GET /api/health`

```json
{"status":"ok","version":"1.4.0","local_only":true,"session_token_required":false}
```

`GET /api/version`

```json
{"name":"HyperBoostX","version":"1.4.0","release":"HyperBoostX v1.4.0 Stable"}
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

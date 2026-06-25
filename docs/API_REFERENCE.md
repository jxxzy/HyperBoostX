# HyperBoostX v1.3.0 API Reference

Base URL: `http://127.0.0.1:5000`

The backend is local-only. Mutating endpoints require `X-HyperBoostX-Session` when the launcher supplies `HYPERBOOSTX_SESSION_TOKEN` to the backend process.

## Health

### GET /api/health

Returns backend readiness and version.

```json
{
  "status": "ok",
  "version": "1.3.0",
  "service": "HyperBoostX Backend",
  "local_only": true,
  "session_token_required": true
}
```

### GET /api/version

```json
{
  "name": "HyperBoostX",
  "version": "1.3.0",
  "release": "HyperBoostX v1.3.0 Stable"
}
```

## System

- `GET /api/system/stats`
- `GET /api/system/info`
- `GET /api/system/startup`
- `GET /api/system/processes`

`/api/system/stats` returns CPU, RAM, disk, network, process count, temperatures when available, and GPU counters when available.

`/api/system/info` returns identity, CPU, memory, disk, system drive, device profile, network, OS, BIOS, GPU, and temperature data.

## Hardware And GPU Center

### GET /api/hardware/gpu

Returns the active GPU summary plus all detected adapters.

Important fields:

- `vendor`: `Nvidia`, `Amd`, `Intel`, `MicrosoftBasic`, or `Unknown`
- `model`
- `family`
- `active_display_gpu`
- `driver_version`
- `vram_total_mb`
- `vram_used_mb`
- `vram_usage_percent`
- `gpu_usage_percent`
- `temperature_c`
- `dedicated_gpu`
- `integrated_gpu`
- `hybrid_gpu_system`
- `multi_gpu_system`
- `badge`
- `profile_recommendation`
- `safe_actions`
- `skipped_actions`
- `blocked_risky_actions`

### GET /api/hardware/vendors

Returns detected vendor, RGB, launcher, and streaming software classifications.

Classifications:

- `Safe to keep`
- `Can pause while gaming`
- `Heavy background service`
- `Needs user decision`
- `Do not disable`
- `Unknown, analyze manually`

### GET /api/hardware/overlays

Returns overlay detections such as NVIDIA Overlay, Radeon Overlay, Intel Arc Overlay, Discord Overlay, Steam Overlay, Xbox Game Bar, and RTSS.

### GET /api/hardware/profile

Returns the hardware profile recommendation.

```json
{
  "recommended_profile": "High-End AMD Radeon PC",
  "confidence": 0.91,
  "reason": ["AMD Radeon RX detected", "32GB RAM detected"],
  "scores": {
    "pc_health": 87,
    "gaming_readiness": 92,
    "streaming_readiness": 84,
    "startup_cleanliness": 76
  },
  "safe_actions": [],
  "requires_approval": [],
  "risky_actions_blocked": [],
  "undo_available": true
}
```

## Boost

### POST /api/boost/plan

Creates a safe action plan. The plan does not execute risky actions.

Request:

```json
{
  "goal": "gaming",
  "mode": "balanced"
}
```

### POST /api/boost/apply

Applies only approved safe actions.

Request:

```json
{
  "user_approved": true,
  "approved_action_ids": []
}
```

Without approval, the endpoint returns `409` with `requires_approval: true`.

### POST /api/boost/undo

Returns undo/restore metadata status for the latest boost flow.

## Reports

### GET /api/reports/latest

Returns the latest before/after report, generating one if none exists.

### POST /api/reports/export

Request:

```json
{
  "format": "json"
}
```

Supported formats: `json`, `txt`, `md`.

### POST /api/reports/crash-export

Creates a local-only crash report payload with privacy redaction. HyperBoostX does not upload crash reports automatically.

Request:

```json
{
  "format": "json",
  "error_message": "Optional error message",
  "stack_trace": "Optional stack trace",
  "last_action": "Optional last user action",
  "backend_status": "Optional backend status"
}
```

The report includes app version, Windows version, CPU, RAM, GPU vendor/model, error message, stack trace, last action, backend status, and timestamp. API keys, AI keys, tokens, GitHub tokens, usernames, sensitive local paths, and future license keys are redacted.

## Jobs

### POST /api/jobs/start

Starts a local long-running job.

```json
{
  "job_type": "cleanup"
}
```

Response:

```json
{
  "job_id": "cleanup_12345678",
  "status": "running",
  "progress": 42,
  "stage": "Cleaning temporary files",
  "can_cancel": true,
  "started_at": "2026-06-26T00:00:00+00:00",
  "finished_at": null,
  "logs": []
}
```

### GET /api/jobs/{id}

Returns job status.

### POST /api/jobs/{id}/cancel

Requests cancellation when `can_cancel` is true.

## Legacy-Compatible Endpoints

Existing endpoints remain available for WPF compatibility:

- `GET /api/booster/profiles`
- `POST /api/booster/apply`
- `GET /api/startup/list`
- `GET /api/tweaks/list`
- `POST /api/tweaks/apply`
- `POST /api/tweaks/revert`
- `GET /api/drivers/list`
- `POST /api/drivers/check-updates`
- `POST /api/repair/run-sfc`
- `POST /api/repair/run-dism`
- `POST /api/repair/cleanup`
- `POST /api/repair/reset-network`
- `GET /api/network/dns-test`
- `POST /api/network/flush-dns`
- `POST /api/network/optimize-tcp`

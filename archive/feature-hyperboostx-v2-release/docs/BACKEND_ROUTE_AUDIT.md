# Backend Route Audit

Date: 2026-06-27  
Status: PASS for source route contract.

## Route Coverage

The runtime contract test now covers health/version, system telemetry, hardware/GPU, scan/advisor, boost plan/apply/undo, game/profile, protection, process, benchmark, history/report, startup, cleanup, network, streaming, creator, restore/recovery, knowledge base, feature audit, update, webhook/NVIDIA credential gates, and restored v1.3 parity routes.

New v1.3 parity namespaces:

- `/api/storage/status`
- `/api/privacy/status`, `/api/privacy/preview`, `/api/privacy/apply`
- `/api/security/status`
- `/api/apps/list`, `/api/apps/impact`, `/api/apps/uninstall-preview`
- `/api/system-config/tweaks`, `/api/system-config/tweaks/preview`
- `/api/windows/features`, `/api/windows/features/preview`
- `/api/windows/services`, `/api/windows/services/preview`
- `/api/update-control/status`, `/api/update-control/preview`
- `/api/repair/status`, `/api/repair/preview`
- `/api/power/status`, `/api/power/preview`
- `/api/visual-effects/status`, `/api/visual-effects/preview`
- `/api/restore-points/status`, `/api/restore-points/preview`
- `/api/automation/rules`, `/api/automation/preview`
- `/api/utilities/status`
- `/api/master-test/status`, `/api/master-test/run`
- `/api/feature-audit/matrix`
- `/api/camera-tracking/status`, `/api/camera-tracking/preview`

New v2.1 compatibility contract namespaces:

- `/api/status`, `/api/settings`
- `/api/dashboard/summary`, `/api/dashboard/score`, `/api/dashboard/alerts`, `/api/dashboard/activity`
- `/api/scan/system`, `/api/scan/quick`, `/api/scan/full`
- `/api/boost/preview`, `/api/boost/last-result`, `/api/boost/history`
- `/api/performance/summary`, `/api/performance/plan`, `/api/performance/apply`
- `/api/startup/summary`
- `/api/processes`, `/api/processes/summary`, `/api/processes/preview-close`, `/api/processes/close-selected`
- `/api/cleanup/history`
- `/api/storage/drives`, `/api/storage/scan`, `/api/storage/analyze`, `/api/storage/cleanup-preview`
- `/api/gaming/detect`, `/api/gaming/profiles`, `/api/gaming/profile/apply`, `/api/gaming/profile/restore`, `/api/gaming/overlay/scan`, `/api/gaming/boost/preview`, `/api/gaming/boost/apply`
- `/api/gpu/info`, `/api/gpu/health`
- `/api/network/status`, `/api/network/ping-test`, `/api/network/dns-preview`, `/api/network/dns-apply`, `/api/network/reset-preview`
- `/api/security/health`, `/api/apps/uninstall`, `/api/windows/features/apply`, `/api/windows/services/apply`
- `/api/repair/sfc-preview`, `/api/repair/sfc-run`, `/api/repair/dism-preview`, `/api/repair/dism-run`
- `/api/restore/create`, `/api/restore/undo-last`
- `/api/automation/create`, `/api/automation/dry-run`, `/api/automation/enable`, `/api/automation/disable`, `/api/automation/delete`
- `/api/ai/status`, `/api/ai/ask`, `/api/ai/plan`, `/api/ai/approve`, `/api/ai/reject`
- `/api/audit/features`, `/api/audit/run`, `/api/audit/report`
- `/api/update/download-preview`, `/api/update/download`, `/api/update/install-preview`
- `/api/reports`, `/api/reports/<id>`, `/api/logs/recent`, `/api/logs/export`

The v2.1 compatibility aliases return a standard JSON envelope with `ok`, `module`, `action`, `status`, `message`, `data`, `warnings`, `blocked_reasons`, `restore_available`, `restore_session_id`, and `report_id`. Risky apply-style aliases intentionally return `blocked` until preview, approval, restore metadata, and report support exist for that exact Windows action.

## Safety Notes

- Risky endpoints are status/preview/guard endpoints unless explicit safe apply support exists.
- Legacy `/api/tweaks/apply` now requires explicit confirmation for mutating tweaks; `disable_defender` and `disable_updates` are absolute-blocked before any restore point or registry action can run.
- Backend does not execute arbitrary shell commands from AI or Master Test Engine.
- Credential-dependent webhook/NVIDIA endpoints return structured credential-required responses.
- Driver install, app uninstall, service changes, update control, power plan, repair tools, restore points, and visual effects remain approval/admin-gated.

## Verification

- `app\\venv\\Scripts\\python.exe -m pytest -q tests\\test_runtime_route_contract.py`: `8 passed` after v1.3, v2.1, and System Reality Guard route additions.


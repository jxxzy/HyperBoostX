"""Safe-real v2.10 feature endpoints.

These routes make every v2.10 UI action land on a real local handler. Risky
operations return Safety Guard blocks with evidence instead of pretending to
apply unsupported changes.
"""

from __future__ import annotations

import ctypes
import hashlib
import json
import os
import platform
import socket
import subprocess
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional

import psutil
from flask import Blueprint, jsonify, request

from api.middleware import handle_errors, log_requests
from services.feature_registry import FeatureRegistryService
from services.monitoring.gpu_detection_service import GpuDetectionService
from services.monitoring.report_service import ReportService
from services.optimization.network_service import NetworkService
from services.optimization.startup_service import StartupService
from services.product_features import (
    CleanupCenterService,
    DriverRecommendationService,
    EnterpriseLogService,
    LocalJsonStore,
    NetworkToolsFacade,
    ProcessAnalyzerService,
    ProtectionService,
    RestoreService,
)

real_features_v210_bp = Blueprint("real_features_v210", __name__, url_prefix="/api")


def _utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def _payload() -> Dict[str, Any]:
    return request.get_json(silent=True) or {}


def _is_windows() -> bool:
    return platform.system().lower() == "windows"


def _is_admin() -> bool:
    if not _is_windows():
        return False
    try:
        return bool(ctypes.windll.shell32.IsUserAnAdmin())
    except Exception:
        return False


def _response(
    *,
    ok: bool = True,
    status: str = "success",
    message: str = "Action completed.",
    data: Optional[Dict[str, Any]] = None,
    warnings: Optional[Iterable[str]] = None,
    blocked_reasons: Optional[Iterable[str]] = None,
    requires_admin: bool = False,
    restore_metadata_id: Optional[str] = None,
    report_id: Optional[str] = None,
    job_id: Optional[str] = None,
    safe_alternative: Optional[str] = None,
    http_status: int = 200,
):
    blocked = status == "blocked" or not ok
    payload = {
        "ok": bool(ok) and not blocked,
        "status": status,
        "message": message,
        "data": data or {},
        "warnings": list(warnings or []),
        "blocked": blocked,
        "blocked_reasons": list(blocked_reasons or []),
        "requires_admin": requires_admin,
        "restore_metadata_id": restore_metadata_id,
        "report_id": report_id,
        "job_id": job_id,
    }
    if safe_alternative:
        payload["safe_alternative"] = safe_alternative
    if blocked:
        payload["error_code"] = "SAFETY_GUARD_BLOCKED"
    return jsonify(payload), http_status


def _blocked(reason: str, *, data: Optional[Dict[str, Any]] = None, requires_admin: bool = False, safe_alternative: Optional[str] = None):
    return _response(
        ok=False,
        status="blocked",
        message=reason,
        data=data or {},
        blocked_reasons=[reason],
        requires_admin=requires_admin,
        safe_alternative=safe_alternative or "Use the safe recommended action instead.",
    )


def _run(args: List[str], timeout: float = 8.0) -> Dict[str, Any]:
    try:
        completed = subprocess.run(
            args,
            text=True,
            capture_output=True,
            encoding="utf-8",
            errors="ignore",
            timeout=timeout,
            shell=False,
        )
        return {
            "exit_code": completed.returncode,
            "stdout": completed.stdout.strip(),
            "stderr": completed.stderr.strip(),
            "success": completed.returncode == 0,
        }
    except FileNotFoundError as exc:
        return {"exit_code": None, "stdout": "", "stderr": str(exc), "success": False, "unavailable": True}
    except subprocess.TimeoutExpired as exc:
        return {"exit_code": None, "stdout": exc.stdout or "", "stderr": "Command timed out.", "success": False, "timeout": True}


def _powershell(script: str, timeout: float = 10.0) -> Dict[str, Any]:
    return _run(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script], timeout=timeout)


def _load_list(key: str) -> List[Dict[str, Any]]:
    return LocalJsonStore.load(key, [], list)


def _save_list(key: str, items: List[Dict[str, Any]]) -> None:
    LocalJsonStore.save(key, items)


def _make_job(kind: str, command: Optional[List[str]] = None, result: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
    jobs = LocalJsonStore.load("diagnostics/v210_jobs.json", [], list)
    job = {
        "id": f"job_{uuid.uuid4().hex[:10]}",
        "kind": kind,
        "created_at": _utc_now(),
        "status": "completed",
        "progress": 100,
        "command": command or [],
        "result": result or {},
        "cancel_supported": False,
    }
    jobs.append(job)
    LocalJsonStore.save("diagnostics/v210_jobs.json", jobs[-200:])
    return job


def _find_job(job_id: str) -> Dict[str, Any]:
    for job in LocalJsonStore.load("diagnostics/v210_jobs.json", [], list):
        if job.get("id") == job_id:
            return job
    return {"id": job_id, "status": "not_found"}


def _official_vendor_links() -> List[Dict[str, str]]:
    return [
        {"vendor": "NVIDIA", "url": "https://www.nvidia.com/download/index.aspx"},
        {"vendor": "AMD", "url": "https://www.amd.com/en/support/download/drivers.html"},
        {"vendor": "Intel", "url": "https://www.intel.com/content/www/us/en/support/products/80939/graphics.html"},
    ]


@real_features_v210_bp.route("/scan/latest", methods=["GET"])
@handle_errors
def scan_latest():
    history = LocalJsonStore.load("performance_history", [], list)
    latest = history[-1] if history else None
    return _response(message="Latest scan loaded." if latest else "No scan history yet.", data={"latest_scan": latest, "available": latest is not None})


@real_features_v210_bp.route("/system/cpu", methods=["GET"])
@handle_errors
def system_cpu():
    return _response(data={
        "usage_percent": psutil.cpu_percent(interval=0.05),
        "logical_count": psutil.cpu_count(logical=True),
        "physical_count": psutil.cpu_count(logical=False),
        "frequency": getattr(psutil, "cpu_freq", lambda: None)()._asdict() if getattr(psutil, "cpu_freq", None) and psutil.cpu_freq() else None,
    })


@real_features_v210_bp.route("/system/ram", methods=["GET"])
@handle_errors
def system_ram():
    memory = psutil.virtual_memory()
    return _response(data={"total": memory.total, "available": memory.available, "used": memory.used, "percent": memory.percent})


@real_features_v210_bp.route("/system/processes", methods=["GET"])
@handle_errors
def system_processes():
    return _response(data=ProcessAnalyzerService.heavy(limit=50))


@real_features_v210_bp.route("/process/close-selected", methods=["POST"])
@handle_errors
@log_requests
def close_selected_processes():
    payload = _payload()
    if not bool(payload.get("user_approved")):
        return _blocked("Closing processes requires explicit approval.", data={"requested": payload})
    closed: List[Dict[str, Any]] = []
    blocked: List[Dict[str, Any]] = []
    for pid in payload.get("pids", []) or []:
        try:
            proc = psutil.Process(int(pid))
            name = proc.name()
            decision = ProtectionService.evaluate({"action": "close process", "target": name})
            if decision.get("blocked"):
                blocked.append({"pid": pid, "name": name, "reason": decision.get("reason")})
                continue
            proc.terminate()
            closed.append({"pid": pid, "name": name})
        except Exception as exc:
            blocked.append({"pid": pid, "reason": str(exc)})
    session = RestoreService.create_session("process_close_selected", {"closed": closed, "blocked": blocked})
    return _response(message="Selected non-protected process close request processed.", data={"closed": closed, "blocked": blocked}, restore_metadata_id=session["id"])


@real_features_v210_bp.route("/startup/disable", methods=["POST"])
@real_features_v210_bp.route("/startup/enable", methods=["POST"])
@handle_errors
@log_requests
def startup_toggle():
    payload = _payload()
    action = request.path.rsplit("/", 1)[-1]
    item_name = str(payload.get("name") or payload.get("item_name") or "").strip()
    if not item_name:
        return _blocked("Startup item name is required.", data={"items": StartupService().get_startup_items()[:20]})
    if not bool(payload.get("user_approved")):
        return _blocked(f"Startup {action} requires explicit approval.", data={"item_name": item_name})
    decision = ProtectionService.evaluate({"action": f"startup {action}", "target": item_name})
    if decision.get("blocked"):
        return _blocked(decision.get("reason", "Startup item is protected."), data={"item_name": item_name})
    success = StartupService.disable_startup_item(item_name) if action == "disable" else StartupService.enable_startup_item(item_name)
    session = RestoreService.create_session(f"startup_{action}", {"item_name": item_name, "success": success})
    return _response(message=f"Startup item {action} processed.", data={"success": success, "item_name": item_name}, restore_metadata_id=session["id"])


@real_features_v210_bp.route("/storage/summary", methods=["GET"])
@handle_errors
def storage_summary():
    drives = []
    for part in psutil.disk_partitions(all=False):
        try:
            usage = psutil.disk_usage(part.mountpoint)
        except (OSError, PermissionError):
            continue
        drives.append({"device": part.device, "mountpoint": part.mountpoint, "fstype": part.fstype, "total": usage.total, "used": usage.used, "free": usage.free, "percent": usage.percent})
    return _response(data={"drives": drives, "cleanup": CleanupCenterService.scan()})


@real_features_v210_bp.route("/network/adapters", methods=["GET"])
@handle_errors
def network_adapters():
    adapters = []
    stats = psutil.net_if_stats()
    addrs = psutil.net_if_addrs()
    for name, stat in stats.items():
        adapters.append({"name": name, "is_up": stat.isup, "speed_mbps": stat.speed, "addresses": [addr.address for addr in addrs.get(name, [])]})
    return _response(data={"items": adapters})


@real_features_v210_bp.route("/network/dns/benchmark", methods=["POST"])
@handle_errors
def network_dns_benchmark():
    hosts = _payload().get("hosts") or ["1.1.1.1", "8.8.8.8", "9.9.9.9"]
    results = []
    for host in hosts[:8]:
        start = datetime.now()
        try:
            socket.getaddrinfo(str(host), 53)
            ms = (datetime.now() - start).total_seconds() * 1000
            results.append({"host": host, "latency_ms": round(ms, 2), "ok": True})
        except OSError as exc:
            results.append({"host": host, "ok": False, "error": str(exc)})
    return _response(message="DNS benchmark completed.", data={"items": results})


@real_features_v210_bp.route("/network/dns/apply", methods=["POST"])
@real_features_v210_bp.route("/network/dns/restore", methods=["POST"])
@handle_errors
@log_requests
def network_dns_apply_restore():
    payload = _payload()
    action = request.path.rsplit("/", 1)[-1]
    if not _is_windows():
        return _blocked("DNS apply/restore is available on Windows only.", data={"platform": platform.platform()})
    if not _is_admin():
        return _blocked("DNS apply/restore requires Administrator.", requires_admin=True)
    if not bool(payload.get("user_approved")):
        return _blocked(f"DNS {action} requires explicit approval.", data={"requested": payload})
    session = RestoreService.create_session(f"dns_{action}", {"requested": payload})
    return _response(message=f"DNS {action} request accepted with restore metadata.", data={"requested": payload, "execution": "adapter-specific apply is guarded; use selected interface payload"}, restore_metadata_id=session["id"])


@real_features_v210_bp.route("/power/plans", methods=["GET"])
@handle_errors
def power_plans():
    result = _run(["powercfg", "/L"]) if _is_windows() else {"success": False, "stderr": "Windows only"}
    return _response(data={"raw": result, "windows_only": True})


@real_features_v210_bp.route("/power/active", methods=["GET"])
@handle_errors
def power_active():
    result = _run(["powercfg", "/GETACTIVESCHEME"]) if _is_windows() else {"success": False, "stderr": "Windows only"}
    return _response(data={"raw": result, "windows_only": True})


@real_features_v210_bp.route("/power/apply", methods=["POST"])
@real_features_v210_bp.route("/power/restore", methods=["POST"])
@handle_errors
@log_requests
def power_apply_restore():
    payload = _payload()
    action = request.path.rsplit("/", 1)[-1]
    if not _is_windows():
        return _blocked("Power plan changes are available on Windows only.")
    if not bool(payload.get("user_approved")):
        return _blocked(f"Power {action} requires explicit approval.", data={"requested": payload})
    guid = str(payload.get("guid") or payload.get("plan_guid") or "").strip()
    if not guid:
        return _blocked("Power plan GUID is required.", data={"active": (_run(["powercfg", "/GETACTIVESCHEME"]) if _is_windows() else {})})
    before = _run(["powercfg", "/GETACTIVESCHEME"])
    result = _run(["powercfg", "/SETACTIVE", guid])
    session = RestoreService.create_session(f"power_{action}", {"before": before, "requested_guid": guid, "result": result})
    ok = bool(result.get("success"))
    return _response(ok=ok, status="success" if ok else "blocked", message="Power plan updated." if ok else "Power plan change failed.", data={"before": before, "result": result}, restore_metadata_id=session["id"])


@real_features_v210_bp.route("/visual-effects/apply", methods=["POST"])
@real_features_v210_bp.route("/visual-effects/restore", methods=["POST"])
@handle_errors
@log_requests
def visual_effects_apply_restore():
    payload = _payload()
    if not bool(payload.get("user_approved")):
        return _blocked("Visual effects change requires explicit approval.", data={"requested": payload})
    session = RestoreService.create_session("visual_effects", {"requested": payload, "safe_reversible": True})
    EnterpriseLogService.append("visual_effects", "metadata_recorded", payload)
    return _response(message="Visual effects safe profile metadata recorded.", data={"profile": payload.get("preset", "balanced"), "system_write": "guarded"}, restore_metadata_id=session["id"])


@real_features_v210_bp.route("/apps/installed", methods=["GET"])
@handle_errors
def apps_installed():
    from api.product_v14 import _installed_apps
    return _response(data=_installed_apps(limit=200))


@real_features_v210_bp.route("/apps/uninstall/plan", methods=["POST"])
@handle_errors
def apps_uninstall_plan():
    payload = _payload()
    app_name = str(payload.get("name") or payload.get("app_name") or "").strip()
    return _response(status="success", message="Uninstall plan generated.", data={"app_name": app_name, "requires_two_step_confirmation": True, "protected_apps_guard": True, "auto_uninstall": False})


@real_features_v210_bp.route("/apps/uninstall/apply", methods=["POST"])
@handle_errors
@log_requests
def apps_uninstall_apply():
    payload = _payload()
    if not bool(payload.get("user_approved")) or str(payload.get("confirmation") or "").lower() != "uninstall":
        return _blocked("Uninstall apply requires explicit approval and confirmation text.", data={"requested": payload})
    app_name = str(payload.get("name") or payload.get("app_name") or "").strip()
    decision = ProtectionService.evaluate({"action": "uninstall app", "target": app_name})
    if decision.get("blocked"):
        return _blocked(decision.get("reason", "App is protected."), data={"app_name": app_name})
    session = RestoreService.create_session("app_uninstall", {"app_name": app_name, "handoff": "Windows uninstall command required"})
    return _response(message="Uninstall approval recorded; use official app uninstaller handoff.", data={"app_name": app_name, "handoff_required": True}, restore_metadata_id=session["id"])


@real_features_v210_bp.route("/automation/tasks", methods=["GET", "POST"])
@handle_errors
@log_requests
def automation_tasks():
    key = "config/v210_automation_tasks.json"
    tasks = _load_list(key)
    if request.method == "GET":
        return _response(data={"items": tasks, "templates": ["weekly_safe_cleanup_scan", "weekly_startup_report", "monthly_health_report_export"]})
    payload = _payload()
    template = str(payload.get("template") or "weekly_safe_cleanup_scan")
    allowed = {"weekly_safe_cleanup_scan", "weekly_startup_report", "monthly_health_report_export"}
    if template not in allowed:
        return _blocked("Only safe built-in automation templates are allowed.", data={"template": template})
    task = {"id": f"task_{uuid.uuid4().hex[:10]}", "template": template, "enabled": False, "created_at": _utc_now(), "hidden": False, "arbitrary_shell": False}
    tasks.append(task)
    _save_list(key, tasks)
    return _response(message="Safe automation task created.", data={"task": task})


@real_features_v210_bp.route("/automation/tasks/<task_id>/enable", methods=["POST"])
@real_features_v210_bp.route("/automation/tasks/<task_id>/disable", methods=["POST"])
@real_features_v210_bp.route("/automation/tasks/<task_id>", methods=["DELETE"])
@handle_errors
@log_requests
def automation_task_mutate(task_id: str):
    key = "config/v210_automation_tasks.json"
    tasks = _load_list(key)
    action = "delete" if request.method == "DELETE" else request.path.rsplit("/", 1)[-1]
    changed = None
    next_tasks = []
    for task in tasks:
        if task.get("id") == task_id:
            changed = task
            if action == "enable":
                task["enabled"] = True
                next_tasks.append(task)
            elif action == "disable":
                task["enabled"] = False
                next_tasks.append(task)
            elif action == "delete":
                continue
        else:
            next_tasks.append(task)
    if changed is None:
        return _blocked("Automation task not found.", data={"task_id": task_id})
    _save_list(key, next_tasks)
    return _response(message=f"Automation task {action} processed.", data={"task": changed, "items": next_tasks})


@real_features_v210_bp.route("/restore/status", methods=["GET"])
@handle_errors
def restore_status():
    return _response(data={"sessions": RestoreService.sessions(), "windows_system_restore": "manual_admin_check_required", "admin": _is_admin()})


@real_features_v210_bp.route("/restore/metadata", methods=["GET"])
@handle_errors
def restore_metadata():
    return _response(data=RestoreService.sessions())


@real_features_v210_bp.route("/restore/rollback", methods=["POST"])
@handle_errors
@log_requests
def restore_rollback():
    payload = _payload()
    session_id = str(payload.get("session_id") or "")
    if not session_id:
        return _blocked("Rollback requires a restore metadata session_id.", data=RestoreService.sessions())
    result = RestoreService.apply(session_id)
    if not result.get("success"):
        return _blocked("Restore metadata session was not found.", data=result)
    return _response(message="Rollback metadata applied where supported.", data=result, restore_metadata_id=session_id)


def _repair_job(tool: str, command: List[str], requires_approval: bool = True):
    payload = _payload()
    if requires_approval and not bool(payload.get("user_approved")):
        return _blocked(f"{tool} requires explicit approval.", requires_admin=True, data={"tool": tool, "command": command})
    if not _is_admin():
        return _blocked(f"{tool} requires Administrator.", requires_admin=True, data={"tool": tool})
    result = _run(command, timeout=90)
    job = _make_job(tool, command, result)
    return _response(message=f"{tool} completed.", data={"result": result}, job_id=job["id"])


@real_features_v210_bp.route("/repair/sfc-scan", methods=["POST"])
@handle_errors
@log_requests
def repair_sfc_scan():
    return _repair_job("sfc-scan", ["sfc", "/scannow"])


@real_features_v210_bp.route("/repair/dism-checkhealth", methods=["POST"])
@handle_errors
@log_requests
def repair_dism_checkhealth():
    return _repair_job("dism-checkhealth", ["DISM", "/Online", "/Cleanup-Image", "/CheckHealth"])


@real_features_v210_bp.route("/repair/dism-scanhealth", methods=["POST"])
@handle_errors
@log_requests
def repair_dism_scanhealth():
    return _repair_job("dism-scanhealth", ["DISM", "/Online", "/Cleanup-Image", "/ScanHealth"])


@real_features_v210_bp.route("/repair/dism-restorehealth", methods=["POST"])
@handle_errors
@log_requests
def repair_dism_restorehealth():
    return _repair_job("dism-restorehealth", ["DISM", "/Online", "/Cleanup-Image", "/RestoreHealth"])


@real_features_v210_bp.route("/repair/chkdsk-check", methods=["POST"])
@handle_errors
@log_requests
def repair_chkdsk_check():
    return _repair_job("chkdsk-check", ["chkdsk", "C:", "/scan"])


@real_features_v210_bp.route("/windows/features/plan", methods=["POST"])
@handle_errors
def windows_features_plan():
    payload = _payload()
    return _response(message="Windows feature plan generated.", data={"requested": payload, "safe_allowlist_only": True, "requires_admin": True, "requires_reboot_notice": True})


@real_features_v210_bp.route("/services", methods=["GET"])
@handle_errors
def services_list():
    from api.product_v14 import _windows_services
    return _response(data=_windows_services(limit=300))


@real_features_v210_bp.route("/services/start", methods=["POST"])
@real_features_v210_bp.route("/services/stop", methods=["POST"])
@handle_errors
@log_requests
def services_start_stop():
    payload = _payload()
    action = request.path.rsplit("/", 1)[-1]
    service = str(payload.get("name") or payload.get("service") or "").strip()
    if not service:
        return _blocked("Service name is required.", data={"requested": payload})
    decision = ProtectionService.evaluate({"action": f"{action} service", "target": service})
    if decision.get("blocked"):
        return _blocked(decision.get("reason", "Service is protected."), data={"service": service})
    if not bool(payload.get("user_approved")):
        return _blocked(f"Service {action} requires explicit approval.", data={"service": service})
    if not _is_admin():
        return _blocked(f"Service {action} requires Administrator.", requires_admin=True, data={"service": service})
    result = _run(["sc", action, service])
    session = RestoreService.create_session(f"service_{action}", {"service": service, "result": result})
    return _response(ok=result.get("success", False), message=f"Service {action} command completed.", data={"result": result}, restore_metadata_id=session["id"])


@real_features_v210_bp.route("/security/defender-status", methods=["GET"])
@handle_errors
def defender_status():
    result = _powershell("Get-MpComputerStatus | Select-Object AMServiceEnabled,AntivirusEnabled,RealTimeProtectionEnabled,AntispywareEnabled | ConvertTo-Json -Compress", timeout=6) if _is_windows() else {"success": False, "stderr": "Windows only"}
    return _response(data={"read_only": True, "raw": result})


@real_features_v210_bp.route("/security/firewall-status", methods=["GET"])
@handle_errors
def firewall_status():
    result = _powershell("Get-NetFirewallProfile | Select-Object Name,Enabled,DefaultInboundAction,DefaultOutboundAction | ConvertTo-Json -Compress", timeout=6) if _is_windows() else {"success": False, "stderr": "Windows only"}
    return _response(data={"read_only": True, "raw": result})


@real_features_v210_bp.route("/security/update-status", methods=["GET"])
@handle_errors
def update_status():
    result = _powershell("Get-Service wuauserv | Select-Object Name,Status,StartType | ConvertTo-Json -Compress", timeout=6) if _is_windows() else {"success": False, "stderr": "Windows only"}
    return _response(data={"read_only": True, "permanent_disable_blocked": True, "raw": result})


@real_features_v210_bp.route("/drivers/summary", methods=["GET"])
@real_features_v210_bp.route("/drivers/recommendations", methods=["GET"])
@handle_errors
def drivers_summary_recommendations():
    gpu = GpuDetectionService.get_gpu_summary()
    return _response(data={"gpu": gpu, "recommendation": DriverRecommendationService.status(), "official_links": _official_vendor_links(), "auto_install": False})


@real_features_v210_bp.route("/drivers/export-report", methods=["POST"])
@handle_errors
def drivers_export_report():
    report = ReportService.build_report()
    report.update({
        "title": "HyperBoostX Driver Guidance Report",
        "driver_guidance": {
            "drivers": DriverRecommendationService.status(),
            "official_links": _official_vendor_links(),
            "gpu": GpuDetectionService.get_gpu_summary(),
        },
        "summary": "Local hardware-aware driver guidance. HyperBoostX does not auto-install GPU drivers.",
    })
    return _response(data=ReportService.export_report(_payload().get("format", "json"), report=report))


@real_features_v210_bp.route("/rgb/software", methods=["GET"])
@real_features_v210_bp.route("/rgb/conflicts", methods=["GET"])
@handle_errors
def rgb_software_conflicts():
    rgb_items = [item for item in GpuDetectionService.detect_background_apps() if item.get("category") == "rgb"]
    detected = [item for item in rgb_items if item.get("detected")]
    return _response(data={"items": rgb_items, "detected": detected, "conflict": len(detected) > 1, "full_control": False, "feature_name": "RGB Conflict Detector"})


@real_features_v210_bp.route("/rgb/restart-app", methods=["POST"])
@handle_errors
@log_requests
def rgb_restart_app():
    payload = _payload()
    name = str(payload.get("name") or payload.get("process") or "").strip()
    if not name:
        return _blocked("RGB app name/process is required.", data={"software": [item for item in GpuDetectionService.detect_background_apps() if item.get("category") == "rgb"]})
    decision = ProtectionService.evaluate({"action": "restart rgb app", "target": name})
    if decision.get("blocked"):
        return _blocked(decision.get("reason", "RGB app is protected."), data={"name": name})
    if not bool(payload.get("user_approved")):
        return _blocked("RGB app restart requires explicit approval.", data={"name": name})
    EnterpriseLogService.append("rgb_restart_app", "approved_manual_restart", {"name": name})
    return _response(message="RGB app restart approved; manual restart boundary recorded.", data={"name": name, "manual_restart_required": True})


@real_features_v210_bp.route("/reports/export-json", methods=["POST"])
@real_features_v210_bp.route("/reports/export-txt", methods=["POST"])
@real_features_v210_bp.route("/reports/export-md", methods=["POST"])
@handle_errors
def reports_export_format():
    fmt = request.path.rsplit("-", 1)[-1]
    return _response(data=ReportService.export_report(fmt))


@real_features_v210_bp.route("/logs/export", methods=["POST"])
@handle_errors
def logs_export_v210():
    log = EnterpriseLogService.latest()
    return _response(data={"format": _payload().get("format", "json"), "content": json.dumps(log, indent=2), "log": log})


@real_features_v210_bp.route("/license/status", methods=["GET"])
@handle_errors
def license_status():
    state = LocalJsonStore.load("config/v210_license_state.json", {"activated": False, "mode": "local_beta", "offline_grace": True}, dict)
    return _response(data={**state, "production_server": False, "hardcoded_secret": False, "fake_lock": False})


@real_features_v210_bp.route("/license/activate-local", methods=["POST"])
@handle_errors
@log_requests
def license_activate_local():
    payload = _payload()
    token = str(payload.get("license_key") or payload.get("token") or "local-beta").strip()
    fingerprint = hashlib.sha256(token.encode("utf-8")).hexdigest()[:16]
    state = {"activated": True, "mode": "local_beta", "activated_at": _utc_now(), "license_fingerprint": fingerprint, "offline_grace": True, "production_server": False}
    LocalJsonStore.save("config/v210_license_state.json", state)
    return _response(message="Local beta license boundary activated.", data=state)


@real_features_v210_bp.route("/license/deactivate-local", methods=["POST"])
@handle_errors
@log_requests
def license_deactivate_local():
    state = {"activated": False, "mode": "local_beta", "deactivated_at": _utc_now(), "offline_grace": True, "production_server": False}
    LocalJsonStore.save("config/v210_license_state.json", state)
    return _response(message="Local beta license boundary deactivated.", data=state)


@real_features_v210_bp.route("/plugins/catalog", methods=["GET"])
@handle_errors
def plugins_catalog():
    catalog = LocalJsonStore.load("config/v210_plugin_catalog.json", {
        "items": [
            {"id": "diagnostics-export", "name": "Diagnostics Export", "version": "1.0.0", "permissions": ["read_reports"], "enabled": False, "built_in": True},
            {"id": "local-report-viewer", "name": "Local Report Viewer", "version": "1.0.0", "permissions": ["read_reports"], "enabled": False, "built_in": True},
        ],
        "arbitrary_execution": False,
        "unsigned_plugins_blocked": True,
    }, dict)
    return _response(data=catalog)


@real_features_v210_bp.route("/plugins/validate", methods=["POST"])
@handle_errors
def plugins_validate():
    payload = _payload()
    manifest = payload.get("manifest") if isinstance(payload.get("manifest"), dict) else payload
    required = {"id", "name", "version", "permissions", "sha256"}
    missing = sorted(required - set(manifest.keys()))
    unsigned = not str(manifest.get("sha256") or "").strip()
    valid = not missing and not unsigned and not bool(manifest.get("exec") or manifest.get("entrypoint"))
    data = {"valid": valid, "missing": missing, "unsigned": unsigned, "arbitrary_execution": bool(manifest.get("exec") or manifest.get("entrypoint")), "manifest": manifest}
    if not valid:
        return _blocked("Plugin manifest failed validation.", data=data)
    return _response(message="Plugin manifest validated.", data=data)


@real_features_v210_bp.route("/plugins/install", methods=["POST"])
@handle_errors
@log_requests
def plugins_install():
    payload = _payload()
    manifest = payload.get("manifest") if isinstance(payload.get("manifest"), dict) else payload
    if not bool(payload.get("user_approved")):
        return _blocked("Plugin install requires explicit approval.", data={"manifest": manifest})
    required = {"id", "name", "version", "permissions", "sha256"}
    missing = sorted(required - set(manifest.keys()))
    if missing or manifest.get("exec") or manifest.get("entrypoint"):
        return _blocked("Unsigned or executable plugin packages are blocked.", data={"missing": missing, "manifest": manifest})
    catalog = LocalJsonStore.load("config/v210_plugin_catalog.json", {"items": [], "arbitrary_execution": False, "unsigned_plugins_blocked": True}, dict)
    item = {key: manifest.get(key) for key in ["id", "name", "version", "permissions", "sha256"]}
    item["enabled"] = False
    item["installed_at"] = _utc_now()
    catalog["items"] = [existing for existing in catalog.get("items", []) if existing.get("id") != item["id"]] + [item]
    LocalJsonStore.save("config/v210_plugin_catalog.json", catalog)
    return _response(message="Plugin manifest installed as disabled local catalog item.", data={"plugin": item})


@real_features_v210_bp.route("/plugins/uninstall", methods=["POST"])
@handle_errors
@log_requests
def plugins_uninstall():
    payload = _payload()
    plugin_id = str(payload.get("id") or payload.get("plugin_id") or "").strip()
    if not plugin_id:
        return _blocked("Plugin id is required.")
    catalog = LocalJsonStore.load("config/v210_plugin_catalog.json", {"items": []}, dict)
    before = len(catalog.get("items", []))
    catalog["items"] = [item for item in catalog.get("items", []) if item.get("id") != plugin_id]
    LocalJsonStore.save("config/v210_plugin_catalog.json", catalog)
    return _response(message="Plugin catalog item removed.", data={"plugin_id": plugin_id, "removed": len(catalog["items"]) != before})

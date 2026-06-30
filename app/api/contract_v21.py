"""HyperBoostX v2.1 compatibility API contract."""

from __future__ import annotations

import json
import platform
from typing import Any, Dict, Iterable, List, Optional

import psutil
from flask import Blueprint, jsonify, request

from api.middleware import handle_errors, log_requests
from core.constants import APP_VERSION
from services.monitoring.gpu_detection_service import GpuDetectionService
from services.monitoring.monitor_service import MonitorService
from services.monitoring.report_service import ReportService
from services.optimization.boost_plan_service import BoostPlanService
from services.product_features import (
    CleanupCenterService,
    EnterpriseLogService,
    FeatureAuditService,
    GameDatabaseService,
    GpuCenterService,
    HyperBoostScoreEngine,
    NetworkToolsFacade,
    PerformanceAdvisorService,
    PerformanceHistoryService,
    ProcessAnalyzerService,
    ProtectionService,
    RestoreService,
    StartupManagerFacade,
    UiSettingsService,
)

contract_v21_bp = Blueprint("contract_v21", __name__, url_prefix="/api")


def _payload() -> Dict[str, Any]:
    return request.get_json(silent=True) or {}


def _response(module: str, action: str, *, status: str = "success", message: str = "OK", data: Optional[Dict[str, Any]] = None, warnings: Optional[Iterable[str]] = None, blocked_reasons: Optional[Iterable[str]] = None, restore_available: bool = False, restore_session_id: Optional[str] = None, report_id: Optional[str] = None):
    action_id = f"{module}.{action}".replace("/", ".")
    response_data = data or {}
    report_available = bool(report_id) or status in {"success", "partial", "preview"}
    return jsonify({
        "ok": status not in {"blocked", "error"},
        "module": module,
        "action": action,
        "action_id": action_id,
        "page": module,
        "status": status,
        "message": message,
        "data": response_data,
        "warnings": list(warnings or []),
        "blocked_reasons": list(blocked_reasons or []),
        "requires_admin": bool(response_data.get("requires_admin", False)),
        "requires_reboot": bool(response_data.get("requires_reboot", False)),
        "rollback_available": restore_available,
        "restore_available": restore_available,
        "restore_session_id": restore_session_id,
        "report_available": report_available,
        "report_id": report_id,
    })


def _preview(module: str, action: str, data: Optional[Dict[str, Any]] = None, message: Optional[str] = None):
    return _response(module, action, status="preview", message=message or "Preview generated. No system change has been applied.", data={"preview": data or {}, "requires_approval": True, "restore_metadata_required": True, "report_required": True, "safety_guard": "active"}, warnings=["Risky actions require explicit approval and supported restore metadata."], restore_available=True)


def _blocked(module: str, action: str, reason: str, data: Optional[Dict[str, Any]] = None):
    return _response(module, action, status="blocked", message=reason, data=data or {}, blocked_reasons=[reason], warnings=["This compatibility route never performs silent or destructive changes."])


def _last_boost_result() -> Dict[str, Any]:
    return getattr(BoostPlanService, "_last_result", None) or {}


def _disk_drives() -> List[Dict[str, Any]]:
    items: List[Dict[str, Any]] = []
    for part in psutil.disk_partitions(all=False):
        try:
            usage = psutil.disk_usage(part.mountpoint)
        except (OSError, PermissionError):
            continue
        items.append({
            "device": part.device,
            "mountpoint": part.mountpoint,
            "fstype": part.fstype,
            "total_gb": round(usage.total / (1024 ** 3), 2),
            "used_gb": round(usage.used / (1024 ** 3), 2),
            "free_gb": round(usage.free / (1024 ** 3), 2),
            "used_percent": usage.percent,
            "read_only": True,
        })
    return items


def _dashboard_data() -> Dict[str, Any]:
    stats = MonitorService.get_current_stats()
    score = HyperBoostScoreEngine.calculate(stats=stats)
    alerts = []
    if float(stats.get("memory") or 0) >= 85:
        alerts.append({"level": "warning", "message": "RAM pressure is high. Review background apps before gaming."})
    if float(stats.get("disk") or 0) >= 90:
        alerts.append({"level": "warning", "message": "System drive is nearly full. Run safe cleanup preview."})
    if not alerts:
        alerts.append({"level": "info", "message": "No critical local pressure alert detected."})
    return {"stats": stats, "score": score, "alerts": alerts, "activity": EnterpriseLogService.latest().get("items", [])[-20:]}


@contract_v21_bp.route("/status", methods=["GET"])
@handle_errors
def api_status():
    return _response("core", "status", data={"version": APP_VERSION, "backend": "connected", **_dashboard_data()})


@contract_v21_bp.route("/settings", methods=["GET", "POST"])
@handle_errors
@log_requests
def settings_contract():
    if request.method == "POST":
        settings = UiSettingsService.update(_payload())
        return _response("settings", "update", message="Settings saved with safe defaults.", data={"settings": settings})
    return _response("settings", "read", data={"settings": UiSettingsService.get()})


@contract_v21_bp.route("/dashboard/summary", methods=["GET"])
@handle_errors
def dashboard_summary():
    return _response("dashboard", "summary", data=_dashboard_data())


@contract_v21_bp.route("/dashboard/score", methods=["GET"])
@handle_errors
def dashboard_score():
    return _response("dashboard", "score", data=HyperBoostScoreEngine.calculate())


@contract_v21_bp.route("/dashboard/alerts", methods=["GET"])
@handle_errors
def dashboard_alerts():
    return _response("dashboard", "alerts", data={"items": _dashboard_data()["alerts"]})


@contract_v21_bp.route("/dashboard/activity", methods=["GET"])
@handle_errors
def dashboard_activity():
    return _response("dashboard", "activity", data=EnterpriseLogService.latest())


@contract_v21_bp.route("/scan/system", methods=["POST"])
@contract_v21_bp.route("/scan/quick", methods=["POST"])
@contract_v21_bp.route("/scan/full", methods=["POST"])
@handle_errors
@log_requests
def scan_contract():
    mode = request.path.rsplit("/", 1)[-1]
    score = HyperBoostScoreEngine.calculate()
    advisor = PerformanceAdvisorService.analyze(_payload())
    record = PerformanceHistoryService.record_scan({"scores": score.get("scores", {}), "advisor_summary": advisor.get("analysis", [{}])[0].get("message")})
    return _response("scan", mode, message=f"{mode.title()} scan completed.", data={"scan": record, "score": score, "advisor": advisor})


@contract_v21_bp.route("/boost/preview", methods=["POST"])
@handle_errors
@log_requests
def boost_preview_contract():
    payload = _payload()
    plan = BoostPlanService.create_plan(goal=payload.get("goal", "gaming"), mode=payload.get("mode", "balanced"))
    return _preview("boost", "preview", {"plan": plan})


@contract_v21_bp.route("/boost/last-result", methods=["GET"])
@handle_errors
def boost_last_result():
    result = _last_boost_result()
    return _response("boost", "last-result", data={"result": result, "available": bool(result)})


@contract_v21_bp.route("/boost/history", methods=["GET"])
@handle_errors
def boost_history():
    return _response("boost", "history", data={"scans": PerformanceHistoryService.history().get("items", []), "last_result": _last_boost_result()})


@contract_v21_bp.route("/performance/summary", methods=["GET"])
@handle_errors
def performance_summary():
    return _response("performance", "summary", data={"score": HyperBoostScoreEngine.calculate(), "advisor": PerformanceAdvisorService.analyze()})


@contract_v21_bp.route("/performance/plan", methods=["POST"])
@handle_errors
@log_requests
def performance_plan():
    payload = _payload()
    plan = BoostPlanService.create_plan(goal=payload.get("goal", "performance"), mode=payload.get("mode", "balanced"))
    return _response("performance", "plan", status="preview", message="Performance plan generated for review.", data={"plan": plan}, restore_available=True)


@contract_v21_bp.route("/performance/apply", methods=["POST"])
@handle_errors
@log_requests
def performance_apply():
    result = BoostPlanService.apply_plan(_payload())
    if not result.get("success"):
        return _blocked("performance", "apply", "User approval is required before applying performance actions.", {"result": result})
    return _response("performance", "apply", message="Approved safe performance actions completed.", data={"result": result}, restore_available=True, report_id=(result.get("report") or {}).get("report_id"))


@contract_v21_bp.route("/startup/summary", methods=["GET"])
@handle_errors
def startup_summary():
    items = StartupManagerFacade.items().get("items", [])
    return _response("startup", "summary", data={"count": len(items), "items": items[:20], "read_only": True})


@contract_v21_bp.route("/processes", methods=["GET"])
@handle_errors
def processes_root():
    return _response("processes", "list", data=ProcessAnalyzerService.heavy())


@contract_v21_bp.route("/processes/summary", methods=["GET"])
@handle_errors
def processes_summary():
    heavy = ProcessAnalyzerService.heavy().get("items", [])
    return _response("processes", "summary", data={"heavy_count": len(heavy), "items": heavy[:10], "recommendations": ProcessAnalyzerService.recommendations().get("items", [])})


@contract_v21_bp.route("/processes/preview-close", methods=["POST"])
@handle_errors
@log_requests
def processes_preview_close():
    return _preview("processes", "preview-close", {"requested": _payload(), "protected_processes": ProtectionService.blocked_actions()})


@contract_v21_bp.route("/processes/close-selected", methods=["POST"])
@handle_errors
@log_requests
def processes_close_selected():
    return _blocked("processes", "close-selected", "Process closing requires per-process preview, approval, and protected-process checks before any future apply flow.", {"requested": _payload()})


@contract_v21_bp.route("/cleanup/history", methods=["GET"])
@handle_errors
def cleanup_history():
    return _response("cleanup", "history", data=CleanupCenterService.report())


@contract_v21_bp.route("/storage/drives", methods=["GET"])
@handle_errors
def storage_drives():
    return _response("storage", "drives", data={"items": _disk_drives(), "read_only": True})


@contract_v21_bp.route("/storage/scan", methods=["POST"])
@contract_v21_bp.route("/storage/analyze", methods=["POST"])
@handle_errors
@log_requests
def storage_scan_analyze():
    action = request.path.rsplit("/", 1)[-1]
    data = {"drives": _disk_drives(), "cleanup_preview": CleanupCenterService.scan(), "safe_excludes": ["Documents", "Downloads", "Desktop", "Pictures", "Videos", "Music", "Game Saves"]}
    return _response("storage", action, data=data)


@contract_v21_bp.route("/storage/cleanup-preview", methods=["POST"])
@handle_errors
@log_requests
def storage_cleanup_preview():
    return _preview("storage", "cleanup-preview", CleanupCenterService.preview(_payload()))


@contract_v21_bp.route("/gaming/detect", methods=["GET"])
@handle_errors
def gaming_detect():
    return _response("gaming", "detect", data=GameDatabaseService.running())


@contract_v21_bp.route("/gaming/profiles", methods=["GET"])
@handle_errors
def gaming_profiles():
    return _response("gaming", "profiles", data=GameDatabaseService.library())


@contract_v21_bp.route("/gaming/profile/apply", methods=["POST"])
@handle_errors
@log_requests
def gaming_profile_apply():
    result = GameDatabaseService.profile_apply(_payload())
    if not result.get("success"):
        return _blocked("gaming", "profile/apply", "Game profile apply requires a valid profile and explicit approval.", {"result": result})
    return _response("gaming", "profile/apply", data={"result": result}, restore_available=True)


@contract_v21_bp.route("/gaming/profile/restore", methods=["POST"])
@handle_errors
@log_requests
def gaming_profile_restore():
    return _response("gaming", "profile/restore", data={"result": GameDatabaseService.profile_restore(_payload())}, restore_available=True)


@contract_v21_bp.route("/gaming/overlay/scan", methods=["POST"])
@handle_errors
@log_requests
def gaming_overlay_scan():
    return _response("gaming", "overlay/scan", data={"items": GpuDetectionService.detect_overlays(), "read_only": True})


@contract_v21_bp.route("/gaming/boost/preview", methods=["POST"])
@handle_errors
@log_requests
def gaming_boost_preview():
    return _preview("gaming", "boost/preview", {"plan": BoostPlanService.create_plan(goal="gaming", mode=_payload().get("mode", "balanced"))})


@contract_v21_bp.route("/gaming/boost/apply", methods=["POST"])
@handle_errors
@log_requests
def gaming_boost_apply():
    result = BoostPlanService.apply_plan(_payload())
    if not result.get("success"):
        return _blocked("gaming", "boost/apply", "Gaming boost apply requires explicit approval.", {"result": result})
    return _response("gaming", "boost/apply", data={"result": result}, restore_available=True, report_id=(result.get("report") or {}).get("report_id"))


@contract_v21_bp.route("/gpu/info", methods=["GET"])
@handle_errors
def gpu_info():
    return _response("gpu", "info", data=GpuCenterService.vendor_guide())


@contract_v21_bp.route("/gpu/health", methods=["GET"])
@handle_errors
def gpu_health():
    data = {"summary": GpuDetectionService.get_gpu_summary(), "recommendations": GpuCenterService.recommendations(), "driver_changes": "manual_official_only"}
    return _response("gpu", "health", data=data)


@contract_v21_bp.route("/network/status", methods=["GET"])
@handle_errors
def network_status():
    return _response("network", "status", data=NetworkToolsFacade.diagnostics())


@contract_v21_bp.route("/network/ping-test", methods=["POST"])
@handle_errors
@log_requests
def network_ping_test():
    return _response("network", "ping-test", data=NetworkToolsFacade.ping(_payload()))


@contract_v21_bp.route("/network/dns-preview", methods=["POST"])
@handle_errors
@log_requests
def network_dns_preview():
    return _preview("network", "dns-preview", {"current_dns_test": NetworkToolsFacade.dns_test(), "requested": _payload()})


@contract_v21_bp.route("/network/dns-apply", methods=["POST"])
@handle_errors
@log_requests
def network_dns_apply():
    return _blocked("network", "dns-apply", "DNS apply is blocked until adapter-specific restore metadata is available.", {"requested": _payload()})


@contract_v21_bp.route("/network/reset-preview", methods=["POST"])
@handle_errors
@log_requests
def network_reset_preview():
    return _preview("network", "reset-preview", {"requested": _payload(), "requires_admin": True})


@contract_v21_bp.route("/security/health", methods=["GET"])
@handle_errors
def security_health():
    data = {"read_only": True, "platform": platform.platform(), "blocked_risky_actions": ProtectionService.blocked_actions()}
    return _response("security", "health", data=data)


@contract_v21_bp.route("/apps/uninstall", methods=["POST"])
@handle_errors
@log_requests
def apps_uninstall():
    return _blocked("apps", "uninstall", "Silent uninstall is blocked. Use preview and Windows confirmation for any future uninstall flow.", {"requested": _payload()})


@contract_v21_bp.route("/windows/features/apply", methods=["POST"])
@handle_errors
@log_requests
def windows_features_apply():
    return _blocked("windows", "features/apply", "Windows feature changes require admin, restore metadata, and explicit OS-level confirmation.", {"requested": _payload()})


@contract_v21_bp.route("/windows/services/apply", methods=["POST"])
@handle_errors
@log_requests
def windows_services_apply():
    return _blocked("windows", "services/apply", "Service startup changes are blocked until a protected-service check and restore plan pass.", {"requested": _payload()})


@contract_v21_bp.route("/repair/sfc-preview", methods=["POST"])
@contract_v21_bp.route("/repair/dism-preview", methods=["POST"])
@handle_errors
@log_requests
def repair_preview_contract():
    tool = request.path.split("/")[-1].replace("-preview", "")
    return _preview("repair", f"{tool}-preview", {"tool": tool, "requires_admin": True, "long_running": True})


@contract_v21_bp.route("/repair/sfc-run", methods=["POST"])
@contract_v21_bp.route("/repair/dism-run", methods=["POST"])
@handle_errors
@log_requests
def repair_run_contract():
    tool = request.path.split("/")[-1].replace("-run", "")
    reason = f"{tool.upper()} run is not started by the compatibility route without an elevated approved job runner."
    return _blocked("repair", f"{tool}-run", reason, {"requested": _payload(), "requires_admin": True})


@contract_v21_bp.route("/restore/create", methods=["POST"])
@handle_errors
@log_requests
def restore_create():
    payload = _payload()
    session = RestoreService.create_session(str(payload.get("module") or "manual"), payload.get("metadata") or payload)
    return _response("restore", "create", message="Restore metadata session created.", data={"session": session}, restore_available=True, restore_session_id=session.get("id"))


@contract_v21_bp.route("/restore/undo-last", methods=["POST"])
@handle_errors
@log_requests
def restore_undo_last():
    sessions = RestoreService.sessions().get("items", [])
    if not sessions:
        return _blocked("restore", "undo-last", "No restore session is available.")
    session_id = sessions[-1].get("id")
    return _response("restore", "undo-last", data=RestoreService.apply(session_id), restore_available=True, restore_session_id=session_id)


@contract_v21_bp.route("/automation/create", methods=["POST"])
@contract_v21_bp.route("/automation/dry-run", methods=["POST"])
@contract_v21_bp.route("/automation/enable", methods=["POST"])
@contract_v21_bp.route("/automation/disable", methods=["POST"])
@contract_v21_bp.route("/automation/delete", methods=["POST"])
@handle_errors
@log_requests
def automation_contract():
    action = request.path.rsplit("/", 1)[-1]
    if action == "dry-run":
        return _preview("automation", action, {"requested": _payload(), "policy": "scan_and_report_only"})
    return _blocked("automation", action, "Automation mutation is disabled until safe-only rule persistence and history are explicitly configured.", {"requested": _payload()})


@contract_v21_bp.route("/ai/status", methods=["GET"])
@handle_errors
def ai_status():
    data = {"online_provider": False, "local_recommendation_fallback": True, "can_run_shell": False, "approval_required": True}
    return _response("ai", "status", data=data)


@contract_v21_bp.route("/ai/ask", methods=["POST"])
@handle_errors
@log_requests
def ai_ask():
    return _response("ai", "ask", data={"answer_mode": "local_fallback", "advisor": PerformanceAdvisorService.analyze(_payload())})


@contract_v21_bp.route("/ai/plan", methods=["POST"])
@handle_errors
@log_requests
def ai_plan():
    data = {"advisor": PerformanceAdvisorService.analyze(_payload()), "plan": BoostPlanService.create_plan()}
    return _response("ai", "plan", status="preview", data=data, restore_available=True)


@contract_v21_bp.route("/ai/approve", methods=["POST"])
@contract_v21_bp.route("/ai/reject", methods=["POST"])
@handle_errors
@log_requests
def ai_approval():
    action = request.path.rsplit("/", 1)[-1]
    return _blocked("ai", action, "AI approval cannot execute actions directly; user approval must go through the module preview/apply flow.", {"requested": _payload()})


@contract_v21_bp.route("/audit/features", methods=["GET"])
@handle_errors
def audit_features():
    return _response("audit", "features", data=FeatureAuditService.run())


@contract_v21_bp.route("/audit/run", methods=["POST"])
@handle_errors
@log_requests
def audit_run():
    return _response("audit", "run", data=FeatureAuditService.run())


@contract_v21_bp.route("/audit/report", methods=["GET"])
@handle_errors
def audit_report():
    audit = FeatureAuditService.run()
    return _response("audit", "report", data={"audit": audit, "content": json.dumps(audit, indent=2)})


@contract_v21_bp.route("/update/download-preview", methods=["POST"])
@handle_errors
@log_requests
def update_download_preview():
    data = {"release_page": "https://github.com/jxxzy/HyperBoostX/releases", "auto_install": False, "signature_required_if_available": True}
    return _preview("update", "download-preview", data)


@contract_v21_bp.route("/update/download", methods=["POST"])
@handle_errors
@log_requests
def update_download():
    return _blocked("update", "download", "Automatic download is blocked until installer asset, hash, and user confirmation are verified.", {"requested": _payload()})


@contract_v21_bp.route("/update/install-preview", methods=["POST"])
@handle_errors
@log_requests
def update_install_preview():
    return _preview("update", "install-preview", {"requires_user_confirmation": True, "unsigned_installer_warning": True})


@contract_v21_bp.route("/reports", methods=["GET"])
@handle_errors
def reports_list():
    latest = ReportService.latest_report()
    return _response("reports", "list", data={"items": [latest] if latest else []})


@contract_v21_bp.route("/reports/<report_id>", methods=["GET"])
@handle_errors
def reports_get(report_id: str):
    latest = ReportService.latest_report()
    if latest and report_id in {latest.get("report_id"), "latest"}:
        return _response("reports", "get", data={"report": latest})
    return _blocked("reports", "get", "Requested report was not found in local history.", {"report_id": report_id})


@contract_v21_bp.route("/logs/recent", methods=["GET"])
@handle_errors
def logs_recent():
    return _response("logs", "recent", data=EnterpriseLogService.latest())


@contract_v21_bp.route("/logs/export", methods=["POST"])
@handle_errors
@log_requests
def logs_export():
    log = EnterpriseLogService.latest()
    return _response("logs", "export", data={"format": "json", "content": json.dumps(log, indent=2), "log": log})

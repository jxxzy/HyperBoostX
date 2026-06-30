"""System Reality Guard API endpoints."""

from __future__ import annotations

from typing import Any, Dict

from flask import Blueprint, jsonify, request

from api.middleware import handle_errors, log_requests
from services.system_reality_guard import RealitySafetyGuard, SystemRealityGuardService


system_reality_guard_bp = Blueprint("system_reality_guard", __name__, url_prefix="/api")


def _payload() -> Dict[str, Any]:
    return request.get_json(silent=True) or {}


def _json(payload: Dict[str, Any]):
    return jsonify(payload)


@system_reality_guard_bp.route("/system-reality/overview", methods=["GET"])
@handle_errors
def system_reality_overview():
    return _json(SystemRealityGuardService.reality_audit())


@system_reality_guard_bp.route("/system-reality/scan", methods=["POST"])
@handle_errors
@log_requests
def system_reality_scan():
    return _json(SystemRealityGuardService.reality_audit())


@system_reality_guard_bp.route("/system-reality/report", methods=["GET"])
@handle_errors
def system_reality_report():
    return _json(SystemRealityGuardService.before_after("report"))


@system_reality_guard_bp.route("/system-reality/before-after/start", methods=["POST"])
@handle_errors
@log_requests
def system_reality_before_after_start():
    return _json(SystemRealityGuardService.before_after("start"))


@system_reality_guard_bp.route("/system-reality/before-after/stop", methods=["POST"])
@handle_errors
@log_requests
def system_reality_before_after_stop():
    return _json(SystemRealityGuardService.before_after("stop"))


@system_reality_guard_bp.route("/lcd/apps", methods=["GET"])
@handle_errors
def lcd_apps():
    return _json(SystemRealityGuardService.lcd_apps())


@system_reality_guard_bp.route("/lcd/roles", methods=["GET", "POST"])
@handle_errors
@log_requests
def lcd_roles():
    return _json(SystemRealityGuardService.lcd_roles(_payload() if request.method == "POST" else None))


@system_reality_guard_bp.route("/lcd/vendors", methods=["GET"])
@handle_errors
def lcd_vendors():
    payload = SystemRealityGuardService.lcd_apps()
    payload["data"]["supported_vendors"] = ["KANALI", "TRCC", "HiMOS"]
    return _json(payload)


@system_reality_guard_bp.route("/lcd/vendors/<vendor>/status", methods=["GET"])
@handle_errors
def lcd_vendor_status(vendor: str):
    return _json(SystemRealityGuardService.lcd_vendor_status(vendor))


@system_reality_guard_bp.route("/lcd/vendors/trcc/helpers", methods=["GET"])
@handle_errors
def lcd_trcc_helpers():
    return _json(SystemRealityGuardService.trcc_helpers())


@system_reality_guard_bp.route("/lcd/vendors/<vendor>/open", methods=["POST"])
@handle_errors
@log_requests
def lcd_vendor_open(vendor: str):
    return _json(SystemRealityGuardService.open_vendor(vendor, _payload()))


@system_reality_guard_bp.route("/lcd/vendors/trcc/restart-preview", methods=["POST"])
@handle_errors
@log_requests
def lcd_trcc_restart_preview():
    return _json(SystemRealityGuardService.restart_preview("TRCC"))


@system_reality_guard_bp.route("/lcd/vendors/trcc/restart-apply", methods=["POST"])
@handle_errors
@log_requests
def lcd_trcc_restart_apply():
    return _json(SystemRealityGuardService.restart_preview("TRCC"))


@system_reality_guard_bp.route("/lcd/vendors/protect", methods=["POST"])
@handle_errors
@log_requests
def lcd_vendor_protect():
    return _json(SystemRealityGuardService.protect_vendor(_payload()))


@system_reality_guard_bp.route("/lcd/wallpaper/analyze", methods=["POST"])
@handle_errors
@log_requests
def lcd_wallpaper_analyze():
    return _json(SystemRealityGuardService.wallpaper_analyze(_payload()))


@system_reality_guard_bp.route("/lcd/wallpaper/convert-preview", methods=["POST"])
@handle_errors
@log_requests
def lcd_wallpaper_convert_preview():
    return _json(SystemRealityGuardService.wallpaper_convert_preview(_payload(), apply=False))


@system_reality_guard_bp.route("/lcd/wallpaper/convert-apply", methods=["POST"])
@handle_errors
@log_requests
def lcd_wallpaper_convert_apply():
    return _json(SystemRealityGuardService.wallpaper_convert_preview(_payload(), apply=True))


@system_reality_guard_bp.route("/lcd/hybrid/preview", methods=["POST"])
@handle_errors
@log_requests
def lcd_hybrid_preview():
    return _json(SystemRealityGuardService.hybrid_preview(apply=False))


@system_reality_guard_bp.route("/lcd/hybrid/apply", methods=["POST"])
@handle_errors
@log_requests
def lcd_hybrid_apply():
    return _json(SystemRealityGuardService.hybrid_preview(apply=True))


@system_reality_guard_bp.route("/lcd/native/compatibility", methods=["GET"])
@handle_errors
def lcd_native_compatibility():
    return _json(SystemRealityGuardService.native_compatibility())


@system_reality_guard_bp.route("/lcd/native/test-preview", methods=["POST"])
@handle_errors
@log_requests
def lcd_native_test_preview():
    return _json(SystemRealityGuardService.native_compatibility())


@system_reality_guard_bp.route("/lcd/safe-mode/preview", methods=["POST"])
@handle_errors
@log_requests
def lcd_safe_mode_preview():
    return _json(SystemRealityGuardService.safe_mode_preview(apply=False))


@system_reality_guard_bp.route("/lcd/safe-mode/apply", methods=["POST"])
@handle_errors
@log_requests
def lcd_safe_mode_apply():
    return _json(SystemRealityGuardService.safe_mode_preview(apply=True))


@system_reality_guard_bp.route("/defender/status", methods=["GET"])
@handle_errors
def defender_status():
    return _json(SystemRealityGuardService.defender_status())


@system_reality_guard_bp.route("/defender/performance/start", methods=["POST"])
@handle_errors
@log_requests
def defender_performance_start():
    return _json(SystemRealityGuardService.defender_performance("start"))


@system_reality_guard_bp.route("/defender/performance/stop", methods=["POST"])
@handle_errors
@log_requests
def defender_performance_stop():
    return _json(SystemRealityGuardService.defender_performance("stop"))


@system_reality_guard_bp.route("/defender/performance/report", methods=["GET"])
@handle_errors
def defender_performance_report():
    return _json(SystemRealityGuardService.defender_performance("report"))


@system_reality_guard_bp.route("/defender/exclusions/advice", methods=["GET"])
@handle_errors
def defender_exclusions_advice():
    return _json(SystemRealityGuardService.defender_exclusion_advice())


@system_reality_guard_bp.route("/defender/exclusions/preview", methods=["POST"])
@handle_errors
@log_requests
def defender_exclusions_preview():
    return _json(SystemRealityGuardService.defender_exclusion_advice(_payload(), apply=False))


@system_reality_guard_bp.route("/defender/exclusions/apply", methods=["POST"])
@handle_errors
@log_requests
def defender_exclusions_apply():
    return _json(SystemRealityGuardService.defender_exclusion_advice(_payload(), apply=True))


@system_reality_guard_bp.route("/defender/exclusions/undo", methods=["POST"])
@handle_errors
@log_requests
def defender_exclusions_undo():
    return _json(SystemRealityGuardService.response(status="admin_required", requires_admin=True, data={"undo_requested": True}, recommendations=["Remove only the specific exclusion that was previously approved. HyperBoostX did not change Defender settings in this session."]))


@system_reality_guard_bp.route("/cpu/turbo/status", methods=["GET"])
@handle_errors
def cpu_turbo_status():
    return _json(SystemRealityGuardService.cpu_status())


@system_reality_guard_bp.route("/cpu/turbo/stress-sample", methods=["POST"])
@handle_errors
@log_requests
def cpu_turbo_stress_sample():
    return _json(SystemRealityGuardService.cpu_status(_payload()))


@system_reality_guard_bp.route("/cpu/power-plan", methods=["GET"])
@handle_errors
def cpu_power_plan():
    return _json(SystemRealityGuardService.cpu_power_plan())


@system_reality_guard_bp.route("/cpu/power-plan/preview", methods=["POST"])
@handle_errors
@log_requests
def cpu_power_plan_preview():
    return _json(SystemRealityGuardService.cpu_power_plan(_payload(), apply=False))


@system_reality_guard_bp.route("/cpu/power-plan/apply", methods=["POST"])
@handle_errors
@log_requests
def cpu_power_plan_apply():
    return _json(SystemRealityGuardService.cpu_power_plan(_payload(), apply=True))


@system_reality_guard_bp.route("/cpu/turbo/bios-checklist", methods=["GET"])
@handle_errors
def cpu_turbo_bios_checklist():
    return _json(SystemRealityGuardService.bios_checklist())


@system_reality_guard_bp.route("/msi/status", methods=["GET"])
@handle_errors
def msi_status():
    return _json(SystemRealityGuardService.msi_status())


@system_reality_guard_bp.route("/msi/recommendations", methods=["GET"])
@handle_errors
def msi_recommendations():
    return _json(SystemRealityGuardService.msi_status())


@system_reality_guard_bp.route("/security/reality-audit", methods=["GET"])
@handle_errors
def security_reality_audit():
    return _json(SystemRealityGuardService.reality_audit())


@system_reality_guard_bp.route("/security/reality-audit/run", methods=["POST"])
@handle_errors
@log_requests
def security_reality_audit_run():
    return _json(SystemRealityGuardService.reality_audit())


@system_reality_guard_bp.route("/security/wsl/status", methods=["GET"])
@handle_errors
def security_wsl_status():
    return _json(SystemRealityGuardService.wsl_status())


@system_reality_guard_bp.route("/security/remote-access/status", methods=["GET"])
@handle_errors
def security_remote_access_status():
    return _json(SystemRealityGuardService.remote_access_status())


@system_reality_guard_bp.route("/security/startup/status", methods=["GET"])
@handle_errors
def security_startup_status():
    return _json(SystemRealityGuardService.startup_status())


@system_reality_guard_bp.route("/security/powershell/activity", methods=["GET"])
@handle_errors
def security_powershell_activity():
    return _json(SystemRealityGuardService.powershell_activity())


@system_reality_guard_bp.route("/security/vendor-services/classify", methods=["GET"])
@handle_errors
def security_vendor_services_classify():
    return _json(SystemRealityGuardService.vendor_services_classify())


@system_reality_guard_bp.route("/system-reality/safety/evaluate", methods=["POST"])
@handle_errors
@log_requests
def system_reality_safety_evaluate():
    payload = _payload()
    action = str(payload.get("action_type") or payload.get("action") or "")
    decision = RealitySafetyGuard.evaluate_action(action, payload)
    status = "blocked" if not decision.get("allowed") else "preview"
    return _json(SystemRealityGuardService.response(status=status, data={"decision": decision}, blocked_reasons=[decision.get("blocked_reason")] if not decision.get("allowed") else [], ok=decision.get("allowed", False)))

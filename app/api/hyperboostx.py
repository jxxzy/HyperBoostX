"""HyperBoostX Triple AI Engine API contract."""

from flask import Blueprint, jsonify, request

from api.middleware import handle_errors, log_requests
from core.config import Config
from core.logger import Logger
from core.restore import RestoreManager
from services.ai.pc_scanner_service import PcScannerService
from services.ai.triple_ai_engine import TripleAiEngine
from services.optimization.tweak_service import TweakService


logger = Logger.get_logger(__name__)

hyperboostx_bp = Blueprint("hyperboostx", __name__)

scanner_service = PcScannerService()
triple_ai_engine = TripleAiEngine()


def _json_payload() -> dict:
    return request.get_json(silent=True) or {}


@hyperboostx_bp.route("/scan", methods=["POST"])
@hyperboostx_bp.route("/api/hyperboostx/scan", methods=["POST"])
@handle_errors
@log_requests
def scan_pc():
    """Run the MVP PC scanner."""
    result = scanner_service.scan_pc()
    return jsonify(result)


@hyperboostx_bp.route("/ai/analyze", methods=["POST"])
@hyperboostx_bp.route("/api/hyperboostx/ai/analyze", methods=["POST"])
@handle_errors
@log_requests
def analyze_scan():
    """Analyze a scan result using AI Analyzer with local fallback."""
    data = _json_payload()
    scan_id = data.get("scan_id") or ""
    scan_result = data.get("scan_result") or scanner_service.load_scan(scan_id)
    if not scan_result:
        return jsonify({"error": "scan_result or valid scan_id is required"}), 400

    result = triple_ai_engine.analyze_scan(
        scan_id=scan_id or scan_result.get("scan_id", ""),
        scan_result=scan_result,
        user_goal=data.get("user_goal", "safe_boost"),
    )
    return jsonify(result)


@hyperboostx_bp.route("/ai/safety-check", methods=["POST"])
@hyperboostx_bp.route("/api/hyperboostx/ai/safety-check", methods=["POST"])
@handle_errors
@log_requests
def safety_check():
    """Validate recommendations before any tweak can be applied."""
    data = _json_payload()
    recommendations = data.get("recommendations") or []
    result = triple_ai_engine.safety_check(recommendations)
    return jsonify(result)


@hyperboostx_bp.route("/doctor/run", methods=["POST"])
@hyperboostx_bp.route("/api/hyperboostx/doctor/run", methods=["POST"])
@handle_errors
@log_requests
def run_doctor_flow():
    """Run scan -> analyze -> safety -> assistant -> report without applying tweaks."""
    data = _json_payload()
    user_goal = data.get("user_goal", "safe_boost")
    scan_result = scanner_service.scan_pc()
    analysis = triple_ai_engine.analyze_scan(scan_result["scan_id"], scan_result, user_goal)
    safety = triple_ai_engine.safety_check(analysis.get("recommendations", []))
    assistant = triple_ai_engine.assistant_summary(scan_result, analysis, safety)
    report = triple_ai_engine.create_report(scan_result, analysis, safety)
    return jsonify(
        {
            "scan_id": scan_result["scan_id"],
            "scan_result": scan_result,
            "analysis": analysis,
            "safety": safety,
            "assistant": assistant,
            "report": report,
            "flow": "Scan PC -> AI Analyzer -> AI Safety Guard -> AI Assistant -> User Approval",
        }
    )


@hyperboostx_bp.route("/tweaks/apply", methods=["POST"])
@hyperboostx_bp.route("/api/hyperboostx/tweaks/apply", methods=["POST"])
@handle_errors
@log_requests
def apply_approved_tweaks():
    """Apply tweaks that already passed Safety Guard and user approval."""
    data = _json_payload()
    approved_tweaks = data.get("approved_tweaks") or []
    if not data.get("user_approved"):
        logger.info("user approval missing for safe tweak apply")
        return jsonify({"applied": [], "failed": [], "backup_id": "", "error": "user_approved must be true"}), 400

    second_pass = triple_ai_engine.safety_check(approved_tweaks)
    allowed = [
        item
        for item in second_pass.get("approved", [])
        if item.get("can_auto_apply") and item.get("safety_status") == "approved"
    ]

    if not allowed:
        return jsonify(
            {
                "applied": [],
                "failed": [],
                "backup_id": "",
                "safety": second_pass,
                "error": "No approved auto-apply tweaks are available.",
            }
        ), 400

    batch = RestoreManager.create_restore_point("safe_boost_batch", "HyperBoostX Safe Boost batch manifest")
    applied = []
    failed = []
    stop_after_failure = False

    for item in allowed:
        tweak_id = item.get("tweak_id") or item.get("id")
        if not tweak_id:
            failed.append({"tweak_id": "", "error": "Missing tweak_id"})
            continue
        if stop_after_failure:
            failed.append({"tweak_id": tweak_id, "error": "Skipped after previous failure."})
            continue

        logger.info("user approval received for tweak: %s", tweak_id)
        result = TweakService.apply_tweak(tweak_id, confirmed=True)
        record = {"tweak_id": tweak_id, "result": result}
        if result.get("success"):
            applied.append(record)
            batch.settings.append(
                {
                    "type": "safe_boost_tweak",
                    "tweak_id": tweak_id,
                    "restore_point": result.get("restore_point", f"tweak_{tweak_id}"),
                    "restore_timestamp": result.get("restore_timestamp", ""),
                }
            )
            RestoreManager.save_restore_point(batch)
            logger.info("tweak applied: %s", tweak_id)
        else:
            failed.append(record)
            logger.warning("tweak failed: %s", tweak_id)
            if item.get("risk_level") in {"medium", "high"}:
                stop_after_failure = True

    response = {
        "applied": applied,
        "failed": failed,
        "backup_id": batch.timestamp if applied else "",
        "safety": second_pass,
    }
    return jsonify(response)


@hyperboostx_bp.route("/tweaks/revert", methods=["POST"])
@hyperboostx_bp.route("/api/hyperboostx/tweaks/revert", methods=["POST"])
@handle_errors
@log_requests
def revert_tweaks():
    """Revert one or more tweaks from a Safe Boost backup batch."""
    data = _json_payload()
    backup_id = data.get("backup_id", "")
    tweak_ids = set(data.get("tweak_ids") or [])
    if not backup_id:
        return jsonify({"reverted": [], "failed": [{"error": "backup_id is required"}]}), 400

    batch = RestoreManager.find_restore_point_by_timestamp(backup_id)
    if not batch:
        return jsonify({"reverted": [], "failed": [{"error": f"Backup not found: {backup_id}"}]}), 404

    reverted = []
    failed = []
    for item in batch.settings:
        if item.get("type") != "safe_boost_tweak":
            continue
        tweak_id = item.get("tweak_id", "")
        if tweak_ids and tweak_id not in tweak_ids:
            continue

        point = None
        timestamp = item.get("restore_timestamp")
        if timestamp:
            point = RestoreManager.find_restore_point_by_timestamp(timestamp)
        if point is None:
            point = RestoreManager.find_latest_restore_point(f"tweak_{tweak_id}")

        if point and RestoreManager.restore(point):
            reverted.append({"tweak_id": tweak_id, "restore_timestamp": point.timestamp})
            logger.info("revert completed: %s", tweak_id)
        else:
            failed.append({"tweak_id": tweak_id, "error": "Restore point missing or failed."})
            logger.warning("revert failed: %s", tweak_id)

    return jsonify({"reverted": reverted, "failed": failed, "backup_id": backup_id})


@hyperboostx_bp.route("/game/optimize", methods=["POST"])
@hyperboostx_bp.route("/api/hyperboostx/game/optimize", methods=["POST"])
@handle_errors
@log_requests
def optimize_game():
    """Return a safe game optimization recommendation."""
    data = _json_payload()
    game_name = data.get("game_name") or data.get("game") or ""
    scan_id = data.get("scan_id") or ""
    scan_result = data.get("scan_result") or scanner_service.load_scan(scan_id)
    result = triple_ai_engine.optimize_game(game_name, scan_result)
    return jsonify(result)


@hyperboostx_bp.route("/reports/<report_id>", methods=["GET"])
@hyperboostx_bp.route("/api/hyperboostx/reports/<report_id>", methods=["GET"])
@handle_errors
def get_report(report_id: str):
    report = triple_ai_engine.load_report(report_id)
    if not report:
        return jsonify({"error": "Report not found"}), 404
    return jsonify(report)


@hyperboostx_bp.route("/config/ai", methods=["GET"])
@hyperboostx_bp.route("/api/hyperboostx/config/ai", methods=["GET"])
@handle_errors
def get_ai_config():
    return jsonify(
        {
            "provider": os_value("AI_PROVIDER", Config.AI_PROVIDER),
            "base_url": os_value("NVIDIA_BASE_URL", Config.NVIDIA_BASE_URL),
            "chat_endpoint": os_value("NVIDIA_CHAT_ENDPOINT", Config.NVIDIA_CHAT_ENDPOINT),
            "cloud_enabled": str(Config.get("ai_cloud_enabled", os_value("AI_CLOUD_ENABLED", "true"))).lower() in {"1", "true", "yes", "on"},
            "models": Config.NVIDIA_MODELS,
            "default_model": os_value("NVIDIA_DEFAULT_MODEL", Config.NVIDIA_DEFAULT_MODEL),
            "fallback_model": os_value("NVIDIA_FALLBACK_MODEL", Config.NVIDIA_FALLBACK_MODEL),
            "assistant_model": os_value("AI_ASSISTANT_MODEL", triple_ai_engine.ASSISTANT_MODEL),
            "analyzer_model": os_value("AI_ANALYZER_MODEL", triple_ai_engine.ANALYZER_MODEL),
            "safety_model": os_value("AI_SAFETY_MODEL", triple_ai_engine.SAFETY_MODEL),
            "embed_model": os_value("AI_EMBED_MODEL", triple_ai_engine.EMBED_MODEL),
            "auto_fallback": str(Config.get("ai_model_auto_fallback", os_value("AI_MODEL_AUTO_FALLBACK", "true"))).lower() in {"1", "true", "yes", "on"},
            "require_action_approval": str(Config.get("ai_require_action_approval", os_value("AI_REQUIRE_ACTION_APPROVAL", "true"))).lower() in {"1", "true", "yes", "on"},
            "safety_guard": str(Config.get("ai_enable_safety_guard", os_value("AI_ENABLE_SAFETY_GUARD", "true"))).lower() in {"1", "true", "yes", "on"},
            "api_key_present": bool(os_value("NVIDIA_API_KEY", "")),
        }
    )


def os_value(key: str, default: str) -> str:
    import os

    return os.environ.get(key, default)

"""Triple AI Engine API contract for HyperBoostX."""

from flask import Blueprint, jsonify, request

from api.middleware import handle_errors, log_requests
from core.config import Config
from core.logger import Logger
from services.ai.triple_ai_engine import TripleAIEngine


logger = Logger.get_logger(__name__)
triple_ai_bp = Blueprint("triple_ai", __name__)
triple_ai_engine = TripleAIEngine()


@triple_ai_bp.route("/scan", methods=["POST"])
@triple_ai_bp.route("/api/triple-ai/scan", methods=["POST"])
@triple_ai_bp.route("/api/hyperboostx/scan", methods=["POST"])
@handle_errors
@log_requests
def scan_pc():
    """Run Scan My PC and return the MVP scan contract."""
    return jsonify(triple_ai_engine.scan_pc())


@triple_ai_bp.route("/ai/analyze", methods=["POST"])
@triple_ai_bp.route("/api/triple-ai/analyze", methods=["POST"])
@triple_ai_bp.route("/api/hyperboostx/ai/analyze", methods=["POST"])
@handle_errors
@log_requests
def analyze_scan():
    """Run AI Analyzer over a scan result."""
    data = request.get_json(silent=True) or {}
    scan_result = data.get("scan_result") or {}
    if not scan_result:
        return jsonify({"error": "scan_result is required"}), 400

    result = triple_ai_engine.analyze(
        scan_result,
        user_goal=data.get("user_goal") or "gaming",
        game=data.get("game") or "",
    )
    return jsonify(result)


@triple_ai_bp.route("/ai/safety-check", methods=["POST"])
@triple_ai_bp.route("/api/triple-ai/safety-check", methods=["POST"])
@triple_ai_bp.route("/api/hyperboostx/ai/safety-check", methods=["POST"])
@handle_errors
@log_requests
def safety_check():
    """Run AI Safety Guard on recommendations."""
    data = request.get_json(silent=True) or {}
    recommendations = data.get("recommendations") or []
    if not isinstance(recommendations, list):
        return jsonify({"error": "recommendations must be a list"}), 400

    return jsonify(triple_ai_engine.safety_check(recommendations))


@triple_ai_bp.route("/ai/assistant", methods=["POST"])
@triple_ai_bp.route("/api/triple-ai/assistant", methods=["POST"])
@triple_ai_bp.route("/api/hyperboostx/ai/assistant", methods=["POST"])
@handle_errors
@log_requests
def assistant_response():
    """Return user-facing Assistant output grounded in Analyzer + Safety Guard."""
    data = request.get_json(silent=True) or {}
    return jsonify(triple_ai_engine.assistant_response(
        data.get("scan_result") or {},
        data.get("analysis_result") or {},
        data.get("safety_result") or {},
    ))


@triple_ai_bp.route("/api/triple-ai/full-flow", methods=["POST"])
@triple_ai_bp.route("/doctor/run", methods=["POST"])
@triple_ai_bp.route("/api/hyperboostx/doctor/run", methods=["POST"])
@handle_errors
@log_requests
def full_flow():
    """Convenience endpoint for Scan -> Analyze -> Safety -> Assistant -> Report."""
    data = request.get_json(silent=True) or {}
    return jsonify(triple_ai_engine.run_full_flow(
        user_goal=data.get("user_goal") or "gaming",
        game=data.get("game") or "",
    ))


@triple_ai_bp.route("/tweaks/apply", methods=["POST"])
@triple_ai_bp.route("/api/triple-ai/tweaks/apply", methods=["POST"])
@triple_ai_bp.route("/api/hyperboostx/tweaks/apply", methods=["POST"])
@handle_errors
@log_requests
def apply_safe_tweaks():
    """Apply only approved, reversible, low-risk tweaks after user approval."""
    data = request.get_json(silent=True) or {}
    approved_tweaks = data.get("approved_tweaks") or []
    if not isinstance(approved_tweaks, list):
        return jsonify({"error": "approved_tweaks must be a list"}), 400

    result = triple_ai_engine.apply_safe_tweaks(
        approved_tweaks,
        user_approved=bool(data.get("user_approved")),
    )
    return jsonify(result)


@triple_ai_bp.route("/tweaks/revert", methods=["POST"])
@triple_ai_bp.route("/api/triple-ai/tweaks/revert", methods=["POST"])
@triple_ai_bp.route("/api/hyperboostx/tweaks/revert", methods=["POST"])
@handle_errors
@log_requests
def revert_tweaks():
    """Revert applied tweaks by backup_id and/or tweak_ids."""
    data = request.get_json(silent=True) or {}
    tweak_ids = data.get("tweak_ids")
    if tweak_ids is not None and not isinstance(tweak_ids, list):
        return jsonify({"error": "tweak_ids must be a list"}), 400

    return jsonify(triple_ai_engine.revert_tweaks(
        backup_id=data.get("backup_id") or "",
        tweak_ids=tweak_ids,
    ))


@triple_ai_bp.route("/performance/report", methods=["POST"])
@triple_ai_bp.route("/api/triple-ai/performance/report", methods=["POST"])
@triple_ai_bp.route("/api/hyperboostx/performance/report", methods=["POST"])
@handle_errors
@log_requests
def performance_report():
    """Create a performance report from scan/analyze/safety payloads."""
    data = request.get_json(silent=True) or {}
    return jsonify(triple_ai_engine.create_performance_report(
        data.get("scan_result") or {},
        data.get("analysis_result") or {},
        data.get("safety_result") or {},
        data.get("assistant_result") or {},
    ))


@triple_ai_bp.route("/game/optimize", methods=["POST"])
@triple_ai_bp.route("/api/triple-ai/game/optimize", methods=["POST"])
@triple_ai_bp.route("/api/hyperboostx/game/optimize", methods=["POST"])
@handle_errors
@log_requests
def optimize_game():
    """Return safe game settings recommendations from the local knowledge base."""
    data = request.get_json(silent=True) or {}
    game = data.get("game_name") or data.get("game") or ""
    if not game:
        return jsonify({"error": "game is required"}), 400

    return jsonify(triple_ai_engine.optimize_game(
        game,
        data.get("scan_result") or {},
    ))


@triple_ai_bp.route("/api/triple-ai/models", methods=["GET"])
@triple_ai_bp.route("/api/hyperboostx/models", methods=["GET"])
@handle_errors
def models():
    """Return configured model targets and local fallback state."""
    return jsonify({
        "engine": "HyperBoostX Triple AI Engine",
        "provider": Config.AI_PROVIDER,
        "base_url": Config.NVIDIA_BASE_URL,
        "chat_endpoint": Config.NVIDIA_CHAT_ENDPOINT,
        "models": Config.NVIDIA_MODELS,
        "default_model": Config.NVIDIA_DEFAULT_MODEL,
        "fallback_model": Config.NVIDIA_FALLBACK_MODEL,
        "assistant_model": TripleAIEngine.ASSISTANT_MODEL,
        "analyzer_model": TripleAIEngine.ANALYZER_MODEL,
        "safety_model": TripleAIEngine.SAFETY_MODEL,
        "embed_model": TripleAIEngine.EMBED_MODEL,
        "auto_fallback": Config.AI_MODEL_AUTO_FALLBACK,
        "require_action_approval": Config.AI_REQUIRE_ACTION_APPROVAL,
        "safety_guard": Config.AI_ENABLE_SAFETY_GUARD,
        "cloud_enabled": triple_ai_engine._cloud_enabled(),
        "rag_layer": "local knowledge base",
    })

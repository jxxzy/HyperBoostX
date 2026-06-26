"""HyperBoostX v1.4 product feature API contract."""

from __future__ import annotations

from flask import Blueprint, jsonify, request

from api.middleware import handle_errors, log_requests
from services.product_features import (
    AutoGamingModeService,
    BenchmarkReportService,
    CleanupCenterService,
    DriverRecommendationService,
    EnterpriseLogService,
    FeatureAuditService,
    GameDatabaseService,
    GamingEssentialsService,
    GpuCenterService,
    HyperBoostScoreEngine,
    KnowledgeBaseService,
    NetworkToolsFacade,
    OverlayCenterService,
    PerformanceAdvisorService,
    PerformanceHistoryService,
    PluginRegistryService,
    ProcessAnalyzerService,
    ProtectionService,
    RestoreService,
    RgbDetectionService,
    StartupManagerFacade,
    StreamingCenterService,
    SystemProductInfoService,
    UiSettingsService,
)

product_v14_bp = Blueprint("product_v14", __name__, url_prefix="/api")


def _payload() -> dict:
    return request.get_json(silent=True) or {}


@product_v14_bp.route("/advisor/performance", methods=["GET", "POST"])
@handle_errors
def advisor_performance():
    return jsonify(PerformanceAdvisorService.analyze(_payload()))


@product_v14_bp.route("/knowledge/terms", methods=["GET"])
@handle_errors
def knowledge_terms():
    return jsonify(KnowledgeBaseService.list_terms())


@product_v14_bp.route("/knowledge/terms/<term_id>", methods=["GET"])
@handle_errors
def knowledge_term(term_id: str):
    payload = KnowledgeBaseService.get(term_id)
    status = 404 if payload.get("error") else 200
    return jsonify(payload), status


@product_v14_bp.route("/history/scans", methods=["GET", "POST"])
@handle_errors
@log_requests
def history_scans():
    if request.method == "POST":
        return jsonify(PerformanceHistoryService.record_scan(_payload()))
    return jsonify(PerformanceHistoryService.history())


@product_v14_bp.route("/history/timeline", methods=["GET"])
@handle_errors
def history_timeline():
    return jsonify(PerformanceHistoryService.timeline())


@product_v14_bp.route("/score/engine", methods=["GET"])
@handle_errors
def score_engine():
    return jsonify(HyperBoostScoreEngine.calculate())


@product_v14_bp.route("/games/library", methods=["GET"])
@handle_errors
def games_library():
    return jsonify(GameDatabaseService.library())


@product_v14_bp.route("/games/running", methods=["GET"])
@handle_errors
def games_running():
    return jsonify(GameDatabaseService.running())


@product_v14_bp.route("/games/scan", methods=["POST"])
@handle_errors
@log_requests
def games_scan():
    return jsonify(GameDatabaseService.scan())


@product_v14_bp.route("/games/add", methods=["POST"])
@handle_errors
@log_requests
def games_add():
    payload = GameDatabaseService.add_custom(_payload())
    return jsonify(payload), 400 if payload.get("error") else 200


@product_v14_bp.route("/games/remove", methods=["POST"])
@handle_errors
@log_requests
def games_remove():
    return jsonify(GameDatabaseService.remove_custom(_payload()))


@product_v14_bp.route("/games/profile/preview", methods=["POST"])
@handle_errors
@log_requests
def games_profile_preview():
    return jsonify(GameDatabaseService.profile_preview(_payload()))


@product_v14_bp.route("/games/profile/apply", methods=["POST"])
@handle_errors
@log_requests
def games_profile_apply():
    result = GameDatabaseService.profile_apply(_payload())
    return jsonify(result), 200 if result.get("success") else 409


@product_v14_bp.route("/games/profile/restore", methods=["POST"])
@handle_errors
@log_requests
def games_profile_restore():
    return jsonify(GameDatabaseService.profile_restore(_payload()))


@product_v14_bp.route("/games/session/latest", methods=["GET"])
@handle_errors
def games_session_latest():
    sessions = RestoreService.sessions().get("items", [])
    game_sessions = [item for item in sessions if item.get("kind") == "game_profile"]
    return jsonify(game_sessions[-1] if game_sessions else {"message": "No game profile sessions yet."})


@product_v14_bp.route("/games/session/history", methods=["GET"])
@handle_errors
def games_session_history():
    sessions = [item for item in RestoreService.sessions().get("items", []) if item.get("kind") == "game_profile"]
    return jsonify({"items": sessions})


@product_v14_bp.route("/games/session/export", methods=["POST"])
@handle_errors
@log_requests
def games_session_export():
    return jsonify(RestoreService.export())


@product_v14_bp.route("/overlays/status", methods=["GET"])
@handle_errors
def overlays_status():
    return jsonify(OverlayCenterService.status())


@product_v14_bp.route("/overlays/recommendations", methods=["GET"])
@handle_errors
def overlays_recommendations():
    return jsonify(OverlayCenterService.recommendations())


@product_v14_bp.route("/protection/processes", methods=["GET"])
@handle_errors
def protection_processes():
    return jsonify(ProtectionService.list_processes())


@product_v14_bp.route("/protection/add", methods=["POST"])
@handle_errors
@log_requests
def protection_add():
    payload = ProtectionService.add(_payload())
    return jsonify(payload), 400 if payload.get("error") else 200


@product_v14_bp.route("/protection/remove", methods=["POST"])
@handle_errors
@log_requests
def protection_remove():
    return jsonify(ProtectionService.remove(_payload()))


@product_v14_bp.route("/protection/reset-defaults", methods=["POST"])
@handle_errors
@log_requests
def protection_reset_defaults():
    return jsonify(ProtectionService.reset())


@product_v14_bp.route("/protection/evaluate-action", methods=["POST"])
@handle_errors
@log_requests
def protection_evaluate_action():
    return jsonify(ProtectionService.evaluate(_payload()))


@product_v14_bp.route("/processes/heavy", methods=["GET"])
@handle_errors
def processes_heavy():
    return jsonify(ProcessAnalyzerService.heavy())


@product_v14_bp.route("/processes/startup-impact", methods=["GET"])
@handle_errors
def processes_startup_impact():
    return jsonify(ProcessAnalyzerService.startup_impact())


@product_v14_bp.route("/processes/recommendations", methods=["GET"])
@handle_errors
def processes_recommendations():
    return jsonify(ProcessAnalyzerService.recommendations())


@product_v14_bp.route("/processes/export-report", methods=["POST"])
@handle_errors
@log_requests
def processes_export_report():
    return jsonify(ProcessAnalyzerService.export_report())


@product_v14_bp.route("/benchmark/manual", methods=["POST"])
@handle_errors
@log_requests
def benchmark_manual():
    return jsonify(BenchmarkReportService.manual(_payload()))


@product_v14_bp.route("/benchmark/import-csv", methods=["POST"])
@handle_errors
@log_requests
def benchmark_import_csv():
    return jsonify(BenchmarkReportService.import_csv(str(_payload().get("content") or "")))


@product_v14_bp.route("/benchmark/latest", methods=["GET"])
@handle_errors
def benchmark_latest():
    return jsonify(BenchmarkReportService.latest())


@product_v14_bp.route("/benchmark/history", methods=["GET"])
@handle_errors
def benchmark_history():
    return jsonify(BenchmarkReportService.history())


@product_v14_bp.route("/benchmark/export", methods=["POST"])
@handle_errors
@log_requests
def benchmark_export():
    return jsonify(BenchmarkReportService.export())


@product_v14_bp.route("/gpu/vendor-guide", methods=["GET"])
@handle_errors
def gpu_vendor_guide():
    return jsonify(GpuCenterService.vendor_guide())


@product_v14_bp.route("/gpu/recommendations", methods=["GET"])
@handle_errors
def gpu_recommendations():
    return jsonify(GpuCenterService.recommendations())


@product_v14_bp.route("/gpu/export-report", methods=["POST"])
@handle_errors
@log_requests
def gpu_export_report():
    return jsonify(GpuCenterService.export_report())


@product_v14_bp.route("/gpu/hardware-database", methods=["GET"])
@handle_errors
def gpu_hardware_database():
    return jsonify(GpuCenterService.hardware_database())


@product_v14_bp.route("/drivers/recommendation", methods=["GET"])
@handle_errors
def drivers_recommendation():
    return jsonify(DriverRecommendationService.status())


@product_v14_bp.route("/startup/items", methods=["GET"])
@handle_errors
def startup_items_v14():
    return jsonify(StartupManagerFacade.items())


@product_v14_bp.route("/startup/preview", methods=["POST"])
@handle_errors
@log_requests
def startup_preview_v14():
    return jsonify(StartupManagerFacade.preview(_payload()))


@product_v14_bp.route("/startup/apply", methods=["POST"])
@handle_errors
@log_requests
def startup_apply_v14():
    result = StartupManagerFacade.apply(_payload())
    return jsonify(result), 200 if result.get("success") else 409


@product_v14_bp.route("/startup/restore", methods=["POST"])
@handle_errors
@log_requests
def startup_restore_v14():
    return jsonify(StartupManagerFacade.restore(_payload()))


@product_v14_bp.route("/startup/export-report", methods=["POST"])
@handle_errors
@log_requests
def startup_export_report_v14():
    return jsonify(StartupManagerFacade.export_report())


@product_v14_bp.route("/cleanup/scan", methods=["GET"])
@handle_errors
def cleanup_scan():
    return jsonify(CleanupCenterService.scan())


@product_v14_bp.route("/cleanup/preview", methods=["POST"])
@handle_errors
@log_requests
def cleanup_preview():
    return jsonify(CleanupCenterService.preview(_payload()))


@product_v14_bp.route("/cleanup/apply", methods=["POST"])
@handle_errors
@log_requests
def cleanup_apply():
    result = CleanupCenterService.apply(_payload())
    return jsonify(result), 200 if result.get("success") else 409


@product_v14_bp.route("/cleanup/report", methods=["GET"])
@handle_errors
def cleanup_report():
    return jsonify(CleanupCenterService.report())


@product_v14_bp.route("/cleanup/export-report", methods=["POST"])
@handle_errors
@log_requests
def cleanup_export_report():
    return jsonify(CleanupCenterService.export_report())


@product_v14_bp.route("/network/diagnostics", methods=["GET"])
@handle_errors
def network_diagnostics():
    return jsonify(NetworkToolsFacade.diagnostics())


@product_v14_bp.route("/network/ping", methods=["POST"])
@handle_errors
@log_requests
def network_ping():
    return jsonify(NetworkToolsFacade.ping(_payload()))


@product_v14_bp.route("/network/dns-test", methods=["GET"])
@handle_errors
def network_dns_test_alias():
    return jsonify(NetworkToolsFacade.dns_test())


@product_v14_bp.route("/network/export-report", methods=["POST"])
@handle_errors
@log_requests
def network_export_report():
    return jsonify(NetworkToolsFacade.export_report())


@product_v14_bp.route("/essentials/list", methods=["GET"])
@handle_errors
def essentials_list():
    return jsonify(GamingEssentialsService.list())


@product_v14_bp.route("/essentials/check", methods=["GET"])
@handle_errors
def essentials_check():
    return jsonify(GamingEssentialsService.check())


@product_v14_bp.route("/essentials/install-preview", methods=["POST"])
@handle_errors
@log_requests
def essentials_install_preview():
    return jsonify(GamingEssentialsService.install_preview(_payload()))


@product_v14_bp.route("/essentials/install", methods=["POST"])
@handle_errors
@log_requests
def essentials_install():
    return jsonify(GamingEssentialsService.install(_payload())), 409


@product_v14_bp.route("/streaming/status", methods=["GET"])
@handle_errors
def streaming_status():
    return jsonify(StreamingCenterService.status())


@product_v14_bp.route("/rgb/status", methods=["GET"])
@handle_errors
def rgb_status():
    return jsonify(RgbDetectionService.status())


@product_v14_bp.route("/plugins/registry", methods=["GET"])
@handle_errors
def plugins_registry():
    return jsonify(PluginRegistryService.registry())


@product_v14_bp.route("/settings/ui", methods=["GET", "POST"])
@handle_errors
@log_requests
def settings_ui():
    if request.method == "POST":
        return jsonify(UiSettingsService.update(_payload()))
    return jsonify(UiSettingsService.get())


@product_v14_bp.route("/restore/sessions", methods=["GET"])
@handle_errors
def restore_sessions():
    return jsonify(RestoreService.sessions())


@product_v14_bp.route("/restore/session/<session_id>", methods=["GET"])
@handle_errors
def restore_session(session_id: str):
    payload = RestoreService.get(session_id)
    return jsonify(payload), 404 if payload.get("error") else 200


@product_v14_bp.route("/restore/session/<session_id>/preview", methods=["POST"])
@handle_errors
@log_requests
def restore_session_preview(session_id: str):
    return jsonify(RestoreService.preview(session_id))


@product_v14_bp.route("/restore/session/<session_id>/apply", methods=["POST"])
@handle_errors
@log_requests
def restore_session_apply(session_id: str):
    return jsonify(RestoreService.apply(session_id))


@product_v14_bp.route("/restore/session/<session_id>/verify", methods=["GET"])
@handle_errors
def restore_session_verify(session_id: str):
    return jsonify(RestoreService.verify(session_id))


@product_v14_bp.route("/restore/export", methods=["POST"])
@handle_errors
@log_requests
def restore_export():
    return jsonify(RestoreService.export())


@product_v14_bp.route("/auto-gaming/settings", methods=["GET"])
@handle_errors
def auto_gaming_settings():
    return jsonify(AutoGamingModeService.settings())


@product_v14_bp.route("/auto-gaming/preview", methods=["POST"])
@handle_errors
@log_requests
def auto_gaming_preview():
    return jsonify(AutoGamingModeService.preview(_payload()))


@product_v14_bp.route("/auto-gaming/apply", methods=["POST"])
@handle_errors
@log_requests
def auto_gaming_apply():
    result = AutoGamingModeService.apply(_payload())
    return jsonify(result), 200 if result.get("success") else 409


@product_v14_bp.route("/auto-gaming/restore", methods=["POST"])
@handle_errors
@log_requests
def auto_gaming_restore():
    return jsonify(AutoGamingModeService.restore(_payload()))


@product_v14_bp.route("/feature-audit/run", methods=["GET", "POST"])
@handle_errors
@log_requests
def feature_audit_run():
    return jsonify(FeatureAuditService.run())


@product_v14_bp.route("/product/storage", methods=["GET"])
@handle_errors
def product_storage():
    return jsonify(SystemProductInfoService.local_storage())


@product_v14_bp.route("/product/action-log", methods=["GET"])
@handle_errors
def product_action_log():
    return jsonify(EnterpriseLogService.latest())


@product_v14_bp.route("/product/v2-roadmap", methods=["GET"])
@handle_errors
def product_v2_roadmap():
    return jsonify(SystemProductInfoService.v2_roadmap())

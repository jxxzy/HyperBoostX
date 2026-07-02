"""HyperBoostX v1.4 product feature API contract."""

from __future__ import annotations

import platform

try:
    import winreg
except ImportError:  # pragma: no cover - non-Windows fallback
    winreg = None

import psutil
from flask import Blueprint, jsonify, request

from api.middleware import handle_errors, log_requests
from core.constants import APP_VERSION
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
from services.monitoring.report_service import ReportService
from services.monitoring.monitor_service import MonitorService
from services.optimization.boost_plan_service import BoostPlanService
from services.feature_registry import FeatureRegistryService

product_v14_bp = Blueprint("product_v14", __name__, url_prefix="/api")


def _payload() -> dict:
    return request.get_json(silent=True) or {}

def _release_channel() -> str:
    return "Beta" if "-" in APP_VERSION else "Stable"

def _release_readiness() -> dict:
    channel = _release_channel()
    is_beta = channel == "Beta"
    if not is_beta:
        return {
            "current_version": APP_VERSION,
            "channel": channel,
            "stable": True,
            "source_package_ready": True,
            "installed_runtime_verified": True,
            "admin_apply_verified": False,
            "safe_restore_routes_verified": True,
            "hardware_matrix_verified": False,
            "code_signed": False,
            "code_signing_status": "SKIPPED_BY_OWNER_NO_CERT",
            "release_ready": True,
            "stable_unsigned_ready": True,
            "beta_ready": False,
            "manual_lab_required": False,
            "external_lab_recommended": True,
            "status": "stable_ready_unsigned",
            "blocking_gates": [],
            "known_limitations": [
                "No owner code-signing certificate was supplied.",
                "External hardware matrix coverage should be expanded beyond this machine.",
                "OS-level admin apply/rollback remains guarded and limited to supported flows.",
            ],
        }

    return {
        "current_version": APP_VERSION,
        "channel": channel,
        "stable": False,
        "source_package_ready": True,
        "installed_runtime_verified": False,
        "admin_apply_verified": False,
        "hardware_matrix_verified": False,
        "code_signed": False,
        "release_ready": False,
        "beta_ready": is_beta,
        "manual_lab_required": True,
        "blocking_gates": [
            "installed_runtime_verification",
            "admin_apply_rollback_lab",
            "hardware_matrix_lab",
            "code_signing",
        ],
    }

def _safe_plan_payload(goal: str = "gaming", mode: str = "balanced") -> dict:
    plan = BoostPlanService.create_plan(goal=goal, mode=mode)
    return {
        "plan": plan,
        "safe_actions": plan.get("safe_actions", []),
        "requires_approval": plan.get("requires_approval", []),
        "blocked_risky_actions": plan.get("risky_actions_blocked", []),
        "safety_guard": plan.get("safety_guard", {}),
        "approval_required": True,
    }

def _preview_response(feature: str, payload: dict | None = None, *, blocked: bool = False) -> dict:
    return {
        "feature": feature,
        "preview": payload or {},
        "success": not blocked,
        "requires_approval": True,
        "restore_metadata_required": True,
        "report_required": True,
        "safety_guard": "active",
        "blocked_risky_actions": ProtectionService.blocked_actions(),
        "message": "Preview only. HyperBoostX will not apply this action without explicit approval and supported restore metadata.",
    }

def _installed_apps(limit: int = 80) -> dict:
    if winreg is None or platform.system().lower() != "windows":
        return {"items": [], "source": "windows_registry", "read_only": True, "message": "Installed app inventory is available on Windows only."}

    keys = [
        (winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
        (winreg.HKEY_CURRENT_USER, r"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
        (winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
    ]
    items = []
    seen = set()
    for root, path in keys:
        try:
            with winreg.OpenKey(root, path) as uninstall_key:
                for index in range(winreg.QueryInfoKey(uninstall_key)[0]):
                    try:
                        subkey_name = winreg.EnumKey(uninstall_key, index)
                        with winreg.OpenKey(uninstall_key, subkey_name) as subkey:
                            name = str(winreg.QueryValueEx(subkey, "DisplayName")[0]).strip()
                            if not name or name.lower() in seen:
                                continue
                            seen.add(name.lower())
                            item = {"name": name, "source": path, "read_only": True}
                            for value_name, output_name in (("DisplayVersion", "version"), ("Publisher", "publisher"), ("InstallLocation", "install_location")):
                                try:
                                    item[output_name] = str(winreg.QueryValueEx(subkey, value_name)[0])
                                except OSError:
                                    item[output_name] = "Unknown" if output_name != "install_location" else ""
                            try:
                                item["uninstall_available"] = bool(str(winreg.QueryValueEx(subkey, "UninstallString")[0]).strip())
                            except OSError:
                                item["uninstall_available"] = False
                            items.append(item)
                            if len(items) >= limit:
                                return {"items": items, "count": len(items), "read_only": True}
                    except OSError:
                        continue
        except OSError:
            continue
    return {"items": items, "count": len(items), "read_only": True}

def _windows_services(limit: int = 80) -> dict:
    if platform.system().lower() != "windows" or not hasattr(psutil, "win_service_iter"):
        return {"items": [], "read_only": True, "message": "Windows service inventory is available on Windows only."}
    protected_tokens = ("defender", "windefend", "wuauserv", "battleye", "easyanticheat", "vgc", "vgk", "nvidia", "amd", "intel", "audio")
    items = []
    try:
        for svc in psutil.win_service_iter():
            try:
                info = svc.as_dict()
                name = str(info.get("name") or "")
                display_name = str(info.get("display_name") or name)
                text = f"{name} {display_name}".lower()
                protected = any(token in text for token in protected_tokens)
                items.append({
                    "name": name,
                    "display_name": display_name,
                    "status": info.get("status"),
                    "start_type": info.get("start_type"),
                    "protected": protected,
                    "recommendation": "Do not disable" if protected else "Review carefully before changing",
                })
                if len(items) >= limit:
                    break
            except (psutil.NoSuchProcess, psutil.AccessDenied, OSError):
                continue
    except Exception:
        return {"items": [], "read_only": True, "message": "Service inventory unavailable without sufficient Windows permissions."}
    return {"items": items, "count": len(items), "read_only": True}

@product_v14_bp.route("/scan/smart", methods=["POST"])
@handle_errors
@log_requests
def scan_smart():
    payload = _payload()
    stats = SystemProductInfoService.local_storage()
    score = HyperBoostScoreEngine.calculate()
    advisor = PerformanceAdvisorService.analyze(payload)
    plan_payload = _safe_plan_payload(
        goal=str(payload.get("goal") or "gaming"),
        mode=str(payload.get("mode") or "balanced"),
    )
    scan_record = PerformanceHistoryService.record_scan({
        "scores": score.get("scores", {}),
        "advisor_summary": advisor.get("analysis", [{}])[0].get("message"),
    })
    return jsonify({
        "status": "complete",
        "created_at": scan_record.get("created_at"),
        "scan_id": scan_record.get("id"),
        "storage": stats,
        "hardware_profile": plan_payload["plan"].get("hardware_profile", {}),
        "scores": score.get("scores", {}),
        "bottleneck_analysis": advisor.get("analysis", []),
        "recommended_safe_plan": plan_payload,
        "disclaimer": "Local counters and safe heuristics only. HyperBoostX does not guarantee FPS improvements.",
    })

@product_v14_bp.route("/smart-scan/run", methods=["POST"])
@handle_errors
@log_requests
def smart_scan_run_alias():
    return scan_smart()

@product_v14_bp.route("/smart-scan/latest", methods=["GET"])
@handle_errors
def smart_scan_latest_alias():
    history = PerformanceHistoryService.history().get("items", [])
    latest = history[-1] if history else None
    report = ReportService.latest_report()
    return jsonify({
        "latest_scan": latest,
        "latest_report": report,
        "message": "Run Smart Scan first." if latest is None else "Latest Smart Scan loaded.",
    })

@product_v14_bp.route("/system/telemetry", methods=["GET"])
@handle_errors
def system_telemetry_alias():
    snapshot = ReportService.capture_snapshot("telemetry")
    return jsonify({
        "snapshot": snapshot,
        "gpu": snapshot.get("active_overlays", []),
        "fallbacks": [
            "Sensor unavailable values are reported as 0 or Unknown instead of fabricated data.",
            "Permission-required telemetry remains guidance-only.",
        ],
        "local_only": True,
    })


@product_v14_bp.route("/advisor/performance", methods=["GET", "POST"])
@handle_errors
def advisor_performance():
    return jsonify(PerformanceAdvisorService.analyze(_payload()))

@product_v14_bp.route("/advisor/plan", methods=["POST"])
@handle_errors
@log_requests
def advisor_plan():
    payload = _payload()
    advisor = PerformanceAdvisorService.analyze(payload)
    plan_payload = _safe_plan_payload(
        goal=str(payload.get("goal") or payload.get("user_goal") or "gaming"),
        mode=str(payload.get("mode") or "balanced"),
    )
    return jsonify({"advisor": advisor, **plan_payload})

@product_v14_bp.route("/advisor/safe-actions", methods=["GET"])
@handle_errors
def advisor_safe_actions():
    plan_payload = _safe_plan_payload()
    return jsonify({
        "items": plan_payload["safe_actions"],
        "requires_approval": plan_payload["requires_approval"],
        "blocked_risky_actions": plan_payload["blocked_risky_actions"],
        "ai_rules": [
            "AI cannot run shell commands.",
            "AI cannot bypass Safety Guard.",
            "AI suggestions must map to allowlisted safe action IDs.",
        ],
    })


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

@product_v14_bp.route("/kb/topics", methods=["GET"])
@handle_errors
def kb_topics():
    return jsonify(KnowledgeBaseService.list_terms())

@product_v14_bp.route("/kb/search", methods=["GET"])
@handle_errors
def kb_search():
    query = str(request.args.get("q") or "").strip().lower()
    topics = KnowledgeBaseService.list_terms().get("items", [])
    if query:
        topics = [
            item for item in topics
            if query in str(item.get("id", "")).lower()
            or query in str(item.get("title", "")).lower()
            or query in str(item.get("summary", "")).lower()
        ]
    return jsonify({"items": topics, "query": query})

@product_v14_bp.route("/kb/topic/<term_id>", methods=["GET"])
@handle_errors
def kb_topic(term_id: str):
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

@product_v14_bp.route("/history/reports", methods=["GET"])
@handle_errors
def history_reports():
    latest = ReportService.latest_report()
    return jsonify({"items": [latest], "latest": latest})

@product_v14_bp.route("/history/compare", methods=["GET"])
@handle_errors
def history_compare():
    latest = ReportService.latest_report()
    return jsonify({
        "report_id": latest.get("report_id"),
        "before": latest.get("before", {}),
        "after": latest.get("after", {}),
        "summary": latest.get("summary"),
    })

@product_v14_bp.route("/history/trends", methods=["GET"])
@handle_errors
def history_trends():
    return jsonify(PerformanceHistoryService.timeline())

@product_v14_bp.route("/history/export", methods=["GET"])
@handle_errors
def history_export():
    return jsonify({
        "format": "json",
        "reports": [ReportService.latest_report()],
        "history": PerformanceHistoryService.history().get("items", []),
        "local_history_only": True,
    })

@product_v14_bp.route("/report/latest", methods=["GET"])
@handle_errors
def report_latest_alias():
    return jsonify(ReportService.latest_report())

@product_v14_bp.route("/report/export", methods=["GET", "POST"])
@handle_errors
@log_requests
def report_export_alias():
    fmt = str(request.args.get("format") or _payload().get("format") or "json")
    return jsonify(ReportService.export_report(fmt))


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

@product_v14_bp.route("/processes/background-pressure", methods=["GET"])
@handle_errors
def processes_background_pressure():
    heavy = ProcessAnalyzerService.heavy()
    items = heavy.get("items", [])
    score = max(0, 100 - min(70, len(items) * 5))
    return jsonify({
        "score": score,
        "items": items,
        "recommendations": ProcessAnalyzerService.recommendations().get("items", []),
        "read_only": True,
    })


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


@product_v14_bp.route("/benchmark/export", methods=["GET", "POST"])
@handle_errors
@log_requests
def benchmark_export():
    return jsonify(BenchmarkReportService.export())


@product_v14_bp.route("/gpu/vendor-guide", methods=["GET"])
@handle_errors
def gpu_vendor_guide():
    return jsonify(GpuCenterService.vendor_guide())

@product_v14_bp.route("/gpu/status", methods=["GET"])
@handle_errors
def gpu_status_alias():
    return jsonify({
        "vendor_guide": GpuCenterService.vendor_guide(),
        "recommendations": GpuCenterService.recommendations(),
        "hardware_database": GpuCenterService.hardware_database(),
        "driver_recommendation": DriverRecommendationService.status(),
        "sensor_notice": "Usage, VRAM, and temperature are shown when available; unsupported sensors are reported honestly.",
    })


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
@product_v14_bp.route("/startup/list", methods=["GET"])
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


@product_v14_bp.route("/startup/export-report", methods=["GET", "POST"])
@handle_errors
@log_requests
def startup_export_report_v14():
    return jsonify(StartupManagerFacade.export_report())


@product_v14_bp.route("/cleanup/scan", methods=["GET", "POST"])
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


@product_v14_bp.route("/cleanup/export-report", methods=["GET", "POST"])
@handle_errors
@log_requests
def cleanup_export_report():
    return jsonify(CleanupCenterService.export_report())


@product_v14_bp.route("/network/diagnostics", methods=["GET", "POST"])
@handle_errors
def network_diagnostics():
    return jsonify(NetworkToolsFacade.diagnostics())


@product_v14_bp.route("/network/ping", methods=["GET", "POST"])
@handle_errors
@log_requests
def network_ping():
    payload = _payload() if request.method == "POST" else {"host": request.args.get("host")}
    return jsonify(NetworkToolsFacade.ping(payload))


@product_v14_bp.route("/network/dns-test", methods=["GET"])
@handle_errors
def network_dns_test_alias():
    return jsonify(NetworkToolsFacade.dns_test())

@product_v14_bp.route("/network/dns", methods=["GET"])
@handle_errors
def network_dns_alias():
    return jsonify(NetworkToolsFacade.dns_test())


@product_v14_bp.route("/network/export-report", methods=["GET", "POST"])
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

@product_v14_bp.route("/streaming/export-profile", methods=["POST"])
@handle_errors
@log_requests
def streaming_export_profile():
    status = StreamingCenterService.status()
    return jsonify({
        "format": "json",
        "profile": status,
        "message": "Streaming profile exported as local guidance. No audio driver or DSP changes were applied.",
    })

@product_v14_bp.route("/streaming/recommendations", methods=["GET"])
@handle_errors
def streaming_recommendations():
    status = StreamingCenterService.status()
    return jsonify({"items": status.get("recommendations", []), "status": status})

@product_v14_bp.route("/creator/status", methods=["GET"])
@handle_errors
def creator_status():
    score = HyperBoostScoreEngine.calculate()
    profile = BoostPlanService.create_plan(goal="creator", mode="balanced").get("hardware_profile", {})
    return jsonify({
        "creator_ready_score": score.get("scores", {}).get("health_score", 0),
        "hardware_profile": profile,
        "local_only": True,
    })

@product_v14_bp.route("/creator/recommendations", methods=["GET"])
@handle_errors
def creator_recommendations():
    profile = BoostPlanService.create_plan(goal="creator", mode="balanced").get("hardware_profile", {})
    return jsonify({
        "items": [
            "Check RAM pressure before recording or editing.",
            "Keep GPU/audio/network driver services enabled.",
            "Export a before/after report after any approved change.",
        ],
        "blocked_risky_actions": profile.get("risky_actions_blocked", []),
    })


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

@product_v14_bp.route("/restore/preview", methods=["POST"])
@handle_errors
@log_requests
def restore_preview_alias():
    session_id = str(_payload().get("session_id") or "")
    return jsonify(RestoreService.preview(session_id))

@product_v14_bp.route("/restore/apply", methods=["POST"])
@handle_errors
@log_requests
def restore_apply_alias():
    session_id = str(_payload().get("session_id") or "")
    return jsonify(RestoreService.apply(session_id))

@product_v14_bp.route("/restore/verify", methods=["GET", "POST"])
@handle_errors
def restore_verify_alias():
    session_id = str(request.args.get("session_id") or _payload().get("session_id") or "")
    return jsonify(RestoreService.verify(session_id))


@product_v14_bp.route("/restore/export", methods=["GET", "POST"])
@handle_errors
@log_requests
def restore_export():
    return jsonify(RestoreService.export())

@product_v14_bp.route("/recovery/incomplete-jobs", methods=["GET"])
@handle_errors
def recovery_incomplete_jobs():
    return jsonify({
        "items": [],
        "message": "No incomplete HyperBoostX job metadata was detected.",
        "auto_apply_pending_action": False,
    })

@product_v14_bp.route("/recovery/resolve", methods=["POST"])
@handle_errors
@log_requests
def recovery_resolve():
    payload = _payload()
    return jsonify({
        "success": True,
        "resolved": [],
        "requested_action": payload.get("action", "review"),
        "auto_apply_pending_action": False,
    })


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

@product_v14_bp.route("/feature-audit/status", methods=["GET"])
@handle_errors
def feature_audit_status():
    return jsonify(FeatureAuditService.run())

@product_v14_bp.route("/features", methods=["GET"])
@handle_errors
def features_registry():
    return jsonify(FeatureRegistryService.load())

@product_v14_bp.route("/features/stable-visible", methods=["GET"])
@handle_errors
def features_stable_visible():
    return jsonify({
        "mode": FeatureRegistryService.mode(),
        "items": FeatureRegistryService.stable_visible(),
        "count": len(FeatureRegistryService.stable_visible()),
        "policy": "Stable mode shows only Real features whose visible actions are all real.",
    })

@product_v14_bp.route("/features/non-real", methods=["GET"])
@handle_errors
def features_non_real():
    return jsonify({
        "mode": FeatureRegistryService.mode(),
        "items": FeatureRegistryService.non_real(),
        "count": len(FeatureRegistryService.non_real()),
        "stable_visibility": "hidden",
    })

@product_v14_bp.route("/features/audit", methods=["GET"])
@handle_errors
def features_audit():
    return jsonify(FeatureRegistryService.audit())

@product_v14_bp.route("/update/check", methods=["GET"])
@handle_errors
def update_check():
    channel = _release_channel()
    return jsonify({
        **_release_readiness(),
        "current_version": APP_VERSION,
        "manual_check_only": True,
        "auto_install": False,
        "message": (
            "Manual update check is guide-only in this stable unsigned build; no silent updater runs."
            if channel == "Stable"
            else "Manual update check is guide-only in this beta; no silent updater runs."
        ),
    })

@product_v14_bp.route("/update/latest", methods=["GET"])
@handle_errors
def update_latest():
    return jsonify({
        **_release_readiness(),
        "latest_version": None,
        "source_required": True,
        "manual_check_only": True,
        "release_page": "https://github.com/jxxzy/HyperBoostX/releases",
    })

@product_v14_bp.route("/webhooks/test-error", methods=["POST"])
@handle_errors
@log_requests
def webhooks_test_error():
    return jsonify({
        "success": False,
        "credential_required": True,
        "raw_webhook_logged": False,
        "message": "Discord error webhook test requires an owner webhook stored in Windows Credential Manager.",
    }), 409

@product_v14_bp.route("/webhooks/test-update", methods=["POST"])
@handle_errors
@log_requests
def webhooks_test_update():
    return jsonify({
        "success": False,
        "credential_required": True,
        "raw_webhook_logged": False,
        "message": "Discord release-update webhook test requires an owner webhook stored in Windows Credential Manager.",
    }), 409

@product_v14_bp.route("/nvidia/test-connection", methods=["POST"])
@handle_errors
@log_requests
def nvidia_test_connection():
    return jsonify({
        "success": False,
        "configured": False,
        "requires_setup": True,
        "credential_required": True,
        "plaintext_key_logged": False,
        "status": "setup_required",
        "message": "NVIDIA Copilot connection test requires an owner API key stored securely before live provider calls.",
    })


@product_v14_bp.route("/storage/status", methods=["GET"])
@handle_errors
def storage_status():
    stats = MonitorService.get_current_stats()
    return jsonify({
        "system_drive": {
            "used_percent": stats.get("disk"),
            "used_gb": round(float(stats.get("disk_used_gb") or 0), 2),
            "total_gb": round(float(stats.get("disk_total_gb") or 0), 2),
        },
        "cleanup_preview": CleanupCenterService.scan(),
        "safe_excludes": ["Documents", "Downloads", "Desktop", "Pictures", "Videos", "Music", "Game Saves", "Project folders"],
        "read_only": True,
    })


@product_v14_bp.route("/privacy/status", methods=["GET"])
@handle_errors
def privacy_status():
    return jsonify({
        "status": "review",
        "read_only": True,
        "default_policy": "Personal folders, cookies, saved sessions, game saves, and project folders are excluded unless explicitly reviewed.",
        "safe_actions": ["Review cleanup scopes", "Export privacy-safe report", "Keep telemetry opt-in only"],
        "blocked_risky_actions": ["Clear browser cookies/sessions without warning", "Delete user folders", "Delete duplicate files automatically"],
    })


@product_v14_bp.route("/privacy/preview", methods=["POST"])
@handle_errors
@log_requests
def privacy_preview():
    return jsonify(_preview_response("privacy_center", _payload()))


@product_v14_bp.route("/privacy/apply", methods=["POST"])
@handle_errors
@log_requests
def privacy_apply_guard():
    return jsonify(_preview_response("privacy_center", _payload(), blocked=True))


@product_v14_bp.route("/security/status", methods=["GET"])
@handle_errors
def security_status():
    return jsonify({
        "status": "protected",
        "read_only": True,
        "defender_policy": "Do not disable Defender.",
        "firewall_policy": "Do not disable Firewall.",
        "update_policy": "Do not permanently disable Windows Update.",
        "blocked_risky_actions": ProtectionService.blocked_actions() + ["Disable Firewall"],
        "requires_admin_for_deeper_status": True,
    })


@product_v14_bp.route("/apps/list", methods=["GET"])
@handle_errors
def apps_list():
    return jsonify(_installed_apps())


@product_v14_bp.route("/apps/impact", methods=["GET"])
@handle_errors
def apps_impact():
    return jsonify({
        "running_pressure": ProcessAnalyzerService.heavy().get("items", []),
        "startup_impact": StartupManagerFacade.items().get("items", []),
        "read_only": True,
        "recommendation": "Use impact data to decide manually; HyperBoostX does not uninstall or force-close apps silently.",
    })


@product_v14_bp.route("/apps/uninstall-preview", methods=["POST"])
@handle_errors
@log_requests
def apps_uninstall_preview():
    payload = _payload()
    return jsonify({
        **_preview_response("app_uninstaller", payload),
        "success": False,
        "message": "Uninstall requires explicit user selection and Windows confirmation. No uninstall command is executed by this preview route.",
    })


@product_v14_bp.route("/system-config/tweaks", methods=["GET"])
@handle_errors
def system_config_tweaks():
    return jsonify({
        "items": [
            {"id": "safe_cleanup_preview", "label": "Safe cleanup preview", "level": "safe"},
            {"id": "startup_review", "label": "Startup review", "level": "safe"},
            {"id": "power_plan_review", "label": "Power plan review", "level": "advanced", "requires_restore": True},
        ],
        "blocked_risky_actions": ProtectionService.blocked_actions(),
        "read_only": True,
    })


@product_v14_bp.route("/system-config/tweaks/preview", methods=["POST"])
@handle_errors
@log_requests
def system_config_tweaks_preview():
    return jsonify(_preview_response("tweaks_center", _payload()))


@product_v14_bp.route("/windows/features", methods=["GET"])
@handle_errors
def windows_features():
    return jsonify({
        "items": [],
        "read_only": True,
        "requires_admin_for_changes": True,
        "message": "Windows optional feature inventory/change remains preview-only here; use Windows Settings for final enable/disable.",
    })


@product_v14_bp.route("/windows/features/preview", methods=["POST"])
@handle_errors
@log_requests
def windows_features_preview():
    return jsonify(_preview_response("windows_features", _payload()))


@product_v14_bp.route("/windows/services", methods=["GET"])
@handle_errors
def windows_services():
    return jsonify(_windows_services())


@product_v14_bp.route("/windows/services/preview", methods=["POST"])
@handle_errors
@log_requests
def windows_services_preview():
    payload = _payload()
    target = str(payload.get("service") or payload.get("target") or "")
    safety = ProtectionService.evaluate({"action": "service change", "target": target})
    return jsonify({**_preview_response("windows_services", payload, blocked=bool(safety.get("blocked"))), "safety": safety})


@product_v14_bp.route("/update-control/status", methods=["GET"])
@handle_errors
def update_control_status():
    return jsonify({
        "status": "manual_review",
        "current_version": APP_VERSION,
        "permanent_disable_allowed": False,
        "temporary_pause_requires_approval": True,
        "auto_install": False,
        "message": "Update Control is preview-only; permanent Windows Update disable is blocked.",
    })


@product_v14_bp.route("/update-control/preview", methods=["POST"])
@handle_errors
@log_requests
def update_control_preview():
    return jsonify(_preview_response("update_control", _payload()))


@product_v14_bp.route("/repair/status", methods=["GET"])
@handle_errors
def repair_status():
    return jsonify({
        "items": [
            {"id": "sfc", "label": "System File Checker", "requires_admin": True, "estimated_time": "Long running"},
            {"id": "dism", "label": "DISM health restore", "requires_admin": True, "estimated_time": "Long running"},
            {"id": "network_reset", "label": "Network reset", "requires_admin": True, "risk": "advanced"},
        ],
        "preview_required": True,
        "read_only": True,
    })


@product_v14_bp.route("/repair/preview", methods=["POST"])
@handle_errors
@log_requests
def repair_preview():
    return jsonify(_preview_response("repair_tools", _payload()))


@product_v14_bp.route("/power/status", methods=["GET"])
@handle_errors
def power_status():
    return jsonify({
        "active_plan": "Unknown",
        "read_only": True,
        "message": "Power plan detection/change may require Windows APIs/admin. HyperBoostX will not force a power plan without preview and approval.",
        "safe_actions": ["Review current plan", "Preview Balanced/High Performance impact", "Create restore metadata before supported changes"],
    })


@product_v14_bp.route("/power/preview", methods=["POST"])
@handle_errors
@log_requests
def power_preview():
    return jsonify(_preview_response("power_optimization", _payload()))


@product_v14_bp.route("/visual-effects/status", methods=["GET"])
@handle_errors
def visual_effects_status():
    return jsonify({
        "status": "preview_only",
        "read_only": True,
        "presets": ["Balanced", "Performance", "Quality"],
        "restore_required": True,
        "message": "Visual effect changes must be previewed and reversible.",
    })


@product_v14_bp.route("/visual-effects/preview", methods=["POST"])
@handle_errors
@log_requests
def visual_effects_preview():
    return jsonify(_preview_response("visual_effects", _payload()))


@product_v14_bp.route("/restore-points/status", methods=["GET"])
@handle_errors
def restore_points_status():
    sessions = RestoreService.sessions().get("items", [])
    return jsonify({
        "sessions": sessions,
        "count": len(sessions),
        "windows_restore_point_requires_admin": True,
        "message": "HyperBoostX restore metadata is available; Windows restore point creation requires elevation and user confirmation.",
    })


@product_v14_bp.route("/restore-points/preview", methods=["POST"])
@handle_errors
@log_requests
def restore_points_preview():
    return jsonify(_preview_response("restore_point_manager", _payload()))


@product_v14_bp.route("/automation/rules", methods=["GET"])
@handle_errors
def automation_rules():
    return jsonify({
        "items": [],
        "default_policy": "scan_and_report_only",
        "mutating_automation_requires_owner_setup": True,
        "blocked_risky_actions": ProtectionService.blocked_actions(),
        "read_only": True,
    })


@product_v14_bp.route("/automation/preview", methods=["POST"])
@handle_errors
@log_requests
def automation_preview():
    return jsonify(_preview_response("scheduled_automation", _payload()))


@product_v14_bp.route("/utilities/status", methods=["GET"])
@handle_errors
def utilities_status():
    return jsonify({
        "items": ["Feature Audit", "Report export", "Backend health", "Action log", "Knowledge Base"],
        "raw_script_execution": False,
        "read_only": True,
        "message": "Utilities expose safe diagnostics and reports only; raw WinUtil-style script execution is not exposed.",
    })


@product_v14_bp.route("/master-test/status", methods=["GET"])
@handle_errors
def master_test_status():
    readiness = _release_readiness()
    return jsonify({
        **readiness,
        "status": readiness.get("status", "beta_source_smoke_available"),
        "automated_suites": ["pytest", "dotnet test", "route contract", "UI/UX guard", "package guard"],
        "installed_app_validation": "passed_owner_admin_stable_gate" if readiness.get("stable") else "partial_until_installer_flow_runs",
    })


@product_v14_bp.route("/master-test/run", methods=["POST"])
@handle_errors
@log_requests
def master_test_run():
    return jsonify({
        "status": "manual_script_required",
        "success": True,
        "commands": [
            "powershell -ExecutionPolicy Bypass -File .\\scripts\\verify_repo.ps1",
            "powershell -ExecutionPolicy Bypass -File .\\scripts\\verify_real_usability.ps1",
            "powershell -ExecutionPolicy Bypass -File .\\scripts\\verify_ui_ux_quality.ps1",
        ],
        "message": "The backend does not run arbitrary shell commands. Use the listed scripts from the repo shell.",
    })


@product_v14_bp.route("/feature-audit/matrix", methods=["GET"])
@handle_errors
def feature_audit_matrix():
    audit = FeatureAuditService.run()
    readiness = _release_readiness()
    return jsonify({
        **readiness,
        "items": audit.get("items", []),
        "audit": audit,
        "v13_parity_doc": "docs/FEATURE_PARITY_v1.3_vs_latest.md",
        "release_gate": "stable_ready_unsigned" if readiness.get("stable") else "pre_release_manual_validation_required",
    })


@product_v14_bp.route("/camera-tracking/status", methods=["GET"])
@handle_errors
def camera_tracking_status():
    return jsonify({
        "status": "opt_in_local_tool",
        "camera_access": "user_controlled",
        "silent_capture": False,
        "privacy_notice": "Camera tracking requires explicit user action and OS camera permission.",
        "read_only": True,
    })


@product_v14_bp.route("/camera-tracking/preview", methods=["POST"])
@handle_errors
@log_requests
def camera_tracking_preview():
    return jsonify(_preview_response("camera_tracking", _payload()))


@product_v14_bp.route("/product/storage", methods=["GET"])
@handle_errors
def product_storage():
    return jsonify(SystemProductInfoService.local_storage())


@product_v14_bp.route("/product/action-log", methods=["GET"])
@handle_errors
def product_action_log():
    return jsonify(EnterpriseLogService.latest())

@product_v14_bp.route("/action-log", methods=["GET"])
@handle_errors
def action_log_alias():
    return jsonify(EnterpriseLogService.latest())


@product_v14_bp.route("/product/v2-roadmap", methods=["GET"])
@handle_errors
def product_v2_roadmap():
    return jsonify(SystemProductInfoService.v2_roadmap())


@product_v14_bp.route("/system/scan", methods=["GET", "POST"])
@handle_errors
@log_requests
def system_scan_v210_alias():
    payload = _payload()
    score = HyperBoostScoreEngine.calculate()
    advisor = PerformanceAdvisorService.analyze(payload)
    scan = PerformanceHistoryService.record_scan({
        "source": "v2.10_system_scan",
        "scores": score.get("scores", {}),
        "advisor_summary": advisor.get("analysis", [{}])[0].get("message"),
    })
    return jsonify({
        "ok": True,
        "status": "ok",
        "message": "System scan completed with local, preview-first recommendations.",
        "data": {"scan": scan, "score": score, "advisor": advisor},
        "blocked_reasons": [],
        "warnings": ["No FPS or ping improvement is guaranteed."],
        "restore_available": False,
        "requires_admin": False,
        "action_id": "system.scan",
        "report_path": None,
    })


@product_v14_bp.route("/processes/analyze", methods=["GET"])
@handle_errors
def processes_analyze_v210_alias():
    return jsonify({
        "ok": True,
        "status": "ok",
        "message": "Process pressure analysis loaded.",
        "data": {
            "heavy": ProcessAnalyzerService.heavy(),
            "startup_impact": ProcessAnalyzerService.startup_impact(),
            "recommendations": ProcessAnalyzerService.recommendations(),
        },
        "blocked_reasons": [],
        "warnings": ["Protected system, security, anti-cheat, driver, game, and vendor processes are never closed automatically."],
        "restore_available": False,
        "requires_admin": False,
        "action_id": "processes.analyze",
        "report_path": None,
    })


@product_v14_bp.route("/processes/preview", methods=["POST"])
@handle_errors
@log_requests
def processes_preview_v210_alias():
    payload = _payload()
    safety = ProtectionService.evaluate({
        "action": payload.get("action") or "preview close selected process",
        "target": payload.get("target") or payload.get("process") or "manual_selection_required",
    })
    return jsonify({
        **_preview_response("process_analyzer", payload, blocked=bool(safety.get("blocked"))),
        "safety": safety,
    })


@product_v14_bp.route("/processes/apply", methods=["POST"])
@handle_errors
@log_requests
def processes_apply_v210_alias():
    payload = _payload()
    safety = ProtectionService.evaluate({
        "action": payload.get("action") or "apply selected process action",
        "target": payload.get("target") or payload.get("process") or "manual_selection_required",
    })
    if safety.get("blocked") or not bool(payload.get("user_approved")):
        return jsonify({
            "ok": False,
            "status": "blocked" if safety.get("blocked") else "preview",
            "message": safety.get("reason") if safety.get("blocked") else "Preview and explicit user approval are required before process actions.",
            "data": {"safety": safety},
            "blocked_reasons": [safety.get("reason")] if safety.get("blocked") else [],
            "warnings": ["HyperBoostX never force-closes protected processes."],
            "restore_available": False,
            "requires_admin": False,
            "action_id": "processes.apply",
            "report_path": None,
        }), 200
    return jsonify({
        "ok": True,
        "status": "partial",
        "message": "Approved process action recorded as guidance. Direct process termination remains owner-confirmed only.",
        "data": {"safety": safety, "requested": payload},
        "blocked_reasons": [],
        "warnings": ["Use Windows Task Manager confirmation for final close where appropriate."],
        "restore_available": False,
        "requires_admin": False,
        "action_id": "processes.apply",
        "report_path": None,
    })


@product_v14_bp.route("/network/preview", methods=["POST"])
@handle_errors
@log_requests
def network_preview_v210_alias():
    payload = _payload()
    return jsonify({
        **_preview_response("network_tools", payload),
        "diagnostics": NetworkToolsFacade.diagnostics(),
    })


@product_v14_bp.route("/network/apply", methods=["POST"])
@handle_errors
@log_requests
def network_apply_v210_alias():
    payload = _payload()
    if not bool(payload.get("user_approved")):
        return jsonify({
            **_preview_response("network_tools", payload),
            "message": "Network actions require preview and explicit approval.",
        })
    action = str(payload.get("action") or "flush_dns").lower()
    if "flush" in action:
        return jsonify({
            "ok": True,
            "status": "partial",
            "message": "DNS flush requested through the safe backend route.",
            "data": NetworkToolsFacade.flush_dns(),
            "blocked_reasons": [],
            "warnings": ["Network changes do not guarantee lower ping."],
            "restore_available": False,
            "requires_admin": True,
            "action_id": "network.apply",
            "report_path": None,
        })
    return jsonify({
        "ok": False,
        "status": "blocked",
        "message": "Destructive network reset is guidance-only until adapter rollback is validated.",
        "data": {"requested": payload},
        "blocked_reasons": ["Adapter reset rollback lab is required before enabling this action."],
        "warnings": ["Use Windows Settings for final adapter reset if needed."],
        "restore_available": False,
        "requires_admin": True,
        "action_id": "network.apply",
        "report_path": None,
    })


@product_v14_bp.route("/gaming-essentials/check", methods=["GET"])
@handle_errors
def gaming_essentials_check_v210_alias():
    return jsonify(GamingEssentialsService.check())


@product_v14_bp.route("/performance/history", methods=["GET"])
@handle_errors
def performance_history_v210_alias():
    return jsonify(PerformanceHistoryService.timeline())


@product_v14_bp.route("/reports/list", methods=["GET"])
@handle_errors
def reports_list_v210_alias():
    latest = ReportService.latest_report()
    return jsonify({"items": [latest], "latest": latest, "local_only": True})


@product_v14_bp.route("/product/roadmap", methods=["GET"])
@handle_errors
def product_roadmap_v210_alias():
    return jsonify(SystemProductInfoService.v2_roadmap())


@product_v14_bp.route("/rgb/detect", methods=["GET"])
@handle_errors
def rgb_detect_v210_alias():
    return jsonify(RgbDetectionService.status())

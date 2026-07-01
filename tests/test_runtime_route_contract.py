import json
from pathlib import Path

from app.backend_server import HyperBoostBackendServer
from services.product_features import ProcessAnalyzerService, RestoreService


REPO_ROOT = Path(__file__).resolve().parents[1]


def _expected_version():
    return (REPO_ROOT / "VERSION").read_text(encoding="utf-8").strip()


def _expected_channel(version):
    return "Beta" if "-" in version else "Stable"


def test_background_pressure_excludes_idle_process_and_normalizes_cpu(monkeypatch):
    class FakeMemory:
        rss = 256 * 1024 * 1024

    class FakeProcess:
        def __init__(self, pid, name, cpu_percent):
            self.info = {
                "pid": pid,
                "name": name,
                "cpu_percent": cpu_percent,
                "memory_info": FakeMemory(),
            }

    monkeypatch.setattr(
        "services.product_features.psutil.cpu_count",
        lambda logical=True: 12,
    )
    monkeypatch.setattr(
        "services.product_features.psutil.process_iter",
        lambda fields: iter([
            FakeProcess(0, "System Idle Process", 1200.0),
            FakeProcess(4, "System", 240.0),
            FakeProcess(1234, "Game.exe", 180.0),
        ]),
    )

    payload = ProcessAnalyzerService.heavy(limit=10)

    names = [item["name"] for item in payload["items"]]
    assert "System Idle Process" not in names
    assert all(item["pid"] != 0 for item in payload["items"])
    assert all(0 <= item["cpu_percent"] <= 100 for item in payload["items"])
    assert any(item["name"] == "Game.exe" and item["cpu_percent"] == 15.0 for item in payload["items"])


def test_runtime_audit_route_contract(monkeypatch, tmp_path):
    monkeypatch.setenv("HYPERBOOSTX_PORTABLE_HOME", str(tmp_path))
    monkeypatch.delenv("HYPERBOOSTX_SESSION_TOKEN", raising=False)
    monkeypatch.setattr(
        "services.optimization.network_service.NetworkService.test_dns",
        staticmethod(lambda: {"response_time": 1.0, "status": "Good"}),
    )
    monkeypatch.setattr(
        "services.optimization.network_service.NetworkService.flush_dns",
        staticmethod(lambda: {"success": True, "output": "mocked"}),
    )
    monkeypatch.setattr(
        "services.optimization.startup_service.StartupService.get_startup_items",
        lambda self: [],
    )

    server = HyperBoostBackendServer()
    client = server.app.test_client()
    session = RestoreService.create_session("route_contract", {"safe_only": True})
    session_id = session["id"]

    route_cases = [
        ("GET", "/api/health", None),
        ("GET", "/api/version", None),
        ("GET", "/api/release/readiness", None),
        ("GET", "/api/features", None),
        ("GET", "/api/features/audit", None),
        ("GET", "/api/features/stable-visible", None),
        ("GET", "/api/features/non-real", None),
        ("GET", "/api/system/stats", None),
        ("GET", "/api/system/info", None),
        ("GET", "/api/system/cpu", None),
        ("GET", "/api/system/ram", None),
        ("GET", "/api/system/processes", None),
        ("GET", "/api/system/telemetry", None),
        ("GET", "/api/hardware/profile", None),
        ("GET", "/api/hardware/gpu", None),
        ("GET", "/api/hardware/vendors", None),
        ("GET", "/api/gpu/vendor-guide", None),
        ("GET", "/api/gpu/status", None),
        ("GET", "/api/gpu/recommendations", None),
        ("GET", "/api/drivers/recommendation", None),
        ("POST", "/api/scan/smart", {"goal": "gaming"}),
        ("POST", "/api/smart-scan/run", {"goal": "gaming"}),
        ("GET", "/api/scan/latest", None),
        ("GET", "/api/smart-scan/latest", None),
        ("POST", "/api/boost/plan", {"goal": "gaming"}),
        ("POST", "/api/boost/apply", {"user_approved": True}),
        ("POST", "/api/boost/undo", {}),
        ("GET", "/api/advisor/performance", None),
        ("POST", "/api/advisor/performance", {"goal": "gaming"}),
        ("POST", "/api/advisor/plan", {"goal": "gaming"}),
        ("GET", "/api/advisor/safe-actions", None),
        ("GET", "/api/games/library", None),
        ("GET", "/api/games/running", None),
        ("POST", "/api/games/add", {"name": "Manual Test Game"}),
        ("POST", "/api/games/remove", {"id": "manual_test_game"}),
        ("POST", "/api/games/profile/preview", {"game_id": "valorant"}),
        ("POST", "/api/games/profile/apply", {"game_id": "valorant", "user_approved": True}),
        ("POST", "/api/games/profile/restore", {"session_id": session_id}),
        ("GET", "/api/games/session/history", None),
        ("GET", "/api/protection/processes", None),
        ("POST", "/api/protection/evaluate-action", {"action": "review overlay"}),
        ("POST", "/api/protection/add", {"name": "RouteContract.exe"}),
        ("POST", "/api/protection/remove", {"name": "RouteContract.exe"}),
        ("POST", "/api/protection/reset-defaults", {}),
        ("GET", "/api/overlays/status", None),
        ("GET", "/api/overlays/recommendations", None),
        ("GET", "/api/processes/heavy", None),
        ("GET", "/api/processes/startup-impact", None),
        ("GET", "/api/processes/background-pressure", None),
        ("GET", "/api/processes/recommendations", None),
        ("POST", "/api/process/close-selected", {}),
        ("POST", "/api/benchmark/manual", {"game": "Route Contract", "avg_fps": 120}),
        ("POST", "/api/benchmark/import-csv", {"content": "game,avg_fps\nRoute,144\n"}),
        ("GET", "/api/benchmark/history", None),
        ("GET", "/api/benchmark/latest", None),
        ("GET", "/api/benchmark/export", None),
        ("GET", "/api/history/reports", None),
        ("GET", "/api/history/compare", None),
        ("GET", "/api/history/trends", None),
        ("GET", "/api/history/export", None),
        ("GET", "/api/reports/latest", None),
        ("POST", "/api/reports/export", {"format": "json"}),
        ("GET", "/api/report/latest", None),
        ("POST", "/api/report/export", {"format": "json"}),
        ("GET", "/api/startup/items", None),
        ("GET", "/api/startup/list", None),
        ("POST", "/api/startup/disable", {}),
        ("POST", "/api/startup/enable", {}),
        ("POST", "/api/startup/preview", {"items": []}),
        ("POST", "/api/startup/apply", {"items": [], "user_approved": True}),
        ("POST", "/api/startup/restore", {"session_id": session_id}),
        ("GET", "/api/startup/export-report", None),
        ("GET", "/api/cleanup/scan", None),
        ("POST", "/api/cleanup/scan", {}),
        ("POST", "/api/cleanup/preview", {"categories": []}),
        ("POST", "/api/cleanup/apply", {"user_approved": True}),
        ("GET", "/api/cleanup/report", None),
        ("GET", "/api/cleanup/export-report", None),
        ("GET", "/api/network/diagnostics", None),
        ("POST", "/api/network/diagnostics", {}),
        ("GET", "/api/network/ping?host=1.1.1.1", None),
        ("GET", "/api/network/dns-test", None),
        ("GET", "/api/network/dns", None),
        ("POST", "/api/network/dns/benchmark", {}),
        ("POST", "/api/network/dns/apply", {}),
        ("POST", "/api/network/dns/restore", {}),
        ("POST", "/api/network/flush-dns", {}),
        ("GET", "/api/network/export-report", None),
        ("GET", "/api/storage/status", None),
        ("GET", "/api/storage/summary", None),
        ("GET", "/api/network/adapters", None),
        ("GET", "/api/privacy/status", None),
        ("POST", "/api/privacy/preview", {"scope": "cache_only"}),
        ("POST", "/api/privacy/apply", {"scope": "cache_only"}),
        ("GET", "/api/security/status", None),
        ("GET", "/api/apps/list", None),
        ("GET", "/api/apps/impact", None),
        ("POST", "/api/apps/uninstall-preview", {"app_id": "manual_selection_required"}),
        ("GET", "/api/system-config/tweaks", None),
        ("POST", "/api/system-config/tweaks/preview", {"tweak_id": "safe_preview"}),
        ("GET", "/api/windows/features", None),
        ("POST", "/api/windows/features/preview", {"feature": "manual_selection_required"}),
        ("POST", "/api/windows/features/plan", {}),
        ("GET", "/api/windows/services", None),
        ("GET", "/api/services", None),
        ("POST", "/api/services/start", {}),
        ("POST", "/api/services/stop", {}),
        ("POST", "/api/windows/services/preview", {"service": "manual_selection_required"}),
        ("GET", "/api/update-control/status", None),
        ("POST", "/api/update-control/preview", {"mode": "temporary_pause"}),
        ("GET", "/api/repair/status", None),
        ("POST", "/api/repair/preview", {"tool": "sfc"}),
        ("POST", "/api/repair/sfc-scan", {}),
        ("POST", "/api/repair/dism-checkhealth", {}),
        ("POST", "/api/repair/dism-scanhealth", {}),
        ("POST", "/api/repair/dism-restorehealth", {}),
        ("POST", "/api/repair/chkdsk-check", {}),
        ("GET", "/api/power/status", None),
        ("GET", "/api/power/plans", None),
        ("GET", "/api/power/active", None),
        ("POST", "/api/power/apply", {}),
        ("POST", "/api/power/restore", {}),
        ("POST", "/api/power/preview", {"plan": "balanced"}),
        ("GET", "/api/visual-effects/status", None),
        ("POST", "/api/visual-effects/preview", {"preset": "balanced"}),
        ("POST", "/api/visual-effects/apply", {}),
        ("POST", "/api/visual-effects/restore", {}),
        ("GET", "/api/restore-points/status", None),
        ("POST", "/api/restore-points/preview", {"action": "create"}),
        ("GET", "/api/automation/rules", None),
        ("GET", "/api/automation/tasks", None),
        ("POST", "/api/automation/tasks", {"template": "weekly_safe_cleanup_scan"}),
        ("POST", "/api/automation/tasks/missing/enable", {}),
        ("POST", "/api/automation/tasks/missing/disable", {}),
        ("DELETE", "/api/automation/tasks/missing", None),
        ("POST", "/api/automation/preview", {"rule": "scan_report_only"}),
        ("GET", "/api/utilities/status", None),
        ("GET", "/api/master-test/status", None),
        ("POST", "/api/master-test/run", {"suite": "smoke"}),
        ("GET", "/api/feature-audit/matrix", None),
        ("GET", "/api/camera-tracking/status", None),
        ("POST", "/api/camera-tracking/preview", {"mode": "local_opt_in"}),
        ("GET", "/api/essentials/list", None),
        ("GET", "/api/essentials/check", None),
        ("POST", "/api/essentials/install-preview", {"id": "steam"}),
        ("GET", "/api/streaming/status", None),
        ("GET", "/api/streaming/recommendations", None),
        ("POST", "/api/streaming/export-profile", {}),
        ("GET", "/api/creator/status", None),
        ("GET", "/api/creator/recommendations", None),
        ("GET", "/api/restore/sessions", None),
        ("GET", "/api/restore/status", None),
        ("GET", "/api/restore/metadata", None),
        ("GET", f"/api/restore/session/{session_id}", None),
        ("POST", f"/api/restore/session/{session_id}/preview", {}),
        ("POST", f"/api/restore/session/{session_id}/apply", {}),
        ("POST", "/api/restore/preview", {"session_id": session_id}),
        ("POST", "/api/restore/apply", {"session_id": session_id}),
        ("POST", "/api/restore/verify", {"session_id": session_id}),
        ("POST", "/api/restore/rollback", {"session_id": session_id}),
        ("GET", "/api/restore/export", None),
        ("GET", "/api/recovery/incomplete-jobs", None),
        ("POST", "/api/recovery/resolve", {"action": "review"}),
        ("GET", "/api/kb/topics", None),
        ("GET", "/api/kb/search?q=dlss", None),
        ("GET", "/api/kb/topic/dlss", None),
        ("GET", "/api/feature-audit/status", None),
        ("GET", "/api/feature-audit/run", None),
        ("GET", "/api/update/check", None),
        ("GET", "/api/update/latest", None),
        ("POST", "/api/webhooks/test-error", {}),
        ("POST", "/api/webhooks/test-update", {}),
        ("POST", "/api/nvidia/test-connection", {}),
        ("GET", "/api/action-log", None),
        ("GET", "/api/apps/installed", None),
        ("POST", "/api/apps/uninstall/plan", {}),
        ("POST", "/api/apps/uninstall/apply", {}),
        ("GET", "/api/security/defender-status", None),
        ("GET", "/api/security/firewall-status", None),
        ("GET", "/api/security/update-status", None),
        ("GET", "/api/drivers/summary", None),
        ("GET", "/api/drivers/recommendations", None),
        ("POST", "/api/drivers/export-report", {}),
        ("GET", "/api/rgb/software", None),
        ("GET", "/api/rgb/conflicts", None),
        ("POST", "/api/rgb/restart-app", {}),
        ("POST", "/api/reports/export-json", {}),
        ("POST", "/api/reports/export-txt", {}),
        ("POST", "/api/reports/export-md", {}),
        ("GET", "/api/logs/recent", None),
        ("POST", "/api/logs/export", {}),
        ("GET", "/api/license/status", None),
        ("POST", "/api/license/activate-local", {}),
        ("POST", "/api/license/deactivate-local", {}),
        ("GET", "/api/plugins/catalog", None),
        ("POST", "/api/plugins/validate", {}),
        ("POST", "/api/plugins/install", {}),
        ("POST", "/api/plugins/uninstall", {}),
    ]

    for method, path, json_payload in route_cases:
        response = client.open(path, method=method, json=json_payload)
        assert response.status_code not in {404, 405, 500}, (method, path, response.status_code, response.get_data(as_text=True))
        assert response.is_json, (method, path, response.get_data(as_text=True))

def test_nvidia_copilot_setup_status_is_user_friendly(monkeypatch, tmp_path):
    monkeypatch.setenv("HYPERBOOSTX_PORTABLE_HOME", str(tmp_path))
    monkeypatch.delenv("HYPERBOOSTX_SESSION_TOKEN", raising=False)

    server = HyperBoostBackendServer()
    client = server.app.test_client()

    response = client.post("/api/nvidia/test-connection", json={})
    payload = response.get_json()

    assert response.status_code == 200
    assert payload["success"] is False
    assert payload["configured"] is False
    assert payload["requires_setup"] is True
    assert payload["plaintext_key_logged"] is False


def test_runtime_route_contract_token_blocks_mutating_endpoints(monkeypatch, tmp_path):
    monkeypatch.setenv("HYPERBOOSTX_PORTABLE_HOME", str(tmp_path))
    monkeypatch.setenv("HYPERBOOSTX_SESSION_TOKEN", "route-token")
    server = HyperBoostBackendServer()
    client = server.app.test_client()

    denied = client.post("/api/boost/plan", json={"goal": "gaming"})
    allowed = client.post(
        "/api/boost/plan",
        json={"goal": "gaming"},
        headers={"X-HyperBoostX-Session": "route-token"},
    )

    assert denied.status_code == 401
    denied_payload = denied.get_json()
    assert denied_payload["ok"] is False
    assert denied_payload["status"] == "unauthorized_local_session"
    assert denied_payload["can_retry"] is True
    assert "Local session token mismatch" in denied_payload["message"]
    assert allowed.status_code == 200


def test_runtime_route_contract_errors_are_ui_friendly(monkeypatch, tmp_path):
    monkeypatch.setenv("HYPERBOOSTX_PORTABLE_HOME", str(tmp_path))
    monkeypatch.delenv("HYPERBOOSTX_SESSION_TOKEN", raising=False)

    server = HyperBoostBackendServer()
    client = server.app.test_client()

    not_found = client.get("/api/definitely-missing-route-v210")
    method_not_allowed = client.delete("/api/health")

    for response, status in ((not_found, "not_found"), (method_not_allowed, "method_not_allowed")):
        assert response.is_json
        payload = response.get_json()
        assert payload["ok"] is False
        assert payload["status"] == status
        assert payload["message"]
        assert "error" in payload
        assert "can_retry" in payload


def test_v21_compatibility_contract_routes_use_standard_envelope(monkeypatch, tmp_path):
    monkeypatch.setenv("HYPERBOOSTX_PORTABLE_HOME", str(tmp_path))
    monkeypatch.delenv("HYPERBOOSTX_SESSION_TOKEN", raising=False)
    monkeypatch.setattr(
        "services.optimization.network_service.NetworkService.test_dns",
        staticmethod(lambda: {"response_time": 1.0, "status": "Good"}),
    )
    monkeypatch.setattr(
        "services.optimization.startup_service.StartupService.get_startup_items",
        lambda self: [],
    )

    server = HyperBoostBackendServer()
    client = server.app.test_client()
    route_specs = """
GET /api/status
GET /api/settings
POST /api/settings {"theme":"Cyber Dark"}
GET /api/dashboard/summary
GET /api/dashboard/score
GET /api/dashboard/alerts
GET /api/dashboard/activity
POST /api/scan/system {}
POST /api/scan/quick {}
POST /api/scan/full {}
POST /api/boost/preview {"goal":"gaming"}
GET /api/boost/last-result
GET /api/boost/history
GET /api/performance/summary
POST /api/performance/plan {"goal":"performance"}
POST /api/performance/apply {}
GET /api/startup/summary
GET /api/processes
GET /api/processes/summary
POST /api/processes/preview-close {"pid":1}
POST /api/processes/close-selected {"pids":[1]}
GET /api/cleanup/history
GET /api/storage/drives
POST /api/storage/scan {}
POST /api/storage/analyze {}
POST /api/storage/cleanup-preview {}
GET /api/gaming/detect
GET /api/gaming/profiles
POST /api/gaming/profile/apply {"game_id":"valorant"}
POST /api/gaming/profile/restore {}
POST /api/gaming/overlay/scan {}
POST /api/gaming/boost/preview {}
POST /api/gaming/boost/apply {}
GET /api/gpu/info
GET /api/gpu/health
GET /api/network/status
POST /api/network/ping-test {"host":"1.1.1.1"}
POST /api/network/dns-preview {}
POST /api/network/dns-apply {}
POST /api/network/reset-preview {}
GET /api/security/health
POST /api/apps/uninstall {"app_id":"manual"}
POST /api/windows/features/apply {}
POST /api/windows/services/apply {}
POST /api/repair/sfc-preview {}
POST /api/repair/sfc-run {}
POST /api/repair/dism-preview {}
POST /api/repair/dism-run {}
POST /api/restore/create {"module":"contract"}
POST /api/restore/undo-last {}
POST /api/automation/create {}
POST /api/automation/dry-run {}
POST /api/automation/enable {}
POST /api/automation/disable {}
POST /api/automation/delete {}
GET /api/ai/status
POST /api/ai/ask {"question":"what is safe"}
POST /api/ai/plan {}
POST /api/ai/approve {}
POST /api/ai/reject {}
GET /api/audit/features
POST /api/audit/run {}
GET /api/audit/report
POST /api/update/download-preview {}
POST /api/update/download {}
POST /api/update/install-preview {}
GET /api/reports
GET /api/reports/manual-id
GET /api/logs/recent
POST /api/logs/export {}
""".strip().splitlines()

    required_keys = {"ok", "module", "action", "action_id", "page", "status", "message", "data", "warnings", "blocked_reasons", "requires_admin", "requires_reboot", "rollback_available", "restore_available", "restore_session_id", "report_available", "report_id"}
    for spec in route_specs:
        method, path, *body = spec.split(" ", 2)
        json_payload = json.loads(body[0]) if body else None
        response = client.open(path, method=method, json=json_payload)
        assert response.status_code == 200, (method, path, response.status_code, response.get_data(as_text=True))
        payload = response.get_json()
        assert required_keys.issubset(payload.keys()), (method, path, payload)
        assert payload["status"] in {"success", "partial", "blocked", "error", "preview"}, (method, path, payload)


def test_release_readiness_and_update_contract_follow_runtime_channel(monkeypatch, tmp_path):
    monkeypatch.setenv("HYPERBOOSTX_PORTABLE_HOME", str(tmp_path))
    monkeypatch.delenv("HYPERBOOSTX_SESSION_TOKEN", raising=False)
    expected_version = _expected_version()
    expected_channel = _expected_channel(expected_version)

    server = HyperBoostBackendServer()
    client = server.app.test_client()

    readiness = client.get("/api/release/readiness").get_json()
    update = client.get("/api/update/check").get_json()
    master = client.get("/api/master-test/status").get_json()

    for payload in (readiness, update, master):
        assert payload["channel"] == expected_channel
        expected_stable = expected_channel == "Stable"
        assert payload["stable"] is expected_stable
        assert payload["manual_lab_required"] is (not expected_stable)
        if expected_stable:
            assert payload["status"] == "stable_ready_unsigned"
            assert payload["blocking_gates"] == []
            assert payload["code_signing_status"] == "SKIPPED_BY_OWNER_NO_CERT"
        else:
            assert "installed_runtime_verification" in payload["blocking_gates"]
            assert "hardware_matrix_lab" in payload["blocking_gates"]

    assert readiness["status"] in {"beta_ready", "stable_candidate", "stable_candidate_requires_lab", "stable_ready", "stable_ready_unsigned"}
    assert update["current_version"] == expected_version
    assert master["release_ready"] is (expected_channel == "Stable")


def test_streaming_legacy_toolkit_is_exposed(monkeypatch, tmp_path):
    monkeypatch.setenv("HYPERBOOSTX_PORTABLE_HOME", str(tmp_path))
    server = HyperBoostBackendServer()
    client = server.app.test_client()

    payload = client.get("/api/streaming/status").get_json()
    toolkit_text = str(payload.get("legacy_toolkit", [])).lower()

    assert "microphone tools" in toolkit_text
    assert "voicemeeter" in toolkit_text
    assert "webcam tools" in toolkit_text
    assert "obs profile recommendation" in toolkit_text
    assert payload["safety"]["no_driver_service_changes"] is True

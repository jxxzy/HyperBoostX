import json

from app.backend_server import HyperBoostBackendServer
from services.product_features import LocalJsonStore, PerformanceAdvisorService


def test_v14_read_endpoints_exist(monkeypatch, tmp_path):
    monkeypatch.setenv("HYPERBOOSTX_PORTABLE_HOME", str(tmp_path))
    server = HyperBoostBackendServer()
    client = server.app.test_client()

    for path in [
        "/api/advisor/performance",
        "/api/knowledge/terms",
        "/api/score/engine",
        "/api/games/library",
        "/api/overlays/status",
        "/api/protection/processes",
        "/api/processes/heavy",
        "/api/benchmark/history",
        "/api/gpu/vendor-guide",
        "/api/drivers/recommendation",
        "/api/cleanup/scan",
        "/api/essentials/list",
        "/api/streaming/status",
        "/api/rgb/status",
        "/api/plugins/registry",
        "/api/settings/ui",
        "/api/product/storage",
        "/api/product/v2-roadmap",
    ]:
        response = client.get(path)
        assert response.status_code == 200, path


def test_performance_advisor_detects_gpu_bottleneck():
    payload = {
        "stats": {"cpu": 43, "memory": 62, "disk": 50, "processes": 120, "memory_total_gb": 16, "cpu_threads": 16},
        "gpu": {"vendor": "Nvidia", "family": "NVIDIA GeForce RTX", "model": "RTX Test", "gpu_usage_percent": 99, "vram_usage_percent": 95, "dedicated_gpu": True, "vram_total_mb": 8192},
    }
    result = PerformanceAdvisorService.analyze(payload)

    assert result["diagnosis_mode"] == "local_deterministic_advisor"
    assert any(item["type"] == "gpu_bottleneck" for item in result["analysis"])
    assert any(item["type"] == "vram_pressure" for item in result["analysis"])
    assert result["requires_user_approval"] is True
    assert "guarantee" in result["expected_effect_without_guarantee"].lower()


def test_knowledge_base_explains_dlss():
    server = HyperBoostBackendServer()
    client = server.app.test_client()

    response = client.get("/api/knowledge/terms/dlss")

    assert response.status_code == 200
    payload = response.get_json()
    assert payload["title"] == "DLSS"
    assert "recommended_for" in payload


def test_v14_mutating_endpoints_require_session_token(monkeypatch, tmp_path):
    monkeypatch.setenv("HYPERBOOSTX_PORTABLE_HOME", str(tmp_path))
    monkeypatch.setenv("HYPERBOOSTX_SESSION_TOKEN", "token-v14")
    server = HyperBoostBackendServer()
    client = server.app.test_client()

    denied = client.post("/api/games/scan", json={})
    allowed = client.post("/api/games/scan", json={}, headers={"X-HyperBoostX-Session": "token-v14"})

    assert denied.status_code == 401
    assert allowed.status_code == 200


def test_protection_blocks_dangerous_actions(monkeypatch, tmp_path):
    monkeypatch.setenv("HYPERBOOSTX_PORTABLE_HOME", str(tmp_path))
    server = HyperBoostBackendServer()
    client = server.app.test_client()

    response = client.post("/api/protection/evaluate-action", json={"action": "disable defender for fps", "target": "MsMpEng.exe"})

    assert response.status_code == 200
    payload = response.get_json()
    assert payload["allowed"] is False
    assert payload["blocked"] is True


def test_benchmark_is_local_history_only(monkeypatch, tmp_path):
    monkeypatch.setenv("HYPERBOOSTX_PORTABLE_HOME", str(tmp_path))
    server = HyperBoostBackendServer()
    client = server.app.test_client()

    created = client.post("/api/benchmark/manual", json={"game": "Valorant", "avg_fps": 144, "one_percent_low_fps": 110})
    history = client.get("/api/benchmark/history")

    assert created.status_code == 200
    assert "Local history only" in created.get_json()["comparison"]
    assert len(history.get_json()["items"]) == 1


def test_corrupted_json_is_backed_up_and_default_regenerated(monkeypatch, tmp_path):
    monkeypatch.setenv("HYPERBOOSTX_PORTABLE_HOME", str(tmp_path))
    path = LocalJsonStore.path("ui_settings")
    path.write_text("not-json", encoding="utf-8")

    data = LocalJsonStore.load("ui_settings", {"reduce_motion": False}, dict)

    assert data["reduce_motion"] is False
    assert json.loads(path.read_text(encoding="utf-8"))["reduce_motion"] is False
    assert list(path.parent.glob("ui_settings.json.corrupt-*"))


def test_reduce_motion_setting_persists(monkeypatch, tmp_path):
    monkeypatch.setenv("HYPERBOOSTX_PORTABLE_HOME", str(tmp_path))
    server = HyperBoostBackendServer()
    client = server.app.test_client()

    saved = client.post("/api/settings/ui", json={"reduce_motion": True, "theme": "OLED"})
    loaded = client.get("/api/settings/ui")

    assert saved.status_code == 200
    assert loaded.get_json()["reduce_motion"] is True
    assert loaded.get_json()["theme"] == "OLED"


def test_roadmap_features_are_not_claimed_as_active(monkeypatch, tmp_path):
    monkeypatch.setenv("HYPERBOOSTX_PORTABLE_HOME", str(tmp_path))
    server = HyperBoostBackendServer()
    client = server.app.test_client()

    plugins = client.get("/api/plugins/registry").get_json()
    rgb = client.get("/api/rgb/status").get_json()
    driver = client.get("/api/drivers/recommendation").get_json()

    assert plugins["third_party_loading"] is False
    assert rgb["control_enabled"] is False
    assert driver["latest_stable"] is None
    assert driver["source_required"] is True

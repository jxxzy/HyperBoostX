from app.backend_server import HyperBoostBackendServer
from app.services.ai.triple_ai_engine import TripleAIEngine


class FakeSystemInfoService:
    def get_system_identity(self):
        return {"os_version": "10.0.22631", "os_release": "11"}

    def get_cpu_info(self):
        return {"processor": "AMD Ryzen Test CPU", "cores": 8, "threads": 16}

    def get_memory_info(self):
        return {"total": 16 * 1024**3, "speed_mhz": 3200}

    def get_disk_info(self):
        return {"C:": {"free": 80 * 1024**3}}

    def get_system_drive_info(self):
        return {"storage_class": "NVMe"}

    def get_device_profile(self, stats=None):
        return {
            "bottleneck": "memory-bound",
            "recommended_profile": "Low RAM",
            "expected_gain": "Moderate",
            "storage_class": "SSD",
        }

    def get_os_info(self):
        return {"version": "10.0.22631", "release": "11"}

    def get_gpu_info(self):
        return {
            "gpus": [
                {
                    "name": "NVIDIA GeForce RTX 4060",
                    "driver_version": "555.85",
                    "vram": 8 * 1024**3,
                }
            ]
        }

    def get_temperature_info(self):
        return {}


class FakeMonitorService:
    def get_current_stats(self):
        return {
            "cpu": 35,
            "cpu_cores": 8,
            "cpu_threads": 16,
            "memory": 84,
            "memory_total_gb": 16,
            "disk": 88,
            "processes": 148,
            "gpu": {
                "name": "NVIDIA GeForce RTX 4060",
                "load": 42,
                "memory_total_mb": 8192,
                "memory_percent": 51,
                "temperature": 68,
            },
        }

    def get_process_list(self, limit=15):
        return [
            {"name": "chrome.exe", "memory": 2.4, "cpu": 3},
            {"name": "launcher.exe", "memory": 1.4, "cpu": 2},
            {"name": "overlay.exe", "memory": 1.2, "cpu": 2},
        ][:limit]


class FakeStartupService:
    def get_startup_items(self):
        return [
            {"name": "Game Launcher", "impact": "High", "impact_score": 72, "enabled": True},
            {"name": "Overlay", "impact": "Medium", "impact_score": 40, "enabled": True},
        ]


class FakeTweakService:
    def __init__(self):
        self.applied = []
        self.reverted = []

    def apply_tweak(self, tweak_id, expert_mode=False, confirmed=False):
        self.applied.append(tweak_id)
        return {
            "success": True,
            "restore_point": f"tweak_{tweak_id}",
            "restore_timestamp": "20260530-000000-000000",
            "registry_backups": 1,
        }

    def revert_tweak(self, tweak_id):
        self.reverted.append(tweak_id)
        return {"success": True, "registry_restored": 1}


def build_engine(fake_tweak_service=None):
    return TripleAIEngine(
        system_info_service=FakeSystemInfoService(),
        monitor_service=FakeMonitorService(),
        startup_service=FakeStartupService(),
        tweak_service=fake_tweak_service or FakeTweakService(),
    )


def test_triple_ai_full_flow_returns_scan_analyze_safety_assistant_report():
    result = build_engine().run_full_flow(user_goal="gaming", game="Fortnite")

    assert result["scan"]["hardware"]["gpu_name"] == "NVIDIA GeForce RTX 4060"
    assert result["analysis"]["role"] == "AI Analyzer"
    assert result["safety"]["role"] == "AI Safety Guard"
    assert result["assistant"]["role"] == "AI Assistant"
    assert result["report"]["pc_health_score"] <= 100
    assert "guaranteed FPS" not in result["assistant"]["message"]
    assert result["analysis"]["rag_context"]


def test_safety_guard_blocks_dangerous_tweaks():
    engine = build_engine()
    safety = engine.safety_check([
        {
            "tweak_id": "disable_defender",
            "title": "Disable Windows Defender",
            "description": "Disable Windows Security for FPS",
            "risk_level": "high",
            "reversible": False,
            "can_auto_apply": True,
        },
        {
            "tweak_id": "auto_overclock_gpu",
            "title": "Auto overclock GPU",
            "description": "Guaranteed FPS boost",
            "risk_level": "high",
            "reversible": False,
            "can_auto_apply": True,
        },
    ])

    assert len(safety["blocked"]) == 2
    assert safety["approved"] == []


def test_safe_tweak_engine_requires_approval_and_runs_only_guard_approved_items():
    fake_tweaks = FakeTweakService()
    engine = build_engine(fake_tweak_service=fake_tweaks)
    recommendations = [
        engine._kb_recommendation("optimize_visual"),
        {
            "tweak_id": "disable_defender",
            "title": "Disable Windows Defender",
            "description": "Disable Windows Security",
            "risk_level": "blocked",
            "reversible": False,
            "can_auto_apply": True,
        },
    ]

    denied = engine.apply_safe_tweaks(recommendations, user_approved=False)
    assert denied["success"] is False
    assert fake_tweaks.applied == []

    result = engine.apply_safe_tweaks(recommendations, user_approved=True)
    assert fake_tweaks.applied == ["optimize_visual"]
    assert result["applied"][0]["tweak_id"] == "optimize_visual"
    assert result["blocked"][0]["tweak_id"] == "disable_defender"


def test_scan_contract_requires_backend_token_and_returns_scan(monkeypatch):
    import api.triple_ai as triple_ai

    monkeypatch.setattr(
        triple_ai.triple_ai_engine,
        "scan_pc",
        lambda: {
            "scan_id": "scan_test",
            "hardware": {},
            "windows": {},
            "nvidia": {},
            "apps": {},
            "timestamp": "2026-05-30T00:00:00Z",
        },
    )
    server = HyperBoostBackendServer(auth_token="test-token")
    client = server.app.test_client()

    assert client.post("/scan").status_code == 401
    response = client.post("/scan", headers={"X-HyperBoostX-Token": "test-token"})

    assert response.status_code == 200
    assert response.get_json()["scan_id"] == "scan_test"


def test_game_optimizer_endpoint_requires_token_and_game_name(monkeypatch):
    import api.triple_ai as triple_ai

    monkeypatch.setattr(
        triple_ai.triple_ai_engine,
        "optimize_game",
        lambda game_name, scan_result=None: {
            "game": game_name,
            "risk_level": "low",
            "manual_apply": True,
            "recommendations": [{"setting": "NVIDIA Reflex", "risk_level": "low"}],
        },
    )
    server = HyperBoostBackendServer(auth_token="test-token")
    client = server.app.test_client()

    assert client.post("/game/optimize", json={"game": "Fortnite"}).status_code == 401

    missing = client.post("/game/optimize", headers={"X-HyperBoostX-Token": "test-token"}, json={})
    assert missing.status_code == 400

    response = client.post("/game/optimize", headers={"X-HyperBoostX-Token": "test-token"}, json={"game": "Fortnite"})
    payload = response.get_json()

    assert response.status_code == 200
    assert payload["game"] == "Fortnite"
    assert payload["manual_apply"] is True

from app.backend_server import HyperBoostBackendServer
from services.system_reality_guard import RealitySafetyGuard, SystemRealityGuardService


class _FakeMemory:
    rss = 128 * 1024 * 1024


class _FakeProcess:
    def __init__(self, pid, name, exe=None):
        self.info = {
            "pid": pid,
            "name": name,
            "exe": exe or f"C:\\Program Files\\Vendor\\{name}",
            "memory_info": _FakeMemory(),
        }

    def cpu_percent(self, interval=0.0):
        return {
            "TRCC.exe": 2.0,
            "ffmpeg.exe": 6.0,
            "HWiNFO.exe": 4.0,
            "USB_LCD.exe": 1.0,
            "KANALI.exe": 1.5,
            "HiMOS.exe": 2.0,
        }.get(self.info["name"], 0.0)


def test_lcd_detector_classifies_trcc_helpers(monkeypatch):
    monkeypatch.setattr(
        "services.system_reality_guard.psutil.process_iter",
        lambda fields: iter([
            _FakeProcess(10, "TRCC.exe"),
            _FakeProcess(11, "ffmpeg.exe"),
            _FakeProcess(12, "HWiNFO.exe"),
            _FakeProcess(13, "USB_LCD.exe"),
        ]),
    )

    payload = SystemRealityGuardService.trcc_helpers()

    roles = {item["role"] for item in payload["data"]["helpers"]}
    assert "main_lcd_app" in roles
    assert "live_wallpaper_decoder" in roles
    assert "sensor_helper" in roles
    assert payload["data"]["wallpaper_active"] is True
    assert payload["status"] == "warning"


def test_safety_guard_blocks_required_lcd_and_vendor_file_actions():
    blocked = RealitySafetyGuard.evaluate_action("kill_required_lcd_app", {"required_for_lcd": True})
    assert blocked["allowed"] is False
    assert blocked["status"] == "blocked"

    disabled = RealitySafetyGuard.evaluate_action("disable_startup", {"required_for_lcd": True})
    assert disabled["allowed"] is False

    patch = RealitySafetyGuard.evaluate_action("patch_vendor_binary", {})
    assert patch["allowed"] is False


def test_defender_exclusion_blocks_broad_paths():
    full_drive = RealitySafetyGuard.evaluate_action("defender_exclusion", {"path": "C:\\"})
    users = RealitySafetyGuard.evaluate_action("defender_exclusion", {"path": "C:\\Users\\jxxzy"})
    narrow = RealitySafetyGuard.evaluate_action("defender_exclusion", {"path": "E:\\Projects\\TrustedGameBuild"})

    assert full_drive["allowed"] is False
    assert users["allowed"] is False
    assert narrow["allowed"] is True
    assert narrow["status"] == "preview_required"


def test_cpu_turbo_diagnostic_cases():
    low_load = SystemRealityGuardService.diagnose_turbo(2.5, 2.5, 25)
    working = SystemRealityGuardService.diagnose_turbo(2.5, 3.4, 95)
    blocked = SystemRealityGuardService.diagnose_turbo(
        2.5,
        2.5,
        95,
        {"max_processor_state": 90, "boost_mode": "disabled", "msi_mode": "silent"},
        {"thermal_throttling": True, "power_limit_exceeded": True},
    )

    assert low_load["status"] == "invalid_test"
    assert working["status"] == "turbo_working"
    assert blocked["status"] == "turbo_not_boosting"
    assert "Windows Maximum Processor State below 100%." in blocked["suspected_causes"]
    assert "Processor Performance Boost Mode disabled." in blocked["suspected_causes"]
    assert "MSI Center low-power mode." in blocked["suspected_causes"]
    assert "Thermal throttling." in blocked["suspected_causes"]
    assert "Power limit exceeded." in blocked["suspected_causes"]


def test_security_classifier_uses_evidence_not_panic_labels():
    normal = SystemRealityGuardService.classify_vendor_component(r"C:\Program Files\Intel\SUR\task.exe")
    suspicious = SystemRealityGuardService.classify_vendor_component(r"C:\Users\me\AppData\Local\Temp\hidden.ps1")
    unknown = SystemRealityGuardService.classify_vendor_component(r"D:\Tools\tool.exe")

    assert normal["classification"] == "normal_vendor_component"
    assert suspicious["classification"] == "suspicious_needs_review"
    assert unknown["classification"] == "manual_review"


def test_system_reality_routes_return_standard_schema(monkeypatch, tmp_path):
    monkeypatch.setenv("HYPERBOOSTX_PORTABLE_HOME", str(tmp_path))
    monkeypatch.delenv("HYPERBOOSTX_SESSION_TOKEN", raising=False)
    monkeypatch.setattr(
        "services.system_reality_guard.psutil.process_iter",
        lambda fields: iter([]),
    )
    monkeypatch.setattr(
        "services.system_reality_guard._run",
        lambda args, timeout=6.0: {"success": True, "stdout": "", "stderr": "", "exit_code": 0},
    )

    server = HyperBoostBackendServer()
    client = server.app.test_client()
    cases = [
        ("GET", "/api/system-reality/overview", None),
        ("POST", "/api/system-reality/scan", {}),
        ("GET", "/api/lcd/apps", None),
        ("GET", "/api/lcd/vendors/trcc/helpers", None),
        ("POST", "/api/lcd/hybrid/preview", {}),
        ("GET", "/api/defender/status", None),
        ("POST", "/api/defender/exclusions/preview", {"path": "C:\\"}),
        ("GET", "/api/cpu/turbo/status", None),
        ("GET", "/api/msi/status", None),
        ("GET", "/api/security/reality-audit", None),
        ("POST", "/api/system-reality/safety/evaluate", {"action_type": "kill_msmpeng"}),
    ]

    for method, path, body in cases:
        response = client.open(path, method=method, json=body)
        assert response.status_code == 200, (method, path, response.get_data(as_text=True))
        payload = response.get_json()
        assert {"ok", "status", "data", "recommendations", "blocked_reasons", "requires_admin", "rollback", "logs"} <= set(payload)

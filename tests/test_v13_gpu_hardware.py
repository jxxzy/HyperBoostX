from services.monitoring.gpu_detection_service import GpuDetectionService, GpuVendor
from services.monitoring.hardware_profile_service import HardwareProfileService


def test_nvidia_rtx_gpu_detection():
    summary = GpuDetectionService.get_gpu_summary([
        {"name": "NVIDIA GeForce RTX 4090", "driver_version": "555.1", "vram": 24 * 1024 * 1024 * 1024, "current_hz": 144}
    ])

    assert summary["vendor"] == GpuVendor.NVIDIA
    assert summary["family"] == "NVIDIA GeForce RTX"
    assert summary["badge"]["accent"] == "NVIDIA Green"
    assert "NVIDIA" in summary["profile_recommendation"] or summary["profile_recommendation"] == "High VRAM Mode"


def test_amd_radeon_gpu_detection():
    summary = GpuDetectionService.get_gpu_summary([
        {"name": "AMD Radeon RX 7900 XT", "vram": 20 * 1024 * 1024 * 1024, "current_hz": 165}
    ])

    assert summary["vendor"] == GpuVendor.AMD
    assert summary["family"] == "AMD Radeon RX"
    assert summary["badge"]["accent"] == "Radeon Red"


def test_intel_arc_gpu_detection():
    summary = GpuDetectionService.get_gpu_summary([
        {"name": "Intel Arc A770 Graphics", "vram": 16 * 1024 * 1024 * 1024, "current_hz": 120}
    ])

    assert summary["vendor"] == GpuVendor.INTEL
    assert summary["family"] == "Intel Arc"
    assert summary["badge"]["accent"] == "Intel Blue"


def test_intel_igpu_and_hybrid_detection():
    summary = GpuDetectionService.get_gpu_summary([
        {"name": "Intel Iris Xe Graphics", "vram": 1024 * 1024 * 1024, "current_hz": 60},
        {"name": "NVIDIA GeForce RTX 4060 Laptop GPU", "vram": 8 * 1024 * 1024 * 1024},
    ])

    assert summary["integrated_gpu"] is True
    assert summary["dedicated_gpu"] is True
    assert summary["hybrid_gpu_system"] is True
    assert any(gpu["profile_recommendation"] == "Laptop Hybrid Graphics Mode" for gpu in summary["gpus"])


def test_microsoft_basic_and_unknown_fallback():
    basic = GpuDetectionService.get_gpu_summary([
        {"name": "Microsoft Basic Display Adapter"}
    ])
    unknown = GpuDetectionService.get_gpu_summary([
        {"name": "Mystery Display Adapter"}
    ])

    assert basic["vendor"] == GpuVendor.MICROSOFT_BASIC
    assert basic["badge"]["label"] == "Generic GPU"
    assert unknown["vendor"] == GpuVendor.UNKNOWN
    assert unknown["profile_recommendation"] == "Unknown Safe GPU Mode"


def test_vendor_software_and_overlay_classification():
    process_names = ["Discord.exe", "GameOverlayUI.exe", "SignalRGB.exe", "RadeonSoftware.exe", "RTSS.exe"]
    overlays = GpuDetectionService.detect_overlays(process_names)
    vendors = GpuDetectionService.detect_vendor_software(process_names)

    steam_overlay = next(item for item in overlays if item["id"] == "steam_overlay")
    signal_rgb = next(item for item in vendors if item["id"] == "signalrgb")
    radeon = next(item for item in vendors if item["id"] == "amd_adrenalin")

    assert steam_overlay["detected"] is True
    assert steam_overlay["classification"] == "Can pause while gaming"
    assert signal_rgb["classification"] == "Can pause while gaming"
    assert radeon["detected"] is True


def test_hardware_profile_schema_for_amd_system(monkeypatch):
    gpu = GpuDetectionService.get_gpu_summary([
        {"name": "AMD Radeon RX 7800 XT", "vram": 16 * 1024 * 1024 * 1024, "current_hz": 144}
    ])
    stats = {
        "cpu": 8,
        "cpu_threads": 16,
        "memory": 42,
        "memory_total_gb": 32,
        "disk": 55,
        "processes": 160,
    }
    monkeypatch.setattr("services.optimization.startup_service.StartupService.get_startup_items", lambda self: [])
    monkeypatch.setattr("services.monitoring.hardware_profile_service.psutil.sensors_battery", lambda: None)

    profile = HardwareProfileService.get_profile(
        stats=stats,
        gpu_summary=gpu,
        vendor_apps=[],
        overlays=[],
    )

    assert profile["recommended_profile"] == "High-End AMD Radeon PC"
    assert profile["confidence"] >= 0.9
    assert profile["undo_available"] is True
    assert "GPU driver service disable" in profile["risky_actions_blocked"]
    assert 0 <= profile["scores"]["gaming_readiness"] <= 100

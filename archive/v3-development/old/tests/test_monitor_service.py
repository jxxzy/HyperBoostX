import pytest
from app.services.monitoring.monitor_service import MonitorService


def test_get_current_stats_returns_dict():
    stats = MonitorService.get_current_stats()
    assert isinstance(stats, dict)
    assert "cpu" in stats
    assert "memory" in stats


def test_get_current_stats_uses_short_lived_cache(monkeypatch):
    MonitorService._last_stats_snapshot = {}
    MonitorService._last_stats_snapshot_utc = 0.0

    call_count = {"count": 0}

    def fake_cpu_percent(interval=0, percpu=False):
        call_count["count"] += 1
        return [1.0, 2.0] if percpu else 7.0

    monkeypatch.setattr("app.services.monitoring.monitor_service.psutil.cpu_percent", fake_cpu_percent)
    monkeypatch.setattr("app.services.monitoring.monitor_service.psutil.cpu_freq", lambda: type("CpuFreq", (), {"current": 4200.0, "max": 5100.0})())
    monkeypatch.setattr("app.services.monitoring.monitor_service.psutil.virtual_memory", lambda: type("Vm", (), {"percent": 48.0, "used": 8 * 1024**3, "total": 16 * 1024**3})())
    monkeypatch.setattr("app.services.monitoring.monitor_service.psutil.disk_usage", lambda path: type("Disk", (), {"percent": 61.0, "used": 100 * 1024**3, "total": 200 * 1024**3})())
    monkeypatch.setattr("app.services.monitoring.monitor_service.psutil.net_io_counters", lambda: type("Net", (), {"bytes_recv": 1000, "bytes_sent": 2000})())
    monkeypatch.setattr("app.services.monitoring.monitor_service.psutil.disk_io_counters", lambda: type("DiskIo", (), {"read_bytes": 0, "write_bytes": 0})())
    monkeypatch.setattr("app.services.monitoring.monitor_service.psutil.cpu_count", lambda logical=True: 8 if logical else 4)
    monkeypatch.setattr("app.services.monitoring.monitor_service.psutil.pids", lambda: [1, 2, 3])
    monkeypatch.setattr("app.services.monitoring.monitor_service.psutil.boot_time", lambda: 123.0)
    monkeypatch.setattr("app.services.monitoring.monitor_service.MonitorService.get_gpu_stats", lambda: {})
    monkeypatch.setattr("app.services.monitoring.monitor_service.MonitorService.get_temperature_info", lambda: {})
    monkeypatch.setattr("app.services.monitoring.monitor_service.MonitorService._delta_counters", classmethod(lambda cls: {
        "net_download_mb_s": 0.0,
        "net_upload_mb_s": 0.0,
        "disk_read_mb_s": 0.0,
        "disk_write_mb_s": 0.0,
    }))

    first = MonitorService.get_current_stats()
    second = MonitorService.get_current_stats()

    assert first["cpu"] == second["cpu"] == 7.0
    assert call_count["count"] == 2

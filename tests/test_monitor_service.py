import pytest
from app.services.monitoring.monitor_service import MonitorService


def test_get_current_stats_returns_dict():
    stats = MonitorService.get_current_stats()
    assert isinstance(stats, dict)
    assert "cpu" in stats
    assert "memory" in stats

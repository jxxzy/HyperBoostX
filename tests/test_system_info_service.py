import pytest
from app.services.monitoring.system_info_service import SystemInfoService


def test_system_identity_contains_os_name():
    identity = SystemInfoService.get_system_identity()
    assert "os_name" in identity
    assert isinstance(identity["os_name"], str)

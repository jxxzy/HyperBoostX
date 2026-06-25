from app.services.optimization.tweak_service import TweakService
from app.core.restore import RestoreManager
from app.core.restore import RestorePoint
from app.core.config import Config
import winreg


def test_unknown_tweak_returns_error():
    result = TweakService.apply_tweak("does_not_exist")
    assert result["success"] is False
    assert "Unknown tweak" in result["error"]


def test_tweak_catalog_contains_expected_entries():
    tweaks = TweakService.get_all_tweaks()
    ids = {item["id"] for item in tweaks}
    assert "optimize_visual" in ids
    assert "disable_xbox" in ids


def test_failed_tweak_triggers_restore(monkeypatch):
    class DummyRestorePoint:
        def __init__(self):
            self.files = {}

    restore_called = {"value": False}

    monkeypatch.setattr(
        "app.services.optimization.tweak_service.RestoreManager.create_restore_point",
        lambda *args, **kwargs: DummyRestorePoint(),
    )
    monkeypatch.setattr(
        "app.services.optimization.tweak_service.TweakService._apply_optimize_visual",
        lambda restore_point: False,
    )
    monkeypatch.setattr(
        "app.services.optimization.tweak_service.RestoreManager.restore",
        lambda restore_point: restore_called.__setitem__("value", True),
    )

    result = TweakService.apply_tweak("optimize_visual")

    assert result["success"] is False
    assert restore_called["value"] is True


def test_high_risk_tweak_requires_expert_mode():
    result = TweakService.apply_tweak("disable_updates")

    assert result["success"] is False
    assert result["requires_expert_mode"] is True


def test_revert_tweak_requires_real_restore_backup(monkeypatch):
    monkeypatch.setattr(
        "app.services.optimization.tweak_service.RestoreManager.find_latest_restore_point",
        lambda name: None,
    )

    result = TweakService.revert_tweak("optimize_visual")

    assert result["success"] is False
    assert "No restore backup" in result["error"]


def test_registry_restore_deletes_value_when_old_value_was_missing(monkeypatch, tmp_path):
    monkeypatch.setattr(Config, "BACKUP_DIR", tmp_path)
    deleted = []

    class DummyKey:
        pass

    def fake_open_key(hkey, path, reserved=0, access=0):
        if access == winreg.KEY_READ:
            raise FileNotFoundError("missing")
        return DummyKey()

    monkeypatch.setattr(winreg, "OpenKey", fake_open_key)
    monkeypatch.setattr(winreg, "CloseKey", lambda key: None)
    monkeypatch.setattr(winreg, "DeleteValue", lambda key, value_name: deleted.append(value_name))

    point = RestorePoint("tweak_test", "test")
    assert RestoreManager.backup_registry(
        point,
        winreg.HKEY_CURRENT_USER,
        r"SOFTWARE\HyperBoostX\Test",
        "CreatedValue",
        1,
        winreg.REG_DWORD,
    )

    assert point.registry[0]["old_value_exists"] is False
    assert RestoreManager.restore(point) is True
    assert deleted == ["CreatedValue"]

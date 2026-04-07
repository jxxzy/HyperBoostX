from app.services.optimization.tweak_service import TweakService


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

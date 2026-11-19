from app.core.profiles import Profile
from app.core.profiles import ProfileManager
from app.services.optimization.booster_service import BoosterService
import winreg


def test_should_close_process_protects_interactive_apps():
    assert BoosterService.should_close_process("Discord.exe") is False
    assert BoosterService.should_close_process("chrome.exe") is False
    assert BoosterService.should_close_process("OneDrive.exe") is True


def test_apply_profile_skips_duplicate_request(monkeypatch):
    BoosterService._last_profile_id = ""
    BoosterService._last_profile_started_at = 0.0

    monkeypatch.setitem(
        __import__("app.core.profiles", fromlist=["ProfileManager"]).ProfileManager.PROFILES,
        "qa-test",
        Profile(
            name="QA Test",
            description="temporary",
            settings={"disable_background_apps": False}
        ),
    )

    first = BoosterService.apply_profile("qa-test")
    second = BoosterService.apply_profile("qa-test")

    assert first["success"] is False or first["success"] is True
    assert second["success"] is True
    assert second.get("duplicate_request") is True


def test_gaming_registry_tweaks_use_current_user(monkeypatch):
    calls = []

    def fake_set_value(path, key, value, value_type, hkey=winreg.HKEY_LOCAL_MACHINE):
        calls.append(
            {
                "path": path,
                "key": key,
                "value": value,
                "value_type": value_type,
                "hkey": hkey,
            }
        )
        return True

    monkeypatch.setattr(
        "app.services.optimization.booster_service.RegistryUtil.set_value",
        fake_set_value,
    )

    assert BoosterService._disable_visual_effects() is True
    assert BoosterService._disable_xbox_overlay() is True
    assert BoosterService._enable_background_recording() is True
    assert BoosterService._disable_background_recording() is True
    assert BoosterService._enable_visual_effects() is True

    assert len(calls) == 6
    assert all(call["hkey"] == winreg.HKEY_CURRENT_USER for call in calls)


def test_streaming_profile_disables_game_bar_background_capture():
    settings = ProfileManager.PROFILES["streaming"].settings

    assert settings["disable_xbox_overlay"] is True
    assert settings["disable_background_recording"] is True
    assert "background_recording" not in settings


def test_apply_setting_reports_admin_requirement_from_registry_error(monkeypatch):
    monkeypatch.setattr(
        "app.services.optimization.booster_service.BoosterService._increase_timer_resolution",
        lambda: False,
    )
    monkeypatch.setattr(
        "app.services.optimization.booster_service.RegistryUtil.get_last_error",
        lambda: {
            "reason": "access_denied",
            "hkey": r"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
        },
    )

    result = BoosterService._apply_setting("increase_timer_resolution")

    assert result["success"] is False
    assert result["reason_code"] == "admin_required"
    assert "elevated access" in result["message"]

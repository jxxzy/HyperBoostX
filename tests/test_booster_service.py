from app.core.profiles import Profile
from app.services.optimization.booster_service import BoosterService


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

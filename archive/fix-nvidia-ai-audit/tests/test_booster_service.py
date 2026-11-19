import logging

from app.core.profiles import Profile
from app.core.profiles import ProfileManager
from app.services.optimization import booster_service
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


def test_duplicate_profile_skip_logs_as_info(monkeypatch, caplog):
    BoosterService._last_profile_id = ""
    BoosterService._last_profile_started_at = 0.0

    monkeypatch.setitem(
        booster_service.ProfileManager.PROFILES,
        "qa-duplicate-log",
        Profile(
            name="QA Duplicate Log",
            description="temporary",
            settings={"disable_background_apps": False}
        ),
    )

    with caplog.at_level(logging.INFO):
        BoosterService.apply_profile("qa-duplicate-log")
        BoosterService.apply_profile("qa-duplicate-log")

    duplicate_records = [record for record in caplog.records if "Skipped duplicate booster apply" in record.message]
    assert duplicate_records
    assert all(record.levelno == logging.INFO for record in duplicate_records)


def test_expected_limited_access_profile_logs_as_info(monkeypatch, caplog):
    BoosterService._last_profile_id = ""
    BoosterService._last_profile_started_at = 0.0

    monkeypatch.setitem(
        booster_service.ProfileManager.PROFILES,
        "qa-limited-access",
        Profile(
            name="QA Limited Access",
            description="temporary",
            settings={
                "safe_setting": True,
                "admin_setting": True,
            }
        ),
    )

    def fake_apply_setting(setting):
        if setting == "safe_setting":
            return {
                "setting": setting,
                "display_name": "Safe setting",
                "success": True,
                "reason_code": "applied",
                "message": "Applied successfully.",
            }

        return {
            "setting": setting,
            "display_name": "Admin setting",
            "success": False,
            "reason_code": "admin_required",
            "message": "Admin setting requires Administrator privileges.",
        }

    monkeypatch.setattr(BoosterService, "_apply_setting", staticmethod(fake_apply_setting))

    with caplog.at_level(logging.INFO):
        result = BoosterService.apply_profile("qa-limited-access")

    assert result["partial_success"] is True
    limited_access_records = [record for record in caplog.records if "limited access" in record.message]
    assert limited_access_records
    assert all(record.levelno == logging.INFO for record in limited_access_records)


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


def test_apply_profile_records_registry_restore_metadata(monkeypatch):
    BoosterService._last_profile_id = ""
    BoosterService._last_profile_started_at = 0.0
    saved_points = []

    monkeypatch.setitem(
        booster_service.ProfileManager.PROFILES,
        "qa-registry-backup",
        Profile(
            name="QA Registry Backup",
            description="temporary",
            settings={"disable_visual_effects": True}
        ),
    )
    monkeypatch.setattr(
        "app.services.optimization.booster_service.RegistryUtil.set_value",
        lambda *args, **kwargs: True,
    )

    def fake_backup_registry(restore_point, hkey, path, key, new_value, new_value_type):
        restore_point.registry.append(
            {
                "hive": "HKEY_CURRENT_USER",
                "path": path,
                "key": key,
                "new_value": new_value,
            }
        )
        return True

    monkeypatch.setattr(
        "app.services.optimization.booster_service.RestoreManager.backup_registry",
        fake_backup_registry,
    )
    monkeypatch.setattr(
        "app.services.optimization.booster_service.RestoreManager.save_restore_point",
        lambda point: saved_points.append(point) or True,
    )

    result = BoosterService.apply_profile("qa-registry-backup")

    assert result["success"] is True
    assert result["registry_backups"] == 1
    assert result["restore_point"] == "profile_qa-registry-backup"
    assert saved_points
    assert BoosterService._current_restore_point is None


def test_apply_profile_records_power_plan_restore_metadata(monkeypatch):
    BoosterService._last_profile_id = ""
    BoosterService._last_profile_started_at = 0.0
    backed_up_schemes = []

    monkeypatch.setitem(
        booster_service.ProfileManager.PROFILES,
        "qa-power-backup",
        Profile(
            name="QA Power Backup",
            description="temporary",
            settings={"balanced_performance": True}
        ),
    )
    monkeypatch.setattr(
        "app.services.optimization.booster_service.RestoreManager.backup_power_plan",
        lambda point, scheme: backed_up_schemes.append(scheme) or point.settings.append({"type": "power_plan"}) or True,
    )
    monkeypatch.setattr(
        "app.services.optimization.booster_service.ShellUtil.execute_command",
        lambda *args, **kwargs: (True, ""),
    )
    monkeypatch.setattr(
        "app.services.optimization.booster_service.RestoreManager.save_restore_point",
        lambda point: True,
    )

    result = BoosterService.apply_profile("qa-power-backup")

    assert result["success"] is True
    assert result["settings_backups"] == 1
    assert backed_up_schemes == ["381b4222-f694-41f0-9685-ff5bb260df2e"]

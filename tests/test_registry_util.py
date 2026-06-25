import logging
import winreg

from app.utils.registry import RegistryUtil


def test_set_value_creates_or_opens_key(monkeypatch):
    calls = []

    class DummyKey:
        pass

    def fake_create_key_ex(hkey, path, reserved, access):
        calls.append(("create", hkey, path, reserved, access))
        return DummyKey()

    def fake_set_value_ex(reg_key, key, reserved, value_type, value):
        calls.append(("set", key, reserved, value_type, value))

    def fake_close_key(reg_key):
        calls.append(("close",))

    monkeypatch.setattr(winreg, "CreateKeyEx", fake_create_key_ex)
    monkeypatch.setattr(winreg, "SetValueEx", fake_set_value_ex)
    monkeypatch.setattr(winreg, "CloseKey", fake_close_key)

    success = RegistryUtil.set_value(
        r"SOFTWARE\HyperBoostX\Test",
        "Enabled",
        1,
        winreg.REG_DWORD,
        hkey=winreg.HKEY_CURRENT_USER,
    )

    assert success is True
    assert calls[0] == (
        "create",
        winreg.HKEY_CURRENT_USER,
        r"SOFTWARE\HyperBoostX\Test",
        0,
        winreg.KEY_WRITE,
    )
    assert calls[1] == ("set", "Enabled", 0, winreg.REG_DWORD, 1)
    assert calls[2] == ("close",)


def test_set_value_access_denied_records_last_error_without_warning(monkeypatch, caplog):
    def fake_create_key_ex(hkey, path, reserved, access):
        raise PermissionError("denied")

    monkeypatch.setattr(winreg, "CreateKeyEx", fake_create_key_ex)
    RegistryUtil.clear_last_error()

    with caplog.at_level(logging.INFO):
        success = RegistryUtil.set_value(
            r"SOFTWARE\HyperBoostX\Test",
            "Enabled",
            1,
            winreg.REG_DWORD,
            hkey=winreg.HKEY_LOCAL_MACHINE,
        )

    assert success is False
    assert RegistryUtil.get_last_error()["reason"] == "access_denied"
    access_records = [record for record in caplog.records if "access denied" in record.message]
    assert access_records
    assert all(record.levelno == logging.INFO for record in access_records)

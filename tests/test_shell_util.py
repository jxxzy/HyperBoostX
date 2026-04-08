from app.utils.shell import ShellUtil


def test_admin_command_returns_clear_message_when_not_elevated(monkeypatch):
    monkeypatch.setattr("app.utils.shell.Permissions.is_admin", lambda: False)

    success, output = ShellUtil.execute_command("netsh int tcp set global autotuninglevel=normal", admin=True)

    assert success is False
    assert "administrator privileges" in output.lower()

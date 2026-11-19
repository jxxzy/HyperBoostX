from app.utils.shell import ShellUtil


def test_admin_command_returns_clear_message_when_not_elevated(monkeypatch):
    monkeypatch.setattr("app.utils.shell.Permissions.is_admin", lambda: False)

    success, output = ShellUtil.execute_command("netsh int tcp set global autotuninglevel=normal", admin=True)

    assert success is False
    assert "administrator privileges" in output.lower()


def test_non_admin_command_executes_through_powershell():
    success, output = ShellUtil.execute_command("Write-Output 'hyperboost-shell-ok'")

    assert success is True
    assert output == "hyperboost-shell-ok"


def test_run_powershell_handles_quoted_script():
    success, output = ShellUtil.run_powershell('Write-Output "quoted value"')

    assert success is True
    assert output == "quoted value"

import logging

from app.utils.shell import ShellUtil


def test_admin_command_returns_clear_message_when_not_elevated(monkeypatch, caplog):
    monkeypatch.setattr("app.utils.shell.Permissions.is_admin", lambda: False)

    with caplog.at_level(logging.INFO):
        success, output = ShellUtil.execute_command("netsh int tcp set global autotuninglevel=normal", admin=True)

    assert success is False
    assert "administrator privileges" in output.lower()
    admin_records = [record for record in caplog.records if "Admin command skipped without elevation" in record.message]
    assert admin_records
    assert all(record.levelno == logging.INFO for record in admin_records)


def test_non_admin_command_executes_through_powershell():
    success, output = ShellUtil.execute_command("Write-Output 'hyperboost-shell-ok'")

    assert success is True
    assert output == "hyperboost-shell-ok"


def test_run_powershell_handles_quoted_script():
    success, output = ShellUtil.run_powershell('Write-Output "quoted value"')

    assert success is True
    assert output == "quoted value"


def test_shell_util_blocks_non_allowlisted_command():
    success, output = ShellUtil.execute_command("Get-ChildItem C:\\")

    assert success is False
    assert "not allowed" in output.lower()


def test_shell_util_allows_battery_display_timeout_command(monkeypatch):
    calls = []

    class FakeProcess:
        returncode = 0
        stdout = "ok"
        stderr = ""

    def fake_run(args, stdout, stderr, text, timeout):
        calls.append(args)
        return FakeProcess()

    monkeypatch.setattr("app.utils.shell.Permissions.is_admin", lambda: True)
    monkeypatch.setattr("app.utils.shell.subprocess.run", fake_run)

    success, output = ShellUtil.execute_command("powercfg /change monitor-timeout-dc 300", admin=True)

    assert success is True
    assert output == "ok"
    assert calls

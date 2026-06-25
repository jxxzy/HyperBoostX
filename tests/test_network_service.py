from app.services.optimization.network_service import NetworkService


def test_flush_dns_reports_admin_required(monkeypatch):
    monkeypatch.setattr(
        "app.services.optimization.network_service.ShellUtil.execute_command",
        lambda command, admin=False: (
            False,
            "This action requires administrator privileges. Run HyperBoostX as Administrator.",
        ),
    )

    result = NetworkService.flush_dns()

    assert result["success"] is False
    assert result["reason_code"] == "admin_required"
    assert result["requires_admin"] is True


def test_optimize_tcp_reports_admin_required(monkeypatch):
    monkeypatch.setattr(
        "app.services.optimization.network_service.ShellUtil.execute_command",
        lambda command, admin=False: (
            False,
            "This action requires administrator privileges. Run HyperBoostX as Administrator.",
        ),
    )

    result = NetworkService.optimize_tcp()

    assert result["success"] is False
    assert result["reason_code"] == "admin_required"
    assert result["requires_admin"] is True

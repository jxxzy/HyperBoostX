import time
from pathlib import Path

from app.backend_server import HyperBoostBackendServer
from services.monitoring.crash_report_service import CrashReportService
from services.monitoring.report_service import ReportService


REPO_ROOT = Path(__file__).resolve().parents[1]


def _expected_version():
    return (REPO_ROOT / "VERSION").read_text(encoding="utf-8").strip()


def _expected_channel(version):
    return "Beta" if "-" in version else "Stable"


def test_health_and_version_match_runtime_version():
    server = HyperBoostBackendServer()
    client = server.app.test_client()
    expected_version = _expected_version()
    expected_channel = _expected_channel(expected_version)

    health = client.get("/api/health")
    version = client.get("/api/version")
    readiness = client.get("/api/release/readiness")

    assert health.status_code == 200
    assert health.get_json()["version"] == expected_version
    version_payload = version.get_json()
    assert version_payload["release"] == f"HyperBoostX v{expected_version} {expected_channel}"
    assert version_payload["channel"] == expected_channel
    assert version_payload["stable"] is (expected_channel == "Stable")
    readiness_payload = readiness.get_json()
    if expected_channel == "Stable":
        assert readiness_payload["status"] == "stable_ready_unsigned"
        assert readiness_payload["blocking_gates"] == []
    else:
        assert readiness_payload["status"] == "beta_ready"
        assert "installed_runtime_verification" in readiness_payload["blocking_gates"]
    assert readiness_payload["stable"] is (expected_channel == "Stable")
    assert readiness_payload["manual_lab_required"] is (expected_channel != "Stable")


def test_required_v13_read_endpoints_exist(monkeypatch):
    server = HyperBoostBackendServer()
    client = server.app.test_client()
    monkeypatch.setattr("services.optimization.startup_service.StartupService.get_startup_items", lambda self: [])

    for path in [
        "/api/system/stats",
        "/api/system/info",
        "/api/system/startup",
        "/api/system/processes",
        "/api/hardware/profile",
        "/api/hardware/gpu",
        "/api/hardware/vendors",
        "/api/hardware/overlays",
        "/api/reports/latest",
    ]:
        response = client.get(path)
        assert response.status_code == 200, path

def test_crash_report_export_api_redacts_secrets(monkeypatch):
    monkeypatch.delenv("HYPERBOOSTX_SESSION_TOKEN", raising=False)
    monkeypatch.setenv("USERNAME", "JaneDoe")
    server = HyperBoostBackendServer()
    client = server.app.test_client()

    response = client.post("/api/reports/crash-export", json={
        "format": "json",
        "error_message": "api_key=SECRET123 token=abc123",
        "stack_trace": r"C:\Users\JaneDoe\.codex\secret.txt github_token=ghp_hidden",
        "last_action": "GPU Center refresh",
        "backend_status": "ok",
    })

    assert response.status_code == 200
    payload = response.get_json()
    content = payload["content"]
    assert payload["format"] == "json"
    assert "SECRET123" not in content
    assert "abc123" not in content
    assert "ghp_hidden" not in content
    assert "JaneDoe" not in content
    assert "[REDACTED" in content


def test_session_token_rejects_unauthorized_mutating_endpoint(monkeypatch):
    monkeypatch.setenv("HYPERBOOSTX_SESSION_TOKEN", "unit-test-token")
    server = HyperBoostBackendServer()
    client = server.app.test_client()

    denied = client.post("/api/boost/plan", json={"goal": "gaming"})
    allowed = client.post(
        "/api/boost/plan",
        json={"goal": "gaming"},
        headers={"X-HyperBoostX-Session": "unit-test-token"},
    )

    assert denied.status_code == 401
    assert allowed.status_code == 200


def test_boost_apply_requires_user_approval(monkeypatch):
    monkeypatch.delenv("HYPERBOOSTX_SESSION_TOKEN", raising=False)
    server = HyperBoostBackendServer()
    client = server.app.test_client()

    plan = client.post("/api/boost/plan", json={"goal": "gaming"})
    blocked = client.post("/api/boost/apply", json={})
    applied = client.post("/api/boost/apply", json={"user_approved": True})

    assert plan.status_code == 200
    assert blocked.status_code == 409
    assert blocked.get_json()["requires_approval"] is True
    assert applied.status_code == 200
    assert applied.get_json()["safety_guard"] == "Active"


def test_job_queue_lifecycle(monkeypatch):
    monkeypatch.delenv("HYPERBOOSTX_SESSION_TOKEN", raising=False)
    server = HyperBoostBackendServer()
    client = server.app.test_client()

    started = client.post("/api/jobs/start", json={"job_type": "cleanup"})
    assert started.status_code == 200
    job_id = started.get_json()["job_id"]

    latest = None
    for _ in range(20):
        latest = client.get(f"/api/jobs/{job_id}")
        if latest.get_json()["status"] in {"completed", "failed", "canceled"}:
            break
        time.sleep(0.02)

    assert latest is not None
    payload = latest.get_json()
    assert payload["job_id"] == job_id
    assert payload["progress"] >= 0
    assert payload["status"] in {"running", "completed"}


def test_report_export_schema():
    report = ReportService.build_report(
        before={"cpu_idle_usage_percent": 7, "ram_usage_percent": 48, "pc_health_score": 80},
        after={"cpu_idle_usage_percent": 2, "ram_usage_percent": 36, "pc_health_score": 88},
    )
    exported = ReportService.export_report("md", report)

    assert report["title"] == "HyperBoostX Performance Report"
    assert exported["format"] == "md"
    assert "CPU Idle: 7 -> 2" in exported["content"]

def test_crash_report_service_exports_local_only_report(monkeypatch):
    monkeypatch.setenv("USERNAME", "JaneDoe")
    exported = CrashReportService.export_report({
        "format": "md",
        "error_message": "Bearer SECRET_TOKEN",
        "stack_trace": r"C:\Users\JaneDoe\AppData\Local\token.txt",
        "last_action": "One Click Boost",
    }, "md")

    assert exported["format"] == "md"
    assert "HyperBoostX Local Crash Report" in exported["content"]
    assert "SECRET_TOKEN" not in exported["content"]
    assert "JaneDoe" not in exported["content"]
    assert exported["report"]["privacy"] == "local_only_manual_export"

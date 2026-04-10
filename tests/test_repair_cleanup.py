from app.backend_server import HyperBoostBackendServer
from services.repair.repair_service import RepairService


def test_cleanup_scope_safe_all_aggregates_category_results(monkeypatch):
    monkeypatch.setattr(
        RepairService,
        "_cleanup_temp_targets",
        staticmethod(lambda: {
            "name": "temp_files",
            "freed_bytes": 1024,
            "freed_mb": 1,
            "deleted_files": 2,
            "deleted_directories": 1,
            "paths": ["temp"],
        }),
    )
    monkeypatch.setattr(
        RepairService,
        "_cleanup_browser_cache_targets",
        staticmethod(lambda: {
            "name": "browser_cache",
            "freed_bytes": 2048,
            "freed_mb": 2,
            "deleted_files": 3,
            "deleted_directories": 0,
            "paths": ["browser"],
        }),
    )
    monkeypatch.setattr(
        RepairService,
        "_cleanup_log_targets",
        staticmethod(lambda: {
            "name": "logs_and_reports",
            "freed_bytes": 4096,
            "freed_mb": 4,
            "deleted_files": 5,
            "deleted_directories": 2,
            "paths": ["logs"],
        }),
    )

    report = RepairService.cleanup_temp_files("safe_all")

    assert report["success"] is True
    assert report["freed_bytes"] == 1024 + 2048 + 4096
    assert report["deleted_files"] == 2 + 3 + 5
    assert report["deleted_directories"] == 1 + 0 + 2
    assert report["categories"]["temp_files"]["freed_bytes"] == 1024
    assert report["categories"]["browser_cache"]["freed_bytes"] == 2048
    assert report["categories"]["logs_and_reports"]["freed_bytes"] == 4096


def test_cleanup_api_accepts_scope(monkeypatch):
    server = HyperBoostBackendServer()
    client = server.app.test_client()

    def fake_cleanup(scope):
        return {
            "success": True,
            "scope": scope,
            "freed_bytes": 1234,
            "freed_mb": 1,
            "deleted_files": 2,
            "deleted_directories": 0,
            "categories": {},
            "summary": ["ok"],
        }

    monkeypatch.setattr("api.repair.repair_service.cleanup_temp_files", fake_cleanup)

    response = client.post("/api/repair/cleanup", json={"scope": "browser_cache"})

    assert response.status_code == 200
    payload = response.get_json()
    assert payload["scope"] == "browser_cache"
    assert payload["freed_bytes"] == 1234

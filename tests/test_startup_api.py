from app.backend_server import HyperBoostBackendServer


def test_startup_list_returns_legacy_and_new_keys(monkeypatch):
    server = HyperBoostBackendServer()
    client = server.app.test_client()
    sample_items = [{"name": "Discord", "enabled": True, "impact": "High"}]

    monkeypatch.setattr(
        "api.startup.startup_service.get_startup_items",
        lambda: sample_items,
    )

    response = client.get("/api/startup/list")

    assert response.status_code == 200
    payload = response.get_json()
    assert payload["items"] == sample_items
    assert payload["startup_items"] == sample_items
    assert payload["count"] == len(sample_items)


def test_startup_list_returns_cached_payload_when_collector_is_slow(monkeypatch):
    server = HyperBoostBackendServer()
    client = server.app.test_client()
    cached_items = [{"name": "Discord", "enabled": True, "impact": "High"}]

    monkeypatch.setattr("services.optimization.startup_service.StartupService._cache_items", cached_items)
    monkeypatch.setattr("services.optimization.startup_service.StartupService._cache_utc", 10_000.0)
    monkeypatch.setattr("services.optimization.startup_service.time.time", lambda: 10_005.0)

    def fail_if_called(*args, **kwargs):
        raise AssertionError("collector should not run when startup cache is still fresh")

    monkeypatch.setattr("services.optimization.startup_service.StartupService._read_registry_startup_items", staticmethod(fail_if_called))
    monkeypatch.setattr("services.optimization.startup_service.StartupService._read_startup_folder_items", staticmethod(fail_if_called))
    monkeypatch.setattr("services.optimization.startup_service.StartupService._read_scheduled_tasks", staticmethod(fail_if_called))
    monkeypatch.setattr("services.optimization.startup_service.StartupService._read_startup_services", staticmethod(fail_if_called))

    response = client.get("/api/startup/list")

    assert response.status_code == 200
    payload = response.get_json()
    assert payload["items"] == cached_items
    assert payload["startup_items"] == cached_items

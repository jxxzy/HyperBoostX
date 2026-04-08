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

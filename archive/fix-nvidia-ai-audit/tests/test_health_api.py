import pytest
from app.backend_server import HyperBoostBackendServer


def test_backend_server_initializes():
    server = HyperBoostBackendServer()
    assert server.host == "127.0.0.1"
    assert server.port == 5000
    assert not server.running


def test_cors_allows_localhost_origin():
    server = HyperBoostBackendServer()

    response = server.app.test_client().get(
        "/api/health",
        headers={
            "Origin": "http://localhost:5173",
            "X-HyperBoostX-Token": server.auth_token,
        },
    )

    assert response.headers["Access-Control-Allow-Origin"] == "http://localhost:5173"


def test_cors_rejects_non_local_origin():
    server = HyperBoostBackendServer()

    response = server.app.test_client().get(
        "/api/health",
        headers={
            "Origin": "https://example.com",
            "X-HyperBoostX-Token": server.auth_token,
        },
    )

    assert "Access-Control-Allow-Origin" not in response.headers


def test_backend_rejects_missing_token():
    server = HyperBoostBackendServer(auth_token="test-token")

    response = server.app.test_client().get("/api/health")

    assert response.status_code == 401

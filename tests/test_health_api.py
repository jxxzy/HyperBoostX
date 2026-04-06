import pytest
from app.backend_server import HyperBoostBackendServer


def test_backend_server_initializes():
    server = HyperBoostBackendServer()
    assert server.host == "127.0.0.1"
    assert server.port == 5000
    assert not server.running

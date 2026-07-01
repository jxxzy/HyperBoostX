"""Shared constants for HyperBoostX."""

import os


def _resolve_backend_port() -> int:
    raw_port = os.environ.get("HYPERBOOSTX_BACKEND_PORT", "5000")
    try:
        port = int(raw_port)
    except (TypeError, ValueError):
        return 5000

    return port if 1024 <= port <= 65535 else 5000

APP_NAME = "HyperBoostX"
APP_VERSION = "2.10.0"
BACKEND_HOST = "127.0.0.1"
BACKEND_PORT = _resolve_backend_port()
BACKEND_URL = f"http://{BACKEND_HOST}:{BACKEND_PORT}"
DEFAULT_LOG_LEVEL = "INFO"

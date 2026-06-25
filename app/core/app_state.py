"""Application-wide runtime state container."""

from dataclasses import dataclass
from typing import Optional


@dataclass
class AppState:
    """Shared runtime state for HyperBoostX."""
    initialized: bool = False
    last_health_check: Optional[float] = None
    backend_url: str = "http://127.0.0.1:5000"

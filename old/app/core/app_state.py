"""Application-wide runtime state container."""

from dataclasses import dataclass
from typing import Optional

from core.constants import BACKEND_URL


@dataclass
class AppState:
    """Shared runtime state for HyperBoostX."""
    initialized: bool = False
    last_health_check: Optional[float] = None
    backend_url: str = BACKEND_URL

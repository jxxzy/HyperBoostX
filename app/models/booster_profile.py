"""Booster profile domain model."""

from dataclasses import dataclass
from typing import Dict, Any


@dataclass
class BoosterProfile:
    id: str
    name: str
    description: str
    settings: Dict[str, Any]
    risk_level: str = "low"

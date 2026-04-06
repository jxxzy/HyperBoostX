"""Tweak domain model for HyperBoost X."""

from dataclasses import dataclass
from typing import Dict, Any


@dataclass
class Tweak:
    id: str
    name: str
    description: str
    category: str
    risk_level: str
    parameters: Dict[str, Any]

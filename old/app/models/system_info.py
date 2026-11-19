"""System information domain model."""

from dataclasses import dataclass
from typing import Dict, Any


@dataclass
class SystemInfo:
    cpu: Dict[str, Any]
    memory: Dict[str, Any]
    disk: Dict[str, Any]
    network: Dict[str, Any]
    os: Dict[str, Any]
    gpu: Dict[str, Any]
    temperatures: Dict[str, Any]

"""Validation helpers for system data schemas."""

from typing import Any, Dict


def validate_system_info(data: Dict[str, Any]) -> bool:
    """Validate the base system information payload."""
    required_keys = ["cpu", "memory", "disk", "network", "os", "gpu", "temperatures"]
    if not isinstance(data, dict):
        return False
    return all(key in data for key in required_keys)

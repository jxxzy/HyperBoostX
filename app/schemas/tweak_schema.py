"""Validation helpers for tweak catalog data."""

from typing import Any, Dict, List


def validate_tweak_catalog(data: List[Dict[str, Any]]) -> bool:
    """Validate the tweak catalog payload structure."""
    if not isinstance(data, list):
        return False
    for item in data:
        if not isinstance(item, dict):
            return False
        if not all(key in item for key in ["id", "name", "description", "category", "risk_level"]):
            return False
    return True

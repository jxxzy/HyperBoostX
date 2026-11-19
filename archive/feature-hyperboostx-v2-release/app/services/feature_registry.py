"""Feature visibility registry for HyperBoostX v2.10.

The registry intentionally keeps the full beta/dev action map available while
making Stable mode prove that only real, non-misleading features are visible.
"""

from __future__ import annotations

import os
import json
from pathlib import Path
from typing import Any, Dict, List

from core.constants import APP_VERSION


REAL_STATUS = "Real"
NON_REAL_STATUSES = {"Partial", "Preview only", "Guidance only", "Roadmap"}


class FeatureRegistryService:
    """Loads the shared WPF action map and applies Stable/Dev visibility rules."""

    @staticmethod
    def repo_root() -> Path:
        return Path(__file__).resolve().parents[2]

    @classmethod
    def action_map_path(cls) -> Path:
        return cls.repo_root() / "wpf" / "Data" / "ui_action_map_v2_10.json"

    @staticmethod
    def mode() -> str:
        raw = os.environ.get("HYPERBOOSTX_MODE", "stable").strip().lower()
        return "dev" if raw in {"dev", "development", "internal"} else "stable"

    @staticmethod
    def show_experimental() -> bool:
        raw = os.environ.get("HYPERBOOSTX_SHOW_EXPERIMENTAL", "false").strip().lower()
        return raw in {"1", "true", "yes"} or FeatureRegistryService.mode() == "dev"

    @staticmethod
    def require_real_features() -> bool:
        raw = os.environ.get("HYPERBOOSTX_REQUIRE_REAL_FEATURES", "true").strip().lower()
        return raw not in {"0", "false", "no"}

    @staticmethod
    def block_non_real_stable_ui() -> bool:
        raw = os.environ.get("HYPERBOOSTX_BLOCK_NON_REAL_STABLE_UI", "true").strip().lower()
        return raw not in {"0", "false", "no"}

    @classmethod
    def load(cls) -> Dict[str, Any]:
        path = cls.action_map_path()
        try:
            payload = json.loads(path.read_text(encoding="utf-8-sig")) if path.exists() else {}
        except (OSError, json.JSONDecodeError):
            payload = {}
        menus = payload.get("menus") if isinstance(payload, dict) else None
        if not isinstance(menus, list):
            menus = []

        normalized = [cls._normalize_menu(menu) for menu in menus if isinstance(menu, dict)]
        normalized = [menu for menu in normalized if menu["key"]]
        for feature in normalized:
            feature["stable_visible"] = cls._is_stable_visible(feature)
        return {
            "schema_version": payload.get("schema_version", "2.10.0") if isinstance(payload, dict) else "2.10.0",
            "app_version": payload.get("app_version", APP_VERSION) if isinstance(payload, dict) else APP_VERSION,
            "mode": cls.mode(),
            "show_experimental": cls.show_experimental(),
            "require_real_features": cls.require_real_features(),
            "block_non_real_stable_ui": cls.block_non_real_stable_ui(),
            "source": str(path),
            "features": normalized,
        }

    @classmethod
    def stable_visible(cls) -> List[Dict[str, Any]]:
        registry = cls.load()
        return [feature for feature in registry["features"] if cls._is_stable_visible(feature)]

    @classmethod
    def non_real(cls) -> List[Dict[str, Any]]:
        registry = cls.load()
        return [feature for feature in registry["features"] if feature["status"] != REAL_STATUS or not feature["all_actions_real"]]

    @classmethod
    def current_visible(cls) -> List[Dict[str, Any]]:
        registry = cls.load()
        if registry["mode"] == "dev" or registry["show_experimental"] or not registry["block_non_real_stable_ui"]:
            return registry["features"]
        return [feature for feature in registry["features"] if cls._is_stable_visible(feature)]

    @classmethod
    def audit(cls) -> Dict[str, Any]:
        registry = cls.load()
        stable_visible = [feature for feature in registry["features"] if cls._is_stable_visible(feature)]
        non_real = [feature for feature in registry["features"] if feature["status"] != REAL_STATUS or not feature["all_actions_real"]]
        current_visible = cls.current_visible()
        non_real_visible = [feature for feature in current_visible if feature["status"] != REAL_STATUS or not feature["all_actions_real"]]
        return {
            "ok": len(non_real_visible) == 0 if registry["mode"] == "stable" and registry["block_non_real_stable_ui"] else True,
            "app_version": registry["app_version"],
            "mode": registry["mode"],
            "policy": {
                "show_experimental": registry["show_experimental"],
                "require_real_features": registry["require_real_features"],
                "block_non_real_stable_ui": registry["block_non_real_stable_ui"],
            },
            "counts": {
                "total_original_features": len(registry["features"]),
                "stable_visible_features": len(stable_visible),
                "current_visible_features": len(current_visible),
                "hidden_from_stable": len(registry["features"]) - len(stable_visible),
                "non_real_total": len(non_real),
                "non_real_visible_in_stable": len(non_real_visible),
                "stable_visible_buttons": sum(feature["button_count"] for feature in stable_visible),
                "current_visible_buttons": sum(feature["button_count"] for feature in current_visible),
            },
            "stable_visible": stable_visible,
            "non_real": non_real,
            "hidden_from_stable": [feature for feature in registry["features"] if not cls._is_stable_visible(feature)],
            "message": "Stable UI contains only real features." if not non_real_visible else "Stable UI contains non-real features and must be blocked.",
        }

    @staticmethod
    def _normalize_menu(menu: Dict[str, Any]) -> Dict[str, Any]:
        actions = [action for action in menu.get("actions", []) if isinstance(action, dict)]
        real_actions = [
            action for action in actions
            if action.get("status", REAL_STATUS) == REAL_STATUS and not bool(action.get("partial"))
        ]
        return {
            "key": str(menu.get("key") or ""),
            "label": str(menu.get("label") or menu.get("key") or ""),
            "category": str(menu.get("category") or "Uncategorized"),
            "status": str(menu.get("status") or REAL_STATUS),
            "readiness": str(menu.get("status") or REAL_STATUS),
            "big": bool(menu.get("big")),
            "button_count": len(actions),
            "real_button_count": len(real_actions),
            "all_actions_real": len(actions) == len(real_actions) and len(actions) > 0,
            "actions": actions,
            "stable_visible": False,
        }

    @staticmethod
    def _is_stable_visible(feature: Dict[str, Any]) -> bool:
        return feature["status"] == REAL_STATUS and bool(feature["all_actions_real"])

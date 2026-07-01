"""Feature visibility registry for HyperBoostX v2.10.

The registry intentionally keeps the full beta/dev action map available while
making Stable mode prove that only real, non-misleading features are visible.
"""

from __future__ import annotations

import os
import json
import sys
from pathlib import Path
from typing import Any, Dict, List, Tuple

from core.constants import APP_VERSION


REAL_STATUS = "Real"
NON_REAL_STATUSES = {"Partial", "Preview only", "Guidance only", "Roadmap"}
EXPECTED_STABLE_MENUS = 72
EXPECTED_STABLE_BUTTONS = 596
EXPECTED_UNIQUE_UI_ENDPOINTS = 165
EXPECTED_NON_REAL_VISIBLE_IN_STABLE = 0
ACTION_MAP_RELATIVE_PATH = Path("Data") / "ui_action_map_v2_10.json"


class FeatureRegistryService:
    """Loads the shared WPF action map and applies Stable/Dev visibility rules."""

    @staticmethod
    def repo_root() -> Path:
        return Path(__file__).resolve().parents[2]

    @classmethod
    def action_map_path(cls) -> Path:
        resolved, _ = cls._resolve_action_map_path()
        return resolved

    @classmethod
    def _candidate_action_map_paths(cls) -> List[Path]:
        candidates: List[Path] = []
        override = os.environ.get("HYPERBOOSTX_ACTION_MAP_PATH", "").strip()
        if override:
            candidates.append(Path(override))

        repo_root = cls.repo_root()
        candidates.append(repo_root / "wpf" / ACTION_MAP_RELATIVE_PATH)

        executable_dir = Path(sys.executable).resolve().parent if getattr(sys, "executable", "") else Path.cwd()
        runtime_bases = [
            executable_dir,
            Path.cwd().resolve(),
            Path(__file__).resolve().parent,
        ]
        if getattr(sys, "frozen", False):
            runtime_bases.append(Path(getattr(sys, "_MEIPASS", executable_dir)).resolve())

        for base in runtime_bases:
            candidates.extend([
                base / "wpf" / ACTION_MAP_RELATIVE_PATH,
                base / ".." / "wpf" / ACTION_MAP_RELATIVE_PATH,
                base / "runtime" / "wpf" / ACTION_MAP_RELATIVE_PATH,
                base / ".." / "runtime" / "wpf" / ACTION_MAP_RELATIVE_PATH,
                base / ".." / ".." / "runtime" / "wpf" / ACTION_MAP_RELATIVE_PATH,
            ])

        unique: List[Path] = []
        seen = set()
        for candidate in candidates:
            try:
                normalized = candidate.expanduser().resolve()
            except OSError:
                normalized = candidate.expanduser().absolute()
            key = str(normalized).lower()
            if key not in seen:
                seen.add(key)
                unique.append(normalized)
        return unique

    @classmethod
    def _resolve_action_map_path(cls) -> Tuple[Path, List[Path]]:
        candidates = cls._candidate_action_map_paths()
        for candidate in candidates:
            if candidate.exists():
                return candidate, candidates
        return candidates[0] if candidates else cls.repo_root() / "wpf" / ACTION_MAP_RELATIVE_PATH, candidates

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
        path, candidates = cls._resolve_action_map_path()
        errors: List[str] = []
        warnings: List[str] = []
        source_found = path.exists()
        try:
            payload = json.loads(path.read_text(encoding="utf-8-sig")) if source_found else {}
        except (OSError, json.JSONDecodeError) as exc:
            errors.append(f"action map parse failed: {exc}")
            payload = {}
        if not source_found:
            errors.append("action map not found")
        menus = payload.get("menus") if isinstance(payload, dict) else None
        if not isinstance(menus, list):
            warnings.append("action map menus collection is missing or invalid")
            menus = []

        normalized = [cls._normalize_menu(menu) for menu in menus if isinstance(menu, dict)]
        normalized = [menu for menu in normalized if menu["key"]]
        for feature in normalized:
            feature["stable_visible"] = cls._is_stable_visible(feature)
        summary = payload.get("summary", {}) if isinstance(payload, dict) and isinstance(payload.get("summary"), dict) else {}
        return {
            "schema_version": payload.get("schema_version", "2.10.0") if isinstance(payload, dict) else "2.10.0",
            "app_version": payload.get("app_version", APP_VERSION) if isinstance(payload, dict) else APP_VERSION,
            "channel": payload.get("channel", "Stable" if "-" not in APP_VERSION else "Beta") if isinstance(payload, dict) else ("Stable" if "-" not in APP_VERSION else "Beta"),
            "mode": cls.mode(),
            "show_experimental": cls.show_experimental(),
            "require_real_features": cls.require_real_features(),
            "block_non_real_stable_ui": cls.block_non_real_stable_ui(),
            "source": str(path),
            "source_found": source_found,
            "source_candidates": [str(candidate) for candidate in candidates],
            "summary": summary,
            "expected_contract": cls.expected_contract(),
            "warnings": warnings,
            "errors": errors,
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
        stable_visible_buttons = sum(feature["button_count"] for feature in stable_visible)
        current_visible_buttons = sum(feature["button_count"] for feature in current_visible)
        total_buttons = sum(feature["button_count"] for feature in registry["features"])
        unique_endpoints = {
            f"{str(action.get('method', 'GET')).upper()} {str(action.get('path', '')).split('?', 1)[0]}"
            for feature in registry["features"]
            for action in feature.get("actions", [])
            if str(action.get("path", "")).startswith("/api/")
        }
        stable_contract_ok = (
            bool(registry["source_found"])
            and len(stable_visible) == EXPECTED_STABLE_MENUS
            and stable_visible_buttons == EXPECTED_STABLE_BUTTONS
            and len(non_real_visible) == EXPECTED_NON_REAL_VISIBLE_IN_STABLE
            and len(unique_endpoints) == EXPECTED_UNIQUE_UI_ENDPOINTS
        )
        stable_ui_ok = stable_contract_ok if registry["mode"] == "stable" and registry["block_non_real_stable_ui"] else True
        errors = list(registry["errors"])
        warnings = list(registry["warnings"])
        if registry["mode"] == "stable" and not stable_contract_ok:
            errors.append(
                "stable feature registry contract mismatch: "
                f"menus={len(stable_visible)}/{EXPECTED_STABLE_MENUS}, "
                f"buttons={stable_visible_buttons}/{EXPECTED_STABLE_BUTTONS}, "
                f"non_real_visible={len(non_real_visible)}/{EXPECTED_NON_REAL_VISIBLE_IN_STABLE}, "
                f"unique_endpoints={len(unique_endpoints)}/{EXPECTED_UNIQUE_UI_ENDPOINTS}"
            )
        return {
            "ok": stable_ui_ok,
            "app_version": registry["app_version"],
            "channel": registry["channel"],
            "mode": registry["mode"],
            "source": registry["source"],
            "source_found": registry["source_found"],
            "source_candidates": registry["source_candidates"],
            "expected_contract": registry["expected_contract"],
            "warnings": warnings,
            "errors": errors,
            "policy": {
                "show_experimental": registry["show_experimental"],
                "require_real_features": registry["require_real_features"],
                "block_non_real_stable_ui": registry["block_non_real_stable_ui"],
            },
            "counts": {
                "total_original_features": len(registry["features"]),
                "total_buttons": total_buttons,
                "total_unique_endpoints_used": len(unique_endpoints),
                "stable_visible_features": len(stable_visible),
                "current_visible_features": len(current_visible),
                "hidden_from_stable": len(registry["features"]) - len(stable_visible),
                "non_real_total": len(non_real),
                "non_real_visible_in_stable": len(non_real_visible),
                "stable_visible_buttons": stable_visible_buttons,
                "current_visible_buttons": current_visible_buttons,
            },
            "stable_visible": stable_visible,
            "non_real": non_real,
            "hidden_from_stable": [feature for feature in registry["features"] if not cls._is_stable_visible(feature)],
            "message": "Stable UI contract is valid." if stable_ui_ok else "Stable UI contract failed and must be blocked.",
        }

    @staticmethod
    def expected_contract() -> Dict[str, int]:
        return {
            "expected_stable_menus": EXPECTED_STABLE_MENUS,
            "expected_stable_buttons": EXPECTED_STABLE_BUTTONS,
            "expected_unique_ui_endpoints": EXPECTED_UNIQUE_UI_ENDPOINTS,
            "expected_non_real_visible_in_stable": EXPECTED_NON_REAL_VISIBLE_IN_STABLE,
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

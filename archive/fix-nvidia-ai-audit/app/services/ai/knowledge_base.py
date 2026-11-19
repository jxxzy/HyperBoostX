"""Local RAG-style knowledge base for HyperBoostX Triple AI Engine."""

import json
import re
import sys
from pathlib import Path
from typing import Any, Dict, Iterable, List

from core.logger import Logger


logger = Logger.get_logger(__name__)


class KnowledgeBase:
    """Small local retrieval layer used before any AI text is generated."""

    DEFAULT_FILE = "hyperboost_knowledge_base.json"

    def __init__(self, path: Path | None = None):
        self.path = path or self._default_path()
        self.data = self._load()

    @classmethod
    def _default_path(cls) -> Path:
        bundled_root = Path(getattr(sys, "_MEIPASS", Path(__file__).resolve().parents[2]))
        candidates = [
            bundled_root / "data" / cls.DEFAULT_FILE,
            Path(__file__).resolve().parents[2] / "data" / cls.DEFAULT_FILE,
        ]
        for candidate in candidates:
            if candidate.exists():
                return candidate
        return candidates[-1]

    def _load(self) -> Dict[str, Any]:
        try:
            return json.loads(self.path.read_text(encoding="utf-8"))
        except Exception as exc:
            logger.error("Failed to load HyperBoostX knowledge base: %s", exc)
            return {}

    def get_tweaks(self) -> List[Dict[str, Any]]:
        return list(self.data.get("tweak_database", []))

    def get_tweak(self, tweak_id: str) -> Dict[str, Any] | None:
        normalized = (tweak_id or "").strip().lower()
        for tweak in self.get_tweaks():
            if str(tweak.get("tweak_id", "")).lower() == normalized:
                return dict(tweak)
        return None

    def safety_policy(self) -> Dict[str, Any]:
        return dict(self.data.get("safety_policy_database", {}))

    def search(self, query: str, categories: Iterable[str] | None = None, limit: int = 5) -> List[Dict[str, Any]]:
        """Return simple keyword-ranked KB snippets.

        This is intentionally local and deterministic. It gives the Assistant and
        Analyzer grounding data without introducing a fourth user-visible AI role.
        """

        tokens = self._tokens(query)
        if not tokens:
            return []

        allowed = set(categories or [])
        entries = self._flatten_entries()
        if allowed:
            entries = [entry for entry in entries if entry["category"] in allowed]

        scored = []
        for entry in entries:
            haystack = entry["text"].lower()
            score = sum(1 for token in tokens if token in haystack)
            if score:
                scored.append((score, entry))

        scored.sort(key=lambda item: item[0], reverse=True)
        return [entry for _, entry in scored[: max(1, limit)]]

    @staticmethod
    def _tokens(query: str) -> List[str]:
        return [token for token in re.findall(r"[a-z0-9_+.-]+", (query or "").lower()) if len(token) >= 3]

    def _flatten_entries(self) -> List[Dict[str, Any]]:
        flattened: List[Dict[str, Any]] = []
        for category in (
            "tweak_database",
            "game_setting_database",
            "nvidia_setting_database",
            "error_knowledge_base",
            "benchmark_database",
        ):
            for item in self.data.get(category, []):
                flattened.append({
                    "category": category,
                    "id": item.get("tweak_id") or item.get("game") or item.get("setting") or item.get("error") or item.get("hardware_profile"),
                    "text": json.dumps(item, ensure_ascii=True, sort_keys=True),
                    "item": item,
                })

        policy = self.data.get("safety_policy_database")
        if policy:
            flattened.append({
                "category": "safety_policy_database",
                "id": "safety_policy",
                "text": json.dumps(policy, ensure_ascii=True, sort_keys=True),
                "item": policy,
            })
        return flattened

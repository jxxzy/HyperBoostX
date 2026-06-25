"""Internal knowledge base and lightweight retrieval for HyperBoostX."""

import json
import re
from pathlib import Path
from typing import Any, Dict, List

from core.logger import Logger


logger = Logger.get_logger(__name__)


class KnowledgeBaseService:
    """Loads HyperBoostX policy/game/tweak data and provides simple local search."""

    DEFAULT_KB_PATH = Path(__file__).resolve().parents[2] / "data" / "hyperboost_knowledge_base.json"

    def __init__(self, kb_path: Path | None = None):
        self.kb_path = kb_path or self.DEFAULT_KB_PATH
        self._data: Dict[str, Any] = {}
        self._documents: List[Dict[str, Any]] = []
        self.reload()

    def reload(self) -> None:
        try:
            self._data = json.loads(self.kb_path.read_text(encoding="utf-8"))
            self._documents = self._build_documents(self._data)
            logger.info("HyperBoostX knowledge base loaded: %s", self.kb_path)
        except Exception as exc:
            logger.error("Failed to load HyperBoostX knowledge base: %s", exc)
            self._data = {}
            self._documents = []

    @property
    def data(self) -> Dict[str, Any]:
        return self._data

    def metadata(self) -> Dict[str, Any]:
        return dict(self._data.get("metadata") or {})

    def tweak_database(self) -> List[Dict[str, Any]]:
        return list(self._data.get("tweak_database") or [])

    def game_database(self) -> List[Dict[str, Any]]:
        return list(self._data.get("game_setting_database") or [])

    def nvidia_database(self) -> List[Dict[str, Any]]:
        return list(self._data.get("nvidia_setting_database") or [])

    def safety_policy(self) -> Dict[str, Any]:
        return dict(self._data.get("safety_policy_database") or {})

    def find_tweak(self, tweak_id: str) -> Dict[str, Any]:
        normalized = (tweak_id or "").strip().lower()
        for item in self.tweak_database():
            if (item.get("tweak_id") or item.get("id") or "").lower() == normalized:
                return dict(item)
        return {}

    def find_game(self, game_name: str) -> Dict[str, Any]:
        normalized = self._normalize(game_name)
        for item in self.game_database():
            if self._normalize(item.get("game", "")) == normalized:
                return dict(item)
        for item in self.game_database():
            if normalized and normalized in self._normalize(item.get("game", "")):
                return dict(item)
        return {}

    def search(self, query: str, limit: int = 5) -> List[Dict[str, Any]]:
        """Return top local knowledge snippets by token overlap."""
        query_tokens = set(self._tokens(query))
        if not query_tokens:
            return []

        scored: List[tuple[int, Dict[str, Any]]] = []
        for doc in self._documents:
            score = len(query_tokens.intersection(doc["tokens"]))
            if score > 0:
                scored.append((score, doc))

        scored.sort(key=lambda item: item[0], reverse=True)
        return [
            {
                "category": doc["category"],
                "id": doc["id"],
                "title": doc["title"],
                "summary": doc["summary"],
                "score": score,
            }
            for score, doc in scored[: max(1, limit)]
        ]

    @staticmethod
    def _build_documents(data: Dict[str, Any]) -> List[Dict[str, Any]]:
        documents: List[Dict[str, Any]] = []

        def add(category: str, doc_id: str, title: str, payload: Dict[str, Any]) -> None:
            text = json.dumps(payload, ensure_ascii=False, sort_keys=True)
            documents.append(
                {
                    "category": category,
                    "id": doc_id,
                    "title": title,
                    "summary": KnowledgeBaseService._summarize(payload),
                    "tokens": set(KnowledgeBaseService._tokens(text)),
                }
            )

        for item in data.get("tweak_database") or []:
            add("tweak", item.get("tweak_id", ""), item.get("name", ""), item)
        for item in data.get("game_setting_database") or []:
            add("game", item.get("game", ""), item.get("game", ""), item)
        for item in data.get("nvidia_setting_database") or []:
            add("nvidia", item.get("setting", ""), item.get("setting", ""), item)
        for item in data.get("error_knowledge_base") or []:
            add("error", item.get("error", ""), item.get("error", ""), item)

        policy = data.get("safety_policy_database") or {}
        if policy:
            add("safety_policy", "safety_policy", "HyperBoostX Safety Policy", policy)

        return documents

    @staticmethod
    def _summarize(payload: Dict[str, Any]) -> str:
        for key in ("description", "explanation", "notes", "profile"):
            value = payload.get(key)
            if isinstance(value, str) and value.strip():
                return value.strip()
        return json.dumps(payload, ensure_ascii=False)[:240]

    @staticmethod
    def _tokens(text: str) -> List[str]:
        return [
            token
            for token in re.findall(r"[a-zA-Z0-9_]+", (text or "").lower())
            if len(token) >= 3
        ]

    @staticmethod
    def _normalize(value: str) -> str:
        return re.sub(r"[^a-z0-9]+", "", (value or "").lower())


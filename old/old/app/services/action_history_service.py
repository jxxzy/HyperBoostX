"""Action History integration for HyperBoostX v2.10 test and feature flows."""

from __future__ import annotations

import json
import re
import uuid
from datetime import datetime, timezone
from typing import Any, Dict, Iterable, List, Optional

from services.product_features import LocalJsonStore


SECRET_PATTERNS = (
    re.compile(r"https://(?:canary\.|ptb\.)?discord(?:app)?\.com/api/webhooks/[^\s\"']+", re.IGNORECASE),
    re.compile(r"(?i)(authorization|x-hyperboostx-session|x-hyperboostx-token|api[_-]?key|license[_-]?key|token)\s*[:=]\s*[^\s,;\"']+"),
    re.compile(r"(?i)(sk-[A-Za-z0-9_\-]{12,}|github_pat_[A-Za-z0-9_]+|ghp_[A-Za-z0-9_]+|xox[baprs]-[A-Za-z0-9\-]+)"),
    re.compile(r"(?i)([A-Z]:\\Users\\)[^\\/\s]+"),
)


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def redact_text(value: str) -> str:
    text = value or ""
    text = SECRET_PATTERNS[0].sub("https://discord.com/api/webhooks/[redacted]", text)
    text = SECRET_PATTERNS[1].sub(lambda match: f"{match.group(1)}=[redacted]", text)
    text = SECRET_PATTERNS[2].sub("[redacted-token]", text)
    text = SECRET_PATTERNS[3].sub(r"\1[redacted-user]", text)
    return text


def redact_value(value: Any) -> Any:
    if isinstance(value, str):
        return redact_text(value)
    if isinstance(value, list):
        return [redact_value(item) for item in value]
    if isinstance(value, tuple):
        return [redact_value(item) for item in value]
    if isinstance(value, dict):
        return {str(key): redact_value(item) for key, item in value.items()}
    return value


class ActionHistoryService:
    """Append-only, redacted local evidence for tests, feature actions, and webhooks."""

    STORE_KEY = "logs/action_history_v2_10.json"
    LIMIT = 1000

    @classmethod
    def append(
        cls,
        *,
        action: str,
        status: str,
        severity: str = "info",
        feature: str = "Test Center",
        menu: str = "Owner Lab",
        action_id: Optional[str] = None,
        endpoint: Optional[str] = None,
        test_run_id: Optional[str] = None,
        suite_id: Optional[str] = None,
        test_id: Optional[str] = None,
        started_at: Optional[str] = None,
        finished_at: Optional[str] = None,
        duration_ms: Optional[int] = None,
        error: Optional[str] = None,
        discord_status: Optional[str] = None,
        report_path: Optional[str] = None,
        restore_status: Optional[str] = None,
        safety_status: Optional[str] = None,
        details: Optional[Dict[str, Any]] = None,
    ) -> Dict[str, Any]:
        records = LocalJsonStore.load(cls.STORE_KEY, [], list)
        now = utc_now()
        entry = {
            "id": f"ah_{uuid.uuid4().hex[:12]}",
            "timestamp": now,
            "test_run_id": test_run_id or "",
            "suite_id": suite_id or "",
            "test_id": test_id or "",
            "feature": feature,
            "menu": menu,
            "action_id": action_id or "",
            "endpoint": endpoint or "",
            "action": action,
            "status": status,
            "severity": severity,
            "started_at": started_at or now,
            "finished_at": finished_at or now,
            "duration_ms": int(duration_ms or 0),
            "error": error or "",
            "discord_status": discord_status or "",
            "report_path": report_path or "",
            "restore_status": restore_status or "",
            "safety_status": safety_status or "",
            "details": details or {},
        }
        entry = redact_value(entry)
        records.append(entry)
        LocalJsonStore.save(cls.STORE_KEY, records[-cls.LIMIT :])
        return entry

    @classmethod
    def latest(cls, limit: int = 100, suite_id: Optional[str] = None, status: Optional[str] = None) -> Dict[str, Any]:
        records = LocalJsonStore.load(cls.STORE_KEY, [], list)
        if suite_id:
            records = [item for item in records if str(item.get("suite_id", "")).lower() == suite_id.lower()]
        if status:
            records = [item for item in records if str(item.get("status", "")).lower() == status.lower()]
        return {
            "ok": True,
            "count": len(records[-limit:]),
            "items": records[-limit:],
            "store": str(LocalJsonStore.path(cls.STORE_KEY)),
        }

    @classmethod
    def statuses(cls, records: Optional[Iterable[Dict[str, Any]]] = None) -> Dict[str, Any]:
        items: List[Dict[str, Any]] = list(records) if records is not None else LocalJsonStore.load(cls.STORE_KEY, [], list)
        failed = [item for item in items if str(item.get("status", "")).lower() in {"fail", "failed", "blocked", "error"}]
        return {
            "total": len(items),
            "failed": len(failed),
            "last_status": items[-1].get("status") if items else "none",
            "last_failed_suite": failed[-1].get("suite_id", "") if failed else "",
        }

    @classmethod
    def export_markdown(cls, limit: int = 100) -> str:
        records = cls.latest(limit=limit)["items"]
        lines = [
            "# Action History Test Integration v2.10",
            "",
            "| Time | Suite | Action | Status | Severity | Endpoint |",
            "|---|---|---|---|---|---|",
        ]
        for item in records:
            lines.append(
                f"| {item.get('timestamp','')} | {item.get('suite_id','')} | {item.get('action','')} | "
                f"{item.get('status','')} | {item.get('severity','')} | `{item.get('endpoint','')}` |"
            )
        return "\n".join(lines) + "\n"


def to_json_safe(data: Any) -> Any:
    return json.loads(json.dumps(redact_value(data), default=str))

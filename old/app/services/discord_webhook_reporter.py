"""Opt-in Discord error reporting for HyperBoostX v2.10."""

from __future__ import annotations

import hashlib
import json
import time
import urllib.error
import urllib.request
import uuid
from datetime import datetime, timezone
from typing import Any, Dict, List, Optional
from urllib.parse import urlparse

from services.action_history_service import ActionHistoryService, redact_text, redact_value
from services.product_features import LocalJsonStore


ALLOWED_HOSTS = {"discord.com", "discordapp.com", "canary.discord.com", "ptb.discord.com"}
MIN_RATE_LIMIT_SECONDS = 30
DEDUP_WINDOW_SECONDS = 30 * 60
QUEUE_RETENTION_SECONDS = 24 * 60 * 60
MAX_RETRIES = 3


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


class DiscordWebhookReporter:
    """Small safe reporter. It never sends unless the owner opted in."""

    SETTINGS_KEY = "config/discord_webhook_v2_10.json"
    HISTORY_KEY = "logs/discord_webhook_history_v2_10.json"
    QUEUE_KEY = "logs/discord_webhook_queue_v2_10.json"

    @classmethod
    def default_settings(cls) -> Dict[str, Any]:
        return {
            "enabled": False,
            "webhook_url": "",
            "webhook_url_configured": False,
            "mode": "manual_opt_in",
            "username": "HyperBoostX Monitor",
            "attachments_enabled": False,
            "allowed_mentions_parse": [],
            "rate_limit_seconds": MIN_RATE_LIMIT_SECONDS,
            "dedupe_window_seconds": DEDUP_WINDOW_SECONDS,
            "retry_max": MAX_RETRIES,
            "queue_retention_hours": 24,
            "last_send_epoch": 0,
        }

    @classmethod
    def settings(cls, include_secret: bool = False) -> Dict[str, Any]:
        data = LocalJsonStore.load(cls.SETTINGS_KEY, cls.default_settings(), dict)
        merged = cls.default_settings()
        merged.update(data)
        merged["rate_limit_seconds"] = max(int(merged.get("rate_limit_seconds") or MIN_RATE_LIMIT_SECONDS), MIN_RATE_LIMIT_SECONDS)
        merged["retry_max"] = min(max(int(merged.get("retry_max") or MAX_RETRIES), 0), MAX_RETRIES)
        merged["allowed_mentions_parse"] = []
        merged["attachments_enabled"] = False
        merged["webhook_url_configured"] = bool(str(merged.get("webhook_url") or "").strip())
        if not include_secret:
            merged.pop("webhook_url", None)
        return merged

    @classmethod
    def update_settings(cls, payload: Dict[str, Any]) -> Dict[str, Any]:
        current = cls.settings(include_secret=True)
        next_settings = cls.default_settings()
        next_settings.update(current)

        if "enabled" in payload:
            next_settings["enabled"] = bool(payload.get("enabled"))
        if "webhook_url" in payload:
            webhook_url = str(payload.get("webhook_url") or "").strip()
            if webhook_url and not cls.validate_webhook_url(webhook_url)["ok"]:
                raise ValueError("Webhook URL must be an official Discord HTTPS webhook URL.")
            next_settings["webhook_url"] = webhook_url
        if "username" in payload:
            next_settings["username"] = str(payload.get("username") or "HyperBoostX Monitor")[:80]

        next_settings["allowed_mentions_parse"] = []
        next_settings["attachments_enabled"] = False
        next_settings["rate_limit_seconds"] = MIN_RATE_LIMIT_SECONDS
        next_settings["dedupe_window_seconds"] = DEDUP_WINDOW_SECONDS
        next_settings["retry_max"] = MAX_RETRIES
        next_settings["queue_retention_hours"] = 24
        next_settings["webhook_url_configured"] = bool(next_settings.get("webhook_url"))
        LocalJsonStore.save(cls.SETTINGS_KEY, next_settings)
        return cls.settings(include_secret=False)

    @classmethod
    def clear_settings(cls) -> Dict[str, Any]:
        LocalJsonStore.save(cls.SETTINGS_KEY, cls.default_settings())
        return cls.settings(include_secret=False)

    @staticmethod
    def validate_webhook_url(webhook_url: str) -> Dict[str, Any]:
        try:
            parsed = urlparse(webhook_url)
        except Exception:
            return {"ok": False, "reason": "invalid_url"}
        if parsed.scheme.lower() != "https":
            return {"ok": False, "reason": "https_required"}
        if parsed.hostname not in ALLOWED_HOSTS:
            return {"ok": False, "reason": "discord_domain_required"}
        if not parsed.path.startswith("/api/webhooks/"):
            return {"ok": False, "reason": "discord_webhook_path_required"}
        return {"ok": True, "host": parsed.hostname}

    @classmethod
    def build_payload(cls, error: Dict[str, Any], settings: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        settings = settings or cls.settings(include_secret=True)
        title = "HyperBoostX v2.10 Feature Error"
        safe_error = redact_value(error)
        description = str(safe_error.get("message") or safe_error.get("error") or "Feature test failed safely.")
        if len(description) > 1800:
            description = description[:1797] + "..."

        fields = []
        for name, key in (
            ("Suite", "suite_id"),
            ("Feature", "feature"),
            ("Action", "action_id"),
            ("Endpoint", "endpoint"),
            ("Severity", "severity"),
            ("Report", "report_path"),
        ):
            value = str(safe_error.get(key) or "").strip()
            if value:
                fields.append({"name": name, "value": value[:900], "inline": False})

        return {
            "username": str(settings.get("username") or "HyperBoostX Monitor")[:80],
            "allowed_mentions": {"parse": []},
            "embeds": [
                {
                    "title": title,
                    "description": description,
                    "color": cls._severity_color(str(safe_error.get("severity") or "error")),
                    "timestamp": utc_now(),
                    "fields": fields[:12],
                }
            ],
        }

    @staticmethod
    def _severity_color(severity: str) -> int:
        return {
            "critical": 0xB91C1C,
            "error": 0xDC2626,
            "warning": 0xD97706,
            "info": 0x2563EB,
            "success": 0x16A34A,
        }.get(severity.lower(), 0xDC2626)

    @staticmethod
    def _fingerprint(payload: Dict[str, Any]) -> str:
        serial = json.dumps(redact_value(payload), sort_keys=True, default=str)
        return hashlib.sha256(serial.encode("utf-8")).hexdigest()[:24]

    @classmethod
    def _history(cls) -> List[Dict[str, Any]]:
        return LocalJsonStore.load(cls.HISTORY_KEY, [], list)

    @classmethod
    def _save_history(cls, history: List[Dict[str, Any]]) -> None:
        LocalJsonStore.save(cls.HISTORY_KEY, history[-500:])

    @classmethod
    def history(cls, limit: int = 100) -> Dict[str, Any]:
        items = cls._history()[-limit:]
        return {"ok": True, "count": len(items), "items": items, "store": str(LocalJsonStore.path(cls.HISTORY_KEY))}

    @classmethod
    def _queue(cls) -> List[Dict[str, Any]]:
        cutoff = time.time() - QUEUE_RETENTION_SECONDS
        items = [item for item in LocalJsonStore.load(cls.QUEUE_KEY, [], list) if float(item.get("created_epoch") or 0) >= cutoff]
        LocalJsonStore.save(cls.QUEUE_KEY, items[-500:])
        return items

    @classmethod
    def send_error(cls, error: Dict[str, Any], *, dry_run: bool = True) -> Dict[str, Any]:
        settings = cls.settings(include_secret=True)
        payload = cls.build_payload(error, settings)
        fingerprint = cls._fingerprint(payload)
        now_epoch = time.time()
        history = cls._history()

        base_record = {
            "id": f"wh_{uuid.uuid4().hex[:10]}",
            "created_at": utc_now(),
            "created_epoch": now_epoch,
            "fingerprint": fingerprint,
            "dry_run": bool(dry_run),
            "payload_preview": redact_value(payload),
            "status": "pending",
            "reason": "",
        }

        if not settings.get("enabled"):
            record = {**base_record, "status": "disabled", "reason": "Discord webhook reporting is opt-in and currently off."}
            history.append(record)
            cls._save_history(history)
            ActionHistoryService.append(action="Discord webhook report", status="blocked", severity="info", discord_status="disabled", details=record)
            return {"ok": False, "status": "disabled", "record": record}

        validation = cls.validate_webhook_url(str(settings.get("webhook_url") or ""))
        if not validation.get("ok"):
            record = {**base_record, "status": "invalid_settings", "reason": validation.get("reason", "invalid_settings")}
            history.append(record)
            cls._save_history(history)
            ActionHistoryService.append(action="Discord webhook report", status="blocked", severity="warning", discord_status="invalid_settings", details=record)
            return {"ok": False, "status": "invalid_settings", "record": record}

        recent_duplicate = next(
            (
                item
                for item in reversed(history)
                if item.get("fingerprint") == fingerprint and now_epoch - float(item.get("created_epoch") or 0) < DEDUP_WINDOW_SECONDS
            ),
            None,
        )
        if recent_duplicate:
            record = {**base_record, "status": "deduped", "reason": "Duplicate error suppressed for 30 minutes."}
            history.append(record)
            cls._save_history(history)
            return {"ok": True, "status": "deduped", "record": record}

        last_send = float(settings.get("last_send_epoch") or 0)
        if now_epoch - last_send < MIN_RATE_LIMIT_SECONDS:
            record = {**base_record, "status": "queued", "reason": "Rate limit active; queued for later retry."}
            queue = cls._queue()
            queue.append(record)
            LocalJsonStore.save(cls.QUEUE_KEY, queue[-500:])
            history.append(record)
            cls._save_history(history)
            return {"ok": True, "status": "queued", "record": record}

        if dry_run:
            record = {**base_record, "status": "dry_run", "reason": "Payload validated; no network send requested."}
            history.append(record)
            cls._save_history(history)
            return {"ok": True, "status": "dry_run", "record": record, "payload": payload}

        encoded = json.dumps(payload).encode("utf-8")
        request = urllib.request.Request(
            str(settings.get("webhook_url")),
            data=encoded,
            headers={"Content-Type": "application/json", "User-Agent": "HyperBoostX-v2.10"},
            method="POST",
        )
        last_error = ""
        for attempt in range(1, MAX_RETRIES + 1):
            try:
                with urllib.request.urlopen(request, timeout=8) as response:
                    status_code = int(response.getcode())
                if 200 <= status_code < 300:
                    settings["last_send_epoch"] = now_epoch
                    LocalJsonStore.save(cls.SETTINGS_KEY, settings)
                    record = {**base_record, "status": "sent", "status_code": status_code, "attempts": attempt}
                    history.append(record)
                    cls._save_history(history)
                    ActionHistoryService.append(action="Discord webhook report", status="success", severity="info", discord_status="sent", details=record)
                    return {"ok": True, "status": "sent", "record": record}
                last_error = f"HTTP {status_code}"
            except urllib.error.HTTPError as exc:
                last_error = f"HTTP {exc.code}"
            except Exception as exc:
                last_error = redact_text(str(exc))

        record = {**base_record, "status": "queued", "reason": last_error, "attempts": MAX_RETRIES}
        queue = cls._queue()
        queue.append(record)
        LocalJsonStore.save(cls.QUEUE_KEY, queue[-500:])
        history.append(record)
        cls._save_history(history)
        ActionHistoryService.append(action="Discord webhook report", status="error", severity="warning", discord_status="queued", error=last_error, details=record)
        return {"ok": False, "status": "queued", "record": record}

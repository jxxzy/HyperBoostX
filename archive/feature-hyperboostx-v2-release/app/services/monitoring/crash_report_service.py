"""Local crash report export with privacy redaction for HyperBoostX."""

from __future__ import annotations

import json
import os
import platform
import re
import uuid
from datetime import datetime, timezone
from typing import Any, Dict, Optional

import psutil

from core.constants import APP_NAME, APP_VERSION
from services.monitoring.gpu_detection_service import GpuDetectionService


class CrashReportService:
    """Build local-only crash reports without leaking secrets."""

    SECRET_PATTERNS = [
        re.compile(r"(?i)(api[_-]?key|ai[_-]?key|token|github[_-]?token|secret|webhook|license[_-]?key)(\s*[:=]\s*)[^\s,;]+"),
        re.compile(r"(?i)bearer\s+[A-Za-z0-9._\-]+"),
        re.compile(r"(?i)(ghp_|github_pat_|sk-|nvapi-)[A-Za-z0-9_\-]+"),
    ]

    @classmethod
    def redact(cls, value: Any) -> str:
        text = "" if value is None else str(value)
        if not text:
            return ""

        username = os.environ.get("USERNAME") or os.environ.get("USER") or ""
        if username:
            text = re.sub(
                rf"(?i)C:\\Users\\{re.escape(username)}\\[^\s\r\n]*",
                r"C:\\Users\\<user>\\[REDACTED_PATH]",
                text,
            )
            text = text.replace(username, "<user>")

        text = re.sub(
            r"(?i)C:\\Users\\[^\\\s\r\n]+\\(?:\.codex|AppData|Documents|Desktop|Downloads)\\[^\s\r\n]*",
            r"C:\\Users\\<user>\\[REDACTED_PATH]",
            text,
        )

        for pattern in cls.SECRET_PATTERNS:
            text = pattern.sub(lambda match: cls._redact_match(match), text)

        return text

    @staticmethod
    def _redact_match(match: re.Match[str]) -> str:
        if len(match.groups()) >= 2:
            return f"{match.group(1)}{match.group(2)}[REDACTED]"
        return "[REDACTED]"

    @staticmethod
    def _ram_gb() -> float:
        try:
            return round(psutil.virtual_memory().total / (1024 ** 3), 1)
        except Exception:
            return 0.0

    @classmethod
    def build_report(cls, payload: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        payload = payload or {}
        gpu = GpuDetectionService.get_gpu_summary()
        report_id = f"crash_{uuid.uuid4().hex[:10]}"

        return {
            "report_id": report_id,
            "title": f"{APP_NAME} Local Crash Report",
            "app_version": APP_VERSION,
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "privacy": "local_only_manual_export",
            "redaction_applied": True,
            "system": {
                "windows_version": platform.platform(),
                "cpu": cls.redact(platform.processor() or platform.machine() or "Unknown CPU"),
                "cpu_threads": psutil.cpu_count(logical=True) or 0,
                "ram_gb": cls._ram_gb(),
                "gpu_vendor": gpu.get("vendor", "Unknown"),
                "gpu_model": cls.redact(gpu.get("model", "Unknown GPU")),
            },
            "error": {
                "message": cls.redact(payload.get("error_message") or payload.get("message") or ""),
                "stack_trace": cls.redact(payload.get("stack_trace") or payload.get("traceback") or ""),
                "last_action": cls.redact(payload.get("last_action") or "Unknown"),
                "backend_status": cls.redact(payload.get("backend_status") or "Unknown"),
            },
            "notes": [
                "Crash reports stay local unless the user manually shares them.",
                "Secrets, tokens, API keys, usernames, and sensitive local paths are redacted.",
            ],
        }

    @classmethod
    def export_report(cls, payload: Optional[Dict[str, Any]] = None, fmt: str = "json") -> Dict[str, Any]:
        report = cls.build_report(payload)
        normalized = (fmt or "json").strip().lower()
        if normalized in {"md", "markdown"}:
            extension = "md"
            content_type = "text/markdown"
            content = cls._to_markdown(report)
        elif normalized == "txt":
            extension = "txt"
            content_type = "text/plain"
            content = cls._to_text(report)
        else:
            extension = "json"
            content_type = "application/json"
            content = json.dumps(report, indent=2)

        return {
            "file_name": f"HyperBoostX-Crash-Report-{report['report_id']}.{extension}",
            "format": extension,
            "content_type": content_type,
            "content": content,
            "report": report,
        }

    @staticmethod
    def _to_text(report: Dict[str, Any]) -> str:
        system = report.get("system", {})
        error = report.get("error", {})
        return "\n".join([
            report.get("title", "HyperBoostX Local Crash Report"),
            f"Version: {report.get('app_version', 'Unknown')}",
            f"Timestamp: {report.get('timestamp', '')}",
            f"Windows: {system.get('windows_version', 'Unknown')}",
            f"CPU: {system.get('cpu', 'Unknown')}",
            f"RAM: {system.get('ram_gb', 0)} GB",
            f"GPU: {system.get('gpu_vendor', 'Unknown')} {system.get('gpu_model', 'Unknown GPU')}",
            f"Backend: {error.get('backend_status', 'Unknown')}",
            f"Last action: {error.get('last_action', 'Unknown')}",
            f"Error: {error.get('message', '')}",
            "Stack trace:",
            error.get("stack_trace", ""),
            "Privacy: local-only manual export; redaction applied.",
        ])

    @classmethod
    def _to_markdown(cls, report: Dict[str, Any]) -> str:
        return "# " + cls._to_text(report).replace("\n", "\n\n")

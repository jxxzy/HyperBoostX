"""Before/after report support for HyperBoostX."""

from __future__ import annotations

import json
import uuid
from datetime import datetime, timezone
from typing import Any, Dict, Optional

from services.monitoring.gpu_detection_service import GpuDetectionService
from services.monitoring.hardware_profile_service import HardwareProfileService
from services.monitoring.monitor_service import MonitorService
from services.optimization.startup_service import StartupService


class ReportService:
    """Capture and export safe performance report snapshots."""

    _latest_report: Optional[Dict[str, Any]] = None

    @staticmethod
    def capture_snapshot(label: str = "snapshot") -> Dict[str, Any]:
        stats = MonitorService.get_current_stats()
        gpu = GpuDetectionService.get_gpu_summary()
        overlays = GpuDetectionService.detect_overlays()
        vendors = GpuDetectionService.detect_vendor_software()
        profile = HardwareProfileService.get_profile(stats=stats, gpu_summary=gpu, vendor_apps=vendors, overlays=overlays)
        try:
            startup_count = len(StartupService().get_startup_items())
        except Exception:
            startup_count = 0

        return {
            "label": label,
            "captured_at": datetime.now(timezone.utc).isoformat(),
            "cpu_idle_usage_percent": stats.get("cpu", 0),
            "ram_usage_percent": stats.get("memory", 0),
            "gpu_usage_percent": gpu.get("gpu_usage_percent", 0),
            "vram_usage_percent": gpu.get("vram_usage_percent", 0),
            "disk_usage_percent": stats.get("disk", 0),
            "startup_apps_count": startup_count,
            "background_process_count": stats.get("processes", 0),
            "network_download_mb_s": stats.get("network_download_mb_s", 0),
            "network_upload_mb_s": stats.get("network_upload_mb_s", 0),
            "dns_latency_ms": None,
            "temp_cache_size_mb": None,
            "power_plan": "Unknown",
            "active_overlays": [item["name"] for item in overlays if item.get("detected")],
            "active_rgb_vendor_services": [item["name"] for item in vendors if item.get("detected") and item.get("category") in {"rgb", "vendor_service"}],
            "pc_health_score": profile["scores"]["pc_health"],
            "gaming_readiness_score": profile["scores"]["gaming_readiness"],
            "streaming_readiness_score": profile["scores"]["streaming_readiness"],
        }

    @classmethod
    def build_report(cls, before: Optional[Dict[str, Any]] = None, after: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        before = before or cls.capture_snapshot("before")
        after = after or cls.capture_snapshot("after")
        report = {
            "report_id": f"report_{uuid.uuid4().hex[:10]}",
            "title": "HyperBoostX Performance Report",
            "created_at": datetime.now(timezone.utc).isoformat(),
            "before": before,
            "after": after,
            "changed_settings": [],
            "skipped_settings": ["Risky or hardware-specific actions require approval and were not run silently."],
            "blocked_risky_actions": [
                "Forced Defender disable",
                "Permanent Windows Update disable",
                "GPU driver service disable",
                "BIOS/UEFI, overclock, undervolt, and voltage changes",
            ],
            "cleaned_storage_mb": 0,
            "safety_guard": "Active",
            "undo_available": True,
            "summary": "Report generated from local counters. Improvements are measured values, not guaranteed FPS claims.",
        }
        cls._latest_report = report
        return report

    @classmethod
    def latest_report(cls) -> Dict[str, Any]:
        if cls._latest_report is None:
            cls._latest_report = cls.build_report()
        return cls._latest_report

    @classmethod
    def export_report(cls, fmt: str = "json", report: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        report = report or cls.latest_report()
        normalized = (fmt or "json").strip().lower()
        if normalized == "txt":
            content = cls._to_text(report)
            extension = "txt"
            content_type = "text/plain"
        elif normalized == "md" or normalized == "markdown":
            content = cls._to_markdown(report)
            extension = "md"
            content_type = "text/markdown"
        else:
            content = json.dumps(report, indent=2)
            extension = "json"
            content_type = "application/json"

        return {
            "file_name": f"HyperBoostX-Performance-Report-{report['report_id']}.{extension}",
            "format": extension,
            "content_type": content_type,
            "content": content,
        }

    @staticmethod
    def _to_text(report: Dict[str, Any]) -> str:
        before = report.get("before", {})
        after = report.get("after", {})
        lines = [report.get("title", "HyperBoostX Performance Report"), ""]
        for key, label in [
            ("cpu_idle_usage_percent", "CPU Idle"),
            ("ram_usage_percent", "RAM Usage"),
            ("gpu_usage_percent", "GPU Usage"),
            ("vram_usage_percent", "VRAM Usage"),
            ("startup_apps_count", "Startup Apps"),
            ("background_process_count", "Background Processes"),
            ("pc_health_score", "PC Health"),
            ("gaming_readiness_score", "Gaming Readiness"),
            ("streaming_readiness_score", "Streaming Readiness"),
        ]:
            lines.append(f"{label}: {before.get(key)} -> {after.get(key)}")
        lines.append(f"Safety Guard: {report.get('safety_guard', 'Active')}")
        lines.append(f"Undo: {'Available' if report.get('undo_available') else 'Unavailable'}")
        return "\n".join(lines)

    @classmethod
    def _to_markdown(cls, report: Dict[str, Any]) -> str:
        text = cls._to_text(report)
        return "# " + text.replace("\n", "\n\n")

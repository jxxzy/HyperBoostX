"""Hardware profile and readiness scoring for HyperBoostX v2.0.0."""

from __future__ import annotations

from typing import Any, Dict, Iterable, List, Optional

import psutil

from services.monitoring.gpu_detection_service import GpuDetectionService, GpuVendor
from services.monitoring.monitor_service import MonitorService
from services.monitoring.system_info_service import SystemInfoService
from services.optimization.startup_service import StartupService


class HardwareProfileService:
    """Build a safe, explainable hardware-aware profile recommendation."""

    @staticmethod
    def _clamp_score(value: float) -> int:
        return int(max(0, min(100, round(value))))

    @staticmethod
    def _safe_count(items: Optional[Iterable[Any]]) -> int:
        try:
            return len(list(items or []))
        except Exception:
            return 0

    @staticmethod
    def _profile_name(stats: Dict[str, Any], gpu: Dict[str, Any], overlay_count: int, streaming_count: int) -> str:
        ram_gb = float(stats.get("memory_total_gb") or 0)
        cpu_threads = int(stats.get("cpu_threads") or 0)
        vendor = gpu.get("vendor")
        family = gpu.get("family", "")
        vram_mb = int(gpu.get("vram_total_mb") or 0)
        has_battery = psutil.sensors_battery() is not None

        if has_battery:
            return "Laptop Battery Safe Mode"
        if streaming_count > 0:
            return "Streaming + Discord PC"
        if overlay_count >= 3:
            return "Esports Low Latency Mode"
        if vendor == GpuVendor.NVIDIA and ram_gb >= 16 and vram_mb >= 8192:
            return "High-End NVIDIA PC"
        if vendor == GpuVendor.AMD and ram_gb >= 16 and vram_mb >= 8192:
            return "High-End AMD Radeon PC"
        if vendor == GpuVendor.INTEL and "Arc" in family:
            return "Intel Arc Gaming PC"
        if ram_gb >= 32 and cpu_threads >= 16:
            return "Creator Workstation"
        if ram_gb >= 16 and vram_mb >= 6144:
            return "High-End Gaming PC"
        if ram_gb >= 8:
            return "Mid-Range Gaming PC"
        if ram_gb > 0:
            return "Low-End PC"
        return "Unknown Safe Mode"

    @classmethod
    def get_profile(
        cls,
        stats: Optional[Dict[str, Any]] = None,
        gpu_summary: Optional[Dict[str, Any]] = None,
        vendor_apps: Optional[List[Dict[str, Any]]] = None,
        overlays: Optional[List[Dict[str, Any]]] = None,
    ) -> Dict[str, Any]:
        stats = stats or MonitorService.get_current_stats()
        gpu_summary = gpu_summary or GpuDetectionService.get_gpu_summary()
        vendor_apps = vendor_apps if vendor_apps is not None else GpuDetectionService.detect_vendor_software()
        overlays = overlays if overlays is not None else GpuDetectionService.detect_overlays()
        system_drive = SystemInfoService.get_system_drive_info()

        startup_count = 0
        try:
            startup_count = len(StartupService().get_startup_items())
        except Exception:
            startup_count = 0

        process_count = int(stats.get("processes") or 0)
        overlay_detected = [item for item in overlays if item.get("detected")]
        vendor_detected = [item for item in vendor_apps if item.get("detected")]
        streaming_detected = [item for item in vendor_detected if item.get("category") == "streaming"]
        rgb_detected = [item for item in vendor_detected if item.get("category") == "rgb"]

        pc_health = 100
        pc_health -= min(30, max(0, float(stats.get("cpu") or 0) - 20) * 0.35)
        pc_health -= min(25, max(0, float(stats.get("memory") or 0) - 45) * 0.45)
        pc_health -= min(12, max(0, startup_count - 8) * 1.2)
        pc_health -= min(10, max(0, process_count - 180) * 0.04)

        gaming_readiness = pc_health
        gaming_readiness -= min(12, len(overlay_detected) * 3)
        gaming_readiness += 4 if gpu_summary.get("dedicated_gpu") else 0

        streaming_readiness = pc_health
        streaming_readiness += 5 if streaming_detected else 0
        streaming_readiness -= min(8, len(overlay_detected) * 1.5)

        startup_cleanliness = 100 - min(70, startup_count * 3)

        profile_name = cls._profile_name(stats, gpu_summary, len(overlay_detected), len(streaming_detected))
        reason: List[str] = [
            f"{gpu_summary.get('family', 'Unknown GPU')} detected",
            f"{float(stats.get('memory_total_gb') or 0):.0f}GB RAM detected" if stats.get("memory_total_gb") else "RAM capacity unavailable",
            f"System drive class: {system_drive.get('storage_class', 'Unknown')}",
        ]
        if overlay_detected:
            reason.append(f"{len(overlay_detected)} overlay app(s) detected")
        if rgb_detected:
            reason.append(f"{len(rgb_detected)} RGB/vendor app(s) detected")
        if gpu_summary.get("hybrid_gpu_system"):
            reason.append("Hybrid GPU system detected")

        safe_actions = [
            "Create restore metadata before applying supported tweaks.",
            "Use GPU vendor-aware recommendations without driver hacks.",
            "Export a before/after report after boost verification.",
        ]
        requires_approval = []
        if overlay_detected:
            requires_approval.append("Pause optional overlays while gaming if recording/streaming is not needed.")
        if rgb_detected:
            requires_approval.append("Pause RGB/vendor companion apps only after user approval.")

        return {
            "recommended_profile": profile_name,
            "confidence": 0.91 if gpu_summary.get("vendor") != GpuVendor.UNKNOWN else 0.62,
            "reason": reason,
            "inputs": {
                "windows_version": SystemInfoService.get_os_info(),
                "cpu_threads": stats.get("cpu_threads", 0),
                "cpu_usage": stats.get("cpu", 0),
                "ram_total_gb": stats.get("memory_total_gb", 0),
                "ram_usage_percent": stats.get("memory", 0),
                "gpu_vendor": gpu_summary.get("vendor", GpuVendor.UNKNOWN),
                "gpu_model": gpu_summary.get("model", "Unknown GPU"),
                "vram_total_mb": gpu_summary.get("vram_total_mb", 0),
                "disk_type": system_drive.get("storage_class", "Unknown"),
                "disk_usage_percent": stats.get("disk", 0),
                "startup_apps_count": startup_count,
                "background_process_count": process_count,
                "overlay_apps_count": len(overlay_detected),
                "rgb_vendor_apps_count": len(rgb_detected),
            },
            "scores": {
                "pc_health": cls._clamp_score(pc_health),
                "gaming_readiness": cls._clamp_score(gaming_readiness),
                "streaming_readiness": cls._clamp_score(streaming_readiness),
                "startup_cleanliness": cls._clamp_score(startup_cleanliness),
            },
            "safe_actions": safe_actions,
            "requires_approval": requires_approval,
            "risky_actions_blocked": [
                "Forced Defender disable",
                "Permanent Windows Update disable",
                "GPU driver service disable",
                "BIOS/UEFI, overclock, undervolt, and voltage changes",
            ],
            "undo_available": True,
        }

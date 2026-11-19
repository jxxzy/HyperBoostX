"""Safe PC scanner used by the HyperBoostX Triple AI Engine."""

import json
import os
import platform
import re
import subprocess
import uuid
import winreg
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict, List

from core.config import Config
from core.logger import Logger
from services.monitoring.monitor_service import MonitorService
from services.monitoring.system_info_service import SystemInfoService
from services.optimization.startup_service import StartupService


logger = Logger.get_logger(__name__)


class PcScannerService:
    """Collects the MVP scan payload without changing system state."""

    def __init__(self):
        self.scan_history_dir = Config.DATA_DIR / "scan-history"
        self.scan_history_dir.mkdir(parents=True, exist_ok=True)

    def scan_pc(self) -> Dict[str, Any]:
        logger.info("scan started")
        scan_id = self._new_scan_id()
        timestamp = datetime.now(timezone.utc).isoformat()

        stats = self._safe_call(MonitorService.get_current_stats, {})
        cpu = self._safe_call(SystemInfoService.get_cpu_info, {})
        memory = self._safe_call(SystemInfoService.get_memory_info, {})
        disk = self._safe_call(SystemInfoService.get_disk_info, {})
        system_drive = self._safe_call(SystemInfoService.get_system_drive_info, {})
        windows_details = self._safe_call(SystemInfoService.get_windows_system_details, {})
        os_info = self._safe_call(SystemInfoService.get_os_info, {})
        gpu = self._safe_call(SystemInfoService.get_gpu_info, {})
        startup_items = self._safe_call(StartupService.get_startup_items, [])
        processes = self._safe_call(lambda: MonitorService.get_process_list(limit=15), [])

        primary_gpu = self._primary_gpu(gpu, stats)
        scan_result = {
            "scan_id": scan_id,
            "timestamp": timestamp,
            "hardware": {
                "cpu_name": cpu.get("processor") or "Unknown",
                "gpu_name": primary_gpu.get("name") or "Unknown",
                "ram_total_gb": self._bytes_to_gb(memory.get("total", 0)),
                "storage_type": system_drive.get("storage_class", "Unknown"),
                "cpu": {
                    "name": cpu.get("processor") or "Unknown",
                    "cores": cpu.get("cores", 0),
                    "threads": cpu.get("threads", 0),
                    "usage_percent": stats.get("cpu", cpu.get("usage", 0)),
                    "frequency_current_mhz": cpu.get("frequency_current", 0),
                    "frequency_max_mhz": cpu.get("frequency_max", 0),
                },
                "gpu": primary_gpu,
                "ram": {
                    "total_gb": self._bytes_to_gb(memory.get("total", 0)),
                    "available_gb": self._bytes_to_gb(memory.get("available", 0)),
                    "usage_percent": memory.get("percent", stats.get("memory", 0)),
                    "speed_mhz": memory.get("speed_mhz", 0),
                    "slots_used": memory.get("slots_used", 0),
                },
                "storage": {
                    "system_drive": system_drive.get("drive_letter", "C"),
                    "type": system_drive.get("storage_class", "Unknown"),
                    "model": system_drive.get("model", "Unknown"),
                    "free_gb": self._system_drive_free_gb(disk, system_drive),
                    "usage_percent": stats.get("disk", 0),
                },
            },
            "windows": {
                "version": windows_details.get("edition") or os_info.get("system") or platform.system(),
                "build_number": windows_details.get("build") or os_info.get("version") or platform.version(),
                "architecture": windows_details.get("architecture") or os_info.get("architecture", "Unknown"),
                "power_plan": self._get_active_power_plan(),
                "game_mode": self._read_game_mode_status(),
                "hags": self._read_hags_status(),
                "startup_apps": self._summarize_startup(startup_items),
                "background_apps_heavy": self._summarize_processes(processes),
                "temporary_files_size_mb": self._estimate_temp_files_mb(),
            },
            "nvidia": self._build_nvidia_payload(primary_gpu),
            "apps": {
                "startup_count": len(startup_items),
                "startup_high_impact": sum(1 for item in startup_items if item.get("impact") == "High"),
                "background_process_count": stats.get("processes", 0),
                "top_background_apps": self._summarize_processes(processes[:8]),
            },
            "performance": {
                "cpu_usage_percent": stats.get("cpu", 0),
                "ram_usage_percent": stats.get("memory", 0),
                "disk_usage_percent": stats.get("disk", 0),
                "gpu_usage_percent": (stats.get("gpu") or {}).get("load", 0),
                "gpu_temperature_c": (stats.get("gpu") or {}).get("temperature", 0),
                "disk_read_mb_s": stats.get("disk_read_mb_s", 0),
                "disk_write_mb_s": stats.get("disk_write_mb_s", 0),
                "processes": stats.get("processes", 0),
                "temperatures": stats.get("temperatures", {}),
            },
            "scores": {},
            "privacy": {
                "cloud_payload_note": "HyperBoostX only sends this sanitized scan payload when AI Cloud Analysis is enabled.",
                "personal_paths_included": False,
                "api_key_logged": False,
            },
        }
        scan_result["scores"] = self.calculate_scores(scan_result)
        self._save_scan(scan_result)
        logger.info("scan completed: %s", scan_id)
        return scan_result

    @staticmethod
    def calculate_scores(scan_result: Dict[str, Any]) -> Dict[str, int]:
        hardware = scan_result.get("hardware") or {}
        windows = scan_result.get("windows") or {}
        performance = scan_result.get("performance") or {}
        apps = scan_result.get("apps") or {}
        nvidia = scan_result.get("nvidia") or {}

        ram_usage = float(performance.get("ram_usage_percent") or 0)
        disk_usage = float(performance.get("disk_usage_percent") or 0)
        cpu_usage = float(performance.get("cpu_usage_percent") or 0)
        background_count = int(apps.get("background_process_count") or 0)
        startup_high = int(apps.get("startup_high_impact") or 0)

        health = 100
        health -= 18 if ram_usage >= 85 else 10 if ram_usage >= 75 else 0
        health -= 16 if disk_usage >= 90 else 8 if disk_usage >= 80 else 0
        health -= 12 if cpu_usage >= 85 else 5 if cpu_usage >= 70 else 0
        health -= min(startup_high * 4, 16)
        health -= 8 if background_count >= 220 else 4 if background_count >= 160 else 0
        if "high performance" not in str(windows.get("power_plan", "")).lower() and "ultimate" not in str(windows.get("power_plan", "")).lower():
            health -= 5
        if str(windows.get("game_mode", "")).lower() in {"off", "disabled"}:
            health -= 5

        ram_total = float((hardware.get("ram") or {}).get("total_gb") or 0)
        vram_gb = float((hardware.get("gpu") or {}).get("vram_gb") or 0)
        readiness = 100
        readiness -= 20 if ram_total and ram_total < 8 else 10 if ram_total and ram_total < 16 else 0
        readiness -= 12 if vram_gb and vram_gb < 4 else 5 if vram_gb and vram_gb < 6 else 0
        readiness -= 10 if not nvidia.get("is_nvidia") else 0
        readiness -= 8 if not nvidia.get("driver_version") or nvidia.get("driver_version") == "Unknown" else 0
        readiness -= 6 if str(windows.get("game_mode", "")).lower() in {"off", "disabled"} else 0
        readiness -= 6 if ram_usage >= 80 else 0

        return {
            "pc_health_score": max(0, min(100, int(round(health)))),
            "gaming_readiness_score": max(0, min(100, int(round(readiness)))),
        }

    def load_scan(self, scan_id: str) -> Dict[str, Any]:
        safe_scan_id = re.sub(r"[^a-zA-Z0-9_-]", "", scan_id or "")
        if not safe_scan_id:
            return {}
        path = self.scan_history_dir / f"{safe_scan_id}.json"
        if not path.exists():
            return {}
        try:
            return json.loads(path.read_text(encoding="utf-8"))
        except Exception as exc:
            logger.error("Failed to load scan %s: %s", scan_id, exc)
            return {}

    def _save_scan(self, scan_result: Dict[str, Any]) -> None:
        try:
            scan_id = scan_result["scan_id"]
            path = self.scan_history_dir / f"{scan_id}.json"
            path.write_text(json.dumps(scan_result, indent=2, ensure_ascii=False), encoding="utf-8")
        except Exception as exc:
            logger.error("Failed to save scan result: %s", exc)

    @staticmethod
    def _new_scan_id() -> str:
        stamp = datetime.now().strftime("%Y%m%d%H%M%S")
        return f"scan-{stamp}-{uuid.uuid4().hex[:8]}"

    @staticmethod
    def _safe_call(func, default):
        try:
            return func()
        except Exception as exc:
            logger.warning("Scanner probe failed: %s", exc)
            return default

    @staticmethod
    def _bytes_to_gb(value: Any) -> float:
        try:
            return round(float(value or 0) / (1024**3), 2)
        except Exception:
            return 0.0

    @staticmethod
    def _system_drive_free_gb(disk: Dict[str, Any], system_drive: Dict[str, Any]) -> float:
        drive = f"{system_drive.get('drive_letter', 'C')}:".upper()
        for device, item in (disk or {}).items():
            if str(device).upper().startswith(drive):
                return PcScannerService._bytes_to_gb(item.get("free", 0))
        return 0.0

    @staticmethod
    def _primary_gpu(gpu_info: Dict[str, Any], stats: Dict[str, Any]) -> Dict[str, Any]:
        gpus = gpu_info.get("gpus") or []
        first = gpus[0] if gpus else {}
        live_gpu = stats.get("gpu") or {}
        adapter_ram = first.get("vram") or 0
        live_vram_mb = live_gpu.get("memory_total_mb") or 0
        vram_gb = round((live_vram_mb / 1024) if live_vram_mb else (float(adapter_ram or 0) / (1024**3)), 2)
        name = first.get("name") or live_gpu.get("name") or "Unknown"
        return {
            "name": name,
            "driver_version": first.get("driver_version") or "Unknown",
            "driver_date": first.get("driver_date") or "Unknown",
            "vram_gb": vram_gb,
            "usage_percent": live_gpu.get("load", 0),
            "temperature_c": live_gpu.get("temperature", 0),
            "video_processor": first.get("video_processor", "Unknown"),
        }

    @staticmethod
    def _build_nvidia_payload(primary_gpu: Dict[str, Any]) -> Dict[str, Any]:
        gpu_name = primary_gpu.get("name") or ""
        is_nvidia = any(token in gpu_name.lower() for token in ("nvidia", "geforce", "rtx", "gtx"))
        is_rtx = "rtx" in gpu_name.lower()
        return {
            "is_nvidia": is_nvidia,
            "is_rtx": is_rtx,
            "gpu_name": gpu_name or "Unknown",
            "driver_version": primary_gpu.get("driver_version", "Unknown"),
            "driver_date": primary_gpu.get("driver_date", "Unknown"),
            "vram_gb": primary_gpu.get("vram_gb", 0),
            "control_panel_status": "Unknown",
            "feature_support": {
                "dlss_possible": is_rtx,
                "reflex_possible": is_nvidia,
                "frame_generation_possible": is_rtx,
            },
            "support_note": "Full NVIDIA recommendations available." if is_nvidia else "Limited support: NVIDIA GPU was not detected.",
        }

    @staticmethod
    def _summarize_startup(startup_items: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
        summary = []
        for item in startup_items[:20]:
            summary.append(
                {
                    "name": item.get("name", "Unknown"),
                    "enabled": item.get("enabled", False),
                    "impact": item.get("impact", "Unknown"),
                    "impact_score": item.get("impact_score", 0),
                    "recommended_action": item.get("recommended_action", ""),
                    "source": item.get("source", "Unknown"),
                    "type": item.get("type", "App"),
                }
            )
        return summary

    @staticmethod
    def _summarize_processes(processes: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
        return [
            {
                "name": item.get("name", "Unknown"),
                "cpu_percent": round(float(item.get("cpu") or 0), 2),
                "memory_percent": round(float(item.get("memory") or 0), 2),
                "threads": item.get("threads", 0),
                "disk_io_mb": round(float(item.get("disk_io_mb") or 0), 2),
            }
            for item in (processes or [])[:12]
        ]

    @staticmethod
    def _get_active_power_plan() -> str:
        if platform.system() != "Windows":
            return "Unavailable"
        try:
            output = subprocess.check_output(
                ["powercfg", "/getactivescheme"],
                text=True,
                stderr=subprocess.DEVNULL,
                timeout=3,
            ).strip()
            return output or "Unknown"
        except Exception:
            return "Unknown"

    @staticmethod
    def _read_game_mode_status() -> str:
        try:
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER, r"Software\Microsoft\GameBar") as key:
                value, _ = winreg.QueryValueEx(key, "AutoGameModeEnabled")
                return "On" if int(value) == 1 else "Off"
        except Exception:
            return "Unknown"

    @staticmethod
    def _read_hags_status() -> str:
        try:
            with winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, r"SYSTEM\CurrentControlSet\Control\GraphicsDrivers") as key:
                value, _ = winreg.QueryValueEx(key, "HwSchMode")
                return "On" if int(value) == 2 else "Off" if int(value) == 1 else "Default"
        except Exception:
            return "Unknown"

    @staticmethod
    def _estimate_temp_files_mb(max_entries: int = 5000) -> float:
        roots = {
            os.environ.get("TEMP", ""),
            os.environ.get("TMP", ""),
            str(Path(os.environ.get("SystemRoot", r"C:\Windows")) / "Temp"),
        }
        total = 0
        visited = 0
        for root in roots:
            if not root:
                continue
            path = Path(root)
            if not path.exists():
                continue
            try:
                for item in path.rglob("*"):
                    if visited >= max_entries:
                        break
                    visited += 1
                    try:
                        if item.is_file():
                            total += item.stat().st_size
                    except OSError:
                        continue
            except OSError:
                continue
        return round(total / (1024 * 1024), 1)

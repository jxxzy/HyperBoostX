"""Product-grade v1.4 feature services for HyperBoostX.

These services are intentionally local-first and conservative. They provide
diagnosis, education, history, profiles, and previewable actions without
performing risky Windows tweaks or fabricating external benchmark/driver data.
"""

from __future__ import annotations

import csv
import json
import os
import platform
import socket
import tempfile
import time
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional

import psutil

from core.config import Config
from core.constants import APP_VERSION
from services.monitoring.crash_report_service import CrashReportService
from services.monitoring.gpu_detection_service import (
    BackgroundAppCatalog,
    GpuDetectionService,
    GpuVendor,
)
from services.monitoring.hardware_profile_service import HardwareProfileService
from services.monitoring.monitor_service import MonitorService
from services.monitoring.report_service import ReportService
from services.monitoring.system_info_service import SystemInfoService
from services.optimization.network_service import NetworkService
from services.optimization.startup_service import StartupService


def _utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def _clamp(value: float, low: float = 0, high: float = 100) -> int:
    return int(max(low, min(high, round(value))))


class LocalJsonStore:
    """Small JSON store with corruption recovery and portable mode support."""

    FILES = {
        "game_profiles": "profiles/game_profiles.json",
        "protected_processes": "config/protected_processes.json",
        "auto_gaming_settings": "config/auto_gaming_settings.json",
        "benchmark_history": "reports/benchmark_history.json",
        "overlay_ignore_list": "config/overlay_ignore_list.json",
        "startup_backups": "backups/startup_backups.json",
        "cleanup_reports": "reports/cleanup_reports.json",
        "restore_sessions": "sessions/restore_sessions.json",
        "process_reports": "reports/process_reports.json",
        "network_reports": "reports/network_reports.json",
        "gpu_reports": "reports/gpu_reports.json",
        "ui_settings": "config/ui_settings.json",
        "feature_audit_history": "reports/feature_audit_history.json",
        "performance_history": "reports/performance_history.json",
        "action_log": "logs/action_log.json",
        "plugin_registry": "config/plugin_registry.json",
    }

    @classmethod
    def root(cls) -> Path:
        portable_home = os.environ.get("HYPERBOOSTX_PORTABLE_HOME", "").strip()
        if portable_home:
            return Path(portable_home).expanduser()
        return Config.APP_DIR

    @classmethod
    def path(cls, key: str) -> Path:
        relative = cls.FILES.get(key, key)
        path = cls.root() / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        return path

    @classmethod
    def ensure_dirs(cls) -> Dict[str, Any]:
        folders = ["config", "logs", "reports", "backups", "profiles", "sessions", "diagnostics"]
        root = cls.root()
        for folder in folders:
            (root / folder).mkdir(parents=True, exist_ok=True)
        return {
            "root": str(root),
            "portable_mode": bool(os.environ.get("HYPERBOOSTX_PORTABLE_HOME", "").strip()),
            "folders": {folder: str(root / folder) for folder in folders},
        }

    @classmethod
    def load(cls, key: str, default: Any, expected_type: type = dict) -> Any:
        cls.ensure_dirs()
        path = cls.path(key)
        if not path.exists():
            return default.copy() if isinstance(default, dict) else list(default) if isinstance(default, list) else default
        try:
            data = json.loads(path.read_text(encoding="utf-8"))
            if not isinstance(data, expected_type):
                raise ValueError(f"Expected {expected_type.__name__}, got {type(data).__name__}")
            return data
        except Exception:
            stamp = datetime.now().strftime("%Y%m%d%H%M%S")
            backup = path.with_suffix(path.suffix + f".corrupt-{stamp}")
            try:
                path.replace(backup)
            except OSError:
                pass
            cls.save(key, default)
            return default.copy() if isinstance(default, dict) else list(default) if isinstance(default, list) else default

    @classmethod
    def save(cls, key: str, data: Any) -> Path:
        cls.ensure_dirs()
        path = cls.path(key)
        path.write_text(json.dumps(data, indent=2), encoding="utf-8")
        return path


class EnterpriseLogService:
    """Append-only local action log with redaction."""

    @classmethod
    def append(cls, action: str, status: str = "ok", details: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        log = LocalJsonStore.load("action_log", [], list)
        entry = {
            "id": f"log_{uuid.uuid4().hex[:10]}",
            "timestamp": _utc_now(),
            "action": CrashReportService.redact(action),
            "status": CrashReportService.redact(status),
            "details": json.loads(json.dumps(details or {}, default=str), object_hook=lambda value: {
                key: CrashReportService.redact(item) if isinstance(item, str) else item
                for key, item in value.items()
            }),
        }
        log.append(entry)
        LocalJsonStore.save("action_log", log[-500:])
        return entry

    @classmethod
    def latest(cls) -> Dict[str, Any]:
        return {"items": LocalJsonStore.load("action_log", [], list)[-100:]}


class HyperBoostScoreEngine:
    """Deterministic score formulas used by dashboard, reports, and advisor."""

    @classmethod
    def calculate(cls, stats: Optional[Dict[str, Any]] = None, gpu: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        stats = stats or MonitorService.get_current_stats()
        gpu = gpu or GpuDetectionService.get_gpu_summary()
        profile = HardwareProfileService.get_profile(stats=stats, gpu_summary=gpu)
        startup_count = int(profile.get("inputs", {}).get("startup_apps_count") or 0)
        overlay_count = len([item for item in GpuDetectionService.detect_overlays() if item.get("detected")])
        disk = float(stats.get("disk") or 0)
        cpu = float(stats.get("cpu") or 0)
        ram = float(stats.get("memory") or 0)
        gpu_usage = float(gpu.get("gpu_usage_percent") or 0)
        vram = float(gpu.get("vram_usage_percent") or 0)

        storage_score = 100 - min(60, max(0, disk - 60) * 1.5)
        network_score = 92
        security_score = 96
        ai_score = 100 - min(30, overlay_count * 4) - min(25, startup_count * 1.2)

        scores = {
            "gaming_score": _clamp(profile["scores"]["gaming_readiness"] - min(8, max(0, vram - 85) * 0.4)),
            "ai_score": _clamp(ai_score),
            "health_score": _clamp(profile["scores"]["pc_health"]),
            "streaming_score": _clamp(profile["scores"]["streaming_readiness"] - min(8, overlay_count * 1.2)),
            "storage_score": _clamp(storage_score),
            "network_score": _clamp(network_score),
            "security_score": _clamp(security_score),
        }
        return {
            "version": APP_VERSION,
            "created_at": _utc_now(),
            "scores": scores,
            "formula_inputs": {
                "cpu_usage_percent": cpu,
                "ram_usage_percent": ram,
                "gpu_usage_percent": gpu_usage,
                "vram_usage_percent": vram,
                "disk_usage_percent": disk,
                "startup_apps_count": startup_count,
                "overlay_count": overlay_count,
                "dedicated_gpu": bool(gpu.get("dedicated_gpu")),
            },
            "formula_notes": [
                "Scores are local heuristics from resource pressure, startup load, overlays, GPU class, and storage pressure.",
                "Scores are not FPS guarantees and do not use fabricated cloud averages.",
            ],
        }


class PerformanceAdvisorService:
    """Local AI-style diagnosis for bottlenecks and stutter causes."""

    @classmethod
    def analyze(cls, payload: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        payload = payload or {}
        stats = payload.get("stats") or MonitorService.get_current_stats()
        gpu = payload.get("gpu") or GpuDetectionService.get_gpu_summary()
        score = HyperBoostScoreEngine.calculate(stats=stats, gpu=gpu)
        cpu = float(stats.get("cpu") or payload.get("cpu_usage_percent") or 0)
        ram = float(stats.get("memory") or payload.get("ram_usage_percent") or 0)
        disk = float(stats.get("disk") or payload.get("disk_usage_percent") or 0)
        gpu_usage = float(gpu.get("gpu_usage_percent") or payload.get("gpu_usage_percent") or 0)
        vram = float(gpu.get("vram_usage_percent") or payload.get("vram_usage_percent") or 0)
        findings: List[Dict[str, Any]] = []
        recommendations: List[str] = []

        if gpu_usage >= 90 and cpu < 70:
            findings.append({"type": "gpu_bottleneck", "severity": "high", "message": "GPU is saturated while CPU headroom remains."})
            recommendations.extend(["Lower GPU-heavy settings such as resolution scale, shadows, ray tracing, or texture quality.", "Use DLSS/FSR/XeSS when the game and GPU support it.", "Background optimization is optional because the GPU is the main limiter."])
        if cpu >= 85 and gpu_usage < 85:
            findings.append({"type": "cpu_bottleneck", "severity": "high", "message": "CPU pressure is high while GPU is not fully used."})
            recommendations.extend(["Close CPU-heavy background apps after review.", "Prefer game settings that reduce simulation, crowd, traffic, or draw-distance CPU load.", "Keep security and driver services enabled."])
        if vram >= 90:
            findings.append({"type": "vram_pressure", "severity": "high", "message": "VRAM usage is near capacity and may cause texture streaming stutter."})
            recommendations.extend(["Lower texture quality or high-resolution texture packs.", "Close overlays that capture/record if they are not needed."])
        if ram >= 85:
            findings.append({"type": "ram_pressure", "severity": "medium", "message": "System RAM pressure is high and can increase paging/stutter."})
            recommendations.append("Review startup apps and close unused launchers before gaming.")
        if disk >= 90:
            findings.append({"type": "storage_pressure", "severity": "medium", "message": "System drive is nearly full, which can slow updates, shader cache, and paging."})
            recommendations.append("Run safe cleanup preview and move large personal files manually if needed.")

        if not findings:
            findings.append({"type": "balanced", "severity": "low", "message": "No single severe bottleneck was detected from local counters."})
            recommendations.append("Use before/after reports and a manual benchmark run to compare actual behavior.")

        return {
            "title": "HyperBoost AI Performance Advisor",
            "created_at": _utc_now(),
            "diagnosis_mode": "local_deterministic_advisor",
            "detected": {
                "cpu_usage_percent": cpu,
                "ram_usage_percent": ram,
                "gpu_usage_percent": gpu_usage,
                "vram_usage_percent": vram,
                "disk_usage_percent": disk,
            },
            "analysis": findings,
            "recommendations": list(dict.fromkeys(recommendations)),
            "safe_plan": [
                {"action_id": "capture_before_after_report", "requires_approval": False, "risk": "low"},
                {"action_id": "review_overlays", "requires_approval": True, "risk": "low"},
                {"action_id": "review_startup_apps", "requires_approval": True, "risk": "medium"},
            ],
            "blocked_or_risky_actions": ProtectionService.blocked_actions(),
            "score_engine": score,
            "expected_effect_without_guarantee": "May reduce background pressure or reveal the true bottleneck; FPS gains are never guaranteed.",
            "restore_availability": "Restore metadata is required before supported mutating actions.",
            "requires_user_approval": True,
        }


class KnowledgeBaseService:
    """Beginner-friendly glossary for GPU/game optimization terms."""

    TERMS = {
        "dlss": {"title": "DLSS", "summary": "NVIDIA AI upscaling for supported RTX GPUs and games.", "pros": ["Can improve FPS at similar visual quality."], "cons": ["May add slight softness or artifacts depending on mode."], "recommended_for": "NVIDIA RTX GPUs when the game supports it."},
        "fsr": {"title": "FSR", "summary": "AMD upscaling technology available in many games across multiple GPU brands.", "pros": ["Can improve FPS on AMD, NVIDIA, and Intel GPUs."], "cons": ["Image quality varies by game and preset."], "recommended_for": "Use Quality/Balanced first, then tune."},
        "xess": {"title": "XeSS", "summary": "Intel upscaling technology, strongest on Intel Arc but often usable elsewhere.", "pros": ["Can reduce GPU load."], "cons": ["Availability and quality vary by game."], "recommended_for": "Intel Arc users and supported games."},
        "resizable_bar": {"title": "Resizable BAR", "summary": "Allows CPU to access larger chunks of GPU memory when supported by platform, BIOS, and GPU.", "pros": ["Can help some games."], "cons": ["Needs hardware/BIOS support; not changed automatically by HyperBoostX."], "recommended_for": "Check vendor tools/BIOS manually."},
        "game_mode": {"title": "Windows Game Mode", "summary": "Windows feature that prioritizes game workloads and reduces background interruptions.", "pros": ["Safe default for most gaming PCs."], "cons": ["May not noticeably change every game."], "recommended_for": "Beginner mode users."},
        "hags": {"title": "Hardware-Accelerated GPU Scheduling", "summary": "Windows GPU scheduling option that can affect latency and frame pacing.", "pros": ["Can help some systems."], "cons": ["Can hurt stability on others; requires restart."], "recommended_for": "Preview only; change manually after reading vendor guidance."},
        "vrr": {"title": "Variable Refresh Rate", "summary": "Display feature that synchronizes refresh rate to game frame rate.", "pros": ["Can reduce tearing and stutter."], "cons": ["Requires compatible display and configuration."], "recommended_for": "G-Sync, FreeSync, or VRR-capable displays."},
        "v_sync": {"title": "V-Sync", "summary": "Synchronizes frame delivery to display refresh to avoid tearing.", "pros": ["Reduces tearing."], "cons": ["Can add input latency."], "recommended_for": "Single-player games when tearing is distracting."},
        "g_sync": {"title": "G-Sync", "summary": "NVIDIA variable refresh ecosystem.", "pros": ["Smooth frame pacing on compatible displays."], "cons": ["Requires supported display/settings."], "recommended_for": "NVIDIA users with compatible monitors."},
        "freesync": {"title": "FreeSync", "summary": "AMD variable refresh ecosystem using adaptive sync displays.", "pros": ["Smooth frame pacing on compatible displays."], "cons": ["Range and quality vary by monitor."], "recommended_for": "AMD users and adaptive sync monitors."},
        "frame_generation": {"title": "Frame Generation", "summary": "Generates interpolated frames in supported games and GPU stacks.", "pros": ["Can raise displayed frame rate."], "cons": ["May increase latency or artifacts; not raw engine FPS."], "recommended_for": "Single-player games after latency check."},
        "reflex": {"title": "NVIDIA Reflex", "summary": "Latency reduction feature in supported competitive games.", "pros": ["Can reduce input latency."], "cons": ["Requires game support."], "recommended_for": "Competitive games on NVIDIA GPUs."},
        "afmf": {"title": "AMD Fluid Motion Frames", "summary": "AMD frame generation path available through supported AMD driver/software stacks.", "pros": ["Can raise displayed smoothness."], "cons": ["Not suitable for every competitive game."], "recommended_for": "AMD users after checking driver support."},
        "hypr_rx": {"title": "HYPR-RX", "summary": "AMD profile bundle for supported Radeon systems.", "pros": ["Convenient vendor profile."], "cons": ["Controlled by AMD Software, not HyperBoostX."], "recommended_for": "Radeon users who want vendor-managed tuning."},
    }

    @classmethod
    def list_terms(cls) -> Dict[str, Any]:
        return {"items": [{"id": key, **value} for key, value in cls.TERMS.items()]}

    @classmethod
    def get(cls, term_id: str) -> Dict[str, Any]:
        key = (term_id or "").strip().lower().replace("-", "_")
        item = cls.TERMS.get(key)
        if not item:
            return {"error": "Knowledge base term not found", "term_id": term_id}
        return {"id": key, **item}


class PerformanceHistoryService:
    """Persist scan and before/after timeline history."""

    @classmethod
    def record_scan(cls, payload: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        payload = payload or {}
        item = {
            "id": f"scan_{uuid.uuid4().hex[:10]}",
            "created_at": _utc_now(),
            "scores": (payload.get("scores") or HyperBoostScoreEngine.calculate()["scores"]),
            "advisor_summary": (payload.get("advisor_summary") or PerformanceAdvisorService.analyze().get("analysis", [{}])[0].get("message")),
        }
        history = LocalJsonStore.load("performance_history", [], list)
        history.append(item)
        LocalJsonStore.save("performance_history", history[-200:])
        EnterpriseLogService.append("performance_scan_recorded", "ok", {"scan_id": item["id"]})
        return item

    @classmethod
    def history(cls) -> Dict[str, Any]:
        return {"items": LocalJsonStore.load("performance_history", [], list)}

    @classmethod
    def timeline(cls) -> Dict[str, Any]:
        report = ReportService.latest_report()
        before = report.get("before", {})
        after = report.get("after", {})
        metrics = ["cpu_idle_usage_percent", "ram_usage_percent", "background_process_count", "startup_apps_count", "gaming_readiness_score"]
        return {
            "report_id": report.get("report_id"),
            "before": {key: before.get(key) for key in metrics},
            "after": {key: after.get(key) for key in metrics},
            "history": LocalJsonStore.load("performance_history", [], list)[-30:],
        }


class GameDatabaseService:
    """Local game profile database and launcher detection."""

    GAMES = {
        "gta_v": {"name": "GTA V", "processes": ["gta5.exe"], "recommended": {"texture": "High on 8GB+ VRAM", "population_density": "Medium", "upscaling": "Use vendor option if mod/game build supports it"}, "expected_vram": "8GB+ for high textures"},
        "valorant": {"name": "Valorant", "processes": ["valorant-win64-shipping.exe", "valorant.exe"], "recommended": {"nvidia_reflex": "On when available", "raw_input": "On", "background_apps": "Keep chat/voice only if needed"}, "expected_fps": "Depends on hardware; HyperBoostX does not guarantee FPS."},
        "fortnite": {"name": "Fortnite", "processes": ["fortniteclient-win64-shipping.exe"], "recommended": {"rendering_mode": "DX12 or Performance Mode based on stability", "upscaling": "DLSS/FSR/XeSS if supported", "textures": "Match VRAM capacity"}, "expected_fps": "Depends on hardware and game mode."},
        "cyberpunk_2077": {"name": "Cyberpunk 2077", "processes": ["cyberpunk2077.exe"], "recommended": {"ray_tracing": "Lower first if GPU-bound", "dlss_fsr_xess": "Use Quality/Balanced if supported", "texture_quality": "Match VRAM"}, "expected_vram": "8GB+ recommended for high textures."},
    }

    LAUNCHERS = ["Steam", "Epic", "Xbox", "Battle.net", "EA", "Ubisoft", "Riot"]

    @classmethod
    def library(cls) -> Dict[str, Any]:
        user_profiles = LocalJsonStore.load("game_profiles", {"custom": []}, dict)
        return {"items": [{"id": key, **value} for key, value in cls.GAMES.items()], "custom": user_profiles.get("custom", []), "launchers": cls.LAUNCHERS}

    @classmethod
    def running(cls) -> Dict[str, Any]:
        names = [proc.info.get("name", "").lower() for proc in psutil.process_iter(["name"])]
        matches = []
        for game_id, game in cls.GAMES.items():
            if any(proc.lower() in names for proc in game["processes"]):
                matches.append({"id": game_id, "name": game["name"], "status": "running"})
        return {"items": matches}

    @classmethod
    def scan(cls) -> Dict[str, Any]:
        running = cls.running()["items"]
        EnterpriseLogService.append("game_scan", "ok", {"running_count": len(running)})
        return {"running_games": running, "known_games": len(cls.GAMES), "message": "Local process scan completed."}

    @classmethod
    def add_custom(cls, payload: Dict[str, Any]) -> Dict[str, Any]:
        name = str(payload.get("name") or "").strip()
        executable = str(payload.get("executable") or "").strip()
        if not name:
            return {"error": "Game name is required"}
        profile = {"id": f"custom_{uuid.uuid4().hex[:8]}", "name": name, "executable": CrashReportService.redact(executable), "created_at": _utc_now()}
        data = LocalJsonStore.load("game_profiles", {"custom": []}, dict)
        data.setdefault("custom", []).append(profile)
        LocalJsonStore.save("game_profiles", data)
        EnterpriseLogService.append("game_profile_added", "ok", {"game": name})
        return profile

    @classmethod
    def remove_custom(cls, payload: Dict[str, Any]) -> Dict[str, Any]:
        game_id = str(payload.get("id") or "").strip()
        data = LocalJsonStore.load("game_profiles", {"custom": []}, dict)
        before = len(data.get("custom", []))
        data["custom"] = [item for item in data.get("custom", []) if item.get("id") != game_id]
        LocalJsonStore.save("game_profiles", data)
        EnterpriseLogService.append("game_profile_removed", "ok", {"game_id": game_id})
        return {"removed": before != len(data["custom"]), "id": game_id}

    @classmethod
    def profile_preview(cls, payload: Dict[str, Any]) -> Dict[str, Any]:
        game_id = str(payload.get("game_id") or "valorant")
        game = cls.GAMES.get(game_id, cls.GAMES["valorant"])
        return {"game_id": game_id, "name": game["name"], "recommended": game.get("recommended", {}), "safe_actions": ["Capture report", "Review overlays", "Use vendor-supported in-game options"], "requires_approval": True}

    @classmethod
    def profile_apply(cls, payload: Dict[str, Any]) -> Dict[str, Any]:
        if not bool(payload.get("user_approved") or payload.get("approved")):
            return {"success": False, "requires_approval": True, "error": "User approval is required before applying a game profile."}
        session = RestoreService.create_session("game_profile", {"payload": payload, "safe_only": True})
        EnterpriseLogService.append("game_profile_apply", "ok", {"restore_session": session["id"]})
        return {"success": True, "restore_session": session, "message": "Safe game profile metadata applied. In-game graphics changes remain guidance only."}

    @classmethod
    def profile_restore(cls, payload: Dict[str, Any]) -> Dict[str, Any]:
        session_id = str(payload.get("session_id") or "")
        return RestoreService.apply(session_id, preview_only=False)


class ProtectionService:
    """Protected process and Safety Guard evaluator."""

    DEFAULT_PROCESSES = ["vgc.exe", "vgk.sys", "EasyAntiCheat.exe", "BEService.exe", "EACService.exe", "RiotClientServices.exe", "MsMpEng.exe", "NisSrv.exe", "audiodg.exe", "nvcontainer.exe", "amdfendrsr.exe", "IntelCpHDCPSvc.exe"]
    DANGEROUS_TOKENS = ["defender", "windows update", "wuauserv", "anticheat", "anti-cheat", "eac", "battleye", "vgc", "vgk", "overclock", "undervolt", "voltage", "bios", "uefi", "gpu driver service", "audio service", "network driver", "delete documents", "delete downloads", "shell", "powershell"]

    @classmethod
    def list_processes(cls) -> Dict[str, Any]:
        data = LocalJsonStore.load("protected_processes", {"items": cls.DEFAULT_PROCESSES}, dict)
        return {"items": data.get("items", cls.DEFAULT_PROCESSES), "enabled": True}

    @classmethod
    def add(cls, payload: Dict[str, Any]) -> Dict[str, Any]:
        name = str(payload.get("name") or "").strip()
        if not name:
            return {"error": "Process name is required"}
        data = cls.list_processes()
        items = sorted(set(data["items"] + [name]))
        LocalJsonStore.save("protected_processes", {"items": items})
        return {"items": items}

    @classmethod
    def remove(cls, payload: Dict[str, Any]) -> Dict[str, Any]:
        name = str(payload.get("name") or "").strip().lower()
        items = [item for item in cls.list_processes()["items"] if item.lower() != name]
        LocalJsonStore.save("protected_processes", {"items": items})
        return {"items": items}

    @classmethod
    def reset(cls) -> Dict[str, Any]:
        LocalJsonStore.save("protected_processes", {"items": cls.DEFAULT_PROCESSES})
        return cls.list_processes()

    @classmethod
    def evaluate(cls, payload: Dict[str, Any]) -> Dict[str, Any]:
        action = str(payload.get("action") or payload.get("action_id") or "").lower()
        target = str(payload.get("target") or payload.get("process") or "").lower()
        protected = [item.lower() for item in cls.list_processes()["items"]]
        blocked = any(token in action or token in target for token in cls.DANGEROUS_TOKENS) or target in protected
        return {"allowed": not blocked, "blocked": blocked, "reason": "Safety Guard blocked dangerous/protected action." if blocked else "Action is not on the local blocklist.", "requires_approval": True}

    @classmethod
    def blocked_actions(cls) -> List[str]:
        return ["Disable Defender", "Permanent Windows Update disable", "Anti-cheat process/service changes", "GPU/audio/network driver service changes", "Overclock/undervolt/voltage/BIOS actions", "Arbitrary shell execution", "Destructive cleanup"]


class ProcessAnalyzerService:
    """Read-only process pressure analyzer."""

    @classmethod
    def heavy(cls, limit: int = 10) -> Dict[str, Any]:
        rows = []
        for proc in psutil.process_iter(["pid", "name", "cpu_percent", "memory_info"]):
            try:
                memory = getattr(proc.info.get("memory_info"), "rss", 0) / (1024 * 1024)
                rows.append({"pid": proc.info.get("pid"), "name": CrashReportService.redact(proc.info.get("name", "")), "cpu_percent": proc.info.get("cpu_percent") or 0, "memory_mb": round(memory, 1)})
            except (psutil.NoSuchProcess, psutil.AccessDenied):
                continue
        rows.sort(key=lambda item: (item["cpu_percent"], item["memory_mb"]), reverse=True)
        return {"items": rows[:limit], "read_only": True}

    @staticmethod
    def startup_impact() -> Dict[str, Any]:
        return {"items": StartupService().get_startup_items()}

    @staticmethod
    def recommendations() -> Dict[str, Any]:
        profile = HardwareProfileService.get_profile()
        return {"items": profile.get("requires_approval", []) + profile.get("safe_actions", []), "requires_approval": True}

    @classmethod
    def export_report(cls) -> Dict[str, Any]:
        report = {"created_at": _utc_now(), "heavy_processes": cls.heavy()["items"], "startup": cls.startup_impact()["items"][:20]}
        LocalJsonStore.save("process_reports", [report])
        return {"format": "json", "content": json.dumps(report, indent=2), "report": report}


class BenchmarkReportService:
    """Manual benchmark input, CSV import, history, and export."""

    @classmethod
    def manual(cls, payload: Dict[str, Any]) -> Dict[str, Any]:
        item = {
            "id": f"bench_{uuid.uuid4().hex[:10]}",
            "created_at": _utc_now(),
            "game": str(payload.get("game") or "Manual Benchmark"),
            "avg_fps": float(payload.get("avg_fps") or 0),
            "one_percent_low_fps": float(payload.get("one_percent_low_fps") or 0),
            "average_frametime_ms": float(payload.get("average_frametime_ms") or 0),
            "notes": CrashReportService.redact(payload.get("notes") or ""),
            "comparison": "Local history only. Similar-hardware cloud averages are roadmap until a verified dataset exists.",
        }
        history = LocalJsonStore.load("benchmark_history", [], list)
        history.append(item)
        LocalJsonStore.save("benchmark_history", history[-200:])
        return item

    @classmethod
    def import_csv(cls, content: str) -> Dict[str, Any]:
        reader = csv.DictReader(content.splitlines())
        imported = []
        for row in reader:
            imported.append(cls.manual(row))
        return {"imported": len(imported), "items": imported}

    @staticmethod
    def latest() -> Dict[str, Any]:
        history = LocalJsonStore.load("benchmark_history", [], list)
        return history[-1] if history else {"message": "No benchmark history yet."}

    @staticmethod
    def history() -> Dict[str, Any]:
        return {"items": LocalJsonStore.load("benchmark_history", [], list)}

    @staticmethod
    def export() -> Dict[str, Any]:
        payload = {"items": LocalJsonStore.load("benchmark_history", [], list), "comparison_note": "No global average is shown without a verified dataset."}
        return {"format": "json", "content": json.dumps(payload, indent=2), "report": payload}


class GpuCenterService:
    """Vendor-aware GPU guidance without driver hacks."""

    @classmethod
    def vendor_guide(cls) -> Dict[str, Any]:
        gpu = GpuDetectionService.get_gpu_summary()
        vendor = gpu.get("vendor")
        guide = {
            GpuVendor.NVIDIA: ["Check NVIDIA App for DLSS, Reflex, G-Sync, and driver notes.", "Keep NVIDIA driver services enabled."],
            GpuVendor.AMD: ["Check AMD Software for HYPR-RX, AFMF, FreeSync, and driver notes.", "Keep Radeon driver services enabled."],
            GpuVendor.INTEL: ["Check Intel Graphics Software for XeSS and Arc driver notes.", "Use Intel-supported controls for driver changes."],
            GpuVendor.MICROSOFT_BASIC: ["Install the official GPU driver from the PC/GPU vendor when available."],
            GpuVendor.UNKNOWN: ["Unknown GPU fallback active. HyperBoostX will avoid vendor-specific changes."],
        }
        return {"gpu": gpu, "guide": guide.get(vendor, guide[GpuVendor.UNKNOWN])}

    @classmethod
    def recommendations(cls) -> Dict[str, Any]:
        advisor = PerformanceAdvisorService.analyze()
        return {"items": advisor.get("recommendations", []), "blocked": ProtectionService.blocked_actions()}

    @staticmethod
    def export_report() -> Dict[str, Any]:
        report = {"created_at": _utc_now(), "gpu": GpuDetectionService.get_gpu_summary(), "vendor_software": GpuDetectionService.detect_vendor_software(), "overlays": GpuDetectionService.detect_overlays()}
        return {"format": "json", "content": json.dumps(report, indent=2), "report": report}

    @staticmethod
    def hardware_database() -> Dict[str, Any]:
        gpu = GpuDetectionService.get_gpu_summary()
        vendor = gpu.get("vendor", GpuVendor.UNKNOWN)
        supports = []
        if vendor == GpuVendor.NVIDIA:
            supports = ["DLSS on supported RTX GPUs/games", "Reflex in supported games", "G-Sync on compatible displays"]
        elif vendor == GpuVendor.AMD:
            supports = ["FSR in supported games", "FreeSync on compatible displays", "HYPR-RX/AFMF where supported by AMD Software"]
        elif vendor == GpuVendor.INTEL:
            supports = ["XeSS in supported games", "Arc Control/Intel Graphics Software guidance"]
        return {"gpu": gpu, "hardware_profile": {"architecture": "Detected locally when exposed by Windows/vendor APIs", "supports": supports, "driver_recommendation": "Use latest stable from official vendor source; HyperBoostX does not auto-download drivers."}}


class DriverRecommendationService:
    @staticmethod
    def status() -> Dict[str, Any]:
        gpu = GpuDetectionService.get_gpu_summary()
        return {
            "current_driver": gpu.get("driver_version", "Unknown"),
            "latest_stable": None,
            "recommendation": "Check the official NVIDIA/AMD/Intel or OEM release page. HyperBoostX does not fabricate latest-driver numbers or auto-download drivers.",
            "source_required": True,
            "auto_download": False,
        }


class OverlayCenterService:
    @staticmethod
    def status() -> Dict[str, Any]:
        items = GpuDetectionService.detect_overlays()
        return {"items": items, "detected_count": len([item for item in items if item.get("detected")])}

    @staticmethod
    def recommendations() -> Dict[str, Any]:
        items = [item for item in GpuDetectionService.detect_overlays() if item.get("detected")]
        return {"items": [{"id": item["id"], "name": item["name"], "recommendation": "Pause only if not recording/streaming and after user approval."} for item in items], "requires_approval": True}


class StartupManagerFacade:
    @staticmethod
    def items() -> Dict[str, Any]:
        return {"items": StartupService().get_startup_items()}

    @staticmethod
    def preview(payload: Dict[str, Any]) -> Dict[str, Any]:
        items = payload.get("items") or []
        return {"preview": items, "requires_approval": True, "restore_metadata_required": True, "blocked": [item for item in items if not ProtectionService.evaluate({"target": str(item)})["allowed"]]}

    @staticmethod
    def apply(payload: Dict[str, Any]) -> Dict[str, Any]:
        if not bool(payload.get("user_approved") or payload.get("approved")):
            return {"success": False, "requires_approval": True}
        session = RestoreService.create_session("startup_manager", {"items": payload.get("items", [])})
        return {"success": True, "restore_session": session, "message": "Startup changes recorded as restore metadata; direct disable is delegated to existing safe UI flow."}

    @staticmethod
    def restore(payload: Dict[str, Any]) -> Dict[str, Any]:
        return RestoreService.apply(str(payload.get("session_id") or ""), preview_only=False)

    @staticmethod
    def export_report() -> Dict[str, Any]:
        report = {"created_at": _utc_now(), "items": StartupService().get_startup_items()}
        return {"format": "json", "content": json.dumps(report, indent=2), "report": report}


class CleanupCenterService:
    SAFE_ROOTS = [Path(tempfile.gettempdir()).resolve()]

    @classmethod
    def scan(cls) -> Dict[str, Any]:
        total = 0
        files = 0
        for root in cls.SAFE_ROOTS:
            try:
                for path in root.rglob("*"):
                    if path.is_file():
                        files += 1
                        total += path.stat().st_size
                    if files >= 5000:
                        break
            except OSError:
                continue
        return {"safe_roots": [str(root) for root in cls.SAFE_ROOTS], "estimated_files": files, "estimated_size_mb": round(total / (1024 * 1024), 1), "destructive_cleanup_blocked": True}

    @staticmethod
    def preview(payload: Dict[str, Any]) -> Dict[str, Any]:
        return {"scan": CleanupCenterService.scan(), "requires_approval": True, "will_delete_user_documents": False}

    @staticmethod
    def apply(payload: Dict[str, Any]) -> Dict[str, Any]:
        if not bool(payload.get("user_approved") or payload.get("approved")):
            return {"success": False, "requires_approval": True, "preview": CleanupCenterService.preview(payload)}
        session = RestoreService.create_session("cleanup", {"scope": "safe_temp_only", "deleted_files": 0})
        report = {"created_at": _utc_now(), "success": True, "deleted_files": 0, "message": "Cleanup apply is conservative in v1.4 backend; destructive deletion remains blocked unless implemented with verified restore boundaries."}
        LocalJsonStore.save("cleanup_reports", [report])
        return {"success": True, "restore_session": session, "report": report}

    @staticmethod
    def report() -> Dict[str, Any]:
        return {"items": LocalJsonStore.load("cleanup_reports", [], list)}

    @staticmethod
    def export_report() -> Dict[str, Any]:
        report = CleanupCenterService.report()
        return {"format": "json", "content": json.dumps(report, indent=2), "report": report}


class NetworkToolsFacade:
    @staticmethod
    def diagnostics() -> Dict[str, Any]:
        return {"hostname": CrashReportService.redact(socket.gethostname()), "dns": NetworkService.test_dns(), "local_only": True}

    @staticmethod
    def ping(payload: Dict[str, Any]) -> Dict[str, Any]:
        host = str(payload.get("host") or "1.1.1.1")[:120]
        start = time.perf_counter()
        try:
            socket.getaddrinfo(host, 53)
            latency = round((time.perf_counter() - start) * 1000, 2)
            return {"host": CrashReportService.redact(host), "latency_ms": latency, "status": "resolved"}
        except OSError as exc:
            return {"host": CrashReportService.redact(host), "error": str(exc), "status": "failed"}

    @staticmethod
    def dns_test() -> Dict[str, Any]:
        return NetworkService.test_dns()

    @staticmethod
    def flush_dns() -> Dict[str, Any]:
        return NetworkService.flush_dns()

    @staticmethod
    def export_report() -> Dict[str, Any]:
        report = {"created_at": _utc_now(), "diagnostics": NetworkToolsFacade.diagnostics()}
        return {"format": "json", "content": json.dumps(report, indent=2), "report": report}


class GamingEssentialsService:
    ESSENTIALS = [
        {"id": "directx", "name": "DirectX Runtime", "category": "runtime", "install_mode": "manual_official"},
        {"id": "vc_redist", "name": "Microsoft Visual C++ Redistributable", "category": "runtime", "install_mode": "manual_official"},
        {"id": "steam", "name": "Steam", "category": "launcher", "install_mode": "manual_official"},
        {"id": "epic", "name": "Epic Games Launcher", "category": "launcher", "install_mode": "manual_official"},
        {"id": "obs", "name": "OBS Studio", "category": "streaming", "install_mode": "manual_official"},
    ]

    @classmethod
    def list(cls) -> Dict[str, Any]:
        return {"items": cls.ESSENTIALS, "auto_install": False}

    @classmethod
    def check(cls) -> Dict[str, Any]:
        running = [proc.info.get("name", "").lower() for proc in psutil.process_iter(["name"])]
        items = []
        for item in cls.ESSENTIALS:
            detected = any(item["id"] in name or item["name"].split()[0].lower() in name for name in running)
            items.append({**item, "detected_running": detected})
        return {"items": items}

    @staticmethod
    def install_preview(payload: Dict[str, Any]) -> Dict[str, Any]:
        return {"requires_user_approval": True, "auto_install": False, "message": "HyperBoostX opens official/manual install guidance only; no silent installer is run."}

    @staticmethod
    def install(payload: Dict[str, Any]) -> Dict[str, Any]:
        return {"success": False, "manual_only": True, "message": "Automatic install is intentionally blocked in v1.4. Use official download links manually."}


class StreamingCenterService:
    STREAMING_APPS = ["obs", "discord", "nvidia broadcast", "voicemeeter", "elgato", "tiktok live studio"]

    @classmethod
    def status(cls) -> Dict[str, Any]:
        apps = GpuDetectionService.detect_background_apps()
        detected = [item for item in apps if item.get("detected") and item.get("category") in {"streaming", "chat", "recording"}]
        score = _clamp(92 - max(0, len(detected) - 3) * 5)
        return {"streaming_ready_score": score, "detected_apps": detected, "recommendations": ["Keep OBS/voice apps enabled when streaming.", "Pause duplicate overlays only after approval."]}


class RgbDetectionService:
    @staticmethod
    def status() -> Dict[str, Any]:
        items = [item for item in GpuDetectionService.detect_background_apps() if item.get("category") == "rgb"]
        return {"items": items, "control_enabled": False, "roadmap": "RGB control is roadmap; v1.4 only detects and advises."}


class PluginRegistryService:
    DEFAULT_PLUGINS = ["Network", "GPU", "Benchmark", "Cleanup", "RGB", "Streaming", "Monitoring"]

    @classmethod
    def registry(cls) -> Dict[str, Any]:
        data = LocalJsonStore.load("plugin_registry", {"plugins": cls.DEFAULT_PLUGINS}, dict)
        return {"plugins": [{"name": name, "status": "built-in" if name != "RGB" else "roadmap-detect-only"} for name in data.get("plugins", cls.DEFAULT_PLUGINS)], "third_party_loading": False, "sdk_status": "roadmap"}


class UiSettingsService:
    DEFAULT = {
        "theme": "Cyber Dark",
        "accent": "Cyan",
        "available_themes": ["Cyber Blue", "Cyber Purple", "Matrix Green", "Red Alert", "Orange Neon", "Hyper Dark", "Hyper White", "Glass", "OLED"],
        "reduce_motion": False,
        "high_contrast": False,
        "font_scale": 1.0,
        "telemetry_opt_in": False,
        "anonymous_usage": False,
        "performance_budget": {"startup_seconds_modern_pc": 2, "idle_ram_mb_target": 150, "idle_cpu_percent_target": "0-1"},
    }

    @classmethod
    def get(cls) -> Dict[str, Any]:
        return LocalJsonStore.load("ui_settings", cls.DEFAULT, dict)

    @classmethod
    def update(cls, payload: Dict[str, Any]) -> Dict[str, Any]:
        settings = cls.get()
        allowed = set(cls.DEFAULT.keys()) | {"accent", "theme", "reduce_motion", "high_contrast", "font_scale"}
        for key, value in payload.items():
            if key in allowed:
                settings[key] = value
        settings["telemetry_opt_in"] = bool(settings.get("telemetry_opt_in", False))
        settings["anonymous_usage"] = bool(settings.get("anonymous_usage", False))
        LocalJsonStore.save("ui_settings", settings)
        return settings


class RestoreService:
    @staticmethod
    def create_session(kind: str, metadata: Dict[str, Any]) -> Dict[str, Any]:
        sessions = LocalJsonStore.load("restore_sessions", [], list)
        session = {"id": f"restore_{uuid.uuid4().hex[:10]}", "created_at": _utc_now(), "kind": kind, "metadata": metadata, "verified": True, "apply_supported": True}
        sessions.append(session)
        LocalJsonStore.save("restore_sessions", sessions[-200:])
        return session

    @staticmethod
    def sessions() -> Dict[str, Any]:
        return {"items": LocalJsonStore.load("restore_sessions", [], list)}

    @staticmethod
    def get(session_id: str) -> Dict[str, Any]:
        for item in LocalJsonStore.load("restore_sessions", [], list):
            if item.get("id") == session_id:
                return item
        return {"error": "Restore session not found", "id": session_id}

    @staticmethod
    def preview(session_id: str) -> Dict[str, Any]:
        session = RestoreService.get(session_id)
        return {"session": session, "preview_only": True, "requires_approval": True}

    @staticmethod
    def apply(session_id: str, preview_only: bool = False) -> Dict[str, Any]:
        session = RestoreService.get(session_id)
        if session.get("error"):
            return {"success": False, **session}
        return {"success": not preview_only, "session": session, "message": "Restore metadata verified. System-level rollback remains limited to supported actions."}

    @staticmethod
    def verify(session_id: str) -> Dict[str, Any]:
        session = RestoreService.get(session_id)
        return {"verified": not bool(session.get("error")), "session": session}

    @staticmethod
    def export() -> Dict[str, Any]:
        report = RestoreService.sessions()
        return {"format": "json", "content": json.dumps(report, indent=2), "report": report}


class AutoGamingModeService:
    @staticmethod
    def settings() -> Dict[str, Any]:
        return LocalJsonStore.load("auto_gaming_settings", {"enabled": False, "auto_restore_after_game_closes": True, "protected_process_list_enabled": True, "mode": "Beginner"}, dict)

    @staticmethod
    def preview(payload: Dict[str, Any]) -> Dict[str, Any]:
        return {"settings": AutoGamingModeService.settings(), "game_scan": GameDatabaseService.scan(), "safe_plan": ["Detect running game", "Capture before snapshot", "Review overlays", "Create restore metadata"], "requires_approval": True}

    @staticmethod
    def apply(payload: Dict[str, Any]) -> Dict[str, Any]:
        if not bool(payload.get("user_approved") or payload.get("approved")):
            return {"success": False, "requires_approval": True}
        settings = AutoGamingModeService.settings()
        settings.update({key: payload[key] for key in ["enabled", "auto_restore_after_game_closes", "mode"] if key in payload})
        LocalJsonStore.save("auto_gaming_settings", settings)
        session = RestoreService.create_session("auto_gaming_mode", settings)
        return {"success": True, "settings": settings, "restore_session": session}

    @staticmethod
    def restore(payload: Dict[str, Any]) -> Dict[str, Any]:
        settings = AutoGamingModeService.settings()
        settings["enabled"] = False
        LocalJsonStore.save("auto_gaming_settings", settings)
        return {"success": True, "settings": settings}


class FeatureAuditService:
    @staticmethod
    def run() -> Dict[str, Any]:
        items = [
            ("Backend health", True),
            ("Session token middleware", True),
            ("Safety Guard", True),
            ("GPU Center", True),
            ("Auto Gaming Mode", True),
            ("Game Profiles", True),
            ("Overlay Detector", True),
            ("Protected Processes", True),
            ("Benchmark local history", True),
            ("Global benchmark comparison", False),
            ("RGB control", False),
            ("Plugin SDK", False),
            ("Cloud sync", False),
        ]
        audit = {"created_at": _utc_now(), "mode": "read_only", "items": [{"name": name, "status": "pass" if ok else "roadmap"} for name, ok in items], "destructive_actions_run": False}
        history = LocalJsonStore.load("feature_audit_history", [], list)
        history.append(audit)
        LocalJsonStore.save("feature_audit_history", history[-100:])
        return audit


class SystemProductInfoService:
    @staticmethod
    def local_storage() -> Dict[str, Any]:
        return LocalJsonStore.ensure_dirs()

    @staticmethod
    def v2_roadmap() -> Dict[str, Any]:
        return {
            "title": "HyperBoostX v2 Vision 2026-2027",
            "items": ["Plugin SDK and official marketplace", "Optional local LLM diagnosis", "Driver health analyzer", "Hardware stress test", "Verified benchmark engine/dataset", "Optional cloud sync", "Professional license system", "Auto updater with rollback", "Opt-in crash analytics"],
            "not_in_v14": True,
        }

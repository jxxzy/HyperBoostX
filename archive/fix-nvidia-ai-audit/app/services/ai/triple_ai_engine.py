"""HyperBoostX Triple AI Engine with cloud-optional, local-safe fallback."""

import json
import os
import re
import time
import uuid
from datetime import datetime, timezone
from typing import Any, Dict, List, Tuple

try:
    import requests
except ImportError:  # optional cloud dependency; local fallback still works
    requests = None

from core.config import Config
from core.logger import Logger
from services.ai.knowledge_base_service import KnowledgeBaseService
from services.ai.pc_scanner_service import PcScannerService
from services.optimization.tweak_service import TweakService


logger = Logger.get_logger(__name__)


class TripleAiEngine:
    """AI Assistant, AI Analyzer, AI Safety Guard, and RAG-backed local fallback."""

    ASSISTANT_MODEL = Config.AI_ASSISTANT_MODEL
    ANALYZER_MODEL = Config.AI_ANALYZER_MODEL
    SAFETY_MODEL = Config.AI_SAFETY_MODEL
    EMBED_MODEL = Config.AI_EMBED_MODEL
    DEFAULT_MODEL = Config.NVIDIA_DEFAULT_MODEL
    FALLBACK_MODEL = Config.NVIDIA_FALLBACK_MODEL

    BLOCKED_TWEAK_IDS = {
        "disable_defender",
        "disable_windows_security",
        "disable_updates",
        "disable_windows_update",
        "auto_overclock",
        "auto_undervolt",
        "voltage_change",
        "bios_uefi_change",
        "delete_windows_service_permanent",
        "irreversible_registry_edit",
    }

    BLOCKED_TERMS = (
        "overclock",
        "undervolt",
        "voltage",
        "bios",
        "uefi",
        "disable windows security",
        "disable defender",
        "disable firewall",
        "disable windows update permanent",
        "permanently disable windows update",
        "delete service",
        "remove service",
        "guaranteed fps",
        "pasti naik",
    )

    def __init__(
        self,
        knowledge_base: KnowledgeBaseService | None = None,
        system_info_service: Any | None = None,
        monitor_service: Any | None = None,
        startup_service: Any | None = None,
        tweak_service: Any | None = None,
    ):
        self.knowledge_base = knowledge_base or KnowledgeBaseService()
        self.reports_dir = Config.DATA_DIR / "performance-reports"
        self.reports_dir.mkdir(parents=True, exist_ok=True)
        self.system_info_service = system_info_service
        self.monitor_service = monitor_service
        self.startup_service = startup_service
        self.tweak_service = tweak_service
        self.scanner = PcScannerService()

    def scan_pc(self) -> Dict[str, Any]:
        """Run Scan My PC and return a sanitized MVP scan contract."""
        if self.system_info_service or self.monitor_service or self.startup_service:
            return self._scan_with_injected_services()
        return self.scanner.scan_pc()

    def analyze(self, scan_result: Dict[str, Any], user_goal: str = "gaming", game: str = "") -> Dict[str, Any]:
        """Compatibility wrapper for the AI Analyzer role."""
        result = self.analyze_scan(scan_result.get("scan_id", ""), scan_result, user_goal)
        result["role"] = "AI Analyzer"
        if game:
            result["game_optimization"] = self.optimize_game(game, scan_result)
        return result

    def assistant_response(
        self,
        scan_result: Dict[str, Any],
        analysis_result: Dict[str, Any],
        safety_result: Dict[str, Any],
    ) -> Dict[str, Any]:
        """Compatibility wrapper for the AI Assistant role."""
        result = self.assistant_summary(scan_result, analysis_result, safety_result)
        result["role"] = "AI Assistant"
        return result

    def run_full_flow(self, user_goal: str = "gaming", game: str = "") -> Dict[str, Any]:
        """Run Scan -> Analyze -> Safety -> Assistant -> Report without applying tweaks."""
        scan = self.scan_pc()
        analysis = self.analyze(scan, user_goal=user_goal, game=game)
        safety = self.safety_check(analysis.get("recommendations", []))
        safety["role"] = "AI Safety Guard"
        assistant = self.assistant_response(scan, analysis, safety)
        report = self.create_performance_report(scan, analysis, safety, assistant)
        return {
            "scan": scan,
            "analysis": analysis,
            "safety": safety,
            "assistant": assistant,
            "report": report,
        }

    def apply_safe_tweaks(self, approved_tweaks: List[Dict[str, Any]], user_approved: bool = False) -> Dict[str, Any]:
        """Apply only Safety Guard approved, reversible, auto-apply tweaks."""
        if not user_approved:
            logger.info("user approval missing for safe tweak apply")
            return {
                "success": False,
                "applied": [],
                "failed": [],
                "blocked": [],
                "backup_id": "",
                "error": "User approval is required before applying tweaks.",
            }

        safety = self.safety_check(approved_tweaks)
        tweak_service = self.tweak_service
        if tweak_service is None:
            from services.optimization.tweak_service import TweakService

            tweak_service = TweakService

        applied = []
        failed = []
        for item in safety.get("approved", []):
            if not item.get("can_auto_apply"):
                continue
            tweak_id = item.get("tweak_id") or item.get("id")
            if not tweak_id:
                continue
            logger.info("user approval received for tweak: %s", tweak_id)
            result = tweak_service.apply_tweak(tweak_id, confirmed=True)
            record = {"tweak_id": tweak_id, "result": result}
            if result.get("success"):
                applied.append(record)
                logger.info("tweak applied: %s", tweak_id)
            else:
                failed.append(record)
                logger.warning("tweak failed: %s", tweak_id)

        backup_id = ""
        for item in applied:
            backup_id = item.get("result", {}).get("restore_timestamp") or backup_id

        return {
            "success": bool(applied) and not failed,
            "applied": applied,
            "failed": failed,
            "blocked": safety.get("blocked", []),
            "warnings": safety.get("warnings", []),
            "backup_id": backup_id,
            "safety": safety,
        }

    def revert_tweaks(self, backup_id: str = "", tweak_ids: List[str] | None = None) -> Dict[str, Any]:
        """Revert tweaks by explicit tweak IDs using their latest restore points."""
        tweak_service = self.tweak_service
        if tweak_service is None:
            from services.optimization.tweak_service import TweakService

            tweak_service = TweakService

        reverted = []
        failed = []
        for tweak_id in tweak_ids or []:
            result = tweak_service.revert_tweak(tweak_id)
            if result.get("success"):
                reverted.append({"tweak_id": tweak_id, "result": result})
            else:
                failed.append({"tweak_id": tweak_id, "result": result})
        return {"reverted": reverted, "failed": failed, "backup_id": backup_id}

    def create_performance_report(
        self,
        scan_result: Dict[str, Any],
        analysis_result: Dict[str, Any],
        safety_result: Dict[str, Any],
        assistant_result: Dict[str, Any] | None = None,
    ) -> Dict[str, Any]:
        report = self.create_report(scan_result, analysis_result, safety_result)
        if assistant_result:
            report["assistant_summary"] = assistant_result.get("message", "")
        return report

    def _kb_recommendation(self, tweak_id: str) -> Dict[str, Any]:
        kb_tweak = self.knowledge_base.find_tweak(tweak_id)
        if not kb_tweak:
            return self._rec(tweak_id, tweak_id.replace("_", " ").title(), "", "low", "Local recommendation.")
        return self._rec_from_tweak(
            tweak_id,
            kb_tweak.get("name", tweak_id),
            kb_tweak.get("description", ""),
            "medium",
        )

    def _cloud_enabled(self) -> bool:
        return (
            str(os.environ.get("AI_CLOUD_ENABLED", "true")).lower() in {"1", "true", "yes", "on"}
            and requests is not None
            and bool(os.environ.get("NVIDIA_API_KEY", "").strip())
        )

    def analyze_scan(self, scan_id: str, scan_result: Dict[str, Any], user_goal: str = "safe_boost") -> Dict[str, Any]:
        logger.info("AI Analyzer request: scan=%s goal=%s", scan_id, user_goal)
        local_result = self._local_analyze(scan_id, scan_result, user_goal)
        cloud_result = self._try_cloud_json(
            model=os.environ.get("AI_ANALYZER_MODEL", self.ANALYZER_MODEL),
            system_prompt=(
                "You are HyperBoostX AI Analyzer. Return only JSON with issues, "
                "recommendations, confidence, health_score, and gaming_readiness_score. "
                "Never recommend dangerous tweaks. All recommendations must include "
                "risk_level, reversible, requires_backup, requires_restore_point, "
                "can_auto_apply, user_approval_required, and expected_impact."
            ),
            payload={
                "scan_id": scan_id,
                "scan_result": self._sanitize_for_cloud(scan_result),
                "user_goal": user_goal,
                "knowledge_context": local_result.get("rag_context", []),
            },
        )
        if self._valid_analysis(cloud_result):
            merged = self._normalize_analysis(cloud_result, local_result)
            logger.info("AI Analyzer result: cloud recommendations=%s", len(merged.get("recommendations", [])))
            return merged

        logger.info("AI Analyzer result: local fallback recommendations=%s", len(local_result.get("recommendations", [])))
        return local_result

    def _scan_with_injected_services(self) -> Dict[str, Any]:
        """Build a scan from injected test/service dependencies."""
        system_info = self.system_info_service
        monitor = self.monitor_service
        startup = self.startup_service
        stats = self._safe_dependency_call(monitor, "get_current_stats", {})
        cpu = self._safe_dependency_call(system_info, "get_cpu_info", {})
        memory = self._safe_dependency_call(system_info, "get_memory_info", {})
        disk = self._safe_dependency_call(system_info, "get_disk_info", {})
        system_drive = self._safe_dependency_call(system_info, "get_system_drive_info", {})
        os_info = self._safe_dependency_call(system_info, "get_os_info", {})
        gpu_info = self._safe_dependency_call(system_info, "get_gpu_info", {})
        startup_items = self._safe_dependency_call(startup, "get_startup_items", [])
        processes = self._safe_dependency_call(monitor, "get_process_list", [])

        gpus = gpu_info.get("gpus") or []
        primary_gpu = gpus[0] if gpus else {}
        gpu_name = primary_gpu.get("name") or (stats.get("gpu") or {}).get("name") or "Unknown"
        vram = primary_gpu.get("vram") or 0
        vram_gb = round(float(vram or 0) / (1024**3), 2) if vram else round(float((stats.get("gpu") or {}).get("memory_total_mb") or 0) / 1024, 2)
        free_gb = 0.0
        for item in (disk or {}).values():
            if isinstance(item, dict) and item.get("free"):
                free_gb = round(float(item.get("free") or 0) / (1024**3), 2)
                break

        scan = {
            "scan_id": f"scan-test-{int(time.time())}",
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "hardware": {
                "cpu_name": cpu.get("processor", "Unknown"),
                "gpu_name": gpu_name,
                "ram_total_gb": round(float(memory.get("total") or 0) / (1024**3), 2),
                "storage_type": system_drive.get("storage_class", "Unknown"),
                "cpu": {
                    "name": cpu.get("processor", "Unknown"),
                    "cores": cpu.get("cores", 0),
                    "threads": cpu.get("threads", 0),
                    "usage_percent": stats.get("cpu", 0),
                },
                "gpu": {
                    "name": gpu_name,
                    "driver_version": primary_gpu.get("driver_version", "Unknown"),
                    "vram_gb": vram_gb,
                    "usage_percent": (stats.get("gpu") or {}).get("load", 0),
                    "temperature_c": (stats.get("gpu") or {}).get("temperature", 0),
                },
                "ram": {
                    "total_gb": round(float(memory.get("total") or 0) / (1024**3), 2),
                    "usage_percent": stats.get("memory", 0),
                    "speed_mhz": memory.get("speed_mhz", 0),
                },
                "storage": {
                    "type": system_drive.get("storage_class", "Unknown"),
                    "free_gb": free_gb,
                    "usage_percent": stats.get("disk", 0),
                },
            },
            "windows": {
                "version": os_info.get("version", "Unknown"),
                "build_number": os_info.get("release", "Unknown"),
                "power_plan": "Balanced",
                "game_mode": "Unknown",
                "hags": "Unknown",
                "startup_apps": startup_items[:20],
                "background_apps_heavy": processes[:12],
                "temporary_files_size_mb": 0,
            },
            "nvidia": {
                "is_nvidia": "nvidia" in gpu_name.lower() or "rtx" in gpu_name.lower() or "gtx" in gpu_name.lower(),
                "is_rtx": "rtx" in gpu_name.lower(),
                "gpu_name": gpu_name,
                "driver_version": primary_gpu.get("driver_version", "Unknown"),
                "vram_gb": vram_gb,
            },
            "apps": {
                "startup_count": len(startup_items),
                "startup_high_impact": sum(1 for item in startup_items if item.get("impact") == "High"),
                "background_process_count": stats.get("processes", 0),
                "top_background_apps": processes[:8],
            },
            "performance": {
                "cpu_usage_percent": stats.get("cpu", 0),
                "ram_usage_percent": stats.get("memory", 0),
                "disk_usage_percent": stats.get("disk", 0),
                "gpu_usage_percent": (stats.get("gpu") or {}).get("load", 0),
                "gpu_temperature_c": (stats.get("gpu") or {}).get("temperature", 0),
                "processes": stats.get("processes", 0),
            },
        }
        from services.ai.pc_scanner_service import PcScannerService as _Scanner

        scan["scores"] = _Scanner.calculate_scores(scan)
        return scan

    @staticmethod
    def _safe_dependency_call(service: Any, method_name: str, default: Any) -> Any:
        if not service:
            return default
        try:
            method = getattr(service, method_name)
            return method()
        except TypeError:
            try:
                method = getattr(service, method_name)
                return method(limit=15)
            except Exception:
                return default
        except Exception:
            return default

    def safety_check(self, recommendations: List[Dict[str, Any]]) -> Dict[str, Any]:
        logger.info("Safety Guard decision requested: recommendations=%s", len(recommendations or []))
        approved: List[Dict[str, Any]] = []
        blocked: List[Dict[str, Any]] = []
        warnings: List[Dict[str, Any]] = []

        for item in recommendations or []:
            decision = self.evaluate_recommendation(item)
            guarded = dict(item)
            guarded.update(
                {
                    "risk_level": decision["risk_level"],
                    "safety_status": decision["status"],
                    "safety_reason": decision["reason"],
                    "requires_backup": decision["requires_backup"],
                    "requires_restore_point": decision["requires_restore_point"],
                    "reversible": decision["reversible"],
                    "can_auto_apply": decision["can_auto_apply"],
                    "user_approval_required": True,
                }
            )
            if decision["status"] == "approved":
                approved.append(guarded)
            elif decision["status"] == "blocked":
                blocked.append(guarded)
            else:
                warnings.append(guarded)

        result = {
            "approved": approved,
            "blocked": blocked,
            "warnings": warnings,
            "summary": {
                "approved_count": len(approved),
                "blocked_count": len(blocked),
                "warning_count": len(warnings),
                "gate": "pass" if approved or warnings else "blocked",
            },
            "models": {
                "safety": os.environ.get("AI_SAFETY_MODEL", self.SAFETY_MODEL),
            },
        }
        logger.info(
            "Safety Guard decision: approved=%s warning=%s blocked=%s",
            len(approved),
            len(warnings),
            len(blocked),
        )
        return result

    def evaluate_recommendation(self, recommendation: Dict[str, Any]) -> Dict[str, Any]:
        tweak_id = (recommendation.get("tweak_id") or recommendation.get("id") or "").strip().lower()
        title = recommendation.get("title") or recommendation.get("name") or ""
        description = recommendation.get("description") or recommendation.get("reason") or ""
        risk_level = (recommendation.get("risk_level") or recommendation.get("risk") or "low").strip().lower()
        text = f"{tweak_id} {title} {description}".lower()

        kb_tweak = self.knowledge_base.find_tweak(tweak_id)
        if kb_tweak:
            risk_level = (kb_tweak.get("risk_level") or risk_level).lower()

        reversible = bool(recommendation.get("reversible", kb_tweak.get("reversible", True)))
        requires_backup = bool(recommendation.get("requires_backup", kb_tweak.get("requires_backup", risk_level in {"low", "medium", "high"})))
        requires_restore = bool(recommendation.get("requires_restore_point", kb_tweak.get("requires_restore_point", risk_level in {"medium", "high"})))

        if tweak_id in self.BLOCKED_TWEAK_IDS or risk_level == "blocked" or any(term in text for term in self.BLOCKED_TERMS):
            return self._decision(
                "blocked",
                "blocked",
                "Blocked by HyperBoostX Safety Guard policy.",
                requires_backup=True,
                requires_restore_point=True,
                reversible=False,
                can_auto_apply=False,
            )

        if not reversible:
            return self._decision(
                "blocked",
                "blocked",
                "Tweak is not reversible, so it cannot be applied by HyperBoostX.",
                requires_backup=True,
                requires_restore_point=True,
                reversible=False,
                can_auto_apply=False,
            )

        if risk_level == "high":
            return self._decision(
                "warning",
                "high",
                "High-risk actions are manual review only and are not auto-applied.",
                requires_backup=True,
                requires_restore_point=True,
                reversible=reversible,
                can_auto_apply=False,
            )

        if risk_level == "medium" and not requires_backup:
            return self._decision(
                "blocked",
                "blocked",
                "Medium-risk tweak does not declare a backup path.",
                requires_backup=True,
                requires_restore_point=requires_restore,
                reversible=reversible,
                can_auto_apply=False,
            )

        can_auto_apply = bool(recommendation.get("can_auto_apply", kb_tweak.get("can_auto_apply", risk_level == "low")))
        if risk_level == "medium":
            can_auto_apply = can_auto_apply and requires_backup

        return self._decision(
            "approved",
            risk_level if risk_level in {"low", "medium"} else "low",
            "Approved after safety validation. User approval is still required.",
            requires_backup=requires_backup,
            requires_restore_point=requires_restore,
            reversible=reversible,
            can_auto_apply=can_auto_apply,
        )

    def assistant_summary(
        self,
        scan_result: Dict[str, Any],
        analysis_result: Dict[str, Any],
        safety_result: Dict[str, Any],
    ) -> Dict[str, Any]:
        logger.info("AI Assistant response requested")
        local_text = self._local_assistant_summary(scan_result, analysis_result, safety_result)
        return {
            "message": local_text,
            "status": self._status_snapshot(scan_result),
            "actions": ["Apply Safe Boost", "Detail", "Skip", "Revert"],
            "models": {
                "assistant": os.environ.get("AI_ASSISTANT_MODEL", self.ASSISTANT_MODEL),
                "analyzer": os.environ.get("AI_ANALYZER_MODEL", self.ANALYZER_MODEL),
                "safety": os.environ.get("AI_SAFETY_MODEL", self.SAFETY_MODEL),
                "embedding": os.environ.get("AI_EMBED_MODEL", self.EMBED_MODEL),
            },
        }

    def optimize_game(self, game_name: str, scan_result: Dict[str, Any] | None = None) -> Dict[str, Any]:
        scan_result = scan_result or {}
        game = self.knowledge_base.find_game(game_name)
        if not game:
            game = {
                "game": game_name or "Unknown game",
                "engine": "Unknown",
                "profile": "general-gaming",
                "dlss_support": False,
                "reflex_support": False,
                "frame_generation_support": False,
                "recommended_low_vram": ["Use lower textures", "cap FPS to a stable target"],
                "recommended_mid_vram": ["Use medium/high textures if VRAM has headroom", "review overlays"],
                "known_issues": ["No internal game profile found yet."],
            }

        nvidia = scan_result.get("nvidia") or {}
        vram = float(nvidia.get("vram_gb") or (scan_result.get("hardware", {}).get("gpu", {}).get("vram_gb") or 0))
        settings = game.get("recommended_low_vram") if vram and vram < 6 else game.get("recommended_mid_vram")
        settings = settings or []
        recommendations = [
            {
                "setting": "Recommended preset",
                "value": "Competitive/Stable" if game.get("profile", "").startswith("latency") else "Balanced",
                "expected_impact": "medium",
                "risk_level": "low",
            },
            {
                "setting": "Texture quality",
                "value": "Lower texture tier" if vram and vram < 6 else "Use higher texture only if VRAM headroom exists",
                "expected_impact": "medium",
                "risk_level": "low",
            },
            {
                "setting": "DLSS",
                "value": self._feature_value(game.get("dlss_support"), nvidia.get("is_rtx"), "Quality/Balanced", "Off or unavailable"),
                "expected_impact": "medium" if game.get("dlss_support") and nvidia.get("is_rtx") else "low",
                "risk_level": "low",
            },
            {
                "setting": "NVIDIA Reflex",
                "value": self._feature_value(game.get("reflex_support"), nvidia.get("is_nvidia"), "On", "Unavailable or game default"),
                "expected_impact": "medium" if game.get("reflex_support") and nvidia.get("is_nvidia") else "low",
                "risk_level": "low",
            },
            {
                "setting": "V-Sync / frame cap",
                "value": "Use frame cap near stable refresh target; avoid guaranteed FPS claims.",
                "expected_impact": "medium",
                "risk_level": "low",
            },
        ]
        return {
            "game": game.get("game"),
            "engine": game.get("engine"),
            "profile": game.get("profile"),
            "recommendations": recommendations,
            "setting_notes": settings,
            "known_issues": game.get("known_issues") or [],
            "risk_level": "low",
            "manual_apply": True,
            "disclaimer": "Results depend on game, driver, hardware, and current system load.",
        }

    def create_report(
        self,
        scan_result: Dict[str, Any],
        analysis_result: Dict[str, Any],
        safety_result: Dict[str, Any],
        apply_result: Dict[str, Any] | None = None,
        reverted_result: Dict[str, Any] | None = None,
    ) -> Dict[str, Any]:
        report_id = f"report-{int(time.time())}"
        report = {
            "report_id": report_id,
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "scan_id": scan_result.get("scan_id"),
            "pc_health_score": (scan_result.get("scores") or {}).get("pc_health_score", 0),
            "gaming_readiness_score": (scan_result.get("scores") or {}).get("gaming_readiness_score", 0),
            "issues": analysis_result.get("issues", []),
            "recommendations": analysis_result.get("recommendations", []),
            "safety": {
                "approved": len(safety_result.get("approved", [])),
                "blocked": len(safety_result.get("blocked", [])),
                "warnings": len(safety_result.get("warnings", [])),
            },
            "applied": apply_result or {},
            "reverted": reverted_result or {},
            "language_policy": "No guaranteed FPS increase. HyperBoostX reports potential stability/latency benefits only.",
        }
        path = self.reports_dir / f"{report_id}.json"
        try:
            path.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
        except Exception as exc:
            logger.error("Failed to write performance report: %s", exc)
        return report

    def load_report(self, report_id: str) -> Dict[str, Any]:
        safe_id = re.sub(r"[^a-zA-Z0-9_-]", "", report_id or "")
        if not safe_id:
            return {}
        path = self.reports_dir / f"{safe_id}.json"
        if not path.exists():
            return {}
        try:
            return json.loads(path.read_text(encoding="utf-8"))
        except Exception:
            return {}

    def _local_analyze(self, scan_id: str, scan_result: Dict[str, Any], user_goal: str) -> Dict[str, Any]:
        hardware = scan_result.get("hardware") or {}
        windows = scan_result.get("windows") or {}
        apps = scan_result.get("apps") or {}
        performance = scan_result.get("performance") or {}
        nvidia = scan_result.get("nvidia") or {}
        scores = scan_result.get("scores") or {}

        issues: List[Dict[str, Any]] = []
        recommendations: List[Dict[str, Any]] = []

        ram_usage = float(performance.get("ram_usage_percent") or 0)
        cpu_usage = float(performance.get("cpu_usage_percent") or 0)
        gpu_usage = float(performance.get("gpu_usage_percent") or 0)
        disk_usage = float(performance.get("disk_usage_percent") or 0)
        startup_high = int(apps.get("startup_high_impact") or 0)
        process_count = int(apps.get("background_process_count") or performance.get("processes") or 0)
        power_plan = str(windows.get("power_plan", "Unknown"))
        game_mode = str(windows.get("game_mode", "Unknown"))

        if "high performance" not in power_plan.lower() and "ultimate" not in power_plan.lower():
            issues.append(self._issue("power_plan_not_optimized", "medium", 0.78, "Power plan is not performance-oriented."))
            recommendations.append(self._rec_from_tweak("optimize_power", "Set performance power plan", "Can help latency and frame pacing when Windows is power-limited.", "medium"))

        if game_mode.lower() in {"off", "disabled"}:
            issues.append(self._issue("game_mode_disabled", "low", 0.72, "Windows Game Mode appears disabled."))
            recommendations.append(self._rec("enable_game_mode", "Enable Windows Game Mode", "Turns on Windows Game Mode for gaming sessions.", "low", "Can help Windows prioritize games.", expected="low"))

        if ram_usage >= 78:
            issues.append(self._issue("ram_pressure", "medium", 0.82, "RAM usage is high and can cause stutter."))
            recommendations.append(self._rec("startup_review", "Review heavy startup apps", "Startup/background apps may be using RAM before gaming.", "low", "Review and disable only non-essential apps with approval.", can_apply=False, expected="medium"))

        if startup_high >= 2 or process_count >= 180:
            issues.append(self._issue("background_apps_heavy", "medium", 0.8, "Heavy startup/background app load detected."))
            recommendations.append(self._rec("background_apps_review", "Close or disable non-essential background apps", "Review non-essential apps instead of force-closing protected tools.", "low", "Can reduce RAM pressure and background spikes.", can_apply=False, expected="medium"))

        if disk_usage >= 85:
            issues.append(self._issue("storage_low_free_space", "medium", 0.76, "System drive free space is low."))
            recommendations.append(self._rec("cleanup_temp_files", "Clean temporary files with approval", "Clean only temporary/cache locations, never personal files.", "low", "Can reduce storage pressure.", can_apply=False, expected="low"))

        if cpu_usage >= 70 and gpu_usage and gpu_usage < 45:
            issues.append(self._issue("possible_cpu_or_engine_limit", "medium", 0.7, "GPU usage is low while CPU usage is high; game engine/CPU/background load may be limiting FPS."))

        if not nvidia.get("is_nvidia"):
            issues.append(self._issue("nvidia_limited_support", "low", 0.9, "NVIDIA GPU was not detected; NVIDIA-specific recommendations are limited."))
        elif nvidia.get("driver_version") in {"", "Unknown", None}:
            issues.append(self._issue("nvidia_driver_unknown", "low", 0.62, "NVIDIA driver version could not be read."))

        if not issues:
            issues.append(self._issue("pc_status_balanced", "low", 0.68, "No major performance bottleneck was detected in the basic scan."))

        rag_context = self.knowledge_base.search(
            " ".join([user_goal, *[issue["issue_type"] for issue in issues], str(hardware.get("gpu", {}).get("name", ""))]),
            limit=6,
        )

        return {
            "scan_id": scan_id,
            "issues": issues,
            "recommendations": self._dedupe_recommendations(recommendations),
            "confidence": round(sum(issue["confidence"] for issue in issues) / max(len(issues), 1), 2),
            "health_score": scores.get("pc_health_score", 0),
            "gaming_readiness_score": scores.get("gaming_readiness_score", 0),
            "rag_context": rag_context,
            "ai_mode": "local_rule_based_fallback",
            "models": {
                "analyzer": os.environ.get("AI_ANALYZER_MODEL", self.ANALYZER_MODEL),
                "embedding": os.environ.get("AI_EMBED_MODEL", self.EMBED_MODEL),
            },
        }

    def _local_assistant_summary(self, scan_result: Dict[str, Any], analysis_result: Dict[str, Any], safety_result: Dict[str, Any]) -> str:
        status = self._status_snapshot(scan_result)
        issues = analysis_result.get("issues", [])[:3]
        approved = safety_result.get("approved", [])[:4]
        blocked = safety_result.get("blocked", [])[:3]
        risk = self._overall_risk(safety_result)

        lines = [
            "Status PC:",
            f"* CPU: {status['cpu']}",
            f"* GPU: {status['gpu']}",
            f"* RAM: {status['ram']}",
            f"* Driver: {status['driver']}",
            f"* Storage: {status['storage']}",
            f"* Power Plan: {status['power_plan']}",
            f"* Background Apps: {status['background_apps']}",
            "",
            "Masalah utama:",
            f"* {issues[0]['reason'] if issues else 'Tidak ada masalah besar yang terdeteksi dari basic scan.'}",
            "",
            "Penyebab kemungkinan:",
        ]
        for index, issue in enumerate(issues, start=1):
            lines.append(f"{index}. {issue.get('reason', issue.get('issue_type', 'Unknown'))}")

        lines.extend(["", "Rekomendasi aman:"])
        if approved:
            for index, item in enumerate(approved, start=1):
                lines.append(f"{index}. {item.get('title', item.get('tweak_id', 'Recommendation'))} - {item.get('risk_level', 'low').upper()}, bisa di-revert: {str(item.get('reversible', True)).lower()}.")
        else:
            lines.append("1. Tidak ada tweak otomatis yang disarankan. Gunakan review manual dan scan ulang setelah kondisi berubah.")

        if blocked:
            lines.extend(["", "Diblokir Safety Guard:"])
            for item in blocked:
                lines.append(f"* {item.get('title', item.get('tweak_id', 'Blocked tweak'))}: {item.get('safety_reason', 'Blocked by policy')}")

        lines.extend(
            [
                "",
                f"Risk level: {risk.upper()}",
                "",
                "Aksi:",
                "* Apply Safe Boost",
                "* Detail",
                "* Skip",
                "* Revert",
                "",
                "Catatan: rekomendasi ini berpotensi membantu stabilitas, stutter, atau latency. HyperBoostX tidak mengklaim FPS pasti naik.",
            ]
        )
        return "\n".join(lines)

    def _try_cloud_json(self, model: str, system_prompt: str, payload: Dict[str, Any]) -> Dict[str, Any]:
        cloud_flag = os.environ.get("AI_CLOUD_ENABLED")
        cloud_enabled = Config.AI_CLOUD_ENABLED if cloud_flag is None else str(cloud_flag).lower() in {"1", "true", "yes", "on"}
        if not cloud_enabled:
            return {}
        api_key = os.environ.get("NVIDIA_API_KEY", "").strip()
        if not api_key or requests is None:
            return {}
        base_url = os.environ.get("NVIDIA_BASE_URL", Config.NVIDIA_BASE_URL).rstrip("/")
        timeout_ms = Config.AI_TIMEOUT_MS
        retries = Config.AI_MAX_RETRIES
        endpoint = Config.NVIDIA_CHAT_ENDPOINT if Config.NVIDIA_CHAT_ENDPOINT.startswith("/") else f"/{Config.NVIDIA_CHAT_ENDPOINT}"
        candidate_models = [model]
        if Config.AI_MODEL_AUTO_FALLBACK and model != Config.NVIDIA_FALLBACK_MODEL:
            candidate_models.append(Config.NVIDIA_FALLBACK_MODEL)

        for candidate_model in candidate_models:
            body = {
                "model": candidate_model,
                "messages": [
                    {"role": "system", "content": system_prompt},
                    {"role": "user", "content": json.dumps(payload, ensure_ascii=False)},
                ],
                "temperature": 0.1,
                "max_tokens": 1800,
            }
            for attempt in range(max(retries, 0) + 1):
                try:
                    response = requests.post(
                        f"{base_url}{endpoint}",
                        headers={"Authorization": f"Bearer {api_key}", "Content-Type": "application/json"},
                        data=json.dumps(body),
                        timeout=max(1, timeout_ms / 1000),
                    )
                    response.raise_for_status()
                    content = response.json()["choices"][0]["message"]["content"]
                    return self._parse_json_object(content)
                except Exception as exc:
                    logger.warning("AI cloud call failed for %s on attempt %s: %s", candidate_model, attempt + 1, type(exc).__name__)
        return {}

    @staticmethod
    def _parse_json_object(text: str) -> Dict[str, Any]:
        if not text:
            return {}
        try:
            return json.loads(text)
        except Exception:
            match = re.search(r"\{.*\}", text, flags=re.DOTALL)
            if not match:
                return {}
            try:
                return json.loads(match.group(0))
            except Exception:
                return {}

    @staticmethod
    def _valid_analysis(payload: Dict[str, Any]) -> bool:
        return isinstance(payload, dict) and isinstance(payload.get("issues"), list) and isinstance(payload.get("recommendations"), list)

    def _normalize_analysis(self, payload: Dict[str, Any], fallback: Dict[str, Any]) -> Dict[str, Any]:
        normalized = dict(fallback)
        normalized["issues"] = payload.get("issues") or fallback.get("issues", [])
        normalized["recommendations"] = self._dedupe_recommendations(payload.get("recommendations") or fallback.get("recommendations", []))
        normalized["confidence"] = float(payload.get("confidence") or fallback.get("confidence") or 0)
        normalized["health_score"] = payload.get("health_score", fallback.get("health_score", 0))
        normalized["gaming_readiness_score"] = payload.get("gaming_readiness_score", fallback.get("gaming_readiness_score", 0))
        normalized["ai_mode"] = "cloud_with_local_guardrails"
        return normalized

    @staticmethod
    def _sanitize_for_cloud(scan_result: Dict[str, Any]) -> Dict[str, Any]:
        allowed = {
            "scan_id",
            "timestamp",
            "hardware",
            "windows",
            "nvidia",
            "apps",
            "performance",
            "scores",
            "privacy",
        }
        return {key: value for key, value in scan_result.items() if key in allowed}

    @staticmethod
    def _issue(issue_type: str, severity: str, confidence: float, reason: str) -> Dict[str, Any]:
        return {
            "issue_type": issue_type,
            "severity": severity,
            "confidence": confidence,
            "reason": reason,
        }

    def _rec_from_tweak(self, tweak_id: str, title: str, description: str, expected: str) -> Dict[str, Any]:
        kb_tweak = self.knowledge_base.find_tweak(tweak_id)
        return self._rec(
            tweak_id=tweak_id,
            title=title or kb_tweak.get("name", tweak_id),
            description=description or kb_tweak.get("description", ""),
            risk=kb_tweak.get("risk_level", "low"),
            reason=kb_tweak.get("notes", description),
            backup=kb_tweak.get("requires_backup", True),
            restore=kb_tweak.get("requires_restore_point", False),
            reversible=kb_tweak.get("reversible", True),
            can_apply=kb_tweak.get("can_auto_apply", True),
            expected=expected,
        )

    @staticmethod
    def _rec(
        tweak_id: str,
        title: str,
        description: str,
        risk: str,
        reason: str,
        backup: bool = True,
        restore: bool = False,
        reversible: bool = True,
        can_apply: bool = True,
        expected: str = "medium",
    ) -> Dict[str, Any]:
        return {
            "tweak_id": tweak_id,
            "title": title,
            "description": description,
            "risk_level": risk,
            "reason": reason,
            "requires_backup": backup,
            "requires_restore_point": restore,
            "reversible": reversible,
            "can_auto_apply": can_apply,
            "user_approval_required": True,
            "expected_impact": expected,
        }

    @staticmethod
    def _dedupe_recommendations(recommendations: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
        seen = set()
        deduped = []
        for item in recommendations:
            key = item.get("tweak_id") or item.get("title") or json.dumps(item, sort_keys=True)
            if key in seen:
                continue
            seen.add(key)
            deduped.append(item)
        return deduped

    @staticmethod
    def _decision(
        status: str,
        risk_level: str,
        reason: str,
        requires_backup: bool,
        requires_restore_point: bool,
        reversible: bool,
        can_auto_apply: bool,
    ) -> Dict[str, Any]:
        return {
            "status": status,
            "risk_level": risk_level,
            "reason": reason,
            "requires_backup": requires_backup,
            "requires_restore_point": requires_restore_point,
            "reversible": reversible,
            "can_auto_apply": can_auto_apply,
        }

    @staticmethod
    def _status_snapshot(scan_result: Dict[str, Any]) -> Dict[str, str]:
        hardware = scan_result.get("hardware") or {}
        windows = scan_result.get("windows") or {}
        apps = scan_result.get("apps") or {}
        cpu = hardware.get("cpu") or {}
        gpu = hardware.get("gpu") or {}
        ram = hardware.get("ram") or {}
        storage = hardware.get("storage") or {}
        return {
            "cpu": f"{cpu.get('name', 'Unknown')} ({cpu.get('usage_percent', 0)}% usage)",
            "gpu": f"{gpu.get('name', 'Unknown')} ({gpu.get('vram_gb', 0)} GB VRAM)",
            "ram": f"{ram.get('total_gb', 0)} GB total, {ram.get('usage_percent', 0)}% used",
            "driver": gpu.get("driver_version", "Unknown"),
            "storage": f"{storage.get('type', 'Unknown')}, {storage.get('free_gb', 0)} GB free",
            "power_plan": windows.get("power_plan", "Unknown"),
            "background_apps": f"{apps.get('background_process_count', 0)} processes, {apps.get('startup_high_impact', 0)} high-impact startup items",
        }

    @staticmethod
    def _overall_risk(safety_result: Dict[str, Any]) -> str:
        levels = [
            str(item.get("risk_level", "low")).lower()
            for item in (safety_result.get("approved") or []) + (safety_result.get("warnings") or [])
        ]
        if "high" in levels:
            return "high"
        if "medium" in levels:
            return "medium"
        return "low"

    @staticmethod
    def _feature_value(game_support: Any, gpu_support: Any, supported: str, unsupported: str) -> str:
        return supported if bool(game_support) and bool(gpu_support) else unsupported


class TripleAIEngine:
    """Compatibility facade used by Flask, WPF, and tests.

    `TripleAiEngine` is the cloud-optional intelligence core. This facade owns
    the product flow and safe tweak/revert bridge expected by the existing app.
    """

    ASSISTANT_MODEL = TripleAiEngine.ASSISTANT_MODEL
    ANALYZER_MODEL = TripleAiEngine.ANALYZER_MODEL
    SAFETY_MODEL = TripleAiEngine.SAFETY_MODEL
    EMBED_MODEL = TripleAiEngine.EMBED_MODEL

    def __init__(
        self,
        knowledge_base: KnowledgeBaseService | None = None,
        system_info_service: Any | None = None,
        monitor_service: Any | None = None,
        startup_service: Any | None = None,
        tweak_service: Any | None = None,
    ):
        self.core = TripleAiEngine(knowledge_base=knowledge_base)
        self.scanner = PcScannerService()
        self.system_info_service = system_info_service
        self.monitor_service = monitor_service
        self.startup_service = startup_service
        self.tweak_service = tweak_service or TweakService()
        self.storage_dir = Config.DATA_DIR / "triple_ai"
        self.storage_dir.mkdir(parents=True, exist_ok=True)

    def scan_pc(self) -> Dict[str, Any]:
        logger.info("Triple AI scan started")
        if self.system_info_service or self.monitor_service or self.startup_service:
            scan = self._scan_from_injected_services()
        else:
            scan = self.scanner.scan_pc()
        scan = self._add_legacy_scan_fields(scan)
        logger.info("Triple AI scan completed: %s", scan.get("scan_id", "unknown"))
        return scan

    def analyze(self, scan_result: Dict[str, Any], user_goal: str = "gaming", game: str = "") -> Dict[str, Any]:
        result = self.core.analyze_scan(
            scan_result.get("scan_id") or f"scan-{uuid.uuid4().hex[:8]}",
            scan_result,
            user_goal or "gaming",
        )
        result.update(
            {
                "engine": "HyperBoostX Triple AI Engine",
                "role": "AI Analyzer",
                "model_target": os.environ.get("AI_ANALYZER_MODEL", self.ANALYZER_MODEL),
                "game": game or "",
            }
        )
        for issue in result.get("issues", []):
            if "description" not in issue and "reason" in issue:
                issue["description"] = issue["reason"]
        return result

    def safety_check(self, recommendations: List[Dict[str, Any]]) -> Dict[str, Any]:
        result = self.core.safety_check(recommendations or [])
        result.update(
            {
                "engine": "HyperBoostX Triple AI Engine",
                "role": "AI Safety Guard",
                "model_target": os.environ.get("AI_SAFETY_MODEL", self.SAFETY_MODEL),
            }
        )
        return result

    def assistant_response(
        self,
        scan_result: Dict[str, Any],
        analysis_result: Dict[str, Any],
        safety_result: Dict[str, Any],
    ) -> Dict[str, Any]:
        result = self.core.assistant_summary(scan_result, analysis_result, safety_result)
        result.update(
            {
                "engine": "HyperBoostX Triple AI Engine",
                "role": "AI Assistant",
                "model_target": os.environ.get("AI_ASSISTANT_MODEL", self.ASSISTANT_MODEL),
                "tagline": "Scan. Analyze. Boost. Revert.",
                "risk_level": self.core._overall_risk(safety_result).title(),
                "status_pc": result.get("status", {}),
                "aksi": result.get("actions", ["Apply Safe Boost", "Detail", "Skip", "Revert"]),
                "blocked_count": len(safety_result.get("blocked") or []),
                "manual_review_count": len(safety_result.get("warnings") or []),
            }
        )
        return result

    def run_full_flow(self, user_goal: str = "gaming", game: str = "") -> Dict[str, Any]:
        scan = self.scan_pc()
        analysis = self.analyze(scan, user_goal=user_goal, game=game)
        safety = self.safety_check(analysis.get("recommendations", []))
        assistant = self.assistant_response(scan, analysis, safety)
        report = self.create_performance_report(scan, analysis, safety, assistant)
        return {
            "scan": scan,
            "analysis": analysis,
            "safety": safety,
            "assistant": assistant,
            "report": report,
        }

    def optimize_game(self, game_name: str, scan_result: Dict[str, Any] | None = None) -> Dict[str, Any]:
        return self.core.optimize_game(game_name, scan_result)

    def _kb_recommendation(self, tweak_id: str) -> Dict[str, Any]:
        return self.core._kb_recommendation(tweak_id)

    def apply_safe_tweaks(self, approved_tweaks: List[Dict[str, Any]], user_approved: bool) -> Dict[str, Any]:
        logger.info("Safe Tweak Engine apply request")
        if not user_approved:
            return {
                "success": False,
                "applied": [],
                "failed": [],
                "backup_id": "",
                "error": "User approval is required before applying tweaks.",
            }

        safety = self.safety_check(approved_tweaks)
        applied: List[Dict[str, Any]] = []
        failed: List[Dict[str, Any]] = []
        backup_id = f"apply-{uuid.uuid4().hex[:12]}"

        for item in safety.get("approved", []):
            tweak_id = item.get("tweak_id")
            if not tweak_id or not item.get("can_auto_apply", False):
                failed.append({"tweak_id": tweak_id or "unknown", "error": "Tweak is not auto-applicable."})
                continue

            result = self.tweak_service.apply_tweak(tweak_id)
            if result.get("success"):
                applied.append({"tweak_id": tweak_id, "result": result})
            else:
                failed.append({"tweak_id": tweak_id, "error": result.get("error", "Apply failed.")})
                if item.get("risk_level") != "low":
                    break

        payload = {
            "success": bool(applied) and not failed,
            "applied": applied,
            "failed": failed,
            "backup_id": backup_id if applied else "",
            "warnings": safety.get("warnings", []),
            "blocked": safety.get("blocked", []),
        }
        self._write_json("applies", backup_id, payload)
        return payload

    def revert_tweaks(self, backup_id: str = "", tweak_ids: List[str] | None = None) -> Dict[str, Any]:
        logger.info("Revert started for backup_id=%s", self._safe_label(backup_id))
        ids = list(tweak_ids or [])
        if not ids and backup_id:
            previous = self._read_json("applies", backup_id)
            ids = [item.get("tweak_id") for item in previous.get("applied", []) if item.get("tweak_id")]

        reverted: List[Dict[str, Any]] = []
        failed: List[Dict[str, Any]] = []
        for tweak_id in ids:
            result = self.tweak_service.revert_tweak(tweak_id)
            if result.get("success"):
                reverted.append({"tweak_id": tweak_id, "result": result})
            else:
                failed.append({"tweak_id": tweak_id, "error": result.get("error", "Revert failed.")})

        return {
            "success": not failed,
            "reverted": reverted,
            "failed": failed,
            "backup_id": backup_id or "",
        }

    def create_performance_report(
        self,
        scan_result: Dict[str, Any],
        analysis_result: Dict[str, Any],
        safety_result: Dict[str, Any],
        assistant_result: Dict[str, Any] | None = None,
    ) -> Dict[str, Any]:
        report = self.core.create_report(scan_result, analysis_result, safety_result)
        if assistant_result:
            report["assistant_summary"] = assistant_result.get("message", "")
        return report

    @staticmethod
    def _cloud_enabled() -> bool:
        cloud_flag = os.environ.get("AI_CLOUD_ENABLED")
        cloud_enabled = Config.AI_CLOUD_ENABLED if cloud_flag is None else str(cloud_flag).lower() in {"1", "true", "yes", "on"}
        return cloud_enabled and requests is not None and bool(os.environ.get("NVIDIA_API_KEY", "").strip())

    def _scan_from_injected_services(self) -> Dict[str, Any]:
        stats = self._safe_call(lambda: self.monitor_service.get_current_stats(), {}) if self.monitor_service else {}
        cpu = self._safe_call(lambda: self.system_info_service.get_cpu_info(), {}) if self.system_info_service else {}
        memory = self._safe_call(lambda: self.system_info_service.get_memory_info(), {}) if self.system_info_service else {}
        disk = self._safe_call(lambda: self.system_info_service.get_disk_info(), {}) if self.system_info_service else {}
        system_drive = self._safe_call(lambda: self.system_info_service.get_system_drive_info(), {}) if self.system_info_service else {}
        os_info = self._safe_call(lambda: self.system_info_service.get_os_info(), {}) if self.system_info_service else {}
        gpu_info = self._safe_call(lambda: self.system_info_service.get_gpu_info(), {}) if self.system_info_service else {}
        startup_items = self._safe_call(lambda: self.startup_service.get_startup_items(), []) if self.startup_service else []
        processes = self._safe_call(lambda: self.monitor_service.get_process_list(limit=15), []) if self.monitor_service else []

        first_gpu = (gpu_info.get("gpus") or [{}])[0]
        live_gpu = stats.get("gpu") or {}
        vram_gb = self._bytes_to_gb(first_gpu.get("vram")) or round(float(live_gpu.get("memory_total_mb") or 0) / 1024, 2)
        disk_first = next(iter(disk.values()), {}) if isinstance(disk, dict) and disk else {}
        scan = {
            "scan_id": f"scan-{datetime.now(timezone.utc).strftime('%Y%m%d%H%M%S')}-{uuid.uuid4().hex[:8]}",
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "hardware": {
                "cpu": {
                    "name": cpu.get("processor", "Unknown"),
                    "cores": cpu.get("cores", 0),
                    "threads": cpu.get("threads", 0),
                    "usage_percent": stats.get("cpu", 0),
                },
                "gpu": {
                    "name": first_gpu.get("name") or live_gpu.get("name") or "Unknown",
                    "driver_version": first_gpu.get("driver_version", "Unknown"),
                    "driver_date": first_gpu.get("driver_date", "Unknown"),
                    "vram_gb": vram_gb,
                    "usage_percent": live_gpu.get("load", 0),
                    "temperature_c": live_gpu.get("temperature", 0),
                },
                "ram": {
                    "total_gb": self._bytes_to_gb(memory.get("total")) or float(stats.get("memory_total_gb") or 0),
                    "available_gb": self._bytes_to_gb(memory.get("available")),
                    "usage_percent": stats.get("memory", memory.get("percent", 0)),
                    "speed_mhz": memory.get("speed_mhz", 0),
                },
                "storage": {
                    "system_drive": system_drive.get("drive_letter", "C"),
                    "type": system_drive.get("storage_class", "Unknown"),
                    "model": system_drive.get("model", "Unknown"),
                    "free_gb": self._bytes_to_gb(disk_first.get("free")),
                    "usage_percent": stats.get("disk", 0),
                },
            },
            "windows": {
                "version": os_info.get("release", "Unknown"),
                "build_number": os_info.get("version", "Unknown"),
                "power_plan": "Unknown",
                "game_mode": "Unknown",
                "hags": "Unknown",
            },
            "nvidia": {
                "is_nvidia": any(token in (first_gpu.get("name") or "").lower() for token in ("nvidia", "geforce", "rtx", "gtx")),
                "is_rtx": "rtx" in (first_gpu.get("name") or "").lower(),
                "gpu_name": first_gpu.get("name") or live_gpu.get("name") or "Unknown",
                "driver_version": first_gpu.get("driver_version", "Unknown"),
                "driver_date": first_gpu.get("driver_date", "Unknown"),
                "vram_gb": vram_gb,
            },
            "apps": {
                "startup_count": len(startup_items),
                "startup_high_impact": sum(1 for item in startup_items if str(item.get("impact", "")).lower() == "high"),
                "background_process_count": stats.get("processes", len(processes)),
                "top_background_apps": processes[:8],
            },
            "performance": {
                "cpu_usage_percent": stats.get("cpu", 0),
                "ram_usage_percent": stats.get("memory", 0),
                "disk_usage_percent": stats.get("disk", 0),
                "gpu_usage_percent": live_gpu.get("load", 0),
                "gpu_temperature_c": live_gpu.get("temperature", 0),
                "processes": stats.get("processes", len(processes)),
            },
            "privacy": {
                "cloud_payload_note": "HyperBoostX only sends sanitized scan payloads when AI Cloud Analysis is enabled.",
                "personal_paths_included": False,
                "api_key_logged": False,
            },
        }
        scan["scores"] = PcScannerService.calculate_scores(scan)
        return scan

    @staticmethod
    def _add_legacy_scan_fields(scan: Dict[str, Any]) -> Dict[str, Any]:
        hardware = scan.setdefault("hardware", {})
        cpu = hardware.get("cpu") or {}
        gpu = hardware.get("gpu") or {}
        ram = hardware.get("ram") or {}
        storage = hardware.get("storage") or {}
        hardware.setdefault("cpu_name", cpu.get("name", "Unknown"))
        hardware.setdefault("gpu_name", gpu.get("name", "Unknown"))
        hardware.setdefault("ram_total_gb", ram.get("total_gb", 0))
        hardware.setdefault("ram_speed_mhz", ram.get("speed_mhz", 0))
        hardware.setdefault("vram_mb", int(float(gpu.get("vram_gb") or 0) * 1024))
        hardware.setdefault("storage_type", storage.get("type", "Unknown"))
        hardware.setdefault("storage_free_gb", storage.get("free_gb", 0))
        return scan

    @staticmethod
    def _safe_call(callback, default):
        try:
            return callback()
        except Exception as exc:
            logger.debug("Triple AI compatibility probe skipped: %s", type(exc).__name__)
            return default

    @staticmethod
    def _bytes_to_gb(value: Any) -> float:
        try:
            return round(float(value or 0) / (1024 ** 3), 2)
        except Exception:
            return 0.0

    @staticmethod
    def _safe_label(value: str) -> str:
        return re.sub(r"[^a-zA-Z0-9_.:-]", "_", str(value or ""))[:80]

    def _write_json(self, folder: str, name: str, payload: Dict[str, Any]) -> None:
        target_dir = self.storage_dir / folder
        target_dir.mkdir(parents=True, exist_ok=True)
        target = target_dir / f"{self._safe_label(name)}.json"
        target.write_text(json.dumps(payload, indent=2, ensure_ascii=False), encoding="utf-8")

    def _read_json(self, folder: str, name: str) -> Dict[str, Any]:
        target = self.storage_dir / folder / f"{self._safe_label(name)}.json"
        if not target.exists():
            return {}
        try:
            return json.loads(target.read_text(encoding="utf-8"))
        except Exception:
            return {}

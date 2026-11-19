"""GPU and background app detection for HyperBoostX v2.0.0."""

from __future__ import annotations

from dataclasses import asdict, dataclass
from typing import Any, Dict, Iterable, List, Optional

import psutil

from core.logger import Logger
from services.monitoring.monitor_service import MonitorService
from services.monitoring.system_info_service import SystemInfoService

logger = Logger.get_logger(__name__)


class GpuVendor:
    NVIDIA = "Nvidia"
    AMD = "Amd"
    INTEL = "Intel"
    MICROSOFT_BASIC = "MicrosoftBasic"
    UNKNOWN = "Unknown"


@dataclass
class GpuDevice:
    vendor: str
    model: str
    family: str
    driver_version: str = "Unknown"
    vram_total_mb: int = 0
    vram_used_mb: int = 0
    vram_usage_percent: float = 0.0
    gpu_usage_percent: float = 0.0
    temperature_c: Optional[float] = None
    active_display: bool = False
    dedicated: bool = False
    integrated: bool = False
    profile_recommendation: str = "Unknown Safe GPU Mode"


class BackgroundAppCatalog:
    """Catalog of vendor, RGB, launcher, streaming, and overlay apps."""

    ITEMS: List[Dict[str, Any]] = [
        {"id": "nvidia_app", "name": "NVIDIA App", "vendor": "NVIDIA", "category": "vendor", "processes": ["nvidia app", "nvidiaapp"], "classification": "Safe to keep"},
        {"id": "geforce_experience", "name": "GeForce Experience", "vendor": "NVIDIA", "category": "vendor", "processes": ["geforce experience", "nvidia geforce experience"], "classification": "Safe to keep"},
        {"id": "nvidia_container", "name": "NVIDIA Container", "vendor": "NVIDIA", "category": "vendor_service", "processes": ["nvcontainer", "nvidia container"], "classification": "Do not disable"},
        {"id": "nvidia_overlay", "name": "NVIDIA Overlay", "vendor": "NVIDIA", "category": "overlay", "processes": ["nvidia share", "nvsphelper", "nvidia overlay"], "classification": "Can pause while gaming"},
        {"id": "shadowplay", "name": "ShadowPlay", "vendor": "NVIDIA", "category": "recording", "processes": ["nvidia share", "shadowplay"], "classification": "Needs user decision"},
        {"id": "nvidia_broadcast", "name": "NVIDIA Broadcast", "vendor": "NVIDIA", "category": "streaming", "processes": ["nvidia broadcast"], "classification": "Needs user decision"},
        {"id": "amd_adrenalin", "name": "AMD Software: Adrenalin Edition", "vendor": "AMD", "category": "vendor", "processes": ["amd software", "radeonsoftware", "cncmd"], "classification": "Safe to keep"},
        {"id": "radeon_software", "name": "Radeon Software", "vendor": "AMD", "category": "vendor", "processes": ["radeonsoftware", "radeon settings"], "classification": "Safe to keep"},
        {"id": "radeon_overlay", "name": "Radeon Overlay", "vendor": "AMD", "category": "overlay", "processes": ["radeonsoftware", "amdrsserv", "radeon overlay"], "classification": "Can pause while gaming"},
        {"id": "radeon_relive", "name": "Radeon ReLive", "vendor": "AMD", "category": "recording", "processes": ["amdrsserv", "relive"], "classification": "Needs user decision"},
        {"id": "intel_arc_control", "name": "Intel Arc Control", "vendor": "Intel", "category": "vendor", "processes": ["arc control", "intelarccontrol"], "classification": "Safe to keep"},
        {"id": "intel_graphics_command_center", "name": "Intel Graphics Command Center", "vendor": "Intel", "category": "vendor", "processes": ["igcc", "graphics command center"], "classification": "Safe to keep"},
        {"id": "intel_arc_overlay", "name": "Intel Arc Overlay", "vendor": "Intel", "category": "overlay", "processes": ["arc control", "intel overlay"], "classification": "Can pause while gaming"},
        {"id": "msi_afterburner", "name": "MSI Afterburner", "vendor": "MSI", "category": "monitoring", "processes": ["msiafterburner"], "classification": "Needs user decision"},
        {"id": "rtss", "name": "RivaTuner Statistics Server", "vendor": "Guru3D", "category": "overlay", "processes": ["rtss", "rtsshooksloader"], "classification": "Can pause while gaming"},
        {"id": "msi_center", "name": "MSI Center", "vendor": "MSI", "category": "vendor_service", "processes": ["msi center", "msicenter"], "classification": "Heavy background service"},
        {"id": "mystic_light", "name": "Mystic Light", "vendor": "MSI", "category": "rgb", "processes": ["mystic light", "ledkeeper"], "classification": "Heavy background service"},
        {"id": "signalrgb", "name": "SignalRGB", "vendor": "WhirlwindFX", "category": "rgb", "processes": ["signalrgb"], "classification": "Can pause while gaming"},
        {"id": "l_connect", "name": "L-Connect", "vendor": "Lian Li", "category": "rgb", "processes": ["l-connect", "lconnect"], "classification": "Needs user decision"},
        {"id": "armoury_crate", "name": "Armoury Crate", "vendor": "ASUS", "category": "vendor_service", "processes": ["armoury crate", "armourycrate", "asus_framework"], "classification": "Heavy background service"},
        {"id": "icue", "name": "iCUE", "vendor": "Corsair", "category": "rgb", "processes": ["icue", "corsair.service"], "classification": "Needs user decision"},
        {"id": "razer_synapse", "name": "Razer Synapse", "vendor": "Razer", "category": "rgb", "processes": ["razer synapse", "razer synapse service"], "classification": "Needs user decision"},
        {"id": "logitech_g_hub", "name": "Logitech G Hub", "vendor": "Logitech", "category": "vendor", "processes": ["lghub", "logitech g hub"], "classification": "Needs user decision"},
        {"id": "wallpaper_engine", "name": "Wallpaper Engine", "vendor": "Wallpaper Engine", "category": "visual", "processes": ["wallpaper32", "wallpaper64", "wallpaper engine"], "classification": "Can pause while gaming"},
        {"id": "discord", "name": "Discord", "vendor": "Discord", "category": "chat", "processes": ["discord"], "classification": "Safe to keep"},
        {"id": "discord_overlay", "name": "Discord Overlay", "vendor": "Discord", "category": "overlay", "processes": ["discordhookhelper", "discord overlay"], "classification": "Can pause while gaming"},
        {"id": "steam_overlay", "name": "Steam Overlay", "vendor": "Valve", "category": "overlay", "processes": ["gameoverlayui", "steam overlay"], "classification": "Can pause while gaming"},
        {"id": "steam_webhelper", "name": "Steam WebHelper", "vendor": "Valve", "category": "launcher", "processes": ["steamwebhelper"], "classification": "Needs user decision"},
        {"id": "xbox_game_bar", "name": "Xbox Game Bar", "vendor": "Microsoft", "category": "overlay", "processes": ["gamebar", "gamebarpresencewriter"], "classification": "Can pause while gaming"},
        {"id": "obs", "name": "OBS", "vendor": "OBS", "category": "streaming", "processes": ["obs64", "obs32", "obs"], "classification": "Safe to keep"},
        {"id": "tiktok_live_studio", "name": "TikTok LIVE Studio", "vendor": "TikTok", "category": "streaming", "processes": ["tiktok live studio"], "classification": "Safe to keep"},
        {"id": "epic_games_launcher", "name": "Epic Games Launcher", "vendor": "Epic", "category": "launcher", "processes": ["epicgameslauncher"], "classification": "Needs user decision"},
        {"id": "riot_client", "name": "Riot Client", "vendor": "Riot", "category": "launcher", "processes": ["riotclientservices", "riot client"], "classification": "Do not disable"},
        {"id": "battle_net", "name": "Battle.net", "vendor": "Blizzard", "category": "launcher", "processes": ["battle.net", "agent"], "classification": "Needs user decision"},
        {"id": "ea_app", "name": "EA App", "vendor": "EA", "category": "launcher", "processes": ["eadesktop", "ea app"], "classification": "Needs user decision"},
        {"id": "ubisoft_connect", "name": "Ubisoft Connect", "vendor": "Ubisoft", "category": "launcher", "processes": ["ubisoftconnect", "upc"], "classification": "Needs user decision"},
    ]


class GpuDetectionService:
    """Detect GPUs, profiles, vendor software, and overlays safely."""

    @staticmethod
    def classify_vendor(model: str) -> str:
        text = (model or "").lower()
        if "microsoft basic display" in text:
            return GpuVendor.MICROSOFT_BASIC
        if any(token in text for token in ("nvidia", "geforce", "rtx", "gtx")):
            return GpuVendor.NVIDIA
        if any(token in text for token in ("amd", "radeon", " radeon", "rx ", "vega")):
            return GpuVendor.AMD
        if any(token in text for token in ("intel", "arc", "iris xe", "uhd graphics", "hd graphics")):
            return GpuVendor.INTEL
        return GpuVendor.UNKNOWN

    @staticmethod
    def classify_family(model: str, vendor: str) -> str:
        text = (model or "").lower()
        if vendor == GpuVendor.NVIDIA:
            if "rtx" in text:
                return "NVIDIA GeForce RTX"
            if "gtx" in text:
                return "NVIDIA GeForce GTX"
            return "NVIDIA GeForce"
        if vendor == GpuVendor.AMD:
            if "rx" in text:
                return "AMD Radeon RX"
            if "vega" in text:
                return "AMD Radeon Vega"
            if "integrated" in text or "apu" in text:
                return "AMD Radeon integrated graphics"
            return "AMD Radeon"
        if vendor == GpuVendor.INTEL:
            if "arc" in text:
                return "Intel Arc"
            if "iris xe" in text:
                return "Intel Iris Xe"
            if "uhd" in text:
                return "Intel UHD Graphics"
            return "Intel iGPU"
        if vendor == GpuVendor.MICROSOFT_BASIC:
            return "Microsoft Basic Display Adapter"
        return "Unknown GPU"

    @staticmethod
    def _to_mb(value: Any) -> int:
        try:
            number = int(value or 0)
        except Exception:
            return 0
        if number <= 0:
            return 0
        return int(number / (1024 * 1024)) if number > 1024 * 1024 else number

    @staticmethod
    def _profile_for(model: str, vendor: str, family: str, vram_mb: int, hybrid: bool) -> str:
        text = (model or "").lower()
        if hybrid:
            return "Laptop Hybrid Graphics Mode"
        if vram_mb and vram_mb < 4096:
            return "Low VRAM Mode"
        if vram_mb >= 12288:
            return "High VRAM Mode"
        if vendor == GpuVendor.NVIDIA:
            if "rtx" in text or "gtx" in text:
                return "NVIDIA Gaming Profile"
            return "NVIDIA Creator Profile"
        if vendor == GpuVendor.AMD:
            if "rx" in text:
                return "AMD Radeon Gaming Profile"
            return "AMD Radeon Creator Profile"
        if vendor == GpuVendor.INTEL:
            if "arc" in text:
                return "Intel Arc Gaming Profile"
            if "iris" in text or "uhd" in text:
                return "Intel iGPU Safe Mode"
            return "Integrated Graphics Safe Mode"
        if vendor in {GpuVendor.MICROSOFT_BASIC, GpuVendor.UNKNOWN}:
            return "Unknown Safe GPU Mode"
        return "Balanced GPU Mode"

    @staticmethod
    def _is_integrated(model: str, vendor: str) -> bool:
        text = (model or "").lower()
        if vendor == GpuVendor.INTEL and "arc" not in text:
            return True
        return any(token in text for token in ("integrated", "igpu", "iris xe", "uhd graphics", "apu"))

    @staticmethod
    def _is_dedicated(model: str, vendor: str) -> bool:
        text = (model or "").lower()
        if vendor == GpuVendor.NVIDIA:
            return True
        if vendor == GpuVendor.AMD and any(token in text for token in ("rx", "radeon", "vega")):
            return "integrated" not in text and "apu" not in text
        if vendor == GpuVendor.INTEL and "arc" in text:
            return True
        return False

    @classmethod
    def detect_gpus(cls, raw_controllers: Optional[Iterable[Dict[str, Any]]] = None) -> List[Dict[str, Any]]:
        if raw_controllers is None:
            raw_controllers = SystemInfoService.get_gpu_info().get("gpus", [])

        raw_list = list(raw_controllers or [])
        gpu_stats = MonitorService.get_gpu_stats()
        gpus: List[GpuDevice] = []
        has_dedicated = False
        has_integrated = False

        for index, raw in enumerate(raw_list):
            model = str(raw.get("name") or raw.get("Name") or raw.get("model") or "Unknown GPU")
            vendor = cls.classify_vendor(model)
            family = cls.classify_family(model, vendor)
            vram_total = cls._to_mb(raw.get("vram") or raw.get("AdapterRAM") or raw.get("vram_total_mb"))
            active = bool(raw.get("active_display")) or bool(raw.get("current_hz") or raw.get("CurrentRefreshRate")) or index == 0
            integrated = cls._is_integrated(model, vendor)
            dedicated = cls._is_dedicated(model, vendor)
            has_dedicated = has_dedicated or dedicated
            has_integrated = has_integrated or integrated

            stats_match = not gpus and gpu_stats
            vram_used = int(gpu_stats.get("memory_used_mb") or 0) if stats_match else 0
            usage = float(gpu_stats.get("load") or 0) if stats_match else 0.0
            vram_percent = float(gpu_stats.get("memory_percent") or 0) if stats_match else 0.0
            temp = gpu_stats.get("temperature") if stats_match else None

            gpus.append(GpuDevice(
                vendor=vendor,
                model=model,
                family=family,
                driver_version=str(raw.get("driver_version") or raw.get("DriverVersion") or "Unknown"),
                vram_total_mb=vram_total,
                vram_used_mb=vram_used,
                vram_usage_percent=vram_percent,
                gpu_usage_percent=usage,
                temperature_c=temp,
                active_display=active,
                dedicated=dedicated,
                integrated=integrated,
            ))

        if not gpus and gpu_stats:
            model = str(gpu_stats.get("name") or "Unknown GPU")
            vendor = cls.classify_vendor(model)
            family = cls.classify_family(model, vendor)
            gpus.append(GpuDevice(
                vendor=vendor,
                model=model,
                family=family,
                vram_total_mb=int(gpu_stats.get("memory_total_mb") or 0),
                vram_used_mb=int(gpu_stats.get("memory_used_mb") or 0),
                vram_usage_percent=float(gpu_stats.get("memory_percent") or 0),
                gpu_usage_percent=float(gpu_stats.get("load") or 0),
                temperature_c=gpu_stats.get("temperature"),
                active_display=True,
                dedicated=cls._is_dedicated(model, vendor),
                integrated=cls._is_integrated(model, vendor),
            ))

        if not gpus:
            gpus.append(GpuDevice(
                vendor=GpuVendor.UNKNOWN,
                model="Unknown GPU",
                family="Unknown GPU",
                active_display=True,
                profile_recommendation="Unknown Safe GPU Mode",
            ))

        hybrid = has_dedicated and has_integrated
        output = []
        for gpu in gpus:
            gpu.profile_recommendation = cls._profile_for(gpu.model, gpu.vendor, gpu.family, gpu.vram_total_mb, hybrid)
            output.append(asdict(gpu))
        return output

    @staticmethod
    def _badge_for_vendor(vendor: str) -> Dict[str, str]:
        if vendor == GpuVendor.NVIDIA:
            return {"label": "NVIDIA GeForce", "accent": "NVIDIA Green", "hex": "#76B900"}
        if vendor == GpuVendor.AMD:
            return {"label": "AMD Radeon", "accent": "Radeon Red", "hex": "#ED1C24"}
        if vendor == GpuVendor.INTEL:
            return {"label": "Intel", "accent": "Intel Blue", "hex": "#0071C5"}
        if vendor == GpuVendor.MICROSOFT_BASIC:
            return {"label": "Generic GPU", "accent": "Minimal Dark", "hex": "#64748B"}
        return {"label": "Generic GPU", "accent": "Hyper Dark", "hex": "#38BDF8"}

    @classmethod
    def get_gpu_summary(cls, raw_controllers: Optional[Iterable[Dict[str, Any]]] = None) -> Dict[str, Any]:
        gpus = cls.detect_gpus(raw_controllers)
        active = next((gpu for gpu in gpus if gpu.get("active_display")), gpus[0])
        dedicated = [gpu for gpu in gpus if gpu.get("dedicated")]
        integrated = [gpu for gpu in gpus if gpu.get("integrated")]
        hybrid = bool(dedicated and integrated)
        badge = cls._badge_for_vendor(active.get("vendor", GpuVendor.UNKNOWN))

        safe_actions = [
            "Review overlays before gaming; pause only with user approval.",
            "Keep GPU driver services enabled.",
            "Export GPU report before applying profile changes.",
        ]
        blocked = [
            "No overclock, undervolt, voltage, BIOS/UEFI, or forced driver-service disable action is allowed.",
        ]

        return {
            "vendor": active.get("vendor", GpuVendor.UNKNOWN),
            "model": active.get("model", "Unknown GPU"),
            "family": active.get("family", "Unknown GPU"),
            "active_display_gpu": active.get("model", "Unknown GPU"),
            "driver_version": active.get("driver_version", "Unknown"),
            "vram_total_mb": active.get("vram_total_mb", 0),
            "vram_used_mb": active.get("vram_used_mb", 0),
            "vram_usage_percent": active.get("vram_usage_percent", 0),
            "gpu_usage_percent": active.get("gpu_usage_percent", 0),
            "temperature_c": active.get("temperature_c"),
            "dedicated_gpu": bool(dedicated),
            "integrated_gpu": bool(integrated),
            "hybrid_gpu_system": hybrid,
            "multi_gpu_system": len(gpus) > 1,
            "gpus": gpus,
            "badge": badge,
            "profile_recommendation": active.get("profile_recommendation", "Unknown Safe GPU Mode"),
            "safe_actions": safe_actions,
            "skipped_actions": ["Vendor-control changes require explicit approval."],
            "blocked_risky_actions": blocked,
            "recommendation": "Use the detected vendor profile as a safe preset, then approve any overlay pause manually.",
        }

    @staticmethod
    def _running_process_names(process_names: Optional[Iterable[str]] = None) -> List[str]:
        if process_names is not None:
            return [str(name).lower() for name in process_names]

        names: List[str] = []
        try:
            for proc in psutil.process_iter(["name"]):
                name = (proc.info.get("name") or "").lower()
                if name:
                    names.append(name)
        except Exception as e:
            logger.debug(f"Process detection fallback: {type(e).__name__}")
        return names

    @classmethod
    def detect_background_apps(cls, process_names: Optional[Iterable[str]] = None, category: Optional[str] = None) -> List[Dict[str, Any]]:
        running = cls._running_process_names(process_names)
        output: List[Dict[str, Any]] = []
        for item in BackgroundAppCatalog.ITEMS:
            if category and item["category"] != category:
                continue
            detected_processes = []
            for needle in item["processes"]:
                needle_lower = needle.lower()
                if any(needle_lower in proc for proc in running):
                    detected_processes.append(needle)
            detected = bool(detected_processes)
            output.append({
                "id": item["id"],
                "name": item["name"],
                "vendor": item["vendor"],
                "category": item["category"],
                "classification": item["classification"],
                "detected": detected,
                "status": "Detected" if detected else "Not detected",
                "matched_processes": detected_processes,
                "safe_action": "Keep enabled" if item["classification"] in {"Safe to keep", "Do not disable"} else "Ask before pausing",
            })
        return output

    @classmethod
    def detect_vendor_software(cls, process_names: Optional[Iterable[str]] = None) -> List[Dict[str, Any]]:
        return [item for item in cls.detect_background_apps(process_names) if item["category"] != "overlay"]

    @classmethod
    def detect_overlays(cls, process_names: Optional[Iterable[str]] = None) -> List[Dict[str, Any]]:
        return [item for item in cls.detect_background_apps(process_names) if item["category"] == "overlay"]

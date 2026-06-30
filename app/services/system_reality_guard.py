"""System Reality Guard diagnostics for HyperBoostX v2.10.

This module is intentionally conservative. It detects, classifies, previews,
and blocks unsafe actions; it does not patch vendor apps, disable Defender,
touch BIOS/voltage settings, or remove required LCD helper files.
"""

from __future__ import annotations

import os
import platform
import subprocess
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional

import psutil

from services.product_features import LocalJsonStore, ProtectionService


def _utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def _run(args: List[str], timeout: float = 6.0) -> Dict[str, Any]:
    try:
        completed = subprocess.run(
            args,
            text=True,
            capture_output=True,
            encoding="utf-8",
            errors="ignore",
            timeout=timeout,
            shell=False,
        )
        return {
            "exit_code": completed.returncode,
            "stdout": completed.stdout.strip(),
            "stderr": completed.stderr.strip(),
            "success": completed.returncode == 0,
        }
    except FileNotFoundError as exc:
        return {"exit_code": None, "stdout": "", "stderr": str(exc), "success": False, "unavailable": True}
    except subprocess.TimeoutExpired as exc:
        return {"exit_code": None, "stdout": exc.stdout or "", "stderr": "Command timed out.", "success": False, "timeout": True}


@dataclass
class HelperProcess:
    process_name: str
    pid: int
    path: Optional[str]
    role: str
    cpu: float
    memory_mb: float
    required: bool = False


class RealitySafetyGuard:
    """Fine-grained Safety Guard rules for System Reality actions."""

    BLOCKED_ACTIONS = {
        "force_disable_defender": "Permanent Defender disable is blocked.",
        "kill_msmpeng": "Killing Defender is blocked.",
        "full_drive_exclusion": "Full-drive Defender exclusion is blocked.",
        "broad_user_folder_exclusion": "Broad user-folder Defender exclusion is blocked.",
        "permanent_windows_update_disable": "Permanent Windows Update disable is blocked.",
        "kill_required_lcd_app": "Required LCD app cannot be killed automatically.",
        "disable_required_lcd_startup": "Required LCD startup cannot be disabled.",
        "patch_vendor_binary": "Patching vendor binaries is blocked.",
        "inject_vendor_process": "Injecting vendor processes is blocked.",
        "redistribute_vendor_files": "Redistributing vendor files is blocked.",
        "delete_vendor_helper_files": "Deleting vendor helper files is blocked.",
        "hidden_vendor_api_call": "Hidden/private vendor API calls are blocked.",
        "unsafe_vendor_config_edit": "Unsafe vendor config edits are blocked.",
        "disable_gpu_driver_service": "GPU driver service disable is blocked.",
        "disable_audio_service": "Audio service disable is blocked.",
        "disable_network_service": "Network service disable is blocked.",
        "disable_fan_control_service": "Fan-control service disable is blocked.",
        "destructive_user_file_cleanup": "Destructive user-file cleanup is blocked.",
        "bios_auto_change": "Automatic BIOS changes are blocked.",
        "voltage_change": "Voltage changes are blocked.",
        "overclock_apply": "Overclock apply is blocked.",
        "undervolt_apply": "Undervolt apply is blocked.",
        "disable_thermal_protection": "Disabling thermal protections is blocked.",
        "arbitrary_ai_shell_execution": "Arbitrary AI shell execution is blocked.",
    }

    BROAD_EXCLUSION_TOKENS = (
        "c:\\",
        "d:\\",
        "f:\\",
        "c:\\users",
        "\\desktop",
        "\\documents",
        "\\downloads",
        "\\appdata",
        "\\temp",
    )

    @classmethod
    def evaluate_action(cls, action_type: str, payload: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        payload = payload or {}
        normalized = (action_type or "").strip().lower()
        if normalized in cls.BLOCKED_ACTIONS:
            return {"allowed": False, "status": "blocked", "blocked_reason": cls.BLOCKED_ACTIONS[normalized]}

        if normalized == "disable_startup" and payload.get("required_for_lcd"):
            return {"allowed": False, "status": "blocked", "blocked_reason": "This app is required for LCD wallpaper."}

        if normalized == "defender_exclusion":
            raw_path = str(payload.get("path") or "").strip()
            path = raw_path.lower()
            if not raw_path:
                return {"allowed": False, "status": "blocked", "blocked_reason": "A specific folder path is required."}
            if path.endswith(":\\") or any(token in path for token in cls.BROAD_EXCLUSION_TOKENS):
                return {"allowed": False, "status": "blocked", "blocked_reason": "Broad Defender exclusion blocked."}
            return {"allowed": True, "status": "preview_required", "warning": "Only specific trusted project folders may be reviewed."}

        return {"allowed": True, "status": "preview_required"}


class SystemRealityGuardService:
    """Safe diagnostics for LCD, Defender, CPU turbo, MSI, and reality audit."""

    LCD_APPS = {
        "kanali.exe": {"vendor": "KANALI", "role": "primary_sensor_display", "required": False},
        "trcc.exe": {"vendor": "TRCC", "role": "live_wallpaper_display", "required": True},
        "himos.exe": {"vendor": "HiMOS", "role": "live_wallpaper_display", "required": True},
    }
    TRCC_HELPERS = {
        "trcc.exe": "main_lcd_app",
        "ffmpeg.exe": "live_wallpaper_decoder",
        "hwinfo.exe": "sensor_helper",
        "hwinfo64.exe": "sensor_helper",
        "usb_lcd.exe": "lcd_transport",
        "usblcdnew.exe": "lcd_transport_new",
        "dotnet.exe": "runtime_dependency",
    }
    DLL_HELPERS = {
        "hwinfo64.dll": "sensor_library",
        "newtonsoft.json.dll": "app_dependency",
        "usbhid.dll": "usb_hid_dependency",
    }
    MSI_NAMES = ("msi center", "msicenter", "msi companion", "dragon center", "msi.service")

    @classmethod
    def response(
        cls,
        *,
        status: str = "normal",
        data: Optional[Dict[str, Any]] = None,
        recommendations: Optional[Iterable[str]] = None,
        blocked_reasons: Optional[Iterable[str]] = None,
        requires_admin: bool = False,
        rollback: Optional[Dict[str, Any]] = None,
        logs: Optional[Iterable[str]] = None,
        ok: Optional[bool] = None,
    ) -> Dict[str, Any]:
        blocked = status == "blocked"
        return {
            "ok": (not blocked) if ok is None else bool(ok),
            "status": status,
            "data": data or {},
            "recommendations": list(recommendations or []),
            "blocked_reasons": list(blocked_reasons or []),
            "requires_admin": requires_admin,
            "rollback": rollback or {},
            "logs": list(logs or []),
        }

    @classmethod
    def _process_rows(cls) -> List[HelperProcess]:
        rows: List[HelperProcess] = []
        for proc in psutil.process_iter(["pid", "name", "exe", "memory_info"]):
            try:
                name = (proc.info.get("name") or "").strip()
                normalized = name.lower()
                metadata = cls.LCD_APPS.get(normalized)
                role = cls.TRCC_HELPERS.get(normalized) or (metadata or {}).get("role")
                if not role:
                    continue
                cpu = float(proc.cpu_percent(interval=0.02) or 0.0)
                memory = proc.info.get("memory_info")
                rows.append(HelperProcess(
                    process_name=name,
                    pid=int(proc.info.get("pid") or 0),
                    path=proc.info.get("exe"),
                    role=role,
                    cpu=round(cpu, 2),
                    memory_mb=round((getattr(memory, "rss", 0) or 0) / 1024 / 1024, 1),
                    required=bool((metadata or {}).get("required")),
                ))
            except (psutil.NoSuchProcess, psutil.AccessDenied):
                continue
        return rows

    @classmethod
    def _status_for_cpu(cls, name: str, cpu: float) -> str:
        lower = name.lower()
        if lower.startswith("kanali"):
            return "critical" if cpu >= 10 else "warning" if cpu >= 5 else "normal"
        if lower.startswith(("trcc", "himos")):
            return "critical" if cpu >= 10 else "warning" if cpu >= 3 else "normal"
        if lower == "ffmpeg.exe":
            return "warning" if cpu >= 5 else "normal"
        if lower.startswith("hwinfo"):
            return "warning" if cpu >= 3 else "normal"
        if lower in {"usb_lcd.exe", "usblcdnew.exe"}:
            return "warning" if cpu >= 3 else "normal"
        return "normal"

    @classmethod
    def lcd_apps(cls) -> Dict[str, Any]:
        rows = [asdict(item) for item in cls._process_rows()]
        for item in rows:
            item["status"] = cls._status_for_cpu(item["process_name"], float(item.get("cpu") or 0))
        detected = {item["process_name"].lower() for item in rows}
        return cls.response(
            status="warning" if any(item["status"] != "normal" for item in rows) else "normal",
            data={
                "apps": rows,
                "detected_vendors": sorted({cls.LCD_APPS[name]["vendor"] for name in detected if name in cls.LCD_APPS}),
                "bridge_mode": "detect_monitor_only",
                "hybrid_mode": "preview_duplicate_work_reduction_only",
                "native_mode": "compatibility_gated",
            },
            recommendations=[
                "Bridge Mode does not guarantee CPU reduction because vendor apps still render, poll, and transport.",
                "Hybrid Mode should reduce duplicate work only where safe and measurable.",
                "Do not kill TRCC/HiMOS automatically when required for LCD wallpaper.",
            ],
        )

    @classmethod
    def lcd_roles(cls, payload: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        if payload:
            role = str(payload.get("role") or "").strip()
            vendor = str(payload.get("vendor") or "").strip()
            if not vendor or not role:
                return cls.response(status="blocked", blocked_reasons=["Vendor and role are required."], ok=False)
            roles = LocalJsonStore.load("config/lcd_roles.json", {}, dict)
            roles[vendor] = {"role": role, "updated_at": _utc_now(), "protected_required": bool(payload.get("protected_required"))}
            LocalJsonStore.save("config/lcd_roles.json", roles)
        defaults = {
            "KANALI": {"role": "primary_sensor_display", "protected_required": False},
            "TRCC": {"role": "live_wallpaper_display", "protected_required": True},
            "HiMOS": {"role": "live_wallpaper_display", "protected_required": True},
        }
        stored = LocalJsonStore.load("config/lcd_roles.json", {}, dict)
        defaults.update(stored)
        return cls.response(data={"roles": defaults})

    @classmethod
    def lcd_vendor_status(cls, vendor: str) -> Dict[str, Any]:
        vendor_norm = vendor.lower()
        rows = [asdict(row) for row in cls._process_rows() if vendor_norm in row.process_name.lower()]
        return cls.response(
            status="normal" if rows else "manual_review",
            data={"vendor": vendor, "running": bool(rows), "processes": rows},
            recommendations=["Open the vendor app manually if it is required for your LCD wallpaper."],
        )

    @classmethod
    def trcc_helpers(cls) -> Dict[str, Any]:
        rows = [asdict(row) for row in cls._process_rows() if row.process_name.lower() in cls.TRCC_HELPERS]
        roles = {row["process_name"].lower(): row["role"] for row in rows}
        return cls.response(
            status="warning" if any(row["process_name"].lower() == "ffmpeg.exe" and row["cpu"] >= 5 for row in rows) else "normal",
            data={
                "helpers": rows,
                "wallpaper_active": "ffmpeg.exe" in roles,
                "sensor_helper_active": any(name.startswith("hwinfo") for name in roles),
                "lcd_transport_active": any(name in {"usb_lcd.exe", "usblcdnew.exe"} for name in roles),
            },
            recommendations=["If ffmpeg is high, analyze wallpaper weight before changing vendor app startup."],
        )

    @classmethod
    def open_vendor(cls, vendor: str, payload: Dict[str, Any]) -> Dict[str, Any]:
        if not bool(payload.get("user_approved")):
            return cls.response(status="preview", data={"vendor": vendor}, recommendations=["Opening vendor apps requires user approval."])
        status = cls.lcd_vendor_status(vendor)
        rows = status["data"].get("processes", [])
        if rows:
            return cls.response(status="manual_review", data={"vendor": vendor, "already_running": True}, recommendations=["Vendor app is already running."])
        return cls.response(status="manual_review", data={"vendor": vendor}, recommendations=["Open this vendor app from Start Menu or its official installer path. HyperBoostX does not bundle vendor binaries."])

    @classmethod
    def restart_preview(cls, vendor: str = "TRCC") -> Dict[str, Any]:
        decision = RealitySafetyGuard.evaluate_action("kill_required_lcd_app", {"vendor": vendor, "required_for_lcd": True})
        return cls.response(
            status="blocked",
            data={"vendor": vendor, "preview_only": True},
            blocked_reasons=[decision["blocked_reason"]],
            recommendations=["Use vendor UI restart controls or close the LCD wallpaper manually if needed."],
            ok=False,
        )

    @classmethod
    def protect_vendor(cls, payload: Dict[str, Any]) -> Dict[str, Any]:
        vendor = str(payload.get("vendor") or payload.get("name") or "").strip()
        if not vendor:
            return cls.response(status="blocked", blocked_reasons=["Vendor name is required."], ok=False)
        ProtectionService.add({"name": f"{vendor}.exe"})
        return cls.response(status="normal", data={"protected": vendor}, recommendations=["Protected apps are blocked from automatic kill/disable actions."])

    @classmethod
    def wallpaper_analyze(cls, payload: Dict[str, Any]) -> Dict[str, Any]:
        path = str(payload.get("path") or payload.get("file") or "").strip()
        if not path:
            return cls.response(status="preview", data={"path_required": True}, recommendations=["Select a wallpaper file to classify weight."])
        file_path = Path(path)
        ext = file_path.suffix.lower()
        size_mb = file_path.stat().st_size / 1024 / 1024 if file_path.exists() else None
        heavy = ext in {".gif", ".webm", ".mp4", ".mov"} and (size_mb is None or size_mb >= 50)
        if ext == ".gif":
            verdict = "heavy"
        elif ext in {".mp4", ".webm", ".mov"} and size_mb and size_mb < 50:
            verdict = "normal"
        else:
            verdict = "manual_review" if heavy else "normal"
        return cls.response(
            status="warning" if verdict == "heavy" else "normal",
            data={"path": path, "exists": file_path.exists(), "extension": ext, "size_mb": round(size_mb, 1) if size_mb is not None else None, "verdict": verdict},
            recommendations=["Prefer H.264 30fps video or static images for low CPU. Audio tracks may increase decode work."],
        )

    @classmethod
    def wallpaper_convert_preview(cls, payload: Dict[str, Any], apply: bool = False) -> Dict[str, Any]:
        decision = RealitySafetyGuard.evaluate_action("patch_vendor_binary", payload)
        if apply:
            return cls.response(status="blocked", blocked_reasons=[decision["blocked_reason"]], ok=False, recommendations=["HyperBoostX does not patch vendor wallpaper engines. Export a new optimized file manually."])
        return cls.response(status="preview", data={"output_new_file_only": True}, recommendations=["Conversion preview is allowed only as new-output guidance; source/vendor files are untouched."])

    @classmethod
    def hybrid_preview(cls, apply: bool = False) -> Dict[str, Any]:
        data = cls.lcd_apps()["data"]
        recommendations = [
            "Prefer KANALI as primary sensor source if already installed.",
            "Keep TRCC/HiMOS protected when required for wallpaper.",
            "Reduce duplicate sensor polling only after before/after measurement.",
        ]
        if apply:
            return cls.response(status="manual_review", data=data, recommendations=recommendations, rollback={"note": "No automatic vendor config edits were made."})
        return cls.response(status="preview", data=data, recommendations=recommendations)

    @classmethod
    def native_compatibility(cls) -> Dict[str, Any]:
        return cls.response(
            status="unsupported",
            data={"native_engine_available": False, "requires_device_protocol": True},
            recommendations=["Native Mode is compatibility-gated and must not be claimed unless a supported device protocol exists."],
        )

    @classmethod
    def safe_mode_preview(cls, apply: bool = False) -> Dict[str, Any]:
        if apply:
            return cls.response(status="manual_review", data={"applied": False}, recommendations=["Safe Mode requires explicit owner review per LCD app; automatic startup disable is blocked for required apps."])
        return cls.response(status="preview", data={"would_protect_required_apps": True, "would_not_kill_vendor_apps": True})

    @classmethod
    def defender_status(cls) -> Dict[str, Any]:
        msmpeng = []
        for proc in psutil.process_iter(["pid", "name", "exe", "memory_info"]):
            try:
                name = proc.info.get("name") or ""
                if name.lower() != "msmpeng.exe":
                    continue
                msmpeng.append({
                    "pid": proc.info.get("pid"),
                    "cpu": round(float(proc.cpu_percent(interval=0.02) or 0), 2),
                    "memory_mb": round((getattr(proc.info.get("memory_info"), "rss", 0) or 0) / 1024 / 1024, 1),
                    "path": proc.info.get("exe"),
                })
            except (psutil.NoSuchProcess, psutil.AccessDenied):
                continue
        mp_status = _run(["powershell", "-NoProfile", "-Command", "Get-MpComputerStatus | ConvertTo-Json -Compress"], timeout=4.0) if platform.system() == "Windows" else {"success": False, "stderr": "Windows only"}
        return cls.response(
            status="warning" if any(item["cpu"] >= 50 for item in msmpeng) else "normal",
            data={"msmpeng": msmpeng, "defender_status_raw": mp_status},
            recommendations=["Defender should not be disabled permanently. High CPU during full scan can be normal and temporary."],
        )

    @classmethod
    def defender_performance(cls, action: str) -> Dict[str, Any]:
        samples = LocalJsonStore.load("diagnostics/defender_performance.json", [], list)
        if action == "start":
            sample = {"started_at": _utc_now(), "status": "sampling_started", "defender": cls.defender_status()["data"]}
            samples.append(sample)
            LocalJsonStore.save("diagnostics/defender_performance.json", samples[-20:])
            return cls.response(status="normal", data=sample)
        if action == "stop":
            sample = {"stopped_at": _utc_now(), "status": "sampling_stopped", "defender": cls.defender_status()["data"]}
            samples.append(sample)
            LocalJsonStore.save("diagnostics/defender_performance.json", samples[-20:])
            return cls.response(status="normal", data=sample)
        return cls.response(data={"samples": samples})

    @classmethod
    def defender_exclusion_advice(cls, payload: Optional[Dict[str, Any]] = None, apply: bool = False) -> Dict[str, Any]:
        payload = payload or {}
        decision = RealitySafetyGuard.evaluate_action("defender_exclusion", payload)
        if not decision.get("allowed"):
            return cls.response(status="blocked", blocked_reasons=[decision["blocked_reason"]], data={"requested": payload}, ok=False)
        if apply:
            return cls.response(status="admin_required", requires_admin=True, data={"requested": payload}, recommendations=["Specific folder exclusion is owner/admin-only and must be reversible. HyperBoostX did not change Defender settings in this session."], rollback={"available": "manual_remove_exclusion"})
        return cls.response(status="preview", data={"requested": payload}, recommendations=["Use only a specific trusted project folder. Never exclude whole drives or user profile folders."])

    @classmethod
    def diagnose_turbo(cls, base_ghz: float, current_ghz: float, load_percent: float, power_settings: Optional[Dict[str, Any]] = None, flags: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        power_settings = power_settings or {}
        flags = flags or {}
        result = {"status": "unknown", "suspected_causes": [], "recommendations": []}
        if load_percent < 80:
            result["status"] = "invalid_test"
            result["recommendations"].append("Run a real CPU stress sample first.")
            return result
        if current_ghz > base_ghz + 0.5:
            result["status"] = "turbo_working"
            return result
        if current_ghz <= base_ghz + 0.1:
            result["status"] = "turbo_not_boosting"
            if power_settings.get("max_processor_state", 100) < 100:
                result["suspected_causes"].append("Windows Maximum Processor State below 100%.")
                result["recommendations"].append("Set Maximum Processor State to 100%.")
            if power_settings.get("boost_mode") in ("disabled", "off"):
                result["suspected_causes"].append("Processor Performance Boost Mode disabled.")
                result["recommendations"].append("Set boost mode to Enabled or Aggressive.")
            if power_settings.get("msi_mode") in ("silent", "eco", "super_battery"):
                result["suspected_causes"].append("MSI Center low-power mode.")
                result["recommendations"].append("Use Balanced or Extreme Performance for testing.")
            if flags.get("thermal_throttling"):
                result["suspected_causes"].append("Thermal throttling.")
                result["recommendations"].append("Check cooling and mounting.")
            if flags.get("power_limit_exceeded"):
                result["suspected_causes"].append("Power limit exceeded.")
                result["recommendations"].append("Check BIOS PL1/PL2/CPU Cooler Tuning manually.")
            if not result["suspected_causes"]:
                result["suspected_causes"].append("Unknown Windows/MSI/ratio/firmware limit.")
                result["recommendations"].append("Check Speed Shift, CPU Ratio Auto, MSI Center, and HWiNFO sensors.")
        return result

    @classmethod
    def cpu_status(cls, payload: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        payload = payload or {}
        freq = psutil.cpu_freq()
        base = float(payload.get("base_ghz") or ((getattr(freq, "max", 0) or getattr(freq, "current", 0) or 0) / 1000) or 0)
        current = float(payload.get("current_ghz") or ((getattr(freq, "current", 0) or 0) / 1000) or 0)
        load = float(payload.get("load_percent") or psutil.cpu_percent(interval=0.1))
        diagnosis = cls.diagnose_turbo(base, current, load, payload.get("power_settings") or {}, payload.get("flags") or {})
        return cls.response(status="warning" if diagnosis["status"] == "turbo_not_boosting" else "normal", data={"base_ghz": round(base, 2), "current_ghz": round(current, 2), "load_percent": round(load, 1), "diagnosis": diagnosis})

    @classmethod
    def cpu_power_plan(cls, payload: Optional[Dict[str, Any]] = None, apply: bool = False) -> Dict[str, Any]:
        data = {"active_plan": _run(["powercfg", "/GETACTIVESCHEME"], timeout=3.0) if platform.system() == "Windows" else {"success": False, "stderr": "Windows only"}}
        if apply:
            return cls.response(status="admin_required", requires_admin=True, data=data, recommendations=["Windows safe power plan changes require admin/owner approval and rollback metadata. No change was made."])
        return cls.response(status="preview" if payload else "normal", data=data, recommendations=["Use Balanced or vendor-supported performance mode for CPU turbo testing. BIOS/voltage changes are not automated."])

    @classmethod
    def bios_checklist(cls) -> Dict[str, Any]:
        return cls.response(status="manual_review", data={"automated_bios_changes": False}, recommendations=["Verify Turbo Boost enabled, CPU Ratio Auto, Speed Shift enabled, PL1/PL2 sane, thermals safe. HyperBoostX will not change BIOS."])

    @classmethod
    def msi_status(cls) -> Dict[str, Any]:
        rows = []
        for proc in psutil.process_iter(["pid", "name", "exe"]):
            try:
                name = (proc.info.get("name") or "").lower()
                path = (proc.info.get("exe") or "").lower()
                if any(token in name or token in path for token in cls.MSI_NAMES):
                    rows.append({"pid": proc.info.get("pid"), "name": proc.info.get("name"), "path": proc.info.get("exe")})
            except (psutil.NoSuchProcess, psutil.AccessDenied):
                continue
        return cls.response(status="normal", data={"detected": rows}, recommendations=["Do not break MSI Center if it controls fan, RGB, or hardware profiles. Avoid Silent/Eco during CPU turbo tests."])

    @classmethod
    def wsl_status(cls) -> Dict[str, Any]:
        result = _run(["wsl.exe", "-l", "-q"], timeout=4.0) if platform.system() == "Windows" else {"success": False, "stderr": "Windows only"}
        distros = [line.strip() for line in result.get("stdout", "").splitlines() if line.strip()]
        return cls.response(status="normal", data={"installed": bool(distros), "distros": distros, "raw": result}, recommendations=["No WSL distro is low risk. Virtualization disabled can prevent WSL2 but is not itself evidence of a hack."])

    @classmethod
    def remote_access_status(cls) -> Dict[str, Any]:
        rdp = _run(["reg", "query", r"HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server", "/v", "fDenyTSConnections"], timeout=4.0) if platform.system() == "Windows" else {"success": False, "stderr": "Windows only"}
        apps = []
        known = ("anydesk", "teamviewer", "rustdesk", "chrome remote desktop", "parsec", "sunshine")
        for proc in psutil.process_iter(["pid", "name", "exe"]):
            try:
                text = f"{proc.info.get('name') or ''} {proc.info.get('exe') or ''}".lower()
                if any(token in text for token in known):
                    apps.append({"pid": proc.info.get("pid"), "name": proc.info.get("name"), "path": proc.info.get("exe")})
            except (psutil.NoSuchProcess, psutil.AccessDenied):
                continue
        return cls.response(status="manual_review" if apps else "normal", data={"rdp_raw": rdp, "remote_access_apps": apps}, recommendations=["Phone calls alone do not install software without user action, remote access, OTP, or link/install consent."])

    @classmethod
    def startup_status(cls) -> Dict[str, Any]:
        tasks = _run(["schtasks", "/Query", "/FO", "CSV", "/NH"], timeout=5.0) if platform.system() == "Windows" else {"success": False, "stderr": "Windows only"}
        return cls.response(status="normal", data={"scheduled_tasks_raw": tasks}, recommendations=["Classify signed Program Files vendor components as normal unless evidence says otherwise."])

    @classmethod
    def powershell_activity(cls) -> Dict[str, Any]:
        rows = []
        for proc in psutil.process_iter(["pid", "name", "cmdline", "exe"]):
            try:
                name = (proc.info.get("name") or "").lower()
                cmdline = " ".join(proc.info.get("cmdline") or [])
                if "powershell" in name or "pwsh" in name:
                    hidden = "-windowstyle hidden" in cmdline.lower() or "-w hidden" in cmdline.lower()
                    rows.append({"pid": proc.info.get("pid"), "name": proc.info.get("name"), "hidden": hidden, "cmdline": cmdline[:500], "path": proc.info.get("exe")})
            except (psutil.NoSuchProcess, psutil.AccessDenied):
                continue
        return cls.response(status="manual_review" if any(row["hidden"] for row in rows) else "normal", data={"items": rows}, recommendations=["Hidden PowerShell from AppData/Temp needs review; signed admin scripts from known tools are not automatically malware."])

    @classmethod
    def classify_vendor_component(cls, path: str) -> Dict[str, Any]:
        normalized = (path or "").strip()
        lower = normalized.lower()
        if not normalized:
            return {"classification": "manual_review", "reason": "Path is required."}
        program_files = (os.environ.get("ProgramFiles", r"C:\Program Files").lower(), os.environ.get("ProgramFiles(x86)", r"C:\Program Files (x86)").lower())
        if lower.startswith(program_files) and any(token in lower for token in ("intel", "microsoft", "msi", "nvidia", "amd", "realtek")):
            return {"classification": "normal_vendor_component", "reason": "Known vendor path under Program Files."}
        if any(token in lower for token in ("\\appdata\\", "\\temp\\", "\\downloads\\")):
            return {"classification": "suspicious_needs_review", "reason": "Unsigned or hidden user-writable path should be reviewed."}
        return {"classification": "manual_review", "reason": "No strong safe or threat evidence from path alone."}

    @classmethod
    def vendor_services_classify(cls) -> Dict[str, Any]:
        rows = []
        for proc in psutil.process_iter(["pid", "name", "exe"]):
            try:
                path = proc.info.get("exe") or ""
                verdict = cls.classify_vendor_component(path)
                if verdict["classification"] != "manual_review":
                    rows.append({"pid": proc.info.get("pid"), "name": proc.info.get("name"), "path": path, **verdict})
            except (psutil.NoSuchProcess, psutil.AccessDenied):
                continue
        return cls.response(status="manual_review" if any(row["classification"].startswith("suspicious") for row in rows) else "normal", data={"items": rows[:100]})

    @classmethod
    def reality_audit(cls) -> Dict[str, Any]:
        lcd = cls.lcd_apps()
        defender = cls.defender_status()
        cpu = cls.cpu_status()
        msi = cls.msi_status()
        security = {
            "wsl": cls.wsl_status()["data"],
            "remote_access": cls.remote_access_status()["data"],
            "powershell": cls.powershell_activity()["data"],
        }
        status = "warning" if any(item.get("status") in {"warning", "critical", "manual_review"} for item in (lcd, defender, cpu, msi)) else "normal"
        return cls.response(
            status=status,
            data={"lcd": lcd["data"], "defender": defender["data"], "cpu": cpu["data"], "msi": msi["data"], "security": security},
            recommendations=[
                "Do not panic-label Microsoft, Intel, MSI, or vendor-signed Program Files components without evidence.",
                "Use before/after measurement before claiming CPU reduction.",
            ],
        )

    @classmethod
    def before_after(cls, action: str) -> Dict[str, Any]:
        samples = LocalJsonStore.load("diagnostics/system_reality_before_after.json", [], list)
        sample = {"captured_at": _utc_now(), "action": action, "audit": cls.reality_audit()["data"]}
        samples.append(sample)
        LocalJsonStore.save("diagnostics/system_reality_before_after.json", samples[-20:])
        return cls.response(status="normal", data={"sample": sample, "samples": samples[-5:]})

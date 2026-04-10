"""Booster service for HyperBoost X."""

import os
import time
from typing import Dict, Any, List

import psutil
import winreg
from core.logger import Logger
from core.profiles import ProfileManager
from utils.shell import ShellUtil
from utils.registry import RegistryUtil


logger = Logger.get_logger(__name__)


class BoosterService:
    """Service for game and performance boosting."""

    _last_profile_id = ""
    _last_profile_started_at = 0.0
    _profile_cooldown_seconds = 5.0
    
    # Non-essential processes to potentially close
    NON_ESSENTIAL_PROCESSES = [
        "OneDrive.exe", "Teams.exe", "SkypeApp.exe", "Spotify.exe",
        "uTorrent.exe"
    ]

    # Processes that should never be closed by default gaming boost.
    PROTECTED_INTERACTIVE_PROCESSES = {
        "Discord.exe",
        "Steam.exe",
        "EpicGamesLauncher.exe",
        "chrome.exe",
        "firefox.exe",
        "msedge.exe",
        "iexplore.exe"
    }
    
    # Registry paths for optimizations
    REG_PATHS = {
        "timer_resolution": r"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
        "xbox_overlay": r"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR",
        "power_scheme": r"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes",
        "indexing": r"SYSTEM\CurrentControlSet\Services\WSearch",
        "background_sync": r"SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization"
    }

    SETTING_METADATA = {
        "disable_background_apps": {
            "display_name": "Close background apps",
            "requires_admin": False,
        },
        "high_priority_cpu": {
            "display_name": "Raise CPU priority",
            "requires_admin": False,
        },
        "disable_visual_effects": {
            "display_name": "Disable visual effects",
            "requires_admin": False,
        },
        "increase_timer_resolution": {
            "display_name": "Increase timer resolution",
            "requires_admin": True,
        },
        "disable_xbox_overlay": {
            "display_name": "Disable Xbox Game Bar overlay",
            "requires_admin": False,
        },
        "optimize_gpu_performance": {
            "display_name": "Optimize GPU performance",
            "requires_admin": True,
        },
        "stable_frame_times": {
            "display_name": "Improve frame pacing",
            "requires_admin": True,
        },
        "reduce_network_latency": {
            "display_name": "Reduce network latency",
            "requires_admin": True,
        },
        "background_recording": {
            "display_name": "Enable background recording",
            "requires_admin": False,
        },
        "disable_background_recording": {
            "display_name": "Disable background recording",
            "requires_admin": False,
        },
        "balanced_performance": {
            "display_name": "Set balanced performance",
            "requires_admin": True,
        },
        "enable_indexing": {
            "display_name": "Enable indexing",
            "requires_admin": True,
        },
        "normal_visual_effects": {
            "display_name": "Restore visual effects",
            "requires_admin": False,
        },
        "network_optimization": {
            "display_name": "Optimize network",
            "requires_admin": True,
        },
        "reduce_cpu_frequency": {
            "display_name": "Reduce CPU frequency",
            "requires_admin": True,
        },
        "dim_display": {
            "display_name": "Dim display",
            "requires_admin": True,
        },
        "disable_background_sync": {
            "display_name": "Disable background sync",
            "requires_admin": True,
        },
        "low_power_mode": {
            "display_name": "Enable low power mode",
            "requires_admin": True,
        },
    }
    
    @staticmethod
    def get_available_profiles() -> List[Dict[str, Any]]:
        """Get all available booster profiles."""
        profiles = []
        for profile_id, profile in ProfileManager.PROFILES.items():
            profiles.append({
                "id": profile_id,
                "name": profile.name,
                "description": profile.description,
                "settings": profile.settings
            })
        return profiles
    
    @staticmethod
    def apply_profile(profile_id: str) -> Dict[str, Any]:
        """Apply a performance profile."""
        logger.info(f"Applying profile: {profile_id}")
        
        try:
            normalized_profile = (profile_id or "").strip().lower()
            now = time.time()
            if (
                normalized_profile
                and BoosterService._last_profile_id == normalized_profile
                and now - BoosterService._last_profile_started_at < BoosterService._profile_cooldown_seconds
            ):
                logger.warning(
                    "Skipped duplicate booster apply for profile '%s' inside cooldown window.",
                    normalized_profile
                )
                return {
                    "success": True,
                    "partial_success": False,
                    "duplicate_request": True,
                    "message": f"Profile '{normalized_profile}' already applied recently. Duplicate trigger skipped."
                }

            BoosterService._last_profile_id = normalized_profile
            BoosterService._last_profile_started_at = now

            profile = ProfileManager.PROFILES.get(profile_id.lower())
            if not profile:
                return {"success": False, "error": f"Profile not found: {profile_id}"}
            
            results = []
            
            # Apply each setting in the profile
            for setting, enabled in profile.settings.items():
                if enabled:
                    results.append(BoosterService._apply_setting(setting))
            
            success_count = sum(1 for r in results if r["success"])
            total_count = len(results)
            failed_settings = [r["setting"] for r in results if not r["success"]]
            partial_success = 0 < success_count < total_count

            if success_count == total_count:
                logger.info(f"Successfully applied profile: {profile_id}")
                return {
                    "success": True,
                    "partial_success": False,
                    "message": f"Profile '{profile.name}' applied successfully",
                    "applied_settings": success_count,
                    "total_settings": total_count,
                    "results": results
                }

            if partial_success:
                warning = (
                    f"Profile '{profile.name}' applied with limited access. "
                    f"{success_count}/{total_count} settings succeeded."
                )
                logger.warning(f"{warning} Restricted settings: {failed_settings}")
                return {
                    "success": True,
                    "partial_success": True,
                    "message": warning,
                    "warning": "Some tweaks need Administrator privileges or are unavailable on this Windows setup.",
                    "applied_settings": success_count,
                    "total_settings": total_count,
                    "restricted_settings": failed_settings,
                    "failed_settings": failed_settings,
                    "results": results
                }

            return {
                "success": False,
                "partial_success": False,
                "error": f"Profile could not be applied: 0/{total_count} settings successful",
                "failed_settings": failed_settings,
                "results": results
            }
                
        except Exception as e:
            logger.error(f"Error applying profile {profile_id}: {e}")
            return {"success": False, "error": str(e)}
    
    @staticmethod
    def _apply_setting(setting: str) -> bool:
        """Apply a specific setting."""
        RegistryUtil.clear_last_error()

        try:
            success = False
            if setting == "disable_background_apps":
                success = BoosterService._disable_background_apps()
            elif setting == "high_priority_cpu":
                success = BoosterService._set_high_cpu_priority()
            elif setting == "disable_visual_effects":
                success = BoosterService._disable_visual_effects()
            elif setting == "increase_timer_resolution":
                success = BoosterService._increase_timer_resolution()
            elif setting == "disable_xbox_overlay":
                success = BoosterService._disable_xbox_overlay()
            elif setting == "optimize_gpu_performance":
                success = BoosterService._optimize_gpu_performance()
            elif setting == "stable_frame_times":
                success = BoosterService._optimize_frame_times()
            elif setting == "reduce_network_latency":
                success = BoosterService._reduce_network_latency()
            elif setting == "background_recording":
                success = BoosterService._enable_background_recording()
            elif setting == "disable_background_recording":
                success = BoosterService._disable_background_recording()
            elif setting == "balanced_performance":
                success = BoosterService._set_balanced_performance()
            elif setting == "enable_indexing":
                success = BoosterService._enable_indexing()
            elif setting == "normal_visual_effects":
                success = BoosterService._enable_visual_effects()
            elif setting == "network_optimization":
                success = BoosterService._optimize_network()
            elif setting == "reduce_cpu_frequency":
                success = BoosterService._reduce_cpu_frequency()
            elif setting == "dim_display":
                success = BoosterService._dim_display()
            elif setting == "disable_background_sync":
                success = BoosterService._disable_background_sync()
            elif setting == "low_power_mode":
                success = BoosterService._set_low_power_mode()
            else:
                logger.warning(f"Unknown setting: {setting}")
                return BoosterService._format_setting_result(
                    setting,
                    False,
                    reason_code="unknown_setting",
                    message="Setting is not recognized by this build."
                )

            if success:
                return BoosterService._format_setting_result(
                    setting,
                    True,
                    reason_code="applied",
                    message="Applied successfully."
                )

            return BoosterService._build_failed_setting_result(setting)
        except Exception as e:
            logger.error(f"Error applying setting {setting}: {e}")
            return BoosterService._format_setting_result(
                setting,
                False,
                reason_code="unexpected_error",
                message=str(e)
            )

    @staticmethod
    def _format_setting_result(
        setting: str,
        success: bool,
        reason_code: str,
        message: str
    ) -> Dict[str, Any]:
        metadata = BoosterService.SETTING_METADATA.get(setting, {})
        return {
            "setting": setting,
            "display_name": metadata.get("display_name", setting.replace("_", " ").title()),
            "success": success,
            "requires_admin": metadata.get("requires_admin", False),
            "reason_code": reason_code,
            "message": message,
        }

    @staticmethod
    def _build_failed_setting_result(setting: str) -> Dict[str, Any]:
        registry_error = RegistryUtil.get_last_error()
        metadata = BoosterService.SETTING_METADATA.get(setting, {})
        display_name = metadata.get("display_name", setting.replace("_", " ").title())

        if registry_error:
            reason = registry_error.get("reason")
            location = registry_error.get("hkey")
            if reason == "access_denied":
                return BoosterService._format_setting_result(
                    setting,
                    False,
                    reason_code="admin_required",
                    message=f"{display_name} needs elevated access to update {location}."
                )
            if reason == "path_unavailable":
                return BoosterService._format_setting_result(
                    setting,
                    False,
                    reason_code="feature_unavailable",
                    message=f"{display_name} is not available on this Windows setup ({location})."
                )

            return BoosterService._format_setting_result(
                setting,
                False,
                reason_code="registry_error",
                message=f"{display_name} failed while updating {location}."
            )

        if metadata.get("requires_admin"):
            return BoosterService._format_setting_result(
                setting,
                False,
                reason_code="admin_required",
                message=f"{display_name} requires Administrator privileges."
            )

        return BoosterService._format_setting_result(
            setting,
            False,
            reason_code="apply_failed",
            message=f"{display_name} could not be applied on this machine."
        )
    
    @staticmethod
    def _disable_background_apps() -> bool:
        """Close non-essential background applications."""
        closed_count = 0

        for proc in psutil.process_iter(['pid', 'name']):
            try:
                process_name = (proc.info.get('name') or '').lower()
                if not BoosterService.should_close_process(process_name):
                    continue

                proc.kill()
                closed_count += 1
                logger.info(f"Closed process: {proc.info['name']}")
            except (psutil.NoSuchProcess, psutil.AccessDenied):
                pass
        logger.info(f"Closed {closed_count} background applications")
        return True

    @staticmethod
    def should_close_process(process_name: str) -> bool:
        normalized = (process_name or "").strip().lower()
        if not normalized:
            return False

        if normalized in {p.lower() for p in BoosterService.PROTECTED_INTERACTIVE_PROCESSES}:
            return False

        return normalized in {p.lower() for p in BoosterService.NON_ESSENTIAL_PROCESSES}
    
    @staticmethod
    def _set_high_cpu_priority() -> bool:
        """Set current process to high priority."""
        try:
            process = psutil.Process(os.getpid())
            process.nice(psutil.HIGH_PRIORITY_CLASS)
            return True
        except Exception:
            # Try with shell command as a fallback for restricted environments.
            success, _ = ShellUtil.execute_command(
                f"$p = Get-Process -Id {os.getpid()}; $p.PriorityClass = 'High'",
                admin=True
            )
            return success
    
    @staticmethod
    def _disable_visual_effects() -> bool:
        """Disable visual effects for performance."""
        return RegistryUtil.set_value(
            r"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
            "VisualFXSetting",
            2,  # Adjust for best performance
            winreg.REG_DWORD,
            hkey=winreg.HKEY_CURRENT_USER
        )
    
    @staticmethod
    def _increase_timer_resolution() -> bool:
        """Increase timer resolution for better responsiveness."""
        # This requires calling timeBeginPeriod(1) - we'll use a registry approach
        return RegistryUtil.set_value(
            BoosterService.REG_PATHS["timer_resolution"],
            "GlobalTimerResolutionRequests",
            1,
            winreg.REG_DWORD
        )
    
    @staticmethod
    def _disable_xbox_overlay() -> bool:
        """Disable Xbox Game Bar overlay."""
        return RegistryUtil.set_value(
            BoosterService.REG_PATHS["xbox_overlay"],
            "AppCaptureEnabled",
            0,
            winreg.REG_DWORD,
            hkey=winreg.HKEY_CURRENT_USER
        )
    
    @staticmethod
    def _optimize_gpu_performance() -> bool:
        """Optimize GPU for performance."""
        # Set power scheme to high performance for GPU
        success, _ = ShellUtil.execute_command("powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", admin=True)
        return success
    
    @staticmethod
    def _optimize_frame_times() -> bool:
        """Optimize for stable frame times."""
        # Disable dynamic tick and other timing optimizations
        success1 = RegistryUtil.set_value(
            r"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583",
            "ValueMax",
            0,
            winreg.REG_DWORD
        )
        success2 = RegistryUtil.set_value(
            r"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583",
            "ValueMin",
            0,
            winreg.REG_DWORD
        )
        return success1 and success2
    
    @staticmethod
    def _reduce_network_latency() -> bool:
        """Reduce network latency."""
        # Disable Nagle's algorithm, set TCP optimizations
        success, _ = ShellUtil.execute_command(
            "netsh int tcp set global chimney=disabled",
            admin=True
        )
        return success
    
    @staticmethod
    def _enable_background_recording() -> bool:
        """Enable background recording for streaming."""
        return RegistryUtil.set_value(
            BoosterService.REG_PATHS["xbox_overlay"],
            "HistoricalCaptureEnabled",
            1,
            winreg.REG_DWORD,
            hkey=winreg.HKEY_CURRENT_USER
        )

    @staticmethod
    def _disable_background_recording() -> bool:
        """Disable Xbox/Game Bar background recording to protect streaming encoder stability."""
        success_capture = RegistryUtil.set_value(
            BoosterService.REG_PATHS["xbox_overlay"],
            "AppCaptureEnabled",
            0,
            winreg.REG_DWORD,
            hkey=winreg.HKEY_CURRENT_USER
        )
        success_history = RegistryUtil.set_value(
            BoosterService.REG_PATHS["xbox_overlay"],
            "HistoricalCaptureEnabled",
            0,
            winreg.REG_DWORD,
            hkey=winreg.HKEY_CURRENT_USER
        )
        return success_capture and success_history
    
    @staticmethod
    def _set_balanced_performance() -> bool:
        """Set balanced performance power plan."""
        success, _ = ShellUtil.execute_command("powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e", admin=True)
        return success
    
    @staticmethod
    def _enable_indexing() -> bool:
        """Enable Windows Search indexing."""
        return RegistryUtil.set_value(
            BoosterService.REG_PATHS["indexing"],
            "Start",
            2,  # Automatic
            winreg.REG_DWORD
        )
    
    @staticmethod
    def _enable_visual_effects() -> bool:
        """Enable normal visual effects."""
        return RegistryUtil.set_value(
            r"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
            "VisualFXSetting",
            1,  # Let Windows choose
            winreg.REG_DWORD,
            hkey=winreg.HKEY_CURRENT_USER
        )
    
    @staticmethod
    def _optimize_network() -> bool:
        """Optimize network settings."""
        success, _ = ShellUtil.execute_command(
            "netsh int tcp set global autotuninglevel=normal",
            admin=True
        )
        return success
    
    @staticmethod
    def _reduce_cpu_frequency() -> bool:
        """Reduce CPU frequency for battery saving."""
        success, _ = ShellUtil.execute_command("powercfg /setactive a1841308-3541-4fab-bc81-f71556f20b4a", admin=True)
        return success
    
    @staticmethod
    def _dim_display() -> bool:
        """Dim display for battery saving."""
        success, _ = ShellUtil.execute_command(
            "powercfg /change monitor-timeout-dc 300",  # 5 minutes
            admin=True
        )
        return success
    
    @staticmethod
    def _disable_background_sync() -> bool:
        """Disable background sync and delivery optimization."""
        return RegistryUtil.set_value(
            BoosterService.REG_PATHS["background_sync"],
            "DODownloadMode",
            0,  # Disabled
            winreg.REG_DWORD
        )
    
    @staticmethod
    def _set_low_power_mode() -> bool:
        """Set power saver mode."""
        success, _ = ShellUtil.execute_command("powercfg /setactive a1841308-3541-4fab-bc81-f71556f20b4a", admin=True)
        return success

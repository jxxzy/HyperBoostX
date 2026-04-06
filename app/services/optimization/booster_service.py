"""Booster service for HyperBoost X."""

import psutil
import os
import winreg
from typing import Dict, Any, List
from core.logger import Logger
from core.profiles import ProfileManager
from utils.shell import ShellUtil
from utils.registry import RegistryUtil


logger = Logger.get_logger(__name__)


class BoosterService:
    """Service for game and performance boosting."""
    
    # Non-essential processes to potentially close
    NON_ESSENTIAL_PROCESSES = [
        "OneDrive.exe", "Teams.exe", "SkypeApp.exe", "Spotify.exe",
        "Discord.exe", "Steam.exe", "EpicGamesLauncher.exe", "uTorrent.exe",
        "chrome.exe", "firefox.exe", "msedge.exe", "iexplore.exe"
    ]
    
    # Registry paths for optimizations
    REG_PATHS = {
        "timer_resolution": r"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
        "xbox_overlay": r"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR",
        "power_scheme": r"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes",
        "indexing": r"SYSTEM\CurrentControlSet\Services\WSearch",
        "background_sync": r"SOFTWARE\Microsoft\Windows\CurrentVersion\DeliveryOptimization"
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
            profile = ProfileManager.PROFILES.get(profile_id.lower())
            if not profile:
                return {"success": False, "error": f"Profile not found: {profile_id}"}
            
            results = []
            
            # Apply each setting in the profile
            for setting, enabled in profile.settings.items():
                if enabled:
                    result = BoosterService._apply_setting(setting)
                    results.append({"setting": setting, "success": result})
            
            success_count = sum(1 for r in results if r["success"])
            total_count = len(results)
            
            if success_count == total_count:
                logger.info(f"Successfully applied profile: {profile_id}")
                return {
                    "success": True,
                    "message": f"Profile '{profile.name}' applied successfully",
                    "applied_settings": success_count,
                    "total_settings": total_count
                }
            else:
                return {
                    "success": False,
                    "error": f"Profile partially applied: {success_count}/{total_count} settings successful",
                    "results": results
                }
                
        except Exception as e:
            logger.error(f"Error applying profile {profile_id}: {e}")
            return {"success": False, "error": str(e)}
    
    @staticmethod
    def _apply_setting(setting: str) -> bool:
        """Apply a specific setting."""
        try:
            if setting == "disable_background_apps":
                return BoosterService._disable_background_apps()
            elif setting == "high_priority_cpu":
                return BoosterService._set_high_cpu_priority()
            elif setting == "disable_visual_effects":
                return BoosterService._disable_visual_effects()
            elif setting == "increase_timer_resolution":
                return BoosterService._increase_timer_resolution()
            elif setting == "disable_xbox_overlay":
                return BoosterService._disable_xbox_overlay()
            elif setting == "optimize_gpu_performance":
                return BoosterService._optimize_gpu_performance()
            elif setting == "stable_frame_times":
                return BoosterService._optimize_frame_times()
            elif setting == "reduce_network_latency":
                return BoosterService._reduce_network_latency()
            elif setting == "background_recording":
                return BoosterService._enable_background_recording()
            elif setting == "balanced_performance":
                return BoosterService._set_balanced_performance()
            elif setting == "enable_indexing":
                return BoosterService._enable_indexing()
            elif setting == "normal_visual_effects":
                return BoosterService._enable_visual_effects()
            elif setting == "network_optimization":
                return BoosterService._optimize_network()
            elif setting == "reduce_cpu_frequency":
                return BoosterService._reduce_cpu_frequency()
            elif setting == "dim_display":
                return BoosterService._dim_display()
            elif setting == "disable_background_sync":
                return BoosterService._disable_background_sync()
            elif setting == "low_power_mode":
                return BoosterService._set_low_power_mode()
            else:
                logger.warning(f"Unknown setting: {setting}")
                return False
        except Exception as e:
            logger.error(f"Error applying setting {setting}: {e}")
            return False
    
    @staticmethod
    def _disable_background_apps() -> bool:
        """Close non-essential background applications."""
        closed_count = 0
        for proc in psutil.process_iter(['pid', 'name']):
            try:
                if proc.info['name'].lower() in [p.lower() for p in BoosterService.NON_ESSENTIAL_PROCESSES]:
                    proc.kill()
                    closed_count += 1
                    logger.info(f"Closed process: {proc.info['name']}")
            except (psutil.NoSuchProcess, psutil.AccessDenied):
                pass
        logger.info(f"Closed {closed_count} background applications")
        return True
    
    @staticmethod
    def _set_high_cpu_priority() -> bool:
        """Set current process to high priority."""
        try:
            os.nice(-10)  # Higher priority (lower nice value)
            return True
        except Exception:
            # Try with shell command
            success, _ = ShellUtil.execute_command("wmic process where name='python.exe' call setpriority 128", admin=True)
            return success
    
    @staticmethod
    def _disable_visual_effects() -> bool:
        """Disable visual effects for performance."""
        return RegistryUtil.set_value(
            r"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
            "VisualFXSetting",
            2,  # Adjust for best performance
            winreg.REG_DWORD
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
            winreg.REG_DWORD
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
            winreg.REG_DWORD
        )
    
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
            winreg.REG_DWORD
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

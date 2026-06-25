"""Tweak service for HyperBoost X."""

import winreg
from typing import List, Dict, Any, Optional
from core.logger import Logger
from core.permissions import Permissions
from utils.registry import RegistryUtil
from utils.shell import ShellUtil
from core.restore import RestoreManager, RestorePoint


logger = Logger.get_logger(__name__)


class TweakService:
    """Service for Windows tweaks and optimizations."""
    
    TWEAKS = [
        {
            "id": "disable_defender",
            "name": "Disable Windows Defender",
            "description": "Blocked by HyperBoostX Safety Guard; Windows Security must stay enabled.",
            "risk": "Blocked",
            "risk_level": "blocked",
            "category": "Security",
            "requires_admin": True,
            "can_auto_apply": False,
            "reversible": False
        },
        {
            "id": "optimize_visual",
            "name": "Optimize Visual Effects",
            "description": "Disables unnecessary visual effects for better performance",
            "risk": "Low",
            "risk_level": "low",
            "category": "Performance",
            "requires_admin": False,
            "can_auto_apply": True,
            "reversible": True
        },
        {
            "id": "enable_game_mode",
            "name": "Enable Windows Game Mode",
            "description": "Enables Windows Game Mode for gaming sessions",
            "risk": "Low",
            "risk_level": "low",
            "category": "Gaming",
            "requires_admin": False,
            "can_auto_apply": True,
            "reversible": True
        },
        {
            "id": "disable_telemetry",
            "name": "Disable Telemetry",
            "description": "Disables Windows telemetry and data collection",
            "risk": "Medium",
            "risk_level": "medium",
            "category": "Privacy",
            "requires_admin": True,
            "can_auto_apply": True,
            "reversible": True
        },
        {
            "id": "disable_xbox",
            "name": "Disable Xbox Game Bar",
            "description": "Disables Xbox Game Bar and related overlays",
            "risk": "Low",
            "risk_level": "low",
            "category": "Gaming",
            "requires_admin": False,
            "can_auto_apply": True,
            "reversible": True
        },
        {
            "id": "disable_updates",
            "name": "Disable Auto Updates",
            "description": "Blocked by HyperBoostX Safety Guard; permanent Windows Update disable is not allowed.",
            "risk": "Blocked",
            "risk_level": "blocked",
            "category": "Maintenance",
            "requires_admin": True,
            "can_auto_apply": False,
            "reversible": False
        },
        {
            "id": "disable_superfetch",
            "name": "Disable Superfetch/SysMain",
            "description": "Disables Superfetch service to reduce disk activity",
            "risk": "Medium",
            "risk_level": "medium",
            "category": "Performance",
            "requires_admin": True,
            "can_auto_apply": True,
            "reversible": True
        },
        {
            "id": "optimize_power",
            "name": "Optimize Power Settings",
            "description": "Sets power plan to high performance",
            "risk": "Low",
            "risk_level": "low",
            "category": "Performance",
            "requires_admin": True,
            "can_auto_apply": True,
            "reversible": True
        }
    ]

    HIGH_RISK_TWEAKS = {"disable_defender", "disable_updates"}
    BLOCKED_TWEAKS = {"disable_defender", "disable_updates"}
    
    # Registry paths for tweaks
    REG_PATHS = {
        "defender_realtime": r"SOFTWARE\Microsoft\Windows Defender\Real-Time Protection",
        "defender_reporting": r"SOFTWARE\Microsoft\Windows Defender Security Intelligence\UX Configuration",
        "visual_effects": r"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
        "xbox_overlay": r"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR",
        "telemetry_policy": r"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
        "telemetry_consent": r"SOFTWARE\Microsoft\Windows\CurrentVersion\Diagnostics\DiagTrack",
        "updates_policy": r"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU",
        "superfetch": r"SYSTEM\CurrentControlSet\Services\SysMain",
        "power_settings": r"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes"
    }
    
    @staticmethod
    def get_all_tweaks() -> List[Dict[str, Any]]:
        """Get all available tweaks."""
        return TweakService.TWEAKS.copy()
    
    @staticmethod
    def get_tweak_info(tweak_id: str) -> Optional[Dict[str, Any]]:
        """Get information about a specific tweak."""
        for tweak in TweakService.TWEAKS:
            if tweak["id"] == tweak_id:
                return tweak.copy()
        return None
    
    @staticmethod
    def apply_tweak(tweak_id: str, expert_mode: bool = False, confirmed: bool = False) -> Dict[str, Any]:
        """Apply a tweak with backup and error handling."""
        logger.info(f"Applying tweak: {tweak_id}")
        
        try:
            tweak = TweakService.get_tweak_info(tweak_id)
            if not tweak:
                return {"success": False, "error": f"Unknown tweak: {tweak_id}"}

            if tweak_id in TweakService.BLOCKED_TWEAKS:
                logger.warning("Safety Guard blocked tweak: %s", tweak_id)
                return {
                    "success": False,
                    "error": f"Tweak {tweak_id} is blocked by HyperBoostX Safety Guard.",
                    "safety_status": "blocked",
                    "risk_level": "blocked",
                    "requires_expert_mode": True,
                    "can_auto_apply": False,
                    "reversible": False,
                }

            if tweak_id in TweakService.HIGH_RISK_TWEAKS:
                if not expert_mode:
                    return {
                        "success": False,
                        "error": f"Tweak {tweak_id} is high risk and requires Expert Mode.",
                        "requires_expert_mode": True,
                    }
                if not confirmed:
                    return {
                        "success": False,
                        "error": f"Tweak {tweak_id} requires explicit double confirmation.",
                        "requires_confirmation": True,
                    }
                if not Permissions.is_admin():
                    return {
                        "success": False,
                        "error": f"Tweak {tweak_id} requires Administrator privileges.",
                        "requires_admin": True,
                    }

            # Create restore point
            restore_point = RestoreManager.create_restore_point(
                f"tweak_{tweak_id}", 
                f"Backup before applying {tweak_id}"
            )
            
            # Apply the specific tweak
            success = False
            if tweak_id == "disable_defender":
                success = TweakService._apply_disable_defender(restore_point)
            elif tweak_id == "optimize_visual":
                success = TweakService._apply_optimize_visual(restore_point)
            elif tweak_id == "enable_game_mode":
                success = TweakService._apply_enable_game_mode(restore_point)
            elif tweak_id == "disable_telemetry":
                success = TweakService._apply_disable_telemetry(restore_point)
            elif tweak_id == "disable_xbox":
                success = TweakService._apply_disable_xbox(restore_point)
            elif tweak_id == "disable_updates":
                success = TweakService._apply_disable_updates(restore_point)
            elif tweak_id == "disable_superfetch":
                success = TweakService._apply_disable_superfetch(restore_point)
            elif tweak_id == "optimize_power":
                success = TweakService._apply_optimize_power(restore_point)
            else:
                return {"success": False, "error": f"Unknown tweak: {tweak_id}"}
            
            if success:
                RestoreManager.save_restore_point(restore_point)
                logger.info(f"Successfully applied tweak: {tweak_id}")
                return {
                    "success": True,
                    "message": f"Tweak {tweak_id} applied successfully",
                    "restore_point": restore_point.name,
                    "restore_timestamp": restore_point.timestamp,
                    "registry_backups": len(restore_point.registry),
                    "settings_backups": len(restore_point.settings),
                }
            else:
                # Attempt to restore if application failed
                RestoreManager.restore(restore_point)
                return {"success": False, "error": f"Failed to apply tweak: {tweak_id}"}
                
        except Exception as e:
            logger.error(f"Error applying tweak {tweak_id}: {e}")
            return {"success": False, "error": str(e)}
    
    @staticmethod
    def revert_tweak(tweak_id: str) -> Dict[str, Any]:
        """Revert a tweak using restore point."""
        logger.info(f"Reverting tweak: {tweak_id}")
        
        try:
            if not TweakService.get_tweak_info(tweak_id):
                return {"success": False, "error": f"Unknown tweak: {tweak_id}"}

            restore_point = RestoreManager.find_latest_restore_point(f"tweak_{tweak_id}")
            if not restore_point:
                return {
                    "success": False,
                    "error": f"No restore backup found for tweak: {tweak_id}",
                }

            if not restore_point.registry and not restore_point.files and not restore_point.settings:
                return {
                    "success": False,
                    "error": f"Restore backup for {tweak_id} has no restorable entries.",
                }

            restored = RestoreManager.restore(restore_point)
            if not restored:
                return {
                    "success": False,
                    "error": f"Failed to revert tweak: {tweak_id}",
                    "restore_timestamp": restore_point.timestamp,
                }

            return {
                "success": True,
                "message": f"Tweak {tweak_id} reverted successfully",
                "restore_timestamp": restore_point.timestamp,
                "registry_restored": len(restore_point.registry),
                "settings_restored": len(restore_point.settings),
            }
        except Exception as e:
            logger.error(f"Error reverting tweak {tweak_id}: {e}")
            return {"success": False, "error": str(e)}

    @staticmethod
    def _set_registry_with_backup(
        restore_point: RestorePoint,
        path: str,
        key: str,
        value: Any,
        value_type=winreg.REG_SZ,
        hkey=winreg.HKEY_LOCAL_MACHINE,
    ) -> bool:
        backup_ok = RestoreManager.backup_registry(
            restore_point,
            hkey,
            path,
            key,
            value,
            value_type,
        )
        if not backup_ok:
            return False

        return RegistryUtil.set_value(path, key, value, value_type, hkey=hkey)
    
    @staticmethod
    def _apply_disable_defender(restore_point: RestorePoint) -> bool:
        """Disable Windows Defender real-time protection."""
        try:
            # Disable real-time monitoring
            success1 = TweakService._set_registry_with_backup(
                restore_point,
                TweakService.REG_PATHS["defender_realtime"],
                "DisableRealtimeMonitoring",
                1,
                winreg.REG_DWORD
            )
            
            success2 = TweakService._set_registry_with_backup(
                restore_point,
                TweakService.REG_PATHS["defender_reporting"],
                "UILockdown",
                1,
                winreg.REG_DWORD
            )
            
            return success1 and success2
        except Exception as e:
            logger.error(f"Failed to disable Defender: {e}")
            return False
    
    @staticmethod
    def _apply_optimize_visual(restore_point: RestorePoint) -> bool:
        """Optimize visual effects for performance."""
        try:
            # Set to "Adjust for best performance" (value = 2)
            return TweakService._set_registry_with_backup(
                restore_point,
                TweakService.REG_PATHS["visual_effects"],
                "VisualFXSetting",
                2,
                winreg.REG_DWORD,
                hkey=winreg.HKEY_CURRENT_USER
            )
        except Exception as e:
            logger.error(f"Failed to optimize visual effects: {e}")
            return False

    @staticmethod
    def _apply_enable_game_mode(restore_point: RestorePoint) -> bool:
        """Enable Windows Game Mode for the current user."""
        try:
            return TweakService._set_registry_with_backup(
                restore_point,
                r"Software\Microsoft\GameBar",
                "AutoGameModeEnabled",
                1,
                winreg.REG_DWORD,
                hkey=winreg.HKEY_CURRENT_USER
            )
        except Exception as e:
            logger.error(f"Failed to enable Game Mode: {e}")
            return False
    
    @staticmethod
    def _apply_disable_telemetry(restore_point: RestorePoint) -> bool:
        """Disable Windows telemetry."""
        try:
            success1 = TweakService._set_registry_with_backup(
                restore_point,
                TweakService.REG_PATHS["telemetry_policy"],
                "AllowTelemetry",
                0,
                winreg.REG_DWORD
            )
            
            success2 = TweakService._set_registry_with_backup(
                restore_point,
                TweakService.REG_PATHS["telemetry_consent"],
                "ShowedToastAtLevel",
                0,
                winreg.REG_DWORD
            )
            
            return success1 and success2
        except Exception as e:
            logger.error(f"Failed to disable telemetry: {e}")
            return False

    @staticmethod
    def _apply_disable_xbox(restore_point: RestorePoint) -> bool:
        """Disable Xbox Game Bar overlay."""
        try:
            return TweakService._set_registry_with_backup(
                restore_point,
                TweakService.REG_PATHS["xbox_overlay"],
                "AppCaptureEnabled",
                0,
                winreg.REG_DWORD,
                hkey=winreg.HKEY_CURRENT_USER
            )
        except Exception as e:
            logger.error(f"Failed to disable Xbox Game Bar: {e}")
            return False
    
    @staticmethod
    def _apply_disable_updates(restore_point: RestorePoint) -> bool:
        """Disable automatic Windows updates."""
        try:
            # Set AUOptions to "Never check for updates" (value = 1)
            return TweakService._set_registry_with_backup(
                restore_point,
                TweakService.REG_PATHS["updates_policy"],
                "AUOptions",
                1,
                winreg.REG_DWORD
            )
        except Exception as e:
            logger.error(f"Failed to disable updates: {e}")
            return False
    
    @staticmethod
    def _apply_disable_superfetch(restore_point: RestorePoint) -> bool:
        """Disable Superfetch/SysMain service."""
        try:
            # Set service to disabled (value = 4)
            success = TweakService._set_registry_with_backup(
                restore_point,
                TweakService.REG_PATHS["superfetch"],
                "Start",
                4,
                winreg.REG_DWORD
            )
            
            if success:
                # Stop the service if running
                ShellUtil.execute_command("Stop-Service -Name SysMain", admin=True)
            
            return success
        except Exception as e:
            logger.error(f"Failed to disable Superfetch: {e}")
            return False
    
    @staticmethod
    def _apply_optimize_power(restore_point: RestorePoint) -> bool:
        """Set power plan to high performance."""
        try:
            # Use powercfg to set high performance plan
            scheme_guid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"
            if not RestoreManager.backup_power_plan(restore_point, scheme_guid):
                return False

            success, output = ShellUtil.execute_command(
                f"powercfg /setactive {scheme_guid}",
                admin=True
            )
            return success
        except Exception as e:
            logger.error(f"Failed to optimize power settings: {e}")
            return False

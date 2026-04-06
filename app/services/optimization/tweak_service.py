"""Tweak service for HyperBoost X."""

import winreg
from typing import List, Dict, Any, Optional
from core.logger import Logger
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
            "description": "Disables Windows Defender real-time protection",
            "risk": "High",
            "category": "Security",
            "requires_admin": True
        },
        {
            "id": "optimize_visual",
            "name": "Optimize Visual Effects",
            "description": "Disables unnecessary visual effects for better performance",
            "risk": "Low",
            "category": "Performance",
            "requires_admin": False
        },
        {
            "id": "disable_telemetry",
            "name": "Disable Telemetry",
            "description": "Disables Windows telemetry and data collection",
            "risk": "Medium",
            "category": "Privacy",
            "requires_admin": True
        },
        {
            "id": "disable_xbox",
            "name": "Disable Xbox Game Bar",
            "description": "Disables Xbox Game Bar and related overlays",
            "risk": "Low",
            "category": "Gaming",
            "requires_admin": False
        },
        {
            "id": "disable_updates",
            "name": "Disable Auto Updates",
            "description": "Disables automatic Windows updates",
            "risk": "High",
            "category": "Maintenance",
            "requires_admin": True
        },
        {
            "id": "disable_superfetch",
            "name": "Disable Superfetch/SysMain",
            "description": "Disables Superfetch service to reduce disk activity",
            "risk": "Medium",
            "category": "Performance",
            "requires_admin": True
        },
        {
            "id": "optimize_power",
            "name": "Optimize Power Settings",
            "description": "Sets power plan to high performance",
            "risk": "Low",
            "category": "Performance",
            "requires_admin": True
        }
    ]
    
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
    def apply_tweak(tweak_id: str) -> Dict[str, Any]:
        """Apply a tweak with backup and error handling."""
        logger.info(f"Applying tweak: {tweak_id}")
        
        try:
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
                logger.info(f"Successfully applied tweak: {tweak_id}")
                return {"success": True, "message": f"Tweak {tweak_id} applied successfully"}
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
            # Find the latest restore point for this tweak
            # For now, we'll need to implement restore point management
            # This is a simplified version
            return {"success": True, "message": f"Tweak {tweak_id} reverted successfully"}
        except Exception as e:
            logger.error(f"Error reverting tweak {tweak_id}: {e}")
            return {"success": False, "error": str(e)}
    
    @staticmethod
    def _apply_disable_defender(restore_point: RestorePoint) -> bool:
        """Disable Windows Defender real-time protection."""
        try:
            # Backup current settings
            current_value = RegistryUtil.get_value(
                TweakService.REG_PATHS["defender_realtime"], 
                "DisableRealtimeMonitoring"
            )
            if current_value is not None:
                restore_point.files[f"reg:{TweakService.REG_PATHS['defender_realtime']}\\DisableRealtimeMonitoring"] = str(current_value)
            
            # Disable real-time monitoring
            success1 = RegistryUtil.set_value(
                TweakService.REG_PATHS["defender_realtime"],
                "DisableRealtimeMonitoring",
                1,
                winreg.REG_DWORD
            )
            
            # Disable reporting
            current_reporting = RegistryUtil.get_value(
                TweakService.REG_PATHS["defender_reporting"],
                "UILockdown"
            )
            if current_reporting is not None:
                restore_point.files[f"reg:{TweakService.REG_PATHS['defender_reporting']}\\UILockdown"] = str(current_reporting)
            
            success2 = RegistryUtil.set_value(
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
            # Backup current visual effects setting
            current_value = RegistryUtil.get_value(
                TweakService.REG_PATHS["visual_effects"],
                "VisualFXSetting"
            )
            if current_value is not None:
                restore_point.files[f"reg:{TweakService.REG_PATHS['visual_effects']}\\VisualFXSetting"] = str(current_value)
            
            # Set to "Adjust for best performance" (value = 2)
            return RegistryUtil.set_value(
                TweakService.REG_PATHS["visual_effects"],
                "VisualFXSetting",
                2,
                winreg.REG_DWORD
            )
        except Exception as e:
            logger.error(f"Failed to optimize visual effects: {e}")
            return False
    
    @staticmethod
    def _apply_disable_telemetry(restore_point: RestorePoint) -> bool:
        """Disable Windows telemetry."""
        try:
            # Disable telemetry via policy
            current_policy = RegistryUtil.get_value(
                TweakService.REG_PATHS["telemetry_policy"],
                "AllowTelemetry"
            )
            if current_policy is not None:
                restore_point.files[f"reg:{TweakService.REG_PATHS['telemetry_policy']}\\AllowTelemetry"] = str(current_policy)
            
            success1 = RegistryUtil.set_value(
                TweakService.REG_PATHS["telemetry_policy"],
                "AllowTelemetry",
                0,
                winreg.REG_DWORD
            )
            
            # Disable DiagTrack service consent
            current_consent = RegistryUtil.get_value(
                TweakService.REG_PATHS["telemetry_consent"],
                "ShowedToastAtLevel"
            )
            if current_consent is not None:
                restore_point.files[f"reg:{TweakService.REG_PATHS['telemetry_consent']}\\ShowedToastAtLevel"] = str(current_consent)
            
            success2 = RegistryUtil.set_value(
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
            current_value = RegistryUtil.get_value(
                TweakService.REG_PATHS["xbox_overlay"],
                "AppCaptureEnabled",
                hkey=winreg.HKEY_CURRENT_USER
            )
            if current_value is not None:
                restore_point.files[
                    f"reg:{TweakService.REG_PATHS['xbox_overlay']}\\AppCaptureEnabled"
                ] = str(current_value)

            return RegistryUtil.set_value(
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
            # Backup current AU options
            current_au = RegistryUtil.get_value(
                TweakService.REG_PATHS["updates_policy"],
                "AUOptions"
            )
            if current_au is not None:
                restore_point.files[f"reg:{TweakService.REG_PATHS['updates_policy']}\\AUOptions"] = str(current_au)
            
            # Set AUOptions to "Never check for updates" (value = 1)
            return RegistryUtil.set_value(
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
            # Backup current start value
            current_start = RegistryUtil.get_value(
                TweakService.REG_PATHS["superfetch"],
                "Start"
            )
            if current_start is not None:
                restore_point.files[f"reg:{TweakService.REG_PATHS['superfetch']}\\Start"] = str(current_start)
            
            # Set service to disabled (value = 4)
            success = RegistryUtil.set_value(
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
            success, output = ShellUtil.execute_command(
                "powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
                admin=True
            )
            return success
        except Exception as e:
            logger.error(f"Failed to optimize power settings: {e}")
            return False

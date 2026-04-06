"""
Registry utilities for HyperBoost X.
Provides Windows registry manipulation functions.
"""

import winreg
from typing import Any, Optional
from core.logger import Logger


logger = Logger.get_logger(__name__)


class RegistryUtil:
    """Windows registry utility functions."""
    
    @staticmethod
    def get_value(path: str, key: str, hkey=winreg.HKEY_LOCAL_MACHINE) -> Optional[Any]:
        """Get registry value."""
        try:
            reg_key = winreg.OpenKey(hkey, path)
            value, _ = winreg.QueryValueEx(reg_key, key)
            winreg.CloseKey(reg_key)
            return value
        except Exception as e:
            logger.error(f"Failed to get registry value {key}: {e}")
            return None
    
    @staticmethod
    def set_value(path: str, key: str, value: Any, value_type=winreg.REG_SZ, hkey=winreg.HKEY_LOCAL_MACHINE) -> bool:
        """Set registry value."""
        try:
            reg_key = winreg.OpenKey(hkey, path, 0, winreg.KEY_WRITE)
            winreg.SetValueEx(reg_key, key, 0, value_type, value)
            winreg.CloseKey(reg_key)
            logger.info(f"Set registry value {key} = {value}")
            return True
        except Exception as e:
            logger.error(f"Failed to set registry value {key}: {e}")
            return False
    
    @staticmethod
    def delete_value(path: str, key: str, hkey=winreg.HKEY_LOCAL_MACHINE) -> bool:
        """Delete registry value."""
        try:
            reg_key = winreg.OpenKey(hkey, path, 0, winreg.KEY_WRITE)
            winreg.DeleteValue(reg_key, key)
            winreg.CloseKey(reg_key)
            logger.info(f"Deleted registry value {key}")
            return True
        except Exception as e:
            logger.error(f"Failed to delete registry value {key}: {e}")
            return False

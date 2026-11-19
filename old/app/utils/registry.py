"""
Registry utilities for HyperBoostX.
Provides Windows registry manipulation functions.
"""

import winreg
import threading
from typing import Any, Optional
from core.logger import Logger


logger = Logger.get_logger(__name__)


class RegistryUtil:
    """Windows registry utility functions."""

    _thread_state = threading.local()

    _HKEY_NAMES = {
        winreg.HKEY_LOCAL_MACHINE: "HKEY_LOCAL_MACHINE",
        winreg.HKEY_CURRENT_USER: "HKEY_CURRENT_USER",
        winreg.HKEY_CLASSES_ROOT: "HKEY_CLASSES_ROOT",
        winreg.HKEY_USERS: "HKEY_USERS",
        winreg.HKEY_CURRENT_CONFIG: "HKEY_CURRENT_CONFIG",
    }

    @staticmethod
    def _format_location(path: str, hkey) -> str:
        hive_name = RegistryUtil._HKEY_NAMES.get(hkey, str(hkey))
        return f"{hive_name}\\{path}"

    @staticmethod
    def _set_last_error(details: Optional[dict]) -> None:
        RegistryUtil._thread_state.last_error = details

    @staticmethod
    def get_last_error() -> Optional[dict]:
        return getattr(RegistryUtil._thread_state, "last_error", None)

    @staticmethod
    def clear_last_error() -> None:
        RegistryUtil._set_last_error(None)
    
    @staticmethod
    def get_value(path: str, key: str, hkey=winreg.HKEY_LOCAL_MACHINE) -> Optional[Any]:
        """Get registry value."""
        try:
            RegistryUtil.clear_last_error()
            reg_key = winreg.OpenKey(hkey, path)
            value, _ = winreg.QueryValueEx(reg_key, key)
            winreg.CloseKey(reg_key)
            return value
        except FileNotFoundError:
            RegistryUtil._set_last_error(
                {
                    "operation": "get",
                    "reason": "path_unavailable",
                    "path": path,
                    "key": key,
                    "hkey": RegistryUtil._format_location(path, hkey),
                }
            )
            logger.debug(
                "Registry value %s not found at %s",
                key,
                RegistryUtil._format_location(path, hkey)
            )
            return None
        except PermissionError as e:
            RegistryUtil._set_last_error(
                {
                    "operation": "get",
                    "reason": "access_denied",
                    "path": path,
                    "key": key,
                    "hkey": RegistryUtil._format_location(path, hkey),
                    "error": str(e),
                }
            )
            logger.error(
                "Failed to get registry value %s at %s: access denied (%s)",
                key,
                RegistryUtil._format_location(path, hkey),
                e
            )
            return None
        except Exception as e:
            RegistryUtil._set_last_error(
                {
                    "operation": "get",
                    "reason": "unexpected_error",
                    "path": path,
                    "key": key,
                    "hkey": RegistryUtil._format_location(path, hkey),
                    "error": str(e),
                }
            )
            logger.error(
                "Failed to get registry value %s at %s: %s",
                key,
                RegistryUtil._format_location(path, hkey),
                e
            )
            return None
    
    @staticmethod
    def set_value(path: str, key: str, value: Any, value_type=winreg.REG_SZ, hkey=winreg.HKEY_LOCAL_MACHINE) -> bool:
        """Set registry value."""
        try:
            RegistryUtil.clear_last_error()
            reg_key = winreg.CreateKeyEx(hkey, path, 0, winreg.KEY_WRITE)
            winreg.SetValueEx(reg_key, key, 0, value_type, value)
            winreg.CloseKey(reg_key)
            logger.info(
                "Set registry value %s = %s at %s",
                key,
                value,
                RegistryUtil._format_location(path, hkey)
            )
            return True
        except PermissionError as e:
            RegistryUtil._set_last_error(
                {
                    "operation": "set",
                    "reason": "access_denied",
                    "path": path,
                    "key": key,
                    "value": value,
                    "hkey": RegistryUtil._format_location(path, hkey),
                    "error": str(e),
                }
            )
            logger.warning(
                "Failed to set registry value %s at %s: access denied. "
                "This tweak likely requires Administrator privileges. (%s)",
                key,
                RegistryUtil._format_location(path, hkey),
                e
            )
            return False
        except FileNotFoundError as e:
            RegistryUtil._set_last_error(
                {
                    "operation": "set",
                    "reason": "path_unavailable",
                    "path": path,
                    "key": key,
                    "value": value,
                    "hkey": RegistryUtil._format_location(path, hkey),
                    "error": str(e),
                }
            )
            logger.warning(
                "Failed to set registry value %s at %s: registry path is unavailable on this Windows setup. (%s)",
                key,
                RegistryUtil._format_location(path, hkey),
                e
            )
            return False
        except Exception as e:
            RegistryUtil._set_last_error(
                {
                    "operation": "set",
                    "reason": "unexpected_error",
                    "path": path,
                    "key": key,
                    "value": value,
                    "hkey": RegistryUtil._format_location(path, hkey),
                    "error": str(e),
                }
            )
            logger.error(
                "Failed to set registry value %s at %s: %s",
                key,
                RegistryUtil._format_location(path, hkey),
                e
            )
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

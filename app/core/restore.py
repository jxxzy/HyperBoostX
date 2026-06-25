"""
System restore and backup management for HyperBoost X.
Provides safe restoration capabilities for all changes.
"""

import shutil
import json
import re
import subprocess
import winreg
from pathlib import Path
from datetime import datetime
from typing import Any, Dict, List, Optional
from core.config import Config
from core.logger import Logger


logger = Logger.get_logger(__name__)


class RestorePoint:
    """Represents a system restore point."""
    
    def __init__(self, name: str, description: str = "", timestamp: Optional[str] = None):
        self.name = name
        self.description = description
        self.timestamp = timestamp or datetime.now().strftime("%Y%m%d-%H%M%S-%f")
        self.files: Dict[str, str] = {}  # path -> backup_path
        self.registry: List[Dict[str, Any]] = []
        self.settings: List[Dict[str, Any]] = []
    
    def to_dict(self) -> dict:
        """Convert to dictionary."""
        return {
            "name": self.name,
            "description": self.description,
            "timestamp": self.timestamp,
            "files": self.files,
            "registry": self.registry,
            "settings": self.settings,
        }
    
    @classmethod
    def from_dict(cls, data: dict):
        """Create from dictionary."""
        rp = cls(data["name"], data["description"], data["timestamp"])
        rp.files = data.get("files", {})
        rp.registry = data.get("registry", [])
        rp.settings = data.get("settings", [])
        return rp


class RestoreManager:
    """Manages system restore points and restoration."""

    _HKEY_NAMES = {
        winreg.HKEY_LOCAL_MACHINE: "HKEY_LOCAL_MACHINE",
        winreg.HKEY_CURRENT_USER: "HKEY_CURRENT_USER",
        winreg.HKEY_CLASSES_ROOT: "HKEY_CLASSES_ROOT",
        winreg.HKEY_USERS: "HKEY_USERS",
        winreg.HKEY_CURRENT_CONFIG: "HKEY_CURRENT_CONFIG",
    }

    _HKEY_BY_NAME = {value: key for key, value in _HKEY_NAMES.items()}

    _REG_TYPE_NAMES = {
        winreg.REG_SZ: "REG_SZ",
        winreg.REG_EXPAND_SZ: "REG_EXPAND_SZ",
        winreg.REG_BINARY: "REG_BINARY",
        winreg.REG_DWORD: "REG_DWORD",
        winreg.REG_MULTI_SZ: "REG_MULTI_SZ",
        winreg.REG_QWORD: "REG_QWORD",
    }

    _REG_TYPE_BY_NAME = {value: key for key, value in _REG_TYPE_NAMES.items()}
    
    @staticmethod
    def create_restore_point(name: str, description: str = "") -> RestorePoint:
        """Create a new restore point."""
        point = RestorePoint(name, description)
        logger.info(f"Created restore point: {name}")
        return point

    @staticmethod
    def _restore_point_dir(restore_point: RestorePoint) -> Path:
        return Config.BACKUP_DIR / restore_point.timestamp

    @staticmethod
    def _restore_point_manifest_path(restore_point: RestorePoint) -> Path:
        return RestoreManager._restore_point_dir(restore_point) / "restore-point.json"

    @staticmethod
    def _registry_backup_path(restore_point: RestorePoint) -> Path:
        return RestoreManager._restore_point_dir(restore_point) / "registry-backup.json"

    @staticmethod
    def _settings_backup_path(restore_point: RestorePoint) -> Path:
        return RestoreManager._restore_point_dir(restore_point) / "settings-backup.json"

    @staticmethod
    def save_restore_point(restore_point: RestorePoint) -> bool:
        try:
            restore_dir = RestoreManager._restore_point_dir(restore_point)
            restore_dir.mkdir(parents=True, exist_ok=True)
            RestoreManager._restore_point_manifest_path(restore_point).write_text(
                json.dumps(restore_point.to_dict(), indent=2),
                encoding="utf-8",
            )
            if restore_point.registry:
                RestoreManager._registry_backup_path(restore_point).write_text(
                    json.dumps(restore_point.registry, indent=2),
                    encoding="utf-8",
                )
            if restore_point.settings:
                RestoreManager._settings_backup_path(restore_point).write_text(
                    json.dumps(restore_point.settings, indent=2),
                    encoding="utf-8",
                )
            return True
        except Exception as e:
            logger.error(f"Failed to save restore point {restore_point.name}: {e}")
            return False

    @staticmethod
    def backup_power_plan(restore_point: RestorePoint, new_scheme_guid: str) -> bool:
        """Backup the active Windows power plan before switching plans."""
        try:
            old_guid, old_name = RestoreManager._get_active_power_plan()
            if not old_guid:
                logger.error("Failed to backup power plan because active scheme could not be read.")
                return False

            restore_point.settings.append({
                "type": "power_plan",
                "old_scheme_guid": old_guid,
                "old_scheme_name": old_name,
                "new_scheme_guid": new_scheme_guid,
                "timestamp": datetime.now().isoformat(timespec="seconds"),
            })
            RestoreManager.save_restore_point(restore_point)
            logger.info("Backed up active power plan: %s", old_name or old_guid)
            return True
        except Exception as e:
            logger.error("Failed to backup power plan: %s", e)
            return False

    @staticmethod
    def _get_active_power_plan() -> tuple[str, str]:
        try:
            output = subprocess.check_output(
                ["powercfg", "/getactivescheme"],
                text=True,
                stderr=subprocess.DEVNULL,
                timeout=3,
            )
            match = re.search(r"([0-9a-fA-F-]{36})(?:\s+\((.*?)\))?", output or "")
            if not match:
                return "", ""
            return match.group(1), match.group(2) or ""
        except Exception:
            return "", ""
    
    @staticmethod
    def backup_file(source: Path, restore_point: RestorePoint) -> bool:
        """Backup a file for a restore point."""
        try:
            backup_path = Config.BACKUP_DIR / restore_point.timestamp / source.name
            backup_path.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, backup_path)
            restore_point.files[str(source)] = str(backup_path)
            RestoreManager.save_restore_point(restore_point)
            logger.info(f"Backed up file: {source}")
            return True
        except Exception as e:
            logger.error(f"Failed to backup file {source}: {e}")
            return False
    
    @staticmethod
    def backup_registry(
        restore_point: RestorePoint,
        hkey,
        path: str,
        key: str,
        new_value: Any,
        new_value_type=winreg.REG_SZ,
    ) -> bool:
        """Backup a registry value before it is changed."""
        try:
            old_value_exists, old_value, old_value_type = RestoreManager._read_registry_value(hkey, path, key)
            entry = {
                "hive": RestoreManager._HKEY_NAMES.get(hkey, str(hkey)),
                "path": path,
                "key": key,
                "type": RestoreManager._REG_TYPE_NAMES.get(
                    old_value_type if old_value_exists else new_value_type,
                    str(old_value_type if old_value_exists else new_value_type),
                ),
                "old_value_exists": old_value_exists,
                "old_value": RestoreManager._serialize_registry_value(old_value),
                "old_type": RestoreManager._REG_TYPE_NAMES.get(old_value_type, str(old_value_type)) if old_value_exists else None,
                "new_value": RestoreManager._serialize_registry_value(new_value),
                "new_type": RestoreManager._REG_TYPE_NAMES.get(new_value_type, str(new_value_type)),
                "timestamp": datetime.now().isoformat(timespec="seconds"),
            }
            restore_point.registry.append(entry)
            RestoreManager.save_restore_point(restore_point)
            logger.info(
                "Backed up registry value %s at %s\\%s",
                key,
                entry["hive"],
                path,
            )
            return True
        except Exception as e:
            logger.error(f"Failed to backup registry value {key}: {e}")
            return False

    @staticmethod
    def _read_registry_value(hkey, path: str, key: str) -> tuple[bool, Any, Optional[int]]:
        try:
            reg_key = winreg.OpenKey(hkey, path, 0, winreg.KEY_READ)
            try:
                value, value_type = winreg.QueryValueEx(reg_key, key)
                return True, value, value_type
            finally:
                winreg.CloseKey(reg_key)
        except FileNotFoundError:
            return False, None, None
        except OSError:
            return False, None, None

    @staticmethod
    def _serialize_registry_value(value: Any) -> Any:
        if isinstance(value, bytes):
            return {"encoding": "hex", "data": value.hex()}
        return value

    @staticmethod
    def _deserialize_registry_value(value: Any) -> Any:
        if isinstance(value, dict) and value.get("encoding") == "hex":
            return bytes.fromhex(value.get("data", ""))
        return value

    @staticmethod
    def _resolve_hkey(hive: str):
        return RestoreManager._HKEY_BY_NAME[hive]

    @staticmethod
    def _resolve_reg_type(type_name: Optional[str]) -> int:
        if not type_name:
            return winreg.REG_SZ
        return RestoreManager._REG_TYPE_BY_NAME.get(type_name, winreg.REG_SZ)

    @staticmethod
    def _restore_registry_entry(entry: Dict[str, Any]) -> bool:
        hive = entry.get("hive", "")
        path = entry.get("path", "")
        key = entry.get("key", "")
        if not hive or not path or not key:
            return False

        hkey = RestoreManager._resolve_hkey(hive)
        if not entry.get("old_value_exists", False):
            try:
                reg_key = winreg.OpenKey(hkey, path, 0, winreg.KEY_SET_VALUE)
                try:
                    winreg.DeleteValue(reg_key, key)
                finally:
                    winreg.CloseKey(reg_key)
                logger.info("Deleted registry value created by tweak: %s at %s\\%s", key, hive, path)
                return True
            except FileNotFoundError:
                return True
            except OSError as e:
                logger.error("Failed to delete registry value %s at %s\\%s: %s", key, hive, path, e)
                return False

        try:
            value = RestoreManager._deserialize_registry_value(entry.get("old_value"))
            value_type = RestoreManager._resolve_reg_type(entry.get("old_type") or entry.get("type"))
            reg_key = winreg.CreateKeyEx(hkey, path, 0, winreg.KEY_WRITE)
            try:
                winreg.SetValueEx(reg_key, key, 0, value_type, value)
            finally:
                winreg.CloseKey(reg_key)
            logger.info("Restored registry value %s at %s\\%s", key, hive, path)
            return True
        except Exception as e:
            logger.error("Failed to restore registry value %s at %s\\%s: %s", key, hive, path, e)
            return False

    @staticmethod
    def _restore_setting_entry(entry: Dict[str, Any]) -> bool:
        setting_type = entry.get("type", "")
        if setting_type != "power_plan":
            return True

        old_scheme_guid = entry.get("old_scheme_guid", "")
        if not old_scheme_guid:
            return False

        try:
            from utils.shell import ShellUtil
            success, output = ShellUtil.execute_command(f"powercfg /setactive {old_scheme_guid}", admin=True)
            if not success:
                logger.error("Failed to restore power plan: %s", output)
                return False
            logger.info("Restored power plan: %s", entry.get("old_scheme_name") or old_scheme_guid)
            return True
        except Exception as e:
            logger.error("Failed to restore power plan: %s", e)
            return False

    @staticmethod
    def find_latest_restore_point(name: str) -> Optional[RestorePoint]:
        try:
            if not Config.BACKUP_DIR.exists():
                return None

            candidates: List[RestorePoint] = []
            for manifest in Config.BACKUP_DIR.glob("*/restore-point.json"):
                try:
                    point = RestorePoint.from_dict(json.loads(manifest.read_text(encoding="utf-8")))
                    if point.name == name:
                        candidates.append(point)
                except Exception:
                    continue

            return sorted(candidates, key=lambda point: point.timestamp, reverse=True)[0] if candidates else None
        except Exception as e:
            logger.error(f"Failed to locate restore point {name}: {e}")
            return None

    @staticmethod
    def find_restore_point_by_timestamp(timestamp: str) -> Optional[RestorePoint]:
        try:
            safe_timestamp = re.sub(r"[^0-9A-Za-z_.:-]", "", timestamp or "")
            if not safe_timestamp or not Config.BACKUP_DIR.exists():
                return None

            manifest = Config.BACKUP_DIR / safe_timestamp / "restore-point.json"
            if not manifest.exists():
                return None

            return RestorePoint.from_dict(json.loads(manifest.read_text(encoding="utf-8")))
        except Exception as e:
            logger.error(f"Failed to locate restore point timestamp {timestamp}: {e}")
            return None
    
    @staticmethod
    def restore(restore_point: RestorePoint) -> bool:
        """Restore from a restore point."""
        try:
            registry_results = [
                RestoreManager._restore_registry_entry(entry)
                for entry in reversed(restore_point.registry)
            ]
            setting_results = [
                RestoreManager._restore_setting_entry(entry)
                for entry in reversed(restore_point.settings)
            ]

            file_results = []
            for original, backup in restore_point.files.items():
                if str(original).startswith("reg:"):
                    continue

                backup_path = Path(backup)
                if backup_path.exists():
                    shutil.copy2(backup_path, original)
                    file_results.append(True)
                    logger.info(f"Restored file: {original}")
            
            logger.info(f"Restore point applied: {restore_point.name}")
            return all(registry_results) and all(setting_results) and all(file_results)
        except Exception as e:
            logger.error(f"Failed to restore from point: {e}")
            return False

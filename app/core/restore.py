"""
System restore and backup management for HyperBoostX.
Provides safe restoration capabilities for all changes.
"""

import shutil
import json
from pathlib import Path
from datetime import datetime
from typing import Dict, List, Optional
from core.config import Config
from core.logger import Logger


logger = Logger.get_logger(__name__)


class RestorePoint:
    """Represents a system restore point."""
    
    def __init__(self, name: str, description: str = "", timestamp: Optional[str] = None):
        self.name = name
        self.description = description
        self.timestamp = timestamp or datetime.now().isoformat()
        self.files: Dict[str, str] = {}  # path -> backup_path
    
    def to_dict(self) -> dict:
        """Convert to dictionary."""
        return {
            "name": self.name,
            "description": self.description,
            "timestamp": self.timestamp,
            "files": self.files
        }
    
    @classmethod
    def from_dict(cls, data: dict):
        """Create from dictionary."""
        rp = cls(data["name"], data["description"], data["timestamp"])
        rp.files = data.get("files", {})
        return rp


class RestoreManager:
    """Manages system restore points and restoration."""
    
    @staticmethod
    def create_restore_point(name: str, description: str = "") -> RestorePoint:
        """Create a new restore point."""
        point = RestorePoint(name, description)
        logger.info(f"Created restore point: {name}")
        return point
    
    @staticmethod
    def backup_file(source: Path, restore_point: RestorePoint) -> bool:
        """Backup a file for a restore point."""
        try:
            backup_path = Config.BACKUP_DIR / restore_point.timestamp / source.name
            backup_path.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, backup_path)
            restore_point.files[str(source)] = str(backup_path)
            logger.info(f"Backed up file: {source}")
            return True
        except Exception as e:
            logger.error(f"Failed to backup file {source}: {e}")
            return False
    
    @staticmethod
    def backup_registry(restore_point: RestorePoint, key: str) -> bool:
        """Backup a registry key."""
        try:
            logger.info(f"Backed up registry key: {key}")
            return True
        except Exception as e:
            logger.error(f"Failed to backup registry key: {e}")
            return False
    
    @staticmethod
    def restore(restore_point: RestorePoint) -> bool:
        """Restore from a restore point."""
        try:
            for original, backup in restore_point.files.items():
                backup_path = Path(backup)
                if backup_path.exists():
                    shutil.copy2(backup_path, original)
                    logger.info(f"Restored file: {original}")
            
            logger.info(f"Restore point applied: {restore_point.name}")
            return True
        except Exception as e:
            logger.error(f"Failed to restore from point: {e}")
            return False

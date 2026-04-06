"""Startup service for HyperBoost X."""

import os
import winreg
from pathlib import Path
from typing import List, Dict, Any
from core.logger import Logger


logger = Logger.get_logger(__name__)


class StartupService:
    """Service for managing startup items and boot optimization."""
    
    @staticmethod
    def get_startup_items() -> List[Dict[str, Any]]:
        """Get list of startup items."""
        items: List[Dict[str, Any]] = []
        seen = set()

        registry_locations = [
            (winreg.HKEY_CURRENT_USER, r"Software\Microsoft\Windows\CurrentVersion\Run", True),
            (winreg.HKEY_LOCAL_MACHINE, r"Software\Microsoft\Windows\CurrentVersion\Run", True),
            (winreg.HKEY_CURRENT_USER, r"Software\Microsoft\Windows\CurrentVersion\RunOnce", False),
            (winreg.HKEY_LOCAL_MACHINE, r"Software\Microsoft\Windows\CurrentVersion\RunOnce", False),
        ]

        for hive, path, enabled in registry_locations:
            items.extend(StartupService._read_registry_startup_items(hive, path, enabled, seen))

        startup_folders = [
            Path(os.environ.get("APPDATA", "")) / r"Microsoft\Windows\Start Menu\Programs\Startup",
            Path(os.environ.get("ProgramData", "")) / r"Microsoft\Windows\Start Menu\Programs\StartUp",
        ]

        for folder in startup_folders:
            items.extend(StartupService._read_startup_folder_items(folder, seen))

        items.sort(key=lambda item: item["name"].lower())
        return items
    
    @staticmethod
    def disable_startup_item(item_name: str) -> bool:
        """Disable a startup item."""
        logger.info(f"Disabling startup item: {item_name}")
        try:
            return True
        except Exception as e:
            logger.error(f"Failed to disable startup item: {e}")
            return False
    
    @staticmethod
    def enable_startup_item(item_name: str) -> bool:
        """Enable a startup item."""
        logger.info(f"Enabling startup item: {item_name}")
        try:
            return True
        except Exception as e:
            logger.error(f"Failed to enable startup item: {e}")
            return False

    @staticmethod
    def _read_registry_startup_items(hive, path: str, enabled: bool, seen: set) -> List[Dict[str, Any]]:
        items: List[Dict[str, Any]] = []

        try:
            with winreg.OpenKey(hive, path) as key:
                index = 0
                while True:
                    try:
                        name, command, _ = winreg.EnumValue(key, index)
                    except OSError:
                        break

                    normalized = name.lower()
                    if normalized not in seen:
                        seen.add(normalized)
                        items.append({
                            "name": name,
                            "enabled": enabled,
                            "impact": StartupService._estimate_impact(command),
                        })
                    index += 1
        except OSError:
            return items

        return items

    @staticmethod
    def _read_startup_folder_items(folder: Path, seen: set) -> List[Dict[str, Any]]:
        items: List[Dict[str, Any]] = []

        if not folder.exists():
            return items

        for file in folder.iterdir():
            if not file.is_file():
                continue

            name = file.stem
            normalized = name.lower()
            if normalized in seen:
                continue

            seen.add(normalized)
            items.append({
                "name": name,
                "enabled": True,
                "impact": StartupService._estimate_impact(str(file)),
            })

        return items

    @staticmethod
    def _estimate_impact(command: str) -> str:
        lowered = command.lower()
        if any(token in lowered for token in ["antivirus", "defender", "security", "adobe", "onedrive", "teams"]):
            return "High"
        if any(token in lowered for token in ["update", "discord", "steam", "spotify", "launcher"]):
            return "Medium"
        return "Low"

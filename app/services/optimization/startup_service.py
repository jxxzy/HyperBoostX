"""Startup service for HyperBoost X."""

import csv
import io
import json
import os
import re
import subprocess
import winreg
from pathlib import Path
from typing import List, Dict, Any, Optional

import psutil

from core.logger import Logger


logger = Logger.get_logger(__name__)


class StartupService:
    """Service for managing startup items and boot optimization."""

    SAFE_DISABLE_TOKENS = [
        "onedrive", "teams", "widgets", "spotify", "discord", "steam",
        "adobe", "launcher", "update", "updater", "epic", "dropbox"
    ]

    KEEP_ENABLED_TOKENS = [
        "defender", "security", "realtek", "audio", "synaptics", "touchpad",
        "nvidia", "amd", "intel", "antivirus", "razer", "lghub", "driver"
    ]

    @staticmethod
    def get_startup_items() -> List[Dict[str, Any]]:
        """Get list of startup items from registry, startup folders, tasks, and services."""
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

        items.extend(StartupService._read_scheduled_tasks(seen))
        items.extend(StartupService._read_startup_services(seen))

        items.sort(key=lambda item: (item["source"], item["name"].lower()))
        return items

    @staticmethod
    def disable_startup_item(item_name: str) -> bool:
        logger.info(f"Disabling startup item: {item_name}")
        return True

    @staticmethod
    def enable_startup_item(item_name: str) -> bool:
        logger.info(f"Enabling startup item: {item_name}")
        return True

    @staticmethod
    def _read_registry_startup_items(hive, path: str, enabled: bool, seen: set) -> List[Dict[str, Any]]:
        items: List[Dict[str, Any]] = []
        hive_name = "HKCU" if hive == winreg.HKEY_CURRENT_USER else "HKLM"

        try:
            with winreg.OpenKey(hive, path) as key:
                index = 0
                while True:
                    try:
                        name, command, _ = winreg.EnumValue(key, index)
                    except OSError:
                        break

                    normalized = f"registry::{name}".lower()
                    if normalized not in seen:
                        seen.add(normalized)
                        items.append(
                            StartupService._build_entry(
                                name=name,
                                enabled=enabled,
                                source="Registry",
                                source_detail=f"{hive_name}\\{path}",
                                item_type=StartupService._detect_startup_type(name, command),
                                command=command,
                            )
                        )
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
            normalized = f"startupfolder::{name}".lower()
            if normalized in seen:
                continue

            seen.add(normalized)
            items.append(
                StartupService._build_entry(
                    name=name,
                    enabled=True,
                    source="Startup Folder",
                    source_detail=str(folder),
                    item_type=StartupService._detect_startup_type(name, str(file)),
                    command=str(file),
                )
            )

        return items

    @staticmethod
    def _read_scheduled_tasks(seen: set) -> List[Dict[str, Any]]:
        items: List[Dict[str, Any]] = []
        try:
            output = subprocess.check_output(
                [
                    "schtasks",
                    "/query",
                    "/fo",
                    "csv",
                    "/v",
                ],
                text=True,
                stderr=subprocess.DEVNULL,
                encoding="utf-8",
                errors="ignore",
            )
            reader = csv.DictReader(io.StringIO(output))
            for row in reader:
                task_name = (row.get("TaskName") or "").strip()
                schedule = (row.get("Schedule Type") or "").strip()
                status = (row.get("Status") or "").strip()
                task_to_run = (row.get("Task To Run") or "").strip()

                if not task_name or "logon" not in schedule.lower():
                    continue

                normalized = f"task::{task_name}".lower()
                if normalized in seen:
                    continue

                seen.add(normalized)
                items.append(
                    StartupService._build_entry(
                        name=task_name.split("\\")[-1],
                        enabled=status.lower() != "disabled",
                        source="Task Scheduler",
                        source_detail=task_name,
                        item_type="Scheduled Task",
                        command=task_to_run or task_name,
                    )
                )
        except Exception as exc:
            logger.warning(f"Unable to read scheduled tasks with schtasks: {exc}")
            items.extend(StartupService._read_scheduled_tasks_powershell(seen))
            if not items:
                items.extend(StartupService._read_scheduled_tasks_basic(seen))

        return items

    @staticmethod
    def _read_scheduled_tasks_powershell(seen: set) -> List[Dict[str, Any]]:
        items: List[Dict[str, Any]] = []
        script = (
            "Get-ScheduledTask | Where-Object { $_.Triggers | Where-Object { $_.TriggerType -in @('Logon','Startup') } } | "
            "Select-Object TaskName, TaskPath, State, @{Name='Execute';Expression={ ($_.Actions | Select-Object -ExpandProperty Execute -ErrorAction SilentlyContinue) -join '; ' }} | "
            "ConvertTo-Json -Depth 4 -Compress"
        )
        try:
            output = subprocess.check_output(
                ["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script],
                text=True,
                stderr=subprocess.DEVNULL,
                encoding="utf-8",
                errors="ignore",
            ).strip()
            if not output:
                return items

            tasks = json.loads(output)
            if isinstance(tasks, dict):
                tasks = [tasks]

            for task in tasks:
                task_name = (task.get("TaskName") or "").strip()
                task_path = (task.get("TaskPath") or "").strip()
                state = (task.get("State") or "").strip()
                execute = (task.get("Execute") or "").strip()
                if not task_name:
                    continue

                normalized = f"task::{task_path}{task_name}".lower()
                if normalized in seen:
                    continue

                seen.add(normalized)
                items.append(
                    StartupService._build_entry(
                        name=task_name,
                        enabled=state.lower() != "disabled",
                        source="Task Scheduler",
                        source_detail=f"{task_path}{task_name}",
                        item_type="Scheduled Task",
                        command=execute or task_name,
                    )
                )
        except Exception as exc:
            logger.warning(f"Unable to read scheduled tasks with PowerShell fallback: {exc}")

        return items

    @staticmethod
    def _read_scheduled_tasks_basic(seen: set) -> List[Dict[str, Any]]:
        items: List[Dict[str, Any]] = []
        try:
            process = subprocess.run(
                ["schtasks", "/query", "/fo", "csv"],
                text=True,
                capture_output=True,
                encoding="utf-8",
                errors="ignore",
            )
            output = process.stdout
            reader = csv.DictReader(io.StringIO(output))
            for row in reader:
                task_name = (row.get("TaskName") or "").strip()
                status = (row.get("Status") or "").strip()
                if not task_name or task_name.startswith("\\Microsoft\\Windows\\"):
                    continue

                normalized = f"task::{task_name}".lower()
                if normalized in seen:
                    continue

                seen.add(normalized)
                items.append(
                    StartupService._build_entry(
                        name=task_name.split("\\")[-1],
                        enabled=status.lower() != "disabled",
                        source="Task Scheduler",
                        source_detail=task_name,
                        item_type="Scheduled Task",
                        command=task_name,
                    )
                )
        except Exception as exc:
            logger.warning(f"Unable to read scheduled tasks with basic schtasks fallback: {exc}")

        return items

    @staticmethod
    def _read_startup_services(seen: set) -> List[Dict[str, Any]]:
        items: List[Dict[str, Any]] = []
        services_root = r"SYSTEM\CurrentControlSet\Services"
        try:
            with winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, services_root) as root:
                service_count = winreg.QueryInfoKey(root)[0]
                for index in range(service_count):
                    try:
                        service_name = winreg.EnumKey(root, index)
                        with winreg.OpenKey(root, service_name) as svc_key:
                            start_type = StartupService._safe_query_value(svc_key, "Start")
                            image_path = StartupService._safe_query_value(svc_key, "ImagePath", "")
                            display_name = StartupService._safe_query_value(svc_key, "DisplayName", service_name)
                            service_type = StartupService._safe_query_value(svc_key, "Type")
                    except OSError:
                        continue

                    if start_type not in (2, 3):
                        continue

                    if not service_type or not (service_type & 0x10 or service_type & 0x20):
                        continue

                    normalized = f"service::{service_name}".lower()
                    if normalized in seen:
                        continue

                    seen.add(normalized)
                    items.append(
                        StartupService._build_entry(
                            name=display_name,
                            enabled=start_type == 2,
                            source="Services",
                            source_detail=service_name,
                            item_type="Service",
                            command=image_path or service_name,
                        )
                    )
        except OSError as exc:
            logger.warning(f"Unable to read startup services: {exc}")

        return items

    @staticmethod
    def _safe_query_value(key, value_name: str, default=None):
        try:
            return winreg.QueryValueEx(key, value_name)[0]
        except OSError:
            return default

    @staticmethod
    def _build_entry(name: str, enabled: bool, source: str, source_detail: str, item_type: str, command: str) -> Dict[str, Any]:
        metrics = StartupService._estimate_metrics(name, command)
        lowered = f"{name} {command}".lower()
        recommended_action = "Keep Enabled"
        if any(token in lowered for token in StartupService.SAFE_DISABLE_TOKENS):
            recommended_action = "Recommended to Disable"
        elif source == "Services" and item_type == "Service":
            recommended_action = "Review Carefully"

        return {
            "name": name,
            "enabled": enabled,
            "impact": metrics["impact"],
            "impact_score": metrics["impact_score"],
            "estimated_memory_mb": metrics["estimated_memory_mb"],
            "estimated_load_time_s": metrics["estimated_load_time_s"],
            "source": source,
            "source_detail": source_detail,
            "type": item_type,
            "command": command,
            "recommended_action": recommended_action,
        }

    @staticmethod
    def _estimate_metrics(name: str, command: str) -> Dict[str, Any]:
        lowered = f"{name} {command}".lower()
        executable_path = StartupService._extract_executable_path(command)
        file_size_mb = 0.0
        running_memory_mb = 0.0

        if executable_path and os.path.exists(executable_path):
            try:
                file_size_mb = os.path.getsize(executable_path) / (1024 * 1024)
            except OSError:
                file_size_mb = 0.0

        process_hint = Path(executable_path).stem.lower() if executable_path else name.lower()
        for proc in psutil.process_iter(["name", "memory_info"]):
            try:
                proc_name = (proc.info["name"] or "").lower()
                if process_hint and process_hint in proc_name:
                    running_memory_mb = max(running_memory_mb, (proc.info["memory_info"].rss or 0) / (1024 * 1024))
            except (psutil.NoSuchProcess, psutil.AccessDenied, AttributeError):
                continue

        score = 10
        if any(token in lowered for token in ["defender", "security", "adobe", "onedrive", "teams", "obs", "discord", "steam", "launcher"]):
            score += 25
        if any(token in lowered for token in ["driver", "service", "nvidia", "amd", "intel", "audio"]):
            score += 20

        score += min(int(file_size_mb / 5), 20)
        score += min(int(running_memory_mb / 20), 25)

        estimated_load_time = round(max(0.5, (file_size_mb / 10.0) + (running_memory_mb / 100.0)), 1)

        if score >= 55:
            impact = "High"
        elif score >= 30:
            impact = "Medium"
        elif score > 0:
            impact = "Low"
        else:
            impact = "Unknown"

        return {
            "impact": impact,
            "impact_score": score,
            "estimated_memory_mb": round(running_memory_mb or max(file_size_mb * 0.6, 0.0), 1),
            "estimated_load_time_s": estimated_load_time,
        }

    @staticmethod
    def _extract_executable_path(command: str) -> Optional[str]:
        if not command:
            return None

        quoted_match = re.match(r'^"([^"]+\.exe)"', command, re.IGNORECASE)
        if quoted_match:
            return quoted_match.group(1)

        plain_match = re.match(r"^([^\s]+\.exe)", command, re.IGNORECASE)
        if plain_match:
            return plain_match.group(1)

        return None

    @staticmethod
    def _detect_startup_type(name: str, command: str) -> str:
        lowered = f"{name} {command}".lower()

        if any(token in lowered for token in StartupService.KEEP_ENABLED_TOKENS):
            return "System"

        if any(token in lowered for token in ["update", "updater", "launcher", "helper"]):
            return "Utility"

        return "App"

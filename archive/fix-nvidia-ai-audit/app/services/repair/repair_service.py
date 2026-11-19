"""Repair service for HyperBoost X."""

import os
import shutil
import tempfile
from typing import Any, Dict, Iterable, List
from core.logger import Logger
from utils.shell import ShellUtil

logger = Logger.get_logger(__name__)


class RepairService:
    """Service for system repair and maintenance."""

    SAFE_LOG_EXTENSIONS = {".etl", ".evtx", ".log", ".txt", ".cab", ".dmp", ".tmp"}
    
    @staticmethod
    def run_sfc() -> Dict[str, Any]:
        """Run System File Checker."""
        logger.info("Running System File Checker")
        success, output = ShellUtil.execute_command("sfc /scannow", admin=True)
        return {
            "success": success,
            "command": "sfc /scannow",
            "output": output
        }
    
    @staticmethod
    def run_dism() -> Dict[str, Any]:
        """Run DISM image repair."""
        logger.info("Running DISM RestoreHealth")
        success, output = ShellUtil.execute_command(
            "DISM /Online /Cleanup-Image /RestoreHealth",
            admin=True
        )
        return {
            "success": success,
            "command": "DISM /Online /Cleanup-Image /RestoreHealth",
            "output": output
        }
    
    @staticmethod
    def cleanup_temp_files(scope: str = "safe_all") -> Dict[str, Any]:
        """Clean cleanup targets based on the requested scope."""
        normalized_scope = (scope or "safe_all").strip().lower()
        logger.info("Running cleanup scope: %s", normalized_scope)

        categories = RepairService._build_cleanup_categories(normalized_scope)
        total_bytes = sum(item["freed_bytes"] for item in categories.values())
        total_files = sum(item["deleted_files"] for item in categories.values())
        total_dirs = sum(item["deleted_directories"] for item in categories.values())
        freed_mb = int(total_bytes / (1024 * 1024))
        summary = [
            f"{item['name']} {item['freed_mb']} MB"
            for item in categories.values()
            if item["freed_bytes"] > 0 or item["deleted_files"] > 0 or item["deleted_directories"] > 0
        ]
        if not summary:
            summary = ["No removable files were found for this cleanup scope."]

        report = {
            "success": True,
            "scope": normalized_scope,
            "freed_bytes": total_bytes,
            "freed_mb": freed_mb,
            "deleted_files": total_files,
            "deleted_directories": total_dirs,
            "categories": categories,
            "summary": summary,
        }
        logger.info(
            "Cleanup scope %s completed: %s MB freed across %s files and %s directories",
            normalized_scope,
            freed_mb,
            total_files,
            total_dirs,
        )
        return report

    @staticmethod
    def _build_cleanup_categories(scope: str) -> Dict[str, Dict[str, Any]]:
        builders = RepairService._cleanup_scope_builders()
        selected = builders.get(scope) or builders["safe_all"]
        return {name: build() for name, build in selected}

    @staticmethod
    def _cleanup_scope_builders() -> Dict[str, List[tuple[str, Any]]]:
        return {
            "safe_all": [
                ("temp_files", RepairService._cleanup_temp_targets),
                ("browser_cache", RepairService._cleanup_browser_cache_targets),
                ("logs_and_reports", RepairService._cleanup_log_targets),
            ],
            "junk_files": [
                ("temp_files", RepairService._cleanup_temp_targets),
                ("browser_cache", RepairService._cleanup_browser_cache_targets),
                ("logs_and_reports", RepairService._cleanup_log_targets),
            ],
            "temp_files": [("temp_files", RepairService._cleanup_temp_targets)],
            "system_cache": [
                ("windows_temp", RepairService._cleanup_windows_temp_targets),
                ("thumbnail_cache", RepairService._cleanup_thumbnail_cache_targets),
                ("prefetch_files", RepairService._cleanup_prefetch_targets),
            ],
            "browser_cache": [("browser_cache", RepairService._cleanup_browser_cache_targets)],
            "browser_cookies": [("browser_cookies", RepairService._cleanup_browser_cookie_targets)],
            "browser_history": [("browser_history", RepairService._cleanup_browser_history_targets)],
            "browser_downloads": [("browser_downloads", RepairService._cleanup_browser_download_targets)],
            "browser_sessions": [("browser_sessions", RepairService._cleanup_browser_session_targets)],
            "advanced_system_files": [("system_files", RepairService._cleanup_system_file_targets)],
            "advanced_windows_temp": [("windows_temp", RepairService._cleanup_windows_temp_targets)],
            "advanced_prefetch": [("prefetch_files", RepairService._cleanup_prefetch_targets)],
            "advanced_update_cache": [("windows_update_cache", RepairService._cleanup_windows_update_targets)],
            "advanced_delivery_opt": [("delivery_optimization", RepairService._cleanup_delivery_optimization_targets)],
            "advanced_logs": [("logs_and_reports", RepairService._cleanup_log_targets)],
            "advanced_user_temp": [("user_temp", RepairService._cleanup_user_temp_targets)],
            "advanced_recent_files": [("recent_files", RepairService._cleanup_recent_files_targets)],
            "advanced_thumbnail": [("thumbnail_cache", RepairService._cleanup_thumbnail_cache_targets)],
            "advanced_app_cache": [("application_cache", RepairService._cleanup_application_cache_targets)],
            "deep_cleanup": [
                ("temp_files", RepairService._cleanup_temp_targets),
                ("browser_cache", RepairService._cleanup_browser_cache_targets),
                ("logs_and_reports", RepairService._cleanup_log_targets),
                ("windows_update_cache", RepairService._cleanup_windows_update_targets),
                ("delivery_optimization", RepairService._cleanup_delivery_optimization_targets),
                ("prefetch_files", RepairService._cleanup_prefetch_targets),
                ("thumbnail_cache", RepairService._cleanup_thumbnail_cache_targets),
                ("recent_files", RepairService._cleanup_recent_files_targets),
            ],
        }

    @staticmethod
    def _cleanup_temp_targets() -> Dict[str, Any]:
        return RepairService._cleanup_group(
            "temp_files",
            [tempfile.gettempdir(), os.path.join(os.environ.get("SystemRoot", r"C:\Windows"), "Temp")],
        )

    @staticmethod
    def _cleanup_user_temp_targets() -> Dict[str, Any]:
        return RepairService._cleanup_group("user_temp", [tempfile.gettempdir()])

    @staticmethod
    def _cleanup_windows_temp_targets() -> Dict[str, Any]:
        return RepairService._cleanup_group(
            "windows_temp",
            [os.path.join(os.environ.get("SystemRoot", r"C:\Windows"), "Temp")],
        )

    @staticmethod
    def _cleanup_prefetch_targets() -> Dict[str, Any]:
        windows_root = os.environ.get("SystemRoot", r"C:\Windows")
        return RepairService._cleanup_group("prefetch_files", [os.path.join(windows_root, "Prefetch")])

    @staticmethod
    def _cleanup_windows_update_targets() -> Dict[str, Any]:
        windows_root = os.environ.get("SystemRoot", r"C:\Windows")
        return RepairService._cleanup_group(
            "windows_update_cache",
            [os.path.join(windows_root, "SoftwareDistribution", "Download")],
        )

    @staticmethod
    def _cleanup_delivery_optimization_targets() -> Dict[str, Any]:
        program_data = os.environ.get("ProgramData", r"C:\ProgramData")
        return RepairService._cleanup_group(
            "delivery_optimization",
            [os.path.join(program_data, "Microsoft", "Windows", "DeliveryOptimization", "Cache")],
        )

    @staticmethod
    def _cleanup_log_targets() -> Dict[str, Any]:
        local = os.environ.get("LOCALAPPDATA", "")
        program_data = os.environ.get("ProgramData", r"C:\ProgramData")
        windows_root = os.environ.get("SystemRoot", r"C:\Windows")
        return RepairService._cleanup_group(
            "logs_and_reports",
            [
                {"path": os.path.join(program_data, "Microsoft", "Windows", "WER")},
                {"path": os.path.join(local, "CrashDumps")},
                {"path": os.path.join(windows_root, "Logs"), "allowed_extensions": RepairService.SAFE_LOG_EXTENSIONS},
            ],
        )

    @staticmethod
    def _cleanup_system_file_targets() -> Dict[str, Any]:
        windows_root = os.environ.get("SystemRoot", r"C:\Windows")
        local = os.environ.get("LOCALAPPDATA", "")
        return RepairService._cleanup_group(
            "system_files",
            [
                {"path": os.path.join(windows_root, "Logs"), "allowed_extensions": RepairService.SAFE_LOG_EXTENSIONS},
                {"path": os.path.join(local, "CrashDumps")},
            ],
        )

    @staticmethod
    def _cleanup_recent_files_targets() -> Dict[str, Any]:
        recent = os.path.join(os.environ.get("APPDATA", ""), "Microsoft", "Windows", "Recent")
        return RepairService._cleanup_group("recent_files", [recent])

    @staticmethod
    def _cleanup_thumbnail_cache_targets() -> Dict[str, Any]:
        explorer = os.path.join(os.environ.get("LOCALAPPDATA", ""), "Microsoft", "Windows", "Explorer")
        return RepairService._cleanup_group(
            "thumbnail_cache",
            [
                {
                    "path": explorer,
                    "patterns": ("thumbcache", "iconcache"),
                    "allowed_extensions": {".db"},
                }
            ],
        )

    @staticmethod
    def _cleanup_application_cache_targets() -> Dict[str, Any]:
        return RepairService._cleanup_group(
            "application_cache",
            [
                tempfile.gettempdir(),
                *RepairService._browser_cache_paths(),
            ],
        )

    @staticmethod
    def _cleanup_browser_cache_targets() -> Dict[str, Any]:
        return RepairService._cleanup_group("browser_cache", RepairService._browser_cache_paths())

    @staticmethod
    def _cleanup_browser_cookie_targets() -> Dict[str, Any]:
        return RepairService._cleanup_group("browser_cookies", RepairService._browser_cookie_files())

    @staticmethod
    def _cleanup_browser_history_targets() -> Dict[str, Any]:
        return RepairService._cleanup_group("browser_history", RepairService._browser_history_files())

    @staticmethod
    def _cleanup_browser_download_targets() -> Dict[str, Any]:
        return RepairService._cleanup_group("browser_downloads", RepairService._browser_download_files())

    @staticmethod
    def _cleanup_browser_session_targets() -> Dict[str, Any]:
        return RepairService._cleanup_group("browser_sessions", RepairService._browser_session_targets())

    @staticmethod
    def _browser_cache_paths() -> List[str]:
        local = os.environ.get("LOCALAPPDATA", "")
        paths: List[str] = [
            os.path.join(local, "Google", "Chrome", "User Data", "Default", "Cache"),
            os.path.join(local, "Google", "Chrome", "User Data", "Default", "Code Cache"),
            os.path.join(local, "Google", "Chrome", "User Data", "Default", "GPUCache"),
            os.path.join(local, "Microsoft", "Edge", "User Data", "Default", "Cache"),
            os.path.join(local, "Microsoft", "Edge", "User Data", "Default", "Code Cache"),
            os.path.join(local, "Microsoft", "Edge", "User Data", "Default", "GPUCache"),
        ]
        for profile_root in RepairService._firefox_profile_roots():
            paths.extend(
                [
                    os.path.join(profile_root, "cache2"),
                    os.path.join(profile_root, "startupCache"),
                    os.path.join(profile_root, "jumpListCache"),
                    os.path.join(profile_root, "shader-cache"),
                ]
            )
        return paths

    @staticmethod
    def _browser_cookie_files() -> List[str]:
        local = os.environ.get("LOCALAPPDATA", "")
        files = [
            os.path.join(local, "Google", "Chrome", "User Data", "Default", "Cookies"),
            os.path.join(local, "Google", "Chrome", "User Data", "Default", "Cookies-journal"),
            os.path.join(local, "Microsoft", "Edge", "User Data", "Default", "Cookies"),
            os.path.join(local, "Microsoft", "Edge", "User Data", "Default", "Cookies-journal"),
        ]
        for profile_root in RepairService._firefox_profile_roots():
            files.extend(
                [
                    os.path.join(profile_root, "cookies.sqlite"),
                    os.path.join(profile_root, "cookies.sqlite-shm"),
                    os.path.join(profile_root, "cookies.sqlite-wal"),
                ]
            )
        return files

    @staticmethod
    def _browser_history_files() -> List[str]:
        local = os.environ.get("LOCALAPPDATA", "")
        return [
            os.path.join(local, "Google", "Chrome", "User Data", "Default", "History"),
            os.path.join(local, "Google", "Chrome", "User Data", "Default", "History-journal"),
            os.path.join(local, "Google", "Chrome", "User Data", "Default", "Visited Links"),
            os.path.join(local, "Microsoft", "Edge", "User Data", "Default", "History"),
            os.path.join(local, "Microsoft", "Edge", "User Data", "Default", "History-journal"),
            os.path.join(local, "Microsoft", "Edge", "User Data", "Default", "Visited Links"),
        ]

    @staticmethod
    def _browser_download_files() -> List[str]:
        local = os.environ.get("LOCALAPPDATA", "")
        return [
            os.path.join(local, "Google", "Chrome", "User Data", "Default", "DownloadMetadata"),
            os.path.join(local, "Google", "Chrome", "User Data", "Default", "History"),
            os.path.join(local, "Google", "Chrome", "User Data", "Default", "History-journal"),
            os.path.join(local, "Microsoft", "Edge", "User Data", "Default", "DownloadMetadata"),
            os.path.join(local, "Microsoft", "Edge", "User Data", "Default", "History"),
            os.path.join(local, "Microsoft", "Edge", "User Data", "Default", "History-journal"),
        ]

    @staticmethod
    def _browser_session_targets() -> List[Any]:
        local = os.environ.get("LOCALAPPDATA", "")
        targets: List[Any] = [
            os.path.join(local, "Google", "Chrome", "User Data", "Default", "Sessions"),
            os.path.join(local, "Google", "Chrome", "User Data", "Default", "Session Storage"),
            os.path.join(local, "Microsoft", "Edge", "User Data", "Default", "Sessions"),
            os.path.join(local, "Microsoft", "Edge", "User Data", "Default", "Session Storage"),
        ]
        for profile_root in RepairService._firefox_profile_roots():
            targets.extend(
                [
                    os.path.join(profile_root, "sessionstore.jsonlz4"),
                    os.path.join(profile_root, "sessionstore-backups"),
                ]
            )
        return targets

    @staticmethod
    def _firefox_profile_roots() -> List[str]:
        profiles_root = os.path.join(os.environ.get("APPDATA", ""), "Mozilla", "Firefox", "Profiles")
        if not os.path.isdir(profiles_root):
            return []
        try:
            return [
                os.path.join(profiles_root, profile)
                for profile in os.listdir(profiles_root)
                if os.path.isdir(os.path.join(profiles_root, profile))
            ]
        except Exception:
            return []

    @staticmethod
    def _cleanup_group(name: str, targets: Iterable[Any]) -> Dict[str, Any]:
        freed_bytes = 0
        deleted_files = 0
        deleted_directories = 0
        touched_paths: List[str] = []

        for target in targets:
            if isinstance(target, dict):
                path = target.get("path", "")
                allowed_extensions = target.get("allowed_extensions")
                patterns = target.get("patterns")
            else:
                path = str(target)
                allowed_extensions = None
                patterns = None

            if not path:
                continue

            if os.path.isdir(path):
                cleaned = RepairService._cleanup_directory(
                    path,
                    allowed_extensions=allowed_extensions,
                    patterns=patterns,
                )
            elif os.path.isfile(path):
                cleaned = RepairService._cleanup_file(path)
            else:
                continue

            if cleaned["freed_bytes"] > 0 or cleaned["deleted_files"] > 0 or cleaned["deleted_directories"] > 0:
                touched_paths.append(path)
            freed_bytes += cleaned["freed_bytes"]
            deleted_files += cleaned["deleted_files"]
            deleted_directories += cleaned["deleted_directories"]

        return {
            "name": name,
            "freed_bytes": freed_bytes,
            "freed_mb": int(freed_bytes / (1024 * 1024)),
            "deleted_files": deleted_files,
            "deleted_directories": deleted_directories,
            "paths": touched_paths,
        }

    @staticmethod
    def _cleanup_directory(
        path: str,
        allowed_extensions: Iterable[str] | None = None,
        patterns: Iterable[str] | None = None,
    ) -> Dict[str, int]:
        freed_bytes = 0
        deleted_files = 0
        deleted_directories = 0
        allowed = {ext.lower() for ext in allowed_extensions} if allowed_extensions else None
        lowered_patterns = tuple(pattern.lower() for pattern in patterns) if patterns else None

        for root, dirs, files in os.walk(path, topdown=False):
            for name in files:
                file_path = os.path.join(root, name)
                extension = os.path.splitext(name)[1].lower()
                if allowed is not None and extension not in allowed:
                    continue
                if lowered_patterns is not None and not any(name.lower().startswith(pattern) for pattern in lowered_patterns):
                    continue
                try:
                    size = os.path.getsize(file_path)
                    os.remove(file_path)
                    freed_bytes += size
                    deleted_files += 1
                except Exception:
                    continue

            for name in dirs:
                dir_path = os.path.join(root, name)
                try:
                    if os.path.isdir(dir_path) and not os.listdir(dir_path):
                        os.rmdir(dir_path)
                        deleted_directories += 1
                except Exception:
                    continue

        return {
            "freed_bytes": freed_bytes,
            "deleted_files": deleted_files,
            "deleted_directories": deleted_directories,
        }

    @staticmethod
    def _cleanup_file(path: str) -> Dict[str, int]:
        try:
            size = os.path.getsize(path)
            os.remove(path)
            return {
                "freed_bytes": size,
                "deleted_files": 1,
                "deleted_directories": 0,
            }
        except Exception:
            return {
                "freed_bytes": 0,
                "deleted_files": 0,
                "deleted_directories": 0,
            }

    @staticmethod
    def reset_network() -> Dict[str, Any]:
        """Reset network and socket stacks."""
        logger.info("Resetting network stack")
        success1, output1 = ShellUtil.execute_command("netsh int ip reset", admin=True)
        success2, output2 = ShellUtil.execute_command("netsh winsock reset", admin=True)
        return {
            "success": success1 and success2,
            "commands": ["netsh int ip reset", "netsh winsock reset"],
            "outputs": [output1, output2]
        }

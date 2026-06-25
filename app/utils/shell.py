"""
Shell utilities for HyperBoost X.
Provides system shell command execution.
"""

import subprocess
import re
from typing import Tuple
from core.logger import Logger
from core.permissions import Permissions


logger = Logger.get_logger(__name__)


class ShellUtil:
    """Shell command execution utility."""

    DEFAULT_TIMEOUT_SECONDS = 45
    ALLOWED_COMMAND_PATTERNS = (
        re.compile(r"^powercfg\s+/setactive\s+[0-9a-fA-F-]{36}$", re.IGNORECASE),
        re.compile(r"^powercfg\s+/change\s+monitor-timeout-dc\s+(?:[1-9]\d{0,3}|0)$", re.IGNORECASE),
        re.compile(r"^ipconfig\s+/flushdns$", re.IGNORECASE),
        re.compile(r"^netsh\s+int(?:erface)?\s+tcp\s+set\s+global\s+(autotuninglevel=normal|chimney=disabled)$", re.IGNORECASE),
        re.compile(r"^netsh\s+int(?:erface)?\s+ip\s+reset$", re.IGNORECASE),
        re.compile(r"^netsh\s+winsock\s+reset$", re.IGNORECASE),
        re.compile(r"^sfc\s+/scannow$", re.IGNORECASE),
        re.compile(r"^dism\s+/online\s+/cleanup-image\s+/restorehealth$", re.IGNORECASE),
        re.compile(r"^stop-service\s+-name\s+sysmain$", re.IGNORECASE),
        re.compile(r"^write-output\s+['\"][^'\"]{1,120}['\"]$", re.IGNORECASE),
    )

    @staticmethod
    def _powershell_args(command: str) -> list[str]:
        return [
            'powershell',
            '-NoProfile',
            '-NonInteractive',
            '-ExecutionPolicy',
            'Bypass',
            '-Command',
            command,
        ]
    
    @staticmethod
    def _is_allowed(command: str) -> bool:
        normalized = " ".join((command or "").strip().split())
        return any(pattern.match(normalized) for pattern in ShellUtil.ALLOWED_COMMAND_PATTERNS)

    @staticmethod
    def _describe_command(command: str) -> str:
        normalized = " ".join((command or "").strip().split())
        if not normalized:
            return "empty command"

        lower = normalized.lower()
        if lower.startswith("powercfg"):
            return "powercfg setactive"
        if lower.startswith("ipconfig"):
            return "ipconfig flushdns"
        if lower.startswith("netsh"):
            return "netsh network setting"
        if lower.startswith("sfc"):
            return "sfc scan"
        if lower.startswith("dism"):
            return "dism restorehealth"
        if lower.startswith("stop-service"):
            return "service control"
        if lower.startswith("write-output"):
            return "write-output"

        return normalized.split()[0]

    @staticmethod
    def execute_command(command: str, admin: bool = False, timeout_seconds: int = DEFAULT_TIMEOUT_SECONDS) -> Tuple[bool, str]:
        """Execute shell command."""
        try:
            command_description = ShellUtil._describe_command(command)
            if not ShellUtil._is_allowed(command):
                logger.warning("Blocked non-allowlisted shell command: %s", command_description)
                return False, "Command is not allowed by HyperBoost X safety policy."

            if admin and not Permissions.is_admin():
                message = "This action requires administrator privileges. Run HyperBoost X as Administrator."
                logger.info("Admin command skipped without elevation: %s", command_description)
                return False, message

            process = subprocess.run(
                ShellUtil._powershell_args(command),
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True,
                timeout=max(1, int(timeout_seconds)),
            )
            success = process.returncode == 0
            stdout = process.stdout
            stderr = process.stderr
            
            if success:
                logger.info("Command executed: %s", command_description)
            else:
                stderr = (stderr or "").strip()
                stdout = (stdout or "").strip()
                details = stderr or stdout or "Command failed without output."
                logger.error("Command failed: %s - %s", command_description, details)
            
            output = (stdout or "").strip() if success else ((stderr or "").strip() or (stdout or "").strip())
            return success, output
        except subprocess.TimeoutExpired:
            return False, f"Command timed out after {timeout_seconds} seconds."
        except Exception as e:
            logger.error("Failed to execute shell command: %s", e)
            return False, str(e)
    
    @staticmethod
    def run_powershell(script: str, admin: bool = False) -> Tuple[bool, str]:
        """Run PowerShell script."""
        return ShellUtil.execute_command(script, admin)

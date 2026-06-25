"""
Shell utilities for HyperBoost X.
Provides system shell command execution.
"""

import subprocess
from typing import Tuple
from core.logger import Logger
from core.permissions import Permissions


logger = Logger.get_logger(__name__)


class ShellUtil:
    """Shell command execution utility."""

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
    def execute_command(command: str, admin: bool = False) -> Tuple[bool, str]:
        """Execute shell command."""
        try:
            if admin and not Permissions.is_admin():
                message = "This action requires administrator privileges. Run HyperBoost X as Administrator."
                logger.info(f"Admin command skipped without elevation: {command}")
                return False, message

            process = subprocess.Popen(
                ShellUtil._powershell_args(command),
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True
            )
            
            stdout, stderr = process.communicate()
            success = process.returncode == 0
            
            if success:
                logger.info(f"Command executed: {command}")
            else:
                stderr = (stderr or "").strip()
                stdout = (stdout or "").strip()
                details = stderr or stdout or "Command failed without output."
                logger.error(f"Command failed: {command} - {details}")
            
            output = (stdout or "").strip() if success else ((stderr or "").strip() or (stdout or "").strip())
            return success, output
        except Exception as e:
            logger.error(f"Failed to execute command: {e}")
            return False, str(e)
    
    @staticmethod
    def run_powershell(script: str, admin: bool = False) -> Tuple[bool, str]:
        """Run PowerShell script."""
        return ShellUtil.execute_command(script, admin)

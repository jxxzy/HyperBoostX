"""
Shell utilities for HyperBoost X.
Provides system shell command execution.
"""

import subprocess
from typing import Tuple, Optional
from core.logger import Logger


logger = Logger.get_logger(__name__)


class ShellUtil:
    """Shell command execution utility."""
    
    @staticmethod
    def execute_command(command: str, admin: bool = False) -> Tuple[bool, str]:
        """Execute shell command."""
        try:
            if admin:
                # Run with admin privileges
                process = subprocess.Popen(
                    ['powershell', '-Command', command],
                    stdout=subprocess.PIPE,
                    stderr=subprocess.PIPE,
                    text=True
                )
            else:
                process = subprocess.Popen(
                    command,
                    stdout=subprocess.PIPE,
                    stderr=subprocess.PIPE,
                    shell=True,
                    text=True
                )
            
            stdout, stderr = process.communicate()
            success = process.returncode == 0
            
            if success:
                logger.info(f"Command executed: {command}")
            else:
                logger.error(f"Command failed: {command} - {stderr}")
            
            return success, stdout if success else stderr
        except Exception as e:
            logger.error(f"Failed to execute command: {e}")
            return False, str(e)
    
    @staticmethod
    def run_powershell(script: str, admin: bool = False) -> Tuple[bool, str]:
        """Run PowerShell script."""
        command = f'powershell -NoProfile -ExecutionPolicy Bypass -Command "{script}"'
        return ShellUtil.execute_command(command, admin)

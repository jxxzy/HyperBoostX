"""Repair service for HyperBoost X."""

import os
import tempfile
import shutil
from typing import Dict, Any
from core.logger import Logger
from utils.shell import ShellUtil

logger = Logger.get_logger(__name__)


class RepairService:
    """Service for system repair and maintenance."""
    
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
    def cleanup_temp_files() -> int:
        """Clean temporary files."""
        logger.info("Cleaning temporary files")
        cleaned_bytes = 0

        paths = [tempfile.gettempdir(), os.path.join(os.environ.get('SystemRoot', 'C:\\Windows'), 'Temp')]
        for path in paths:
            if not os.path.isdir(path):
                continue
            for root, dirs, files in os.walk(path):
                for name in files:
                    try:
                        file_path = os.path.join(root, name)
                        size = os.path.getsize(file_path)
                        os.remove(file_path)
                        cleaned_bytes += size
                    except Exception:
                        continue
                for name in dirs:
                    dir_path = os.path.join(root, name)
                    try:
                        shutil.rmtree(dir_path)
                    except Exception:
                        continue

        freed_mb = int(cleaned_bytes / (1024 * 1024))
        logger.info(f"Cleaned approximately {freed_mb} MB of temporary files")
        return freed_mb

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

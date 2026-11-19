"""
Permission management for HyperBoost X.
Handles admin privilege checks and elevation.
"""

import os
import ctypes
import sys
from core.logger import Logger


logger = Logger.get_logger(__name__)


class Permissions:
    """Application permission handler."""
    
    @staticmethod
    def is_admin() -> bool:
        """Check if running with administrator privileges."""
        try:
            return ctypes.windll.shell.IsUserAnAdmin() != 0
        except Exception:
            return False
    
    @staticmethod
    def require_admin() -> bool:
        """Ensure admin privileges are available."""
        if not Permissions.is_admin():
            logger.warning("Admin privileges required but not available")
            return False
        return True
    
    @staticmethod
    def elevate_privileges() -> bool:
        """Attempt to elevate to admin privileges."""
        if Permissions.is_admin():
            return True
        
        try:
            # Re-run program with admin privileges
            ctypes.windll.shell.ShellExecuteW(None, "runas", sys.executable, " ".join(sys.argv), None, 1)
            logger.info("Attempting privilege elevation")
            return True
        except Exception as e:
            logger.error(f"Failed to elevate privileges: {e}")
            return False

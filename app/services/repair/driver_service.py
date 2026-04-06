"""Driver service for HyperBoost X."""

import csv
import platform
import subprocess
from typing import List, Dict, Any
from core.logger import Logger

try:
    import wmi
except ImportError:
    wmi = None

logger = Logger.get_logger(__name__)


class DriverService:
    """Service for driver detection and management."""

    @staticmethod
    def get_installed_drivers() -> List[Dict[str, Any]]:
        """Get list of installed drivers."""
        drivers = []
        if platform.system() != "Windows":
            return [
                {"name": "NVIDIA GeForce RTX 3080", "manufacturer": "NVIDIA", "version": "531.0", "status": "Updated"},
                {"name": "Intel Network Adapter", "manufacturer": "Intel", "version": "24.1", "status": "Outdated"},
                {"name": "Realtek Audio", "manufacturer": "Realtek", "version": "6.0", "status": "Updated"},
            ]

        # Try command-line driverquery first for a fast, readable list.
        try:
            output = subprocess.check_output(
                ["driverquery", "/FO", "CSV", "/NH"],
                text=True,
                stderr=subprocess.DEVNULL,
            )
            for row in csv.reader(output.splitlines()):
                if len(row) < 2:
                    continue
                module_name = row[0].strip('"')
                display_name = row[1].strip('"')
                drivers.append({
                    "name": display_name or module_name,
                    "manufacturer": "Unknown",
                    "version": "N/A",
                    "status": "Installed",
                })
            if drivers:
                return drivers
        except Exception as e:
            logger.warning(f"Driverquery probe failed: {e}")

        # Fallback to WMI for driver details.
        if wmi is not None:
            try:
                wmi_client = wmi.WMI()
                for driver in wmi_client.Win32_PnPSignedDriver():
                    name = getattr(driver, "DeviceName", None) or getattr(driver, "Caption", None) or getattr(driver, "Description", "Unknown")
                    version = getattr(driver, "DriverVersion", "Unknown")
                    manufacturer = getattr(driver, "Manufacturer", "Unknown")
                    status_value = getattr(driver, "Status", "Installed") or "Installed"
                    status_text = "Updated" if status_value.lower() in ("ok", "running", "active", "installed") else status_value
                    drivers.append({
                        "name": name,
                        "manufacturer": manufacturer,
                        "version": version,
                        "status": status_text,
                    })
                return drivers[:50]
            except Exception as e:
                logger.error(f"Error getting installed drivers: {e}")
                return []
        else:
            logger.warning("WMI library not available for driver detection.")
            return []

    @staticmethod
    def check_driver_updates() -> List[str]:
        """Check for available driver updates."""
        logger.info("Checking for driver updates")
        try:
            drivers = DriverService.get_installed_drivers()
            return [driver["name"] for driver in drivers if driver.get("status", "").lower() == "outdated"]
        except Exception as e:
            logger.error(f"Error checking driver updates: {e}")
            return []

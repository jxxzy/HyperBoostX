"""
System information service for HyperBoost X.
Collects and provides system hardware and software information.
"""

import csv
import getpass
import json
import locale
import os
import platform
import psutil
import socket
import subprocess
import time
from datetime import datetime
from typing import Dict, Any, List, Optional

try:
    import wmi
except ImportError:
    wmi = None

from core.logger import Logger

logger = Logger.get_logger(__name__)


class SystemInfoService:
    """Service for gathering system information."""

    _wmi_client = None

    @classmethod
    def _get_wmi(cls):
        """Get WMI client."""
        if cls._wmi_client is None and wmi is not None:
            try:
                cls._wmi_client = wmi.WMI()
            except Exception as e:
                # WMI may fail in threaded context on some systems - this is not critical
                logger.debug(f"WMI initialization note: {type(e).__name__}")
        return cls._wmi_client

    @staticmethod
    def _parse_wmi_datetime(wmi_date: str) -> str:
        """Parse WMI datetime strings to ISO format."""
        try:
            if not wmi_date:
                return "Unknown"
            boot = datetime.strptime(wmi_date.split('.')[0], "%Y%m%d%H%M%S")
            return boot.strftime("%Y-%m-%d %H:%M:%S")
        except Exception:
            return wmi_date

    @staticmethod
    def _run_powershell_json(command: str) -> Dict[str, Any]:
        """Run a PowerShell command that returns JSON."""
        try:
            output = subprocess.check_output(
                ["powershell", "-NoProfile", "-Command", command],
                text=True,
                stderr=subprocess.DEVNULL,
            ).strip()
            if not output:
                return {}
            data = json.loads(output)
            return data if isinstance(data, dict) else {}
        except Exception as e:
            logger.debug(f"PowerShell JSON probe failed: {type(e).__name__}")
            return {}

    @staticmethod
    def _infer_storage_class(media_type: str, bus_type: str, spindle_speed: Any) -> str:
        media = (media_type or "").strip().lower()
        bus = (bus_type or "").strip().lower()
        try:
            spindle = int(spindle_speed or 0)
        except Exception:
            spindle = 0

        if "ssd" in media or "nvme" in bus:
            return "SSD"
        if "hdd" in media or spindle > 0:
            return "HDD"
        if "scm" in media:
            return "SCM"
        if "sata" in bus:
            return "SSD/HDD (Unknown SATA)"
        return "Unknown"

    @staticmethod
    def get_system_drive_info() -> Dict[str, Any]:
        """Get Windows system drive characteristics for device classification."""
        system_drive = (os.getenv("SystemDrive") or "C:").rstrip(":").upper()
        info = {
            "drive_letter": system_drive,
            "mountpoint": f"{system_drive}:\\",
            "model": "Unknown",
            "media_type": "Unknown",
            "bus_type": "Unknown",
            "spindle_speed": 0,
            "storage_class": "Unknown",
        }

        if platform.system() != "Windows":
            return info

        command = (
            "$drive = '{drive}'; "
            "$partition = Get-Partition -DriveLetter $drive -ErrorAction Stop; "
            "$disk = $partition | Get-Disk -ErrorAction Stop; "
            "[PSCustomObject]@{{ "
            "DriveLetter=$drive; "
            "FriendlyName=$disk.FriendlyName; "
            "MediaType=$disk.MediaType; "
            "BusType=$disk.BusType; "
            "SpindleSpeed=$disk.SpindleSpeed "
            "}} | ConvertTo-Json -Compress"
        ).format(drive=system_drive)
        data = SystemInfoService._run_powershell_json(command)

        info["model"] = data.get("FriendlyName") or info["model"]
        info["media_type"] = data.get("MediaType") or info["media_type"]
        info["bus_type"] = data.get("BusType") or info["bus_type"]
        info["spindle_speed"] = data.get("SpindleSpeed") or info["spindle_speed"]
        info["storage_class"] = SystemInfoService._infer_storage_class(
            info["media_type"],
            info["bus_type"],
            info["spindle_speed"],
        )
        return info

    @staticmethod
    def get_device_profile(stats: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        """Build a simple device class and bottleneck profile for adaptive optimization."""
        stats = stats or {}
        memory_total_gb = float(stats.get("memory_total_gb") or 0)
        memory_percent = float(stats.get("memory") or 0)
        disk_percent = float(stats.get("disk") or 0)
        cpu_percent = float(stats.get("cpu") or 0)
        process_count = int(stats.get("processes") or 0)
        disk_read_mb_s = float(stats.get("disk_read_mb_s") or 0)
        disk_write_mb_s = float(stats.get("disk_write_mb_s") or 0)
        gpu = stats.get("gpu") or {}
        gpu_load = float(gpu.get("load") or gpu.get("memory_percent") or 0)

        system_drive = SystemInfoService.get_system_drive_info()
        storage_class = system_drive.get("storage_class", "Unknown")
        os_release = platform.release()
        os_family = f"Windows {os_release}" if platform.system() == "Windows" else platform.system()
        has_battery = psutil.sensors_battery() is not None
        form_factor = "Laptop" if has_battery else "Desktop"

        if memory_total_gb <= 0:
            ram_class = "Unknown RAM"
        elif memory_total_gb < 8:
            ram_class = "Low RAM"
        elif memory_total_gb < 16:
            ram_class = "Mid RAM"
        else:
            ram_class = "High RAM"

        if storage_class == "HDD" and (disk_percent >= 65 or disk_read_mb_s + disk_write_mb_s >= 8):
            bottleneck = "storage-bound"
        elif memory_percent >= 78 or memory_total_gb < 8:
            bottleneck = "memory-bound"
        elif cpu_percent >= 75 or gpu_load >= 75:
            bottleneck = "cpu-bound"
        elif process_count >= 220:
            bottleneck = "background-load-bound"
        else:
            bottleneck = "balanced"

        if storage_class == "HDD":
            recommended_profile = "HDD Survival"
            expected_gain = "Limited to Moderate"
        elif form_factor == "Laptop" and memory_percent >= 70:
            recommended_profile = "Balanced Laptop"
            expected_gain = "Moderate"
        elif bottleneck == "memory-bound":
            recommended_profile = "Low RAM"
            expected_gain = "Moderate"
        else:
            recommended_profile = "SSD Responsiveness" if storage_class == "SSD" else "Balanced Adaptive"
            expected_gain = "Moderate to High" if storage_class == "SSD" else "Moderate"

        notes = []
        if storage_class == "HDD":
            notes.append("System drive HDD detected. Load-heavy tasks remain limited by storage hardware.")
        elif storage_class == "SSD":
            notes.append("System drive SSD detected. Startup and app responsiveness tuning should be more noticeable.")
        if form_factor == "Laptop":
            notes.append("Laptop detected. Thermal and battery-aware optimization is preferred.")
        if ram_class == "Low RAM":
            notes.append("Low RAM class detected. Background trimming and memory pressure control should be prioritized.")

        return {
            "os_family": os_family,
            "form_factor": form_factor,
            "storage_class": storage_class,
            "ram_class": ram_class,
            "bottleneck": bottleneck,
            "recommended_profile": recommended_profile,
            "expected_gain": expected_gain,
            "notes": notes,
        }

    @staticmethod
    def get_system_identity() -> Dict[str, Any]:
        """Return system identity and environment details."""
        try:
            now = datetime.now()
            uptime_seconds = int(time.time() - psutil.boot_time())
            uptime = {
                "seconds": uptime_seconds,
                "minutes": uptime_seconds // 60,
                "hours": uptime_seconds // 3600,
                "days": uptime_seconds // 86400,
                "formatted": f"{uptime_seconds // 3600}h {(uptime_seconds % 3600) // 60}m {uptime_seconds % 60}s",
            }
            return {
                "os_name": platform.system(),
                "os_release": platform.release(),
                "os_version": platform.version(),
                "build": platform.platform(),
                "architecture": platform.machine(),
                "hostname": socket.gethostname(),
                "user": getpass.getuser(),
                "timezone": time.tzname[0] if time.tzname else "Unknown",
                "language": locale.getdefaultlocale()[0] or "Unknown",
                "uptime": uptime,
                "boot_time": datetime.fromtimestamp(psutil.boot_time()).strftime("%Y-%m-%d %H:%M:%S"),
            }
        except Exception as e:
            logger.error(f"Error getting system identity: {e}")
            return {}

    @staticmethod
    def get_windows_system_details() -> Dict[str, Any]:
        """Get Windows edition, activation, BIOS, TPM, and secure boot details."""
        details = {
            "edition": "Unknown",
            "activation": "Unknown",
            "install_date": "Unknown",
            "boot_mode": "Unknown",
            "secure_boot": "Unknown",
            "tpm_version": "Unknown",
            "virtualization": "Unknown",
            "architecture": "Unknown",
            "build": "Unknown",
        }
        if platform.system() != "Windows":
            return details

        def _run_powershell_command(command: str) -> str:
            try:
                return subprocess.check_output(
                    ["powershell", "-NoProfile", "-Command", command],
                    text=True,
                    stderr=subprocess.DEVNULL,
                ).strip()
            except Exception as e:
                logger.warning(f"PowerShell command failed: {e}")
                return ""

        try:
            output = _run_powershell_command(
                "Get-CimInstance Win32_OperatingSystem | Select-Object Caption, InstallDate, BuildNumber, OSArchitecture | ConvertTo-Json"
            )
            if output:
                data = json.loads(output)
                details['edition'] = data.get('Caption', details['edition'])
                details['install_date'] = SystemInfoService._parse_wmi_datetime(data.get('InstallDate', ''))
                details['build'] = data.get('BuildNumber', details['build'])
                details['architecture'] = data.get('OSArchitecture', details['architecture'])
        except Exception as e:
            logger.warning(f"Windows details JSON probe failed: {e}")

        if details['edition'] == 'Unknown' or details['architecture'] == 'Unknown':
            try:
                output = subprocess.check_output(
                    ["wmic", "os", "get", "Caption,InstallDate,BuildNumber,OSArchitecture", "/value"],
                    text=True,
                    stderr=subprocess.DEVNULL,
                )
                for line in output.splitlines():
                    if not line.strip():
                        continue
                    key, _, value = line.partition('=')
                    if key == 'Caption':
                        details['edition'] = value.strip()
                    elif key == 'InstallDate':
                        details['install_date'] = SystemInfoService._parse_wmi_datetime(value.strip())
                    elif key == 'BuildNumber':
                        details['build'] = value.strip()
                    elif key == 'OSArchitecture':
                        details['architecture'] = value.strip()
            except Exception as e:
                logger.warning(f"Windows details probe failed: {e}")

        try:
            activation = subprocess.check_output(
                ["cscript", "%windir%\\system32\\slmgr.vbs", "/xpr"],
                text=True,
                stderr=subprocess.DEVNULL,
            )
            details['activation'] = activation.strip().replace('\r', '')
        except Exception:
            details['activation'] = "Restricted"

        try:
            secure_boot = subprocess.check_output(
                ["powershell", "-NoProfile", "-Command",
                 "Try { if (Confirm-SecureBootUEFI) { 'On' } else { 'Off' } } Catch { 'Unknown' }"],
                text=True,
                stderr=subprocess.DEVNULL,
            )
            details['secure_boot'] = secure_boot.strip()
        except Exception:
            details['secure_boot'] = "Unknown"

        try:
            virt = subprocess.check_output(
                ["powershell", "-NoProfile", "-Command",
                 "Try { (Get-CimInstance -ClassName Win32_Processor).VirtualizationFirmwareEnabled } Catch { $false }"],
                text=True,
                stderr=subprocess.DEVNULL,
            )
            details['virtualization'] = "On" if 'True' in virt else "Off"
        except Exception:
            details['virtualization'] = "Unknown"

        try:
            boot_mode = subprocess.check_output(
                ["powershell", "-NoProfile", "-Command",
                 "(Get-WmiObject -Class Win32_ComputerSystem).BootupState"],
                text=True,
                stderr=subprocess.DEVNULL,
            )
            details['boot_mode'] = boot_mode.strip() or "Unknown"
        except Exception:
            details['boot_mode'] = "Unknown"

        try:
            tpm_info = subprocess.check_output(
                ["powershell", "-NoProfile", "-Command",
                 "Try {\n$TPM = Get-Tpm; if ($TPM.TpmPresent) { $TPM.SpecVersion } else { 'None' }\n} Catch { 'Unknown' }"],
                text=True,
                stderr=subprocess.DEVNULL,
            )
            details['tpm_version'] = tpm_info.strip()
        except Exception:
            details['tpm_version'] = "Unknown"

        return details

    @staticmethod
    def get_bios_info() -> Dict[str, Any]:
        """Get BIOS and low-level firmware information."""
        try:
            wmi_obj = SystemInfoService._get_wmi()
            if not wmi_obj:
                return {}
            bios_data = {}
            for bios in wmi_obj.Win32_BIOS():
                bios_data = {
                    "vendor": bios.Manufacturer,
                    "version": bios.SMBIOSBIOSVersion,
                    "release_date": bios.ReleaseDate,
                    "serial_number": bios.SerialNumber,
                }
                break
            return bios_data
        except Exception as e:
            logger.error(f"Error getting BIOS info: {e}")
            return {}

    @staticmethod
    def get_cpu_info() -> Dict[str, Any]:
        """Get comprehensive CPU information."""
        info = {
            "processor": platform.processor(),
            "cores": psutil.cpu_count(logical=False) or 0,
            "threads": psutil.cpu_count(logical=True) or 0,
            "frequency_current": 0,
            "frequency_max": 0,
            "usage": psutil.cpu_percent(interval=0),
            "usage_per_core": psutil.cpu_percent(interval=0, percpu=True),
            "socket": "Unknown",
            "lithography": "Unknown",
            "tdp": "Unknown",
            "codename": "Unknown",
        }
        try:
            cpu_freq = psutil.cpu_freq()
            if cpu_freq:
                info["frequency_current"] = cpu_freq.current
                info["frequency_max"] = cpu_freq.max
        except Exception:
            pass

        try:
            wmi_obj = SystemInfoService._get_wmi()
            if wmi_obj:
                for cpu in wmi_obj.Win32_Processor():
                    info["processor"] = cpu.Name or info["processor"]
                    info["socket"] = cpu.SocketDesignation or info["socket"]
                    info["lithography"] = f"{cpu.L2CacheSize}nm" if getattr(cpu, "L2CacheSize", None) else info["lithography"]
                    info["tdp"] = f"{cpu.TDP}W" if getattr(cpu, "TDP", None) else info["tdp"]
                    info["codename"] = cpu.Caption or info["codename"]
                    break
        except Exception as e:
            logger.warning(f"CPU WMI probe failed: {e}")

        return info

    @staticmethod
    def get_gpu_info() -> Dict[str, Any]:
        """Get GPU information using WMI."""
        try:
            wmi_obj = SystemInfoService._get_wmi()
            if not wmi_obj:
                return {}

            gpus = []
            for gpu in wmi_obj.Win32_VideoController():
                gpus.append({
                    "name": gpu.Name,
                    "driver_version": gpu.DriverVersion,
                    "vram": gpu.AdapterRAM if gpu.AdapterRAM else 0,
                    "video_processor": getattr(gpu, "VideoProcessor", "Unknown"),
                    "current_hz": gpu.CurrentRefreshRate,
                    "max_hz": gpu.MaxRefreshRate,
                })
            return {"gpus": gpus}
        except Exception as e:
            logger.error(f"Error getting GPU info: {e}")
            return {}

    @staticmethod
    def get_memory_info() -> Dict[str, Any]:
        """Get memory information."""
        try:
            mem = psutil.virtual_memory()
            swap = psutil.swap_memory()
            info = {
                "total": mem.total,
                "used": mem.used,
                "available": mem.available,
                "percent": mem.percent,
                "swap_total": swap.total,
                "swap_used": swap.used,
                "swap_percent": swap.percent,
            }
            try:
                wmi_obj = SystemInfoService._get_wmi()
                if wmi_obj:
                    modules = []
                    for module in wmi_obj.Win32_PhysicalMemory():
                        modules.append({
                            "capacity": int(module.Capacity or 0),
                            "speed": int(module.Speed or 0),
                            "manufacturer": module.Manufacturer,
                            "part_number": module.PartNumber,
                            "device_locator": module.DeviceLocator,
                        })
                    info["modules"] = modules
                    info["slots_used"] = len(modules)
                    if modules:
                        info["speed_mhz"] = modules[0].get("speed", 0)
            except Exception:
                pass
            return info
        except Exception as e:
            logger.error(f"Error getting memory info: {e}")
            return {}

    @staticmethod
    def get_disk_info() -> Dict[str, Any]:
        """Get disk information for all drives."""
        try:
            disks = {}
            for partition in psutil.disk_partitions():
                try:
                    usage = psutil.disk_usage(partition.mountpoint)
                    disks[partition.device] = {
                        "mountpoint": partition.mountpoint,
                        "fstype": partition.fstype,
                        "total": usage.total,
                        "used": usage.used,
                        "free": usage.free,
                        "percent": usage.percent,
                    }
                except Exception:
                    pass
            return disks
        except Exception as e:
            logger.error(f"Error getting disk info: {e}")
            return {}

    @staticmethod
    def get_network_info() -> Dict[str, Any]:
        """Get network information."""
        try:
            stats = psutil.net_if_stats()
            addresses = psutil.net_if_addrs()
            interfaces = {}
            for iface, addrs in addresses.items():
                iface_stats = stats.get(iface)
                interfaces[iface] = {
                    "addresses": [addr.address for addr in addrs],
                    "mac": next((addr.address for addr in addrs if addr.family.name == 'AF_LINK'), ""),
                    "stats": {
                        "is_up": iface_stats.isup if iface_stats else False,
                        "speed_mbps": iface_stats.speed if iface_stats else 0,
                    },
                }
            return interfaces
        except Exception as e:
            logger.error(f"Error getting network info: {e}")
            return {}

    @staticmethod
    def get_os_info() -> Dict[str, Any]:
        """Get OS information."""
        try:
            return {
                "system": platform.system(),
                "release": platform.release(),
                "version": platform.version(),
                "architecture": platform.architecture()[0],
                "processor_count": psutil.cpu_count(),
            }
        except Exception as e:
            logger.error(f"Error getting OS info: {e}")
            return {}

    @staticmethod
    def get_process_count() -> int:
        """Get number of running processes."""
        try:
            return len(psutil.pids())
        except Exception as e:
            logger.error(f"Error getting process count: {e}")
            return 0

    @staticmethod
    def get_boot_time() -> str:
        """Get last boot time."""
        try:
            boot_time_timestamp = psutil.boot_time()
            boot_time = datetime.fromtimestamp(boot_time_timestamp)
            return boot_time.strftime("%Y-%m-%d %H:%M:%S")
        except Exception as e:
            logger.error(f"Error getting boot time: {e}")
            return "Unknown"

    @staticmethod
    def get_temperature_info() -> Dict[str, Any]:
        """Get system temperature information."""
        try:
            # Check if sensors_temperatures method is available (not available on all systems)
            if not hasattr(psutil, 'sensors_temperatures'):
                return {}
            temps = psutil.sensors_temperatures()
            if not temps:
                return {}
            return {name: [(entry.label, entry.current) for entry in entries]
                    for name, entries in temps.items()}
        except (AttributeError, OSError):
            # sensors_temperatures not available on this system
            return {}
        except Exception as e:
            logger.error(f"Error getting temperature info: {e}")
            return {}


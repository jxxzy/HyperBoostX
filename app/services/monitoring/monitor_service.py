"""Monitor service for HyperBoost X."""

import os
import time
from typing import Dict, List
import psutil
from core.logger import Logger

try:
    import GPUtil
except ImportError:
    GPUtil = None

logger = Logger.get_logger(__name__)


class MonitorService:
    """Service for real-time system monitoring."""

    _last_net = None
    _last_disk = None
    _last_time = None
    _system_drive = None
    _last_process_snapshot = []
    _last_process_snapshot_utc = 0.0
    _last_gpu_snapshot = {}
    _last_gpu_snapshot_utc = 0.0
    _last_temperature_snapshot = {}
    _last_temperature_snapshot_utc = 0.0
    _last_stats_snapshot = {}
    _last_stats_snapshot_utc = 0.0

    @classmethod
    def _get_system_drive_root(cls) -> str:
        if cls._system_drive:
            return cls._system_drive

        system_drive = os.getenv("SystemDrive") or "C:"
        cls._system_drive = f"{system_drive}\\"
        return cls._system_drive

    @classmethod
    def _delta_counters(cls):
        now = time.time()
        net = psutil.net_io_counters()
        disk = psutil.disk_io_counters()

        if cls._last_time is None:
            cls._last_net = net
            cls._last_disk = disk
            cls._last_time = now
            return {
                "net_download_mb_s": 0.0,
                "net_upload_mb_s": 0.0,
                "disk_read_mb_s": 0.0,
                "disk_write_mb_s": 0.0,
            }

        elapsed = max(now - cls._last_time, 0.001)
        download = (net.bytes_recv - cls._last_net.bytes_recv) / (1024**2) / elapsed
        upload = (net.bytes_sent - cls._last_net.bytes_sent) / (1024**2) / elapsed
        read_mb = (disk.read_bytes - cls._last_disk.read_bytes) / (1024**2) / elapsed
        write_mb = (disk.write_bytes - cls._last_disk.write_bytes) / (1024**2) / elapsed

        cls._last_net = net
        cls._last_disk = disk
        cls._last_time = now

        return {
            "net_download_mb_s": max(download, 0),
            "net_upload_mb_s": max(upload, 0),
            "disk_read_mb_s": max(read_mb, 0),
            "disk_write_mb_s": max(write_mb, 0),
        }

    @classmethod
    def get_current_stats(cls) -> Dict:
        """Get current system statistics (non-blocking)."""
        if cls._last_stats_snapshot and (time.time() - cls._last_stats_snapshot_utc) <= 2.5:
            return dict(cls._last_stats_snapshot)

        try:
            cpu_freq = psutil.cpu_freq()
            cpu_percent = psutil.cpu_percent(interval=0)
            cpu_per_core = psutil.cpu_percent(interval=0, percpu=True)
            vm = psutil.virtual_memory()
            root = psutil.disk_usage(cls._get_system_drive_root())
            deltas = cls._delta_counters()
            net_io = psutil.net_io_counters()

            gpu_info = cls.get_gpu_stats()
            temperatures = cls.get_temperature_info()

            snapshot = {
                "cpu": cpu_percent,
                "cpu_freq": cpu_freq.current / 1000 if cpu_freq else 0,
                "cpu_freq_max": cpu_freq.max / 1000 if cpu_freq else 0,
                "cpu_cores": psutil.cpu_count(logical=False) or 0,
                "cpu_threads": psutil.cpu_count(logical=True) or 0,
                "cpu_per_core": cpu_per_core,
                "memory": vm.percent,
                "memory_used_gb": vm.used / (1024**3),
                "memory_total_gb": vm.total / (1024**3),
                "disk": root.percent,
                "disk_used_gb": root.used / (1024**3),
                "disk_total_gb": root.total / (1024**3),
                "disk_read_mb_s": deltas["disk_read_mb_s"],
                "disk_write_mb_s": deltas["disk_write_mb_s"],
                "network_download_mb_s": deltas["net_download_mb_s"],
                "network_upload_mb_s": deltas["net_upload_mb_s"],
                "network": net_io.bytes_recv + net_io.bytes_sent,
                "processes": len(psutil.pids()),
                "boot_time": psutil.boot_time(),
                "temperatures": temperatures,
                "gpu": gpu_info,
            }
            cls._last_stats_snapshot = snapshot
            cls._last_stats_snapshot_utc = time.time()
            return snapshot
        except Exception as e:
            logger.error(f"Error getting current stats: {e}")
            if cls._last_stats_snapshot:
                return dict(cls._last_stats_snapshot)
            return {
                "cpu": 0,
                "cpu_freq": 0,
                "cpu_freq_max": 0,
                "cpu_cores": 0,
                "cpu_threads": 0,
                "cpu_per_core": [],
                "memory": 0,
                "memory_used_gb": 0,
                "memory_total_gb": 0,
                "disk": 0,
                "disk_used_gb": 0,
                "disk_total_gb": 0,
                "disk_read_mb_s": 0,
                "disk_write_mb_s": 0,
                "network_download_mb_s": 0,
                "network_upload_mb_s": 0,
                "network": 0,
                "processes": 0,
                "boot_time": 0,
                "temperatures": {},
                "gpu": {},
            }

    @staticmethod
    def get_process_list(limit: int = 10) -> List[Dict]:
        """Get list of running processes sorted by memory."""
        try:
            processes = []
            for proc in psutil.process_iter(['pid', 'name', 'memory_percent', 'cpu_percent', 'io_counters', 'num_threads']):
                try:
                    proc_info = proc.info or {}
                    pid = proc_info.get('pid')
                    if pid is None:
                        pid = proc.pid
                    if pid is None:
                        continue

                    io_counters = proc_info.get('io_counters')
                    processes.append({
                        'pid': pid,
                        'name': proc_info.get('name') or 'Unknown',
                        'memory': proc_info.get('memory_percent') or 0,
                        'cpu': proc_info.get('cpu_percent') or 0,
                        'threads': proc_info.get('num_threads', 0) or 0,
                        'disk_io_mb': (io_counters.read_bytes + io_counters.write_bytes) / (1024**2) if io_counters else 0,
                    })
                except (psutil.NoSuchProcess, psutil.AccessDenied, KeyError, AttributeError):
                    pass
            sorted_processes = sorted(processes, key=lambda x: x['memory'], reverse=True)[:limit]
            MonitorService._last_process_snapshot = sorted_processes
            MonitorService._last_process_snapshot_utc = time.time()
            return sorted_processes
        except Exception as e:
            logger.error(f"Error getting process list: {e}")
            if MonitorService._last_process_snapshot and (time.time() - MonitorService._last_process_snapshot_utc) <= 10:
                return MonitorService._last_process_snapshot[:limit]
            return []

    @staticmethod
    def get_network_stats() -> Dict:
        """Get network statistics."""
        try:
            stats = psutil.net_io_counters()
            return {
                "bytes_sent": stats.bytes_sent,
                "bytes_recv": stats.bytes_recv,
                "packets_sent": stats.packets_sent,
                "packets_recv": stats.packets_recv,
                "errors_in": stats.errin,
                "errors_out": stats.errout,
            }
        except Exception as e:
            logger.error(f"Error getting network stats: {e}")
            return {}

    @staticmethod
    def get_disk_stats() -> Dict:
        """Get disk I/O statistics."""
        try:
            stats = psutil.disk_io_counters()
            return {
                "read_count": stats.read_count,
                "write_count": stats.write_count,
                "read_bytes": stats.read_bytes,
                "write_bytes": stats.write_bytes,
            }
        except Exception as e:
            logger.error(f"Error getting disk stats: {e}")
            return {}

    @staticmethod
    def get_temperature_info() -> Dict:
        """Return temperature sensors if available."""
        if MonitorService._last_temperature_snapshot and (time.time() - MonitorService._last_temperature_snapshot_utc) <= 5:
            return dict(MonitorService._last_temperature_snapshot)

        try:
            # Check if sensors_temperatures method is available (not available on all systems)
            if not hasattr(psutil, 'sensors_temperatures'):
                return {}
            temps = psutil.sensors_temperatures()
            if not temps:
                return {}
            snapshot = {name: [(entry.label or 'sensor', entry.current) for entry in entries] for name, entries in temps.items()}
            MonitorService._last_temperature_snapshot = snapshot
            MonitorService._last_temperature_snapshot_utc = time.time()
            return snapshot
        except (AttributeError, OSError):
            # sensors_temperatures not available on this system
            return {}
        except Exception as e:
            logger.error(f"Error getting temperature info: {e}")
            return {}

    @staticmethod
    def get_gpu_stats() -> Dict:
        """Return GPU utilization information if supported."""
        if MonitorService._last_gpu_snapshot and (time.time() - MonitorService._last_gpu_snapshot_utc) <= 5:
            return dict(MonitorService._last_gpu_snapshot)

        if GPUtil is None:
            return {}
        try:
            gpus = GPUtil.getGPUs()
            if not gpus:
                return {}
            gpu = gpus[0]
            snapshot = {
                "name": gpu.name,
                "load": gpu.load * 100,
                "memory_used_mb": gpu.memoryUsed,
                "memory_total_mb": gpu.memoryTotal,
                "memory_percent": gpu.memoryUtil * 100,
                "temperature": gpu.temperature,
                "fan_speed": getattr(gpu, 'fanSpeed', 0),
                "power_draw": getattr(gpu, 'powerDraw', 0),
                "power_limit": getattr(gpu, 'powerLimit', 0),
            }
            MonitorService._last_gpu_snapshot = snapshot
            MonitorService._last_gpu_snapshot_utc = time.time()
            return snapshot
        except Exception as e:
            logger.error(f"Error getting GPU stats: {e}")
            return {}

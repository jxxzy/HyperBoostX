"""
System API Blueprint for HyperBoostX
Handles system information and monitoring endpoints
"""

from flask import Blueprint, jsonify
from core.logger import Logger
from services.monitoring.system_info_service import SystemInfoService
from services.monitoring.monitor_service import MonitorService
from services.optimization.startup_service import StartupService

logger = Logger.get_logger(__name__)

system_bp = Blueprint('system', __name__, url_prefix='/api/system')

# Initialize services
system_info_service = SystemInfoService()
monitor_service = MonitorService()
startup_service = StartupService()


@system_bp.route('/info', methods=['GET'])
def get_system_info():
    """Get comprehensive system information."""
    try:
        current_stats = monitor_service.get_current_stats()
        return jsonify({
            "identity": system_info_service.get_system_identity(),
            "cpu": system_info_service.get_cpu_info(),
            "memory": system_info_service.get_memory_info(),
            "disk": system_info_service.get_disk_info(),
            "system_drive": system_info_service.get_system_drive_info(),
            "device_profile": system_info_service.get_device_profile(current_stats),
            "network": system_info_service.get_network_info(),
            "os": system_info_service.get_os_info(),
            "bios": system_info_service.get_bios_info(),
            "gpu": system_info_service.get_gpu_info(),
            "temperatures": system_info_service.get_temperature_info(),
        })
    except Exception as e:
        logger.error(f"Error in /api/system/info: {e}")
        return jsonify({"error": str(e)}), 500


@system_bp.route('/startup', methods=['GET'])
def get_system_startup():
    """Return startup item data from the v1.4.0 system namespace."""
    try:
        items = startup_service.get_startup_items()
        return jsonify({
            "items": items,
            "startup_items": items,
            "count": len(items),
        })
    except Exception as e:
        logger.error(f"Error in /api/system/startup: {e}")
        return jsonify({"error": str(e)}), 500



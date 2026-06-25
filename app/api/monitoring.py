"""
Monitoring API Blueprint for HyperBoostX
Handles real-time system statistics and process list endpoints.
"""

from flask import Blueprint, jsonify
from core.logger import Logger
from services.monitoring.monitor_service import MonitorService

logger = Logger.get_logger(__name__)

monitoring_bp = Blueprint('monitoring', __name__, url_prefix='/api/system')

monitor_service = MonitorService()


@monitoring_bp.route('/stats', methods=['GET'])
def get_system_stats():
    """Get real-time system statistics."""
    try:
        return jsonify(monitor_service.get_current_stats())
    except Exception as e:
        logger.error(f"Error in /api/system/stats: {e}")
        return jsonify({"error": str(e)}), 500


@monitoring_bp.route('/processes', methods=['GET'])
def get_processes():
    """Get running processes information."""
    try:
        return jsonify({"processes": monitor_service.get_process_list()})
    except Exception as e:
        logger.error(f"Error in /api/system/processes: {e}")
        return jsonify({"error": str(e)}), 500

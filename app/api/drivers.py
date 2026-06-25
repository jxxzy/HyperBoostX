"""
Drivers API Blueprint for HyperBoostX
Handles installed driver discovery and update diagnostics.
"""

from flask import Blueprint, jsonify
from core.logger import Logger
from api.middleware import handle_errors, log_requests
from services.repair.driver_service import DriverService

logger = Logger.get_logger(__name__)

drivers_bp = Blueprint('drivers', __name__, url_prefix='/api/drivers')

driver_service = DriverService()


@drivers_bp.route('/list', methods=['GET'])
def list_drivers():
    """Return installed drivers."""
    try:
        drivers = driver_service.get_installed_drivers()
        return jsonify({"drivers": drivers})
    except Exception as e:
        logger.error(f"Error in /api/drivers/list: {e}")
        return jsonify({"error": str(e)}), 500


@drivers_bp.route('/check-updates', methods=['POST'])
@handle_errors
@log_requests
def check_driver_updates():
    """Check available driver updates."""
    updates = driver_service.check_driver_updates()
    return jsonify({"updates": updates})

"""
Repair API Blueprint for HyperBoostX
Handles system repair, cleanup, and diagnostics.
"""

from flask import Blueprint, jsonify, request
from core.logger import Logger
from api.middleware import handle_errors, log_requests
from services.repair.repair_service import RepairService

logger = Logger.get_logger(__name__)

repair_bp = Blueprint('repair', __name__, url_prefix='/api/repair')

repair_service = RepairService()


@repair_bp.route('/run-sfc', methods=['POST'])
@handle_errors
@log_requests
def run_sfc():
    """Run System File Checker."""
    result = repair_service.run_sfc()
    return jsonify(result)


@repair_bp.route('/run-dism', methods=['POST'])
@handle_errors
@log_requests
def run_dism():
    """Run DISM image repair."""
    result = repair_service.run_dism()
    return jsonify(result)


@repair_bp.route('/cleanup', methods=['POST'])
@handle_errors
@log_requests
def cleanup():
    """Clean temporary files and free disk space."""
    payload = request.get_json(silent=True) or {}
    result = repair_service.cleanup_temp_files(payload.get("scope", "safe_all"))
    return jsonify(result)


@repair_bp.route('/reset-network', methods=['POST'])
@handle_errors
@log_requests
def reset_network():
    """Reset network components."""
    result = repair_service.reset_network()
    return jsonify(result)

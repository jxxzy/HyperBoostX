"""
Network API Blueprint for HyperBoost X
Handles network testing and optimization operations.
"""

from flask import Blueprint, jsonify
from core.logger import Logger
from api.middleware import handle_errors, log_requests
from services.optimization.network_service import NetworkService

logger = Logger.get_logger(__name__)

network_bp = Blueprint('network', __name__, url_prefix='/api/network')

network_service = NetworkService()


@network_bp.route('/dns-test', methods=['GET'])
@handle_errors
def dns_test():
    """Test DNS responsiveness."""
    result = network_service.test_dns()
    return jsonify(result)


@network_bp.route('/flush-dns', methods=['POST'])
@handle_errors
@log_requests
def flush_dns():
    """Flush DNS resolver cache."""
    result = network_service.flush_dns()
    return jsonify(result if isinstance(result, dict) else {"success": result})


@network_bp.route('/optimize-tcp', methods=['POST'])
@handle_errors
@log_requests
def optimize_tcp():
    """Optimize TCP settings."""
    result = network_service.optimize_tcp()
    return jsonify(result if isinstance(result, dict) else {"success": result})

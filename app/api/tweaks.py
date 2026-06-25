"""
Tweaks API Blueprint for HyperBoost X
Handles system tweaks and optimizations
"""

from flask import Blueprint, jsonify, request
from core.logger import Logger
from api.middleware import validate_json, handle_errors, log_requests
from services.optimization.tweak_service import TweakService

logger = Logger.get_logger(__name__)

tweaks_bp = Blueprint('tweaks', __name__, url_prefix='/api/tweaks')

# Initialize service
tweak_service = TweakService()


@tweaks_bp.route('/list', methods=['GET'])
def get_tweaks():
    """Get all available system tweaks."""
    try:
        tweaks = tweak_service.get_all_tweaks()
        return jsonify({"tweaks": tweaks})
    except Exception as e:
        logger.error(f"Error in /api/tweaks/list: {e}")
        return jsonify({"error": str(e)}), 500


@tweaks_bp.route('/apply', methods=['POST'])
@validate_json(['tweak_id'])
@handle_errors
@log_requests
def apply_tweak():
    """Apply a specific system tweak."""
    data = request.get_json()
    tweak_id = data['tweak_id']
    result = tweak_service.apply_tweak(
        tweak_id,
        expert_mode=bool(data.get("expert_mode")),
        confirmed=bool(data.get("confirmed")),
    )
    return jsonify(result)


@tweaks_bp.route('/revert', methods=['POST'])
@validate_json(['tweak_id'])
@handle_errors
@log_requests
def revert_tweak():
    """Revert a specific system tweak."""
    data = request.get_json()
    tweak_id = data['tweak_id']
    result = tweak_service.revert_tweak(tweak_id)
    return jsonify(result)


@tweaks_bp.route('/info/<tweak_id>', methods=['GET'])
def get_tweak_info(tweak_id):
    """Get information about a specific tweak."""
    try:
        info = tweak_service.get_tweak_info(tweak_id)
        if info:
            return jsonify(info)
        else:
            return jsonify({"error": "Tweak not found"}), 404
    except Exception as e:
        logger.error(f"Error in /api/tweaks/info/{tweak_id}: {e}")
        return jsonify({"error": str(e)}), 500

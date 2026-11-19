"""
Booster API Blueprint for HyperBoost X
Handles performance profiles and boosting
"""

from flask import Blueprint, jsonify, request
from core.logger import Logger
from api.middleware import validate_json, handle_errors, log_requests
from services.optimization.booster_service import BoosterService

logger = Logger.get_logger(__name__)

booster_bp = Blueprint('booster', __name__, url_prefix='/api/booster')

# Initialize service
booster_service = BoosterService()


@booster_bp.route('/profiles', methods=['GET'])
def get_booster_profiles():
    """Get available booster profiles."""
    try:
        profiles = booster_service.get_available_profiles()
        return jsonify({"profiles": profiles})
    except Exception as e:
        logger.error(f"Error in /api/booster/profiles: {e}")
        return jsonify({"error": str(e)}), 500


@booster_bp.route('/apply', methods=['POST'])
@handle_errors
@log_requests
def apply_booster_profile():
    """Apply a booster profile."""
    data = request.get_json(silent=True)
    if not data:
        return jsonify({"error": "Request must be JSON"}), 400

    profile_id = data.get('profile_id') or data.get('profile')
    if not profile_id:
        return jsonify({"success": False, "error": "profile_id or profile is required"}), 400

    result = booster_service.apply_profile(profile_id)
    return jsonify(result)
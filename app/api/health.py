"""
Health API Blueprint for HyperBoost X
Handles health checks and service status
"""

from flask import Blueprint, jsonify
from core.logger import Logger

logger = Logger.get_logger(__name__)

health_bp = Blueprint('health', __name__, url_prefix='/api')


@health_bp.route('/health', methods=['GET'])
def health_check():
    """Basic health check endpoint."""
    try:
        return jsonify({
            "status": "ok",
            "version": "1.2.12",
            "service": "HyperBoost X Backend"
        })
    except Exception as e:
        logger.error(f"Error in /api/health: {e}")
        return jsonify({"error": str(e)}), 500



"""
Health API Blueprint for HyperBoostX
Handles health checks and service status
"""

import os

from flask import Blueprint, jsonify
from core.config import Config
from core.logger import Logger

logger = Logger.get_logger(__name__)

health_bp = Blueprint('health', __name__, url_prefix='/api')


@health_bp.route('/health', methods=['GET'])
def health_check():
    """Basic health check endpoint."""
    try:
        return jsonify({
            "status": "ok",
            "version": Config.VERSION,
            "service": "HyperBoostX Backend",
            "local_only": True,
            "session_token_required": bool(os.environ.get("HYPERBOOSTX_SESSION_TOKEN", "").strip()),
        })
    except Exception as e:
        logger.error(f"Error in /api/health: {e}")
        return jsonify({"error": str(e)}), 500


@health_bp.route('/version', methods=['GET'])
def version_check():
    """Return the backend product/version contract."""
    return jsonify({
        "name": "HyperBoostX",
        "version": Config.VERSION,
        "release": f"HyperBoostX v{Config.VERSION} Stable",
    })



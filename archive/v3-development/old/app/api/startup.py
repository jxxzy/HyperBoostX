"""
Startup API Blueprint for HyperBoostX
Handles startup item listing and simple startup management operations.
"""

from flask import Blueprint, jsonify
from core.logger import Logger
from api.middleware import handle_errors
from services.optimization.startup_service import StartupService

logger = Logger.get_logger(__name__)

startup_bp = Blueprint("startup", __name__, url_prefix="/api/startup")

startup_service = StartupService()


@startup_bp.route("/list", methods=["GET"])
@handle_errors
def list_startup_items():
    """Return startup item data for the WPF client."""
    items = startup_service.get_startup_items()
    return jsonify({
        "items": items,
        "startup_items": items,
        "count": len(items),
    })

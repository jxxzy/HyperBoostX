"""Hardware API Blueprint for HyperBoostX v2.0.0."""

from flask import Blueprint, jsonify

from core.logger import Logger
from services.monitoring.gpu_detection_service import GpuDetectionService
from services.monitoring.hardware_profile_service import HardwareProfileService

logger = Logger.get_logger(__name__)

hardware_bp = Blueprint("hardware", __name__, url_prefix="/api/hardware")


@hardware_bp.route("/gpu", methods=["GET"])
def get_gpu():
    try:
        return jsonify(GpuDetectionService.get_gpu_summary())
    except Exception as e:
        logger.error(f"Error in /api/hardware/gpu: {e}")
        return jsonify({"error": str(e)}), 500


@hardware_bp.route("/vendors", methods=["GET"])
def get_vendor_software():
    try:
        return jsonify({"items": GpuDetectionService.detect_vendor_software()})
    except Exception as e:
        logger.error(f"Error in /api/hardware/vendors: {e}")
        return jsonify({"error": str(e)}), 500


@hardware_bp.route("/overlays", methods=["GET"])
def get_overlays():
    try:
        return jsonify({"items": GpuDetectionService.detect_overlays()})
    except Exception as e:
        logger.error(f"Error in /api/hardware/overlays: {e}")
        return jsonify({"error": str(e)}), 500


@hardware_bp.route("/profile", methods=["GET"])
def get_hardware_profile():
    try:
        return jsonify(HardwareProfileService.get_profile())
    except Exception as e:
        logger.error(f"Error in /api/hardware/profile: {e}")
        return jsonify({"error": str(e)}), 500

"""
Health API Blueprint for HyperBoostX
Handles health checks and service status
"""

import os

from flask import Blueprint, jsonify
from core.config import Config
from core.logger import Logger
from services.feature_registry import FeatureRegistryService

logger = Logger.get_logger(__name__)

health_bp = Blueprint('health', __name__, url_prefix='/api')


def _release_channel() -> str:
    if Config.VERSION == "2.10.0" and os.environ.get("HYPERBOOSTX_PUBLIC_STABLE_APPROVED", "").strip().lower() not in {"1", "true", "yes"}:
        return "Owner Lab"
    return "Beta" if "-" in Config.VERSION else "Stable"


def _release_readiness():
    channel = _release_channel()
    is_public_stable = channel == "Stable"
    if is_public_stable:
        return {
            "version": Config.VERSION,
            "channel": channel,
            "stable": True,
            "source_package_ready": True,
            "installer_built": True,
            "installed_runtime_verified": True,
            "admin_apply_verified": False,
            "safe_restore_routes_verified": True,
            "hardware_matrix_verified": False,
            "code_signed": False,
            "code_signing_status": "SKIPPED_BY_OWNER_NO_CERT",
            "manual_lab_required": False,
            "external_lab_recommended": True,
            "status": "stable_ready_unsigned",
            "blocking_gates": [],
            "known_limitations": [
                "No owner code-signing certificate was supplied.",
                "External hardware matrix coverage should be expanded beyond this machine.",
                "OS-level admin apply/rollback remains guarded and limited to supported flows.",
            ],
            "message": "Stable unsigned build is ready based on source, package, installer, and installed-runtime gate evidence. Code signing was skipped because no owner certificate was supplied.",
        }

    return {
        "version": Config.VERSION,
        "channel": channel,
        "stable": False,
        "public_stable": False,
        "owner_lab_only": True,
        "source_package_ready": True,
        "installer_built": True,
        "installed_runtime_verified": False,
        "admin_apply_verified": False,
        "hardware_matrix_verified": False,
        "code_signed": False,
        "manual_lab_required": True,
        "status": "OWNER_LAB_BLOCKED",
        "blocking_gates": [
            "full_feature_parity_audit",
            "ui_ux_repair_polish",
            "installed_runtime_verification",
            "admin_apply_rollback_lab",
            "hardware_matrix_lab",
            "code_signing",
        ],
        "message": "v2.10.0 is held for owner-lab validation and repair. Public release remains blocked until feature integrity, UI/UX, installer/runtime, rollback, hardware matrix, and safety gates pass.",
    }


@health_bp.route('/health', methods=['GET'])
def health_check():
    """Basic health check endpoint."""
    try:
        feature_audit = FeatureRegistryService.audit()
        return jsonify({
            "status": "ok",
            "version": Config.VERSION,
            "service": "HyperBoostX Backend",
            "backend_mode": FeatureRegistryService.mode(),
            "local_only": True,
            "session_token_required": bool(os.environ.get("HYPERBOOSTX_SESSION_TOKEN", "").strip()),
            "feature_registry_status": {
                "stable_ui_ok": feature_audit.get("ok", False),
                "action_map_found": feature_audit.get("source_found", False),
                "action_map_source": feature_audit.get("source"),
                "stable_visible_features": feature_audit.get("counts", {}).get("stable_visible_features", 0),
                "stable_visible_buttons": feature_audit.get("counts", {}).get("stable_visible_buttons", 0),
                "non_real_visible_in_stable": feature_audit.get("counts", {}).get("non_real_visible_in_stable", 0),
                "expected": feature_audit.get("expected_contract", {}),
                "warnings": feature_audit.get("warnings", []),
                "errors": feature_audit.get("errors", []),
            },
        })
    except Exception as e:
        logger.error(f"Error in /api/health: {e}")
        return jsonify({"error": str(e)}), 500


@health_bp.route('/version', methods=['GET'])
def version_check():
    """Return the backend product/version contract."""
    channel = _release_channel()
    return jsonify({
        "name": "HyperBoostX",
        "version": Config.VERSION,
        "release": f"HyperBoostX v{Config.VERSION} {channel}",
        "channel": channel,
        "stable": channel == "Stable",
        "public_stable": channel == "Stable",
        "owner_lab_only": channel != "Stable",
    })


@health_bp.route('/release/readiness', methods=['GET'])
def release_readiness():
    """Return explicit release-channel and remaining gate status."""
    return jsonify(_release_readiness())

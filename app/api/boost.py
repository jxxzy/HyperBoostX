"""Safe boost API Blueprint for HyperBoostX v1.3.0."""

from flask import Blueprint, jsonify, request

from api.middleware import handle_errors, log_requests
from services.optimization.boost_plan_service import BoostPlanService

boost_bp = Blueprint("boost", __name__, url_prefix="/api/boost")


@boost_bp.route("/plan", methods=["POST"])
@handle_errors
@log_requests
def create_boost_plan():
    data = request.get_json(silent=True) or {}
    return jsonify(BoostPlanService.create_plan(
        goal=data.get("goal", "gaming"),
        mode=data.get("mode", "balanced"),
    ))


@boost_bp.route("/apply", methods=["POST"])
@handle_errors
@log_requests
def apply_boost_plan():
    data = request.get_json(silent=True) or {}
    result = BoostPlanService.apply_plan(data)
    status_code = 200 if result.get("success") else 409
    return jsonify(result), status_code


@boost_bp.route("/undo", methods=["POST"])
@handle_errors
@log_requests
def undo_boost_plan():
    return jsonify(BoostPlanService.undo())

"""Job queue API Blueprint for HyperBoostX v2.0.0."""

from flask import Blueprint, jsonify, request

from api.middleware import handle_errors, log_requests
from services.optimization.job_queue_service import JobQueueService

jobs_bp = Blueprint("jobs", __name__, url_prefix="/api/jobs")


@jobs_bp.route("/start", methods=["POST"])
@handle_errors
@log_requests
def start_job():
    data = request.get_json(silent=True) or {}
    job_type = data.get("job_type") or data.get("type") or "hardware_analysis"
    return jsonify(JobQueueService.start_job(job_type, data))


@jobs_bp.route("/<job_id>", methods=["GET"])
def get_job(job_id):
    job = JobQueueService.get_job(job_id)
    return jsonify(job), 404 if job.get("error") else 200


@jobs_bp.route("/<job_id>/cancel", methods=["POST"])
@handle_errors
@log_requests
def cancel_job(job_id):
    job = JobQueueService.cancel_job(job_id)
    return jsonify(job), 404 if job.get("error") else 200

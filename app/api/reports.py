"""Report API Blueprint for HyperBoostX v2.0.0."""

from flask import Blueprint, jsonify, request

from api.middleware import handle_errors, log_requests
from services.monitoring.crash_report_service import CrashReportService
from services.monitoring.report_service import ReportService

reports_bp = Blueprint("reports", __name__, url_prefix="/api/reports")


@reports_bp.route("/latest", methods=["GET"])
def latest_report():
    return jsonify(ReportService.latest_report())


@reports_bp.route("/export", methods=["POST"])
@handle_errors
@log_requests
def export_report():
    data = request.get_json(silent=True) or {}
    return jsonify(ReportService.export_report(data.get("format", "json")))

@reports_bp.route("/crash-export", methods=["POST"])
@handle_errors
@log_requests
def export_crash_report():
    data = request.get_json(silent=True) or {}
    return jsonify(CrashReportService.export_report(data, data.get("format", "json")))

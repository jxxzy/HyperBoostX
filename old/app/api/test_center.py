"""Embedded Test Center API for HyperBoostX v2.10."""

from __future__ import annotations

from flask import Blueprint, jsonify, request

from api.middleware import handle_errors, log_requests
from services.action_history_service import ActionHistoryService
from services.discord_webhook_reporter import DiscordWebhookReporter
from services.test_center_service import TestCenterService


test_center_bp = Blueprint("test_center", __name__, url_prefix="/api/test-center")


def _payload() -> dict:
    return request.get_json(silent=True) or {}


@test_center_bp.route("/status", methods=["GET"])
@handle_errors
def status():
    return jsonify(TestCenterService.status())


@test_center_bp.route("/run", methods=["POST"])
@handle_errors
@log_requests
def run():
    return jsonify(TestCenterService.run(_payload()))


@test_center_bp.route("/run-suite", methods=["POST"])
@handle_errors
@log_requests
def run_suite():
    payload = _payload()
    suite_id = str(payload.get("suite_id") or "").strip()
    return jsonify(TestCenterService.run_suite(suite_id, payload=payload))


@test_center_bp.route("/cancel", methods=["POST"])
@handle_errors
@log_requests
def cancel():
    return jsonify(TestCenterService.cancel())


@test_center_bp.route("/results", methods=["GET"])
@handle_errors
def results():
    try:
        limit = int(request.args.get("limit", "20"))
    except ValueError:
        limit = 20
    return jsonify(TestCenterService.results(limit=limit))


@test_center_bp.route("/report/latest", methods=["GET"])
@handle_errors
def latest_report():
    return jsonify(TestCenterService.latest_report())


@test_center_bp.route("/report/generate", methods=["POST"])
@handle_errors
@log_requests
def generate_report():
    paths = TestCenterService.generate_report()
    return jsonify({"ok": True, "report_paths": paths})


@test_center_bp.route("/action-history", methods=["GET"])
@handle_errors
def action_history():
    try:
        limit = int(request.args.get("limit", "100"))
    except ValueError:
        limit = 100
    return jsonify(ActionHistoryService.latest(limit=limit))


@test_center_bp.route("/feature-integrity/run", methods=["POST"])
@handle_errors
@log_requests
def feature_integrity_run():
    return jsonify(TestCenterService.feature_integrity())


@test_center_bp.route("/discord/test", methods=["POST"])
@handle_errors
@log_requests
def discord_test():
    return jsonify(TestCenterService.discord_test(_payload()))


@test_center_bp.route("/discord/send-latest-failures", methods=["POST"])
@handle_errors
@log_requests
def discord_send_latest_failures():
    return jsonify(TestCenterService.send_latest_failures_to_discord(_payload()))


@test_center_bp.route("/discord/history", methods=["GET"])
@handle_errors
def discord_history():
    try:
        limit = int(request.args.get("limit", "100"))
    except ValueError:
        limit = 100
    return jsonify(DiscordWebhookReporter.history(limit=limit))


@test_center_bp.route("/discord/settings", methods=["GET"])
@handle_errors
def discord_settings_get():
    return jsonify({"ok": True, "settings": DiscordWebhookReporter.settings(include_secret=False)})


@test_center_bp.route("/discord/settings", methods=["POST"])
@handle_errors
@log_requests
def discord_settings_post():
    return jsonify({"ok": True, "settings": DiscordWebhookReporter.update_settings(_payload())})


@test_center_bp.route("/discord/settings", methods=["DELETE"])
@handle_errors
@log_requests
def discord_settings_delete():
    return jsonify({"ok": True, "settings": DiscordWebhookReporter.clear_settings()})


@test_center_bp.route("/suites", methods=["GET"])
@handle_errors
def suites():
    return jsonify(TestCenterService.suites())


@test_center_bp.route("/suites/<suite_id>", methods=["GET"])
@handle_errors
def suite_detail(suite_id: str):
    return jsonify(TestCenterService.suite(suite_id))


@test_center_bp.route("/suites/<suite_id>/run", methods=["POST"])
@handle_errors
@log_requests
def suite_run_alias(suite_id: str):
    return jsonify(TestCenterService.run_suite(suite_id, payload=_payload()))

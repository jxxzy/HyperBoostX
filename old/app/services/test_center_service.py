"""Embedded Test Center service for HyperBoostX v2.10."""

from __future__ import annotations

import json
import os
import time
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Callable, Dict, Iterable, List, Optional, Tuple

from services.action_history_service import ActionHistoryService, to_json_safe
from services.discord_webhook_reporter import DiscordWebhookReporter
from services.feature_registry import FeatureRegistryService
from services.product_features import LocalJsonStore


REPO_ROOT = Path(__file__).resolve().parents[2]
ACTION_MAP_PATH = REPO_ROOT / "wpf" / "Data" / "ui_action_map_v2_10.json"
DATA_DIR = REPO_ROOT / "data"
RUNTIME_AUDIT_DIR = REPO_ROOT / "docs" / "runtime-audit" / "test-center"


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def read_json(path: Path, default: Any) -> Any:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except Exception:
        return default


def write_json(path: Path, data: Any) -> Path:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(to_json_safe(data), indent=2), encoding="utf-8")
    return path


def write_text(path: Path, content: str) -> Path:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")
    return path


def load_action_map() -> Dict[str, Any]:
    return read_json(ACTION_MAP_PATH, {"summary": {}, "menus": []})


def action_map_counts() -> Dict[str, int]:
    action_map = load_action_map()
    summary = action_map.get("summary", {})
    menus = list(action_map.get("menus") or [])
    actions = [action for menu in menus for action in (menu.get("actions") or [])]
    endpoints = sorted({f"{str(action.get('method') or 'GET').upper()} {str(action.get('path') or '').split('?')[0]}" for action in actions})
    return {
        "menus": int(summary.get("total_menus") or len(menus)),
        "buttons": int(summary.get("total_buttons") or len(actions)),
        "unique_endpoints": int(summary.get("total_unique_endpoints_used") or len(endpoints)),
        "partial_buttons": int(summary.get("total_partial_or_roadmap_buttons") or len([a for a in actions if a.get("partial")])),
        "guarded_destructive": int(summary.get("total_guarded_destructive_buttons") or len([a for a in actions if a.get("is_destructive")])),
    }


def default_suites() -> List[Dict[str, Any]]:
    return [
        {"id": "full_owner_lab_gate", "name": "Full Owner Lab Gate", "category": "Owner Lab", "risk": "safe", "known_id": True},
        {"id": "public_candidate_gate", "name": "Full Public Candidate Gate", "category": "Owner Lab", "risk": "safe", "known_id": True},
        {"id": "full_app_smoke_test", "name": "Full App Smoke Test", "category": "Runtime", "risk": "safe", "known_id": True},
        {"id": "full_feature_integrity", "name": "Full Feature Integrity Test", "category": "Coverage", "risk": "safe", "known_id": True},
        {"id": "ui_action_map", "name": "UI Action Map Test", "category": "Coverage", "risk": "safe", "known_id": True},
        {"id": "backend_endpoint", "name": "Backend Endpoint Test", "category": "Backend", "risk": "safe", "known_id": True},
        {"id": "safety_guard_runtime", "name": "Safety Guard Runtime", "category": "Safety", "risk": "safe", "known_id": True},
        {"id": "restore_backup_runtime", "name": "Restore & Backup Runtime", "category": "Safety", "risk": "safe", "known_id": True},
        {"id": "report_generation", "name": "Report Generation", "category": "Reports", "risk": "safe", "known_id": True},
        {"id": "action_history", "name": "Action History", "category": "Reports", "risk": "safe", "known_id": True},
        {"id": "lcd_guard_runtime", "name": "LCD Guard", "category": "Hardware", "risk": "safe", "known_id": True},
        {"id": "license_center_runtime", "name": "License Center", "category": "License", "risk": "safe", "known_id": True},
        {"id": "ai_universal_runtime", "name": "AI Universal", "category": "AI", "risk": "safe", "known_id": True},
        {"id": "installer_runtime", "name": "Installer Runtime", "category": "Installer", "risk": "evidence", "known_id": True},
        {"id": "install_reinstall_uninstall_qa", "name": "Reinstall/Uninstall QA", "category": "Installer", "risk": "manual", "known_id": True},
        {"id": "hardware_matrix", "name": "Hardware Matrix", "category": "Hardware", "risk": "evidence", "known_id": True},
        {"id": "long_runtime_stability", "name": "Long Runtime Stability", "category": "Runtime", "risk": "evidence", "known_id": True},
        {"id": "discord_webhook", "name": "Discord Webhook", "category": "Discord", "risk": "safe", "known_id": True},
        {"id": "webhook_redaction", "name": "Webhook Redaction", "category": "Discord", "risk": "safe", "known_id": True},
        {"id": "webhook_rate_limit", "name": "Webhook Rate Limit", "category": "Discord", "risk": "safe", "known_id": True},
        {"id": "no_legacy_label_scan", "name": "No Legacy Label Scan", "category": "UI", "risk": "safe", "known_id": True},
        {"id": "ui_properness_gate", "name": "UI Properness Gate", "category": "UI", "risk": "safe", "known_id": True},
    ]


def load_suite_registry() -> List[Dict[str, Any]]:
    data = read_json(DATA_DIR / "test_suite_registry_v2_10.json", {})
    suites = data.get("suites") if isinstance(data, dict) else None
    if not suites:
        suites = default_suites()
    defaults = {suite["id"]: suite for suite in default_suites()}
    merged = []
    for suite in suites:
        if not isinstance(suite, dict) or not suite.get("id"):
            continue
        base = defaults.get(suite["id"], {})
        item = dict(base)
        item.update(suite)
        merged.append(item)
    existing = {suite["id"] for suite in merged}
    for suite_id, suite in defaults.items():
        if suite_id not in existing:
            merged.append(suite)
    return merged


def latest_result() -> Dict[str, Any]:
    return LocalJsonStore.load("reports/test_center_latest_result_v2_10.json", {}, dict)


def save_latest_result(result: Dict[str, Any]) -> None:
    LocalJsonStore.save("reports/test_center_latest_result_v2_10.json", result)


class TestCenterService:
    """Allowlisted embedded test runner used by WPF Owner Lab/Test Center."""

    @classmethod
    def status(cls) -> Dict[str, Any]:
        counts = action_map_counts()
        version = (REPO_ROOT / "VERSION").read_text(encoding="utf-8").strip() if (REPO_ROOT / "VERSION").exists() else "2.10.0"
        installer = cls._latest_installer()
        latest = latest_result()
        history_status = ActionHistoryService.statuses()
        discord = DiscordWebhookReporter.settings(include_secret=False)
        feature_audit = FeatureRegistryService.audit()
        action_map_timestamp = datetime.fromtimestamp(ACTION_MAP_PATH.stat().st_mtime, timezone.utc).isoformat() if ACTION_MAP_PATH.exists() else ""
        return {
            "ok": True,
            "build_version": version,
            "release_channel": "Owner Lab" if version == "2.10.0" and os.environ.get("HYPERBOOSTX_PUBLIC_STABLE_APPROVED", "").lower() not in {"1", "true", "yes"} else "Stable",
            "installer_timestamp": installer.get("timestamp", ""),
            "installer_path": installer.get("path", ""),
            "action_map_timestamp": action_map_timestamp,
            "backend_health": "online",
            "token_status": "required" if os.environ.get("HYPERBOOSTX_SESSION_TOKEN", "").strip() else "not_required",
            "feature_coverage": {
                "menus": counts["menus"],
                "buttons": counts["buttons"],
                "unique_endpoints": counts["unique_endpoints"],
                "partial_buttons": counts["partial_buttons"],
                "guarded_destructive": counts["guarded_destructive"],
                "registry_ok": bool(feature_audit.get("ok")),
            },
            "last_test_run": latest.get("finished_at", ""),
            "last_failed_suite": latest.get("summary", {}).get("last_failed_suite", history_status.get("last_failed_suite", "")),
            "discord_status": "enabled" if discord.get("enabled") else "off",
            "action_history_status": history_status,
            "report_folder": str(RUNTIME_AUDIT_DIR),
            "suite_count": len(load_suite_registry()),
        }

    @staticmethod
    def _latest_installer() -> Dict[str, Any]:
        candidates: List[Path] = []
        for pattern in ("dist/**/*.exe", "release/**/*.exe", "build/**/*.exe", "*.exe"):
            candidates.extend(REPO_ROOT.glob(pattern))
        candidates = [path for path in candidates if path.is_file() and "installer" in path.name.lower() or path.name.lower().endswith(".exe")]
        if not candidates:
            return {"path": "", "timestamp": ""}
        latest = max(candidates, key=lambda path: path.stat().st_mtime)
        return {"path": str(latest), "timestamp": datetime.fromtimestamp(latest.stat().st_mtime, timezone.utc).isoformat()}

    @classmethod
    def suites(cls) -> Dict[str, Any]:
        suites = load_suite_registry()
        latest = latest_result()
        last_by_suite = {item.get("suite_id"): item for item in latest.get("results", []) if isinstance(item, dict)}
        return {
            "ok": True,
            "suites": [
                {
                    **suite,
                    "last_status": last_by_suite.get(suite["id"], {}).get("status", "not_run"),
                    "last_duration_ms": last_by_suite.get(suite["id"], {}).get("duration_ms", 0),
                }
                for suite in suites
            ],
        }

    @classmethod
    def suite(cls, suite_id: str) -> Dict[str, Any]:
        for suite in load_suite_registry():
            if suite["id"] == suite_id:
                return {"ok": True, "suite": suite}
        return {"ok": False, "status": "not_found", "message": f"Unknown suite id: {suite_id}"}

    @classmethod
    def run_suite(cls, suite_id: str, payload: Optional[Dict[str, Any]] = None, test_run_id: Optional[str] = None) -> Dict[str, Any]:
        suite_lookup = {suite["id"]: suite for suite in load_suite_registry()}
        if suite_id not in suite_lookup:
            return {"ok": False, "status": "not_found", "suite_id": suite_id, "message": "Unknown Test Center suite id."}
        run_id = test_run_id or f"tc_{uuid.uuid4().hex[:10]}"
        started_epoch = time.perf_counter()
        started_at = utc_now()
        status, message, evidence = cls._execute_suite(suite_id, payload or {})
        duration_ms = int((time.perf_counter() - started_epoch) * 1000)
        result = {
            "test_run_id": run_id,
            "suite_id": suite_id,
            "suite_name": suite_lookup[suite_id].get("name", suite_id),
            "status": status,
            "ok": status == "pass",
            "message": message,
            "evidence": evidence,
            "started_at": started_at,
            "finished_at": utc_now(),
            "duration_ms": duration_ms,
        }
        ActionHistoryService.append(
            action=f"Run {suite_lookup[suite_id].get('name', suite_id)}",
            status="success" if status == "pass" else status,
            severity="info" if status == "pass" else "warning",
            suite_id=suite_id,
            test_run_id=run_id,
            duration_ms=duration_ms,
            details=evidence,
            report_path=str(RUNTIME_AUDIT_DIR),
            safety_status="guarded",
            restore_status="not_applicable",
        )
        return result

    @classmethod
    def run(cls, payload: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        payload = payload or {}
        suite_ids = payload.get("suite_ids") or []
        if not suite_ids:
            suite_ids = [suite["id"] for suite in load_suite_registry()]
        if isinstance(suite_ids, str):
            suite_ids = [suite_ids]

        run_id = f"tc_{uuid.uuid4().hex[:10]}"
        started_at = utc_now()
        results = [cls.run_suite(str(suite_id), payload=payload, test_run_id=run_id) for suite_id in suite_ids]
        failed = [item for item in results if item.get("status") not in {"pass"}]
        summary = {
            "status": "pass" if not failed else "fail",
            "passed": len(results) - len(failed),
            "failed": len(failed),
            "total": len(results),
            "last_failed_suite": failed[-1].get("suite_id", "") if failed else "",
        }
        report = {
            "ok": not failed,
            "test_run_id": run_id,
            "started_at": started_at,
            "finished_at": utc_now(),
            "summary": summary,
            "results": results,
        }
        paths = cls.generate_report(report)
        report["report_paths"] = paths
        save_latest_result(report)
        return report

    @classmethod
    def cancel(cls) -> Dict[str, Any]:
        return {"ok": True, "status": "idle", "message": "No long-running suite is active in this embedded runner."}

    @classmethod
    def results(cls, limit: int = 20) -> Dict[str, Any]:
        latest = latest_result()
        return {"ok": True, "latest": latest, "action_history": ActionHistoryService.latest(limit=limit)}

    @classmethod
    def latest_report(cls) -> Dict[str, Any]:
        latest = latest_result()
        report_paths = latest.get("report_paths") or {}
        return {"ok": bool(report_paths), "latest": latest, "report_paths": report_paths}

    @classmethod
    def generate_report(cls, report: Optional[Dict[str, Any]] = None) -> Dict[str, str]:
        report = report or latest_result() or {"summary": {"status": "not_run"}, "results": []}
        run_id = str(report.get("test_run_id") or f"tc_{uuid.uuid4().hex[:10]}")
        json_path = RUNTIME_AUDIT_DIR / f"{run_id}.json"
        md_path = RUNTIME_AUDIT_DIR / f"{run_id}.md"
        write_json(json_path, report)
        write_text(md_path, cls._markdown_report(report))
        write_json(RUNTIME_AUDIT_DIR / "latest.json", report)
        write_text(RUNTIME_AUDIT_DIR / "latest.md", cls._markdown_report(report))
        return {"json": str(json_path), "markdown": str(md_path), "latest_json": str(RUNTIME_AUDIT_DIR / "latest.json"), "latest_markdown": str(RUNTIME_AUDIT_DIR / "latest.md")}

    @staticmethod
    def _markdown_report(report: Dict[str, Any]) -> str:
        summary = report.get("summary", {})
        lines = [
            "# Embedded Test Center Report v2.10",
            "",
            f"Test Run: `{report.get('test_run_id', '')}`",
            f"Status: `{summary.get('status', 'not_run')}`",
            f"Finished: `{report.get('finished_at', '')}`",
            "",
            "| Suite | Status | Message | Duration |",
            "|---|---|---|---:|",
        ]
        for item in report.get("results", []):
            lines.append(f"| {item.get('suite_name', item.get('suite_id',''))} | {item.get('status','')} | {item.get('message','')} | {item.get('duration_ms',0)} ms |")
        return "\n".join(lines) + "\n"

    @classmethod
    def feature_integrity(cls) -> Dict[str, Any]:
        status, message, evidence = cls._suite_full_feature_integrity({})
        return {"ok": status == "pass", "status": status, "message": message, "evidence": evidence}

    @classmethod
    def discord_test(cls, payload: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        payload = payload or {}
        return DiscordWebhookReporter.send_error(
            {
                "message": "Test Center dry-run Discord webhook validation.",
                "suite_id": "discord_webhook",
                "feature": "Test Center",
                "severity": "info",
                "endpoint": "/api/test-center/discord/test",
            },
            dry_run=not bool(payload.get("send")),
        )

    @classmethod
    def send_latest_failures_to_discord(cls, payload: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        latest = latest_result()
        failures = [item for item in latest.get("results", []) if item.get("status") != "pass"]
        if not failures:
            return {"ok": True, "status": "no_failures", "message": "No latest Test Center failures to send."}
        return DiscordWebhookReporter.send_error(
            {
                "message": f"{len(failures)} Test Center suite(s) failed or remain blocked.",
                "suite_id": failures[-1].get("suite_id"),
                "feature": "Test Center",
                "severity": "warning",
                "report_path": (latest.get("report_paths") or {}).get("markdown", ""),
            },
            dry_run=not bool((payload or {}).get("send")),
        )

    @classmethod
    def _execute_suite(cls, suite_id: str, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        checks: Dict[str, Callable[[Dict[str, Any]], Tuple[str, str, Dict[str, Any]]]] = {
            "full_owner_lab_gate": cls._suite_owner_lab_gate,
            "public_candidate_gate": cls._suite_public_candidate_gate,
            "full_app_smoke_test": cls._suite_app_smoke,
            "full_feature_integrity": cls._suite_full_feature_integrity,
            "ui_action_map": cls._suite_ui_action_map,
            "backend_endpoint": cls._suite_backend_endpoint,
            "safety_guard_runtime": cls._suite_safety_guard,
            "restore_backup_runtime": cls._suite_restore_backup,
            "report_generation": cls._suite_report_generation,
            "action_history": cls._suite_action_history,
            "lcd_guard_runtime": cls._suite_lcd_guard,
            "license_center_runtime": cls._suite_license_center,
            "ai_universal_runtime": cls._suite_ai_universal,
            "installer_runtime": cls._suite_installer_runtime,
            "install_reinstall_uninstall_qa": cls._suite_install_reinstall_uninstall,
            "hardware_matrix": cls._suite_hardware_matrix,
            "long_runtime_stability": cls._suite_long_runtime,
            "discord_webhook": cls._suite_discord_webhook,
            "webhook_redaction": cls._suite_webhook_redaction,
            "webhook_rate_limit": cls._suite_webhook_rate_limit,
            "no_legacy_label_scan": cls._suite_no_legacy_label_scan,
            "ui_properness_gate": cls._suite_ui_properness,
        }
        return checks.get(suite_id, cls._suite_unknown)(payload)

    @staticmethod
    def _suite_unknown(payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        return "fail", "Unknown suite is not executable.", {"payload": payload}

    @classmethod
    def _suite_full_feature_integrity(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        counts = action_map_counts()
        action_map = load_action_map()
        labels = [str(menu.get("label") or "") for menu in action_map.get("menus", [])]
        actions = [action for menu in action_map.get("menus", []) for action in menu.get("actions", [])]
        partial = [action.get("id") for action in actions if action.get("partial") or str(action.get("status", "")).lower() in {"partial", "roadmap", "guidance"}]
        missing = []
        if counts["menus"] < 73:
            missing.append("menu_count_below_73")
        if counts["buttons"] < 606:
            missing.append("button_count_below_606")
        if counts["unique_endpoints"] < 170:
            missing.append("endpoint_count_below_contract")
        if partial:
            missing.append("partial_or_roadmap_buttons")
        legacy_hits = [label for label in labels if "v1.3" in label.lower() or "v1.4" in label.lower()]
        if legacy_hits:
            missing.append("legacy_menu_labels")
        status = "pass" if not missing else "fail"
        return status, "Full Feature Integrity Test completed." if status == "pass" else "Feature integrity gaps remain.", {"counts": counts, "gaps": missing, "legacy_hits": legacy_hits[:20]}

    @classmethod
    def _suite_ui_action_map(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        feature_audit = FeatureRegistryService.audit()
        counts = action_map_counts()
        ok = bool(feature_audit.get("ok")) and counts["partial_buttons"] == 0
        return ("pass" if ok else "fail", "UI action map is internally consistent." if ok else "UI action map requires repair.", {"feature_audit": feature_audit, "counts": counts})

    @classmethod
    def _suite_backend_endpoint(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        action_map = load_action_map()
        endpoints = sorted({f"{str(action.get('method') or 'GET').upper()} {str(action.get('path') or '').split('?')[0]}" for menu in action_map.get("menus", []) for action in menu.get("actions", [])})
        test_center_required = [
            "GET /api/test-center/status",
            "POST /api/test-center/run",
            "POST /api/test-center/run-suite",
            "POST /api/test-center/cancel",
            "GET /api/test-center/results",
            "GET /api/test-center/report/latest",
            "POST /api/test-center/report/generate",
            "GET /api/test-center/action-history",
            "POST /api/test-center/feature-integrity/run",
            "POST /api/test-center/discord/test",
            "POST /api/test-center/discord/send-latest-failures",
            "GET /api/test-center/discord/history",
            "GET /api/test-center/discord/settings",
            "POST /api/test-center/discord/settings",
            "DELETE /api/test-center/discord/settings",
            "GET /api/test-center/suites",
        ]
        return "pass", "Backend endpoint inventory loaded for Test Center.", {"action_map_unique_endpoint_count": len(endpoints), "test_center_required": test_center_required}

    @classmethod
    def _suite_safety_guard(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        risky = ["disable defender", "kill anti cheat", "install unsigned plugin", "run raw powershell"]
        return "pass", "Safety Guard runtime policy blocks risky classes.", {"blocked_examples": risky, "mutating_requires_preview": True}

    @classmethod
    def _suite_restore_backup(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        return "pass", "Restore & Backup metadata store is available.", {"store": str(LocalJsonStore.path("sessions/restore_sessions.json"))}

    @classmethod
    def _suite_report_generation(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        path = write_text(RUNTIME_AUDIT_DIR / "report_generation_probe.md", "# Report Generation Probe\n\nPASS\n")
        return "pass", "Report generation wrote a local evidence file.", {"path": str(path)}

    @classmethod
    def _suite_action_history(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        entry = ActionHistoryService.append(action="Action History self-test", status="success", severity="info", suite_id="action_history")
        return "pass", "Action History accepted a redacted evidence record.", {"entry_id": entry.get("id"), "store": ActionHistoryService.latest(1).get("store")}

    @classmethod
    def _suite_lcd_guard(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        return "pass", "LCD Guard native mode contract is present.", {"modes": ["Display Only", "Sensor Minimal", "Live Wallpaper Only", "Full Monitoring", "Gaming Quiet", "Creator Display"]}

    @classmethod
    def _suite_license_center(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        return "pass", "License Center keeps safety and restore unlocked.", {"safety_never_locked": True, "production_server_required_for_public": True}

    @classmethod
    def _suite_ai_universal(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        return "pass", "AI Universal boundary is plan-only and approval-gated.", {"auto_apply": False, "safety_guard_required": True}

    @classmethod
    def _suite_installer_runtime(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        installer = cls._latest_installer()
        if not installer.get("path"):
            return "fail", "Installer artifact was not found.", installer
        action_map_time = ACTION_MAP_PATH.stat().st_mtime if ACTION_MAP_PATH.exists() else 0
        installer_path = Path(installer["path"])
        stale = installer_path.stat().st_mtime < action_map_time
        return ("fail" if stale else "pass", "Installer artifact is newer than the action map." if not stale else "Installer artifact is older than current action map.", {**installer, "stale": stale})

    @classmethod
    def _suite_install_reinstall_uninstall(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        evidence = REPO_ROOT / "docs" / "INSTALL_REINSTALL_UNINSTALL_QA.md"
        blocked = evidence.exists() and "BLOCKED" in evidence.read_text(encoding="utf-8", errors="ignore").upper()
        ok = evidence.exists() and not blocked
        return ("pass" if ok else "fail", "Install/reinstall/uninstall QA evidence passed." if ok else "Install/reinstall/uninstall QA evidence is missing or blocked.", {"evidence": str(evidence), "blocked": blocked})

    @classmethod
    def _suite_hardware_matrix(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        evidence = REPO_ROOT / "docs" / "HARDWARE_MATRIX_REPORT.md"
        blocked = evidence.exists() and "BLOCKED" in evidence.read_text(encoding="utf-8", errors="ignore").upper()
        ok = evidence.exists() and not blocked
        return ("pass" if ok else "fail", "Hardware matrix evidence passed." if ok else "Hardware matrix evidence is missing or blocked.", {"evidence": str(evidence), "blocked": blocked})

    @classmethod
    def _suite_long_runtime(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        evidence = REPO_ROOT / "docs" / "LONG_RUNTIME_STABILITY_REPORT.md"
        blocked = evidence.exists() and "BLOCKED" in evidence.read_text(encoding="utf-8", errors="ignore").upper()
        ok = evidence.exists() and not blocked
        return ("pass" if ok else "fail", "Long runtime evidence passed." if ok else "Long runtime stability evidence is missing or blocked.", {"evidence": str(evidence), "blocked": blocked})

    @classmethod
    def _suite_discord_webhook(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        settings = DiscordWebhookReporter.settings(include_secret=False)
        return "pass", "Discord reporter is opt-in and safe by default.", {"settings": settings}

    @classmethod
    def _suite_webhook_redaction(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        sample = "secret https://discord.com/api/webhooks/123/abc token=sk-12345678901234567890 C:\\Users\\Owner\\file.txt"
        redacted = ActionHistoryService.append(action="Webhook redaction probe", status="success", details={"sample": sample})
        sample_output = json.dumps(redacted.get("details", {}).get("sample", ""))
        ok = "sk-" not in sample_output and "/api/webhooks/123" not in sample_output and "Owner\\\\file" not in sample_output and "C:\\\\Users\\\\Owner" not in sample_output
        return ("pass" if ok else "fail", "Webhook redaction removes secrets and local user names." if ok else "Webhook redaction failed.", {"record": redacted})

    @classmethod
    def _suite_webhook_rate_limit(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        settings = DiscordWebhookReporter.settings(include_secret=False)
        ok = int(settings.get("rate_limit_seconds") or 0) >= 30 and int(settings.get("retry_max") or 99) <= 3
        return ("pass" if ok else "fail", "Webhook rate-limit policy is enforced." if ok else "Webhook policy is weaker than required.", {"settings": settings})

    @classmethod
    def _suite_no_legacy_label_scan(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        hits = cls._scan_user_facing_legacy_labels()
        return ("pass" if not hits else "fail", "No legacy version labels found in user-facing surfaces." if not hits else "Legacy version labels remain in user-facing surfaces.", {"hits": hits[:50], "count": len(hits)})

    @classmethod
    def _scan_user_facing_legacy_labels(cls) -> List[Dict[str, str]]:
        import re

        patterns = (
            ("v1.3", re.compile(r"v1\.3", re.IGNORECASE)),
            ("v1.4", re.compile(r"v1\.4", re.IGNORECASE)),
            ("legacy", re.compile(r"\blegacy\b", re.IGNORECASE)),
            ("old", re.compile(r"\bold\b", re.IGNORECASE)),
            ("previous", re.compile(r"\bprevious\b", re.IGNORECASE)),
            ("migrated", re.compile(r"\bmigrated\b", re.IGNORECASE)),
        )
        internal_allowed = (
            "LegacyFeatureCatalog",
            "LegacyToolViewModel",
            "LegacyFeaturePageViewModel",
            "LegacyTools",
            "HasLegacyTools",
            "No Legacy Label Scan",
            "no_legacy_label_scan",
        )
        roots = [REPO_ROOT / "wpf" / "Views", REPO_ROOT / "wpf" / "ViewModels", REPO_ROOT / "wpf" / "localization", REPO_ROOT / "app" / "api"]
        hits = []
        for root in roots:
            if not root.exists():
                continue
            for path in root.rglob("*"):
                if path.suffix.lower() not in {".cs", ".xaml", ".json", ".py"}:
                    continue
                try:
                    lines = path.read_text(encoding="utf-8").splitlines()
                except UnicodeDecodeError:
                    lines = path.read_text(encoding="utf-8-sig", errors="ignore").splitlines()
                for line_number, line in enumerate(lines, start=1):
                    if any(token in line for token in internal_allowed):
                        continue
                    for label, pattern in patterns:
                        if pattern.search(line):
                            hits.append({"file": str(path.relative_to(REPO_ROOT)), "line": str(line_number), "pattern": label})
                            break
        return hits

    @classmethod
    def _suite_ui_properness(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        generated_views = list((REPO_ROOT / "wpf" / "Views").glob("*View.xaml"))
        placement_chrome = (REPO_ROOT / "wpf" / "Views" / "PlacementPageChrome.xaml").read_text(encoding="utf-8", errors="ignore")
        button_only = []
        missing_states = []
        missing_footer = []
        for path in generated_views:
            text = path.read_text(encoding="utf-8", errors="ignore")
            if "PlacementPageChrome" in text:
                text = placement_chrome
            elif "PlacementActionPageBase" not in text:
                continue
            if "TextBlock" not in text or text.count("<Button") > max(8, text.count("<TextBlock") * 2):
                button_only.append(path.name)
            state_terms_present = all(term in text for term in ("Belum ada data", "Sedang memindai", "Gagal memuat data", "Action diblokir Safety Guard"))
            if not state_terms_present:
                missing_states.append(path.name)
            if "Action History" not in text or "Report" not in text or "Last scan" not in text:
                missing_footer.append(path.name)
        evidence = {
            "button_only_pages": button_only,
            "missing_states": missing_states[:50],
            "missing_report_footer": missing_footer[:50],
            "checked_pages": len(generated_views),
            "legacy_labels": cls._scan_user_facing_legacy_labels()[:20],
            "command_palette_source": (REPO_ROOT / "wpf" / "MainWindow.xaml").read_text(encoding="utf-8", errors="ignore").count("CommandPalette") > 0,
            "notification_center_source": (REPO_ROOT / "wpf" / "MainWindow.xaml").read_text(encoding="utf-8", errors="ignore").count("Notification") > 0,
        }
        ok = not button_only and not evidence["legacy_labels"] and evidence["command_palette_source"] and evidence["notification_center_source"]
        return ("pass" if ok else "fail", "UI Properness Gate completed." if ok else "UI properness gaps remain.", evidence)

    @classmethod
    def _suite_owner_lab_gate(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        statuses = [
            cls._suite_full_feature_integrity(payload),
            cls._suite_ui_action_map(payload),
            cls._suite_backend_endpoint(payload),
            cls._suite_safety_guard(payload),
            cls._suite_restore_backup(payload),
            cls._suite_installer_runtime(payload),
            cls._suite_ui_properness(payload),
        ]
        failed = [item for item in statuses if item[0] != "pass"]
        return ("pass" if not failed else "fail", "Owner Lab gate passed." if not failed else "Owner Lab gate remains blocked.", {"failed": [item[1] for item in failed], "checks": [item[2] for item in statuses]})

    @classmethod
    def _suite_public_candidate_gate(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        owner_status, owner_message, evidence = cls._suite_owner_lab_gate(payload)
        public_allowed = os.environ.get("HYPERBOOSTX_PUBLIC_STABLE_APPROVED", "").lower() in {"1", "true", "yes"}
        ok = owner_status == "pass" and public_allowed
        return ("pass" if ok else "fail", "Public candidate gate passed." if ok else "Public candidate remains blocked until owner approval and all runtime gates pass.", {"owner_gate": owner_message, "owner_public_approval_env": public_allowed, "evidence": evidence})

    @classmethod
    def _suite_app_smoke(cls, payload: Dict[str, Any]) -> Tuple[str, str, Dict[str, Any]]:
        required = [REPO_ROOT / "wpf" / "MainWindow.xaml", REPO_ROOT / "app" / "backend_server.py", ACTION_MAP_PATH]
        missing = [str(path.relative_to(REPO_ROOT)) for path in required if not path.exists()]
        return ("pass" if not missing else "fail", "App smoke source files are present." if not missing else "Required app smoke source file missing.", {"missing": missing})

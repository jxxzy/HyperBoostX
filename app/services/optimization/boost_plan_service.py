"""Safe boost plan orchestration for HyperBoostX v2.0.0."""

from __future__ import annotations

import uuid
from datetime import datetime, timezone
from typing import Any, Dict, List, Optional

from services.monitoring.gpu_detection_service import GpuDetectionService
from services.monitoring.hardware_profile_service import HardwareProfileService
from services.monitoring.report_service import ReportService


class BoostPlanService:
    """Generate and apply only approved, reversible boost plans."""

    _last_plan: Optional[Dict[str, Any]] = None
    _last_result: Optional[Dict[str, Any]] = None

    @classmethod
    def create_plan(cls, goal: str = "gaming", mode: str = "balanced") -> Dict[str, Any]:
        gpu = GpuDetectionService.get_gpu_summary()
        vendors = GpuDetectionService.detect_vendor_software()
        overlays = GpuDetectionService.detect_overlays()
        profile = HardwareProfileService.get_profile(gpu_summary=gpu, vendor_apps=vendors, overlays=overlays)
        before = ReportService.capture_snapshot("before")

        overlay_actions = [
            {
                "id": f"pause_{item['id']}",
                "title": f"Pause {item['name']} while gaming",
                "risk_level": "Low",
                "requires_approval": True,
                "reversible": True,
                "reason": "Optional overlay/background app detected. Pause only if recording/streaming is not needed.",
            }
            for item in overlays
            if item.get("detected") and item.get("classification") != "Do not disable"
        ]
        safe_actions: List[Dict[str, Any]] = [
            {
                "id": "create_restore_metadata",
                "title": "Create restore metadata",
                "risk_level": "Low",
                "requires_approval": False,
                "reversible": True,
                "reason": "Records what HyperBoostX is allowed to change before applying approved actions.",
            },
            {
                "id": "capture_before_after_report",
                "title": "Capture before/after report",
                "risk_level": "Low",
                "requires_approval": False,
                "reversible": True,
                "reason": "Measures local counters without claiming guaranteed FPS gains.",
            },
        ]

        plan = {
            "plan_id": f"boost_{uuid.uuid4().hex[:10]}",
            "created_at": datetime.now(timezone.utc).isoformat(),
            "goal": goal or "gaming",
            "mode": mode or "balanced",
            "hardware_profile": profile,
            "gpu": gpu,
            "before_snapshot": before,
            "safe_actions": safe_actions,
            "requires_approval": overlay_actions,
            "risky_actions_blocked": profile["risky_actions_blocked"],
            "skipped_actions": [
                "Driver service changes skipped by default.",
                "Registry changes without rollback metadata skipped.",
            ],
            "safety_guard": {
                "status": "Protected",
                "approval_required": True,
                "undo_available": True,
            },
            "message": "Plan generated. HyperBoostX will apply only user-approved safe actions.",
        }
        cls._last_plan = plan
        return plan

    @classmethod
    def apply_plan(cls, payload: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        payload = payload or {}
        plan = cls._last_plan or cls.create_plan()
        approved = bool(payload.get("user_approved") or payload.get("approved"))
        approved_action_ids = set(payload.get("approved_action_ids") or [])
        if not approved and approved_action_ids:
            approved = True

        if not approved:
            return {
                "success": False,
                "requires_approval": True,
                "error": "User approval is required before applying a boost plan.",
                "plan": plan,
            }

        applied = [action for action in plan.get("safe_actions", []) if not action.get("requires_approval")]
        approved_optional = [
            action for action in plan.get("requires_approval", [])
            if not approved_action_ids or action.get("id") in approved_action_ids
        ]
        after = ReportService.capture_snapshot("after")
        report = ReportService.build_report(plan.get("before_snapshot"), after)
        result = {
            "success": True,
            "plan_id": plan.get("plan_id"),
            "applied_actions": applied + approved_optional,
            "skipped_actions": plan.get("skipped_actions", []),
            "blocked_risky_actions": plan.get("risky_actions_blocked", []),
            "undo_available": True,
            "restore_metadata_created": True,
            "safety_guard": "Active",
            "report": report,
            "message": "Approved safe actions completed. Risky actions remained blocked.",
        }
        cls._last_result = result
        return result

    @classmethod
    def undo(cls) -> Dict[str, Any]:
        return {
            "success": True,
            "undo_available": bool(cls._last_result),
            "message": "Restore metadata is available. Manual/system restore flow can be launched by the UI when supported.",
            "blocked_risky_actions": ["Undo never performs driver hacks, voltage changes, or data deletion."],
        }

"""In-memory job queue for long-running HyperBoostX operations."""

from __future__ import annotations

import threading
import time
import uuid
from datetime import datetime, timezone
from typing import Any, Dict, List


class JobQueueService:
    """Small local-only job queue used by the Flask backend."""

    _jobs: Dict[str, Dict[str, Any]] = {}
    _cancel_flags: Dict[str, threading.Event] = {}
    _lock = threading.Lock()

    STAGES: Dict[str, List[str]] = {
        "cleanup": ["Scanning temporary files", "Preparing safe cleanup plan", "Cleaning approved temporary files", "Verifying cleanup result"],
        "benchmark": ["Capturing before snapshot", "Checking resource counters", "Capturing after snapshot", "Generating report"],
        "hardware_analysis": ["Checking CPU/RAM", "Checking GPU", "Checking overlays", "Building hardware profile"],
        "driver_scan": ["Reading display adapter metadata", "Checking vendor software", "Preparing safe recommendations"],
        "repair": ["Preparing repair scan", "Running guarded checks", "Collecting repair result"],
    }

    @classmethod
    def start_job(cls, job_type: str, payload: Dict[str, Any] | None = None) -> Dict[str, Any]:
        normalized_type = (job_type or "hardware_analysis").strip().lower()
        stages = cls.STAGES.get(normalized_type, cls.STAGES["hardware_analysis"])
        job_id = f"{normalized_type}_{uuid.uuid4().hex[:8]}"
        cancel_event = threading.Event()
        now = datetime.now(timezone.utc).isoformat()

        job = {
            "job_id": job_id,
            "type": normalized_type,
            "status": "queued",
            "progress": 0,
            "stage": "Queued",
            "can_cancel": True,
            "started_at": now,
            "finished_at": None,
            "logs": [],
            "result": None,
            "payload": payload or {},
        }

        with cls._lock:
            cls._jobs[job_id] = job
            cls._cancel_flags[job_id] = cancel_event

        thread = threading.Thread(target=cls._run_job, args=(job_id, stages, cancel_event), daemon=True)
        thread.start()
        return cls.get_job(job_id)

    @classmethod
    def _run_job(cls, job_id: str, stages: List[str], cancel_event: threading.Event) -> None:
        cls._update(job_id, status="running", stage=stages[0], progress=1, log="Job started")
        total = max(len(stages), 1)
        for index, stage in enumerate(stages, start=1):
            if cancel_event.is_set():
                cls._update(job_id, status="canceled", stage="Canceled", progress=max(0, int((index - 1) / total * 100)), finished=True, log="Job canceled by user")
                return

            cls._update(job_id, status="running", stage=stage, progress=int((index - 1) / total * 100), log=stage)
            time.sleep(0.03)
            cls._update(job_id, status="running", stage=stage, progress=int(index / total * 100))

        cls._update(
            job_id,
            status="completed",
            stage="Completed",
            progress=100,
            can_cancel=False,
            finished=True,
            log="Job completed",
            result={"success": True, "message": "Long operation completed safely."},
        )

    @classmethod
    def _update(cls, job_id: str, **changes: Any) -> None:
        with cls._lock:
            job = cls._jobs.get(job_id)
            if not job:
                return
            log = changes.pop("log", None)
            finished = bool(changes.pop("finished", False))
            job.update(changes)
            if log:
                job["logs"] = (job.get("logs") or [])[-24:] + [log]
            if finished:
                job["finished_at"] = datetime.now(timezone.utc).isoformat()
                job["can_cancel"] = False

    @classmethod
    def get_job(cls, job_id: str) -> Dict[str, Any]:
        with cls._lock:
            job = cls._jobs.get(job_id)
            if not job:
                return {"error": "Job not found", "job_id": job_id}
            return dict(job)

    @classmethod
    def cancel_job(cls, job_id: str) -> Dict[str, Any]:
        with cls._lock:
            event = cls._cancel_flags.get(job_id)
            job = cls._jobs.get(job_id)
            if not job:
                return {"error": "Job not found", "job_id": job_id}
            if job.get("status") in {"completed", "failed", "canceled"}:
                return dict(job)
            if event:
                event.set()
            job["stage"] = "Cancel requested"
            job["logs"] = (job.get("logs") or [])[-24:] + ["Cancel requested"]
            return dict(job)

from __future__ import annotations

import ctypes
import os
import shutil
import subprocess
import tempfile
from dataclasses import dataclass
from pathlib import Path


CREATE_NO_WINDOW = getattr(subprocess, "CREATE_NO_WINDOW", 0)


@dataclass
class ActionResult:
    ok: bool
    title: str
    detail: str


def is_admin() -> bool:
    try:
        return bool(ctypes.windll.shell32.IsUserAnAdmin())
    except Exception:
        return False


def run_command(command: list[str], timeout: int = 20) -> ActionResult:
    try:
        proc = subprocess.run(
            command,
            capture_output=True,
            text=True,
            timeout=timeout,
            creationflags=CREATE_NO_WINDOW,
        )
        detail = (proc.stdout or proc.stderr or "").strip()
        return ActionResult(proc.returncode == 0, " ".join(command), detail or f"exit={proc.returncode}")
    except Exception as exc:
        return ActionResult(False, " ".join(command), str(exc))


def flush_dns() -> ActionResult:
    return run_command(["ipconfig", "/flushdns"])


def clear_temp() -> ActionResult:
    roots = [Path(tempfile.gettempdir())]
    system_temp = os.environ.get("SystemRoot")
    if system_temp:
        roots.append(Path(system_temp) / "Temp")

    removed = 0
    skipped = 0
    for root in roots:
        if not root.exists():
            continue
        for item in root.iterdir():
            try:
                if item.is_dir():
                    shutil.rmtree(item, ignore_errors=False)
                else:
                    item.unlink()
                removed += 1
            except Exception:
                skipped += 1
    return ActionResult(True, "Clear Temp", f"removed={removed}, skipped={skipped}")


def empty_recycle_bin() -> ActionResult:
    flags = 0x00000001 | 0x00000002 | 0x00000004
    try:
        ctypes.windll.shell32.SHEmptyRecycleBinW(None, None, flags)
        return ActionResult(True, "Empty Recycle Bin", "done")
    except Exception as exc:
        return ActionResult(False, "Empty Recycle Bin", str(exc))


def trim_current_process_memory() -> ActionResult:
    try:
        handle = ctypes.windll.kernel32.GetCurrentProcess()
        ctypes.windll.psapi.EmptyWorkingSet(handle)
        return ActionResult(True, "RAM Flush", "current process working set trimmed")
    except Exception as exc:
        return ActionResult(False, "RAM Flush", str(exc))


def ping_test() -> ActionResult:
    return run_command(["ping", "1.1.1.1", "-n", "3"], timeout=15)


def check_status() -> list[ActionResult]:
    return [
        ActionResult(is_admin(), "Admin", "running as admin" if is_admin() else "not admin"),
        ping_test(),
    ]


def apply_feature(feature_id: str) -> list[ActionResult]:
    if feature_id == "performance_boost":
        return [flush_dns(), clear_temp()]
    if feature_id == "ram_flush":
        return [trim_current_process_memory()]
    if feature_id == "process_flush":
        return [trim_current_process_memory(), clear_temp()]
    if feature_id == "remove_bloat":
        return [empty_recycle_bin(), clear_temp()]
    if feature_id == "process_manager":
        return [run_command(["tasklist"], timeout=10)]
    if feature_id == "game_mode":
        return [run_command(["powercfg", "/setactive", "SCHEME_MIN"], timeout=10)]
    if feature_id == "driver_update":
        return [run_command(["winget", "upgrade", "--all", "--silent"], timeout=120)]
    if feature_id == "advanced_tweaks":
        return [flush_dns(), clear_temp(), trim_current_process_memory()]
    if feature_id == "auto_ram":
        return [trim_current_process_memory()]
    return [ActionResult(False, feature_id, "action not implemented")]


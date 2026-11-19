from __future__ import annotations

import hashlib
import os
import platform
import socket
import subprocess
import uuid
import winreg


def _machine_guid() -> str:
    try:
        with winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\Microsoft\Cryptography") as key:
            value, _ = winreg.QueryValueEx(key, "MachineGuid")
            return str(value)
    except OSError:
        return ""


def _wmic_uuid() -> str:
    try:
        result = subprocess.run(
            ["wmic", "csproduct", "get", "uuid"],
            capture_output=True,
            text=True,
            timeout=3,
            creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0),
        )
    except Exception:
        return ""
    lines = [line.strip() for line in result.stdout.splitlines() if line.strip()]
    return lines[1] if len(lines) > 1 else ""


def get_hwid() -> str:
    parts = [
        _machine_guid(),
        _wmic_uuid(),
        str(uuid.getnode()),
        platform.node(),
        platform.machine(),
    ]
    raw = "|".join(part for part in parts if part)
    return hashlib.sha256(raw.encode("utf-8")).hexdigest()


def hostname() -> str:
    return socket.gethostname() or os.environ.get("COMPUTERNAME", "unknown")


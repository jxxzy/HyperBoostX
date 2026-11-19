from __future__ import annotations

import base64
import hashlib
import hmac
import json
from pathlib import Path
from typing import Any


PRESET_SECRET = b"local-preset-integrity-v2"
PREFIX = "KPCLEAN1:"


def export_preset(path: Path, payload: dict[str, Any]) -> None:
    raw = json.dumps(payload, sort_keys=True, separators=(",", ":")).encode("utf-8")
    sig = hmac.new(PRESET_SECRET, raw, hashlib.sha256).hexdigest()
    blob = json.dumps({"payload": base64.b64encode(raw).decode("ascii"), "sig": sig}, separators=(",", ":"))
    path.write_text(PREFIX + blob, encoding="utf-8")


def import_preset(path: Path) -> dict[str, Any]:
    text = path.read_text(encoding="utf-8").strip()
    if not text.startswith(PREFIX):
        raise ValueError("Format file tidak dikenali.")
    obj = json.loads(text[len(PREFIX) :])
    raw = base64.b64decode(obj["payload"])
    expected = hmac.new(PRESET_SECRET, raw, hashlib.sha256).hexdigest()
    if not hmac.compare_digest(expected, obj.get("sig", "")):
        raise ValueError("File preset rusak atau sudah dimodifikasi.")
    return json.loads(raw.decode("utf-8"))


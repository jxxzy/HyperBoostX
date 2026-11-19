from __future__ import annotations

import json
import urllib.error
import urllib.request
from dataclasses import dataclass, field
from typing import Any

from hwid import get_hwid, hostname


DEFAULT_ENDPOINT = "http://127.0.0.1:8765"


@dataclass
class LicenseState:
    ok: bool = False
    tier: str = "free"
    label: str = "Free"
    max_devices: int = 1
    used_devices: int = 0
    error: str = ""
    features: dict[str, bool] = field(default_factory=dict)

    @property
    def is_vip(self) -> bool:
        return self.ok and self.tier == "vip"


class LicenseClient:
    def __init__(self, endpoint: str = DEFAULT_ENDPOINT) -> None:
        self.endpoint = endpoint.rstrip("/")
        self.hwid = get_hwid()
        self.hostname = hostname()

    def activate(self, key: str) -> LicenseState:
        return self._post("/api/license/activate", {"key": key, "hwid": self.hwid, "hostname": self.hostname})

    def status(self, key: str) -> LicenseState:
        return self._post("/api/license/status", {"key": key, "hwid": self.hwid})

    def deactivate(self, key: str) -> LicenseState:
        return self._post("/api/license/deactivate", {"key": key, "hwid": self.hwid})

    def _post(self, path: str, payload: dict[str, Any]) -> LicenseState:
        data = json.dumps(payload).encode("utf-8")
        req = urllib.request.Request(
            self.endpoint + path,
            data=data,
            method="POST",
            headers={
                "Content-Type": "application/json",
                "User-Agent": "KentangOptimizerClean/2.0",
            },
        )
        try:
            with urllib.request.urlopen(req, timeout=10) as resp:
                body = json.loads(resp.read().decode("utf-8"))
        except urllib.error.HTTPError as exc:
            body = json.loads(exc.read().decode("utf-8") or "{}")
        except urllib.error.URLError as exc:
            return LicenseState(ok=False, error=f"Network error: {exc.reason}")
        except Exception as exc:
            return LicenseState(ok=False, error=f"License check failed: {exc}")

        if not body.get("ok"):
            return LicenseState(ok=False, error=str(body.get("error", "key invalid")))
        return LicenseState(
            ok=True,
            tier=str(body.get("tier", "free")),
            label=str(body.get("label", "Free")),
            max_devices=int(body.get("max_devices", 1)),
            used_devices=int(body.get("used_devices", 0)),
            features=dict(body.get("features") or {}),
        )


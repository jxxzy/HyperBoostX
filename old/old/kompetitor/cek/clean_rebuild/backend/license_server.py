from __future__ import annotations

import argparse
import hashlib
import hmac
import json
import os
import secrets
import sqlite3
import sys
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parent
DB_PATH = ROOT / "licenses.sqlite3"
DEFAULT_HOST = "127.0.0.1"
DEFAULT_PORT = 8765
SECRET_ENV = "KENTANG_LICENSE_SECRET"
DEMO_SECRET = "dev-only-change-me"


FEATURES_FREE = {
    "performance_boost": True,
    "ram_flush": True,
    "process_flush": False,
    "remove_bloat": False,
    "process_manager": True,
    "presets_free": True,
    "game_mode": False,
    "hardware_monitor": False,
    "driver_update": False,
    "advanced_tweaks": False,
    "auto_ram": False,
}

FEATURES_VIP = {key: True for key in FEATURES_FREE}


def server_secret() -> bytes:
    return os.environ.get(SECRET_ENV, DEMO_SECRET).encode("utf-8")


def key_hash(raw_key: str) -> str:
    normalized = raw_key.strip().upper()
    return hmac.new(server_secret(), normalized.encode("utf-8"), hashlib.sha256).hexdigest()


def now_ts() -> int:
    return int(time.time())


def connect() -> sqlite3.Connection:
    con = sqlite3.connect(DB_PATH)
    con.row_factory = sqlite3.Row
    con.execute("PRAGMA foreign_keys = ON")
    return con


def init_db() -> None:
    with connect() as con:
        con.executescript(
            """
            CREATE TABLE IF NOT EXISTS licenses (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                key_hash TEXT NOT NULL UNIQUE,
                label TEXT NOT NULL,
                tier TEXT NOT NULL CHECK (tier IN ('free', 'vip')),
                max_devices INTEGER NOT NULL CHECK (max_devices > 0),
                active INTEGER NOT NULL DEFAULT 1,
                expires_at INTEGER,
                created_at INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS devices (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                license_id INTEGER NOT NULL REFERENCES licenses(id) ON DELETE CASCADE,
                hwid TEXT NOT NULL,
                hostname TEXT,
                first_seen INTEGER NOT NULL,
                last_seen INTEGER NOT NULL,
                UNIQUE (license_id, hwid)
            );
            """
        )


def upsert_license(raw_key: str, label: str, tier: str, max_devices: int, expires_at: int | None = None) -> None:
    init_db()
    if tier not in ("free", "vip"):
        raise ValueError("tier must be free or vip")
    with connect() as con:
        con.execute(
            """
            INSERT INTO licenses (key_hash, label, tier, max_devices, active, expires_at, created_at)
            VALUES (?, ?, ?, ?, 1, ?, ?)
            ON CONFLICT(key_hash) DO UPDATE SET
                label = excluded.label,
                tier = excluded.tier,
                max_devices = excluded.max_devices,
                active = 1,
                expires_at = excluded.expires_at
            """,
            (key_hash(raw_key), label, tier, max_devices, expires_at, now_ts()),
        )


def seed_demo() -> None:
    upsert_license("FREE-DEMO-0000", "Demo Free", "free", 1)
    upsert_license("VIP-DEMO-0000", "Demo VIP", "vip", 3)


def license_payload(row: sqlite3.Row, used_devices: int) -> dict[str, Any]:
    tier = row["tier"]
    return {
        "ok": True,
        "tier": tier,
        "label": row["label"],
        "max_devices": row["max_devices"],
        "used_devices": used_devices,
        "expires_at": row["expires_at"],
        "features": FEATURES_VIP if tier == "vip" else FEATURES_FREE,
        "issued_at": now_ts(),
    }


def sign_payload(payload: dict[str, Any]) -> dict[str, Any]:
    canonical = json.dumps(payload, sort_keys=True, separators=(",", ":")).encode("utf-8")
    signature = hmac.new(server_secret(), canonical, hashlib.sha256).hexdigest()
    payload = dict(payload)
    payload["server_sig"] = signature
    return payload


def lookup_license(con: sqlite3.Connection, raw_key: str) -> sqlite3.Row | None:
    return con.execute(
        "SELECT * FROM licenses WHERE key_hash = ?",
        (key_hash(raw_key),),
    ).fetchone()


def validate_common(raw_key: str, hwid: str) -> tuple[bool, str | None]:
    if not raw_key or not raw_key.strip():
        return False, "key is empty"
    if not hwid or len(hwid.strip()) < 8:
        return False, "hwid is invalid"
    return True, None


def activate(raw_key: str, hwid: str, hostname: str | None) -> dict[str, Any]:
    valid, error = validate_common(raw_key, hwid)
    if not valid:
        return {"ok": False, "error": error}

    init_db()
    with connect() as con:
        row = lookup_license(con, raw_key)
        if row is None:
            return {"ok": False, "error": "key invalid"}
        if not row["active"]:
            return {"ok": False, "error": "key inactive"}
        if row["expires_at"] is not None and int(row["expires_at"]) < now_ts():
            return {"ok": False, "error": "key expired"}

        existing = con.execute(
            "SELECT id FROM devices WHERE license_id = ? AND hwid = ?",
            (row["id"], hwid),
        ).fetchone()
        used = con.execute(
            "SELECT COUNT(*) AS n FROM devices WHERE license_id = ?",
            (row["id"],),
        ).fetchone()["n"]

        if existing is None and used >= row["max_devices"]:
            return {
                "ok": False,
                "error": f"device limit reached ({used}/{row['max_devices']})",
            }

        if existing is None:
            con.execute(
                """
                INSERT INTO devices (license_id, hwid, hostname, first_seen, last_seen)
                VALUES (?, ?, ?, ?, ?)
                """,
                (row["id"], hwid, hostname, now_ts(), now_ts()),
            )
            used += 1
        else:
            con.execute(
                "UPDATE devices SET hostname = ?, last_seen = ? WHERE id = ?",
                (hostname, now_ts(), existing["id"]),
            )

        return sign_payload(license_payload(row, used))


def status(raw_key: str, hwid: str) -> dict[str, Any]:
    valid, error = validate_common(raw_key, hwid)
    if not valid:
        return {"ok": False, "error": error}

    init_db()
    with connect() as con:
        row = lookup_license(con, raw_key)
        if row is None or not row["active"]:
            return {"ok": False, "error": "key invalid"}
        linked = con.execute(
            "SELECT 1 FROM devices WHERE license_id = ? AND hwid = ?",
            (row["id"], hwid),
        ).fetchone()
        if linked is None:
            return {"ok": False, "error": "device not registered"}
        used = con.execute(
            "SELECT COUNT(*) AS n FROM devices WHERE license_id = ?",
            (row["id"],),
        ).fetchone()["n"]
        return sign_payload(license_payload(row, used))


def deactivate(raw_key: str, hwid: str) -> dict[str, Any]:
    valid, error = validate_common(raw_key, hwid)
    if not valid:
        return {"ok": False, "error": error}

    init_db()
    with connect() as con:
        row = lookup_license(con, raw_key)
        if row is None:
            return {"ok": False, "error": "key invalid"}
        con.execute(
            "DELETE FROM devices WHERE license_id = ? AND hwid = ?",
            (row["id"], hwid),
        )
        return {"ok": True}


class Handler(BaseHTTPRequestHandler):
    server_version = "KentangLicense/2.0"

    def do_GET(self) -> None:
        if self.path == "/health":
            self.write_json({"ok": True, "time": now_ts()})
            return
        self.write_json({"ok": False, "error": "not found"}, status_code=404)

    def do_POST(self) -> None:
        try:
            payload = self.read_json()
            if self.path == "/api/license/activate":
                result = activate(payload.get("key", ""), payload.get("hwid", ""), payload.get("hostname"))
            elif self.path == "/api/license/status":
                result = status(payload.get("key", ""), payload.get("hwid", ""))
            elif self.path == "/api/license/deactivate":
                result = deactivate(payload.get("key", ""), payload.get("hwid", ""))
            else:
                result = {"ok": False, "error": "not found"}
                self.write_json(result, status_code=404)
                return
            self.write_json(result, status_code=200 if result.get("ok") else 400)
        except Exception as exc:
            self.write_json({"ok": False, "error": f"server error: {exc}"}, status_code=500)

    def log_message(self, fmt: str, *args: Any) -> None:
        sys.stderr.write("%s - %s\n" % (self.address_string(), fmt % args))

    def read_json(self) -> dict[str, Any]:
        length = int(self.headers.get("Content-Length", "0"))
        if length > 64 * 1024:
            raise ValueError("request too large")
        raw = self.rfile.read(length)
        if not raw:
            return {}
        return json.loads(raw.decode("utf-8"))

    def write_json(self, payload: dict[str, Any], status_code: int = 200) -> None:
        raw = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        self.send_response(status_code)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Cache-Control", "no-store")
        self.send_header("Content-Length", str(len(raw)))
        self.end_headers()
        self.wfile.write(raw)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", default=DEFAULT_HOST)
    parser.add_argument("--port", type=int, default=DEFAULT_PORT)
    parser.add_argument("--init-demo", action="store_true")
    parser.add_argument("--create-key", help="create or update a key")
    parser.add_argument("--label", default="Manual Key")
    parser.add_argument("--tier", choices=["free", "vip"], default="vip")
    parser.add_argument("--max-devices", type=int, default=1)
    args = parser.parse_args()

    init_db()
    if args.init_demo:
        seed_demo()
        print("Demo keys ready: FREE-DEMO-0000, VIP-DEMO-0000")

    if args.create_key:
        upsert_license(args.create_key, args.label, args.tier, args.max_devices)
        print(f"Key created/updated: {args.label} ({args.tier}, max_devices={args.max_devices})")
        return

    httpd = ThreadingHTTPServer((args.host, args.port), Handler)
    print(f"License backend listening on http://{args.host}:{args.port}")
    print(f"Database: {DB_PATH}")
    httpd.serve_forever()


if __name__ == "__main__":
    main()


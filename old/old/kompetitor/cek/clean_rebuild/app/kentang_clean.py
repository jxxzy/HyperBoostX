from __future__ import annotations

import json
import os
import threading
import tkinter as tk
from pathlib import Path
from tkinter import filedialog, messagebox, ttk

import actions
from license_client import DEFAULT_ENDPOINT, LicenseClient, LicenseState
from presets import export_preset, import_preset


APP_NAME = "Kentang Optimizer"
APP_TITLE = "Kentang Optimizer by Prosnatics"
STATE_DIR = Path(os.environ.get("APPDATA", str(Path.home()))) / "KentangOptimizerClean"
STATE_FILE = STATE_DIR / "state.json"


FEATURES = [
    {
        "id": "performance_boost",
        "group": "ADVANCED",
        "title": "Performance Boost",
        "desc": "Core cleanup, DNS refresh, and safe performance housekeeping.",
        "vip": False,
    },
    {
        "id": "ram_flush",
        "group": "ADVANCED",
        "title": "RAM Flush",
        "desc": "Trim current process working set and refresh memory pressure.",
        "vip": False,
    },
    {
        "id": "process_flush",
        "group": "ADVANCED",
        "title": "Process Flush",
        "desc": "Deeper cleanup flow for VIP users.",
        "vip": True,
    },
    {
        "id": "remove_bloat",
        "group": "ADVANCED",
        "title": "Remove Bloat",
        "desc": "Clean recycle bin and temp junk without touching personal files.",
        "vip": True,
    },
    {
        "id": "process_manager",
        "group": "TOOLS",
        "title": "Process Mgr",
        "desc": "Inspect running processes using Windows tasklist.",
        "vip": False,
    },
    {
        "id": "presets_free",
        "group": "TOOLS",
        "title": "Presets",
        "desc": "Save and share your FREE tweak config.",
        "vip": False,
    },
    {
        "id": "game_mode",
        "group": "TOOLS",
        "title": "Game Mode",
        "desc": "One-click gaming optimization exclusive for VIP users.",
        "vip": True,
    },
    {
        "id": "hardware_monitor",
        "group": "TOOLS",
        "title": "Hardware Monitor",
        "desc": "Monitor GPU/CPU/RAM live.",
        "vip": True,
    },
    {
        "id": "driver_update",
        "group": "TOOLS",
        "title": "Driver Update",
        "desc": "Auto update drivers via winget.",
        "vip": True,
    },
    {
        "id": "advanced_tweaks",
        "group": "TOOLS",
        "title": "Tweak Advanced",
        "desc": "300+ CPU/GPU/RAM tweak blueprint, rebuilt as safe actions.",
        "vip": True,
    },
    {
        "id": "auto_ram",
        "group": "TOOLS",
        "title": "Auto RAM",
        "desc": "Auto trim RAM every 2 hours.",
        "vip": True,
    },
]


TEXT = {
    "id": {
        "unlock": "Unlock VIP",
        "enter_key": "Masukkan key VIP",
        "logout": "Logout Key",
        "join": "Join Discord",
        "apply": "Apply",
        "apply_all": "Apply All",
        "check": "Check Status",
        "reset": "Reset All Tweaks",
        "restart": "Restart PC",
        "free": "Free",
        "vip_active": "VIP Aktif",
        "no_selection": "No tweaks selected.",
        "admin": "Run as Administrator first.",
        "preview": "preview mode",
        "vip_hint": "view VIP tweaks, apply needs key",
    },
    "en": {
        "unlock": "Unlock VIP",
        "enter_key": "Enter VIP key",
        "logout": "Logout Key",
        "join": "Join Discord",
        "apply": "Apply",
        "apply_all": "Apply All",
        "check": "Check Status",
        "reset": "Reset All Tweaks",
        "restart": "Restart PC",
        "free": "Free",
        "vip_active": "VIP Active",
        "no_selection": "No tweaks selected.",
        "admin": "Run as Administrator first.",
        "preview": "preview mode",
        "vip_hint": "view VIP tweaks, apply needs key",
    },
}


def load_state() -> dict[str, object]:
    try:
        return json.loads(STATE_FILE.read_text(encoding="utf-8"))
    except Exception:
        return {"language": "id", "endpoint": DEFAULT_ENDPOINT, "selected": []}


def save_state(state: dict[str, object]) -> None:
    STATE_DIR.mkdir(parents=True, exist_ok=True)
    STATE_FILE.write_text(json.dumps(state, indent=2), encoding="utf-8")


class KentangApp(tk.Tk):
    def __init__(self) -> None:
        super().__init__()
        self.title(APP_TITLE)
        self.geometry("1020x680")
        self.minsize(980, 640)

        self.state_data = load_state()
        self.lang = str(self.state_data.get("language", "id"))
        self.license = LicenseState()
        self.license_client = LicenseClient(str(self.state_data.get("endpoint", DEFAULT_ENDPOINT)))
        self.vars: dict[str, tk.BooleanVar] = {}
        self.status_var = tk.StringVar(value="Ready")
        self.tier_var = tk.StringVar(value=TEXT[self.lang]["free"])
        self.endpoint_var = tk.StringVar(value=self.license_client.endpoint)
        self.key_var = tk.StringVar(value=str(self.state_data.get("saved_key", "")))

        self.configure(bg="#0b0b0d")
        self._style()
        self._build_ui()
        self._restore_saved_key()

    def t(self, key: str) -> str:
        return TEXT[self.lang].get(key, key)

    def _style(self) -> None:
        style = ttk.Style()
        style.theme_use("clam")
        style.configure("TButton", padding=(12, 8), font=("Segoe UI", 10, "bold"))
        style.configure("TCheckbutton", background="#111216", foreground="#f2f2f2", font=("Segoe UI", 10))
        style.configure("TEntry", fieldbackground="#17181d", foreground="#f2f2f2")

    def _build_ui(self) -> None:
        self.columnconfigure(1, weight=1)
        self.rowconfigure(0, weight=1)

        self.sidebar = tk.Frame(self, bg="#111216", width=230)
        self.sidebar.grid(row=0, column=0, sticky="nsew")
        self.sidebar.grid_propagate(False)

        self.main = tk.Frame(self, bg="#0b0b0d")
        self.main.grid(row=0, column=1, sticky="nsew")
        self.main.columnconfigure(0, weight=1)
        self.main.rowconfigure(1, weight=1)

        self._build_sidebar()
        self._build_header()
        self._build_content()
        self._build_footer()

    def _build_sidebar(self) -> None:
        logo = tk.Label(self.sidebar, text=APP_NAME, bg="#111216", fg="#f4f4f5", font=("Segoe UI", 18, "bold"))
        logo.pack(anchor="w", padx=18, pady=(18, 2))
        made = tk.Label(self.sidebar, text="Prosnatics", bg="#111216", fg="#7a4de0", font=("Segoe UI", 10, "bold"))
        made.pack(anchor="w", padx=20, pady=(0, 16))

        tier = tk.Label(self.sidebar, textvariable=self.tier_var, bg="#2d1f00", fg="#f7b733", font=("Segoe UI", 10, "bold"))
        tier.pack(anchor="w", padx=18, pady=(0, 14), ipadx=10, ipady=4)

        for group in ["ADVANCED", "TOOLS", "Troubleshoot"]:
            label = tk.Label(self.sidebar, text=f"  {group}", bg="#111216", fg="#a1a1aa", font=("Segoe UI", 9, "bold"))
            label.pack(anchor="w", fill="x", padx=8, pady=(12, 4))
            for feat in [item for item in FEATURES if item["group"].lower() == group.lower()]:
                text = feat["title"] + ("  VIP" if feat["vip"] else "")
                btn = tk.Button(
                    self.sidebar,
                    text=text,
                    anchor="w",
                    bg="#111216",
                    fg="#e4e4e7",
                    activebackground="#1b1c22",
                    activeforeground="#ffffff",
                    relief="flat",
                    command=lambda fid=feat["id"]: self._focus_feature(fid),
                )
                btn.pack(fill="x", padx=12, pady=1)

        bottom = tk.Frame(self.sidebar, bg="#111216")
        bottom.pack(side="bottom", fill="x", padx=12, pady=14)
        tk.Button(bottom, text="ID / EN", command=self._toggle_language, bg="#191919", fg="#e8e8e8", relief="flat").pack(fill="x", pady=3)
        tk.Button(bottom, text=self.t("join"), command=lambda: self._log("Discord URL baru belum disetel."), bg="#191919", fg="#e8e8e8", relief="flat").pack(fill="x", pady=3)
        tk.Button(bottom, text=self.t("logout"), command=self._logout_key, bg="#2d2000", fg="#f7b733", relief="flat").pack(fill="x", pady=3)

    def _build_header(self) -> None:
        header = tk.Frame(self.main, bg="#0b0b0d")
        header.grid(row=0, column=0, sticky="ew", padx=22, pady=(18, 8))
        header.columnconfigure(1, weight=1)

        title = tk.Label(header, text=APP_NAME, bg="#0b0b0d", fg="#f8fafc", font=("Segoe UI", 22, "bold"))
        title.grid(row=0, column=0, sticky="w")
        tk.Label(header, textvariable=self.status_var, bg="#0b0b0d", fg="#94a3b8", font=("Segoe UI", 10)).grid(row=1, column=0, sticky="w")

        tk.Entry(header, textvariable=self.endpoint_var, bg="#17181d", fg="#e8e8e8", insertbackground="#e8e8e8", relief="flat", width=34).grid(row=0, column=1, sticky="e", padx=(12, 8))
        tk.Button(header, text=self.t("unlock"), command=self._show_vip_dialog, bg="#e89a0a", fg="#080808", relief="flat", font=("Segoe UI", 10, "bold")).grid(row=0, column=2, sticky="e")

    def _build_content(self) -> None:
        self.canvas = tk.Canvas(self.main, bg="#0b0b0d", highlightthickness=0)
        self.canvas.grid(row=1, column=0, sticky="nsew", padx=22, pady=8)
        scroll = ttk.Scrollbar(self.main, orient="vertical", command=self.canvas.yview)
        scroll.grid(row=1, column=1, sticky="ns")
        self.canvas.configure(yscrollcommand=scroll.set)

        self.cards = tk.Frame(self.canvas, bg="#0b0b0d")
        self.canvas.create_window((0, 0), window=self.cards, anchor="nw")
        self.cards.bind("<Configure>", lambda _event: self.canvas.configure(scrollregion=self.canvas.bbox("all")))
        self.cards.columnconfigure(0, weight=1)
        self.cards.columnconfigure(1, weight=1)

        for idx, feat in enumerate(FEATURES):
            self._feature_card(idx, feat)

    def _feature_card(self, idx: int, feat: dict[str, object]) -> None:
        row, col = divmod(idx, 2)
        card = tk.Frame(self.cards, bg="#14151a", highlightbackground="#252733", highlightthickness=1)
        card.grid(row=row, column=col, sticky="nsew", padx=8, pady=8)
        card.columnconfigure(0, weight=1)

        head = tk.Frame(card, bg="#14151a")
        head.grid(row=0, column=0, sticky="ew", padx=14, pady=(12, 4))
        head.columnconfigure(0, weight=1)
        tk.Label(head, text=str(feat["title"]), bg="#14151a", fg="#f8fafc", font=("Segoe UI", 13, "bold")).grid(row=0, column=0, sticky="w")
        if feat["vip"]:
            tk.Label(head, text="VIP", bg="#2a2000", fg="#f7b733", font=("Segoe UI", 9, "bold")).grid(row=0, column=1, sticky="e", padx=(8, 0), ipadx=8, ipady=2)

        tk.Label(card, text=str(feat["desc"]), bg="#14151a", fg="#a1a1aa", justify="left", wraplength=330, font=("Segoe UI", 10)).grid(row=1, column=0, sticky="w", padx=14, pady=(0, 10))
        var = tk.BooleanVar(value=str(feat["id"]) in self.state_data.get("selected", []))
        self.vars[str(feat["id"])] = var
        ttk.Checkbutton(card, text="Select", variable=var, command=self._save_selection).grid(row=2, column=0, sticky="w", padx=12, pady=(0, 12))
        tk.Button(card, text=self.t("apply"), command=lambda fid=str(feat["id"]): self._apply_feature(fid), bg="#1f6f4a", fg="#f8fafc", relief="flat").grid(row=3, column=0, sticky="ew", padx=14, pady=(0, 14))

    def _build_footer(self) -> None:
        footer = tk.Frame(self.main, bg="#0b0b0d")
        footer.grid(row=2, column=0, sticky="ew", padx=22, pady=(8, 18))
        for i in range(5):
            footer.columnconfigure(i, weight=1)
        tk.Button(footer, text=self.t("apply_all"), command=self._apply_all, bg="#e89a0a", fg="#080808", relief="flat", font=("Segoe UI", 10, "bold")).grid(row=0, column=0, sticky="ew", padx=4)
        tk.Button(footer, text=self.t("check"), command=self._check_status, bg="#1b1c22", fg="#f8fafc", relief="flat").grid(row=0, column=1, sticky="ew", padx=4)
        tk.Button(footer, text="Save Preset", command=self._save_preset, bg="#1b1c22", fg="#f8fafc", relief="flat").grid(row=0, column=2, sticky="ew", padx=4)
        tk.Button(footer, text="Import Preset", command=self._import_preset, bg="#1b1c22", fg="#f8fafc", relief="flat").grid(row=0, column=3, sticky="ew", padx=4)
        tk.Button(footer, text=self.t("reset"), command=self._reset_all, bg="#4b1111", fg="#f8fafc", relief="flat").grid(row=0, column=4, sticky="ew", padx=4)

    def _restore_saved_key(self) -> None:
        key = str(self.state_data.get("saved_key", ""))
        if not key:
            return
        self._run_bg(lambda: self.license_client.status(key), self._on_license_result)

    def _show_vip_dialog(self) -> None:
        self.license_client.endpoint = self.endpoint_var.get().strip().rstrip("/")
        dialog = tk.Toplevel(self)
        dialog.title("Kentang Optimizer VIP")
        dialog.configure(bg="#111111")
        dialog.resizable(False, False)
        dialog.transient(self)
        dialog.grab_set()

        tk.Label(dialog, text="Kentang Optimizer VIP", bg="#111111", fg="#e8e8e8", font=("Segoe UI", 18, "bold")).pack(anchor="w", padx=18, pady=(18, 4))
        tk.Label(dialog, text="all features unlocked, no limits", bg="#111111", fg="#94a3b8", font=("Segoe UI", 10)).pack(anchor="w", padx=18)
        tk.Entry(dialog, textvariable=self.key_var, width=38, bg="#161616", fg="#e8e8e8", insertbackground="#e8e8e8", relief="flat", font=("Consolas", 11)).pack(fill="x", padx=18, pady=16)

        preview = [
            "Game Mode - enable gaming mode in 1 click",
            "FPS Boost Pro - GPU+CPU+RAM tweaks for gaming",
            "Hardware Monitor - monitor GPU/CPU/RAM live",
            "Driver Update - auto update drivers via winget",
            "Auto RAM - auto trim RAM every 2 hours",
        ]
        for text in preview:
            tk.Label(dialog, text=text, bg="#111111", fg="#e0e0e0", font=("Segoe UI", 9), anchor="w").pack(fill="x", padx=22)

        result = tk.StringVar(value="")
        tk.Label(dialog, textvariable=result, bg="#111111", fg="#f97316", font=("Segoe UI", 10)).pack(anchor="w", padx=18, pady=(12, 0))

        def submit() -> None:
            key = self.key_var.get().strip()
            if not key:
                result.set("key is empty")
                return
            result.set("Checking...")
            self._run_bg(lambda: self.license_client.activate(key), lambda state: self._after_dialog_license(dialog, result, state, key))

        row = tk.Frame(dialog, bg="#111111")
        row.pack(fill="x", padx=18, pady=18)
        tk.Button(row, text="Enter VIP", command=submit, bg="#e89a0a", fg="#000000", relief="flat", font=("Segoe UI", 10, "bold")).pack(side="left", fill="x", expand=True, padx=(0, 6))
        tk.Button(row, text="Later", command=dialog.destroy, bg="#252525", fg="#e8e8e8", relief="flat").pack(side="left", fill="x", expand=True, padx=(6, 0))

    def _after_dialog_license(self, dialog: tk.Toplevel, result: tk.StringVar, state: LicenseState, key: str) -> None:
        if not state.ok:
            result.set(state.error or "key invalid")
            return
        self._on_license_result(state)
        self.state_data["saved_key"] = key
        self.state_data["endpoint"] = self.license_client.endpoint
        save_state(self.state_data)
        dialog.destroy()
        if state.is_vip:
            messagebox.showinfo("Kentang Optimizer VIP", "All VIP features are unlocked.\nCheck and Apply as needed.")
        else:
            messagebox.showwarning("Kentang Optimizer", "this is a free key, not VIP")

    def _on_license_result(self, state: LicenseState) -> None:
        self.license = state
        if state.ok:
            label = self.t("vip_active") if state.is_vip else self.t("free")
            self.tier_var.set(f"{label} - {state.label}")
            self.status_var.set(f"{state.tier.upper()} | slots {state.used_devices}/{state.max_devices}")
        else:
            self.tier_var.set(self.t("free"))
            self.status_var.set(state.error or "Free mode")

    def _logout_key(self) -> None:
        key = str(self.state_data.get("saved_key", "")).strip()
        if not key:
            self.status_var.set("No saved key.")
            return
        self._run_bg(lambda: self.license_client.deactivate(key), lambda _state: self._finish_logout())

    def _finish_logout(self) -> None:
        self.state_data.pop("saved_key", None)
        save_state(self.state_data)
        self.license = LicenseState()
        self.tier_var.set(self.t("free"))
        self.status_var.set("Device Logout")

    def _feature_allowed(self, feature_id: str) -> bool:
        feat = next((item for item in FEATURES if item["id"] == feature_id), None)
        if not feat:
            return False
        if not feat["vip"]:
            return True
        return self.license.is_vip and self.license.features.get(feature_id, True)

    def _focus_feature(self, feature_id: str) -> None:
        self.vars.get(feature_id, tk.BooleanVar()).set(True)
        self._save_selection()
        self.status_var.set(f"Selected: {feature_id}")

    def _apply_feature(self, feature_id: str) -> None:
        if not self._feature_allowed(feature_id):
            self.status_var.set(self.t("vip_hint"))
            self._show_vip_dialog()
            return
        self.status_var.set(f"Applying: {feature_id}")
        self._run_bg(lambda: actions.apply_feature(feature_id), self._show_results)

    def _apply_all(self) -> None:
        selected = [fid for fid, var in self.vars.items() if var.get()]
        if not selected:
            self.status_var.set(self.t("no_selection"))
            return
        denied = [fid for fid in selected if not self._feature_allowed(fid)]
        if denied:
            self.status_var.set(self.t("vip_hint"))
            self._show_vip_dialog()
            return

        def run_all() -> list[actions.ActionResult]:
            results: list[actions.ActionResult] = []
            for fid in selected:
                results.extend(actions.apply_feature(fid))
            return results

        self.status_var.set("Applying selected tweaks...")
        self._run_bg(run_all, self._show_results)

    def _check_status(self) -> None:
        self._run_bg(actions.check_status, self._show_results)

    def _reset_all(self) -> None:
        if messagebox.askyesno(
            "Reset All Tweaks",
            "Ini akan MENGEMBALIKAN semua registry tweaks ke nilai default Windows.\nSemua optimasi yang kamu apply akan dibatalkan.\nLanjutkan?",
        ):
            for var in self.vars.values():
                var.set(False)
            self._save_selection()
            self.status_var.set("Reset All done. Restart PC.")

    def _save_preset(self) -> None:
        path = filedialog.asksaveasfilename(defaultextension=".kfreepreset", filetypes=[("Kentang preset", "*.kfreepreset")])
        if not path:
            return
        payload = {
            "selected": [fid for fid, var in self.vars.items() if var.get()],
            "language": self.lang,
            "tier": self.license.tier,
        }
        export_preset(Path(path), payload)
        self.status_var.set("Preset saved.")

    def _import_preset(self) -> None:
        path = filedialog.askopenfilename(filetypes=[("Kentang preset", "*.kfreepreset"), ("All files", "*.*")])
        if not path:
            return
        try:
            payload = import_preset(Path(path))
        except Exception as exc:
            messagebox.showerror(APP_NAME, str(exc))
            return
        selected = set(payload.get("selected", []))
        for fid, var in self.vars.items():
            var.set(fid in selected)
        self._save_selection()
        self.status_var.set("Preset imported.")

    def _save_selection(self) -> None:
        self.state_data["selected"] = [fid for fid, var in self.vars.items() if var.get()]
        self.state_data["language"] = self.lang
        self.state_data["endpoint"] = self.endpoint_var.get().strip()
        save_state(self.state_data)

    def _toggle_language(self) -> None:
        self.lang = "en" if self.lang == "id" else "id"
        self.state_data["language"] = self.lang
        save_state(self.state_data)
        messagebox.showinfo(APP_NAME, "Language changed. Restart app to refresh all labels.")

    def _show_results(self, results: list[actions.ActionResult]) -> None:
        lines = []
        ok_count = 0
        for result in results:
            mark = "OK" if result.ok else "FAIL"
            ok_count += 1 if result.ok else 0
            lines.append(f"[{mark}] {result.title}: {result.detail}")
        self._log("\n".join(lines))
        self.status_var.set(f"done, restart PC if needed ({ok_count}/{len(results)} ok)")

    def _log(self, text: str) -> None:
        win = tk.Toplevel(self)
        win.title("Kentang Optimizer Log")
        win.geometry("720x420")
        win.configure(bg="#111111")
        box = tk.Text(win, bg="#0b0b0d", fg="#e8e8e8", insertbackground="#e8e8e8", relief="flat", wrap="word")
        box.pack(fill="both", expand=True, padx=12, pady=12)
        box.insert("1.0", text)
        box.configure(state="disabled")

    def _run_bg(self, func, callback) -> None:
        def worker() -> None:
            result = func()
            self.after(0, lambda: callback(result))

        threading.Thread(target=worker, daemon=True).start()


if __name__ == "__main__":
    KentangApp().mainloop()

